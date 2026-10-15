using DataAccess.Models;
using DataAccess.Models.FinanceIntegration;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DataAccess.Services;

public sealed class FinanceSyncService
{
    public const string Pending = "PENDING";
    public const string Sent = "SENT";
    public const string Uncertain = "UNCERTAIN";
    public const string RetryableError = "RETRYABLE_ERROR";
    public const string BusinessError = "BUSINESS_ERROR";

    private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(2);
    private readonly MijmsContext _context;
    private readonly FinanceInvoicePayloadBuilder _payloadBuilder;
    private readonly IFinanceDocumentPoster _poster;
    private readonly IFinanceDocumentStatusReader _statusReader;

    public FinanceSyncService(
        MijmsContext context,
        FinanceInvoicePayloadBuilder payloadBuilder,
        IFinanceDocumentPoster poster,
        IFinanceDocumentStatusReader statusReader)
    {
        _context = context;
        _payloadBuilder = payloadBuilder;
        _poster = poster;
        _statusReader = statusReader;
    }

    public async Task<FinanceQueueResult> QueueFinanceSyncAsync(
        int invoiceGkey,
        int orgGkey,
        int locationGkey,
        CancellationToken cancellationToken = default)
    {
        var sourceEventId = $"INVOICE-{invoiceGkey}-FINAL";
        var alreadyQueued = await _context.FinanceSyncOutboxes.AnyAsync(
            x => x.SourceEventId == sourceEventId, cancellationToken);
        if (alreadyQueued) return FinanceQueueResult.AlreadyQueued;

        var payload = await _payloadBuilder.BuildAsync(
            invoiceGkey, orgGkey, locationGkey, cancellationToken);
        // Only settlement voucher types explicitly mapped by the payload
        // builder create a FinanceTracker event. Credit-only and unsupported
        // non-cash settlements must not be converted into invented movements.
        if (payload.Movements.Count == 0)
            return FinanceQueueResult.NotRequired;
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _context.FinanceSyncOutboxes.Add(new FinanceSyncOutbox
        {
            EventType = "FINANCIAL_DOCUMENT",
            SourceType = "INVOICE",
            SourceGkey = invoiceGkey,
            SourceEventId = sourceEventId,
            DocumentNo = payload.DocumentNo,
            DocumentDate = payload.DocumentDate.ToDateTime(TimeOnly.MinValue),
            Payload = json,
            Status = Pending,
            RetryCount = 0,
            CreatedOn = DateTime.Now
        });
        await _context.SaveChangesAsync(cancellationToken);
        return FinanceQueueResult.Queued;
    }

    public async Task<FinanceRemoteEventState> GetRemoteEventStateAsync(
        int orgGkey,
        string sourceEventId,
        CancellationToken cancellationToken = default) =>
        await _statusReader.GetStatusAsync(
            orgGkey, "INVENTRY", sourceEventId, cancellationToken);

    public async Task<int> SendPendingAsync(
        CancellationToken cancellationToken = default)
    {
        var claimedItems = await ClaimBatchAsync(cancellationToken);
        return await DispatchBatchAsync(claimedItems, null, cancellationToken);
    }

    public async Task<FinanceDispatchBatchResult> DispatchSelectedAsync(
        IReadOnlyCollection<long> ids,
        Func<FinanceDispatchProgress, Task>? progress,
        CancellationToken cancellationToken = default)
    {
        var claimedItems = await ClaimIdsAsync(ids, cancellationToken);
        var sent = await DispatchBatchAsync(claimedItems, progress, cancellationToken);
        return new(ids.Count, claimedItems.Count, sent,
            claimedItems.Count - sent);
    }

    private async Task<int> DispatchBatchAsync(
        IReadOnlyCollection<FinanceSyncOutbox> claimedItems,
        Func<FinanceDispatchProgress, Task>? progress,
        CancellationToken cancellationToken)
    {
        if (claimedItems.Count == 0) return 0;

        var sentCount = 0;
        var completed = 0;

        foreach (var item in claimedItems)
        {
            try
            {
                if (await DispatchClaimedAsync(item, cancellationToken))
                    sentCount++;
            }
            finally
            {
                item.ClaimToken = null;
                item.ClaimExpiresOn = null;
                await _context.SaveChangesAsync(cancellationToken);
                completed++;
                if (progress != null)
                    await progress(new FinanceDispatchProgress(completed,
                        sentCount, claimedItems.Count - completed,
                        item.Gkey, item.Status));
            }
        }

        return sentCount;
    }

    private async Task<List<FinanceSyncOutbox>> ClaimIdsAsync(
        IReadOnlyCollection<long> requestedIds,
        CancellationToken cancellationToken)
    {
        var ids = requestedIds.Distinct().Take(100).ToList();
        var now = DateTime.UtcNow;
        var claimToken = Guid.NewGuid().ToString("N");
        foreach (var gkey in ids)
        {
            await _context.FinanceSyncOutboxes
                .Where(x => x.Gkey == gkey &&
                    (x.Status == Pending || x.Status == RetryableError || x.Status == Uncertain) &&
                    (x.ClaimExpiresOn == null || x.ClaimExpiresOn < now))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.ClaimToken, claimToken)
                    .SetProperty(x => x.ClaimExpiresOn, now.Add(ClaimLease)),
                    cancellationToken);
        }
        return await _context.FinanceSyncOutboxes
            .Where(x => x.ClaimToken == claimToken)
            .OrderBy(x => x.CreatedOn).ToListAsync(cancellationToken);
    }

    private async Task<List<FinanceSyncOutbox>> ClaimBatchAsync(
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var claimToken = Guid.NewGuid().ToString("N");
        var candidateIds = await _context.FinanceSyncOutboxes
            .AsNoTracking()
            .Where(x =>
                (x.Status == Pending || x.Status == RetryableError ||
                 x.Status == Uncertain) &&
                (x.ClaimExpiresOn == null || x.ClaimExpiresOn < now))
            .OrderBy(x => x.CreatedOn)
            .Select(x => x.Gkey)
            // Ten rows per cycle keeps each claim lease comfortably inside
            // its two-minute window even in the worst case.
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var gkey in candidateIds)
        {
            await _context.FinanceSyncOutboxes
                .Where(x => x.Gkey == gkey &&
                    (x.Status == Pending || x.Status == RetryableError ||
                     x.Status == Uncertain) &&
                    (x.ClaimExpiresOn == null || x.ClaimExpiresOn < now))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.ClaimToken, claimToken)
                    .SetProperty(x => x.ClaimExpiresOn, now.Add(ClaimLease)),
                    cancellationToken);
        }

        return await _context.FinanceSyncOutboxes
            .Where(x => x.ClaimToken == claimToken)
            .OrderBy(x => x.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    internal async Task<bool> DispatchClaimedAsync(
        FinanceSyncOutbox item,
        CancellationToken cancellationToken)
    {
        var wasUncertain = item.Status == Uncertain;
        item.LastAttemptOn = DateTime.Now;
        item.RetryCount++;

        try
        {
            var storedPayload = JsonSerializer.Deserialize<FinanceDocumentPayload>(
                item.Payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (storedPayload?.Movements == null || storedPayload.Movements.Count == 0)
            {
                item.Status = BusinessError;
                item.LastError = "The stored financial event contains no eligible financial movements.";
                return false;
            }
            if (item.Status == Uncertain)
            {
                var reconciliation = await ReconcileAsync(
                    storedPayload, item, cancellationToken);
                if (reconciliation == ReconciliationResult.Posted)
                {
                    MarkSent(item);
                    return true;
                }
                if (reconciliation == ReconciliationResult.Unavailable)
                    return false;
            }

            var result = await _poster.PostAsync(storedPayload, cancellationToken);
            switch (result.Outcome)
            {
                case FinancePostingOutcome.Posted:
                    MarkSent(item);
                    return true;
                case FinancePostingOutcome.BusinessRejected:
                    item.Status = BusinessError;
                    item.LastError = Truncate(result.ErrorMessage, 800);
                    return false;
                default:
                    item.Status = RetryableError;
                    item.LastError = Truncate(result.ErrorMessage, 800);
                    return false;
            }
        }
        catch (Exception ex)
        {
            item.Status = wasUncertain ? Uncertain : RetryableError;
            item.LastError = Truncate(ex.Message, 800);
            return false;
        }
    }

    private async Task<ReconciliationResult> ReconcileAsync(
        FinanceDocumentPayload payload,
        FinanceSyncOutbox item,
        CancellationToken cancellationToken)
    {
        var state = await _statusReader.GetStatusAsync(payload.OrgGkey,
            payload.SourceSystem, payload.SourceEventId, cancellationToken);
        return state switch
        {
            FinanceRemoteEventState.Posted => ReconciliationResult.Posted,
            FinanceRemoteEventState.NotPosted => ReconciliationResult.NotPosted,
            _ => ReconciliationResult.Unavailable
        };
    }

    private static void MarkSent(FinanceSyncOutbox item)
    {
        item.Status = Sent;
        item.SentOn = DateTime.Now;
        item.LastError = null;
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        value = value.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private enum ReconciliationResult
    {
        Posted,
        NotPosted,
        Unavailable
    }
}

public enum FinanceQueueResult
{
    Queued,
    AlreadyQueued,
    NotRequired
}

public enum FinanceRemoteEventState
{
    Posted,
    NotPosted,
    Unavailable
}

public sealed record FinanceDispatchProgress(int Completed, int Sent,
    int Remaining, long EventId, string EventStatus);
public sealed record FinanceDispatchBatchResult(int Requested, int Claimed,
    int Sent, int NotSent);
