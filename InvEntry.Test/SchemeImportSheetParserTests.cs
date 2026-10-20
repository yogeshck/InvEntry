using InvEntry.Helpers;

namespace InvEntry.Test;

[TestFixture]
public sealed class SchemeImportSheetParserTests
{
    // Same headers as the shop's RD register, made-up values, as pasted from Excel.
    private const string Master =
        "RD ID\tMobileNumber\tScheme Duration\tMonthly Amount\tStart Date\tMaturity Date \tBonus Amount\tMaturity Amount \tStatus \n" +
        "XX-RD-002\t9000000101\t12\t2000\t17-Jul-2024\t17-Jul-2025\t1000\t25000\tActive\n" +
        "\t\t\t\t\t\t\t\t\n" +
        "XX-RD-026\t\t12\t1,000\t07-Nov-2025\t07-Nov-2026\t1000\t13000\tActive\n";

    private const string Payments =
        "RD ID\tCustomer\tTran ID\tDue Date\tPaid Date\tAmount Paid\tPayment Mode (Cash / UPI / Bank)\tReceipt Number\n" +
        "XX-RD-002\t9000000101\t\t17-Jul-2024\t17-Jul-2024\t2000\tCash\tRDR-0001\n" +
        "\t\t\t\t\t\t\t\n" +
        "XX-RD-031\t9000000102\t6.13506E+11\t14-May-2026\t15-May-2026\t10000\tGpay\tRDR-0003\n";

    [Test]
    public void Master_sheet_is_read_by_header_names_and_blank_rows_are_skipped()
    {
        var result = SchemeImportSheetParser.ParseSchemes(Master);

        Assert.Multiple(() =>
        {
            Assert.That(result.Problems, Is.Empty);
            Assert.That(result.Rows, Has.Count.EqualTo(2));
            Assert.That(result.Rows[0].LegacyId, Is.EqualTo("XX-RD-002"));
            Assert.That(result.Rows[0].MobileNumber, Is.EqualTo("9000000101"));
            Assert.That(result.Rows[0].TenureMonths, Is.EqualTo(12));
            Assert.That(result.Rows[0].MonthlyAmount, Is.EqualTo(2000M));
            Assert.That(result.Rows[0].StartDate, Is.EqualTo(new DateOnly(2024, 7, 17)));
            Assert.That(result.Rows[0].MaturityDate, Is.EqualTo(new DateOnly(2025, 7, 17)));
            Assert.That(result.Rows[0].MaturityAmount, Is.EqualTo(25000M));
            Assert.That(result.Rows[1].MobileNumber, Is.Null);
            Assert.That(result.Rows[1].MonthlyAmount, Is.EqualTo(1000M));
        });
    }

    [Test]
    public void Payment_ledger_is_read_by_header_names()
    {
        var result = SchemeImportSheetParser.ParsePayments(Payments);

        Assert.Multiple(() =>
        {
            Assert.That(result.Problems, Is.Empty);
            Assert.That(result.Rows, Has.Count.EqualTo(2));
            Assert.That(result.Rows[1].LegacyId, Is.EqualTo("XX-RD-031"));
            Assert.That(result.Rows[1].MobileNumber, Is.EqualTo("9000000102"));
            Assert.That(result.Rows[1].TransactionId, Is.EqualTo("6.13506E+11"));
            Assert.That(result.Rows[1].DueDate, Is.EqualTo(new DateOnly(2026, 5, 14)));
            Assert.That(result.Rows[1].PaidDate, Is.EqualTo(new DateOnly(2026, 5, 15)));
            Assert.That(result.Rows[1].Amount, Is.EqualTo(10000M));
            Assert.That(result.Rows[1].Mode, Is.EqualTo("Gpay"));
            Assert.That(result.Rows[1].ReceiptNumber, Is.EqualTo("RDR-0003"));
        });
    }

    [Test]
    public void Bad_values_and_missing_columns_are_reported()
    {
        var badDate = SchemeImportSheetParser.ParseSchemes(
            "RD ID\tMonthly Amount\tStart Date\nXX-RD-1\tabc\t31/31/2025\n");
        var noReceipt = SchemeImportSheetParser.ParsePayments(
            "RD ID\tPaid Date\tAmount Paid\nXX-RD-1\t01-Jan-2026\t500\n");

        Assert.Multiple(() =>
        {
            Assert.That(badDate.Problems, Has.Some.Contains("is not a number"));
            Assert.That(badDate.Problems, Has.Some.Contains("is not a date"));
            Assert.That(noReceipt.Problems, Has.Some.Contains("'Receipt Number'"));
        });
    }
}
