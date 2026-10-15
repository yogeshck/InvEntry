using DataAccess.Models;
using DataAccess.Models.FinanceIntegration;
using DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DataAccess.Tests;

public sealed class FinanceSyncServiceTests
{
    [Test]
    public async Task Unavailable_posting_outcome_is_retryable()
    {
        await using var context = CreateContext();
        var poster = new FakePoster(FinancePostingOutcome.Unavailable,
            "FinanceTracker posting is temporarily unavailable.");
        var service = CreateService(context, poster);
        var item = CreateItem(FinanceSyncService.Pending);

        var sent = await service.DispatchClaimedAsync(item, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(sent, Is.False);
            Assert.That(item.Status, Is.EqualTo(FinanceSyncService.RetryableError));
            Assert.That(item.LastError, Does.Contain("temporarily unavailable"));
        });
    }

    [Test]
    public async Task Uncertain_event_already_posted_is_reconciled_without_posting_again()
    {
        await using var context = CreateContext();
        var poster = new FakePoster(FinancePostingOutcome.Posted, null);
        var statusReader = new FakeStatusReader(FinanceRemoteEventState.Posted);
        var service = CreateService(context, poster, statusReader);
        var item = CreateItem(FinanceSyncService.Uncertain);

        var sent = await service.DispatchClaimedAsync(item, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(sent, Is.True);
            Assert.That(item.Status, Is.EqualTo(FinanceSyncService.Sent));
            Assert.That(poster.CallCount, Is.EqualTo(0),
                "An event already confirmed posted must not be posted again.");
        });
    }

    [Test]
    public async Task Business_rejection_is_not_retried()
    {
        await using var context = CreateContext();
        var poster = new FakePoster(FinancePostingOutcome.BusinessRejected,
            "The business date is closed.");
        var service = CreateService(context, poster);
        var item = CreateItem(FinanceSyncService.Pending);

        await service.DispatchClaimedAsync(item, CancellationToken.None);

        Assert.That(item.Status, Is.EqualTo(FinanceSyncService.BusinessError));
    }

    private static MijmsContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MijmsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MijmsContext(options);
    }

    private static FinanceSyncService CreateService(MijmsContext context,
        IFinanceDocumentPoster? poster = null,
        IFinanceDocumentStatusReader? statusReader = null) =>
        new(context, new FinanceInvoicePayloadBuilder(context),
            poster ?? new FakePoster(FinancePostingOutcome.Posted, null),
            statusReader ?? new FakeStatusReader(FinanceRemoteEventState.NotPosted));

    private static FinanceSyncOutbox CreateItem(string status) => new()
    {
        EventType = "FINANCIAL_DOCUMENT",
        SourceType = "INVOICE",
        SourceEventId = "INVOICE-7-FINAL",
        Payload = "{\"orgGkey\":1,\"sourceSystem\":\"INVENTRY\",\"sourceEventId\":\"INVOICE-7-FINAL\",\"movements\":[{\"direction\":\"IN\",\"paymentGroup\":\"CASH\",\"amount\":1}]}",
        Status = status,
        CreatedOn = DateTime.Now
    };

    private sealed class FakePoster(
        FinancePostingOutcome outcome, string? error) : IFinanceDocumentPoster
    {
        public int CallCount { get; private set; }

        public Task<FinancePostingResult> PostAsync(
            FinanceDocumentPayload payload, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new FinancePostingResult(outcome, error));
        }
    }

    private sealed class FakeStatusReader(FinanceRemoteEventState state)
        : IFinanceDocumentStatusReader
    {
        public Task<FinanceRemoteEventState> GetStatusAsync(int orgGkey,
            string sourceSystem, string sourceEventId,
            CancellationToken cancellationToken) => Task.FromResult(state);
    }
}
