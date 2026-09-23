using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Printing;
using DevExpress.XtraGauges.Core.Styles;
using DevExpress.XtraRichEdit.Forms;
using InvEntry.Extension;
using InvEntry.Helpers;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Services.Printing;
using InvEntry.Store;
using InvEntry.Utils;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using IDialogService = DevExpress.Mvvm.IDialogService;


namespace InvEntry.ViewModels
{
    public partial class ProductStockEntryViewModel : ObservableObject
    {

        [ObservableProperty]
        private string _supplierID;

        [ObservableProperty]
        private DateSearchOption _searchOption;

        [ObservableProperty]
        private DateTime _Today = DateTime.Today;

        [ObservableProperty]
        private bool _isPrintingTag;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private string _errorPanelTitle;

        [ObservableProperty]
        private string? _errorPanelMessage;

        [ObservableProperty]
        private string? _simulationLabelPreview;

        public bool IsLabelPrintSimulation => _labelPrinter is SimulatedLabelPrinter;
        [ObservableProperty]
        private System.Windows.Media.Imaging.BitmapSource? _labelPreviewImage;

        [ObservableProperty]
        private string? _labelPreviewZpl;

        [ObservableProperty]
        private bool _hasLabelPreview;
        [ObservableProperty]
        private int _selectedWorkflowTabIndex;

        [ObservableProperty]
        private int _totalQuantity;

        [ObservableProperty]
        private int _completedQuantity;

        [ObservableProperty]
        private int _pendingQuantity;

        [ObservableProperty]
        private bool _canContinueToWeighing;

        [ObservableProperty]
        private bool _isWeighingTabEnabled;

        [ObservableProperty]
        private bool _isWorkflowComplete;

        [ObservableProperty]
        private ObservableCollection<GrnLine> _completedItems = new();

        public string ProgressText => TotalQuantity <= 0
            ? "No item selected"
            : $"{CompletedQuantity} of {TotalQuantity} completed";

        private readonly IGrnService _grnService;
        private readonly IProductCategoryService _productCategoryService;
        private readonly IProductViewService _productViewService;
        private readonly IProductTransactionService _productTransactionService;
        private readonly IProductStockService _productStockService;
        private readonly IMessageBoxService _messageBoxService;
        private readonly IDialogService _dialogService;
        private readonly IMtblReferencesService _mtblReferencesService;
        private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;

        [ObservableProperty]
        private ObservableCollection<string> _supplierReferencesList;

        [ObservableProperty]
        private ObservableCollection<GrnHeader> _grnHdrList;

        [ObservableProperty]
        private ObservableCollection<ProductStock> _productStockList;

        [ObservableProperty]
        private ObservableCollection<GrnLine> _grnLineList;

        [ObservableProperty]
        private OrgThisCompanyView _company;

        [ObservableProperty]
        private Product _product;

        [ObservableProperty]
        private ObservableCollection<GrnLineSummary> _grnLineSumryList;

        [ObservableProperty]
        private GrnLine _SelectedGrnLine;

        [ObservableProperty]
        private GrnLineSummary _SelectedGrnLineSumry;

        [ObservableProperty]
        private GrnHeader _SelectedGrn;

        private WeighScaleReader _scaleReader;

        private MtblReference mtblReference;
        private bool isTagPrinted;
        private bool isPrintEnabled = true;
        private decimal _capturedWeight;
        private bool isManualMode;

        private WeighScaleReaderAuto reader;

        private Dictionary<int, ObservableCollection<GrnLine>> _lineGrnLookup;
        private readonly Dictionary<int, ProductStock> _pendingStockByGkey = new();
        private readonly Dictionary<GrnLine, int> _stockGkeyByLine = new();
        private Dictionary<string, Action<GrnLine, decimal?>> copyGRNLineExpression;
        private Dictionary<string, Action<GrnLineSummary, decimal?>> copyGRNLineSumryExpression;
        private readonly ILabelPrinter _labelPrinter;
        private readonly ILabelPreviewRenderer _labelPreviewRenderer;

        public ProductStockEntryViewModel(IGrnService grnService,
                                            IProductViewService productViewService,
                                            IProductTransactionService productTransactionService,
                                            IProductStockService productStockService,
                                            IDialogService dialogService,
                                            IProductCategoryService productCategoryService,
                                            IOrgThisCompanyViewService orgThisCompanyViewService,
                                            IMessageBoxService messageBoxService,
                                            IMtblReferencesService mtblReferencesService,
                                            ILabelPrinter labelPrinter,
                                            ILabelPreviewRenderer labelPreviewRenderer)
        {
            _grnService = grnService;
            _productViewService = productViewService;
            _productStockService = productStockService;
            _productTransactionService = productTransactionService;
            _dialogService = dialogService;
            _productCategoryService = productCategoryService;
            _messageBoxService = messageBoxService;
            _mtblReferencesService = mtblReferencesService;
            _orgThisCompanyViewService = orgThisCompanyViewService;
            _labelPrinter = labelPrinter;
            _labelPreviewRenderer = labelPreviewRenderer;

            _lineGrnLookup = new();

            SetThisCompany();

            PopulateMtblSupplierListAsync();
            PopulateOpenGRN();
            PopulateUnboundLineDataMap();
            PopulateUnboundLineSummaryDataMap();

        }

        private async void SetThisCompany()
        {
            Company = new();
            Company = await _orgThisCompanyViewService.GetOrgThisCompany();
            //Header.TenantGkey = Company.TenantGkey;
        }

        private async void PopulateMtblSupplierListAsync()
        {

            var suppRefServiceList = await _mtblReferencesService.GetReferenceList("SUPPLIERS");

            SupplierReferencesList = new(suppRefServiceList.Select(x => x.RefValue));

        }

        [RelayCommand]
        private void ValidateCell(GridCellValidationEventArgs e)
        {
            if (e.Column.FieldName == nameof(GrnLine.StoneWeight))
            {
                if (e.Value is decimal stoneWeight)
                {
                    var item = (GrnLine)e.Row;
                    if (stoneWeight < 0)
                        e.SetError("Stone weight cannot be negative.");
                    else if (stoneWeight > item.GrossWeight)
                        e.SetError("Stone weight cannot exceed total weight.");
                }
            }
        }


        private async void PopulateOpenGRN()
        {
            //bring all 'Open' status grn headers
            var grnHdrList = await _grnService.GetBySupplier(SupplierID);
        }


        [RelayCommand]
        private async Task SelectedGRN()
        {
            var grnLineList = await _grnService.GetByHdrGkey(SelectedGrn.GKey);

            var grnHeader = await _grnService.GetByHdrGkey(SelectedGrn.GKey);

        }

        [RelayCommand]
        private async Task CaptureScaleWeightAsync()
        {
            if (SelectedGrnLine is null || isManualMode)
                return;

            var waitVM = WaitIndicatorVM.ShowIndicator("Reading weight from scale...");
            SplashScreenManager.CreateWaitIndicator(waitVM).Show();

            try
            {
                var scaleReader = new WeighScaleReaderAuto();
                var weight = await scaleReader.StartManualAsync();
                scaleReader.Stop();

                if (weight < 0)
                {
                    ShowError("Weighing machine error",
                        "Unable to read the weight. Please check the weighing-machine connection.");
                    return;
                }

                InvalidateLabelPreview();
                SelectedGrnLine.GrossWeight = weight;
                SelectedGrnLine.StoneWeight = 0;
                EvaluateGrnLine(SelectedGrnLine);
            }
            finally
            {
                SplashScreenManager.ActiveSplashScreens
                    .FirstOrDefault(x => x.ViewModel == waitVM)?.Close();
            }
        }

        [RelayCommand]
        private void CalculateCurrentWeights()
        {
            InvalidateLabelPreview();
            if (SelectedGrnLine is not null)
                EvaluateGrnLine(SelectedGrnLine);
        }

        [RelayCommand]
        private async Task PreviewTagAsync()
        {
            ClearErrors();
            var validationErrors = ValidateTagForPrinting(SelectedGrnLine);
            if (validationErrors.Count > 0)
            {
                ShowErrors("Unable to preview label", validationErrors);
                return;
            }

            try
            {
                var line = SelectedGrnLine!;
                int stockGkey = line.ProductStockGkey!.Value;
                var productStock = await ResolvePendingStockAsync(line, stockGkey);
                line.ProductSku = productStock.ProductSku;
                if (string.IsNullOrWhiteSpace(line.ProductSku) ||
                    line.ProductSku.StartsWith("TMP-", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "The current item does not yet have a reserved permanent SKU. " +
                        "Return to selection and continue to weighing again.");
                }

                var request = CreateLabelPrintRequest(line);
                var preview = _labelPreviewRenderer.Render(request);

                LabelPreviewZpl = preview.Zpl;
                LabelPreviewImage = preview.Image;
                HasLabelPreview = true;
                SimulationLabelPreview = IsLabelPrintSimulation
                    ? $"Simulation preview for {productStock.ProductSku}; no physical label was printed."
                    : null;
            }
            catch (Exception ex)
            {
                InvalidateLabelPreview();
                ShowError("Unable to preview label", ex.Message);
            }
        }

        private void InvalidateLabelPreview()
        {
            LabelPreviewImage = null;
            LabelPreviewZpl = null;
            HasLabelPreview = false;
        }
        [RelayCommand]
        private void ClearErrors()
        {
            HasError = false;
            ErrorPanelTitle = null;
            ErrorPanelMessage = null;
        }

        private void ShowError(string title, string? message)
        {
            ShowErrors(title, new[] { message });
        }

        private void ShowErrors(
    string title,
    IEnumerable<string?> messages)
        {
            var errors = messages
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Select(message => message!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (errors.Count == 0)
                errors.Add("An unknown error occurred.");

            ErrorPanelTitle = string.IsNullOrWhiteSpace(title)
                ? "Error"
                : title;

            ErrorPanelMessage = errors.Count switch
            {
                1 => errors[0],

                _ => string.Join(
                    Environment.NewLine,
                    errors.Select(
                        (error, index) => $"{index + 1}. {error}"))
            };

            HasError = true;
        }

        private IReadOnlyList<string> ValidateTagForPrinting(GrnLine? grnLine)
        {
            var errors = new List<string>();

            if (grnLine is null)
            {
                errors.Add("Please select a GRN line before printing.");
                return errors;
            }

            if (SelectedGrn is null)
                errors.Add("Please select a GRN before printing.");

            if (Company is null)
                errors.Add("Company details have not been loaded.");

            if (mtblReference is null)
                errors.Add("The product SKU sequence reference has not been loaded.");

            if (!grnLine.ProductStockGkey.HasValue || grnLine.ProductStockGkey.Value <= 0)
                errors.Add("The selected row is not associated with pending stock.");

            if (!grnLine.GrossWeight.HasValue || grnLine.GrossWeight.Value <= 0m)
                errors.Add("Gross weight must be greater than zero.");

            if (!grnLine.NetWeight.HasValue || grnLine.NetWeight.Value <= 0m)
                errors.Add("Net weight must be greater than zero.");

            if (grnLine.StoneWeight.GetValueOrDefault() < 0m)
                errors.Add("Stone weight cannot be negative.");

            if (grnLine.StoneWeight.GetValueOrDefault() >
                grnLine.GrossWeight.GetValueOrDefault())
            {
                errors.Add("Stone weight cannot exceed gross weight.");
            }

            if (grnLine.SuppVaPercent.GetValueOrDefault() < 0m)
                errors.Add("VA percentage cannot be negative.");

            return errors;
        }

        [RelayCommand]
        private async Task PrintTagAsync(GrnLine? grnLine)
        {
            if (IsPrintingTag)
                return;

            ClearErrors();

            var validationErrors = ValidateTagForPrinting(grnLine);
            if (validationErrors.Count > 0)
            {
                ShowErrors("Unable to print label", validationErrors);
                return;
            }

            var line = grnLine!;
            IsPrintingTag = true;

            try
            {
                int stockGkey = line.ProductStockGkey!.Value;
                await ResolveAndReserveStockAsync(line);
                var request = CreateLabelPrintRequest(line);

                if (IsLabelPrintSimulation)
                {
                    var preview = _labelPreviewRenderer.Render(request);
                    LabelPreviewZpl = preview.Zpl;
                    LabelPreviewImage = preview.Image;
                    HasLabelPreview = true;
                    SimulationLabelPreview =
                        $"Simulation preview for {request.ProductCode}; no physical label was printed.";
                }

                var printResult = await _labelPrinter.PrintAsync(request);
                if (!printResult.Success)
                {
                    SelectedGrnLine = line;
                    ShowError(
                        "Label printing failed",
                        printResult.ErrorMessage ?? "The printer did not accept the label.");
                    return;
                }

                var completedLineNumber = line.LineNbr.GetValueOrDefault();
                var finalizedStock = await ProcessStockLinesAsync(line);

                try
                {
                    var existingLine = await _grnService.GetByProductSku(line.ProductSku);
                    if (existingLine is null)
                    {
                        line.GrnHdrGkey = SelectedGrn!.GKey;
                        line.Status = "Closed";
                        await _grnService.CreateGrnLine(line);
                    }
                }
                catch
                {
                    // The label has printed, but the GRN line did not persist. Keep the
                    // same reserved SKU pending so a retry/reconciliation cannot create
                    // another stock row or allocate another SKU.
                    finalizedStock.Status = "Pending Tag";
                    finalizedStock.IsBarcodePrinted = false;
                    finalizedStock.ModifiedOn = DateTime.Now;
                    await _productStockService.UpdateProductStock(finalizedStock);
                    _pendingStockByGkey[stockGkey] = finalizedStock;
                    throw;
                }

                line.IsPrinted = true;
                InvalidateLabelPreview();
                _pendingStockByGkey.Remove(stockGkey);
                _stockGkeyByLine.Remove(line);
                GrnLineList.Remove(line);
                CompletedItems.Add(line);
                CompletedQuantity = CompletedItems.Count;
                PendingQuantity = GrnLineList.Count;
                CanContinueToWeighing = PendingQuantity > 0;
                IsWorkflowComplete = PendingQuantity == 0;
                OnPropertyChanged(nameof(ProgressText));
                SelectNextPrintableLine(completedLineNumber);

                if (IsWorkflowComplete)
                    SelectedGrnLine = null;
            }
            catch (Exception ex)
            {
                SelectedGrnLine = line;
                System.Diagnostics.Debug.WriteLine(ex);
                ShowErrors(
                    "Label operation failed",
                    new[]
                    {
                        "The label operation could not be completed.",
                        ex.Message
                    });
            }
            finally
            {
                IsPrintingTag = false;
            }
        }
        private async Task<ProductStock> ResolveAndReserveStockAsync(GrnLine line)
        {
            int stockGkey = line.ProductStockGkey.GetValueOrDefault();
            if (stockGkey <= 0)
                throw new InvalidOperationException("The selected item has no pending ProductStock identity.");

            var productStock = await ResolvePendingStockAsync(line, stockGkey);
            if (string.IsNullOrWhiteSpace(productStock.ProductSku) ||
                productStock.ProductSku.StartsWith("TMP-", StringComparison.OrdinalIgnoreCase))
            {
                productStock = await _productStockService.ReserveProductSku(stockGkey);
                _pendingStockByGkey[stockGkey] = productStock;
            }

            line.ProductSku = productStock.ProductSku;
            if (string.IsNullOrWhiteSpace(line.ProductSku))
                throw new InvalidOperationException("A permanent product SKU could not be reserved.");

            return productStock;
        }

        private LabelPrintRequest CreateLabelPrintRequest(GrnLine line) => new(
            line.ProductSku!,
            line.ProductDesc ?? string.Empty,
            line.SuppVaPercent.GetValueOrDefault(),
            line.NetWeight!.Value,
            line.StoneWeight.GetValueOrDefault(),
            line.ProductPurity ?? string.Empty,
            Company!.CompanyName ?? "MATHA");
        private void SelectNextPrintableLine(int completedLineNumber)
        {
            if (GrnLineList is null || GrnLineList.Count == 0)
                return;

            // Do not assume that line numbers are continuous.
            var nextLine = GrnLineList
                .Where(line => !line.IsPrinted && line.LineNbr > completedLineNumber)
                .OrderBy(line => line.LineNbr)
                .FirstOrDefault();

            if (nextLine is not null)
                SelectedGrnLine = nextLine;
        }

        private void OnCellValueChanged(CellValueChangedEventArgs e)
        {
            if (e.Column.FieldName == "GrossWeight" && e.Value != null)
            {
                if (decimal.TryParse(e.Value.ToString(), out var weight))
                {
                    // PrintWeightDetails(weight); // your processing logic
                }
            }
        }


        [RelayCommand]
        private async Task SelectionGrnSumryListChanged()
        {
            ClearErrors();
            ResetWorkstationState();

            if (SelectedGrnLineSumry is null)
                return;

            var pendingStocks = (await _productStockService
                    .GetPendingByGrnLineSummary(SelectedGrnLineSumry.GKey))
                .OrderBy(stock => stock.GKey)
                .ToList();
            var completedLines = (await _grnService.GetByLineSumryGkey(
                    SelectedGrnLineSumry.GKey,
                    SelectedGrnLineSumry.GrnHdrGkey.GetValueOrDefault()))
                .OrderBy(line => line.LineNbr)
                .ToList();

            TotalQuantity = SelectedGrnLineSumry.SuppliedQty.GetValueOrDefault();
            PendingQuantity = pendingStocks.Count;
            CompletedQuantity = completedLines.Count;
            CompletedItems = new(completedLines);
            CanContinueToWeighing = SelectedGrn is not null && PendingQuantity > 0;
            IsWorkflowComplete = TotalQuantity > 0 && PendingQuantity == 0;
            OnPropertyChanged(nameof(ProgressText));
        }

        [RelayCommand]
        private async Task ContinueToWeighingAsync()
        {
            if (!CanContinueToWeighing || SelectedGrnLineSumry is null)
                return;

            var result = _messageBoxService.ShowMessage(
                "Do you want to print in AUTO mode?",
                "Confirmation",
                MessageButton.YesNoCancel,
                MessageIcon.Question);

            if (result == MessageResult.Cancel)
                return;

            isManualMode = result == MessageResult.No;
            await LoadWorkstationAsync();

            if (PendingQuantity > 0)
            {
                IsWeighingTabEnabled = true;
                SelectedWorkflowTabIndex = 1;
            }
        }

        private async Task LoadWorkstationAsync()
        {
            ClearErrors();
            ResetWorkstationState();

            if (SelectedGrnLineSumry is null)
                return;

            string? category = SelectedGrnLineSumry.ProductCategory;
            if (string.IsNullOrWhiteSpace(category))
                return;

            mtblReference = await _mtblReferencesService.GetReference(
                "PRODUCT_CATEGORY", category);
            if (mtblReference is null)
            {
                ShowError("Unable to load pending stock",
                    $"SKU sequence is not configured for category '{category}'.");
                return;
            }

            var productView = await _productViewService.GetProduct(category);
            if (productView is null)
            {
                ShowError("Unable to load pending stock",
                    $"Product details were not found for category '{category}'.");
                return;
            }

            var pendingStocks = (await _productStockService
                    .GetPendingByGrnLineSummary(SelectedGrnLineSumry.GKey))
                .OrderBy(stock => stock.GKey)
                .ToList();
            var completedLines = (await _grnService.GetByLineSumryGkey(
                    SelectedGrnLineSumry.GKey,
                    SelectedGrnLineSumry.GrnHdrGkey.GetValueOrDefault()))
                .OrderBy(line => line.LineNbr)
                .ToList();

            ProductStockList = new(pendingStocks);
            CompletedItems = new(completedLines);
            int nextLineNumber = completedLines.Select(x => x.LineNbr.GetValueOrDefault())
                .DefaultIfEmpty(0).Max();

            foreach (var pendingStock in pendingStocks)
            {
                var stock = pendingStock;
                if (string.IsNullOrWhiteSpace(stock.ProductSku) ||
                    stock.ProductSku.StartsWith("TMP-", StringComparison.OrdinalIgnoreCase))
                {
                    stock = await _productStockService.ReserveProductSku(stock.GKey);
                }
                var line = new GrnLine
                {
                    GrnHdrGkey = SelectedGrnLineSumry.GrnHdrGkey,
                    ProductId = category,
                    ProductGkey = stock.ProductGkey ?? SelectedGrnLineSumry.ProductGkey,
                    LineNbr = ++nextLineNumber,
                    ProductDesc = productView.Description,
                    ProductPurity = productView.Purity,
                    SuppVaPercent = productView.VaPercent,
                    GrnLineSumryGkey = SelectedGrnLineSumry.GKey,
                    ProductStockGkey = stock.GKey,
                    ProductSku = !string.IsNullOrWhiteSpace(stock.ProductSku) &&
                                 !stock.ProductSku.StartsWith("TMP-", StringComparison.OrdinalIgnoreCase)
                        ? stock.ProductSku
                        : null,
                    GrossWeight = stock.GrossWeight,
                    StoneWeight = stock.StoneWeight.GetValueOrDefault(),
                    NetWeight = stock.NetWeight
                };

                GrnLineList.Add(line);
                _pendingStockByGkey[stock.GKey] = stock;
                _stockGkeyByLine[line] = stock.GKey;
            }

            TotalQuantity = SelectedGrnLineSumry.SuppliedQty.GetValueOrDefault();
            PendingQuantity = pendingStocks.Count;
            CompletedQuantity = completedLines.Count;
            CanContinueToWeighing = PendingQuantity > 0;
            IsWorkflowComplete = TotalQuantity > 0 && PendingQuantity == 0;
            SelectedGrnLine = GrnLineList.FirstOrDefault();
            OnPropertyChanged(nameof(ProgressText));
        }

        [RelayCommand]
        private async Task BackToSelectionAsync()
        {
            SelectedWorkflowTabIndex = 0;
            IsWeighingTabEnabled = false;
            ResetWorkstationState();
            await SelectionGrnSumryListChanged();
        }

        private void ResetWorkstationState()
        {
            _pendingStockByGkey.Clear();
            _stockGkeyByLine.Clear();
            GrnLineList = new();
            ProductStockList = new();
            SelectedGrnLine = null;
            InvalidateLabelPreview();
        }
        partial void OnSelectedGrnLineChanged(GrnLine value)
        {
            InvalidateLabelPreview();
        }
        partial void OnSelectedGrnLineSumryChanged(GrnLineSummary oldValue, GrnLineSummary newValue)
        {
            _pendingStockByGkey.Clear();
            _stockGkeyByLine.Clear();
        }

        partial void OnSelectedGrnChanged(GrnHeader oldValue, GrnHeader newValue)
        {
            if (oldValue is not null && newValue is not null && oldValue.GKey == newValue.GKey)
                return;

            if (GrnLineList is not null && GrnLineList.Any() && SelectedGrnLineSumry is not null)
            {
                //"Do you want discard?"

                _lineGrnLookup[SelectedGrnLineSumry.GKey] = GrnLineList;
            }
        }

        [RelayCommand]
        private async Task SelectionGRNChanged()
        {
            ClearErrors();
            _pendingStockByGkey.Clear();
            _stockGkeyByLine.Clear();
            GrnLineList = new();
            ProductStockList = new();

            SelectedGrnLineSumry = null;
            TotalQuantity = 0;
            CompletedQuantity = 0;
            PendingQuantity = 0;
            CanContinueToWeighing = false;
            IsWorkflowComplete = false;
            IsWeighingTabEnabled = false;
            SelectedWorkflowTabIndex = 0;
            CompletedItems = new();
            OnPropertyChanged(nameof(ProgressText));

            if (SelectedGrn is null) return;

            var grnLineListSumryResult = await _grnService.GetBySumryHdrGkey(SelectedGrn.GKey);

            if (grnLineListSumryResult is not null)
                GrnLineSumryList = new(grnLineListSumryResult);

            //var grnLineListResult = await _grnService.GetByHdrGkey(SelectedGrn.GKey);
            //if (grnLineListResult is not null)
            //    GrnLineList = new(grnLineListResult);
        }

        [RelayCommand]
        private async Task RefreshGRN()
        {

            var grnResult = await _grnService.GetBySupplier(SupplierID);
            if (grnResult is not null)
                GrnHdrList = new(grnResult);
        }

        [RelayCommand]
        private void SelectionGRNListChanged()
        {
            var count = SelectedGrnLine.SuppliedQty;
            for (int i = 1; i <= count; i++)
            {

            }

        }

        private async Task SavingGrnLinesList()
        {

            foreach (var keyValue in _lineGrnLookup)
            {
                await SavingGrnLine(keyValue.Value);
            }

            _lineGrnLookup.Clear();
        }

        private async Task SavingGrnLine(ObservableCollection<GrnLine> grnLines)
        {

            if (grnLines is null || !grnLines.Any()) return;

            grnLines.ForEach(async x =>
            {
                if (x.NetWeight.HasValue && x.NetWeight > 0 && x.ProductSku is not null)
                {
                    x.GrnHdrGkey = SelectedGrn.GKey;

                    /*                    if (x.ProductSku is null)
                                        {
                                            return;
                                        } else*/
                    {
                        x.Status = "Closed";
                        _ = ProcessStockLinesAsync(x);

                        var grnLineChk = await _grnService.GetByProductSku(x.ProductSku);
                        if (grnLineChk is null)
                        {
                            await _grnService.CreateGrnLine(grnLines);
                        }
                    }
                }
            });

            //if user maintains seq nbr for product sku - this nees to be executed - but in difference place - need to fix
            await _mtblReferencesService.UpdateReference(mtblReference);



        }


        [RelayCommand]
        private async Task Submit()
        {
            if (SelectedGrn is null)
                return;

            var summaries = (await _grnService.GetBySumryHdrGkey(SelectedGrn.GKey)).ToList();
            int remainingCount = 0;

            foreach (var summary in summaries)
            {
                remainingCount += (await _productStockService
                        .GetPendingByGrnLineSummary(summary.GKey))
                    .Count();
            }

            if (remainingCount > 0)
            {
                ShowError(
                    "GRN cannot be closed",
                    $"{remainingCount} item(s) still require weighing and tagging.");
                return;
            }

            _lineGrnLookup.Clear();
            _pendingStockByGkey.Clear();
            _stockGkeyByLine.Clear();

            SelectedGrn.Status = "Closed";
            await _grnService.UpdateHeader(SelectedGrn);

            if (GrnHdrList.Contains(SelectedGrn))
                GrnHdrList.Remove(SelectedGrn);

            GrnLineList.Clear();
            GrnLineSumryList.Clear();
            ProductStockList.Clear();

            _messageBoxService.ShowMessage(
                "Stock Updated Successfully",
                "Stock Created",
                MessageButton.OK,
                MessageIcon.Exclamation);
        }
        private async void CreateProductTransaction(ProductStock productStock)
        {
            ProductTransaction productTransaction = new();

            //Get previous record closing balance to set this record opening - if not found set opening to zero
            var productTrans = await _productTransactionService.GetLastProductTransactionBySku(productStock.ProductSku);
            if (productTrans != null)
            {
                productTransaction.OpeningGrossWeight = productTrans.ClosingGrossWeight;
                productTransaction.OpeningStoneWeight = productTrans.ClosingStoneWeight;
                productTransaction.OpeningNetWeight = productTrans.ClosingNetWeight;

            }
            else
            {
                productTransaction.OpeningGrossWeight = 0;
                productTransaction.OpeningStoneWeight = 0;
                productTransaction.OpeningNetWeight = 0;
            }

            productTransaction.ProductSku = productStock.ProductSku;
            productTransaction.RefGkey = productStock.GKey;
            productTransaction.TransactionDate = DateTime.Now;
            productTransaction.ProductCategory = productStock.Category;

            productTransaction.TransactionType = "Receipt";
            productTransaction.DocumentNbr = SelectedGrn.GrnNbr;
            productTransaction.DocumentDate = SelectedGrn.GrnDate;
            productTransaction.DocumentType = "GRN";
            productTransaction.VoucherType = "Stock Receipt";

            productTransaction.ObQty = 0;
            productTransaction.TransactionQty = productStock.StockQty;
            productTransaction.CbQty = productStock.SuppliedQty;

            productTransaction.TransactionGrossWeight = productStock.GrossWeight;
            productTransaction.TransactionStoneWeight = productStock.StoneWeight;
            productTransaction.TransactionNetWeight = productStock.NetWeight;

            productTransaction.ClosingGrossWeight = productTransaction.OpeningGrossWeight + productStock.GrossWeight;
            productTransaction.ClosingStoneWeight = productTransaction.OpeningStoneWeight + productStock.StoneWeight;
            productTransaction.ClosingNetWeight = productTransaction.OpeningNetWeight + productStock.NetWeight;

            await _productTransactionService.CreateProductTransaction(productTransaction);
        }

        private async Task<ProductStock> ProcessStockLinesAsync(GrnLine grnLineStock)
        {
            int stockGkey = grnLineStock.ProductStockGkey.GetValueOrDefault();
            if (stockGkey <= 0)
                throw new InvalidOperationException("Pending stock association was not found.");

            var productStock = await _productStockService.GetProductStock(stockGkey);
            if (productStock is null)
                throw new InvalidOperationException($"Product stock {stockGkey} was not found.");

            productStock.ProductSku = grnLineStock.ProductSku;
            productStock.GrossWeight = grnLineStock.GrossWeight;
            productStock.StoneWeight = grnLineStock.StoneWeight;
            productStock.NetWeight = grnLineStock.NetWeight;
            productStock.SuppliedGrossWeight = grnLineStock.GrossWeight;
            productStock.BalanceWeight = grnLineStock.NetWeight;
            productStock.SuppliedQty = 1;
            productStock.StockQty = 1;
            productStock.SoldQty = 0;
            productStock.Status = "In-Stock";
            productStock.IsBarcodePrinted = true;
            productStock.IsProductSold = false;
            productStock.ModifiedOn = DateTime.Now;

            await _productStockService.UpdateProductStock(productStock);
            _pendingStockByGkey[stockGkey] = productStock;

            return productStock;
        }
        private async Task<ProductStock> ResolvePendingStockAsync(
            GrnLine line,
            int stockGkey)
        {
            if (!_pendingStockByGkey.TryGetValue(stockGkey, out var productStock))
                productStock = await _productStockService.GetProductStock(stockGkey);

            string? rejectionReason = GetPendingStockRejectionReason(
                productStock,
                line,
                stockGkey);

            if (rejectionReason is not null)
                throw new InvalidOperationException(rejectionReason);

            _pendingStockByGkey[stockGkey] = productStock!;
            _stockGkeyByLine[line] = stockGkey;
            return productStock!;
        }

        private static string? GetPendingStockRejectionReason(
            ProductStock? productStock,
            GrnLine line,
            int requestedStockGkey)
        {
            if (productStock is null)
                return $"Pending ProductStock GKEY {requestedStockGkey} was not found.";

            if (productStock.GKey != requestedStockGkey)
            {
                return $"ProductStock lookup returned GKEY {productStock.GKey} " +
                       $"instead of requested GKEY {requestedStockGkey}.";
            }

            if (productStock.GrnLineSummaryGkey != line.GrnLineSumryGkey)
            {
                return $"ProductStock GKEY {requestedStockGkey} belongs to GRN line summary " +
                       $"{productStock.GrnLineSummaryGkey?.ToString() ?? "<null>"}, but the selected row " +
                       $"belongs to summary {line.GrnLineSumryGkey?.ToString() ?? "<null>"}.";
            }

            if (!string.Equals(
                    productStock.Status,
                    "Pending Tag",
                    StringComparison.OrdinalIgnoreCase))
            {
                return $"ProductStock GKEY {requestedStockGkey} is not pending tag; " +
                       $"its status is '{productStock.Status ?? "<null>"}'.";
            }

            if (productStock.IsBarcodePrinted)
                return $"ProductStock GKEY {requestedStockGkey} is already marked as barcode printed.";

            if (productStock.IsProductSold == true)
                return $"ProductStock GKEY {requestedStockGkey} is already marked as sold.";

            return null;
        }
        [RelayCommand]
        private void CellUpdate(CellValueChangedEventArgs args)
        {
            if (args.Row is not GrnLine line)
                return;

            ClearErrors();
            //ClearPrintStatusFor(line);
            EvaluateFormula(line);
        }

        private void PopulateUnboundLineDataMap()
        {
            if (copyGRNLineExpression is null) copyGRNLineExpression = new();

            copyGRNLineExpression.Add($"{nameof(GrnLine.NetWeight)}", (item, val) => item.NetWeight = val);
            copyGRNLineExpression.Add($"{nameof(GrnLine.OrderedQty)}", (item, val) => item.SuppliedQty = (int?)val);
            copyGRNLineExpression.Add($"{nameof(GrnLine.RejectedQty)}", (item, val) => item.RejectedQty = (int?)val);

        }

        private void PopulateUnboundLineSummaryDataMap()
        {
            if (copyGRNLineSumryExpression is null) copyGRNLineSumryExpression = new();

            copyGRNLineSumryExpression.Add($"{nameof(GrnLineSummary.NetWeight)}", (item, val) => item.NetWeight = val);
        }

        private void EvaluateGrnLine(GrnLine grnLine)
        {
            if (grnLine.StoneWeight.HasValue)
                grnLine.NetWeight = Math.Round(
                    grnLine.GrossWeight.GetValueOrDefault() - grnLine.StoneWeight.GetValueOrDefault(),
                    3,
                    MidpointRounding.AwayFromZero);

            grnLine.OrderedQty = 1;
            grnLine.ReceivedQty = 1;
            grnLine.SuppliedQty = 1;
            grnLine.AcceptedQty = 1;
            grnLine.RejectedQty = 0;

        }


        private void EvaluateFormula<T>(T item, bool isInit = false) where T : class
        {
            var formulas = FormulaStore.Instance.GetFormulas<T>();

            foreach (var formula in formulas)
            {
                //if (!isInit && IGNORE_UPDATE.Contains(formula.FieldName)) continue;

                var val = formula.Evaluate<T, decimal>(item, 0M);

                if (item is GrnLine grnLine)
                {
                    EvaluateGrnLine(grnLine);
                    copyGRNLineExpression[formula.FieldName].Invoke(grnLine, val);
                }
                else if (item is GrnLineSummary grnLineSumry)
                    copyGRNLineSumryExpression[formula.FieldName].Invoke(grnLineSumry, val);

            }
        }
    }


}
