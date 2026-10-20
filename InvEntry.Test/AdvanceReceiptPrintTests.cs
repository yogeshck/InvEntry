using InvEntry.Contracts.Advances;
using InvEntry.Models;
using InvEntry.Reports;

namespace InvEntry.Test;

[TestFixture]
public sealed class AdvanceReceiptPrintTests
{
    private static readonly OrgThisCompanyView Company = new()
    {
        CompanyName = "MATHA JEWELLERS",
        Tagline = "Trusted since 1985",
        AddressLine1 = "12 Main Road",
        Area = "Town Hall",
        District = "Thrissur",
        State = "Kerala",
        Pincode = 680001,
        ContactNbr1 = "9876543210",
        GstNbr = "32ABCDE1234F1Z5"
    };

    [Test]
    public void Scheme_receipt_fits_on_one_a5_page()
    {
        var report = new AdvanceReceiptPrint(SchemeReceipt(), Company, isReprint: false);

        report.CreateDocument();
        report.ExportToPdf(OutputPath("AdvanceReceiptPrint_scheme.pdf"));

        Assert.That(report.Pages.Count, Is.EqualTo(1));
    }

    [Test]
    public void Advance_reprint_fits_on_one_a5_page()
    {
        var receipt = new AdvanceReceiptPrintDto
        {
            VoucherGkey = 10,
            VoucherNbr = "Adv-00003",
            VoucherDate = new DateTime(2026, 10, 20),
            ReceiptType = AdvanceReceiptType.AdvanceReceipt,
            Mode = "Cash",
            Amount = 25000.50M,
            CustomerName = "KUMAR",
            CustomerMobile = "9841071151",
            AccountNbr = "ADV-000004",
            AccountType = "OPEN",
            BalanceAfter = 110000.50M,
            TotalReceivedToDate = 150000.50M,
            Notes = "Advance for wedding purchase"
        };

        var report = new AdvanceReceiptPrint(receipt, Company, isReprint: true);

        report.CreateDocument();
        report.ExportToPdf(OutputPath("AdvanceReceiptPrint_advance.pdf"));

        Assert.That(report.Pages.Count, Is.EqualTo(1));
    }

    [TestCase(5000, "RUPEES FIVE THOUSAND ONLY")]
    [TestCase(125000.50, "RUPEES ONE LAKH TWENTY FIVE THOUSAND AND FIFTY PAISE ONLY")]
    public void Amount_in_words_includes_paise(decimal amount, string expected)
    {
        Assert.That(
            System.Text.RegularExpressions.Regex.Replace(AdvanceReceiptPrint.AmountInWords(amount), @"\s+", " "),
            Is.EqualTo(expected));
    }

    private static AdvanceReceiptPrintDto SchemeReceipt() => new()
    {
        VoucherGkey = 11,
        VoucherNbr = "RD-00012",
        VoucherDate = new DateTime(2026, 10, 20),
        ReceiptType = AdvanceReceiptType.RecurringDeposit,
        Mode = "GPAY",
        Amount = 5000M,
        CustomerName = "MEENA",
        CustomerMobile = "9400766060",
        AccountNbr = "RD-000007",
        AccountType = "SCHEME",
        BalanceAfter = 15000M,
        TotalReceivedToDate = 15000M,
        Weight = 0.357M,
        MetalRate = 14000M,
        Metal = "GOLD",
        Purity = "916",
        WeightBalanceAfter = 1.071M,
        IsScheme = true,
        PlanCode = "SWARNA12",
        PlanName = "Swarna 12 Gold Plan",
        InstallmentAmount = 5000M,
        TenureMonths = 12,
        InstallmentsCovered = "3",
        InstallmentsPaid = 3,
        NextDueDate = new DateOnly(2026, 11, 20),
        NextDueAmount = 5000M,
        MaturityDate = new DateOnly(2027, 8, 20),
        NomineeName = "ARUN",
        Notes = "October installment"
    };

    private static string OutputPath(string fileName) =>
        Path.Combine(Path.GetTempPath(), fileName);
}
