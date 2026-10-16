using DataAccess.Models;
using DataAccess.Models.FinanceIntegration;
using DataAccess.Services;
using InvEntry.Contracts.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace DataAccess.Tests;

public sealed class FinanceSyncReconciliationTests
{
    [Test]
    public async Task D01274_cash_receipt_maps_to_expected_movement()
    {
        await using var context = CreateContext();
        SeedD01274(context);
        await context.SaveChangesAsync();

        var payload = await new FinanceInvoicePayloadBuilder(context)
            .BuildAsync(29944, 1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(payload.SourceEventId, Is.EqualTo("INVOICE-29944-FINAL"));
            Assert.That(payload.Movements, Has.Count.EqualTo(1));
            Assert.That(payload.Movements[0].Direction, Is.EqualTo("IN"));
            Assert.That(payload.Movements[0].PaymentGroup, Is.EqualTo("CASH"));
            Assert.That(payload.Movements[0].Amount, Is.EqualTo(50000m));
            Assert.That(payload.Movements[0].SourceReference, Is.EqualTo("20766"));
        });
    }

    [Test]
    public async Task Dry_run_reports_missing_event_without_inserting_it()
    {
        await using var context = CreateContext();
        SeedD01274(context);
        await context.SaveChangesAsync();
        var service = CreateReconciliation(context, FinanceRemoteEventState.NotPosted);

        var report = await service.DryRunAsync(
            new(new DateOnly(2026, 10, 11), new DateOnly(2026, 10, 11)));

        Assert.Multiple(() =>
        {
            Assert.That(report.DryRun, Is.True);
            Assert.That(report.Items, Has.Count.EqualTo(1));
            Assert.That(report.Items[0].ExpectedEventId,
                Is.EqualTo("INVOICE-29944-FINAL"));
            Assert.That(report.Items[0].RecommendedAction,
                Is.EqualTo("ELIGIBLE_FOR_APPROVED_RECOVERY"));
            Assert.That(context.FinanceSyncOutboxes.Count(), Is.Zero);
        });
    }

    [Test]
    public async Task Dry_run_marks_remote_unavailable_as_review_uncertain()
    {
        await using var context = CreateContext();
        SeedD01274(context);
        await context.SaveChangesAsync();
        var service = CreateReconciliation(context, FinanceRemoteEventState.Unavailable);

        var report = await service.DryRunAsync(
            new(new DateOnly(2026, 10, 11), new DateOnly(2026, 10, 11)));

        Assert.That(report.Items.Single().RecommendedAction,
            Is.EqualTo("REVIEW_UNCERTAIN"));
        Assert.That(context.FinanceSyncOutboxes.Count(), Is.Zero);
    }

    [Test]
    public async Task Approved_recovery_of_unposted_event_creates_one_pending_row()
    {
        await using var context = CreateContext();
        SeedD01274(context);
        await context.SaveChangesAsync();
        var service = CreateReconciliation(context, FinanceRemoteEventState.NotPosted);

        var result = await service.RecoverAsync(new(29944, Approved: true));
        var second = await service.RecoverAsync(new(29944, Approved: true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.EqualTo("CREATED_PENDING_NO_POST"));
            Assert.That(second.Result, Is.EqualTo("ALREADY_EXISTS_NO_CHANGE"));
            Assert.That(context.FinanceSyncOutboxes.Count(), Is.EqualTo(1));
            Assert.That(context.FinanceSyncOutboxes.Single().Status,
                Is.EqualTo(FinanceSyncService.Pending));
        });
    }

    [Test]
    public async Task Approved_recovery_of_posted_event_restores_sent_without_post()
    {
        await using var context = CreateContext();
        SeedD01274(context);
        await context.SaveChangesAsync();
        var poster = new FakePoster();
        var service = CreateReconciliation(context, FinanceRemoteEventState.Posted, poster);

        var result = await service.RecoverAsync(new(29944, Approved: true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.EqualTo("RESTORED_SENT_WITHOUT_POST"));
            Assert.That(context.FinanceSyncOutboxes.Single().Status,
                Is.EqualTo(FinanceSyncService.Sent));
            Assert.That(poster.CallCount, Is.EqualTo(0),
                "Restoring a known-posted event must not post it again.");
        });
    }

    [Test]
    public void Recovery_requires_explicit_approval()
    {
        using var context = CreateContext();
        var service = CreateReconciliation(context, FinanceRemoteEventState.NotPosted);
        Assert.ThrowsAsync<ArgumentException>(() =>
            service.RecoverAsync(new(29944, Approved: false)));
    }

    [Test]
    public void Startup_check_reports_missing_schema_objects()
    {
        var services = new ServiceCollection();
        services.AddScoped<IFinanceSyncSchemaReadinessChecker>(_ =>
            new MissingSchemaChecker("column dbo.FINANCE_SYNC_OUTBOX.CLAIM_TOKEN"));
        using var provider = services.BuildServiceProvider();
        var hosted = new FinanceSyncSchemaReadinessService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new FinanceSyncOptions
            {
                Enabled = true, OrgGkey = 1, LocationGkey = 1
            }),
            NullLogger<FinanceSyncSchemaReadinessService>.Instance);

        var error = Assert.ThrowsAsync<InvalidOperationException>(() =>
            hosted.StartAsync(CancellationToken.None));
        Assert.That(error!.Message, Does.Contain("CLAIM_TOKEN"));
    }

    private static FinanceSyncReconciliationService CreateReconciliation(
        MijmsContext context,
        FinanceRemoteEventState remoteState,
        FakePoster? poster = null)
    {
        var builder = new FinanceInvoicePayloadBuilder(context);
        var sync = new FinanceSyncService(context, builder,
            poster ?? new FakePoster(),
            new FakeStatusReader(remoteState));
        return new FinanceSyncReconciliationService(context, builder, sync,
            Options.Create(new FinanceSyncOptions
            {
                Enabled = true, OrgGkey = 1, LocationGkey = 1
            }));
    }

    [Test]
    public async Task Non_cash_and_credit_vouchers_map_to_payload_movements()
    {
        await using var context = CreateContext();
        SeedInvoice(context, 29990, "D-01299", 180000m);
        AddVoucher(context, 1, 29990, "Receipt", "Bank Scanner", 60000m);
        AddVoucher(context, 2, 29990, "Receipt", "GPAY", 20000m);
        AddVoucher(context, 3, 29990, "Receipt", "Credit Card", 30000m);
        AddVoucher(context, 4, 29990, "Journal", "Credit", 70000m);
        await context.SaveChangesAsync();

        var payload = await new FinanceInvoicePayloadBuilder(context)
            .BuildAsync(29990, 1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(payload.Movements, Has.Count.EqualTo(4));
            Assert.That(payload.Movements.Where(x => x.PaymentGroup == "NON_CASH").Sum(x => x.Amount),
                Is.EqualTo(110000m));
            Assert.That(payload.Movements.Single(x => x.PaymentGroup == "CREDIT").Amount,
                Is.EqualTo(70000m));
        });
    }

    [Test]
    public async Task Invoice_with_only_unmapped_vouchers_is_queued_and_shown_as_business_error()
    {
        await using var context = CreateContext();
        SeedInvoice(context, 29991, "D-01300", 5000m);
        AddVoucher(context, 5, 29991, "Receipt", "Discount", 5000m);
        await context.SaveChangesAsync();
        var poster = new FakePoster();
        var service = new FinanceSyncService(context,
            new FinanceInvoicePayloadBuilder(context), poster,
            new FakeStatusReader(FinanceRemoteEventState.NotPosted));

        var queued = await service.QueueFinanceSyncAsync(29991, 1, 1);
        var row = await context.FinanceSyncOutboxes.SingleAsync();
        var sent = await service.DispatchClaimedAsync(row, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(queued, Is.EqualTo(FinanceQueueResult.Queued));
            Assert.That(sent, Is.False);
            Assert.That(row.Status, Is.EqualTo(FinanceSyncService.BusinessError));
            Assert.That(row.LastError, Does.Contain("Review its vouchers in InvEntry"));
            Assert.That(poster.CallCount, Is.Zero);
        });
    }

    private static void SeedInvoice(MijmsContext context, int gkey, string number, decimal amount)
    {
        context.InvoiceHeaders.Add(new InvoiceHeader
        {
            Gkey = gkey,
            InvNbr = number,
            InvDate = new DateTime(2026, 10, 16, 10, 0, 0),
            Status = InvoiceStatus.Finalised,
            AmountPayable = amount,
            RecdAmount = 0m
        });
    }

    private static void AddVoucher(MijmsContext context, int gkey, int invoiceGkey,
        string transType, string modeOrType, decimal amount)
    {
        var isJournal = transType == "Journal";
        context.Vouchers.Add(new Voucher
        {
            Gkey = 30000 + gkey,
            RefDocGkey = invoiceGkey,
            TransType = transType,
            VoucherType = isJournal ? modeOrType : null,
            Mode = isJournal ? "Credit" : modeOrType,
            TransAmount = amount,
            TransDesc = "test"
        });
    }

    private static MijmsContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MijmsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(
                InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new MijmsContext(options);
    }

    private static void SeedD01274(MijmsContext context)
    {
        context.InvoiceHeaders.Add(new InvoiceHeader
        {
            Gkey = 29944,
            InvNbr = "D-01274",
            InvDate = new DateTime(2026, 10, 11, 20, 20, 52),
            Status = InvoiceStatus.Finalised,
            AmountPayable = 50000m,
            RecdAmount = 50000m
        });
        context.Vouchers.Add(new Voucher
        {
            Gkey = 20766,
            RefDocGkey = 29944,
            TransType = "Receipt",
            Mode = "Cash",
            TransAmount = 50000m,
            TransDesc = "Invoice Receipt / Cash"
        });
    }

    private sealed class MissingSchemaChecker(params string[] missing)
        : IFinanceSyncSchemaReadinessChecker
    {
        public Task<IReadOnlyList<string>> FindMissingObjectsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>(missing);
    }

    private sealed class FakePoster : IFinanceDocumentPoster
    {
        public int CallCount { get; private set; }

        public Task<FinancePostingResult> PostAsync(
            FinanceDocumentPayload payload, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(
                new FinancePostingResult(FinancePostingOutcome.Posted, null));
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
