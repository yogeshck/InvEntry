using NUnit.Framework;

namespace DataAccess.Tests;

[TestFixture]
public sealed class InvoiceStatusMigrationScriptTests
{
    private static string ReadMigration()
    {
        var path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..",
            "InvEntry.DB", "Scripts", "Migrations", "2026-10",
            "20261014_NormalizeInvoiceStatusToFinalised.sql"));

        return File.ReadAllText(path);
    }

    [Test]
    public void Migration_IsTransactionalAndTargetsOnlyVerifiedAliases()
    {
        var sql = ReadMigration();

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("SET XACT_ABORT ON"));
            Assert.That(sql, Does.Contain("BEGIN TRANSACTION"));
            Assert.That(sql, Does.Contain("SET STATUS = 'FINALISED'"));
            Assert.That(sql, Does.Contain("IN ('FINAL', 'FINALIZED')"));
            Assert.That(sql, Does.Not.Contain("STATUS = 'VOID'"));
        });
    }

    [Test]
    public void Migration_ProvidesOperatorSafePreviewAndPostVerification()
    {
        var sql = ReadMigration();

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("INV_NBR AS INVOICE_NUMBER"));
            Assert.That(sql, Does.Contain("CONVERT(date, INV_DATE) AS INVOICE_DATE"));
            Assert.That(sql, Does.Not.Contain("SELECT GKEY"));
            Assert.That(sql, Does.Contain("Post-migration verification"));
        });
    }
}
