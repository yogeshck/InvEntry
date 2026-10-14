using DataAccess.Models;
using DataAccess.Services;
using DataAccess.Controllers;
using InvEntry.Contracts.Invoices;
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
    public async Task Eight_manual_invoice_movements_clear_when_all_are_verified()
    {
        await using var db = Context();
        for (var id = 1; id <= 8; id++)
            AddInvoice(db, id, Day, cash: true);
        db.Vouchers.Add(new Voucher { Gkey = 100, RefDocGkey = 1,
            TransType = "Journal", VoucherType = "OM Purchase",
            Mode = "OM Purchase", TransAmount = 8500m });
        await db.SaveChangesAsync();
        var evidence = Enumerable.Range(1, 8).ToDictionary(x => x,
            _ => FinanceClosingEvidenceDecision.Verified);

        var result = await Service(db, evidence: evidence)
            .EvaluateAsync(1, 1, Day);

        Assert.Multiple(() =>
        {
            Assert.That(result.Decision, Is.EqualTo("CLEAR"));
            Assert.That(result.RequiredCount, Is.EqualTo(8));
            Assert.That(result.SynchronizedCount, Is.EqualTo(8));
            Assert.That(result.MissingOutboxCount, Is.Zero);
        });
    }

    [Test]
    public void Unique_posted_document_is_verified()
    {
        var requirement = Requirement();

        var result = FinanceClosingFinancialEvidenceReader.EvaluateInvoice(
            requirement, [new(20, "POSTED")], []);

        Assert.That(result, Is.EqualTo(FinanceClosingEvidenceDecision.Verified));
    }

    [Test]
    public void Exact_unique_manual_movement_is_verified()
    {
        var requirement = Requirement();

        var result = FinanceClosingFinancialEvidenceReader.EvaluateInvoice(
            requirement, [], [Manual(1, 100m)]);

        Assert.That(result, Is.EqualTo(FinanceClosingEvidenceDecision.Verified));
    }

    [Test]
    public void Duplicate_manual_movement_is_ambiguous()
    {
        var requirement = Requirement();

        var result = FinanceClosingFinancialEvidenceReader.EvaluateInvoice(
            requirement, [], [Manual(1, 100m), Manual(2, 100m)]);

        Assert.That(result, Is.EqualTo(FinanceClosingEvidenceDecision.Ambiguous));
    }

    [Test]
    public void Missing_manual_movement_is_missing()
    {
        var result = FinanceClosingFinancialEvidenceReader.EvaluateInvoice(
            Requirement(), [], []);

        Assert.That(result, Is.EqualTo(FinanceClosingEvidenceDecision.Missing));
    }

    [Test]
    public void Incorrect_manual_amount_is_mismatched()
    {
        var result = FinanceClosingFinancialEvidenceReader.EvaluateInvoice(
            Requirement(), [], [Manual(1, 99m)]);

        Assert.That(result, Is.EqualTo(FinanceClosingEvidenceDecision.Mismatched));
    }

    [TestCase("OUT", "CASH", "POSTED", 0)]
    [TestCase("IN", "UPI", "POSTED", 0)]
    [TestCase("IN", "CASH", "CANCELLED", 0)]
    [TestCase("IN", "CASH", "POSTED", 1)]
    public void Incorrect_manual_attributes_are_mismatched(string direction,
        string mode, string status, int dayOffset)
    {
        var row = Manual(1, 100m) with
        {
            Direction = direction,
            PaymentMode = mode,
            Status = status,
            TransactionDate = Day.AddDays(dayOffset)
        };

        var result = FinanceClosingFinancialEvidenceReader.EvaluateInvoice(
            Requirement(), [], [row]);

        Assert.That(result, Is.EqualTo(FinanceClosingEvidenceDecision.Mismatched));
    }

    [Test]
    public void Nonposted_document_is_mismatched()
    {
        var result = FinanceClosingFinancialEvidenceReader.EvaluateInvoice(
            Requirement(), [new(20, "CANCELLED")], []);

        Assert.That(result, Is.EqualTo(FinanceClosingEvidenceDecision.Mismatched));
    }

    [Test]
    public void Posted_document_and_manual_movement_are_ambiguous()
    {
        var result = FinanceClosingFinancialEvidenceReader.EvaluateInvoice(
            Requirement(), [new(20, "POSTED")], [Manual(1, 100m)]);

        Assert.That(result, Is.EqualTo(FinanceClosingEvidenceDecision.Ambiguous));
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
        var fence = Fence(db, eligibility);
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
        var fence = Fence(db, eligibility);
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
        var fence = Fence(db);
        var id = Guid.NewGuid();
        await fence.ReserveAsync(new(id, 1, 1, Day), default);
        await fence.MarkUncertainAsync(id, default);

        Assert.ThrowsAsync<InvalidOperationException>(() => fence.CancelAsync(id, default));
        await fence.CancelAfterVerifiedOpenAsync(id, 1, 1, default);

        Assert.That((await db.FinanceClosingFences.SingleAsync()).Status,
            Is.EqualTo(FinanceClosingFenceService.Cancelled));
    }

    [Test]
    public async Task Confirmed_fence_is_cancelled_only_after_verified_open_balance()
    {
        await using var db = Context();
        var fence = Fence(db);
        var id = Guid.NewGuid();
        await fence.ReserveAsync(new(id, 1, 1, Day), default);
        await fence.ConfirmAsync(id, default);
        var before = (await db.FinanceClosingFences.SingleAsync()).ModifiedOnUtc;

        await fence.CancelAfterVerifiedOpenAsync(id, 1, 1, default);

        var row = await db.FinanceClosingFences.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(row.Status, Is.EqualTo(FinanceClosingFenceService.Cancelled));
            Assert.That(row.ReservationId, Is.EqualTo(id));
            Assert.That(row.ModifiedOnUtc, Is.GreaterThanOrEqualTo(before));
        });
    }

    [TestCase(FinanceClosingFenceService.Confirmed)]
    [TestCase(FinanceClosingFenceService.Uncertain)]
    public async Task Recoverable_fence_returns_its_existing_reservation_id(
        string recoverableStatus)
    {
        await using var db = Context();
        var fence = Fence(db);
        var existingId = Guid.NewGuid();
        await fence.ReserveAsync(new(existingId, 1, 1, Day), default);
        if (recoverableStatus == FinanceClosingFenceService.Confirmed)
            await fence.ConfirmAsync(existingId, default);
        else
            await fence.MarkUncertainAsync(existingId, default);

        var result = await fence.ReserveAsync(
            new(Guid.NewGuid(), 1, 1, Day), default);

        Assert.Multiple(() =>
        {
            Assert.That(result.ReservationId, Is.EqualTo(existingId));
            Assert.That(result.FenceStatus, Is.EqualTo(recoverableStatus));
            Assert.That(result.Eligibility.Decision, Is.EqualTo("CLEAR"));
        });
        Assert.That((await db.FinanceClosingFences.SingleAsync()).Status,
            Is.EqualTo(recoverableStatus));
    }

    [TestCase(FinanceClosingCashBalanceState.Missing)]
    [TestCase(FinanceClosingCashBalanceState.ClosedOrFinalised)]
    [TestCase(FinanceClosingCashBalanceState.Ambiguous)]
    public async Task Confirmed_fence_recovery_rejects_non_open_balance(
        FinanceClosingCashBalanceState state)
    {
        await using var db = Context();
        var fence = Fence(db, verifier: new FakeCashBalanceVerifier(state));
        var id = Guid.NewGuid();
        await fence.ReserveAsync(new(id, 1, 1, Day), default);
        await fence.ConfirmAsync(id, default);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            fence.CancelAfterVerifiedOpenAsync(id, 1, 1, default));
        Assert.That((await db.FinanceClosingFences.SingleAsync()).Status,
            Is.EqualTo(FinanceClosingFenceService.Confirmed));
    }

    [Test]
    public async Task Confirmed_fence_recovery_rejects_unavailable_verification()
    {
        await using var db = Context();
        var verifier = new FakeCashBalanceVerifier(exception:
            new InvalidOperationException("unavailable"));
        var fence = Fence(db, verifier: verifier);
        var id = Guid.NewGuid();
        await fence.ReserveAsync(new(id, 1, 1, Day), default);
        await fence.ConfirmAsync(id, default);

        var error = Assert.ThrowsAsync<InvalidOperationException>(() =>
            fence.CancelAfterVerifiedOpenAsync(id, 1, 1, default));
        Assert.That(error!.Message, Does.Contain("unavailable"));
        Assert.That((await db.FinanceClosingFences.SingleAsync()).Status,
            Is.EqualTo(FinanceClosingFenceService.Confirmed));
    }

    [Test]
    public async Task Confirmed_fence_recovery_rejects_scope_or_reservation_mismatch()
    {
        await using var db = Context();
        var fence = Fence(db);
        var id = Guid.NewGuid();
        await fence.ReserveAsync(new(id, 1, 1, Day), default);
        await fence.ConfirmAsync(id, default);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            fence.CancelAfterVerifiedOpenAsync(id, 1, 2, default));
        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            fence.CancelAfterVerifiedOpenAsync(Guid.NewGuid(), 1, 1, default));
    }

    [Test]
    public async Task Recovery_rejects_active_or_already_cancelled_fence()
    {
        await using var db = Context();
        var fence = Fence(db);
        var id = Guid.NewGuid();
        await fence.ReserveAsync(new(id, 1, 1, Day), default);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            fence.CancelAfterVerifiedOpenAsync(id, 1, 1, default));
        await fence.CancelAsync(id, default);
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            fence.CancelAfterVerifiedOpenAsync(id, 1, 1, default));
    }

    [Test]
    public async Task Recovery_rechecks_fence_state_after_balance_verification()
    {
        await using var db = Context();
        var id = Guid.NewGuid();
        var verifier = new FakeCashBalanceVerifier(onVerify: async () =>
        {
            var row = await db.FinanceClosingFences.SingleAsync();
            row.Status = FinanceClosingFenceService.Reserved;
            await db.SaveChangesAsync();
        });
        var fence = Fence(db, verifier: verifier);
        await fence.ReserveAsync(new(id, 1, 1, Day), default);
        await fence.ConfirmAsync(id, default);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            fence.CancelAfterVerifiedOpenAsync(id, 1, 1, default));
        Assert.That((await db.FinanceClosingFences.SingleAsync()).Status,
            Is.EqualTo(FinanceClosingFenceService.Reserved));
    }

    private static MijmsContext Context() => new(new DbContextOptionsBuilder<MijmsContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

    private static FinanceClosingEligibilityService Service(MijmsContext db,
        bool scopeVerified = true,
        IReadOnlyDictionary<int, FinanceClosingEvidenceDecision>? evidence = null) =>
        new(db, Options.Create(new FinanceSyncOptions
        {
            Enabled = true, OrgGkey = 1, LocationGkey = 1,
            ClosingEligibilityScopeVerified = scopeVerified,
            ClosingEligibilityCoverageStartDate = Day.AddDays(-30)
        }), new FakeFinancialEvidenceReader(evidence));

    private static FinanceClosingFenceService Fence(MijmsContext db,
        FinanceClosingEligibilityService? eligibility = null,
        IFinanceClosingCashBalanceVerifier? verifier = null) =>
        new(db, eligibility ?? Service(db),
            verifier ?? new FakeCashBalanceVerifier());

    private static void AddInvoice(MijmsContext db, int id, DateOnly date, bool cash)
    {
        db.InvoiceHeaders.Add(new InvoiceHeader { Gkey = id, InvNbr = $"I{id}",
            InvDate = date.ToDateTime(TimeOnly.MinValue), Status = InvoiceStatus.Finalised, AmountPayable = 100 });
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

    private static FinanceClosingInvoiceRequirement Requirement() => new(
        1, "I1", Day, "INVOICE-1-FINAL",
        [new FinanceClosingMovement("IN", "CASH", 100m)]);

    private static FinanceClosingManualEvidence Manual(long id, decimal amount) =>
        new(id, Day, null, "I1", "POSTED", "IN", "CASH", amount);

    private sealed class FakeFinancialEvidenceReader(
        IReadOnlyDictionary<int, FinanceClosingEvidenceDecision>? evidence = null)
        : IFinanceClosingFinancialEvidenceReader
    {
        public Task<IReadOnlyDictionary<int, FinanceClosingEvidenceDecision>> ReadAsync(
            int orgGkey, int locationGkey,
            IReadOnlyList<FinanceClosingInvoiceRequirement> requirements,
            CancellationToken cancellationToken) => Task.FromResult(
                evidence ?? new Dictionary<int, FinanceClosingEvidenceDecision>());
    }

    private sealed class FakeCashBalanceVerifier(
        FinanceClosingCashBalanceState state = FinanceClosingCashBalanceState.Open,
        Exception? exception = null, Func<Task>? onVerify = null)
        : IFinanceClosingCashBalanceVerifier
    {
        public async Task<FinanceClosingCashBalanceState> VerifyAsync(int orgGkey,
            int locationGkey, DateOnly businessDate,
            CancellationToken cancellationToken)
        {
            if (exception != null) throw exception;
            if (onVerify != null) await onVerify();
            return state;
        }
    }
}
