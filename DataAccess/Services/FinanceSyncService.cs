using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
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
    private readonly IHttpClientFactory _httpClientFactory;

    public FinanceSyncService(
        MijmsContext context,
        FinanceInvoicePayloadBuilder payloadBuilder,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _payloadBuilder = payloadBuilder;
        _httpClientFactory = httpClientFactory;
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
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("FinanceTracker");
        var uri = "/api/integrations/financial-document/status" +
                  $"?orgGkey={orgGkey}" +
                  "&sourceSystem=INVENTRY" +
                  $"&sourceEventId={Uri.EscapeDataString(sourceEventId)}";
        try
        {
            using var response = await client.GetAsync(uri, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return FinanceRemoteEventState.NotPosted;
            if (!response.IsSuccessStatusCode)
                return FinanceRemoteEventState.Unavailable;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var status = JsonSerializer.Deserialize<FinanceEventStatus>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return status?.Posted == true
                ? FinanceRemoteEventState.Posted
                : FinanceRemoteEventState.NotPosted;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return FinanceRemoteEventState.Unavailable;
        }
        catch (HttpRequestException)
        {
            return FinanceRemoteEventState.Unavailable;
        }
    }

    public async Task<int> SendPendingAsync(
        CancellationToken cancellationToken = default)
    {
        var claimedItems = await ClaimBatchAsync(cancellationToken);
        if (claimedItems.Count == 0) return 0;

        var client = _httpClientFactory.CreateClient("FinanceTracker");
        var sentCount = 0;

        foreach (var item in claimedItems)
        {
            try
            {
                if (await DispatchClaimedAsync(item, client, cancellationToken))
                    sentCount++;
            }
            finally
            {
                item.ClaimToken = null;
                item.ClaimExpiresOn = null;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return sentCount;
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
            // Ten requests fit inside the two-minute lease even when every
            // FinanceTracker call reaches the configured ten-second timeout.
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
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var wasUncertain = item.Status == Uncertain;
        item.LastAttemptOn = DateTime.Now;
        item.RetryCount++;

        try
        {
            if (item.Status == Uncertain)
            {
                var reconciliation = await ReconcileAsync(
                    item, client, cancellationToken);
                if (reconciliation == ReconciliationResult.Posted)
                {
                    MarkSent(item);
                    return true;
                }
                if (reconciliation == ReconciliationResult.Unavailable)
                    return false;
            }

            using var content = new StringContent(
                item.Payload, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(
                "/api/integrations/financial-document", content,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                MarkSent(item);
                return true;
            }

            var responseBody = await response.Content.ReadAsStringAsync(
                cancellationToken);
            item.Status = ClassifyHttpFailure(response.StatusCode);
            item.LastError = $"HTTP {(int)response.StatusCode}: " +
                             Truncate(responseBody, 800);
            return false;
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            item.Status = Uncertain;
            item.LastError = "FinanceTracker request timed out; posting outcome is uncertain.";
            return false;
        }
        catch (HttpRequestException ex)
        {
            item.Status = Uncertain;
            item.LastError = "FinanceTracker response was not received; posting outcome is uncertain. " +
                             Truncate(ex.Message, 700);
            return false;
        }
        catch (Exception ex)
        {
            item.Status = wasUncertain ? Uncertain : RetryableError;
            item.LastError = Truncate(ex.Message, 800);
            return false;
        }
    }

    private async Task<ReconciliationResult> ReconcileAsync(
        FinanceSyncOutbox item,
        HttpClient client,
        CancellationToken cancellationToken)
    {
        FinanceEventIdentity identity;
        try
        {
            identity = JsonSerializer.Deserialize<FinanceEventIdentity>(
                item.Payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new JsonException("The outbox payload is empty.");
        }
        catch (JsonException ex)
        {
            item.Status = BusinessError;
            item.LastError = "The stored finance payload is invalid: " +
                             Truncate(ex.Message, 700);
            return ReconciliationResult.Unavailable;
        }

        var uri = "/api/integrations/financial-document/status" +
                  $"?orgGkey={identity.OrgGkey}" +
                  $"&sourceSystem={Uri.EscapeDataString(identity.SourceSystem)}" +
                  $"&sourceEventId={Uri.EscapeDataString(identity.SourceEventId)}";
        using var response = await client.GetAsync(uri, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return ReconciliationResult.NotPosted;

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            item.LastError = "Unable to verify uncertain posting outcome. " +
                             $"HTTP {(int)response.StatusCode}: " +
                             Truncate(body, 700);
            return ReconciliationResult.Unavailable;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var status = JsonSerializer.Deserialize<FinanceEventStatus>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return status?.Posted == true
            ? ReconciliationResult.Posted
            : ReconciliationResult.NotPosted;
    }

    internal static string ClassifyHttpFailure(HttpStatusCode statusCode)
    {
        if (statusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict ||
            (int)statusCode == 422)
            return BusinessError;
        if (statusCode == HttpStatusCode.RequestTimeout)
            return Uncertain;
        return RetryableError;
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

    private sealed class FinanceEventIdentity
    {
        public int OrgGkey { get; set; }
        public string SourceSystem { get; set; } = string.Empty;
        public string SourceEventId { get; set; } = string.Empty;
    }

    private sealed class FinanceEventStatus
    {
        public bool Posted { get; set; }
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
