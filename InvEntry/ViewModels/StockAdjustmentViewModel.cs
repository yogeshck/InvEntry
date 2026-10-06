using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using InvEntry.Helpers;
using InvEntry.Contracts.StockAdjustments;
using InvEntry.Models;
using InvEntry.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

public partial class StockAdjustmentViewModel : ObservableObject
{
    public const string ConsolidatedStockOption = "Consolidated Stock (No SKU)";

    private readonly IProductStockService _productStockService;
    private readonly IProductStockSummaryService _productStockSummaryService;
    private readonly IProductService _productService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IMtblReferencesService _mtblReferencesService;
    private readonly IProductCategoryService _productCategoryService;
    private readonly IStockAdjustmentService _stockAdjustmentService;
    private readonly ReferenceLoader _referenceLoader;

    [ObservableProperty] private ObservableCollection<string> _stockAdjReasonList = new();
    [ObservableProperty] private ObservableCollection<MtblReference> _mtblReferencesList = new();
    [ObservableProperty] private ObservableCollection<string> _productCategoryList = new();
    [ObservableProperty] private ObservableCollection<string> _productSkuStrList = new();
    [ObservableProperty] private ObservableCollection<string> _toProductSkuStrList = new();

    // Source/current stock. ProductStock is populated only when an actual SKU is selected.
    [ObservableProperty] private ProductStock _productStock = new();
    [ObservableProperty] private ProductStock _modifiedProductStock = new();
    [ObservableProperty] private ProductStockSummary _productStockSummary = new();
    [ObservableProperty] private ProductStockSummary _modifiedProductStockSummary = new();

    // Destination stock used by reallocation.
    [ObservableProperty] private ProductStock _toProductStock = new();
    [ObservableProperty] private ProductStock _toModifiedProductStock = new();
    [ObservableProperty] private ProductStockSummary _toProductStockSummary = new();
    [ObservableProperty] private ProductStockSummary _toModifiedProductStockSummary = new();

    // UI-only movement values used by the preview.
    [ObservableProperty] private ProductStock _userEntryStock = new();

    [ObservableProperty] private string? _selectedCategoryId;
    [ObservableProperty] private string? _selectedProductSku;
    [ObservableProperty] private string? _toSelectedCategoryId;
    [ObservableProperty] private string? _toSelectedProductSku;
    [ObservableProperty] private string? _selectedReasonCode;
    [ObservableProperty] private StockAdjustmentMode _selectedMode = StockAdjustmentMode.Increase;

    [ObservableProperty] private string _adjustmentNumber = "NEW";
    [ObservableProperty] private DateTime _adjustmentDate = DateTime.Today;
    [ObservableProperty] private string? _remarks;
    [ObservableProperty] private decimal? _adjustmentGrossWeight;
    [ObservableProperty] private decimal? _adjustmentStoneWeight;

    public decimal AdjustmentNetWeight =>
        AdjustmentGrossWeight.GetValueOrDefault() -
        AdjustmentStoneWeight.GetValueOrDefault();

    public bool IsIncreaseMode => SelectedMode == StockAdjustmentMode.Increase;
    public bool IsDecreaseMode => SelectedMode == StockAdjustmentMode.Decrease;
    public bool IsReallocationMode => SelectedMode == StockAdjustmentMode.Reallocation;

    public bool HasSourceSku =>
        !string.IsNullOrWhiteSpace(SelectedProductSku) &&
        !IsConsolidatedSelection(SelectedProductSku);

    public bool IsSourceConsolidated =>
        IsConsolidatedSelection(SelectedProductSku);

    public bool HasDestinationSku =>
        !string.IsNullOrWhiteSpace(ToSelectedProductSku) &&
        !IsConsolidatedSelection(ToSelectedProductSku);

    public bool IsDestinationConsolidated =>
        IsConsolidatedSelection(ToSelectedProductSku);

    public string SourceStockLevelText =>
        HasSourceSku ? "INDIVIDUAL SKU + CATEGORY SUMMARY" : "CONSOLIDATED / SUMMARY STOCK";

    public string DestinationStockLevelText =>
        HasDestinationSku ? "INDIVIDUAL SKU + CATEGORY SUMMARY" : "CONSOLIDATED / SUMMARY STOCK";

    public string MiddleColumnTitle =>
        IsReallocationMode ? "2. MATERIAL TRANSFER" : "2. ADJUSTMENT";

    public string LeftColumnTitle =>
        IsReallocationMode ? "1. FROM / CURRENT" : "1. CURRENT STOCK";

    public string RightColumnTitle =>
        IsReallocationMode ? "3. TO / RESULT" : "3. RESULTING STOCK";

    public bool IsReallocationPreviewValid => TryValidateReallocationPreview(out _);

    public string ValidationMessage
    {
        get
        {
            if (IsReallocationMode)
            {
                return TryValidateReallocationPreview(out var message)
                    ? "Transfer preview is valid. OUT and IN will be posted together."
                    : message;
            }

            if (string.IsNullOrWhiteSpace(SelectedCategoryId))
                return "Select a product category.";

            if (ProductStockSummary.GKey <= 0)
                return "Current category stock summary is not available.";

            if (HasSourceSku && string.IsNullOrWhiteSpace(ProductStock.ProductSku))
                return "The selected SKU stock could not be loaded.";

            if (!TryValidateWeights(out var weightMessage))
                return weightMessage;

            if (SelectedMode == StockAdjustmentMode.Decrease &&
                !CanSourceSupplyAdjustment(out var sourceMessage))
                return sourceMessage;

            if (IsSourceConsolidated)
                return "Consolidated adjustment is ready to post.";

            return "SKU adjustment preview is ready.";
        }
    }

    public StockAdjustmentViewModel(
        IProductStockService productStockService,
        IProductStockSummaryService productStockSummaryService,
        IProductService productService,
        IProductCategoryService productCategoryService,
        IMtblReferencesService mtblReferencesService,
        IMessageBoxService messageBoxService,
        IStockAdjustmentService stockAdjustmentService,
        ReferenceLoader referenceLoader)
    {
        _productStockService = productStockService;
        _productStockSummaryService = productStockSummaryService;
        _productService = productService;
        _productCategoryService = productCategoryService;
        _messageBoxService = messageBoxService;
        _mtblReferencesService = mtblReferencesService;
        _stockAdjustmentService = stockAdjustmentService;
        _referenceLoader = referenceLoader;

        SetBase();
        _ = LoadReferencesAsync();
        PopulateProductCategoryList();
    }

    private void SetBase()
    {
        ProductStock = new ProductStock();
        ModifiedProductStock = new ProductStock();
        ProductStockSummary = new ProductStockSummary();
        ModifiedProductStockSummary = new ProductStockSummary();

        ToProductStock = new ProductStock();
        ToModifiedProductStock = new ProductStock();
        ToProductStockSummary = new ProductStockSummary();
        ToModifiedProductStockSummary = new ProductStockSummary();

        UserEntryStock = new ProductStock();
    }

    partial void OnSelectedModeChanged(StockAdjustmentMode value)
    {
        OnPropertyChanged(nameof(IsIncreaseMode));
        OnPropertyChanged(nameof(IsDecreaseMode));
        OnPropertyChanged(nameof(IsReallocationMode));
        OnPropertyChanged(nameof(MiddleColumnTitle));
        OnPropertyChanged(nameof(LeftColumnTitle));
        OnPropertyChanged(nameof(RightColumnTitle));
        Recalculate();
        NotifyUiState();
    }

    partial void OnSelectedReasonCodeChanged(string? value) => NotifyUiState();
    partial void OnRemarksChanged(string? value) => NotifyUiState();

    partial void OnAdjustmentGrossWeightChanged(decimal? value)
    {
        UserEntryStock.GrossWeight = value;
        UserEntryStock.NetWeight = AdjustmentNetWeight;
        OnPropertyChanged(nameof(AdjustmentNetWeight));
        Recalculate();
    }

    partial void OnAdjustmentStoneWeightChanged(decimal? value)
    {
        UserEntryStock.StoneWeight = value;
        UserEntryStock.NetWeight = AdjustmentNetWeight;
        OnPropertyChanged(nameof(AdjustmentNetWeight));
        Recalculate();
    }

    [RelayCommand] private void SelectIncreaseMode() => SelectedMode = StockAdjustmentMode.Increase;
    [RelayCommand] private void SelectDecreaseMode() => SelectedMode = StockAdjustmentMode.Decrease;
    [RelayCommand] private void SelectReallocationMode() => SelectedMode = StockAdjustmentMode.Reallocation;

    private async void PopulateProductCategoryList()
    {
        var list = await _productCategoryService.GetProductCategoryList();
        ProductCategoryList = new ObservableCollection<string>(list.Select(x => x.Name));
    }

    private async Task LoadSourceSelectionAsync(string productId)
    {
        try
        {
            var product = await _productService.GetProduct(productId);

            // Ignore a stale async result if the operator changed selection meanwhile.
            if (!string.Equals(
                    SelectedCategoryId,
                    productId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (product is null || product.GKey <= 0)
            {
                ProductSkuStrList = new ObservableCollection<string>();
                ProductStockSummary = new ProductStockSummary();
                ModifiedProductStockSummary = new ProductStockSummary();
                Recalculate();
                return;
            }

            var summary =
                await _productStockSummaryService
                    .GetByProductGkey(product.GKey);

            if (!string.Equals(
                    SelectedCategoryId,
                    productId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ProductStockSummary =
                summary is null
                    ? new ProductStockSummary
                    {
                        ProductGkey = product.GKey,
                        Category = product.Category
                    }
                    : CopySummaryForPreview(summary);

            ModifiedProductStockSummary =
                CopySummaryForPreview(ProductStockSummary);

            var stockCategory =
                !string.IsNullOrWhiteSpace(product.Category)
                    ? product.Category
                    : ProductStockSummary.Category;

            await PopulateProductSkuListByStockCategory(
                stockCategory,
                isDestination: false,
                expectedProductId: productId);

            Recalculate();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(
                ex,
                "Unable to load source stock selection for Product ID {ProductId}",
                productId);

            if (string.Equals(
                    SelectedCategoryId,
                    productId,
                    StringComparison.OrdinalIgnoreCase))
            {
                ProductSkuStrList = new ObservableCollection<string>();
                ProductStockSummary = new ProductStockSummary();
                ModifiedProductStockSummary = new ProductStockSummary();
                Recalculate();
            }
        }
    }

    private async Task LoadDestinationSelectionAsync(string productId)
    {
        try
        {
            var product = await _productService.GetProduct(productId);

            if (!string.Equals(
                    ToSelectedCategoryId,
                    productId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (product is null || product.GKey <= 0)
            {
                ToProductSkuStrList = new ObservableCollection<string>();
                ToProductStockSummary = new ProductStockSummary();
                ToModifiedProductStockSummary = new ProductStockSummary();
                Recalculate();
                return;
            }

            var summary =
                await _productStockSummaryService
                    .GetByProductGkey(product.GKey);

            if (!string.Equals(
                    ToSelectedCategoryId,
                    productId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ToProductStockSummary =
                summary is null
                    ? new ProductStockSummary
                    {
                        ProductGkey = product.GKey,
                        Category = product.Category
                    }
                    : CopySummaryForPreview(summary);

            ToModifiedProductStockSummary =
                CopySummaryForPreview(ToProductStockSummary);

            var stockCategory =
                !string.IsNullOrWhiteSpace(product.Category)
                    ? product.Category
                    : ToProductStockSummary.Category;

            await PopulateProductSkuListByStockCategory(
                stockCategory,
                isDestination: true,
                expectedProductId: productId);

            Recalculate();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(
                ex,
                "Unable to load destination stock selection for Product ID {ProductId}",
                productId);

            if (string.Equals(
                    ToSelectedCategoryId,
                    productId,
                    StringComparison.OrdinalIgnoreCase))
            {
                ToProductSkuStrList = new ObservableCollection<string>();
                ToProductStockSummary = new ProductStockSummary();
                ToModifiedProductStockSummary = new ProductStockSummary();
                Recalculate();
            }
        }
    }

    private async Task PopulateProductSkuListByStockCategory(
        string? stockCategory,
        bool isDestination,
        string expectedProductId)
    {
        var values = new System.Collections.Generic.List<string>
        {
            ConsolidatedStockOption
        };

        if (!string.IsNullOrWhiteSpace(stockCategory))
        {
            var skuList =
                await _productStockService
                    .GetCategoryList(stockCategory);

            values.AddRange(
                skuList
                    .Where(x => !string.IsNullOrWhiteSpace(x.ProductSku))
                    .Select(x => x.ProductSku!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x));
        }

        if (isDestination)
        {
            if (string.Equals(
                    ToSelectedCategoryId,
                    expectedProductId,
                    StringComparison.OrdinalIgnoreCase))
            {
                ToProductSkuStrList =
                    new ObservableCollection<string>(values);
            }
        }
        else if (string.Equals(
                     SelectedCategoryId,
                     expectedProductId,
                     StringComparison.OrdinalIgnoreCase))
        {
            ProductSkuStrList =
                new ObservableCollection<string>(values);
        }
    }

    partial void OnSelectedCategoryIdChanged(string? value)
    {
        SelectedProductSku = null;
        ProductStock = new ProductStock();
        ModifiedProductStock = new ProductStock();
        ProductStockSummary = new ProductStockSummary();
        ModifiedProductStockSummary = new ProductStockSummary();

        if (!string.IsNullOrWhiteSpace(value))
        {
            _ = LoadSourceSelectionAsync(value);
        }
        else
        {
            ProductSkuStrList = new ObservableCollection<string>();
        }

        NotifySourceSelectionChanged();
        Recalculate();
    }

    partial void OnSelectedProductSkuChanged(string? value)
    {
        if (HasSourceSku)
        {
            _ = LoadStockAsync(value!);
        }
        else
        {
            ProductStock = new ProductStock();
            ModifiedProductStock = new ProductStock();
            Recalculate();
        }

        NotifySourceSelectionChanged();
    }

    partial void OnToSelectedCategoryIdChanged(string? value)
    {
        ToSelectedProductSku = null;
        ToProductStock = new ProductStock();
        ToModifiedProductStock = new ProductStock();
        ToProductStockSummary = new ProductStockSummary();
        ToModifiedProductStockSummary = new ProductStockSummary();

        if (!string.IsNullOrWhiteSpace(value))
        {
            _ = LoadDestinationSelectionAsync(value);
        }
        else
        {
            ToProductSkuStrList = new ObservableCollection<string>();
        }

        NotifyDestinationSelectionChanged();
        Recalculate();
    }

    partial void OnToSelectedProductSkuChanged(string? value)
    {
        if (HasDestinationSku)
        {
            _ = LoadToStockAsync(value!);
        }
        else
        {
            ToProductStock = new ProductStock();
            ToModifiedProductStock = new ProductStock();
            Recalculate();
        }

        NotifyDestinationSelectionChanged();
    }

    private async Task LoadStockAsync(string productSku)
    {
        var stock = await _productStockService.GetProductStock(productSku);
        ProductStock = CopyStockForPreview(stock);
        ModifiedProductStock = CopyStockForPreview(stock);
        Recalculate();
    }

    private async Task LoadToStockAsync(string productSku)
    {
        var stock = await _productStockService.GetProductStock(productSku);
        ToProductStock = CopyStockForPreview(stock);
        ToModifiedProductStock = CopyStockForPreview(stock);
        Recalculate();
    }

    private static ProductStock CopyStockForPreview(ProductStock source)
    {
        return new ProductStock
        {
            GKey = source.GKey,
            StockSummaryGkey = source.StockSummaryGkey,
            ProductGkey = source.ProductGkey,
            ProductSku = source.ProductSku,
            Category = source.Category,
            StockQty = source.StockQty,
            GrossWeight = source.GrossWeight,
            StoneWeight = source.StoneWeight,
            NetWeight = source.NetWeight,
            BalanceWeight = source.NetWeight,
            Status = source.Status
        };
    }

    private static ProductStockSummary CopySummaryForPreview(ProductStockSummary source)
    {
        return new ProductStockSummary
        {
            GKey = source.GKey,
            Category = source.Category,
            ProductGkey = source.ProductGkey,
            ProductSku = source.ProductSku,
            StockQty = source.StockQty,
            GrossWeight = source.GrossWeight,
            StoneWeight = source.StoneWeight,
            NetWeight = source.NetWeight,
            BalanceWeight = source.NetWeight,
            Status = source.Status
        };
    }

    private async Task LoadReferencesAsync()
    {
        StockAdjReasonList = await _referenceLoader.LoadValuesAsync("STOCK_ADJUSTMENTS");
    }

    private void Recalculate()
    {
        UserEntryStock.GrossWeight = AdjustmentGrossWeight;
        UserEntryStock.StoneWeight = AdjustmentStoneWeight;
        UserEntryStock.NetWeight = AdjustmentNetWeight;

        if (IsReallocationMode)
            RecalculateReallocation();
        else
            RecalculateNormalAdjustment();

        NotifyUiState();
    }

    private void RecalculateNormalAdjustment()
    {
        ModifiedProductStockSummary = CopySummaryForPreview(ProductStockSummary);
        ModifiedProductStock = HasSourceSku
            ? CopyStockForPreview(ProductStock)
            : new ProductStock();

        if (string.IsNullOrWhiteSpace(SelectedCategoryId))
            return;

        var sign = SelectedMode == StockAdjustmentMode.Increase ? 1M : -1M;
        var gross = AdjustmentGrossWeight.GetValueOrDefault();
        var stone = AdjustmentStoneWeight.GetValueOrDefault();
        var net = AdjustmentNetWeight;

        ApplyWeightDelta(ModifiedProductStockSummary, sign, gross, stone, net);

        if (HasSourceSku)
            ApplyWeightDelta(ModifiedProductStock, sign, gross, stone, net);
    }

    private void RecalculateReallocation()
    {
        ModifiedProductStock = HasSourceSku
            ? CopyStockForPreview(ProductStock)
            : new ProductStock();
        ToModifiedProductStock = HasDestinationSku
            ? CopyStockForPreview(ToProductStock)
            : new ProductStock();

        ModifiedProductStockSummary = CopySummaryForPreview(ProductStockSummary);
        ToModifiedProductStockSummary = CopySummaryForPreview(ToProductStockSummary);

        if (string.IsNullOrWhiteSpace(SelectedCategoryId) ||
            string.IsNullOrWhiteSpace(ToSelectedCategoryId))
            return;

        var gross = AdjustmentGrossWeight.GetValueOrDefault();
        var stone = AdjustmentStoneWeight.GetValueOrDefault();
        var net = AdjustmentNetWeight;

        if (string.Equals(SelectedCategoryId, ToSelectedCategoryId, StringComparison.OrdinalIgnoreCase))
        {
            // Same category: summary OUT and IN cancel each other. Individual SKU
            // balances still change when a tagged item is selected on either side.
            ModifiedProductStockSummary = CopySummaryForPreview(ProductStockSummary);
            ToModifiedProductStockSummary = CopySummaryForPreview(ProductStockSummary);
        }
        else
        {
            ApplyWeightDelta(ModifiedProductStockSummary, -1M, gross, stone, net);
            ApplyWeightDelta(ToModifiedProductStockSummary, +1M, gross, stone, net);
        }

        if (HasSourceSku)
            ApplyWeightDelta(ModifiedProductStock, -1M, gross, stone, net);

        if (HasDestinationSku)
            ApplyWeightDelta(ToModifiedProductStock, +1M, gross, stone, net);
    }

    private static void ApplyWeightDelta(
        ProductStockSummary stock,
        decimal sign,
        decimal gross,
        decimal stone,
        decimal net)
    {
        stock.GrossWeight = stock.GrossWeight.GetValueOrDefault() + (sign * gross);
        stock.StoneWeight = stock.StoneWeight.GetValueOrDefault() + (sign * stone);
        stock.NetWeight = stock.NetWeight.GetValueOrDefault() + (sign * net);
        stock.BalanceWeight = stock.NetWeight;
    }

    private static void ApplyWeightDelta(
        ProductStock stock,
        decimal sign,
        decimal gross,
        decimal stone,
        decimal net)
    {
        stock.GrossWeight = stock.GrossWeight.GetValueOrDefault() + (sign * gross);
        stock.StoneWeight = stock.StoneWeight.GetValueOrDefault() + (sign * stone);
        stock.NetWeight = stock.NetWeight.GetValueOrDefault() + (sign * net);
        stock.BalanceWeight = stock.NetWeight;
    }

    private bool TryValidateWeights(out string message)
    {
        var gross = AdjustmentGrossWeight.GetValueOrDefault();
        var stone = AdjustmentStoneWeight.GetValueOrDefault();
        var net = AdjustmentNetWeight;

        if (gross <= 0)
        {
            message = IsReallocationMode
                ? "Enter a transfer gross weight greater than zero."
                : "Enter an adjustment gross weight greater than zero.";
            return false;
        }

        if (stone < 0 || stone > gross)
        {
            message = "Stone weight must be between zero and gross weight.";
            return false;
        }

        if (net <= 0)
        {
            message = "Net weight must be greater than zero.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private bool CanSourceSupplyAdjustment(out string message)
    {
        var gross = AdjustmentGrossWeight.GetValueOrDefault();
        var stone = AdjustmentStoneWeight.GetValueOrDefault();
        var net = AdjustmentNetWeight;

        if (ProductStockSummary.GKey <= 0)
        {
            message = "Current category stock summary is not available.";
            return false;
        }

        if (gross > ProductStockSummary.GrossWeight.GetValueOrDefault() ||
            stone > ProductStockSummary.StoneWeight.GetValueOrDefault() ||
            net > ProductStockSummary.NetWeight.GetValueOrDefault())
        {
            message = "The requested weight is greater than the available category stock summary.";
            return false;
        }

        if (HasSourceSku &&
            (gross > ProductStock.GrossWeight.GetValueOrDefault() ||
             stone > ProductStock.StoneWeight.GetValueOrDefault() ||
             net > ProductStock.NetWeight.GetValueOrDefault()))
        {
            message = "The requested weight is greater than the available SKU stock.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private bool TryValidateReallocationPreview(out string message)
    {
        if (!IsReallocationMode)
        {
            message = string.Empty;
            return false;
        }

        if (string.IsNullOrWhiteSpace(SelectedCategoryId))
        {
            message = "Select the FROM category.";
            return false;
        }

        if (ProductStockSummary.GKey <= 0)
        {
            message = "The FROM category stock summary is not available.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(SelectedProductSku))
        {
            message = "Choose either a FROM SKU or Consolidated Stock (No SKU).";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ToSelectedCategoryId))
        {
            message = "Select the TO category.";
            return false;
        }

        if (ToProductStockSummary.GKey <= 0)
        {
            message = "The TO category stock summary is not available.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ToSelectedProductSku))
        {
            message = "Choose either a TO SKU or Consolidated Stock (No SKU).";
            return false;
        }

        if (HasSourceSku &&
            (ProductStock.GKey <= 0 ||
             ProductStock.ProductGkey.GetValueOrDefault() <= 0))
        {
            message = "The selected FROM SKU stock could not be loaded.";
            return false;
        }

        if (HasDestinationSku &&
            (ToProductStock.GKey <= 0 ||
             ToProductStock.ProductGkey.GetValueOrDefault() <= 0))
        {
            message = "The selected TO SKU stock could not be loaded.";
            return false;
        }

        if (IsSourceConsolidated &&
            ProductStockSummary.ProductGkey.GetValueOrDefault() <= 0)
        {
            message = "The FROM consolidated stock does not contain a valid product reference.";
            return false;
        }

        if (IsDestinationConsolidated &&
            ToProductStockSummary.ProductGkey.GetValueOrDefault() <= 0)
        {
            message = "The TO consolidated stock does not contain a valid product reference.";
            return false;
        }

        if (HasSourceSku && HasDestinationSku &&
            string.Equals(SelectedProductSku, ToSelectedProductSku, StringComparison.OrdinalIgnoreCase))
        {
            message = "FROM item and TO item must be different.";
            return false;
        }

        if (IsSourceConsolidated && IsDestinationConsolidated &&
            string.Equals(SelectedCategoryId, ToSelectedCategoryId, StringComparison.OrdinalIgnoreCase))
        {
            message = "FROM and TO cannot both be the same consolidated category.";
            return false;
        }

        if (!TryValidateWeights(out message))
            return false;

        if (!CanSourceSupplyAdjustment(out message))
            return false;

        if (HasSourceSku &&
            ModifiedProductStock.StoneWeight.GetValueOrDefault() >
            ModifiedProductStock.GrossWeight.GetValueOrDefault())
        {
            message = "The resulting FROM item stone weight cannot exceed gross weight.";
            return false;
        }

        if (HasDestinationSku &&
            ToModifiedProductStock.StoneWeight.GetValueOrDefault() >
            ToModifiedProductStock.GrossWeight.GetValueOrDefault())
        {
            message = "The resulting TO item stone weight cannot exceed gross weight.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(SelectedReasonCode))
        {
            ShowInfo("Please select an adjustment reason to continue.");
            return false;
        }

        if (string.Equals(
                SelectedReasonCode,
                "OTHER",
                StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(Remarks))
        {
            ShowInfo("Please enter remarks when the adjustment reason is Other.");
            return false;
        }

        if (IsReallocationMode)
        {
            if (!TryValidateReallocationPreview(out var message))
            {
                ShowInfo(message);
                return false;
            }

            return true;
        }

        if (string.IsNullOrWhiteSpace(SelectedCategoryId))
        {
            ShowInfo("Please select a product category to continue.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(SelectedProductSku))
        {
            ShowInfo("Please choose an SKU or Consolidated Stock (No SKU).");
            return false;
        }

        if (ProductStockSummary.GKey <= 0)
        {
            ShowInfo("Current category stock summary is not available.");
            return false;
        }

        if (HasSourceSku &&
            (ProductStock.GKey <= 0 ||
             ProductStock.ProductGkey.GetValueOrDefault() <= 0))
        {
            ShowInfo("The selected SKU stock could not be loaded.");
            return false;
        }

        if (IsSourceConsolidated &&
            ProductStockSummary.ProductGkey.GetValueOrDefault() <= 0)
        {
            ShowInfo("The selected category stock does not contain a valid product reference.");
            return false;
        }

        if (!TryValidateWeights(out var weightMessage))
        {
            ShowInfo(weightMessage);
            return false;
        }

        if (SelectedMode == StockAdjustmentMode.Decrease &&
            !CanSourceSupplyAdjustment(out var sourceMessage))
        {
            ShowInfo(sourceMessage);
            return false;
        }

        return true;
    }

    public bool CanApplyAdjustment
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SelectedReasonCode))
                return false;

            if (string.Equals(
                    SelectedReasonCode,
                    "OTHER",
                    StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(Remarks))
                return false;

            if (IsReallocationMode)
                return TryValidateReallocationPreview(out _);

            if (string.IsNullOrWhiteSpace(SelectedCategoryId) ||
                string.IsNullOrWhiteSpace(SelectedProductSku) ||
                ProductStockSummary.GKey <= 0)
                return false;

            if (!HasSourceSku && !IsSourceConsolidated)
                return false;

            if (HasSourceSku &&
                (ProductStock.GKey <= 0 ||
                 ProductStock.ProductGkey.GetValueOrDefault() <= 0))
                return false;

            if (IsSourceConsolidated &&
                ProductStockSummary.ProductGkey.GetValueOrDefault() <= 0)
                return false;

            if (!TryValidateWeights(out _))
                return false;

            if (SelectedMode == StockAdjustmentMode.Decrease &&
                !CanSourceSupplyAdjustment(out _))
                return false;

            return true;
        }
    }

    [RelayCommand]
    private async Task ApplyAdjustment()
    {
        if (!Validate() || !CanApplyAdjustment)
            return;

        try
        {
            var request = BuildCreateRequest();

            var result =
                await _stockAdjustmentService.CreateAsync(request);

            AdjustmentNumber = result.AdjustmentNbr;

            _messageBoxService.ShowMessage(
                $"Stock adjustment {result.AdjustmentNbr} was posted successfully.",
                "Stock Adjustment",
                MessageButton.OK,
                MessageIcon.Information);

            ResetSAN();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(
                ex,
                "Unable to post stock adjustment. Mode: {Mode}, Category: {Category}, SKU: {Sku}",
                SelectedMode,
                SelectedCategoryId,
                SelectedProductSku);

            _messageBoxService.ShowMessage(
                "Unable to complete the stock adjustment. " +
                "Please review the selected stock and try again.\n\n" +
                ex.Message,
                "Stock Adjustment",
                MessageButton.OK,
                MessageIcon.Warning);
        }
    }

    private CreateStockAdjustmentRequest BuildCreateRequest()
    {
        var request = new CreateStockAdjustmentRequest
        {
            AdjustmentDate = AdjustmentDate,
            AdjustmentType = SelectedMode switch
            {
                StockAdjustmentMode.Increase =>
                    StockAdjustmentTypes.Increase,

                StockAdjustmentMode.Decrease =>
                    StockAdjustmentTypes.Decrease,

                StockAdjustmentMode.Reallocation =>
                    StockAdjustmentTypes.Reallocation,

                _ => throw new InvalidOperationException(
                    $"Unsupported stock adjustment mode: {SelectedMode}.")
            },
            ReasonCode = SelectedReasonCode!.Trim(),
            Remarks = string.IsNullOrWhiteSpace(Remarks)
                ? null
                : Remarks.Trim()
        };

        if (IsReallocationMode)
        {
            request.Lines.Add(
                BuildSourceLine(
                    lineNbr: 1,
                    direction: StockAdjustmentDirections.Out,
                    pairNbr: 1));

            request.Lines.Add(
                BuildDestinationLine(
                    lineNbr: 2,
                    direction: StockAdjustmentDirections.In,
                    pairNbr: 1));

            return request;
        }

        request.Lines.Add(
            BuildSourceLine(
                lineNbr: 1,
                direction:
                    IsIncreaseMode
                        ? StockAdjustmentDirections.In
                        : StockAdjustmentDirections.Out,
                pairNbr: null));

        return request;
    }

    private CreateStockAdjustmentLineRequest BuildSourceLine(
        int lineNbr,
        string direction,
        int? pairNbr)
    {
        var productGkey =
            HasSourceSku
                ? ProductStock.ProductGkey.GetValueOrDefault()
                : ProductStockSummary.ProductGkey.GetValueOrDefault();

        if (productGkey <= 0)
        {
            throw new InvalidOperationException(
                "The selected source stock does not contain a valid product reference.");
        }

        return new CreateStockAdjustmentLineRequest
        {
            LineNbr = lineNbr,
            Direction = direction,
            StockLevel =
                HasSourceSku
                    ? StockAdjustmentStockLevels.Sku
                    : StockAdjustmentStockLevels.Consolidated,
            MovementKind = StockAdjustmentMovementKinds.Weight,
            ProductGkey = productGkey,
            ProductStockGkey =
                HasSourceSku
                    ? ProductStock.GKey
                    : null,
            ProductSku =
                HasSourceSku
                    ? ProductStock.ProductSku
                    : null,
            ProductCategory = GetSourceStockCategory(),
            Qty = 0,
            GrossWeight = AdjustmentGrossWeight.GetValueOrDefault(),
            StoneWeight = AdjustmentStoneWeight.GetValueOrDefault(),
            NetWeight = AdjustmentNetWeight,
            PairNbr = pairNbr,
            Notes = Remarks
        };
    }

    private CreateStockAdjustmentLineRequest BuildDestinationLine(
        int lineNbr,
        string direction,
        int? pairNbr)
    {
        var productGkey =
            HasDestinationSku
                ? ToProductStock.ProductGkey.GetValueOrDefault()
                : ToProductStockSummary.ProductGkey.GetValueOrDefault();

        if (productGkey <= 0)
        {
            throw new InvalidOperationException(
                "The selected destination stock does not contain a valid product reference.");
        }

        return new CreateStockAdjustmentLineRequest
        {
            LineNbr = lineNbr,
            Direction = direction,
            StockLevel =
                HasDestinationSku
                    ? StockAdjustmentStockLevels.Sku
                    : StockAdjustmentStockLevels.Consolidated,
            MovementKind = StockAdjustmentMovementKinds.Weight,
            ProductGkey = productGkey,
            ProductStockGkey =
                HasDestinationSku
                    ? ToProductStock.GKey
                    : null,
            ProductSku =
                HasDestinationSku
                    ? ToProductStock.ProductSku
                    : null,
            ProductCategory = GetDestinationStockCategory(),
            Qty = 0,
            GrossWeight = AdjustmentGrossWeight.GetValueOrDefault(),
            StoneWeight = AdjustmentStoneWeight.GetValueOrDefault(),
            NetWeight = AdjustmentNetWeight,
            PairNbr = pairNbr,
            Notes = Remarks
        };
    }

    private string GetSourceStockCategory()
    {
        var category =
            HasSourceSku
                ? ProductStock.Category
                : ProductStockSummary.Category;

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new InvalidOperationException(
                "The selected source stock does not contain a stock category.");
        }

        return category;
    }

    private string GetDestinationStockCategory()
    {
        var category =
            HasDestinationSku
                ? ToProductStock.Category
                : ToProductStockSummary.Category;

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new InvalidOperationException(
                "The selected destination stock does not contain a stock category.");
        }

        return category;
    }

    [RelayCommand]
    private void ResetSAN()
    {
        SetBase();
        SelectedCategoryId = null;
        SelectedProductSku = null;
        ToSelectedCategoryId = null;
        ToSelectedProductSku = null;
        SelectedReasonCode = null;
        Remarks = null;
        AdjustmentGrossWeight = null;
        AdjustmentStoneWeight = null;
        AdjustmentDate = DateTime.Today;
        AdjustmentNumber = "NEW";
        SelectedMode = StockAdjustmentMode.Increase;
        ProductSkuStrList = new ObservableCollection<string>();
        ToProductSkuStrList = new ObservableCollection<string>();
        NotifyUiState();
    }

    [RelayCommand]
    private void ReprintTag()
    {
        // Tag reprint remains outside the Stock Adjustment work.
    }

    private void ShowInfo(string message)
    {
        _messageBoxService.ShowMessage(
            message,
            "Stock Adjustment",
            MessageButton.OK,
            MessageIcon.Information);
    }

    private void NotifySourceSelectionChanged()
    {
        OnPropertyChanged(nameof(HasSourceSku));
        OnPropertyChanged(nameof(IsSourceConsolidated));
        OnPropertyChanged(nameof(SourceStockLevelText));
        NotifyUiState();
    }

    private void NotifyDestinationSelectionChanged()
    {
        OnPropertyChanged(nameof(HasDestinationSku));
        OnPropertyChanged(nameof(IsDestinationConsolidated));
        OnPropertyChanged(nameof(DestinationStockLevelText));
        NotifyUiState();
    }

    private void NotifyUiState()
    {
        OnPropertyChanged(nameof(CanApplyAdjustment));
        OnPropertyChanged(nameof(IsReallocationPreviewValid));
        OnPropertyChanged(nameof(ValidationMessage));
        OnPropertyChanged(nameof(AdjustmentNetWeight));
    }

    private static bool IsConsolidatedSelection(string? value) =>
        string.Equals(value, ConsolidatedStockOption, StringComparison.Ordinal);
}
