using DevExpress.Mvvm;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Services.Printing;
using InvEntry.Utils;
using InvEntry.ViewModels;
using System.Collections.Concurrent;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace InvEntry.Test;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
public class BarcodeMaintenanceTests
{
    [Test]
    public async Task Reprint_PreservesStockIdentityAndInventoryAndUsesExistingSku()
    {
        var printer = new RecordingPrinter();
        var stockService = new RecordingStockService();
        var stock = ValidStock();
        var before = Snapshot(stock);
        var viewModel = await CreateViewModel(stockService, printer: printer);

        await viewModel.ReprintTagCommand.ExecuteAsync(stock);

        Assert.Multiple(() =>
        {
            Assert.That(printer.CallCount, Is.EqualTo(1));
            Assert.That(printer.LastRequest?.ProductCode, Is.EqualTo(stock.ProductSku));
            Assert.That(stock.GKey, Is.EqualTo(2608));
            Assert.That(Snapshot(stock), Is.EqualTo(before));
            Assert.That(viewModel.SelectedGridLine, Is.SameAs(stock));
            Assert.That(stockService.CreateCalls, Is.Zero);
            Assert.That(stockService.UpdateCalls, Is.Zero);
            Assert.That(stockService.ReserveCalls, Is.Zero);
        });
    }

    [Test]
    public async Task Preview_UsesProductionRendererWithoutCallingPrinterOrInventoryWrites()
    {
        var printer = new RecordingPrinter();
        var stockService = new RecordingStockService();
        var renderer = new RecordingPreviewRenderer();
        var stock = ValidStock();
        var before = Snapshot(stock);
        var viewModel = await CreateViewModel(stockService, printer: printer, renderer: renderer);

        await viewModel.PreviewTagCommand.ExecuteAsync(stock);

        string expected = BarCodePrint.CreateZpl(
            "GBL2-0176", "Gold Bangle", 12.5m, 10.333m, 0.111m, "916", "MATHA");
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasLabelPreview, Is.True);
            Assert.That(viewModel.LabelPreviewZpl, Is.EqualTo(expected));
            Assert.That(renderer.CallCount, Is.EqualTo(1));
            Assert.That(printer.CallCount, Is.Zero);
            Assert.That(stockService.TotalMutationCalls, Is.Zero);
            Assert.That(Snapshot(stock), Is.EqualTo(before));
        });
    }

    [TestCase(SimulatedPrintOutcome.Success, "Simulated print success")]
    [TestCase(SimulatedPrintOutcome.Failure, "Simulated label printing failure")]
    [TestCase(SimulatedPrintOutcome.Exception, "Simulated label printer is unavailable")]
    public async Task SimulationOutcomes_KeepSelectedStockAndNeverMutateInventory(
        SimulatedPrintOutcome outcome, string expectedStatus)
    {
        var stockService = new RecordingStockService();
        var stock = ValidStock();
        var before = Snapshot(stock);
        var printer = new SimulatedLabelPrinter { Outcome = outcome };
        var viewModel = await CreateViewModel(stockService, printer: printer);

        await viewModel.ReprintTagCommand.ExecuteAsync(stock);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.MaintenanceStatus, Does.Contain(expectedStatus));
            Assert.That(viewModel.SelectedGridLine, Is.SameAs(stock));
            Assert.That(stockService.TotalMutationCalls, Is.Zero);
            Assert.That(Snapshot(stock), Is.EqualTo(before));
        });
    }

    [Test]
    public async Task DuplicateSkuLookup_DisplaysEveryMatchAndDoesNotChooseOne()
    {
        var first = ValidStock();
        var second = ValidStock();
        second.GKey = 2609;
        second.Status = "InActive";
        var stockService = new RecordingStockService { CategoryStock = new[] { first, second } };
        var viewModel = await CreateViewModel(stockService);
        viewModel.SelectedCategory = "BANGLE";
        viewModel.SelectedProductSku = "GBL2-0176";

        await viewModel.RefreshBarcodeCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.ProductStockList.Select(x => x.GKey), Is.EqualTo(new[] { 2608, 2609 }));
            Assert.That(viewModel.ProductStockList.All(x => x.DuplicateFlag == "D"), Is.True);
            Assert.That(viewModel.SelectedGridLine, Is.Null);
            Assert.That(viewModel.MaintenanceStatus, Does.Contain("Select the required ProductStock GKey"));
        });
    }

    [TestCase(true, false, "Associated PRODUCT GKey")]
    [TestCase(false, true, "Company details are unavailable")]
    public async Task MissingProductOrCompany_IsHandledBeforePrinting(
        bool missingProduct, bool missingCompany, string expected)
    {
        var printer = new RecordingPrinter();
        var viewModel = await CreateViewModel(
            new RecordingStockService(), printer: printer,
            missingProduct: missingProduct, missingCompany: missingCompany);

        await viewModel.ReprintTagCommand.ExecuteAsync(ValidStock());

        Assert.Multiple(() =>
        {
            Assert.That(printer.CallCount, Is.Zero);
            Assert.That(viewModel.MaintenanceStatus, Does.Contain(expected));
        });
    }

    [Test]
    public async Task DoubleClick_SubmitsOnlyOnePrintRequest()
    {
        var printer = new RecordingPrinter { Hold = true };
        var stock = ValidStock();
        var viewModel = await CreateViewModel(new RecordingStockService(), printer: printer);

        Task first = viewModel.ReprintTagCommand.ExecuteAsync(stock);
        await printer.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Task second = viewModel.ReprintTagCommand.ExecuteAsync(stock);
        printer.Release.TrySetResult();
        await Task.WhenAll(first, second);

        Assert.That(printer.CallCount, Is.EqualTo(1));
    }

    [Test]
    public void ReprintSource_HasNoInventoryOrSkuReassignmentCalls()
    {
        string source = File.ReadAllText(FindRepositoryFile("InvEntry", "ViewModels", "BarCodeTagListViewModel.cs"));
        int start = source.IndexOf("private async Task ReprintTagAsync", StringComparison.Ordinal);
        int end = source.IndexOf("private async Task<LabelPrintRequest>", start, StringComparison.Ordinal);
        string method = source[start..end];
        string xaml = File.ReadAllText(FindRepositoryFile("InvEntry", "Views", "BarCodeTagListView.xaml"));

        Assert.Multiple(() =>
        {
            Assert.That(method, Does.Not.Contain("CreateProductStock"));
            Assert.That(method, Does.Not.Contain("UpdateProductStock"));
            Assert.That(method, Does.Not.Contain("UpdateReference"));
            Assert.That(method, Does.Not.Contain("ReserveProductSku"));
            Assert.That(method, Does.Not.Contain("ProcessBarCode"));
            Assert.That(method, Does.Contain("_labelPrinter.PrintAsync"));
            Assert.That(xaml, Does.Not.Contain("IsReAssign"));
            Assert.That(xaml, Does.Contain("PrintGridBehavior"));
            Assert.That(xaml, Does.Contain("JewelleryTagPreview"));
        });
    }

    [Test]
    public async Task PendingInitialTagging_IsRecognizedFromGrnAssociationAndCannotPreviewOrReprint()
    {
        var printer = new RecordingPrinter();
        var stockService = new RecordingStockService();
        var pending = PendingStock();
        var before = Snapshot(pending);
        var viewModel = await CreateViewModel(stockService, printer: printer);

        viewModel.SelectedGridLine = pending;

        BarcodeMaintenanceEligibility eligibility = BarcodeMaintenanceEligibilityEvaluator.Evaluate(pending);
        Assert.Multiple(() =>
        {
            Assert.That(eligibility.State, Is.EqualTo(BarcodeMaintenanceEligibilityState.PendingInitialTagging));
            Assert.That(viewModel.PreviewStatusHeading, Is.EqualTo("Initial weighing and tagging required"));
            Assert.That(viewModel.PreviewStatusMessage, Is.EqualTo("Complete this item through GRN Weighing & Tagging before using Barcode Maintenance."));
            Assert.That(viewModel.PreviewTagCommand.CanExecute(pending), Is.False);
            Assert.That(viewModel.ReprintTagCommand.CanExecute(pending), Is.False);
            Assert.That(viewModel.SelectedGridLine, Is.SameAs(pending));
            Assert.That(viewModel.HasLabelPreview, Is.False);
            Assert.That(printer.CallCount, Is.Zero);
            Assert.That(stockService.TotalMutationCalls, Is.Zero);
            Assert.That(Snapshot(pending), Is.EqualTo(before));
        });
    }

    [Test]
    public void CompletedValidStock_IsReadyForReprint()
    {
        BarcodeMaintenanceEligibility eligibility = BarcodeMaintenanceEligibilityEvaluator.Evaluate(ValidStock());

        Assert.Multiple(() =>
        {
            Assert.That(eligibility.State, Is.EqualTo(BarcodeMaintenanceEligibilityState.ReadyForReprint));
            Assert.That(eligibility.CanPreviewOrReprint, Is.True);
        });
    }

    [Test]
    public async Task CompletedStockWithMissingWeights_ShowsIntegrityWarningAndDisablesActions()
    {
        ProductStock stock = ValidStock();
        stock.GrossWeight = null;
        stock.NetWeight = null;
        var viewModel = await CreateViewModel(new RecordingStockService());

        viewModel.SelectedGridLine = stock;

        Assert.Multiple(() =>
        {
            Assert.That(BarcodeMaintenanceEligibilityEvaluator.Evaluate(stock).State,
                Is.EqualTo(BarcodeMaintenanceEligibilityState.InvalidStockData));
            Assert.That(viewModel.PreviewStatusHeading, Is.EqualTo("Tag preview unavailable"));
            Assert.That(viewModel.PreviewStatusMessage, Does.Contain("missing or invalid weight data"));
            Assert.That(viewModel.PreviewTagCommand.CanExecute(stock), Is.False);
            Assert.That(viewModel.ReprintTagCommand.CanExecute(stock), Is.False);
            Assert.That(viewModel.SelectedGridLine, Is.SameAs(stock));
        });
    }

    [Test]
    public async Task PreviewValidationFailure_ClearsStaleContentAndPreservesSelection()
    {
        ProductStock stock = ValidStock();
        var viewModel = await CreateViewModel(new RecordingStockService(), missingProduct: true);
        viewModel.SelectedGridLine = stock;
        viewModel.LabelPreviewZpl = "stale zpl";
        viewModel.LabelPreviewImage = new BitmapImage();
        viewModel.HasLabelPreview = true;

        await viewModel.PreviewTagCommand.ExecuteAsync(stock);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.SelectedGridLine, Is.SameAs(stock));
            Assert.That(viewModel.HasLabelPreview, Is.False);
            Assert.That(viewModel.LabelPreviewZpl, Is.Null);
            Assert.That(viewModel.LabelPreviewImage, Is.Null);
            Assert.That(viewModel.PreviewStatusMessage, Does.Contain("Associated PRODUCT GKey"));
        });
    }
    [Test]
    public async Task PreviewPanelState_TransitionsBetweenNoSelectionValidAndPendingWithoutStaleContent()
    {
        var viewModel = await CreateViewModel(new RecordingStockService());
        ProductStock valid = ValidStock();
        ProductStock pending = PendingStock();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasLabelPreview, Is.False);
            Assert.That(viewModel.PreviewStatusMessage, Is.EqualTo("Select a stock record to preview its tag."));
        });

        await viewModel.PreviewTagCommand.ExecuteAsync(valid);
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasLabelPreview, Is.True);
            Assert.That(viewModel.PreviewStatusHeading, Is.Null);
            Assert.That(viewModel.PreviewStatusMessage, Is.Null);
        });

        viewModel.SelectedGridLine = pending;
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasLabelPreview, Is.False);
            Assert.That(viewModel.LabelPreviewImage, Is.Null);
            Assert.That(viewModel.LabelPreviewZpl, Is.Null);
            Assert.That(viewModel.PreviewStatusHeading, Is.EqualTo("Initial weighing and tagging required"));
            Assert.That(viewModel.PreviewStatusMessage, Does.Contain("GRN Weighing & Tagging"));
        });

        viewModel.SelectedGridLine = valid;
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasLabelPreview, Is.False);
            Assert.That(viewModel.PreviewStatusHeading, Is.EqualTo("Tag preview unavailable"));
            Assert.That(viewModel.PreviewStatusMessage, Does.Contain("Ready to preview or reprint"));
        });
    }
    [Test]
    public async Task LabelResolution_UsesProductGkeyAndExactStockSummaryWhileKeepingIndividualSkuAndWeights()
    {
        ProductStock first = ValidStock();
        first.GKey = 2651;
        first.ProductSku = "GBL2-0180";
        first.GrossWeight = 8.250m;
        first.StoneWeight = 0.125m;
        first.NetWeight = 8.125m;
        first.VaPercent = 9.5m;

        ProductStock second = ValidStock();
        second.GKey = 2652;
        second.ProductGkey = 202;
        second.StockSummaryGkey = 22;
        second.ProductSku = "GBL2-0181";
        second.GrossWeight = 6.750m;
        second.StoneWeight = 0.250m;
        second.NetWeight = 6.500m;
        second.VaPercent = null;

        var products = new MappingProductService(
            new Product { GKey = 101, Category = "BANGLE", Description = "Gold Bangle A", Purity = "916" },
            new Product { GKey = 202, Category = "BANGLE", Description = "Gold Bangle B", Purity = "750" });
        var summaries = new MappingStockSummaryService(
            new ProductStockSummary { GKey = 11, ProductGkey = 101, VaPercent = 12m },
            new ProductStockSummary { GKey = 22, ProductGkey = 202, VaPercent = 18m });
        var renderer = new RecordingPreviewRenderer();
        var printer = new RecordingPrinter();
        var stockService = new RecordingStockService();
        var viewModel = new BarCodeTagListViewModel(products, stockService, summaries,
            new StubCategoryService(), null!, new StubCompanyService(ValidCompany()), printer, renderer);
        await viewModel.InitializationTask;

        await viewModel.PreviewTagCommand.ExecuteAsync(first);
        LabelPrintRequest firstRequest = renderer.LastRequest!;
        await viewModel.PreviewTagCommand.ExecuteAsync(second);
        LabelPrintRequest secondPreviewRequest = renderer.LastRequest!;
        await viewModel.ReprintTagCommand.ExecuteAsync(second);

        Assert.Multiple(() =>
        {
            Assert.That(products.RequestedGkeys, Is.EqualTo(new[] { 101, 202, 202 }));
            Assert.That(summaries.RequestedGkeys, Is.EqualTo(new[] { 22, 22 }));
            Assert.That(firstRequest.ProductCode, Is.EqualTo("GBL2-0180"));
            Assert.That(firstRequest.ProductName, Is.EqualTo("Gold Bangle A"));
            Assert.That(firstRequest.ProductWeight, Is.EqualTo(8.125m));
            Assert.That(firstRequest.StoneWeight, Is.EqualTo(0.125m));
            Assert.That(firstRequest.VaPercent, Is.EqualTo(9.5m), "Individual ProductStock VA takes precedence.");
            Assert.That(secondPreviewRequest.ProductCode, Is.EqualTo("GBL2-0181"));
            Assert.That(secondPreviewRequest.ProductName, Is.EqualTo("Gold Bangle B"));
            Assert.That(secondPreviewRequest.ProductPurity, Is.EqualTo("750"));
            Assert.That(secondPreviewRequest.ProductWeight, Is.EqualTo(6.500m));
            Assert.That(secondPreviewRequest.VaPercent, Is.EqualTo(18m), "VA must come from the exact associated summary.");
            Assert.That(printer.LastRequest, Is.EqualTo(secondPreviewRequest));
            Assert.That(stockService.TotalMutationCalls, Is.Zero);
        });
    }

    [Test]
    public async Task MissingAssociatedProduct_ErrorIdentifiesSkuAndStockGkey()
    {
        ProductStock stock = ValidStock();
        stock.GKey = 2651;
        stock.ProductSku = "GBL2-0180";
        var viewModel = await CreateViewModel(new RecordingStockService(), missingProduct: true);

        await viewModel.PreviewTagCommand.ExecuteAsync(stock);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasLabelPreview, Is.False);
            Assert.That(viewModel.PreviewStatusMessage, Does.Contain("GBL2-0180"));
            Assert.That(viewModel.PreviewStatusMessage, Does.Contain("ProductStock GKey 2651"));
            Assert.That(viewModel.PreviewStatusMessage, Does.Contain("PRODUCT GKey 101"));
        });
    }
    private static async Task<BarCodeTagListViewModel> CreateViewModel(
        RecordingStockService stockService,
        ILabelPrinter? printer = null,
        ILabelPreviewRenderer? renderer = null,
        Product? product = null,
        ProductStockSummary? stockSummary = null,
        OrgThisCompanyView? company = null,
        bool missingProduct = false,
        bool missingCompany = false)
    {
        var viewModel = new BarCodeTagListViewModel(
            new StubProductService(missingProduct ? null : product ?? ValidProduct()),
            stockService,
            new StubStockSummaryService(stockSummary ?? ValidStockSummary()),
            new StubCategoryService(),
            null!,
            new StubCompanyService(missingCompany ? null : company ?? ValidCompany()),
            printer ?? new RecordingPrinter(),
            renderer ?? new ZplLabelPreviewRenderer());
        await viewModel.InitializationTask;
        return viewModel;
    }
    private static ProductStock PendingStock() => new()
    {
        GKey = 2625, ProductGkey = 102, StockSummaryGkey = 12, GrnLineSummaryGkey = 23,
        ProductSku = "GCN2-0152", Category = "CHAIN", GrossWeight = null,
        StoneWeight = 0m, NetWeight = null, StockQty = 1, SuppliedQty = 1,
        SoldQty = 0, BalanceWeight = 0m, Status = "Pending Tag", IsProductSold = false,
        SupplierId = "SUP-1", IsBarcodePrinted = false
    };
    private static ProductStock ValidStock() => new()
    {
        GKey = 2608, ProductGkey = 101, StockSummaryGkey = 11, GrnLineSummaryGkey = 22,
        ProductSku = "GBL2-0176", Category = "BANGLE", GrossWeight = 10.444m,
        StoneWeight = 0.111m, NetWeight = 10.333m, StockQty = 1, SuppliedQty = 1,
        SoldQty = 0, BalanceWeight = 10.333m, Status = "In-Stock", IsProductSold = false,
        SupplierId = "SUP-1", IsBarcodePrinted = true
    };

    private static Product ValidProduct() => new()
    {
        GKey = 101, Id = "GBL", Category = "BANGLE", Description = "Gold Bangle", Purity = "916"
    };

    private static ProductStockSummary ValidStockSummary() => new()
    {
        GKey = 11, ProductGkey = 101, Category = "BANGLE", VaPercent = 12.5m
    };
    private static OrgThisCompanyView ValidCompany() => new() { CompanyName = "MATHA" };

    private static string Snapshot(ProductStock s) => string.Join('|',
        s.GKey, s.ProductSku, s.ProductGkey, s.StockSummaryGkey, s.GrnLineSummaryGkey,
        s.GrossWeight, s.StoneWeight, s.NetWeight, s.StockQty, s.SuppliedQty, s.SoldQty,
        s.BalanceWeight, s.Status, s.IsProductSold, s.SupplierId, s.IsBarcodePrinted);

    private static string FindRepositoryFile(params string[] segments)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(new[] { directory.FullName }.Concat(segments).ToArray());
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException(string.Join('/', segments));
    }

    private sealed class RecordingPrinter : ILabelPrinter
    {
        public int CallCount;
        public LabelPrintRequest? LastRequest;
        public bool Hold;
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<LabelPrintResult> PrintAsync(LabelPrintRequest request, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref CallCount);
            LastRequest = request;
            Started.TrySetResult();
            if (Hold) await Release.Task.WaitAsync(cancellationToken);
            return new LabelPrintResult(LabelPrintStatus.Submitted);
        }
    }

    private sealed class RecordingPreviewRenderer : ILabelPreviewRenderer
    {
        public int CallCount;
        public LabelPrintRequest? LastRequest;
        public LabelPreviewResult Render(LabelPrintRequest request)
        {
            CallCount++;
            LastRequest = request;
            return new ZplLabelPreviewRenderer().Render(request);
        }
    }

    private sealed class RecordingStockService : IProductStockService
    {
        public IEnumerable<ProductStock> CategoryStock { get; set; } = Array.Empty<ProductStock>();
        public int CreateCalls, UpdateCalls, ReserveCalls;
        public int TotalMutationCalls => CreateCalls + UpdateCalls + ReserveCalls;
        public Task<ProductStock> GetProductStock(int gKey) => Task.FromResult<ProductStock>(null!);
        public Task<ProductStock> GetProduct(string productId) => Task.FromResult<ProductStock>(null!);
        public Task<ProductStock> GetProductStock(string productId) => Task.FromResult<ProductStock>(null!);
        public Task<ProductStock?> GetExactProductStock(string productSku) => Task.FromResult<ProductStock?>(null);
        public Task<IEnumerable<ProductStock>> GetCategoryList(string category) => Task.FromResult(CategoryStock);
        public Task<IEnumerable<ProductStock>> GetPendingByGrnLineSummary(int key) => Task.FromResult<IEnumerable<ProductStock>>(Array.Empty<ProductStock>());
        public Task<ProductStock> ReserveProductSku(int gKey) { ReserveCalls++; return Task.FromResult<ProductStock>(null!); }
        public Task CreateProductStock(ProductStock stock) { CreateCalls++; return Task.CompletedTask; }
        public Task UpdateProductStock(ProductStock stock) { UpdateCalls++; return Task.CompletedTask; }
    }

    private sealed class MappingProductService(params Product[] products) : IProductService
    {
        private readonly Dictionary<int, Product> _products = products.ToDictionary(x => x.GKey);
        public List<int> RequestedGkeys { get; } = new();
        public Task<Product> GetByGkey(int productGkey)
        {
            RequestedGkeys.Add(productGkey);
            return Task.FromResult(_products.GetValueOrDefault(productGkey)!);
        }
        public Task<Product> GetProduct(string id) => throw new AssertionException("SKU/ID PRODUCT lookup is not allowed.");
        public Task<Product> GetByCategory(string category) => throw new AssertionException("Category fallback is not allowed.");
        public Task CreateProduct(Product product) => Task.CompletedTask;
        public Task UpdateProduct(Product product) => Task.CompletedTask;
    }

    private sealed class MappingStockSummaryService(params ProductStockSummary[] summaries) : IProductStockSummaryService
    {
        private readonly Dictionary<int, ProductStockSummary> _summaries = summaries.ToDictionary(x => x.GKey);
        public List<int> RequestedGkeys { get; } = new();
        public Task<ProductStockSummary> GetByGkey(int stockSummaryGkey)
        {
            RequestedGkeys.Add(stockSummaryGkey);
            return Task.FromResult(_summaries.GetValueOrDefault(stockSummaryGkey)!);
        }
        public Task<ProductStockSummary> GetByProductGkey(int? productGkey) => throw new AssertionException("ProductGkey summary lookup is not allowed here.");
        public Task<ProductStockSummary> GetProductStockSummary(string productId) => throw new AssertionException("Wrong summary lookup.");
        public Task<ProductStockSummary> GetProductStockSummaryByProductSku(string productSku) => throw new AssertionException("Wrong summary lookup.");
        public Task<ProductStockSummary> GetProductStockSummaryByCategory(string category) => throw new AssertionException("Category fallback is not allowed.");
        public Task CreateProductStockSummary(ProductStockSummary value) => Task.CompletedTask;
        public Task UpdateProductStockSummary(ProductStockSummary value) => Task.CompletedTask;
        public Task<IEnumerable<ProductStockSummary>> GetAll() => Task.FromResult<IEnumerable<ProductStockSummary>>(Array.Empty<ProductStockSummary>());
    }
    private sealed class StubProductService(Product? value) : IProductService
    {
        private readonly Product? _value = value;
        public int? RequestedGkey { get; private set; }
        public Task<Product> GetByGkey(int productGkey) { RequestedGkey = productGkey; return Task.FromResult(_value!); }
        public Task<Product> GetProduct(string id) => throw new AssertionException("Barcode Maintenance must resolve PRODUCT by GKey.");
        public Task<Product> GetByCategory(string category) => throw new AssertionException("Category fallback is not allowed.");
        public Task CreateProduct(Product product) => Task.CompletedTask;
        public Task UpdateProduct(Product product) => Task.CompletedTask;
    }

    private sealed class StubStockSummaryService(ProductStockSummary? value) : IProductStockSummaryService
    {
        private readonly ProductStockSummary? _value = value;
        public int? RequestedGkey { get; private set; }
        public Task<ProductStockSummary> GetByGkey(int stockSummaryGkey) { RequestedGkey = stockSummaryGkey; return Task.FromResult(_value!); }
        public Task<ProductStockSummary> GetByProductGkey(int? productGkey) => throw new AssertionException("Summary must resolve by StockSummaryGkey.");
        public Task<ProductStockSummary> GetProductStockSummary(string productId) => throw new AssertionException("Wrong summary lookup.");
        public Task<ProductStockSummary> GetProductStockSummaryByProductSku(string productSku) => throw new AssertionException("Wrong summary lookup.");
        public Task<ProductStockSummary> GetProductStockSummaryByCategory(string category) => throw new AssertionException("Category fallback is not allowed.");
        public Task CreateProductStockSummary(ProductStockSummary value) => Task.CompletedTask;
        public Task UpdateProductStockSummary(ProductStockSummary value) => Task.CompletedTask;
        public Task<IEnumerable<ProductStockSummary>> GetAll() => Task.FromResult<IEnumerable<ProductStockSummary>>(Array.Empty<ProductStockSummary>());
    }
    private sealed class StubCompanyService(OrgThisCompanyView? value) : IOrgThisCompanyViewService
    {
        private readonly OrgThisCompanyView? _value = value;
        public Task<OrgThisCompanyView> GetOrgThisCompany() => Task.FromResult(_value!);
    }

    private sealed class StubCategoryService : IProductCategoryService
    {
        public Task<ProductCategory> GetProductCategory(string name) => Task.FromResult<ProductCategory>(null!);
        public Task<IEnumerable<ProductCategory>> GetProductCategoryList() => Task.FromResult<IEnumerable<ProductCategory>>([new ProductCategory { Name = "BANGLE" }]);
        public Task CreatProductCategory(ProductCategory value) => Task.CompletedTask;
        public Task UpdateProductCategory(ProductCategory value) => Task.CompletedTask;
    }
}
