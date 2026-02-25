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
using InvEntry.Models;
using InvEntry.Services;
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

        public ProductStockEntryViewModel(IGrnService grnService,
                                            IProductViewService productViewService,
                                            IProductTransactionService productTransactionService,
                                            IProductStockService productStockService,
                                            IDialogService dialogService,
                                            IProductCategoryService productCategoryService,
                                            IOrgThisCompanyViewService orgThisCompanyViewService,
                                            IMessageBoxService messageBoxService,
                                            IMtblReferencesService mtblReferencesService
                                            )
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

            _lineGrnLookup = new();

            /*            reader = new();
                        reader.StartAuto();*/

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


        /*        partial void OnSelectedGrnLineChanged(GrnLine value)
                {

                  //  if ((nameof(GrnLine.GrossWeight).Equals(args.Column.FieldName) // || nameof(GrnLine.StoneWeight).Equals(args.Column.FieldName) ) 
                                                                                    //&& line.NetWeight > 0 )
                    {


                        OnWeightCaptured(_capturedWeight);
                    }
                }*/



        [RelayCommand]
        private async Task SelectedGRN()
        {
            var grnLineList = await _grnService.GetByHdrGkey(SelectedGrn.GKey);

            var grnHeader = await _grnService.GetByHdrGkey(SelectedGrn.GKey);

        }

        [RelayCommand]
        private async void OnEditorActivated(ShowingEditorEventArgs e)
        { 

            var waitVM = WaitIndicatorVM.ShowIndicator("Press... print button... reading weight.... .");
          
            if (!isManualMode)
            {
                Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Awaiting ...input..."));

                
            }

            if (e.Column.FieldName == "GrossWeight")
            {

                SplashScreenManager.CreateWaitIndicator(waitVM).Show();

                var line = e.Row as GrnLine;
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
                            _messageBoxService.ShowMessage("Weigh machine communication error...... ");
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

        }



        /*        private async void OnEditorActivated(ShowingEditorEventArgs e)
                {
                    if (e.Column.FieldName == "GrossWeight")
                    {
                        var line = e.Row as GrnLine;
                        if (line != null)
                        {
                            var reader = new WeighScaleReader();
                            var weight = await reader.StartScaleAsync(); // await one stable value

                            // Update the bound property
                            line.GrossWeight = weight;

                            Dispatcher.Invoke(() =>
                            {
                                var view = e.Source as TableView; // or get via sender
                                var rowHandle = view.FocusedRowHandle;
                                view.SetCellValue(rowHandle, "GrossWeight", weight);
                            });

                        }
                    }
                }*/



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

            var grnLineRecCnt = 0;

            var grnLineList_1 = await _grnService.GetByLineSumryGkey(SelectedGrnLineSumry.GKey, (int)SelectedGrnLineSumry.GrnHdrGkey);

            if (grnLineList_1 is not null)
                grnLineRecCnt = grnLineList_1.Count(x => x.ProductId == prdView.Id &&
                                                        x.ProductSku != null);

            if (grnLineRecCnt > 0)
            //records already exist, then populate
            {
                grnLineRecCnt = (int)(SelectedGrnLineSumry.SuppliedQty - grnLineRecCnt);
            }
            else
            {
                grnLineRecCnt = (int)SelectedGrnLineSumry.SuppliedQty;
            }

            var tempSku = productSkuSeq;

            for (int i = 1; i <= grnLineRecCnt; i++)
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
                grnLine.SuppVaPercent = 18;// prdView.VaPercent;
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
                        await _grnService.CreateGrnLine(grnLines);
                    }
                }
            });

            //if user maintains seq nbr for product sku - this nees to be executed - but in difference place - need to fix
            await _mtblReferencesService.UpdateReference(mtblReference);



        }

        [RelayCommand]
        private async Task PrintTagAsync(GrnLine grnline)
        {


            if (grnline.NetWeight > 0.00m)
            {

                var result = BarCodePrint.ProcessBarCode(grnline.ProductSku, grnline.ProductDesc,
                                                                            grnline.SuppVaPercent.Value, grnline.NetWeight.Value,
                                                                            grnline.StoneWeight.Value,
                                                                            grnline.ProductPurity, Company.CompanyName);
                //return Task.CompletedTask;
            }


            //set isprinted to false

            var nextSeq = SelectedGrnLine.LineNbr + 1;

            if (GrnLineList.Any(x => x.LineNbr == nextSeq))
            {
                SelectedGrnLine = GrnLineList.First(x => x.LineNbr == nextSeq);
            }
            else
            {
                //mofied 24-feb needs to be tested
               // reader.Stop();
            }

            if (grnline.NetWeight.HasValue && grnline.NetWeight > 0 && grnline.ProductSku is not null)
            {
                grnline.GrnHdrGkey = SelectedGrn.GKey;

                /*                    if (x.ProductSku is null)
                                    {
                                        return;
                                    } else*/
                {
                    grnline.Status = "Closed";
                    _ = ProcessStockLinesAsync(grnline);
                    await _grnService.CreateGrnLine(grnline);
                }

                productSkuSeq = productSkuSeq + 1;
                mtblReference.RefValue = productSkuSeq.ToString();
                //if user maintains seq nbr for product sku - this nees to be executed - but in difference place - need to fix
                await _mtblReferencesService.UpdateReference(mtblReference);

            }

        }

        /*        private void DisablePrintButton(int rowHandle)
                {
                    var row = gridView1.GetRow(rowHandle) as MyRowModel;
                    if (row != null)
                    {
                        row.CanPrint = false; // mark as disabled
                        gridView1.RefreshRow(rowHandle); // refresh UI
                    }
                }*/

        // Move cursor to next row and specific column


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
            productStock.StockQty = grnLineStock.AcceptedQty;
            productStock.Status = "In-Stock";
            productStock.SupplierId = SelectedGrn.SupplierId;
            productStock.IsProductSold = false;
            productStock.Category = grnLineStock.ProductId;
            productStock.ProductSku = grnLineStock.ProductSku;
            productStock.IsBarcodePrinted = true;
            productStock.CreatedOn = DateTime.Now;
            productStock.CreatedBy = "System";

            // ProductStockList.Add(productStock);
            //save to db immediate - if list has 100 or more nos, it takes lots of time
            await _productStockService.CreateProductStock(productStock);

        }

        [RelayCommand]
        private void CellUpdate(CellValueChangedEventArgs args)
        {
            if (args.Row is GrnLine line)
            {

                EvaluateFormula(line);

            }
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
