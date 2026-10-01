using System.Reflection;
using DataAccess.Controllers;
using DataAccess.Models;
using DataAccess.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using InvEntry.Contracts.Gst;
using InvEntry.Gst.Core.Classification;
using InvEntry.Gst.Core.Rules;

namespace InvEntry.Test.GST;

public sealed class HistoricalInvoiceAuditTests
{
    private const string Gstin = "29ABCDE1234F1Z5";

    [Test]
    public void Classifier_UsesRequiredCompletionEvidenceWithoutInferringProvenance()
    {
        var completed = HistoricalInvoiceCompletionClassifier.Classify(
            new HistoricalInvoiceCompletionEvidence("B100", 2, true, "29", 1, 1));
        var draft = HistoricalInvoiceCompletionClassifier.Classify(
            new HistoricalInvoiceCompletionEvidence(null, 2, true, "29", 0, 0));
        var conflicting = HistoricalInvoiceCompletionClassifier.Classify(
            new HistoricalInvoiceCompletionEvidence("B101", 2, true, "29", 0, 0));

        Assert.Multiple(() =>
        {
            Assert.That(completed.Classification, Is.EqualTo(HistoricalInvoiceCompletionClassifier.CompletionCandidate));
            Assert.That(draft.Classification, Is.EqualTo(HistoricalInvoiceCompletionClassifier.LikelyGenuineDraft));
            Assert.That(conflicting.Classification, Is.EqualTo(HistoricalInvoiceCompletionClassifier.NeedsInvestigation));
        });
    }

    [Test]
    public async Task Service_FiltersDatesCountsLinksAndDoesNotTrackOrWrite()
    {
        await using var context = CreateContext();
        context.InvoiceHeaders.AddRange(
            CreateInvoice(1, new DateTime(2026, 9, 1), "B100"),
            CreateInvoice(2, new DateTime(2026, 9, 30, 23, 59, 59), "B101"),
            CreateInvoice(3, new DateTime(2026, 10, 1), "B102"));
        context.OrgCustomers.Add(new OrgCustomer { Gkey = 10, CustomerName = "Audit Customer" });
        context.InvoiceLines.AddRange(
            new InvoiceLine { Gkey = 11, InvoiceHdrGkey = 1 },
            new InvoiceLine { Gkey = 12, InvoiceHdrGkey = 1 },
            new InvoiceLine { Gkey = 13, InvoiceHdrGkey = 2 });
        context.Vouchers.AddRange(
            new Voucher { Gkey = 21, RefDocGkey = 1 },
            new Voucher { Gkey = 22, RefDocGkey = 1 });
        context.InvoiceArReceipts.Add(new InvoiceArReceipt { Gkey = 31, InvoiceGkey = 1 });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateService(context);
        var result = await service.GetAsync(Gstin, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30));

        Assert.Multiple(() =>
        {
            Assert.That(result.InvoiceCount, Is.EqualTo(2));
            Assert.That(result.Items.Select(x => x.InvoiceGkey), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(result.Items[0].InvoiceLineCount, Is.EqualTo(2));
            Assert.That(result.Items[0].LinkedVoucherCount, Is.EqualTo(2));
            Assert.That(result.Items[0].LinkedArReceiptCount, Is.EqualTo(1));
            Assert.That(result.Items[0].CompletionEvidenceClassification,
                Is.EqualTo(HistoricalInvoiceCompletionClassifier.CompletionCandidate));
            Assert.That(result.Items.All(x => x.ProductionTestProvenance == "Unknown"), Is.True);
            Assert.That(context.ChangeTracker.Entries(), Is.Empty);
        });
    }

    [Test]
    public async Task Service_MapsTaxableAndComponentTaxesWithoutUsingLineTotalAsTaxTotal()
    {
        await using var context = CreateContext();
        context.InvoiceHeaders.Add(new InvoiceHeader
        {
            Gkey = 100,
            InvNbr = "D-TAX",
            InvDate = new DateTime(2026, 9, 15),
            Status = "FINAL",
            CustGkey = 10,
            GstLocBuyer = "29",
            AmountPayable = 53_000M,
            InvTaxableAmount = 51_996M,
            InvlTaxTotal = 51_996M,
            CgstAmount = 779.94M,
            SgstAmount = 779.94M,
            IgstAmount = 0M
        });
        context.OrgCustomers.Add(new OrgCustomer { Gkey = 10, CustomerName = "Tax Customer" });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateService(context);
        var result = await service.GetAsync(Gstin, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30));
        var item = result.Items.Single();

        Assert.Multiple(() =>
        {
            Assert.That(item.AmountPayable, Is.EqualTo(53_000M));
            Assert.That(item.TaxableAmount, Is.EqualTo(51_996M));
            Assert.That(item.CgstAmount, Is.EqualTo(779.94M));
            Assert.That(item.SgstAmount, Is.EqualTo(779.94M));
            Assert.That(item.IgstAmount, Is.Zero);
            Assert.That(item.TaxTotal, Is.EqualTo(1_559.88M));
            Assert.That(item.TaxTotal, Is.Not.EqualTo(51_996M));
        });

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result));
        Assert.That(json.RootElement.GetProperty("Items")[0].GetProperty("TaxTotal").GetDecimal(),
            Is.EqualTo(1_559.88M));
    }

    [Test]
    public async Task Service_UsesStoredLineTaxableSumForHistoricalZeroHeaderWithGst()
    {
        await using var context = CreateContext();
        context.InvoiceHeaders.Add(new InvoiceHeader
        {
            Gkey = 200,
            InvNbr = "D-HISTORICAL",
            InvDate = new DateTime(2026, 9, 10),
            Status = "DRAFT",
            CustGkey = 10,
            GstLocBuyer = "29",
            AmountPayable = 5_300M,
            InvTaxableAmount = 0M,
            InvlTaxTotal = 5_815.50M,
            CgstAmount = 87.23M,
            SgstAmount = 87.23M,
            DiscountAmount = 690M,
            RoundOff = 0.04M,
            GrossRcbAmount = 5_990M
        });
        context.OrgCustomers.Add(new OrgCustomer { Gkey = 10, CustomerName = "Historical Customer" });
        context.InvoiceLines.Add(new InvoiceLine
        {
            Gkey = 201,
            InvoiceHdrGkey = 200,
            InvlTaxableAmount = 5_815.50M,
            InvlTotal = 5_815.50M,
            InvlCgstAmount = 87.23M,
            InvlSgstAmount = 87.23M
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateService(context);
        var item = (await service.GetAsync(
            Gstin, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30))).Items.Single();

        Assert.Multiple(() =>
        {
            Assert.That(item.TaxableAmount, Is.EqualTo(5_815.50M));
            Assert.That(item.TaxTotal, Is.EqualTo(174.46M));
            Assert.That(item.AmountPayable, Is.EqualTo(5_300M));
            Assert.That(item.TaxableAmount, Is.Not.EqualTo(item.AmountPayable - item.TaxTotal),
                "Discount and round-off prevent amount-minus-tax reconstruction.");
            Assert.That(context.ChangeTracker.Entries(), Is.Empty);
        });
    }

    [Test]
    public async Task Service_ReturnsReadOnlyLineGstAndClassificationDetails_ForHistoricalDraftInvoice()
    {
        await using var context = CreateContext();
        context.InvoiceHeaders.Add(new InvoiceHeader
        {
            Gkey = 300, InvNbr = "D-LINE", InvDate = new DateTime(2026, 9, 20), Status = "DRAFT",
            CustGkey = 10, GstLocBuyer = "29", AmountPayable = 103M, IsTaxApplicable = true
        });
        context.OrgCustomers.Add(new OrgCustomer
        {
            Gkey = 10, CustomerName = "Registered Customer", GstinNbr = "29AAAAA0000A1Z5",
            GstStateCode = "29", CustomerType = "Business"
        });
        context.Products.Add(new Product { Gkey = 20, Name = "Gold Chain", Uom = "Grams", HsnCode = "7113" });
        context.InvoiceLines.Add(new InvoiceLine
        {
            Gkey = 301, InvoiceHdrGkey = 300, ProductGkey = 20, ProductDesc = "Gold Chain",
            HsnCode = "7113", ProdQty = 1, ProdGrossWeight = 10M, ProdNetWeight = 9M,
            InvlTaxableAmount = 100M, InvlCgstPercent = 1.5M, InvlSgstPercent = 1.5M,
            InvlCgstAmount = 1.5M, InvlSgstAmount = 1.5M, TaxAmount = 3M, IsTaxable = true
        });
        context.OldMetalTransactions.Add(new OldMetalTransaction
        {
            Gkey = 401, DocRefGkey = 300, DocRefNbr = "D-LINE", DocRefType = "Sale Invoice",
            TransNbr = "OGP-TEST", TransDate = new DateTime(2026, 9, 20), TransType = "OG Purchase",
            Remarks = "Legacy settlement", Metal = "GOLD", Purity = "916", Uom = "Grams",
            GrossWeight = 0.550M, NetWeight = 0.540M, FinalPurchasePrice = 7_832M
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var item = (await CreateService(context).GetAsync(
            Gstin, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30))).Items.Single();
        var line = item.Lines.Single();

        Assert.Multiple(() =>
        {
            Assert.That(item.CustomerGstin, Is.EqualTo("29AAAAA0000A1Z5"));
            Assert.That(item.SupplierStateCode, Is.EqualTo("29"));
            Assert.That(item.IsRecipientRegistered, Is.True);
            Assert.That(item.SupplyType, Is.EqualTo("IntraState"));
            Assert.That(item.GstReturnCategory, Is.EqualTo("B2B"));
            Assert.That(item.CurrentStatus, Is.EqualTo("DRAFT"));
            Assert.That(item.InvoiceLineCount, Is.EqualTo(1));
            Assert.That(item.Lines, Has.Count.EqualTo(1));
            Assert.That(item.HasOldGoldTransaction, Is.True);
            Assert.That(item.OldGoldTransactions, Has.Count.EqualTo(1));
            Assert.That(line.LineNumber, Is.EqualTo(1));
            Assert.That(line.HsnCode, Is.EqualTo("7113"));
            Assert.That(line.Uom, Is.EqualTo("Grams"));
            Assert.That(line.Uqc, Is.EqualTo("GMS"));
            Assert.That(line.GstRate, Is.EqualTo(3M));
            Assert.That(line.TaxAmount, Is.EqualTo(3M));
            Assert.That(item.OldGoldTransactions[0].DocumentNumber, Is.EqualTo("OGP-TEST"));
            Assert.That(item.OldGoldTransactions[0].TransactionType, Is.EqualTo("OG Purchase"));
            Assert.That(item.OldGoldTransactions[0].Metal, Is.EqualTo("GOLD"));
            Assert.That(item.OldGoldTransactions[0].Purity, Is.EqualTo("916"));
            Assert.That(item.OldGoldTransactions[0].GrossWeight, Is.EqualTo(0.550M));
            Assert.That(item.OldGoldTransactions[0].FinalPurchasePrice, Is.EqualTo(7_832M));
            Assert.That(context.ChangeTracker.Entries(), Is.Empty);
        });

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(
            new HistoricalInvoiceAuditResponse { InvoiceCount = 1, Items = { item } }));
        var serializedLines = json.RootElement.GetProperty("Items")[0].GetProperty("Lines");
        Assert.Multiple(() =>
        {
            Assert.That(serializedLines.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(serializedLines.GetArrayLength(), Is.EqualTo(1));
            Assert.That(serializedLines[0].GetProperty("HsnCode").GetString(), Is.EqualTo("7113"));
            var oldGold = json.RootElement.GetProperty("Items")[0].GetProperty("OldGoldTransactions")[0];
            Assert.That(oldGold.GetProperty("DocumentNumber").GetString(), Is.EqualTo("OGP-TEST"));
            Assert.That(oldGold.TryGetProperty("Gkey", out _), Is.False);
            Assert.That(oldGold.TryGetProperty("InvoiceGkey", out _), Is.False);
        });
    }

    [Test]
    public void ResolveTaxableAmount_PreservesPopulatedHeaderAndDoesNotInventUnsupportedFallbacks()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HistoricalInvoiceAuditService.ResolveTaxableAmount(10_000M, 9_500M, 300M),
                Is.EqualTo(10_000M), "A populated header remains authoritative when lines conflict.");
            Assert.That(HistoricalInvoiceAuditService.ResolveTaxableAmount(0M, 9_500M, 0M),
                Is.Zero, "Line values alone do not establish a GST-taxable fallback.");
            Assert.That(HistoricalInvoiceAuditService.ResolveTaxableAmount(0M, 0M, 300M),
                Is.Zero, "Tax must not be subtracted from invoice totals to invent a taxable value.");
        });
    }

    [Test]
    public void Controller_RequiresAdminAuthorization()
    {
        var authorize = typeof(HistoricalInvoiceAuditController)
            .GetCustomAttribute<AuthorizeAttribute>();

        Assert.Multiple(() =>
        {
            Assert.That(authorize, Is.Not.Null);
            Assert.That(authorize!.Roles, Is.EqualTo("admin"));
        });
    }

    private static TestMijmsContext CreateContext() =>
        new($"historical-audit-{Guid.NewGuid():N}");

    private static HistoricalInvoiceAuditService CreateService(MijmsContext context) =>
        new(context, new StubGstinProvider(), new GstClassificationService(new GstRuleProvider()));

    private static InvoiceHeader CreateInvoice(int gkey, DateTime date, string number) =>
        new()
        {
            Gkey = gkey,
            InvNbr = number,
            InvDate = date,
            Status = "DRAFT",
            CustGkey = 10,
            GstLocBuyer = "29",
            AmountPayable = 100M,
            InvTaxableAmount = 95M,
            InvlTaxTotal = 5M
        };

    private sealed class StubGstinProvider : ICurrentCompanyGstinProvider
    {
        public Task<string?> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(Gstin);
    }

    private sealed class TestMijmsContext(string databaseName) : MijmsContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseInMemoryDatabase(databaseName);
    }
}
