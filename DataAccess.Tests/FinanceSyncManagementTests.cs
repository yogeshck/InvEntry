using DataAccess.Models;
using DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace DataAccess.Tests;

public sealed class FinanceSyncManagementTests
{
    [Test]
    public async Task Listing_filters_paginates_and_does_not_expose_payload()
    {
        await using var db = Context();
        db.FinanceSyncOutboxes.AddRange(
            Row(1, "D-1", FinanceSyncService.Sent, new DateTime(2026, 10, 10), 100),
            Row(2, "D-2", FinanceSyncService.Pending, new DateTime(2026, 10, 11), 200),
            Row(3, "D-3", FinanceSyncService.Pending, new DateTime(2026, 10, 12), 300));
        await db.SaveChangesAsync();
        var service = Service(db);

        var result = await service.ListAsync(new(new DateOnly(2026, 10, 11),
            new DateOnly(2026, 10, 12), FinanceSyncService.Pending, "D-", 1, 1), default);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalCount, Is.EqualTo(2));
            Assert.That(result.Items, Has.Count.EqualTo(1));
            Assert.That(result.Items[0].Amount, Is.EqualTo(300));
            Assert.That(result.Items[0].ManualDispatchEligible, Is.True);
            Assert.That(typeof(FinanceSyncEventDto).GetProperty("Payload"), Is.Null);
        });
    }

    [Test]
    public async Task Business_and_legacy_errors_are_not_manual_dispatch_eligible()
    {
        await using var db = Context();
        db.FinanceSyncOutboxes.AddRange(Row(1, "D-1", FinanceSyncService.BusinessError,
            DateTime.Today, 1), Row(2, "D-2", "ERROR", DateTime.Today, 2));
        await db.SaveChangesAsync();
        var result = await Service(db).ListAsync(new(null, null, null, null, 1, 25), default);
        Assert.That(result.Items.All(x => !x.ManualDispatchEligible), Is.True);
    }

    [Test]
    public async Task Summary_provides_four_card_totals_and_attention_breakdown()
    {
        await using var db = Context();
        var statuses = new[] { FinanceSyncService.Sent, FinanceSyncService.Pending,
            FinanceSyncService.RetryableError, FinanceSyncService.BusinessError,
            FinanceSyncService.Uncertain, "ERROR" };
        for (var i = 0; i < statuses.Length; i++)
            db.FinanceSyncOutboxes.Add(Row(i + 1, $"D-{i}", statuses[i], DateTime.Today, i));
        await db.SaveChangesAsync();
        var result = await Service(db).SummaryAsync(null, null, default);
        Assert.Multiple(() =>
        {
            Assert.That(result.Synchronized, Is.EqualTo(1));
            Assert.That(result.Pending, Is.EqualTo(1));
            Assert.That(result.Retryable, Is.EqualTo(1));
            Assert.That(result.NeedsAttention, Is.EqualTo(3));
            Assert.That(result.VerificationRequired, Is.EqualTo(1));
            Assert.That(result.HistoricalError, Is.EqualTo(1));
        });
    }

    [Test]
    public void Duplicate_active_dispatch_request_is_rejected()
    {
        var jobs = new FinanceSyncDispatchJobQueue();
        jobs.Enqueue([7], "user:1");
        Assert.Throws<InvalidOperationException>(() => jobs.Enqueue([7], "user:1"));
    }

    private static FinanceSyncManagementService Service(MijmsContext db) =>
        new(db, Options.Create(new FinanceSyncOptions { Enabled = true,
            OrgGkey = 1, LocationGkey = 1 }));
    private static MijmsContext Context() => new(new DbContextOptionsBuilder<MijmsContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static FinanceSyncOutbox Row(long id, string document, string status,
        DateTime date, decimal amount) => new()
        {
            Gkey = id, EventType = "FINANCIAL_DOCUMENT", SourceType = "INVOICE",
            SourceGkey = (int)id, SourceEventId = $"INVOICE-{id}-FINAL",
            DocumentNo = document, DocumentDate = date,
            Payload = $"{{\"documentAmount\":{amount},\"movements\":[{{\"amount\":{amount}}}]}}",
            Status = status, CreatedOn = date
        };
}
