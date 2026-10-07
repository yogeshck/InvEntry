using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace DataAccess.Services;

public sealed class FinanceSyncService
{
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

    public async Task QueueFinanceSyncAsync(
        int invoiceGkey,
        int orgGkey,
        int locationGkey,
        CancellationToken cancellationToken = default)
    {
        var sourceEventId = $"INVOICE-{invoiceGkey}-FINAL";

        // Safe if QueueFinanceSyncAsync is accidentally called twice.
        var alreadyQueued = await _context.FinanceSyncOutboxes
            .AnyAsync(
                x => x.SourceEventId == sourceEventId,
                cancellationToken);

        if (alreadyQueued)
            return;

        var payload = await _payloadBuilder.BuildAsync(
            invoiceGkey,
            orgGkey,
            locationGkey,
            cancellationToken);

        var json = JsonSerializer.Serialize(
            payload,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

        var outbox = new FinanceSyncOutbox
        {
            EventType = "FINANCIAL_DOCUMENT",
            SourceType = "INVOICE",

            SourceGkey = invoiceGkey,
            SourceEventId = sourceEventId,

            DocumentNo = payload.DocumentNo,
            DocumentDate =
                payload.DocumentDate.ToDateTime(TimeOnly.MinValue),

            Payload = json,

            Status = "PENDING",
            RetryCount = 0,
            CreatedOn = DateTime.Now
        };

        _context.FinanceSyncOutboxes.Add(outbox);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> SendPendingAsync(
        CancellationToken cancellationToken = default)
    {
        var pendingItems = await _context.FinanceSyncOutboxes
            .Where(x =>
                x.Status == "PENDING" ||
                x.Status == "ERROR")
            .OrderBy(x => x.CreatedOn)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (pendingItems.Count == 0)
            return 0;

        var client =
            _httpClientFactory.CreateClient("FinanceTracker");

        var sentCount = 0;

        foreach (var item in pendingItems)
        {
            try
            {
                item.LastAttemptOn = DateTime.Now;
                item.RetryCount++;

                using var content = new StringContent(
                    item.Payload,
                    Encoding.UTF8,
                    "application/json");

                using var response = await client.PostAsync(
                    "/api/integrations/financial-document",
                    content,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    item.Status = "SENT";
                    item.SentOn = DateTime.Now;
                    item.LastError = null;

                    sentCount++;
                }
                else
                {
                    var responseBody =
                        await response.Content.ReadAsStringAsync(
                            cancellationToken);

                    item.Status = "ERROR";

                    item.LastError =
                        $"HTTP {(int)response.StatusCode}: " +
                        Truncate(responseBody, 800);
                }
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                item.Status = "ERROR";
                item.LastError =
                    "Finance Tracker request timed out.";
            }
            catch (HttpRequestException ex)
            {
                item.Status = "ERROR";
                item.LastError =
                    Truncate(ex.Message, 800);
            }
            catch (Exception ex)
            {
                item.Status = "ERROR";
                item.LastError =
                    Truncate(ex.Message, 800);
            }

            await _context.SaveChangesAsync(
                cancellationToken);
        }

        return sentCount;
    }

    private static string Truncate(
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value.Trim();

        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }
}