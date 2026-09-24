using InvEntry.Contracts.Gst;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.ViewModels;

namespace InvEntry.Tests.GST;

[TestFixture]
public class Gstr1MasterDetailSelectionTests
{
    [Test]
    public void InitialLoadShowsSummaryAndCategoriesButNoDocumentsOrLines()
    {
        var vm = CreateViewModel(new Reports());

        Assert.That(vm.Summary, Is.Not.Null);
        Assert.That(vm.Categories, Has.Count.EqualTo(2));
        Assert.That(vm.SelectedCategory, Is.Null);
        Assert.That(vm.Documents, Is.Empty);
        Assert.That(vm.SelectedDocument, Is.Null);
        Assert.That(vm.DocumentLines, Is.Empty);
        Assert.That(vm.DocumentsEmptyMessage, Is.EqualTo("Select a category above to view its documents."));
    }

    [Test]
    public void CategorySelectionUsesReturnCategoryAndTableAndReplacesDocuments()
    {
        var vm = CreateViewModel(new Reports());

        vm.SelectedCategory = vm.Categories.Single(x => x.Category == "B2CS");
        Assert.That(vm.Documents.Select(x => x.Gkey), Is.EqualTo(new long[] { 20 }));
        Assert.That(vm.ShowDocumentsEmptyState, Is.False);

        vm.SelectedCategory = vm.Categories.Single(x => x.Category == "B2B");
        Assert.That(vm.Documents.Select(x => x.Gkey), Is.EqualTo(new long[] { 10 }));
        Assert.That(vm.SelectedDocument, Is.Null);
        Assert.That(vm.DocumentLines, Is.Empty);
    }

    [Test]
    public void EmptyCategoryShowsGuidanceAndMonthChangeClearsDependentState()
    {
        var reports = new Reports();
        reports.Summary.Categories.Add(new() { Category = "B2CL", Gstr1Table = "5" });
        var vm = CreateViewModel(reports);

        vm.SelectedCategory = vm.Categories.Single(x => x.Category == "B2CL");
        Assert.That(vm.Documents, Is.Empty);
        Assert.That(vm.DocumentsEmptyMessage, Is.EqualTo("No documents found for the selected category."));

        vm.ReturnMonth = vm.ReturnMonth.AddMonths(1);
        Assert.That(vm.Summary, Is.Null);
        Assert.That(vm.Categories, Is.Empty);
        Assert.That(vm.SelectedCategory, Is.Null);
        Assert.That(vm.Documents, Is.Empty);
        Assert.That(vm.SelectedDocument, Is.Null);
        Assert.That(vm.DocumentLines, Is.Empty);
    }

    [Test]
    public async Task LateDocumentLinesCannotReplaceTheLatestSelection()
    {
        var reports = new Reports();
        var oldLines = new TaskCompletionSource<IReadOnlyList<Gstr1DocumentLineResponse>>();
        reports.LineResponses[10] = oldLines.Task;
        reports.LineResponses[20] = Task.FromResult<IReadOnlyList<Gstr1DocumentLineResponse>>(
            [new() { GstDocumentGkey = 20, LineNbr = 1, HsnCode = "CURRENT" }]);
        var vm = CreateViewModel(reports);

        vm.SelectedCategory = vm.Categories.Single(x => x.Category == "B2B");
        vm.SelectedDocument = vm.Documents.Single();
        vm.SelectedCategory = vm.Categories.Single(x => x.Category == "B2CS");
        vm.SelectedDocument = vm.Documents.Single();
        await Task.Yield();

        oldLines.SetResult([new() { GstDocumentGkey = 10, LineNbr = 1, HsnCode = "STALE" }]);
        await Task.Yield();

        Assert.That(vm.DocumentLines, Has.Count.EqualTo(1));
        Assert.That(vm.DocumentLines[0].GstDocumentGkey, Is.EqualTo(20));
        Assert.That(vm.DocumentLines[0].HsnCode, Is.EqualTo("CURRENT"));
    }
    private static Gstr1ReturnViewModel CreateViewModel(Reports reports) =>
        new(reports, new Company(), null!);

    private sealed class Company : IOrgThisCompanyViewService
    {
        public Task<OrgThisCompanyView> GetOrgThisCompany() =>
            Task.FromResult(new OrgThisCompanyView { GstNbr = "32TEST", CompanyName = "Test" });
    }

    private sealed class Reports : IGstr1ReportService
    {
        public Dictionary<long, Task<IReadOnlyList<Gstr1DocumentLineResponse>>> LineResponses { get; } = [];
        public Gstr1ReturnSummaryResponse Summary { get; } = new()
        {
            Categories =
            [
                new() { Category = "B2B", Gstr1Table = "4A", DocumentCount = 1 },
                new() { Category = "B2CS", Gstr1Table = "7", DocumentCount = 1 }
            ],
            DocumentCount = 2,
            TotalInvoiceValue = 300M
        };

        private readonly IReadOnlyList<Gstr1DocumentResponse> _documents =
        [
            new() { Gkey = 10, DocumentNbr = "B2B-1", ReturnCategory = "B2B", Gstr1Table = "4A" },
            new() { Gkey = 20, DocumentNbr = "B2CS-1", ReturnCategory = "B2CS", Gstr1Table = "7" },
            new() { Gkey = 30, DocumentNbr = "B2B-other-table", ReturnCategory = "B2B", Gstr1Table = "4B" }
        ];

        public Task<Gstr1ReturnSummaryResponse> GetSummaryAsync(string gstin, string period) => Task.FromResult(Summary);
        public Task<IReadOnlyList<Gstr1DocumentResponse>> GetDocumentsAsync(string gstin, string period) => Task.FromResult(_documents);
        public Task<IReadOnlyList<Gstr1DocumentLineResponse>> GetDocumentLinesAsync(long key, string gstin, string period) =>
            LineResponses.TryGetValue(key, out var response)
                ? response
                : Task.FromResult<IReadOnlyList<Gstr1DocumentLineResponse>>([]);
        public Task<Gstr1ValidationResponse> GetValidationAsync(string gstin, string period) => Task.FromResult(new Gstr1ValidationResponse { SupplierGstin = gstin, ReturnPeriod = period });
        public Task<byte[]> GetExportJsonAsync(string gstin, string period) => Task.FromResult(Array.Empty<byte>());
    }
}