using DataAccess.Controllers;
using DataAccess.Services;
using InvEntry.Contracts.Gst;
using Microsoft.AspNetCore.Authorization;

namespace InvEntry.Test.GST;

[TestFixture]
public sealed class HistoricalMigrationPreviewTests
{
    [TestCase(true, 100, "Completion candidate", false, "AlreadyStaged")]
    [TestCase(false, -1, "Completion candidate", false, "CancelledPendingReview")]
    [TestCase(false, 100, "Likely genuine draft", false, "Incomplete")]
    [TestCase(false, 0, "Completion candidate", false, "NeedsInvestigation")]
    public void SafetyPrecedence(bool staged, decimal payable, string completion, bool oldGold, string expected)
    {
        var item = Valid(); item.HasGstStagingDocument = staged; item.AmountPayable = payable;
        item.CompletionEvidenceClassification = completion; item.HasOldGoldTransaction = oldGold;
        Assert.That(HistoricalMigrationPreviewService.Assess(item), Is.EqualTo(expected));
    }

    [Test]
    public void ValidHistoricalDraftCanQualifyWithoutStatusChange()
    {
        var item = Valid(); item.CurrentStatus = "DRAFT";
        Assert.That(HistoricalMigrationPreviewService.Assess(item), Is.EqualTo("QualifiedForStaging"));
        Assert.That(item.CurrentStatus, Is.EqualTo("DRAFT"));
    }

    [Test]
    public void ActionableAndReviewStatesAreExcluded()
    {
        var correction = Valid(); correction.Lines[0].HsnCode = null;
        var historical = Valid(); historical.Lines[0].GstRate = historical.Lines[0].CgstRate = historical.Lines[0].SgstRate = 0;
        historical.Lines[0].CgstAmount = historical.Lines[0].SgstAmount = 0;
        var auditor = Valid(); auditor.HasOldGoldTransaction = true; auditor.Lines[0].CgstAmount = 0;
        Assert.Multiple(() =>
        {
            Assert.That(HistoricalMigrationPreviewService.Assess(correction), Is.EqualTo("NeedsGstCorrection"));
            Assert.That(HistoricalMigrationPreviewService.Assess(historical), Is.EqualTo("HistoricalGstReview"));
            Assert.That(HistoricalMigrationPreviewService.Assess(auditor), Is.EqualTo("AuditorReview"));
        });
    }

    [Test]
    public void ControllerRemainsAdminOnly() =>
        Assert.That(typeof(HistoricalMigrationPreviewController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Single().Roles, Is.EqualTo("admin"));

    private static HistoricalInvoiceAuditItemResponse Valid() => new()
    {
        InvoiceNbr = "D-TEST", InvoiceDate = new DateTime(2026, 9, 1), CurrentStatus = "DRAFT",
        AmountPayable = 103, TaxableAmount = 100, TaxTotal = 3, CgstAmount = 1.5m, SgstAmount = 1.5m,
        PlaceOfSupply = "29", CompletionEvidenceClassification = "Completion candidate",
        IsTaxApplicable = true, GstClassificationValid = true, InvoiceLineCount = 1,
        Lines = [new HistoricalInvoiceAuditLineResponse { LineNumber = 1, HsnCode = "7113", Uom = "Grams", Uqc = "GMS",
            TaxableValue = 100, GstRate = 3, CgstRate = 1.5m, SgstRate = 1.5m,
            CgstAmount = 1.5m, SgstAmount = 1.5m, TaxAmount = 3 }]
    };
}
