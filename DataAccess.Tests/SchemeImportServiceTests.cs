using DataAccess.Models;
using DataAccess.Services;
using DataAccess.Services.Schemes;
using InvEntry.Contracts.Schemes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DataAccess.Tests;

/// <summary>
/// Import of the shop's RD register (made-up customers, same shapes as the real sheets).
/// </summary>
[TestFixture]
public sealed class SchemeImportServiceTests
{
    private SqliteConnection _connection = null!;
    private MijmsContext _context = null!;
    private SchemeImportService _import = null!;
    private int _planGkey;

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _context = CreateContext();
        _context.Database.EnsureCreated();

        _context.OrgCustomers.AddRange(
            new OrgCustomer { Gkey = 701, CustomerName = "ANITHA", MobileNbr = "9000000101" },
            new OrgCustomer { Gkey = 702, CustomerName = "BINU", MobileNbr = "+91 90000 00102" },
            new OrgCustomer { Gkey = 703, CustomerName = "CHITRA", MobileNbr = "9000000103" });
        _context.MtblLedgers.AddRange(
            new MtblLedger { LedgerName = "Advance", LedgerAccountCode = 1000 },
            new MtblLedger { LedgerName = "RD", LedgerAccountCode = 3000 });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();

        var identity = new AuditIdentityProvider();
        var plan = await new SchemePlanService(_context, identity).SavePlanAsync(new SchemePlanDto
        {
            PlanCode = "MJRD12",
            PlanName = "12 month RD",
            SchemeType = SchemeType.Amount,
            TenureMonths = 12,
            AmountFixed = true,
            MinInstallmentAmount = 500M,
            ValidFrom = new DateOnly(2023, 1, 1),
            Status = SchemePlanStatus.Active,
            BenefitRules = new()
            {
                new() { BenefitType = SchemeTerms.BenefitFlatBonus, BenefitValue = 1000M, AppliesOn = "MATURITY", MinInstallmentsPaid = 12 }
            }
        });

        _planGkey = plan.Gkey;
        _import = new SchemeImportService(_context, identity);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Test]
    public async Task Preview_reports_without_writing_anything()
    {
        var result = await _import.ImportAsync(Request(apply: false));

        Assert.Multiple(() =>
        {
            Assert.That(result.Applied, Is.False);
            Assert.That(result.ReadyToImport, Is.EqualTo(3));
            Assert.That(result.Skipped, Is.EqualTo(2));
            Assert.That(_context.CustomerAdvanceAccounts.Count(), Is.Zero);
            Assert.That(_context.AdvanceLedgers.Count(), Is.Zero);
        });
    }

    [Test]
    public async Task Import_creates_scheme_accounts_with_history_and_spreads_catch_up_payments()
    {
        var result = await _import.ImportAsync(Request(apply: true));

        var account = await _context.CustomerAdvanceAccounts.AsNoTracking()
            .SingleAsync(x => x.AccountNbr == "XX-RD-001");
        var enrollment = await _context.SchemeEnrollments.AsNoTracking()
            .Include(x => x.Installments)
            .SingleAsync(x => x.AdvanceAccountGkey == account.Gkey);
        var ledger = await _context.AdvanceLedgers.AsNoTracking()
            .Where(x => x.AdvanceAccountGkey == account.Gkey)
            .OrderBy(x => x.EntryDate)
            .ToListAsync();
        var schedule = enrollment.Installments.OrderBy(x => x.SeqNbr).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result.Imported, Is.EqualTo(3));
            Assert.That(result.AmountImported, Is.EqualTo(5000M + 3000M + 1000M));

            // The legacy RD id is kept as the account number.
            Assert.That(account.AccountType, Is.EqualTo("SCHEME"));
            Assert.That(account.CustGkey, Is.EqualTo(701));
            Assert.That(account.IsOpeningBalance, Is.True);
            Assert.That(account.BalanceAmount, Is.EqualTo(5000M));
            Assert.That(account.TotalReceivedAmount, Is.EqualTo(5000M));

            Assert.That(enrollment.MaturityDate, Is.EqualTo(new DateOnly(2026, 1, 15)));
            Assert.That(enrollment.InstallmentsPaid, Is.EqualTo(5));

            // 1000 + 2000 (catch-up) + 2000 (catch-up) on a 1000 a month scheme.
            Assert.That(schedule.Take(6).Select(x => x.Status),
                Is.EqualTo(new[] { "PAID", "PAID", "PAID", "PAID", "PAID", "PENDING" }));

            // History only: no vouchers, so nothing reaches the day's cash again.
            Assert.That(ledger.All(x => x.VoucherGkey is null && x.EntryType == "RECEIPT"));
            Assert.That(ledger.Select(x => x.SourceRef), Is.EqualTo(new[]
            {
                "LEGACY-RD-XX-RD-001-RDR-0001", "LEGACY-RD-XX-RD-001-RDR-0002", "LEGACY-RD-XX-RD-001-RDR-0003"
            }));
            Assert.That(ledger[1].EntryDate, Is.EqualTo(new DateOnly(2025, 3, 10)));
            Assert.That(ledger[2].Narration, Does.Contain("RDR-0003").And.Contain("GPAY"));
            Assert.That(_context.Vouchers.Count(), Is.Zero);
            Assert.That(_context.CustomerAdvanceAccounts.Count(), Is.EqualTo(3));
        });
    }

    [Test]
    public async Task Two_rds_of_one_customer_become_two_schemes_and_mobile_formats_match()
    {
        await _import.ImportAsync(Request(apply: true));

        var accounts = await _context.CustomerAdvanceAccounts.AsNoTracking()
            .Where(x => x.CustGkey == 702)
            .Select(x => x.AccountNbr)
            .ToListAsync();

        Assert.That(accounts, Is.EquivalentTo(new[] { "XX-RD-002", "XX-RD-003" }));
    }

    [Test]
    public async Task Schemes_with_errors_are_skipped_with_the_reason()
    {
        var result = await _import.ImportAsync(Request(apply: true));

        var noMobile = result.Schemes.Single(x => x.LegacyId == "XX-RD-004");
        var overpaid = result.Schemes.Single(x => x.LegacyId == "XX-RD-005");

        Assert.Multiple(() =>
        {
            Assert.That(noMobile.Outcome, Is.EqualTo(SchemeImportOutcome.Skipped));
            Assert.That(noMobile.IssueText, Does.Contain("Mobile number is missing"));
            Assert.That(overpaid.Outcome, Is.EqualTo(SchemeImportOutcome.Skipped));
            Assert.That(overpaid.IssueText, Does.Contain("more than the"));
            Assert.That(result.GeneralIssues.Single().Message, Does.Contain("XX-RD-999"));
            Assert.That(_context.CustomerAdvanceAccounts.Any(x => x.AccountNbr == "XX-RD-004"), Is.False);
        });
    }

    [Test]
    public async Task Warnings_flag_sheet_mistakes_but_still_import()
    {
        var result = await _import.ImportAsync(Request(apply: false));

        var first = result.Schemes.Single(x => x.LegacyId == "XX-RD-001");
        var second = result.Schemes.Single(x => x.LegacyId == "XX-RD-002");

        Assert.Multiple(() =>
        {
            Assert.That(first.Outcome, Is.EqualTo(SchemeImportOutcome.Ready));
            Assert.That(first.IssueText, Does.Contain("shortened by Excel"));
            Assert.That(first.IssueText, Does.Contain("after the maturity date"));
            Assert.That(second.IssueText, Does.Contain("Maturity amount in the sheet"));
            Assert.That(second.Issues.All(x => x.Severity == SchemeImportSeverity.Warning));
        });
    }

    [Test]
    public async Task Running_the_import_again_skips_rds_already_imported()
    {
        await _import.ImportAsync(Request(apply: true));

        using var againContext = CreateContext();
        var again = await new SchemeImportService(againContext, new AuditIdentityProvider())
            .ImportAsync(Request(apply: true));

        Assert.Multiple(() =>
        {
            Assert.That(again.Imported, Is.Zero);
            Assert.That(again.AlreadyImported, Is.EqualTo(3));
            Assert.That(_context.CustomerAdvanceAccounts.Count(), Is.EqualTo(3));
            // 3 + 2 + 1 payments of the three imported RDs, still once each.
            Assert.That(_context.AdvanceLedgers.Count(), Is.EqualTo(6));
        });
    }

    private SchemeImportRequest Request(bool apply) => new()
    {
        Apply = apply,
        SchemePlanGkey = _planGkey,
        Schemes = new()
        {
            Scheme("XX-RD-001", "9000000101", 1000M, new DateOnly(2025, 1, 15), maturityAmount: 13000M),
            Scheme("XX-RD-002", "9000000102", 2000M, new DateOnly(2026, 6, 1), maturityAmount: 20000M),
            Scheme("XX-RD-003", "9000000102", 1000M, new DateOnly(2026, 6, 1), maturityAmount: 13000M),
            Scheme("XX-RD-004", null, 1000M, new DateOnly(2026, 2, 1), maturityAmount: 13000M),
            Scheme("XX-RD-005", "9000000103", 500M, new DateOnly(2026, 1, 1), maturityAmount: 7000M)
        },
        Payments = new()
        {
            Payment("XX-RD-001", "RDR-0001", new DateOnly(2025, 1, 15), 1000M, "Cash"),
            Payment("XX-RD-001", "RDR-0002", new DateOnly(2025, 3, 10), 2000M, "Cash"),
            Payment("XX-RD-001", "RDR-0003", new DateOnly(2026, 3, 1), 2000M, "GPAY", "6.13506E+11"),
            Payment("XX-RD-002", "RDR-0001", new DateOnly(2026, 6, 1), 2000M, "Cash"),
            Payment("XX-RD-002", "RDR-0002", new DateOnly(2026, 7, 1), 1000M, "Cash"),
            Payment("XX-RD-003", "RDR-0001", new DateOnly(2026, 6, 1), 1000M, "Cash"),
            Payment("XX-RD-004", "RDR-0001", new DateOnly(2026, 2, 1), 1000M, "Cash"),
            Payment("XX-RD-005", "RDR-0001", new DateOnly(2026, 1, 1), 7000M, "Cash"),
            Payment("XX-RD-999", "RDR-0001", new DateOnly(2026, 1, 1), 500M, "Cash")
        }
    };

    private static SchemeImportScheme Scheme(
        string id, string? mobile, decimal monthly, DateOnly start, decimal maturityAmount) => new()
    {
        LegacyId = id,
        MobileNumber = mobile,
        TenureMonths = 12,
        MonthlyAmount = monthly,
        StartDate = start,
        MaturityDate = start.AddMonths(12),
        BonusAmount = 1000M,
        MaturityAmount = maturityAmount,
        Status = "Active"
    };

    private static SchemeImportPayment Payment(
        string id, string receipt, DateOnly paid, decimal amount, string mode, string? tranId = null) => new()
    {
        LegacyId = id,
        ReceiptNumber = receipt,
        PaidDate = paid,
        DueDate = paid,
        Amount = amount,
        Mode = mode,
        TransactionId = tranId
    };

    private MijmsContext CreateContext() =>
        new AdvanceAccountServiceTests.SqliteMijmsContext(new DbContextOptionsBuilder<MijmsContext>()
            .UseSqlite(_connection)
            .Options);
}
