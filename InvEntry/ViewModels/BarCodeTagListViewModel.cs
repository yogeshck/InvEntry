using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Services.Printing;
using InvEntry.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace InvEntry.ViewModels;

public partial class BarCodeTagListViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly IProductStockSummaryService _productStockSummaryService;
    private readonly IProductStockService _productStockService;
    private readonly IMessageBoxService? _messageBoxService;
    private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;
    private readonly IProductCategoryService _productCategoryService;
    private readonly ILabelPrinter _labelPrinter;
    private readonly ILabelPreviewRenderer _labelPreviewRenderer;
    private int _searchVersion;
    private int _categoryVersion;
    private CancellationTokenSource? _skuLookupCts;

    [ObservableProperty] private ObservableCollection<string> _productSkuStrList = new();
    [ObservableProperty] private ObservableCollection<string> _productCategoryList = new();
    [ObservableProperty] private ObservableCollection<ProductStock> _productStockList = new();
    [ObservableProperty] private ProductStock? _selectedGridLine;
    [ObservableProperty] private OrgThisCompanyView? _company;
    [ObservableProperty] private string? _selectedCategory;
    [ObservableProperty] private string? _selectedProductSku;
    [ObservableProperty] private string? _maintenanceStatus;
    [ObservableProperty] private bool _isErrorStatus;
    [ObservableProperty] private bool _isLoadingStock;
    [ObservableProperty] private bool _isPreviewingTag;
    [ObservableProperty] private bool _isPrintingTag;
    [ObservableProperty] private BitmapSource? _labelPreviewImage;
    [ObservableProperty] private string? _labelPreviewZpl;
    [ObservableProperty] private bool _hasLabelPreview;
    [ObservableProperty] private string? _previewStatusHeading = "Tag preview";
    [ObservableProperty] private string? _previewStatusMessage = "Select a stock record to preview its tag.";

    public bool IsLabelPrintSimulation => _labelPrinter is SimulatedLabelPrinter;
    public Task InitializationTask { get; }

    public BarCodeTagListViewModel(
        IProductService productService,
        IProductStockService productStockService,
        IProductStockSummaryService productStockSummaryService,
        IProductCategoryService productCategoryService,
        IMessageBoxService messageBoxService,
        IOrgThisCompanyViewService orgThisCompanyViewService,
        ILabelPrinter labelPrinter,
        ILabelPreviewRenderer labelPreviewRenderer)
    {
        _productStockService = productStockService;
        _productService = productService;
        _productStockSummaryService = productStockSummaryService;
        _productCategoryService = productCategoryService;
        _messageBoxService = messageBoxService;
        _orgThisCompanyViewService = orgThisCompanyViewService;
        _labelPrinter = labelPrinter;
        _labelPreviewRenderer = labelPreviewRenderer;
        InitializationTask = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            MaintenanceStatus = "Loading barcode maintenance data...";
            var categoriesTask = _productCategoryService.GetProductCategoryList();
            var companyTask = _orgThisCompanyViewService.GetOrgThisCompany();
            await Task.WhenAll(categoriesTask, companyTask);
            ProductCategoryList = new ObservableCollection<string>(
                (await categoriesTask)
                    .Select(category => category.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name));
            Company = await companyTask;
            MaintenanceStatus = "Enter or scan an exact SKU, or select a category and refresh.";
        }
        catch (Exception ex)
        {
            SetError("Unable to load barcode maintenance data", ex.Message, showDialog: false);
        }
    }

    partial void OnSelectedCategoryChanged(string? value)
    {
        SelectedProductSku = null;
        SelectedGridLine = null;
        InvalidatePreview();
        _ = LoadCategorySkusSafelyAsync(value, Interlocked.Increment(ref _categoryVersion));
    }

    partial void OnSelectedProductSkuChanged(string? value)
    {
        _skuLookupCts?.Cancel();
        _skuLookupCts?.Dispose();
        _skuLookupCts = null;
        if (string.IsNullOrWhiteSpace(value)) return;

        var cancellation = new CancellationTokenSource();
        _skuLookupCts = cancellation;
        _ = LookupScannedSkuSafelyAsync(cancellation.Token);
    }

    private async Task LookupScannedSkuSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(300, cancellationToken);
            await RefreshBarcodeAsync();
        }
        catch (OperationCanceledException)
        {
            // A keyboard-wedge scan or manual edit supplied a newer value.
        }
        catch (Exception ex)
        {
            SetError("Unable to look up SKU", ex.Message, showDialog: false);
        }
    }
    partial void OnSelectedGridLineChanged(ProductStock? value)
    {
        InvalidatePreview();
        var eligibility = BarcodeMaintenanceEligibilityEvaluator.Evaluate(value);
        SetPreviewUnavailableState(value, eligibility);
        if (value is not null) MaintenanceStatus = $"{eligibility.DisplayText}: {eligibility.Guidance}";
        PreviewTagCommand.NotifyCanExecuteChanged();
        ReprintTagCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsPrintingTagChanged(bool value) => ReprintTagCommand.NotifyCanExecuteChanged();
    partial void OnIsPreviewingTagChanged(bool value) => PreviewTagCommand.NotifyCanExecuteChanged();

    private async Task LoadCategorySkusSafelyAsync(string? category, int requestVersion)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                if (requestVersion == _categoryVersion)
                    ProductSkuStrList = new();
                return;
            }

            var stock = (await _productStockService.GetCategoryList(category)).ToList();
            if (requestVersion != _categoryVersion) return;
            ProductSkuStrList = new ObservableCollection<string>(stock
                .Select(item => item.ProductSku)
                .Where(sku => !string.IsNullOrWhiteSpace(sku))
                .Select(sku => sku!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(sku => sku));
        }
        catch (Exception ex)
        {
            if (requestVersion == _categoryVersion)
                SetError("Unable to load category SKUs", ex.Message, showDialog: false);
        }
    }

    [RelayCommand]
    private void ResetForm()
    {
        _skuLookupCts?.Cancel();
        Interlocked.Increment(ref _searchVersion);
        Interlocked.Increment(ref _categoryVersion);
        SelectedCategory = null;
        SelectedProductSku = null;
        SelectedGridLine = null;
        ProductSkuStrList = new();
        ProductStockList = new();
        InvalidatePreview();
        IsErrorStatus = false;
        MaintenanceStatus = "Enter or scan an exact SKU, or select a category and refresh.";
    }

    [RelayCommand]
    private async Task RefreshBarcodeAsync()
    {
        int requestVersion = Interlocked.Increment(ref _searchVersion);
        IsLoadingStock = true;
        IsErrorStatus = false;
        MaintenanceStatus = "Loading stock...";
        InvalidatePreview();

        try
        {
            string? sku = string.IsNullOrWhiteSpace(SelectedProductSku)
                ? null
                : SelectedProductSku.Trim();
            string? category = string.IsNullOrWhiteSpace(SelectedCategory)
                ? null
                : SelectedCategory.Trim();
            List<ProductStock> matches;

            if (category is not null)
            {
                matches = (await _productStockService.GetCategoryList(category)).ToList();
            }
            else if (sku is not null)
            {
                var categoryNames = ProductCategoryList.ToList();
                var tasks = categoryNames.Select(_productStockService.GetCategoryList).ToArray();
                var categoryResults = await Task.WhenAll(tasks);
                matches = categoryResults.SelectMany(items => items).ToList();
            }
            else
            {
                matches = new List<ProductStock>();
            }

            if (sku is not null)
            {
                matches = matches
                    .Where(stock => string.Equals(stock.ProductSku?.Trim(), sku, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (requestVersion != _searchVersion) return;
            MarkDuplicates(matches);
            ProductStockList = new ObservableCollection<ProductStock>(matches.OrderBy(stock => stock.ProductSku).ThenBy(stock => stock.GKey));
            SelectedGridLine = null;

            MaintenanceStatus = matches.Count switch
            {
                0 => sku is null && category is null
                    ? "Select a category or enter/scan an exact SKU."
                    : "No matching ProductStock records were found.",
                1 => "Ready to preview or reprint the selected stock record.",
                _ when sku is not null => $"{matches.Count} records match SKU {sku}. Select the required ProductStock GKey.",
                _ => $"Loaded {matches.Count} stock records."
            };
        }
        catch (Exception ex)
        {
            if (requestVersion == _searchVersion)
            {
                ProductStockList = new();
                SelectedGridLine = null;
                SetError("Unable to load stock", ex.Message);
            }
        }
        finally
        {
            if (requestVersion == _searchVersion)
                IsLoadingStock = false;
        }
    }

    private static void MarkDuplicates(IReadOnlyCollection<ProductStock> stock)
    {
        var duplicates = stock
            .Where(item => !string.IsNullOrWhiteSpace(item.ProductSku))
            .GroupBy(item => item.ProductSku!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (ProductStock item in stock)
            item.DuplicateFlag = item.ProductSku is not null && duplicates.Contains(item.ProductSku.Trim()) ? "D" : string.Empty;
    }

    private bool CanPreviewTag(ProductStock? stock) =>
        !IsPreviewingTag && BarcodeMaintenanceEligibilityEvaluator.Evaluate(stock).CanPreviewOrReprint;

    [RelayCommand(CanExecute = nameof(CanPreviewTag))]
    private async Task PreviewTagAsync(ProductStock? stock)
    {
        if (stock is null || IsPreviewingTag) return;
        BarcodeMaintenanceEligibility eligibility = BarcodeMaintenanceEligibilityEvaluator.Evaluate(stock);
        if (!eligibility.CanPreviewOrReprint)
        {
            InvalidatePreview();
            SetPreviewUnavailableState(stock, eligibility);
            MaintenanceStatus = eligibility.DisplayText;
            return;
        }

        SelectedGridLine = stock;
        IsPreviewingTag = true;
        IsErrorStatus = false;
        MaintenanceStatus = "Generating tag preview...";

        try
        {
            LabelPrintRequest request = await CreateLabelPrintRequestAsync(stock);
            LabelPreviewResult preview = _labelPreviewRenderer.Render(request);
            LabelPreviewZpl = preview.Zpl;
            LabelPreviewImage = preview.Image;
            HasLabelPreview = true;
            PreviewStatusHeading = null;
            PreviewStatusMessage = null;
            MaintenanceStatus = $"Preview generated for {stock.ProductSku}.";
        }
        catch (Exception ex)
        {
            InvalidatePreview();
            PreviewStatusHeading = "Unable to generate tag preview";
            PreviewStatusMessage = ex.Message;
            SetError("Unable to preview label", ex.Message);
        }
        finally
        {
            IsPreviewingTag = false;
        }
    }

    private bool CanReprintTag(ProductStock? stock) =>
        !IsPrintingTag && BarcodeMaintenanceEligibilityEvaluator.Evaluate(stock).CanPreviewOrReprint;

    [RelayCommand(CanExecute = nameof(CanReprintTag))]
    private async Task ReprintTagAsync(ProductStock? stock)
    {
        if (stock is null || IsPrintingTag) return;
        BarcodeMaintenanceEligibility eligibility = BarcodeMaintenanceEligibilityEvaluator.Evaluate(stock);
        if (!eligibility.CanPreviewOrReprint)
        {
            MaintenanceStatus = eligibility.DisplayText;
            return;
        }

        SelectedGridLine = stock;
        IsPrintingTag = true;
        IsErrorStatus = false;
        MaintenanceStatus = $"Printing {stock.ProductSku}...";

        try
        {
            LabelPrintRequest request = await CreateLabelPrintRequestAsync(stock);
            LabelPrintResult result = await _labelPrinter.PrintAsync(request);
            if (!result.Success)
            {
                SetError("Label reprint failed", result.ErrorMessage ?? "The printer did not accept the label.");
                return;
            }

            MaintenanceStatus = IsLabelPrintSimulation
                ? $"Simulated print success for {stock.ProductSku}; no physical label was printed."
                : $"Physical print submitted successfully for {stock.ProductSku}.";
            _messageBoxService?.ShowMessage(MaintenanceStatus, "Barcode reprint", MessageButton.OK, MessageIcon.Information);
        }
        catch (Exception ex)
        {
            SetError("Label reprint failed", ex.Message);
        }
        finally
        {
            IsPrintingTag = false;
        }
    }

    private async Task<LabelPrintRequest> CreateLabelPrintRequestAsync(ProductStock stock)
    {
        var errors = ValidateStock(stock).ToList();
        Product? product = null;
        ProductStockSummary? stockSummary = null;
        OrgThisCompanyView? company = Company;
        string stockIdentity = $"SKU {stock.ProductSku ?? "<missing>"}, ProductStock GKey {stock.GKey}";

        if (stock.ProductGkey.GetValueOrDefault() > 0)
        {
            product = await _productService.GetByGkey(stock.ProductGkey!.Value);
            if (product is null)
                errors.Add($"Associated PRODUCT GKey {stock.ProductGkey.Value} was not found for {stockIdentity}.");
        }

        decimal? vaPercent = stock.VaPercent;
        if (!vaPercent.HasValue && stock.StockSummaryGkey.GetValueOrDefault() > 0)
        {
            stockSummary = await _productStockSummaryService.GetByGkey(stock.StockSummaryGkey!.Value);
            if (stockSummary is null)
                errors.Add($"PRODUCT_STOCK_SUMMARY GKey {stock.StockSummaryGkey.Value} was not found for {stockIdentity}.");
            else if (stockSummary.ProductGkey != stock.ProductGkey)
                errors.Add($"PRODUCT_STOCK_SUMMARY GKey {stockSummary.GKey} does not belong to PRODUCT GKey {stock.ProductGkey} for {stockIdentity}.");
            else
                vaPercent = stockSummary.VaPercent;
        }

        if (company is null)
        {
            company = await _orgThisCompanyViewService.GetOrgThisCompany();
            Company = company;
        }
        if (company is null || string.IsNullOrWhiteSpace(company.CompanyName))
            errors.Add($"Company details are unavailable for {stockIdentity}.");

        if (product is not null)
        {
            if (string.IsNullOrWhiteSpace(product.Description)) errors.Add($"Product description is unavailable for {stockIdentity}.");
            if (string.IsNullOrWhiteSpace(product.Purity)) errors.Add($"Product purity is unavailable for {stockIdentity}.");
        }
        if (!vaPercent.HasValue || vaPercent.Value < 0m)
            errors.Add($"Making charge / VA percentage is unavailable or invalid for {stockIdentity}.");

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Distinct()));

        return new LabelPrintRequest(
            stock.ProductSku!,
            product!.Description!,
            vaPercent!.Value,
            stock.NetWeight!.Value,
            stock.StoneWeight!.Value,
            product.Purity!,
            company!.CompanyName!);
    }
    private static IEnumerable<string> ValidateStock(ProductStock stock)
    {
        if (stock.GKey <= 0) yield return "ProductStock GKey is invalid.";
        if (string.IsNullOrWhiteSpace(stock.ProductSku)) yield return "Product SKU is required.";
        if (!stock.ProductGkey.HasValue || stock.ProductGkey.Value <= 0) yield return "Product identity is unavailable.";
        if (string.IsNullOrWhiteSpace(stock.Category)) yield return "Product category is unavailable.";
        if (!stock.GrossWeight.HasValue || stock.GrossWeight.Value <= 0m) yield return "Gross weight must be greater than zero.";
        if (!stock.NetWeight.HasValue || stock.NetWeight.Value <= 0m) yield return "Net weight must be greater than zero.";
        if (!stock.StoneWeight.HasValue || stock.StoneWeight.Value < 0m) yield return "Stone weight is unavailable or invalid.";
        if (stock.StoneWeight.GetValueOrDefault() > stock.GrossWeight.GetValueOrDefault()) yield return "Stone weight cannot exceed gross weight.";
    }

    private void SetPreviewUnavailableState(ProductStock? stock, BarcodeMaintenanceEligibility eligibility)
    {
        if (stock is null)
        {
            PreviewStatusHeading = "Tag preview";
            PreviewStatusMessage = "Select a stock record to preview its tag.";
            return;
        }

        switch (eligibility.State)
        {
            case BarcodeMaintenanceEligibilityState.PendingInitialTagging:
                PreviewStatusHeading = "Initial weighing and tagging required";
                PreviewStatusMessage = "Complete this item through GRN Weighing & Tagging before using Barcode Maintenance.";
                break;
            case BarcodeMaintenanceEligibilityState.InvalidStockData:
                PreviewStatusHeading = "Tag preview unavailable";
                PreviewStatusMessage = "The selected stock record has missing or invalid weight data. Correct it through the appropriate existing stock/GRN workflow.";
                break;
            default:
                PreviewStatusHeading = "Tag preview unavailable";
                PreviewStatusMessage = eligibility.Guidance;
                break;
        }
    }
    private void InvalidatePreview()
    {
        LabelPreviewImage = null;
        LabelPreviewZpl = null;
        HasLabelPreview = false;
    }

    private void SetError(string title, string message, bool showDialog = true)
    {
        IsErrorStatus = true;
        MaintenanceStatus = $"{title}: {message}";
        if (showDialog)
            _messageBoxService?.ShowMessage(message, title, MessageButton.OK, MessageIcon.Error);
    }
}
