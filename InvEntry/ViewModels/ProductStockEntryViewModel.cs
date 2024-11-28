using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Printing;
using DevExpress.XtraLayout.Customization;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Models.Extensions;
using InvEntry.Reports;
using InvEntry.Services;
using InvEntry.Store;
using InvEntry.Utils;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IProductService _productService;
        private readonly IProductTransactionService _productTransactionService;
        private readonly IProductStockService _productStockService;
        private readonly IMessageBoxService _messageBoxService;
        private readonly IDialogService _dialogService;
        private readonly IMtblReferencesService _mtblReferencesService;

        [ObservableProperty]
        private ObservableCollection<string> _supplierReferencesList;

        [ObservableProperty]
        private ObservableCollection<GrnHeader> _grnHdrList;

        [ObservableProperty]
        private ObservableCollection<ProductStock> _productStockList;

        [ObservableProperty]
        private ObservableCollection<GrnLine> _grnLineList;

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

        private MtblReference mtblReference;

        private Dictionary<int, ObservableCollection<GrnLine>> _lineGrnLookup;
        private Dictionary<string, Action<GrnLine, decimal?>> copyGRNLineExpression;
        private Dictionary<string, Action<GrnLineSummary, decimal?>> copyGRNLineSumryExpression;

        public ProductStockEntryViewModel(  IGrnService grnService,
                                            IProductService productService,
                                            IProductTransactionService productTransactionService,
                                            IProductStockService productStockService,
                                            IDialogService dialogService,
                                            IProductCategoryService productCategoryService,
                                            IMessageBoxService messageBoxService,
                                            IMtblReferencesService mtblReferencesService
                                            )
        {
            _grnService                 = grnService;
            _productService             = productService;
            _productStockService        = productStockService;
            _productTransactionService  = productTransactionService;
            _dialogService              = dialogService;
            _productCategoryService     = productCategoryService;
            _messageBoxService          = messageBoxService;
            _mtblReferencesService      = mtblReferencesService;

            _lineGrnLookup = new();

            PopulateMtblSupplierListAsync();
            PopulateOpenGRN();
            PopulateUnboundLineDataMap();
            PopulateUnboundLineSummaryDataMap();

        }


        private async void PopulateMtblSupplierListAsync()
        {

            var suppRefServiceList = await _mtblReferencesService.GetReferenceList("SUPPLIERS");

            SupplierReferencesList = new(suppRefServiceList.Select(x => x.RefValue));

        }

        private async void PopulateOpenGRN()
        {
            var grnHdrList = await _grnService.GetBySupplier(SupplierID);
        }


        [RelayCommand]
        private async Task SelectedGRN()
        {
            var grnLineList = await _grnService.GetByHdrGkey(SelectedGrn.GKey);

            var grnHeader = await _grnService.GetByHdrGkey(SelectedGrn.GKey);

        }

        [RelayCommand]
        private void SelectionGrnSumryListChanged()
        {

            if (SelectedGrnLineSumry is null) return;


            if (_lineGrnLookup.TryGetValue(SelectedGrnLineSumry.GKey, out var grnLines))
            {
                GrnLineList = new(grnLines);
                return;
            }

            GrnLineList = new();

            var count = SelectedGrnLineSumry.SuppliedQty;
            for (int i = 1; i <= count; i++)
            {
                GrnLine grnLine = new();

                grnLine.ProductId = SelectedGrnLineSumry.ProductCategory;
                grnLine.ProductGkey = SelectedGrnLineSumry.ProductGkey;
                grnLine.LineNbr = i;

                GrnLineList.Add(grnLine);

            }

            _lineGrnLookup[SelectedGrnLineSumry.GKey] = GrnLineList;
        }

        partial void OnSelectedGrnLineSumryChanged(GrnLineSummary oldValue, GrnLineSummary newValue)
        {
            if(oldValue is not null)
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
            if (SelectedGrn is null ) return;

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

            foreach(var keyValue in _lineGrnLookup)
            {
                await SavingGrnLine(keyValue.Value);
            }

            _lineGrnLookup.Clear();
        }

        private async Task SavingGrnLine(ObservableCollection<GrnLine> grnLines)
        {

            if (grnLines is null || !grnLines.Any() ) return;

            var category = grnLines.First().ProductId;
                
            var mtblReference = await _mtblReferencesService.GetReference("PRODUCT_CATEGORY", category);

            if (mtblReference is null)
            {
                return;
            }

            grnLines.ForEach(x =>
            {

                var sku = int.Parse(mtblReference.RefValue);
                sku++;

                x.GrnHdrGkey = SelectedGrn.GKey;
                x.Status = "Closed";

                x.ProductSku = string.Format("{0}{1}", mtblReference.RefDesc, sku.ToString("D4"));
 
                ProcessStockLines(x);

                mtblReference.RefValue = sku.ToString();

            });

            await _mtblReferencesService.UpdateReference(mtblReference);

            await _grnService.CreateGrnLine(grnLines);

        }


        [RelayCommand]
        private async Task Submit()
        {

            if (SelectedGrn is null )
            {
                return;
            }

            await SavingGrnLinesList();


            if (ProductStockList is not null)
            {

                ProductStockList.ForEach(async x =>
                {
                    await _productStockService.CreateProductStock(x);

                    CreateProductTransaction(x);
                });
            }

            SelectedGrn.Status = "Closed";
            await _grnService.UpdateHeader(SelectedGrn);

            if (GrnHdrList.Contains(SelectedGrn) )
            {
                GrnHdrList.Remove(SelectedGrn);
            }

            GrnLineList.Clear();
            GrnLineSumryList.Clear();
            ProductStockList.Clear();

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

            } else
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
            productTransaction.ClosingNetWeight   = productTransaction.OpeningNetWeight + productStock.NetWeight;

            await _productTransactionService.CreateProductTransaction(productTransaction);
        }

        private void ProcessStockLines(GrnLine grnLineStock)
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

            ProductStockList.Add(productStock);

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

        private void EvaluateFormula<T>(T item, bool isInit = false) where T : class
        {
            var formulas = FormulaStore.Instance.GetFormulas<T>();

            foreach (var formula in formulas)
            {
                //if (!isInit && IGNORE_UPDATE.Contains(formula.FieldName)) continue;

                var val = formula.Evaluate<T, decimal>(item, 0M);

                if (item is GrnLine grnLine)
                    copyGRNLineExpression[formula.FieldName].Invoke(grnLine, val);
                else if (item is GrnLineSummary grnLineSumry)
                    copyGRNLineSumryExpression[formula.FieldName].Invoke(grnLineSumry, val);

            }
        }
    }
}
