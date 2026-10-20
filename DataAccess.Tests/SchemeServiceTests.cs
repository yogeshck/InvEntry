using DataAccess.Models;
using DataAccess.Services;
using DataAccess.Services.Advances;
using DataAccess.Services.Schemes;
using InvEntry.Contracts.Advances;
using InvEntry.Contracts.Schemes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DataAccess.Tests;

/// <summary>
/// Scheme plans, enrollment and installment receipts on SQLite in memory.
/// </summary>
[TestFixture]
public sealed class SchemeServiceTests
{
    private const int CustomerGkey = 601;
    private const decimal GoldRate = 7500M;

    private SqliteConnection _connection = null!;
    private MijmsContext _context = null!;
    private SchemePlanService _plans = null!;
    private SchemeEnrollmentService _enrollments = null!;
    private AdvanceAccountService _receipts = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _context = CreateContext();
        _context.Database.EnsureCreated();

        _context.OrgCustomers.Add(new OrgCustomer { Gkey = CustomerGkey, CustomerName = "Meena", MobileNbr = "9000000002" });
        _context.MtblLedgers.AddRange(
            new MtblLedger { LedgerName = "Advance", LedgerAccountCode = 1000 },
            new MtblLedger { LedgerName = "Cash", LedgerAccountCode = 2000 },
            new MtblLedger { LedgerName = "RD", LedgerAccountCode = 3000 });
        _context.VoucherTypes.AddRange(
            new VoucherType { Gkey = 1, DocumentType = AdvanceReceiptType.AdvanceReceipt, DocNbrPrefix = "AR-", DocNbrLength = 4, LastUsedNumber = 0, IsActive = true },
            new VoucherType { Gkey = 2, DocumentType = AdvanceReceiptType.RecurringDeposit, DocNbrPrefix = "RD-", DocNbrLength = 4, LastUsedNumber = 0, IsActive = true });
        _context.DailyRates.Add(new DailyRate
        {
            EffectiveDate = DateTime.Today, Metal = "GOLD", Purity = "916", Carat = "22 KT", Price = GoldRate, IsDisplay = true
        });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();

        var identity = new AuditIdentityProvider();
        _plans = new SchemePlanService(_context, identity);
        _enrollments = new SchemeEnrollmentService(_context, identity);
        _receipts = new AdvanceAccountService(_context, new VoucherNumberService(_context), identity);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    // =========================================================
    // PLANS
    // =========================================================

    [Test]
    public async Task Plan_is_saved_with_its_benefit_rules_and_updates_replace_them()
    {
        var saved = await _plans.SavePlanAsync(AmountPlan("GOLD11"));

        saved.PlanName = "Gold 11 + 1";
        saved.BenefitRules = new()
        {
            new() { BenefitType = SchemeTerms.BenefitPercentBonus, BenefitValue = 5M, AppliesOn = "MATURITY", MinInstallmentsPaid = 11 }
        };

        var updated = await _plans.SavePlanAsync(saved);

        Assert.Multiple(() =>
        {
            Assert.That(updated.Gkey, Is.EqualTo(saved.Gkey));
            Assert.That(updated.PlanName, Is.EqualTo("Gold 11 + 1"));
            Assert.That(updated.BenefitRules.Select(x => (x.SeqNbr, x.BenefitType)),
                Is.EqualTo(new[] { (1, SchemeTerms.BenefitPercentBonus) }));
            Assert.That(_context.SchemeBenefitRules.Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public void Plan_rules_are_enforced()
    {
        var tooLong = AmountPlan("LONG");
        tooLong.TenureMonths = 13;

        var weightWithoutRate = WeightPlan("SILVER");
        weightWithoutRate.Metal = "SILVER";
        weightWithoutRate.Purity = "XX";

        var gramsOnAmountPlan = AmountPlan("GRAMS");
        gramsOnAmountPlan.BenefitRules.Add(new() { BenefitType = SchemeTerms.BenefitBonusGrams, BenefitValue = 0.5M, AppliesOn = "MATURITY" });

        Assert.Multiple(() =>
        {
            Assert.That(() => _plans.SavePlanAsync(tooLong),
                Throws.TypeOf<AdvanceAccountBusinessException>().With.Message.Contains("between 1 and 12"));
            Assert.That(() => _plans.SavePlanAsync(weightWithoutRate),
                Throws.TypeOf<AdvanceAccountBusinessException>().With.Message.Contains("No daily rate"));
            Assert.That(() => _plans.SavePlanAsync(gramsOnAmountPlan),
                Throws.TypeOf<AdvanceAccountBusinessException>().With.Message.Contains("weight scheme"));
        });
    }

    [Test]
    public async Task Plan_code_must_be_unique()
    {
        await _plans.SavePlanAsync(AmountPlan("GOLD11"));

        Assert.That(() => _plans.SavePlanAsync(AmountPlan("gold11")),
            Throws.TypeOf<AdvanceAccountBusinessException>().With.Message.Contains("already used"));
    }

    [Test]
    public async Task Metal_options_come_from_daily_rates()
    {
        var options = await _plans.GetMetalOptionsAsync();

        Assert.That(options.Select(x => (x.Metal, x.Purity, x.LatestPrice)),
            Is.EqualTo(new[] { ("GOLD", "916", (decimal?)GoldRate) }));
    }

    // =========================================================
    // ENROLLMENT
    // =========================================================

    [Test]
    public async Task Enrollment_opens_a_scheme_account_with_a_monthly_schedule()
    {
        var plan = await _plans.SavePlanAsync(AmountPlan("GOLD11"));

        var enrollment = await _enrollments.EnrollAsync(Enroll(plan.Gkey, 2000M, new DateOnly(2027, 1, 31)));

        var account = await _context.CustomerAdvanceAccounts.AsNoTracking().SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(account.AccountType, Is.EqualTo("SCHEME"));
            Assert.That(account.AccountNbr, Is.EqualTo($"RD-{account.Gkey:D6}"));
            Assert.That(enrollment.AccountNbr, Is.EqualTo(account.AccountNbr));
            Assert.That(enrollment.MaturityDate, Is.EqualTo(new DateOnly(2027, 12, 31)));
            Assert.That(enrollment.Installments, Has.Count.EqualTo(11));
            Assert.That(enrollment.Installments.Take(3).Select(x => x.DueDate), Is.EqualTo(new[]
            {
                new DateOnly(2027, 1, 31), new DateOnly(2027, 2, 28), new DateOnly(2027, 3, 31)
            }));
            Assert.That(enrollment.Installments.All(x => x.AmountDue == 2000M && x.Status == "PENDING"));
            Assert.That(enrollment.TotalDue, Is.EqualTo(22000M));
            Assert.That(enrollment.NextInstallment!.SeqNbr, Is.EqualTo(1));
            Assert.That(enrollment.CustomerName, Is.EqualTo("Meena"));
        });
    }

    [Test]
    public async Task Retried_enrollment_returns_the_original()
    {
        var plan = await _plans.SavePlanAsync(AmountPlan("GOLD11"));
        var request = Enroll(plan.Gkey, 2000M);

        var first = await _enrollments.EnrollAsync(request);

        using var retryContext = CreateContext();
        var retry = await new SchemeEnrollmentService(retryContext, new AuditIdentityProvider()).EnrollAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(retry.IsReplay, Is.True);
            Assert.That(retry.EnrollmentGkey, Is.EqualTo(first.EnrollmentGkey));
            Assert.That(_context.SchemeEnrollments.Count(), Is.EqualTo(1));
            Assert.That(_context.CustomerAdvanceAccounts.Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Enrollment_respects_plan_status_dates_and_amount_limits()
    {
        var plan = AmountPlan("GOLD11");
        plan.Status = SchemePlanStatus.Draft;
        var draft = await _plans.SavePlanAsync(plan);

        var active = await _plans.SavePlanAsync(AmountPlan("GOLD12"));

        Assert.Multiple(() =>
        {
            Assert.That(() => _enrollments.EnrollAsync(Enroll(draft.Gkey, 2000M)),
                Throws.TypeOf<AdvanceAccountBusinessException>().With.Message.Contains("only ACTIVE"));
            Assert.That(() => _enrollments.EnrollAsync(Enroll(active.Gkey, 500M)),
                Throws.TypeOf<AdvanceAccountBusinessException>().With.Message.Contains("at least"));
            Assert.That(() => _enrollments.EnrollAsync(Enroll(active.Gkey, 60000M)),
                Throws.TypeOf<AdvanceAccountBusinessException>().With.Message.Contains("cannot exceed"));
            Assert.That(() => _enrollments.EnrollAsync(Enroll(active.Gkey, 2000M, new DateOnly(2025, 1, 1))),
                Throws.TypeOf<AdvanceAccountBusinessException>().With.Message.Contains("takes enrollments from"));
            Assert.That(_context.CustomerAdvanceAccounts.Count(), Is.Zero);
        });
    }

    [Test]
    public async Task A_customer_can_hold_several_schemes_and_must_choose_one_for_a_receipt()
    {
        var plan = await _plans.SavePlanAsync(AmountPlan("GOLD11"));
        var first = await _enrollments.EnrollAsync(Enroll(plan.Gkey, 1000M));
        var second = await _enrollments.EnrollAsync(Enroll(plan.Gkey, 3000M));

        Assert.That(() => _receipts.CreateReceiptAsync(RdReceipt(null, 1000M)),
            Throws.TypeOf<AdvanceAccountBusinessException>().With.Message.Contains("Select the account"));

        var receipt = await _receipts.CreateReceiptAsync(RdReceipt(second.AdvanceAccountGkey, 3000M));
        var customerSchemes = await _enrollments.GetCustomerEnrollmentsAsync(CustomerGkey, includeClosed: false);

        Assert.Multiple(() =>
        {
            Assert.That(receipt.AccountNbr, Is.EqualTo(second.AccountNbr));
            Assert.That(receipt.InstallmentsPaid, Is.EqualTo(1));
            Assert.That(customerSchemes.Select(x => x.EnrollmentGkey),
                Is.EquivalentTo(new[] { first.EnrollmentGkey, second.EnrollmentGkey }));
            Assert.That(customerSchemes.Single(x => x.EnrollmentGkey == first.EnrollmentGkey).TotalPaid, Is.Zero);
        });
    }

    [Test]
    public async Task Scheme_is_found_by_its_rd_number_in_any_case()
    {
        var plan = await _plans.SavePlanAsync(AmountPlan("GOLD11"));
        var first = await _enrollments.EnrollAsync(Enroll(plan.Gkey, 1000M));
        var second = await _enrollments.EnrollAsync(Enroll(plan.Gkey, 3000M));

        var found = await _enrollments.FindByAccountNumberAsync($"  {second.AccountNbr.ToLowerInvariant()} ");
        var unknown = await _enrollments.FindByAccountNumberAsync(first.AccountNbr + "X");
        var blank = await _enrollments.FindByAccountNumberAsync(" ");

        Assert.Multiple(() =>
        {
            Assert.That(found!.EnrollmentGkey, Is.EqualTo(second.EnrollmentGkey));
            Assert.That(found.CustomerGkey, Is.EqualTo(CustomerGkey));
            Assert.That(found.CustomerMobile, Is.EqualTo("9000000002"));
            Assert.That(found.NextInstallment!.AmountOutstanding, Is.EqualTo(3000M));
            Assert.That(unknown, Is.Null);
            Assert.That(blank, Is.Null);
        });
    }

    // =========================================================
    // INSTALLMENT RECEIPTS
    // =========================================================

    [Test]
    public async Task Rd_receipt_needs_an_enrolled_scheme()
    {
        Assert.That(() => _receipts.CreateReceiptAsync(RdReceipt(null, 1000M)),
            Throws.TypeOf<AdvanceAccountBusinessException>().With.Message.Contains("Enroll the customer"));

        Assert.That(_context.Vouchers.Count(), Is.Zero);
    }

    [Test]
    public async Task Fixed_plan_receipt_pays_oldest_installments_first_and_can_pay_part_of_one()
    {
        var plan = await _plans.SavePlanAsync(AmountPlan("GOLD11"));
        var enrollment = await _enrollments.EnrollAsync(Enroll(plan.Gkey, 1000M));

        var receipt = await _receipts.CreateReceiptAsync(RdReceipt(enrollment.AdvanceAccountGkey, 2500M));

        var schedule = (await _enrollments.GetEnrollmentAsync(enrollment.EnrollmentGkey))!.Installments;
        var allocations = await _context.SchemeInstallmentAllocations.AsNoTracking().ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(receipt.VoucherNbr, Is.EqualTo("RD-0001"));
            Assert.That(receipt.BalanceAmount, Is.EqualTo(2500M));
            Assert.That(receipt.InstallmentsPaid, Is.EqualTo(2));
            Assert.That(schedule.Take(4).Select(x => (x.Status, x.AmountPaid)), Is.EqualTo(new[]
            {
                ("PAID", 1000M), ("PAID", 1000M), ("PARTIAL", 500M), ("PENDING", 0M)
            }));
            Assert.That(allocations.Sum(x => x.Amount), Is.EqualTo(2500M));
            Assert.That(allocations.All(x => x.AdvanceLedgerGkey == receipt.AdvanceLedgerGkey));
        });
    }

    [Test]
    public async Task Fixed_plan_receipt_cannot_exceed_what_is_still_due()
    {
        var plan = AmountPlan("SHORT");
        plan.TenureMonths = 2;
        plan.BenefitRules.Clear();
        var saved = await _plans.SavePlanAsync(plan);
        var enrollment = await _enrollments.EnrollAsync(Enroll(saved.Gkey, 1000M));

        await _receipts.CreateReceiptAsync(RdReceipt(enrollment.AdvanceAccountGkey, 1500M));

        Assert.That(() => _receipts.CreateReceiptAsync(RdReceipt(enrollment.AdvanceAccountGkey, 600M)),
            Throws.TypeOf<AdvanceAccountBusinessException>().With.Message.Contains("Only ₹500.00 is still due"));

        var account = await _context.CustomerAdvanceAccounts.AsNoTracking().SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(account.BalanceAmount, Is.EqualTo(1500M));
            Assert.That(_context.Vouchers.Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Weight_plan_receipt_converts_at_todays_rate_and_pays_one_month()
    {
        var plan = await _plans.SavePlanAsync(WeightPlan("SWARNA"));
        var enrollment = await _enrollments.EnrollAsync(Enroll(plan.Gkey, 1000M));

        var receipt = await _receipts.CreateReceiptAsync(RdReceipt(enrollment.AdvanceAccountGkey, 5000M));

        var ledger = await _context.AdvanceLedgers.AsNoTracking().SingleAsync();
        var schedule = (await _enrollments.GetEnrollmentAsync(enrollment.EnrollmentGkey))!.Installments;

        Assert.Multiple(() =>
        {
            // 5000 / 7500 = 0.6666.. g, truncated to 0.666 g.
            Assert.That(receipt.Weight, Is.EqualTo(0.666M));
            Assert.That(receipt.MetalRate, Is.EqualTo(GoldRate));
            Assert.That(receipt.BalanceWeight, Is.EqualTo(0.666M));
            Assert.That(ledger.Weight, Is.EqualTo(0.666M));
            Assert.That(ledger.DailyRateGkey, Is.Not.Null);
            Assert.That(schedule[0].Status, Is.EqualTo("PAID"));
            Assert.That(schedule[0].AmountPaid, Is.EqualTo(5000M));
            Assert.That(schedule[0].WeightCredited, Is.EqualTo(0.666M));
            Assert.That(schedule[1].Status, Is.EqualTo("PENDING"));
        });
    }

    [Test]
    public async Task Flexible_plan_receipt_must_be_between_the_minimum_and_maximum()
    {
        var plan = await _plans.SavePlanAsync(WeightPlan("SWARNA"));
        var enrollment = await _enrollments.EnrollAsync(Enroll(plan.Gkey, 1000M));

        Assert.Multiple(() =>
        {
            Assert.That(() => _receipts.CreateReceiptAsync(RdReceipt(enrollment.AdvanceAccountGkey, 900M)),
                Throws.TypeOf<AdvanceAccountBusinessException>().With.Message.Contains("needs at least ₹1,000.00"));
            Assert.That(() => _receipts.CreateReceiptAsync(RdReceipt(enrollment.AdvanceAccountGkey, 25000M)),
                Throws.TypeOf<AdvanceAccountBusinessException>().With.Message.Contains("cannot exceed ₹20,000.00"));
            Assert.That(_context.AdvanceLedgers.Count(), Is.Zero);
        });
    }

    [Test]
    public async Task Weight_plan_receipt_needs_the_rate_of_the_receipt_date()
    {
        var plan = await _plans.SavePlanAsync(WeightPlan("SWARNA"));
        var enrollment = await _enrollments.EnrollAsync(Enroll(plan.Gkey, 1000M));

        var request = RdReceipt(enrollment.AdvanceAccountGkey, 5000M);
        request.TransDate = DateTime.Today.AddDays(1);

        Assert.That(() => _receipts.CreateReceiptAsync(request),
            Throws.TypeOf<AdvanceAccountBusinessException>().With.Message.Contains("rate for"));
    }

    [Test]
    public async Task Enrollment_keeps_the_terms_it_was_enrolled_on()
    {
        var plan = await _plans.SavePlanAsync(WeightPlan("SWARNA"));
        var enrollment = await _enrollments.EnrollAsync(Enroll(plan.Gkey, 1000M));

        // The plan's maximum is lowered after the customer enrolled.
        plan.MaxInstallmentAmount = 2000M;
        await _plans.SavePlanAsync(plan);

        var receipt = await _receipts.CreateReceiptAsync(RdReceipt(enrollment.AdvanceAccountGkey, 15000M));

        Assert.That(receipt.Amount, Is.EqualTo(15000M));
    }

    [Test]
    public async Task Retried_scheme_receipt_pays_the_installments_once()
    {
        var plan = await _plans.SavePlanAsync(AmountPlan("GOLD11"));
        var enrollment = await _enrollments.EnrollAsync(Enroll(plan.Gkey, 1000M));
        var request = RdReceipt(enrollment.AdvanceAccountGkey, 1000M);

        await _receipts.CreateReceiptAsync(request);

        using var retryContext = CreateContext();
        var retry = await new AdvanceAccountService(
                retryContext, new VoucherNumberService(retryContext), new AuditIdentityProvider())
            .CreateReceiptAsync(request);

        var schedule = (await _enrollments.GetEnrollmentAsync(enrollment.EnrollmentGkey))!.Installments;

        Assert.Multiple(() =>
        {
            Assert.That(retry.IsReplay, Is.True);
            Assert.That(retry.InstallmentsPaid, Is.EqualTo(1));
            Assert.That(schedule.Sum(x => x.AmountPaid), Is.EqualTo(1000M));
            Assert.That(_context.SchemeInstallmentAllocations.Count(), Is.EqualTo(1));
        });
    }

    // =========================================================
    // RECEIPT PRINT
    // =========================================================

    [Test]
    public async Task Receipt_print_shows_the_installments_as_they_stood_after_that_receipt()
    {
        var plan = await _plans.SavePlanAsync(AmountPlan("GOLD11"));
        var enrollment = await _enrollments.EnrollAsync(Enroll(plan.Gkey, 1000M));

        var first = await _receipts.CreateReceiptAsync(RdReceipt(enrollment.AdvanceAccountGkey, 2500M));
        var second = await _receipts.CreateReceiptAsync(RdReceipt(enrollment.AdvanceAccountGkey, 500M));

        // The first receipt is printed again after the second one was taken.
        var reprint = (await _receipts.GetReceiptPrintAsync(first.VoucherGkey))!;
        var latest = (await _receipts.GetReceiptPrintAsync(second.VoucherGkey))!;

        Assert.Multiple(() =>
        {
            Assert.That(reprint.IsScheme, Is.True);
            Assert.That(reprint.CustomerName, Is.EqualTo("Meena"));
            Assert.That(reprint.PlanCode, Is.EqualTo("GOLD11"));
            Assert.That(reprint.Amount, Is.EqualTo(2500M));
            Assert.That(reprint.InstallmentsCovered, Is.EqualTo("1, 2, 3 (part)"));
            Assert.That(reprint.InstallmentsPaid, Is.EqualTo(2));
            Assert.That(reprint.BalanceAfter, Is.EqualTo(2500M));
            Assert.That(reprint.TotalReceivedToDate, Is.EqualTo(2500M));
            Assert.That(reprint.NextDueAmount, Is.EqualTo(500M));
            Assert.That(reprint.NextDueDate, Is.EqualTo(enrollment.Installments[2].DueDate));

            Assert.That(latest.InstallmentsCovered, Is.EqualTo("3"));
            Assert.That(latest.InstallmentsPaid, Is.EqualTo(3));
            Assert.That(latest.BalanceAfter, Is.EqualTo(3000M));
            Assert.That(latest.NextDueAmount, Is.EqualTo(1000M));
            Assert.That(latest.NextDueDate, Is.EqualTo(enrollment.Installments[3].DueDate));
            Assert.That(latest.MaturityDate, Is.EqualTo(enrollment.MaturityDate));
            Assert.That(latest.NomineeName, Is.EqualTo("Arun"));
        });
    }

    [Test]
    public async Task Weight_receipt_print_shows_grams_and_rate()
    {
        var plan = await _plans.SavePlanAsync(WeightPlan("SWARNA"));
        var enrollment = await _enrollments.EnrollAsync(Enroll(plan.Gkey, 1000M));
        var receipt = await _receipts.CreateReceiptAsync(RdReceipt(enrollment.AdvanceAccountGkey, 15000M));

        var print = (await _receipts.GetReceiptPrintAsync(receipt.VoucherGkey))!;

        Assert.Multiple(() =>
        {
            Assert.That(print.Weight, Is.EqualTo(2.000M));
            Assert.That(print.MetalRate, Is.EqualTo(GoldRate));
            Assert.That(print.Metal, Is.EqualTo("GOLD"));
            Assert.That(print.Purity, Is.EqualTo("916"));
            Assert.That(print.WeightBalanceAfter, Is.EqualTo(2.000M));
        });
    }

    [Test]
    public async Task Advance_receipt_print_shows_the_balance_and_other_vouchers_are_not_printed()
    {
        var first = await _receipts.CreateReceiptAsync(AdvanceReceipt(4000M));
        var second = await _receipts.CreateReceiptAsync(AdvanceReceipt(1000M));

        var print = (await _receipts.GetReceiptPrintAsync(first.VoucherGkey))!;

        Assert.Multiple(() =>
        {
            Assert.That(print.IsScheme, Is.False);
            Assert.That(print.ReceiptType, Is.EqualTo(AdvanceReceiptType.AdvanceReceipt));
            Assert.That(print.VoucherNbr, Is.EqualTo("AR-0001"));
            Assert.That(print.BalanceAfter, Is.EqualTo(4000M));
            Assert.That(print.AccountType, Is.EqualTo("OPEN"));
            Assert.That(second.BalanceAmount, Is.EqualTo(5000M));
        });

        Assert.That(await _receipts.GetReceiptPrintAsync(999999), Is.Null);
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static AdvanceReceiptRequest AdvanceReceipt(decimal amount) => new()
    {
        ClientRequestId = Guid.NewGuid(),
        CustomerGkey = CustomerGkey,
        ReceiptType = AdvanceReceiptType.AdvanceReceipt,
        Amount = amount,
        Mode = "Cash"
    };

    private MijmsContext CreateContext() =>
        new AdvanceAccountServiceTests.SqliteMijmsContext(new DbContextOptionsBuilder<MijmsContext>()
            .UseSqlite(_connection)
            .Options);

    private static SchemePlanDto AmountPlan(string code) => new()
    {
        PlanCode = code,
        PlanName = $"{code} plan",
        SchemeType = SchemeType.Amount,
        TenureMonths = 11,
        AmountFixed = true,
        MinInstallmentAmount = 1000M,
        MaxInstallmentAmount = 50000M,
        ValidFrom = new DateOnly(2026, 1, 1),
        Status = SchemePlanStatus.Active,
        BenefitRules = new()
        {
            new() { BenefitType = SchemeTerms.BenefitInstallmentWaiver, BenefitValue = 1M, AppliesOn = "MATURITY", MinInstallmentsPaid = 11 },
            new() { BenefitType = SchemeTerms.BenefitMakingChargeWaiver, BenefitValue = 50M, AppliesOn = "MATURITY" }
        }
    };

    private static SchemePlanDto WeightPlan(string code) => new()
    {
        PlanCode = code,
        PlanName = $"{code} gold",
        SchemeType = SchemeType.Weight,
        Metal = "GOLD",
        Purity = "916",
        TenureMonths = 12,
        AmountFixed = false,
        MinInstallmentAmount = 1000M,
        MaxInstallmentAmount = 20000M,
        ValidFrom = new DateOnly(2026, 1, 1),
        Status = SchemePlanStatus.Active
    };

    private static SchemeEnrollmentRequest Enroll(int planGkey, decimal amount, DateOnly? start = null) => new()
    {
        ClientRequestId = Guid.NewGuid(),
        CustomerGkey = CustomerGkey,
        SchemePlanGkey = planGkey,
        InstallmentAmount = amount,
        StartDate = start ?? DateOnly.FromDateTime(DateTime.Today),
        NomineeName = "Arun",
        NomineeRelation = "Son"
    };

    private static AdvanceReceiptRequest RdReceipt(int? accountGkey, decimal amount) => new()
    {
        ClientRequestId = Guid.NewGuid(),
        CustomerGkey = CustomerGkey,
        ReceiptType = AdvanceReceiptType.RecurringDeposit,
        AdvanceAccountGkey = accountGkey,
        Amount = amount,
        Mode = "Cash"
    };
}
