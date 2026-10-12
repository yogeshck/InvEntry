using DataAccess.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace DataAccess.Services;

public sealed record FinanceClosingReservationRequest(Guid ReservationId,
    int OrgGkey, int LocationGkey, DateOnly BusinessDate);
public sealed record FinanceClosingReservationResult(Guid ReservationId,
    string FenceStatus, FinanceClosingEligibilityResult Eligibility);

public sealed class FinanceClosingFenceService
{
    public const string Reserved = "RESERVED";
    public const string Uncertain = "UNCERTAIN";
    public const string Confirmed = "CONFIRMED";
    public const string Cancelled = "CANCELLED";
    private readonly MijmsContext _context;
    private readonly FinanceClosingEligibilityService _eligibility;

    public FinanceClosingFenceService(MijmsContext context,
        FinanceClosingEligibilityService eligibility) =>
        (_context, _eligibility) = (context, eligibility);

    public async Task<FinanceClosingReservationResult> ReserveAsync(
        FinanceClosingReservationRequest request, CancellationToken token)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, token);
        await AcquireDateLockAsync(request.OrgGkey, request.LocationGkey,
            request.BusinessDate, token);

        var existing = await _context.FinanceClosingFences.SingleOrDefaultAsync(x =>
            x.OrgGkey == request.OrgGkey && x.LocationGkey == request.LocationGkey &&
            x.BusinessDate == request.BusinessDate, token);
        if (existing != null && existing.ReservationId == request.ReservationId &&
            existing.Status != Cancelled)
        {
            var repeatedEligibility = await _eligibility.EvaluateAsync(request.OrgGkey,
                request.LocationGkey, request.BusinessDate, token);
            await transaction.CommitAsync(token);
            return new(existing.ReservationId, existing.Status, repeatedEligibility);
        }
        if (existing != null && existing.Status is Reserved or Uncertain or Confirmed)
            throw new InvalidOperationException(existing.Status == Confirmed
                ? "The selected business date is already closed."
                : "The selected business date is currently being closed.");

        var eligibility = await _eligibility.EvaluateAsync(request.OrgGkey,
            request.LocationGkey, request.BusinessDate, token);
        if (eligibility.Decision != "CLEAR")
        {
            await transaction.RollbackAsync(token);
            return new(request.ReservationId, "NOT_RESERVED", eligibility);
        }

        var now = DateTime.UtcNow;
        if (existing == null)
        {
            existing = new FinanceClosingFence
            {
                OrgGkey = request.OrgGkey, LocationGkey = request.LocationGkey,
                BusinessDate = request.BusinessDate, ReservationId = request.ReservationId,
                Status = Reserved, CreatedOnUtc = now, ModifiedOnUtc = now
            };
            _context.FinanceClosingFences.Add(existing);
        }
        else
        {
            existing.ReservationId = request.ReservationId;
            existing.Status = Reserved;
            existing.ModifiedOnUtc = now;
        }
        await _context.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return new(request.ReservationId, Reserved, eligibility);
    }

    public Task ConfirmAsync(Guid id, CancellationToken token) =>
        TransitionAsync(id, Confirmed, allowFromCancelled: false, token);
    public Task MarkUncertainAsync(Guid id, CancellationToken token) =>
        TransitionAsync(id, Uncertain, allowFromCancelled: false, token);
    public Task CancelAsync(Guid id, CancellationToken token) =>
        TransitionAsync(id, Cancelled, allowFromCancelled: true, token);
    public Task CancelAfterVerifiedOpenAsync(Guid id, CancellationToken token) =>
        TransitionAsync(id, Cancelled, allowFromCancelled: true, token,
            allowFromUncertain: true);

    public async Task EnsurePostingAllowedAsync(int org, int location,
        DateOnly date, CancellationToken token)
    {
        if (_context.Database.IsSqlServer() &&
            _context.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Invoice finalisation requires an active transaction.");
        await AcquireDateLockAsync(org, location, date, token);
        var blocked = await _context.FinanceClosingFences.AsNoTracking().AnyAsync(x =>
            x.OrgGkey == org && x.LocationGkey == location && x.BusinessDate == date &&
            (x.Status == Reserved || x.Status == Uncertain || x.Status == Confirmed), token);
        if (blocked)
            throw new InvalidOperationException(
                "The selected business date is currently being closed or is already closed.");
    }

    private async Task TransitionAsync(Guid id, string target,
        bool allowFromCancelled, CancellationToken token,
        bool allowFromUncertain = false)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, token);
        var row = await _context.FinanceClosingFences.SingleOrDefaultAsync(
            x => x.ReservationId == id, token)
            ?? throw new KeyNotFoundException("Closing reservation was not found.");
        await AcquireDateLockAsync(row.OrgGkey, row.LocationGkey, row.BusinessDate, token);

        if (row.Status == target || row.Status == Confirmed && target == Confirmed ||
            row.Status == Cancelled && target == Cancelled)
        {
            await transaction.CommitAsync(token);
            return;
        }
        if (row.Status == Confirmed || row.Status == Cancelled && !allowFromCancelled)
            throw new InvalidOperationException("The closing reservation is already final.");
        if (target == Cancelled && row.Status == Uncertain && !allowFromUncertain)
            throw new InvalidOperationException(
                "An uncertain reservation requires authoritative closing-status verification before cancellation.");

        row.Status = target;
        row.ModifiedOnUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    private async Task AcquireDateLockAsync(int org, int location, DateOnly date,
        CancellationToken token)
    {
        if (!_context.Database.IsSqlServer()) return;
        var resource = new SqlParameter("@resource", SqlDbType.NVarChar, 255)
        { Value = $"FinanceClosing:{org}:{location}:{date:yyyy-MM-dd}" };
        var result = new SqlParameter("@result", SqlDbType.Int)
        { Direction = ParameterDirection.Output };
        await _context.Database.ExecuteSqlRawAsync(
            "EXEC @result = sys.sp_getapplock @Resource=@resource, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;",
            [result, resource], token);
        if (result.Value is not int value || value < 0)
            throw new InvalidOperationException(
                "The selected business date is currently being closed.");
    }
}
