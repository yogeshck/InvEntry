using DataAccess.Models;
using DataAccess.Services;
using InvEntry.Contracts.Gst;
using InvEntry.Services;
using InvEntry.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace InvEntry.Test.GST;

[TestFixture]
public sealed class Gstr1LineCorrectionTests
{
    private const string Gstin = "32ABCDE1234F1Z5";
    private const string Period = "202609";

    // =========================================================
    // UQC MAPPING
    // =========================================================

    [TestCase("Grams", "GMS")]
    [TestCase(" grams ", "GMS")]
    [TestCase("Pcs", "PCS")]
    [TestCase("Kg", "KGS")]
    [TestCase("Carat", null)]
    [TestCase(null, null)]
    public void UomMapsOnlyToUnambiguousUqc(string? uom, string? expected) =>
        Assert.That(GstUqc.MapFromUom(uom), Is.EqualTo(expected));

    [Test]
    public void MismatchIsOnlyReportedForMappedUoms()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GstUqc.IsMismatch("Grams", "NOS"), Is.True);
            Assert.That(GstUqc.IsMismatch("Grams", "gms"), Is.False);
            Assert.That(GstUqc.IsMismatch("Carat", "NOS"), Is.False);
            Assert.That(GstUqc.IsMismatch("Grams", null), Is.False);
        });
    }

    [Test]
    public void GstQuantityUsesNetWeightForWeightUnitsAndQuantityOtherwise()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GstUqc.SuggestGstQuantity("GMS", 1M, 8.456M), Is.EqualTo(8.456M));
            Assert.That(GstUqc.SuggestGstQuantity("KGS", 1M, 1500M), Is.EqualTo(1.5M));
            Assert.That(GstUqc.SuggestGstQuantity("PCS", 2M, 8.456M), Is.EqualTo(2M));
            Assert.That(GstUqc.SuggestGstQuantity("GMS", 1M, 0M), Is.Null);
        });
    }

    // =========================================================
    // SERVICE
    // =========================================================

    [Test]
    public async Task GetSuggestsUqcAndQuantityFromProductAndInvoiceLine()
    {
        await using var context = await SeedAsync(stagedUom: null, stagedUqc: null);

        var line = await new Gstr1LineCorrectionService(context)
            .GetAsync(100, 1, Gstin, Period);

        Assert.That(line, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(line!.ProductUom, Is.EqualTo("Grams"));
            Assert.That(line.SuggestedUom, Is.EqualTo("Grams"));
            Assert.That(line.SuggestedUqc, Is.EqualTo("GMS"));
            Assert.That(line.SuggestedGstQuantity, Is.EqualTo(9.250M));
            Assert.That(line.SuggestedHsnCode, Is.EqualTo("7113"));
        });
    }

    [Test]
    public async Task GetFlagsStagedMismatch()
    {
        await using var context = await SeedAsync(stagedUom: "Grams", stagedUqc: "NOS");

        var line = await new Gstr1LineCorrectionService(context)
            .GetAsync(100, 1, Gstin, Period);

        Assert.That(line!.SuggestedUqc, Is.EqualTo("GMS"));
        Assert.That(line.SuggestionNote, Does.Contain("does not match"));
    }

    [Test]
    public async Task OutOfScopeLineIsNotFound()
    {
        await using var context = await SeedAsync(stagedUom: null, stagedUqc: null);
        var service = new Gstr1LineCorrectionService(context);

        Assert.Multiple(async () =>
        {
            Assert.That(await service.GetAsync(100, 9, Gstin, Period), Is.Null);
            Assert.That(await service.GetAsync(100, 1, Gstin, "202610"), Is.Null);
        });
    }

    [Test]
    public async Task SaveUpdatesOnlyGstReportingFields()
    {
        await using var context = await SeedAsync(stagedUom: null, stagedUqc: null);

        var saved = await new Gstr1LineCorrectionService(context).SaveAsync(100, 1, new()
        {
            SupplierGstin = Gstin,
            ReturnPeriod = Period,
            HsnCode = "711319",
            Uom = "Grams",
            Uqc = "gms",
            GstQuantity = 9.25M
        });

        context.ChangeTracker.Clear();
        var line = await context.GstGstr1DocumentLines.SingleAsync();

        Assert.That(saved, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(line.HsnCode, Is.EqualTo("711319"));
            Assert.That(line.Uom, Is.EqualTo("Grams"));
            Assert.That(line.Uqc, Is.EqualTo("GMS"));
            Assert.That(line.GstQuantity, Is.EqualTo(9.25M));
            Assert.That(line.TaxableValue, Is.EqualTo(50000M));
            Assert.That(line.CgstAmount, Is.EqualTo(750M));
        });
    }

    [TestCase("71A3", "Grams", "GMS", 1.0, "HSN code must be 4, 6 or 8 digits.")]
    [TestCase("7113", "", "GMS", 1.0, "UOM is required.")]
    [TestCase("7113", "Grams", "XYZ", 1.0, "not a valid GST UQC")]
    [TestCase("7113", "Grams", "NOS", 1.0, "must be reported as UQC 'GMS'")]
    [TestCase("7113", "Grams", "GMS", 0.0, "GST quantity must be greater than zero.")]
    [TestCase("7113", "Grams", "GMS", 1.2345, "more than 3 decimal places")]
    public async Task SaveRejectsInvalidCorrectionWithoutWriting(
        string hsn, string uom, string uqc, double quantity, string expected)
    {
        await using var context = await SeedAsync(stagedUom: null, stagedUqc: null);

        var error = Assert.ThrowsAsync<ArgumentException>(() =>
            new Gstr1LineCorrectionService(context).SaveAsync(100, 1, new()
            {
                SupplierGstin = Gstin,
                ReturnPeriod = Period,
                HsnCode = hsn,
                Uom = uom,
                Uqc = uqc,
                GstQuantity = (decimal)quantity
            }));

        context.ChangeTracker.Clear();
        var line = await context.GstGstr1DocumentLines.SingleAsync();

        Assert.That(error!.Message, Does.Contain(expected));
        Assert.That(line.Uqc, Is.Null);
    }

    [Test]
    public async Task ValidationWarnsOnUomUqcMismatch()
    {
        await using var context = await SeedAsync(stagedUom: "Grams", stagedUqc: "NOS");

        var validation = new Gstr1ValidationService(context, null!, null!, null!);
        var response = new Gstr1ValidationResponse();
        var document = await context.GstGstr1Documents.Include(x => x.GstGstr1DocumentLines).SingleAsync();

        typeof(Gstr1ValidationService)
            .GetMethod("ValidateLines", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, [document, response]);

        var issue = response.Issues.Single(x => x.Code == "GST-LINE-008");
        Assert.That(issue.Severity, Is.EqualTo("Warning"));
        Assert.That(issue.LineNbr, Is.EqualTo(1));
        Assert.That(Gstr1ValidationIssueRow.IsCorrectableIssue(issue), Is.True);
    }

    // =========================================================
    // POPUP VIEW MODEL
    // =========================================================

    [Test]
    public async Task PopupStartsFromSuggestionAndChangingUomUpdatesUqcAndQuantity()
    {
        var reports = new Reports();
        var vm = new Gstr1LineCorrectionViewModel(reports);

        await vm.InitializeAsync(Issue(), Gstin, Period);

        Assert.Multiple(() =>
        {
            Assert.That(vm.Uqc, Is.EqualTo("GMS"));
            Assert.That(vm.GstQuantity, Is.EqualTo(9.25M));
            Assert.That(vm.HasChanges, Is.True);
        });

        vm.Uom = "Pieces";

        Assert.Multiple(() =>
        {
            Assert.That(vm.Uqc, Is.EqualTo("PCS"));
            Assert.That(vm.GstQuantity, Is.EqualTo(1M));
        });
    }

    [Test]
    public async Task PopupDoesNotOverwriteTypedQuantityWhenUqcChanges()
    {
        var vm = new Gstr1LineCorrectionViewModel(new Reports());
        await vm.InitializeAsync(Issue(), Gstin, Period);

        vm.GstQuantity = 7.5M;
        vm.Uqc = "KGS";

        Assert.That(vm.GstQuantity, Is.EqualTo(7.5M));
    }

    [Test]
    public async Task PopupBlocksInvalidSaveAndSavesValidCorrection()
    {
        var reports = new Reports();
        var vm = new Gstr1LineCorrectionViewModel(reports);
        var saved = false;
        vm.Saved += (_, _) => saved = true;
        await vm.InitializeAsync(Issue(), Gstin, Period);

        vm.Uqc = "NOS";
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.That(reports.SavedRequest, Is.Null);
        Assert.That(vm.ErrorMessage, Does.Contain("must be reported as UQC 'GMS'"));

        vm.Uqc = "GMS";
        vm.GstQuantity = 9.25M;
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(saved, Is.True);
            Assert.That(vm.WasSaved, Is.True);
            Assert.That(vm.ErrorMessage, Is.Null);
            Assert.That(reports.SavedRequest!.Uqc, Is.EqualTo("GMS"));
            Assert.That(reports.SavedRequest.SupplierGstin, Is.EqualTo(Gstin));
        });
    }

    [Test]
    public async Task PopupShowsServerErrorMessage()
    {
        var reports = new Reports
        {
            SaveError = new HttpRequestException(
                "PUT 'api/x' failed: BadRequest - {\"error\":\"HSN code must be 4, 6 or 8 digits.\"}")
        };
        var vm = new Gstr1LineCorrectionViewModel(reports);
        await vm.InitializeAsync(Issue(), Gstin, Period);

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.That(vm.ErrorMessage, Is.EqualTo("HSN code must be 4, 6 or 8 digits."));
        Assert.That(vm.WasSaved, Is.False);
    }

    [Test]
    public void OnlyLineLevelReportingIssuesAreCorrectable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Gstr1ValidationIssueRow.IsCorrectableIssue(Issue()), Is.True);
            Assert.That(Gstr1ValidationIssueRow.IsCorrectableIssue(
                new() { Code = "GST-LINE-005", DocumentGkey = 100 }), Is.False);
            Assert.That(Gstr1ValidationIssueRow.IsCorrectableIssue(
                new() { Code = "GST-REC-001", DocumentGkey = 100, LineNbr = 1 }), Is.False);
        });
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static Gstr1ValidationIssueResponse Issue() => new()
    {
        Severity = "Error",
        Code = "GST-LINE-005",
        Message = "GST UQC is required for reportable line 1.",
        DocumentGkey = 100,
        DocumentNbr = "INV-1",
        LineNbr = 1
    };

    private static async Task<TestMijmsContext> SeedAsync(string? stagedUom, string? stagedUqc)
    {
        var context = new TestMijmsContext(Guid.NewGuid().ToString());

        context.Products.Add(new Product { Gkey = 7, Name = "Gold Chain", Uom = "Grams", HsnCode = "7113" });
        context.InvoiceLines.Add(new InvoiceLine
        {
            Gkey = 55,
            InvoiceHdrGkey = 1,
            ProductGkey = 7,
            ProductName = "Gold Chain",
            ProdQty = 1,
            ProdNetWeight = 9.250M,
            ProdGrossWeight = 9.500M
        });
        context.GstGstr1Documents.Add(new GstGstr1Document
        {
            Gkey = 100,
            SourceGkey = 1,
            DocumentType = "SalesInvoice",
            DocumentNbr = "INV-1",
            DocumentDate = new DateOnly(2026, 9, 10),
            SupplierGstin = Gstin,
            ReturnPeriod = Period,
            PlaceOfSupplyCode = "32",
            SupplyType = "IntraState",
            TaxType = "CgstSgst",
            ReturnCategory = "B2CS",
            IsReportable = true,
            Status = "Staged",
            TaxableValue = 50000M,
            CgstAmount = 750M,
            SgstAmount = 750M,
            GstGstr1DocumentLines =
            {
                new GstGstr1DocumentLine
                {
                    Gkey = 1000,
                    LineNbr = 1,
                    SourceLineGkey = 55,
                    Quantity = 1M,
                    TaxableValue = 50000M,
                    CgstAmount = 750M,
                    SgstAmount = 750M,
                    Uom = stagedUom,
                    Uqc = stagedUqc
                }
            }
        });

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return context;
    }

    private sealed class TestMijmsContext(string databaseName) : MijmsContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseInMemoryDatabase(databaseName);
    }

    private sealed class Reports : IGstr1ReportService
    {
        public Gstr1LineCorrectionRequest? SavedRequest { get; private set; }
        public Exception? SaveError { get; init; }

        private static Gstr1LineCorrectionResponse Line() => new()
        {
            DocumentGkey = 100,
            DocumentNbr = "INV-1",
            LineNbr = 1,
            Quantity = 1M,
            NetWeight = 9.25M,
            ProductUom = "Grams",
            HsnCode = "7113",
            SuggestedHsnCode = "7113",
            SuggestedUom = "Grams",
            SuggestedUqc = "GMS",
            SuggestedGstQuantity = 9.25M
        };

        public Task<Gstr1LineCorrectionResponse> GetLineCorrectionAsync(long key, int line, string gstin, string period) =>
            Task.FromResult(Line());

        public Task<Gstr1LineCorrectionResponse> SaveLineCorrectionAsync(long key, int line, Gstr1LineCorrectionRequest request)
        {
            if (SaveError is not null)
                return Task.FromException<Gstr1LineCorrectionResponse>(SaveError);

            SavedRequest = request;
            var saved = Line();
            saved.HsnCode = request.HsnCode;
            saved.Uom = request.Uom;
            saved.Uqc = request.Uqc;
            saved.GstQuantity = request.GstQuantity;
            return Task.FromResult(saved);
        }

        public Task<Gstr1ValidationResponse> GetValidationAsync(string gstin, string period) => throw new NotSupportedException();
        public Task<byte[]> GetExportJsonAsync(string gstin, string period) => throw new NotSupportedException();
        public Task<Gstr1ReturnSummaryResponse> GetSummaryAsync(string gstin, string period) => throw new NotSupportedException();
        public Task<IReadOnlyList<Gstr1DocumentResponse>> GetDocumentsAsync(string gstin, string period) => throw new NotSupportedException();
        public Task<IReadOnlyList<Gstr1DocumentLineResponse>> GetDocumentLinesAsync(long key, string gstin, string period) => throw new NotSupportedException();
    }
}
