using DataAccess.Models;
using DataAccess.Models.FinanceIntegration;
using DataAccess.Services;
using InvEntry.Contracts.FinanceSync;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DataAccess.Tests;

public sealed class FinanceSyncAttentionTests
{
    private static readonly DateTime Now = new(2026, 10, 29, 18, 0, 0);
    private static readonly DateTime NowUtc = Now.AddHours(-5.5);

    [Test]
    public void A_just_queued_event_is_in_progress_and_needs_nothing()
    {
        var assessment = Assess(Row(FinanceSyncService.Pending, createdMinutesAgo: 2));

        Assert.Multiple(() =>
        {
            Assert.That(assessment!.State, Is.EqualTo(FinanceSyncAttentionStates.InProgress));
            Assert.That(assessment.CanSend, Is.False);
        });
    }

    [Test]
    public void A_waiting_event_never_tried_is_stuck_and_can_be_sent()
    {
        var assessment = Assess(Row(FinanceSyncService.Pending, createdMinutesAgo: 3 * 24 * 60));

        Assert.Multiple(() =>
        {
            Assert.That(assessment!.State, Is.EqualTo(FinanceSyncAttentionStates.Stuck));
            Assert.That(assessment.CanSend, Is.True);
            Assert.That(assessment.Reason, Does.Contain("3 day(s)"));
        });
    }

    [Test]
    public void An_event_being_sent_is_in_progress_even_when_old()
    {
        var row = Row(FinanceSyncService.Pending, createdMinutesAgo: 600);
        row.ClaimExpiresOn = NowUtc.AddMinutes(1);

        Assert.That(Assess(row)!.State, Is.EqualTo(FinanceSyncAttentionStates.InProgress));
    }

    [Test]
    public void Missing_finance_setup_needs_the_administrator()
    {
        var row = Row(FinanceSyncService.RetryableError, createdMinutesAgo: 60,
            error: "FinanceTracker payment mode or category mapping is incomplete.");

        var assessment = Assess(row)!;

        Assert.Multiple(() =>
        {
            Assert.That(assessment.State, Is.EqualTo(FinanceSyncAttentionStates.NeedsAdmin));
            Assert.That(assessment.Reason, Does.Contain("setup"));
            Assert.That(assessment.CanSend, Is.True, "Once fixed, the user sends it again.");
        });
    }

    [Test]
    public void A_transient_failure_is_retried_and_can_be_sent()
    {
        var row = Row(FinanceSyncService.RetryableError, createdMinutesAgo: 60,
            error: "Unable to acquire source lock.");

        var assessment = Assess(row)!;

        Assert.Multiple(() =>
        {
            Assert.That(assessment.State, Is.EqualTo(FinanceSyncAttentionStates.Stuck));
            Assert.That(assessment.CanSend, Is.True);
        });
    }

    [Test]
    public void A_business_rejection_needs_correction_and_cannot_be_resent()
    {
        var row = Row(FinanceSyncService.BusinessError, createdMinutesAgo: 60,
            error: "No receipt, refund, credit or old-metal voucher that FinanceTracker can post was found on this invoice.");

        var assessment = Assess(row)!;

        Assert.Multiple(() =>
        {
            Assert.That(assessment.State, Is.EqualTo(FinanceSyncAttentionStates.NeedsAction));
            Assert.That(assessment.CanSend, Is.False);
        });
    }

    [Test]
    public void A_synchronized_event_needs_nothing_unless_held_in_suspense()
    {
        var row = Row(FinanceSyncService.Sent, createdMinutesAgo: 60);

        Assert.Multiple(() =>
        {
            Assert.That(Assess(row), Is.Null);
            Assert.That(Assess(row, inSuspense: true)!.State,
                Is.EqualTo(FinanceSyncAttentionStates.InSuspense));
            Assert.That(Assess(row, inSuspense: true)!.NextStep, Does.Contain("25-10-2026"));
        });
    }

    [Test]
    public async Task One_event_whose_result_cannot_be_saved_does_not_block_the_rest()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        await using var context = new AdvanceAccountServiceTests.SqliteMijmsContext(
            new DbContextOptionsBuilder<MijmsContext>().UseSqlite(connection).Options);
        context.Database.EnsureCreated();
        // As on production before the fix: the outbox refused the error statuses.
        context.Database.ExecuteSqlRaw("""
            CREATE TRIGGER OUTBOX_STATUS_CHECK BEFORE UPDATE ON FINANCE_SYNC_OUTBOX
            WHEN NEW.STATUS NOT IN ('PENDING', 'PROCESSING', 'SENT', 'ERROR')
            BEGIN SELECT RAISE(ABORT, 'CHECK constraint CK_FINANCE_SYNC_OUTBOX_STATUS'); END;
            """);

        context.FinanceSyncOutboxes.AddRange(
            Outbox("COLLECTION-FAILS", Now.AddDays(-4)),
            Outbox("INVOICE-POSTS", Now.AddDays(-3)));
        await context.SaveChangesAsync();

        var poster = new ByEventPoster("COLLECTION-FAILS");
        var service = new FinanceSyncService(context, new FinanceInvoicePayloadBuilder(context),
            poster, new NotPostedStatusReader());

        var sent = await service.SendPendingAsync();

        context.ChangeTracker.Clear();
        var failed = await context.FinanceSyncOutboxes.SingleAsync(x => x.SourceEventId == "COLLECTION-FAILS");
        var posted = await context.FinanceSyncOutboxes.SingleAsync(x => x.SourceEventId == "INVOICE-POSTS");

        Assert.Multiple(() =>
        {
            Assert.That(sent, Is.EqualTo(1));
            Assert.That(posted.Status, Is.EqualTo(FinanceSyncService.Sent));
            Assert.That(failed.Status, Is.EqualTo(FinanceSyncService.Pending));
            Assert.That(failed.RetryCount, Is.EqualTo(1));
            Assert.That(failed.LastAttemptOn, Is.Not.Null);
            Assert.That(failed.LastError, Does.Contain("could not be saved"));
            Assert.That(failed.ClaimToken, Is.Null);
            Assert.That(failed.ClaimExpiresOn, Is.Null);
        });

        // The status screen then tells the user it needs the administrator.
        Assert.That(FinanceSyncAttention.Assess(failed, DateTime.Now, DateTime.UtcNow, false)!.State,
            Is.EqualTo(FinanceSyncAttentionStates.NeedsAdmin));
    }

    [Test]
    public async Task The_automatic_sync_waits_before_retrying_a_failure_but_a_manual_send_does_not()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        await using var context = new AdvanceAccountServiceTests.SqliteMijmsContext(
            new DbContextOptionsBuilder<MijmsContext>().UseSqlite(connection).Options);
        context.Database.EnsureCreated();

        var row = Outbox("JUST-FAILED", DateTime.Now.AddHours(-1));
        row.Status = FinanceSyncService.RetryableError;
        row.LastAttemptOn = DateTime.Now.AddMinutes(-1);
        context.FinanceSyncOutboxes.Add(row);
        await context.SaveChangesAsync();

        var poster = new ByEventPoster();
        var service = new FinanceSyncService(context, new FinanceInvoicePayloadBuilder(context),
            poster, new NotPostedStatusReader());

        var automatic = await service.SendPendingAsync();
        var manual = await service.DispatchSelectedAsync([row.Gkey], null);

        Assert.Multiple(() =>
        {
            Assert.That(automatic, Is.EqualTo(0));
            Assert.That(manual.Sent, Is.EqualTo(1));
        });
    }

    private static FinanceSyncAttention.Assessment? Assess(FinanceSyncOutbox row, bool inSuspense = false) =>
        FinanceSyncAttention.Assess(row, Now, NowUtc, inSuspense);

    private static FinanceSyncOutbox Row(string status, int createdMinutesAgo, string? error = null) => new()
    {
        EventType = "FINANCIAL_DOCUMENT",
        SourceType = "INVOICE",
        SourceEventId = "INVOICE-1-FINAL",
        DocumentNo = "D-00001",
        DocumentDate = new DateTime(2026, 10, 25),
        Payload = "{}",
        Status = status,
        CreatedOn = Now.AddMinutes(-createdMinutesAgo),
        LastError = error
    };

    private static FinanceSyncOutbox Outbox(string sourceEventId, DateTime createdOn) => new()
    {
        EventType = "FINANCIAL_DOCUMENT",
        SourceType = "INVOICE",
        SourceGkey = 1,
        SourceEventId = sourceEventId,
        DocumentNo = sourceEventId,
        DocumentDate = createdOn.Date,
        Payload = $"{{\"orgGkey\":1,\"sourceSystem\":\"INVENTRY\",\"sourceEventId\":\"{sourceEventId}\",\"movements\":[{{\"direction\":\"IN\",\"paymentGroup\":\"CASH\",\"amount\":1}}]}}",
        Status = FinanceSyncService.Pending,
        CreatedOn = createdOn
    };

    /// <summary>Posts every event except the ones named, which FinanceTracker cannot take.</summary>
    private sealed class ByEventPoster(params string[] failing) : IFinanceDocumentPoster
    {
        public Task<FinancePostingResult> PostAsync(FinanceDocumentPayload payload,
            CancellationToken cancellationToken) =>
            Task.FromResult(failing.Contains(payload.SourceEventId)
                ? new FinancePostingResult(FinancePostingOutcome.Unavailable,
                    "FinanceTracker payment mode or category mapping is incomplete.")
                : new FinancePostingResult(FinancePostingOutcome.Posted, null));
    }

    private sealed class NotPostedStatusReader : IFinanceDocumentStatusReader
    {
        public Task<FinanceRemoteEventState> GetStatusAsync(int orgGkey, string sourceSystem,
            string sourceEventId, CancellationToken cancellationToken) =>
            Task.FromResult(FinanceRemoteEventState.NotPosted);
    }
}
