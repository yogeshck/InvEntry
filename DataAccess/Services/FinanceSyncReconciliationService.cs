using DataAccess.Models;
using InvEntry.Contracts.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;
using System.Text.Json;

namespace DataAccess.Services;

public sealed record FinanceSyncDryRunRequest(
    DateOnly FromDate,
    DateOnly ToDate);

public sealed record FinanceSyncRecoveryRequest(
    int InvoiceGkey,
    bool Approved);

public sealed record FinanceSyncReconciliationItem(
    int InvoiceGkey,
    string InvoiceNumber,
    DateOnly DocumentDate,
    decimal Amount,
    string ExpectedEventId,
    string LocalOutboxState,
    string RemotePostingState,
    string RecommendedAction,
    int MovementCount,
    decimal MovementAmount);

public sealed record FinanceSyncReconciliationReport(
    bool DryRun,
    int OrgGkey,
    int LocationGkey,
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<FinanceSyncReconciliationItem> Items);

public sealed record FinanceSyncRecoveryResult(
    int InvoiceGkey,
    string SourceEventId,
    string Result,
    string OutboxStatus);

public sealed class FinanceSyncReconciliationService
{
    private readonly MijmsContext _context;
    private readonly FinanceInvoicePayloadBuilder _payloadBuilder;
    private readonly FinanceSyncService _syncService;
    private readonly FinanceSyncOptions _options;

    public FinanceSyncReconciliationService(
        MijmsContext context,
        FinanceInvoicePayloadBuilder payloadBuilder,
        FinanceSyncService syncService,
        IOptions<FinanceSyncOptions> options)
    {
        _context = context;
        _payloadBuilder = payloadBuilder;
        _syncService = syncService;
        _options = options.Value;
    }

    public async Task<FinanceSyncReconciliationReport> DryRunAsync(
        FinanceSyncDryRunRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FromDate > request.ToDate)
            throw new ArgumentException("FromDate must not be after ToDate.");
        if ((request.ToDate.DayNumber - request.FromDate.DayNumber) > 366)
            throw new ArgumentException("The reconciliation range cannot exceed 366 days.");

        var from = request.FromDate.ToDateTime(TimeOnly.MinValue);
        var through = request.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var invoices = await _context.InvoiceHeaders.AsNoTracking()
            .Where(x => x.InvDate >= from && x.InvDate < through &&
                x.Status == InvoiceStatus.Finalised)
            .OrderBy(x => x.InvDate).ThenBy(x => x.Gkey)
            .Select(x => new
            {
                x.Gkey, x.InvNbr, x.InvDate, x.AmountPayable
            })
            .ToListAsync(cancellationToken);

        var eventIds = invoices.Select(x => EventId(x.Gkey)).ToList();
        var localStates = await _context.FinanceSyncOutboxes.AsNoTracking()
            .Where(x => eventIds.Contains(x.SourceEventId))
            .ToDictionaryAsync(x => x.SourceEventId, x => x.Status,
                cancellationToken);
        var items = new List<FinanceSyncReconciliationItem>();

        foreach (var invoice in invoices)
        {
            var eventId = EventId(invoice.Gkey);
            if (localStates.TryGetValue(eventId, out var localStatus))
            {
                items.Add(Item(invoice.Gkey, invoice.InvNbr, invoice.InvDate,
                    invoice.AmountPayable, eventId, localStatus, "NOT_CHECKED",
                    "NO_CHANGE_EXISTING_EVENT", 0, 0));
                continue;
            }

            try
            {
                var payload = await _payloadBuilder.BuildAsync(invoice.Gkey,
                    _options.OrgGkey, _options.LocationGkey, cancellationToken);
                var movementAmount = payload.Movements.Sum(x => x.Amount);
                if (payload.Movements.Count == 0)
                {
                    items.Add(Item(invoice.Gkey, invoice.InvNbr, invoice.InvDate,
                        invoice.AmountPayable, eventId, "MISSING", "NOT_APPLICABLE",
                        "NO_EVENT_REQUIRED", 0, 0));
                    continue;
                }

                var remote = await _syncService.GetRemoteEventStateAsync(
                    _options.OrgGkey, eventId, cancellationToken);
                items.Add(Item(invoice.Gkey, invoice.InvNbr, invoice.InvDate,
                    invoice.AmountPayable, eventId, "MISSING", remote.ToString().ToUpperInvariant(),
                    remote switch
                    {
                        FinanceRemoteEventState.Posted => "APPROVE_RESTORE_SENT",
                        FinanceRemoteEventState.NotPosted => "ELIGIBLE_FOR_APPROVED_RECOVERY",
                        _ => "REVIEW_UNCERTAIN"
                    }, payload.Movements.Count, movementAmount));
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                items.Add(Item(invoice.Gkey, invoice.InvNbr, invoice.InvDate,
                    invoice.AmountPayable, eventId, "MISSING", "NOT_CHECKED",
                    "PAYLOAD_VALIDATION_REQUIRED", 0, 0));
            }
        }

        return new FinanceSyncReconciliationReport(true, _options.OrgGkey,
            _options.LocationGkey, request.FromDate, request.ToDate, items);
    }

    public async Task<FinanceSyncRecoveryResult> RecoverAsync(
        FinanceSyncRecoveryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.Approved)
            throw new ArgumentException(
                "Explicit administrative approval is required for recovery.");

        var eventId = EventId(request.InvoiceGkey);
        var remote = await _syncService.GetRemoteEventStateAsync(
            _options.OrgGkey, eventId, cancellationToken);
        if (remote == FinanceRemoteEventState.Unavailable)
            return new(request.InvoiceGkey, eventId,
                "REMOTE_STATUS_UNCERTAIN_NO_CHANGE", FinanceSyncService.Uncertain);

        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var existing = await _context.FinanceSyncOutboxes
                .SingleOrDefaultAsync(x => x.SourceEventId == eventId,
                    cancellationToken);
            if (existing != null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(request.InvoiceGkey, eventId,
                    "ALREADY_EXISTS_NO_CHANGE", existing.Status);
            }

            var payload = await _payloadBuilder.BuildAsync(request.InvoiceGkey,
                _options.OrgGkey, _options.LocationGkey, cancellationToken);
            if (payload.Movements.Count == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(request.InvoiceGkey, eventId,
                    "NO_EVENT_REQUIRED", "NONE");
            }

            var status = remote == FinanceRemoteEventState.Posted
                ? FinanceSyncService.Sent
                : FinanceSyncService.Pending;
            _context.FinanceSyncOutboxes.Add(new FinanceSyncOutbox
            {
                EventType = "FINANCIAL_DOCUMENT",
                SourceType = "INVOICE",
                SourceGkey = request.InvoiceGkey,
                SourceEventId = eventId,
                DocumentNo = payload.DocumentNo,
                DocumentDate = payload.DocumentDate.ToDateTime(TimeOnly.MinValue),
                Payload = JsonSerializer.Serialize(payload,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }),
                Status = status,
                RetryCount = 0,
                CreatedOn = DateTime.Now,
                SentOn = status == FinanceSyncService.Sent ? DateTime.Now : null,
                LastError = status == FinanceSyncService.Sent
                    ? "Local outbox state restored after authenticated remote status verification."
                    : null
            });
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(request.InvoiceGkey, eventId,
                status == FinanceSyncService.Sent
                    ? "RESTORED_SENT_WITHOUT_POST"
                    : "CREATED_PENDING_NO_POST",
                status);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static string EventId(int invoiceGkey) =>
        $"INVOICE-{invoiceGkey}-FINAL";

    private static FinanceSyncReconciliationItem Item(
        int gkey, string? number, DateTime? date, decimal? amount,
        string eventId, string local, string remote, string action,
        int movementCount, decimal movementAmount) =>
        new(gkey, number ?? string.Empty,
            DateOnly.FromDateTime(date ?? DateTime.MinValue), amount ?? 0,
            eventId, local, remote, action, movementCount, movementAmount);
}
