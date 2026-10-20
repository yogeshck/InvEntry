using InvEntry.Contracts.Schemes;
using InvEntry.Models;
using InvEntry.Reports;

namespace InvEntry.Test;

[TestFixture]
public sealed class RdStatementPrintTests
{
    private static readonly OrgThisCompanyView Company = new()
    {
        CompanyName = "MATHA JEWELLERS",
        Tagline = "Trusted since 1985",
        AddressLine1 = "12 Main Road",
        Area = "Town Hall",
        District = "Thrissur",
        GstCode = "32",
        State = "Kerala",
        ContactNbr1 = "9876543210",
        EmailId = "shop@example.com",
        GstNbr = "32ABCDE1234F1Z5"
    };

    [TestCase(RdStatementPaper.A4, "RdStatement_A4.pdf")]
    [TestCase(RdStatementPaper.A5, "RdStatement_A5.pdf")]
    public void Statement_renders_schedule_and_payments(RdStatementPaper paper, string fileName)
    {
        var report = new RdStatementPrint(Statement(), Company, paper);

        report.CreateDocument();
        report.ExportToPdf(Path.Combine(Path.GetTempPath(), fileName));

        Assert.Multiple(() =>
        {
            Assert.That(report.Pages.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(report.PaperKind, Is.EqualTo(paper == RdStatementPaper.A4
                ? DevExpress.Drawing.Printing.DXPaperKind.A4
                : DevExpress.Drawing.Printing.DXPaperKind.A5));
        });

        if (paper == RdStatementPaper.A4)
            Assert.That(report.Pages.Count, Is.EqualTo(1), "A 12 month RD fits on one A4 page.");
    }

    /// <summary>Same shape as MJ-RD26-022: 1000 a month, imported receipts, one catch-up, 11 of 12 paid.</summary>
    private static RdStatementDto Statement()
    {
        var start = new DateOnly(2025, 9, 21);
        var paidOn = new[]
        {
            new DateOnly(2025, 9, 21), new DateOnly(2025, 11, 15), new DateOnly(2025, 11, 15),
            new DateOnly(2025, 12, 31), new DateOnly(2026, 1, 31), new DateOnly(2026, 2, 27),
            new DateOnly(2026, 3, 26), new DateOnly(2026, 4, 25), new DateOnly(2026, 5, 29),
            new DateOnly(2026, 6, 30), new DateOnly(2026, 7, 29)
        };

        var installments = Enumerable.Range(1, 12).Select(seq => new SchemeInstallmentDto
        {
            Gkey = seq,
            SeqNbr = seq,
            DueDate = start.AddMonths(seq - 1),
            AmountDue = 1000M,
            AmountPaid = seq <= 11 ? 1000M : 0M,
            Status = seq <= 11 ? "PAID" : "PENDING",
            PaidOn = seq <= 11 ? paidOn[seq - 1] : null
        }).ToList();

        var receipts = new (DateOnly Date, string Number, decimal Amount, string Covered)[]
        {
            (new DateOnly(2025, 9, 21), "RDR-0001", 1000M, "1"),
            (new DateOnly(2025, 11, 15), "RDR-0002", 2000M, "2, 3"),
            (new DateOnly(2025, 12, 31), "RDR-0003", 1000M, "4"),
            (new DateOnly(2026, 1, 31), "RDR-0004", 1000M, "5"),
            (new DateOnly(2026, 2, 27), "RDR-0005", 1000M, "6"),
            (new DateOnly(2026, 3, 26), "RDR-0006", 1000M, "7"),
            (new DateOnly(2026, 4, 25), "RDR-0007", 1000M, "8"),
            (new DateOnly(2026, 5, 29), "RDR-0008", 1000M, "9"),
            (new DateOnly(2026, 6, 30), "RDR-0009", 1000M, "10"),
            (new DateOnly(2026, 7, 29), "RDR-0010", 1000M, "11")
        };

        var balance = 0M;
        var payments = receipts.Select(x =>
        {
            balance += x.Amount;
            return new RdStatementPayment
            {
                Date = x.Date,
                ReceiptNumber = x.Number,
                Mode = "Cash",
                EntryType = "RECEIPT",
                Amount = x.Amount,
                InstallmentsCovered = x.Covered,
                RunningBalance = balance,
                IsImported = true
            };
        }).ToList();

        return new RdStatementDto
        {
            Scheme = new SchemeEnrollmentDto
            {
                AccountNbr = "XX-RD26-022",
                CustomerName = "SAMPLE CUSTOMER",
                CustomerMobile = "9000000022",
                PlanCode = "MJRD12",
                PlanName = "12 Month RD",
                SchemeType = SchemeType.Amount,
                AmountFixed = true,
                Status = "ACTIVE",
                InstallmentAmount = 1000M,
                StartDate = start,
                MaturityDate = start.AddMonths(12),
                TenureMonths = 12,
                InstallmentsPaid = 11,
                TotalDue = 12000M,
                TotalPaid = 11000M,
                BalanceAmount = 11000M,
                NextInstallment = installments[11],
                NomineeName = "SAMPLE NOMINEE",
                NomineeRelation = "Son",
                Installments = installments
            },
            Payments = payments,
            BonusAmount = 1000M,
            BonusMinInstallments = 12,
            BonusEarned = false,
            MaturityValue = 11000M,
            GeneratedOn = new DateTime(2026, 10, 20, 15, 30, 0)
        };
    }
}
