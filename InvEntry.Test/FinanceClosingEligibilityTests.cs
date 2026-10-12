using DataAccess.Models;
using DataAccess.Services;
using DataAccess.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace InvEntry.Test;

[TestFixture]
public sealed class FinanceClosingEligibilityTests
{
    private static readonly DateOnly Day = new(2026, 10, 11);

    [Test]
    public async Task Sent_required_invoice_is_clear_and_check_is_read_only()
    {
        await using var db = Context();
        AddInvoice(db, 1, Day, cash: true);
        AddOutbox(db, 1, FinanceSyncService.Sent);
        await db.SaveChangesAsync();
        var service = Service(db);

        var first = await service.EvaluateAsync(1, 1, Day);
        var second = await service.EvaluateAsync(1, 1, Day);

        Assert.Multiple(() =>
        {
            Assert.That(first.Decision, Is.EqualTo("CLEAR"));
            Assert.That(first.RequiredCount, Is.EqualTo(1));
            Assert.That(first.SynchronizedCount, Is.EqualTo(1));
            Assert.That(second.Decision, Is.EqualTo(first.Decision));
            Assert.That(second.RequiredCount, Is.EqualTo(first.RequiredCount));
            Assert.That(second.SynchronizedCount, Is.EqualTo(first.SynchronizedCount));
            Assert.That(second.ReasonCodes, Is.EqualTo(first.ReasonCodes));
            Assert.That(db.ChangeTracker.HasChanges(), Is.False);
        });
    }

    [TestCase(FinanceSyncService.Pending, "BLOCKED", "PENDING_EVENTS")]
    [TestCase(FinanceSyncService.Uncertain, "BLOCKED", "UNCERTAIN_EVENTS")]
    [TestCase(FinanceSyncService.BusinessError, "BLOCKED", "BUSINESS_ERROR_EVENTS")]
    [TestCase("ERROR", "BLOCKED", "HISTORICAL_ERROR_EVENTS")]
    public async Task Blocking_outbox_states_never_clear(
        string status, string decision, string reason)
    {
        await using var db = Context();
        AddInvoice(db, 1, Day, cash: true);
        AddOutbox(db, 1, status);
        await db.SaveChangesAsync();

        var result = await Service(db).EvaluateAsync(1, 1, Day);

        Assert.That(result.Decision, Is.EqualTo(decision));
        Assert.That(result.ReasonCodes, Does.Contain(reason));
    }

    [Test]
    public async Task Missing_outbox_and_unresolved_earlier_document_block()
    {
        await using var db = Context();
        AddInvoice(db, 1, Day.AddDays(-1), cash: true);
        await db.SaveChangesAsync();

        var result = await Service(db).EvaluateAsync(1, 1, Day);

        Assert.Multiple(() =>
        {
            Assert.That(result.Decision, Is.EqualTo("BLOCKED"));
            Assert.That(result.MissingOutboxCount, Is.EqualTo(1));
            Assert.That(result.UnresolvedEarlierCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Today_invoice_does_not_block_yesterday_and_credit_only_is_not_required()
    {
        await using var db = Context();
        AddInvoice(db, 1, Day, cash: false);
        AddInvoice(db, 2, Day.AddDays(1), cash: true);
        await db.SaveChangesAsync();

        var result = await Service(db).EvaluateAsync(1, 1, Day);

        Assert.That(result.Decision, Is.EqualTo("CLEAR"));
        Assert.That(result.RequiredCount, Is.Zero);
    }

    [Test]
    public async Task Unknown_voucher_classification_is_indeterminate()
    {
        await using var db = Context();
        AddInvoice(db, 1, Day, cash: false);
        db.Vouchers.Add(new Voucher { Gkey = 50, RefDocGkey = 1,
            TransType = "Receipt", Mode = "Crypto", TransAmount = 10 });
        await db.SaveChangesAsync();

        var result = await Service(db).EvaluateAsync(1, 1, Day);

        Assert.That(result.Decision, Is.EqualTo("INDETERMINATE"));
        Assert.That(result.ReasonCodes, Does.Contain("UNKNOWN_FINANCIAL_ELIGIBILITY"));
    }

    [Test]
    public async Task Exact_bank_scanner_receipt_is_not_required_for_cash_closing()
    {
        await using var db = Context();
        AddInvoice(db, 1, Day, cash: false);
        db.Vouchers.Add(new Voucher { Gkey = 50, RefDocGkey = 1,
            TransType = "Receipt", VoucherType = "Bank Scanner",
            Mode = "Bank Scanner", TransAmount = 160000m });
        await db.SaveChangesAsync();

        var result = await Service(db).EvaluateAsync(1, 1, Day);

        Assert.That(result.Decision, Is.EqualTo("CLEAR"));
        Assert.That(result.RequiredCount, Is.Zero);
        Assert.That(result.ReasonCodes, Does.Not.Contain("UNKNOWN_FINANCIAL_ELIGIBILITY"));
    }

    [Test]
    public async Task Other_bank_or_unmapped_receipts_remain_indeterminate()
    {
        await using var db = Context();
        AddInvoice(db, 1, Day, cash: false);
        db.Vouchers.Add(new Voucher { Gkey = 50, RefDocGkey = 1,
            TransType = "Receipt", VoucherType = "Bank Transfer",
            Mode = "Bank Scanner", TransAmount = 10m });
        await db.SaveChangesAsync();

        var result = await Service(db).EvaluateAsync(1, 1, Day);

        Assert.That(result.Decision, Is.EqualTo("INDETERMINATE"));
        Assert.That(result.ReasonCodes, Does.Contain("UNKNOWN_FINANCIAL_ELIGIBILITY"));
    }

    [Test]
    public async Task Unverified_scope_is_indeterminate_without_query_mutation()
    {
        await using var db = Context();
        var service = Service(db, scopeVerified: false);

        var result = await service.EvaluateAsync(1, 1, Day);

        Assert.That(result.Decision, Is.EqualTo("INDETERMINATE"));
        Assert.That(result.ReasonCodes, Does.Contain("SCOPE_UNVERIFIED"));
        Assert.That(db.ChangeTracker.HasChanges(), Is.False);
    }

    [Test]
    public async Task Unauthorized_organization_or_location_is_rejected()
    {
        await using var db = Context();
        var management = new FinanceSyncManagementService(db,
            Options.Create(new FinanceSyncOptions { OrgGkey = 1, LocationGkey = 1 }));
        var eligibility = Service(db);
        var fence = new FinanceClosingFenceService(db, eligibility);
        var controller = new FinanceSyncManagementController(management, eligibility, fence,
            new FinanceSyncDispatchJobQueue(),
            NullLogger<FinanceSyncManagementController>.Instance);

        var result = await controller.ClosingEligibility(1, 2, Day, default);

        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task Reservation_is_idempotent_and_blocks_only_its_business_date()
    {
        await using var db = Context();
        var eligibility = Service(db);
        var fence = new FinanceClosingFenceService(db, eligibility);
        var id = Guid.NewGuid();
        var request = new FinanceClosingReservationRequest(id, 1, 1, Day);

        var first = await fence.ReserveAsync(request, default);
        var repeated = await fence.ReserveAsync(request, default);

        Assert.That(first.FenceStatus, Is.EqualTo(FinanceClosingFenceService.Reserved));
        Assert.That(repeated.FenceStatus, Is.EqualTo(first.FenceStatus));
        Assert.That(await db.FinanceClosingFences.CountAsync(), Is.EqualTo(1));
        Assert.DoesNotThrowAsync(() => fence.EnsurePostingAllowedAsync(
            1, 1, Day.AddDays(1), default));
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            fence.EnsurePostingAllowedAsync(1, 1, Day, default));
    }

    [Test]
    public async Task Uncertain_fence_requires_verified_open_recovery()
    {
        await using var db = Context();
        var fence = new FinanceClosingFenceService(db, Service(db));
        var id = Guid.NewGuid();
        await fence.ReserveAsync(new(id, 1, 1, Day), default);
        await fence.MarkUncertainAsync(id, default);

        Assert.ThrowsAsync<InvalidOperationException>(() => fence.CancelAsync(id, default));
        await fence.CancelAfterVerifiedOpenAsync(id, default);

        Assert.That((await db.FinanceClosingFences.SingleAsync()).Status,
            Is.EqualTo(FinanceClosingFenceService.Cancelled));
    }

    private static MijmsContext Context() => new(new DbContextOptionsBuilder<MijmsContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

    private static FinanceClosingEligibilityService Service(MijmsContext db,
        bool scopeVerified = true) => new(db, Options.Create(new FinanceSyncOptions
        {
            Enabled = true, OrgGkey = 1, LocationGkey = 1,
            ClosingEligibilityScopeVerified = scopeVerified,
            ClosingEligibilityCoverageStartDate = Day.AddDays(-30)
        }));

    private static void AddInvoice(MijmsContext db, int id, DateOnly date, bool cash)
    {
        db.InvoiceHeaders.Add(new InvoiceHeader { Gkey = id, InvNbr = $"I{id}",
            InvDate = date.ToDateTime(TimeOnly.MinValue), Status = "FINAL", AmountPayable = 100 });
        if (cash)
            db.Vouchers.Add(new Voucher { Gkey = id, RefDocGkey = id,
                TransType = "Receipt", Mode = "Cash", TransAmount = 100 });
    }

    private static void AddOutbox(MijmsContext db, int invoiceId, string status) =>
        db.FinanceSyncOutboxes.Add(new FinanceSyncOutbox
        {
            Gkey = invoiceId, EventType = "FINANCIAL_DOCUMENT", SourceType = "INVOICE",
            SourceGkey = invoiceId, SourceEventId = $"INVOICE-{invoiceId}-FINAL",
            DocumentDate = Day.ToDateTime(TimeOnly.MinValue), Payload = "{}",
            Status = status, CreatedOn = DateTime.UtcNow
        });
}
