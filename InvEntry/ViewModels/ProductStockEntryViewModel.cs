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
        private int productSkuSeq;

        private WeighScaleReaderAuto reader;

        private Dictionary<int, ObservableCollection<GrnLine>> _lineGrnLookup;
        private Dictionary<string, Action<GrnLine, decimal?>> copyGRNLineExpression;
        private Dictionary<string, Action<GrnLineSummary, decimal?>> copyGRNLineSumryExpression;
        private readonly ILabelPrinter _labelPrinter;

        public ProductStockEntryViewModel(IGrnService grnService,
                                            IProductViewService productViewService,
                                            IProductTransactionService productTransactionService,
                                            IProductStockService productStockService,
                                            IDialogService dialogService,
                                            IProductCategoryService productCategoryService,
                                            IOrgThisCompanyViewService orgThisCompanyViewService,
                                            IMessageBoxService messageBoxService,
                                            IMtblReferencesService mtblReferencesService,
                                            ILabelPrinter labelPrinter)
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
        private async void OnEditorActivated(ShowingEditorEventArgs e)
        {
            //var line = e.Row as GrnLine;

            if (e.Row is not GrnLine line)
                return;

            ClearErrors();
            // User has started correcting the current line.
            //ClearPrintStatusFor(line);

            var waitVM = WaitIndicatorVM.ShowIndicator("Press... print button... reading weight.... .");
          
            if (!isManualMode)
            {
                Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Awaiting ...input..."));

                
            }

            if (e.Column.FieldName == "GrossWeight")
            {

                SplashScreenManager.CreateWaitIndicator(waitVM).Show();

                //var line = e.Row as GrnLine;
                if (line != null)
                {
                    if (!isManualMode)   //AUTO Mode
                    {
                        var reader = new WeighScaleReaderAuto();
                        var weight = await reader.StartManualAsync();
                        //.StartScaleAsync(); // await one stable value

                        if (weight < 0)
                        {
                            //display error message
                            ShowError(
                                        "Weighing machine error",
                                        "Unable to read the weight. Please check the weighing-machine connection.");
                            reader.Stop();
                            return;
                        }

                        line.GrossWeight = weight;
                        line.StoneWeight = 0;
                        line.NetWeight = line.GrossWeight;


                    }

/*                    else
                    {
                        if (isManualMode)
                        {
                            line.GrossWeight = weight;
                        }

                    }*/
                }

                SplashScreenManager.ActiveSplashScreens.FirstOrDefault(x => x.ViewModel == waitVM).Close();

                if (!isManualMode)
                    _ = PrintTagAsync(line);
            }
            else
            {
                if(line != null)
                {
                    if(line.NetWeight > 0)
                    {
                        _ = PrintTagAsync(line);
                    }
                }
            }

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

            if (string.IsNullOrWhiteSpace(grnLine.ProductSku))
                errors.Add("Product SKU is required.");

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
                ShowErrors(
        "Unable to print label",
        validationErrors);

                return; // Remain on the current row
            }

            var line = grnLine!;

            IsPrintingTag = true;

            try
            {
/*                var printResult = BarCodePrint.ProcessBarCode(
                    line.ProductSku!,
                    line.ProductDesc ?? string.Empty,
                    line.SuppVaPercent.GetValueOrDefault(),
                    line.NetWeight!.Value,
                    line.StoneWeight.GetValueOrDefault(),
                    line.ProductPurity ?? string.Empty,
                    Company!.CompanyName ?? "MATHA");*/

                var request = new LabelPrintRequest(
    line.ProductSku!,
    line.ProductDesc ?? string.Empty,
    line.SuppVaPercent.GetValueOrDefault(),
    line.NetWeight!.Value,
    line.StoneWeight.GetValueOrDefault(),
    line.ProductPurity ?? string.Empty,
    Company!.CompanyName ?? "MATHA");

                var printResult = await _labelPrinter.PrintAsync(request);

                if (!printResult.Success)
                {
                    SelectedGrnLine = line;

                    ShowError(
                            "Label printing failed",
                            printResult.ErrorMessage ??
                            "The printer did not accept the label.");


                    return; // Critical: do not move to the next row
                }

                var completedLineNumber = line.LineNbr;

                var existingLine =
                    await _grnService.GetByProductSku(line.ProductSku!);

                if (existingLine is null)
                {
                    line.GrnHdrGkey = SelectedGrn!.GKey;
                    line.Status = "Closed";

                    await ProcessStockLinesAsync(line);
                    await _grnService.CreateGrnLine(line);

                    productSkuSeq++;
                    mtblReference!.RefValue = productSkuSeq.ToString();

                    await _mtblReferencesService.UpdateReference(
                        mtblReference);
                }

               // PrintStatusMessage =
               //     $"Label {line.ProductSku} was submitted to the printer.";

                // Execute this only after everything succeeds.
                SelectNextPrintableLine((int)completedLineNumber);
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
            

            // Do not call SelectNextPrintableLine() here.
        }
            finally
            {
                IsPrintingTag = false;
            }
        }

        private void SelectNextPrintableLine(int completedLineNumber)
        {
            if (GrnLineList is null || GrnLineList.Count == 0)
                return;

            // Do not assume that line numbers are continuous.
            var nextLine = GrnLineList
                .Where(line => line.LineNbr > completedLineNumber)
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

            if (SelectedGrnLineSumry is null) return;


            if (_lineGrnLookup.TryGetValue(SelectedGrnLineSumry.GKey, out var grnLines))
            {
                GrnLineList = new(grnLines);
                return;
            }

            var result = _messageBoxService.ShowMessage(
                "Do you want to print in AUTO mode?",
                "Confirmation",
                MessageButton.YesNoCancel,
                MessageIcon.Question);

            if (result == MessageResult.Yes)
            {
                isManualMode = false;
            }
            else if (result == MessageResult.No)
            {
                isManualMode = true;
            }
            else if (result == MessageResult.Cancel)
            {
                return;
            }


            //var category = GrnLineSumryList.First().ProductCategory;

            var category = GrnLineSumryList.Where(x => x.GKey == SelectedGrnLineSumry.GKey)
                            .Select(x => x.ProductCategory).FirstOrDefault();


            if (category == null) return;

            mtblReference = await _mtblReferencesService.GetReference("PRODUCT_CATEGORY", category);

            if (mtblReference is null)
            {
                return;
            }

            productSkuSeq = int.Parse(mtblReference.RefValue);

            var prdView = await _productViewService.GetProduct(category);

            // check grn line has any records already in table
            // if there populate the old records and then allow user to add new rec

            GrnLineList = new();

            var grnLineSkuCnt = 0;
            var grnLineSkuToPrint = 0;

            var grnLineList_1 = await _grnService.GetByLineSumryGkey(SelectedGrnLineSumry.GKey, (int)SelectedGrnLineSumry.GrnHdrGkey);

            if (grnLineList_1 is not null)
                grnLineSkuCnt = grnLineList_1.Count(x => x.ProductId == prdView.Id &&
                                                        x.ProductSku != null);

            if (grnLineSkuCnt > 0)
            //records already exist, then populate
            {
                grnLineSkuToPrint = (int)(SelectedGrnLineSumry.SuppliedQty - grnLineSkuCnt);
            }
            else
            {
                grnLineSkuToPrint = (int)SelectedGrnLineSumry.SuppliedQty;
            }

            var tempSku = productSkuSeq;

            for (int i = grnLineSkuCnt+1; i <= grnLineSkuToPrint+1; i++)
            {
                //Sequence number as product sku alongwith product code

                tempSku++;

                GrnLine grnLine = new();

                grnLine.GrnHdrGkey = SelectedGrnLineSumry.GrnHdrGkey;
                grnLine.ProductId = SelectedGrnLineSumry.ProductCategory;
                grnLine.ProductGkey = SelectedGrnLineSumry.ProductGkey;
                grnLine.LineNbr = i;
                grnLine.ProductDesc = prdView.Description;
                grnLine.ProductPurity = prdView?.Purity;
                grnLine.SuppVaPercent = prdView.VaPercent;
                grnLine.GrnLineSumryGkey = SelectedGrnLineSumry.GKey;

                //grnLine.ProductSku = SelectedGrnLineSumry.ProductCategory;

                var tagPurityCode = "";
                if (grnLine.ProductPurity == "916")
                    tagPurityCode = "2";
                else if (grnLine.ProductPurity == "750")
                    tagPurityCode = "8";

                var productSku = string.Format("{0}{1}{2}{3}", mtblReference.RefDesc, tagPurityCode, "-", tempSku.ToString("D4")); //, grnLine.NetWeight);
                grnLine.ProductSku = productSku;

                //string.Format("{0}{1}", mtblReference.RefDesc, ProductSku.ToString("D4"));

                GrnLineList.Add(grnLine);

            }

            _lineGrnLookup[SelectedGrnLineSumry.GKey] = GrnLineList;
        }

        partial void OnSelectedGrnLineSumryChanged(GrnLineSummary oldValue, GrnLineSummary newValue)
        {
            if (oldValue is not null)
                _lineGrnLookup[oldValue.GKey] = GrnLineList;
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
            {
                return;
            }

            // saving immediate no need below line
            // await SavingGrnLinesList();
            _lineGrnLookup.Clear();

            //check should be introduced here to find any leftover line to be closed, if any do not set closed otherwise do
            SelectedGrn.Status = "Closed";
            await _grnService.UpdateHeader(SelectedGrn);

            if (GrnHdrList.Contains(SelectedGrn))
            {
                GrnHdrList.Remove(SelectedGrn);
            }

            GrnLineList.Clear();
            GrnLineSumryList.Clear();
            ProductStockList.Clear();

            //StopScale();

            _messageBoxService.ShowMessage("Stock Updated Successfully", "Stock Created",
                                MessageButton.OK, MessageIcon.Exclamation);

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

        private async Task ProcessStockLinesAsync(GrnLine grnLineStock)
        {

            var prdStk = await _productStockService.GetProductStock(grnLineStock.ProductSku);
            if (prdStk is not null)   
                return;     //avoid duplication of product stock

            if (ProductStockList is null)
                ProductStockList = new();

            ProductStock productStock = new ProductStock();

            productStock.ProductGkey = grnLineStock.ProductGkey;
            productStock.GrossWeight = grnLineStock.GrossWeight;
            productStock.StoneWeight = grnLineStock.StoneWeight;
            productStock.NetWeight = grnLineStock.NetWeight;
            productStock.SuppliedGrossWeight = grnLineStock.GrossWeight;
            productStock.AdjustedWeight = 0;
            productStock.SoldWeight = 0;
            productStock.BalanceWeight = grnLineStock.NetWeight;
            productStock.SuppliedQty = grnLineStock.SuppliedQty;
            productStock.SoldQty = 0;
            productStock.StockQty = 1; //hardcoded to be reviewed later >>>> grnLineStock.AcceptedQty;
            productStock.Status = "In-Stock";
            productStock.SupplierId = SelectedGrn.SupplierId;
            productStock.IsProductSold = false;
            productStock.Category = grnLineStock.ProductId;
            productStock.ProductSku = grnLineStock.ProductSku;
            productStock.IsBarcodePrinted = true;
            productStock.CreatedOn = DateTime.Now;
            productStock.CreatedBy = "System";
            productStock.WastageAmount = 0;
            productStock.WastagePercent = 0;

            // ProductStockList.Add(productStock);
            //save to db immediate - if list has 100 or more nos, it takes lots of time
            await _productStockService.CreateProductStock(productStock);

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
                grnLine.NetWeight = grnLine.GrossWeight.GetValueOrDefault() - grnLine.StoneWeight.GetValueOrDefault();

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
