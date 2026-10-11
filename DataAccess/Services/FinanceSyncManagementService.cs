using DataAccess.Models;
using DataAccess.Models.FinanceIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DataAccess.Services;

public sealed record FinanceSyncEventQuery(DateOnly? FromDate, DateOnly? ToDate,
    string? Status, string? DocumentNumber, int Page = 1, int PageSize = 25);
public sealed record FinanceSyncEventDto(long Id, string SourceEventId,
    string DocumentNumber, DateOnly? DocumentDate, decimal Amount, string Status,
    string DisplayStatus, int RetryCount, DateTime CreatedOn,
    DateTime? LastAttemptOn, DateTime? SentOn, string? SafeError,
    bool ManualDispatchEligible);
public sealed record FinanceSyncEventPage(int Page, int PageSize, int TotalCount,
    IReadOnlyList<FinanceSyncEventDto> Items);
public sealed record FinanceSyncSummaryDto(int Synchronized, int Pending,
    int Retryable, int NeedsAttention, int VerificationRequired,
    int HistoricalError);

public sealed class FinanceSyncManagementService
{
    public const int MaximumPageSize = 100;
    private readonly MijmsContext _context;
    private readonly FinanceSyncOptions _scope;
    public FinanceSyncManagementService(MijmsContext context,
        IOptions<FinanceSyncOptions> scope) => (_context, _scope) = (context, scope.Value);
    public (int OrgGkey, int LocationGkey) Scope => (_scope.OrgGkey, _scope.LocationGkey);

    public async Task<FinanceSyncEventPage> ListAsync(FinanceSyncEventQuery request,
        CancellationToken token)
    {
        Validate(request);
        var query = ApplyFilters(_context.FinanceSyncOutboxes.AsNoTracking(), request);
        var total = await query.CountAsync(token);
        var rows = await query.OrderByDescending(x => x.CreatedOn).ThenByDescending(x => x.Gkey)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(token);
        return new(request.Page, request.PageSize, total, rows.Select(ToDto).ToList());
    }

    public async Task<FinanceSyncEventDto?> FindAsync(long id, CancellationToken token)
    {
        var row = await _context.FinanceSyncOutboxes.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Gkey == id, token);
        return row == null ? null : ToDto(row);
    }

    public async Task<FinanceSyncSummaryDto> SummaryAsync(DateOnly? fromDate,
        DateOnly? toDate, CancellationToken token)
    {
        var request = new FinanceSyncEventQuery(fromDate, toDate, null, null);
        Validate(request);
        var pairs = await ApplyFilters(_context.FinanceSyncOutboxes.AsNoTracking(), request)
            .GroupBy(x => x.Status).Select(x => new { Status = x.Key, Count = x.Count() })
            .ToListAsync(token);
        var counts = pairs.ToDictionary(x => x.Status, x => x.Count,
            StringComparer.OrdinalIgnoreCase);
        int Count(string status) => counts.GetValueOrDefault(status);
        var verification = Count(FinanceSyncService.Uncertain);
        var historical = Count("ERROR");
        return new(Count(FinanceSyncService.Sent), Count(FinanceSyncService.Pending),
            Count(FinanceSyncService.RetryableError),
            Count(FinanceSyncService.BusinessError) + verification + historical,
            verification, historical);
    }

    public async Task<IReadOnlyList<long>> EligibleIdsAsync(
        IReadOnlyCollection<long> ids, CancellationToken token)
    {
        if (ids.Count is < 1 or > 100) throw new ArgumentException("Select between 1 and 100 events.");
        return await _context.FinanceSyncOutboxes.AsNoTracking()
            .Where(x => ids.Contains(x.Gkey) && (x.Status == FinanceSyncService.Pending ||
                x.Status == FinanceSyncService.RetryableError || x.Status == FinanceSyncService.Uncertain))
            .Select(x => x.Gkey).ToListAsync(token);
    }

    internal static bool IsEligible(string status) => status == FinanceSyncService.Pending ||
        status == FinanceSyncService.RetryableError || status == FinanceSyncService.Uncertain;
    internal static string DisplayStatus(string status) => status switch
    {
        FinanceSyncService.Sent => "Synchronized",
        FinanceSyncService.Pending => "Waiting",
        FinanceSyncService.RetryableError => "Retry available",
        FinanceSyncService.Uncertain => "Verification required",
        FinanceSyncService.BusinessError => "Needs attention",
        "ERROR" => "Historical error",
        _ => "Unknown"
    };

    private static IQueryable<FinanceSyncOutbox> ApplyFilters(
        IQueryable<FinanceSyncOutbox> query, FinanceSyncEventQuery request)
    {
        if (request.FromDate.HasValue) { var d = request.FromDate.Value.ToDateTime(TimeOnly.MinValue); query = query.Where(x => x.DocumentDate >= d); }
        if (request.ToDate.HasValue) { var d = request.ToDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue); query = query.Where(x => x.DocumentDate < d); }
        if (!string.IsNullOrWhiteSpace(request.Status)) { var s = request.Status.Trim().ToUpperInvariant(); query = query.Where(x => x.Status == s); }
        if (!string.IsNullOrWhiteSpace(request.DocumentNumber)) { var n = request.DocumentNumber.Trim(); query = query.Where(x => x.DocumentNo != null && x.DocumentNo.Contains(n)); }
        return query;
    }

    private static void Validate(FinanceSyncEventQuery request)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > MaximumPageSize) throw new ArgumentException("page must be positive and pageSize must be between 1 and 100.");
        if (request.FromDate > request.ToDate) throw new ArgumentException("fromDate must not be after toDate.");
        if (request.FromDate.HasValue && request.ToDate.HasValue && request.ToDate.Value.DayNumber - request.FromDate.Value.DayNumber > 366) throw new ArgumentException("The date range cannot exceed 366 days.");
        if (request.DocumentNumber?.Length > 50) throw new ArgumentException("Document number filter is too long.");
        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status.Trim().ToUpperInvariant() is not
            (FinanceSyncService.Pending or FinanceSyncService.Sent or FinanceSyncService.Uncertain or
             FinanceSyncService.RetryableError or FinanceSyncService.BusinessError or "ERROR"))
            throw new ArgumentException("Select a supported synchronization status.");
    }

    private static FinanceSyncEventDto ToDto(FinanceSyncOutbox row)
    {
        decimal amount = 0;
        try { amount = JsonSerializer.Deserialize<FinanceDocumentPayload>(row.Payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.DocumentAmount ?? 0; }
        catch (JsonException) { }
        return new(row.Gkey, row.SourceEventId, row.DocumentNo ?? string.Empty,
            row.DocumentDate.HasValue ? DateOnly.FromDateTime(row.DocumentDate.Value) : null,
            amount, row.Status, DisplayStatus(row.Status), row.RetryCount, row.CreatedOn,
            row.LastAttemptOn, row.SentOn, SafeError(row.LastError), IsEligible(row.Status));
    }
    private static string? SafeError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error)) return null;
        var value = error.Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (value.Contains("timed out", StringComparison.OrdinalIgnoreCase))
            return "FinanceTracker did not respond before the request timeout; verification is required.";
        if (value.Contains("response was not received", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Unable to verify", StringComparison.OrdinalIgnoreCase))
            return "FinanceTracker could not be reached or verified; review connectivity before retrying.";
        if (value.StartsWith("HTTP 409", StringComparison.OrdinalIgnoreCase))
            return "FinanceTracker rejected the event because of a business-state conflict, such as a closed business day.";
        if (value.StartsWith("HTTP 400", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("HTTP 422", StringComparison.OrdinalIgnoreCase))
            return "FinanceTracker rejected the event because its financial data did not pass validation.";
        return "Synchronization did not complete. Review the protected server logs for technical details.";
    }
}
