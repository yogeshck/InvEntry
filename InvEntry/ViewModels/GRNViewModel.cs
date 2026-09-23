using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Printing;
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
    public partial class GRNViewModel : ObservableObject
    {
        [ObservableProperty]
        private GrnHeader _header;

        [ObservableProperty]
        private string _categoryUI;

/*        [ObservableProperty]
        private string _supplierId;*/

        [ObservableProperty]
        private MtblReference _supplierId;

        [ObservableProperty]
        private ObservableCollection<string> _productCategoryList;

        [ObservableProperty]
        private ObservableCollection<GrnLineSummary> selectedRows;

        [ObservableProperty]
        private ObservableCollection<string> _supplierReferencesList;

        [ObservableProperty]
        private DateSearchOption _searchOption;

        /*        [ObservableProperty]
                private ObservableCollection<GrnLine> selectedRows;*/

        private readonly IGrnService _grnService;
        private readonly IProductCategoryService _productCategoryService;
        private readonly IProductService _productService;
        private readonly IProductTransactionService _productTransactionService;
        private readonly IProductTransactionSummaryService _productTransactionSummaryService;
        private readonly IProductStockSummaryService _productStockSummaryService;
        private readonly IProductStockService _productStockService;
        private readonly IMessageBoxService _messageBoxService;
        private readonly IDialogService _dialogService;
        private readonly IMtblReferencesService _mtblReferencesService;

        private Dictionary<string, Action<GrnLineSummary, decimal?>> copyGRNLineSumryExpression;

        //private readonly IProductStockService _productStockService;
        //private readonly IDialogService _reportDialogService;
        //private readonly ICustomerService _customerService;

        public GRNViewModel(IGrnService                         grnService,
                            IProductService                     productService ,
                            IProductStockSummaryService         productStockSummaryService,
                            IProductTransactionService          productTransactionService,
                            IProductTransactionSummaryService   productTransactionSummaryService,
                            IDialogService                      dialogService,
                            IProductCategoryService             productCategoryService,
                            IMessageBoxService                  messageBoxService ,
                            IProductStockService                productStockService,
                            IMtblReferencesService              mtblReferencesService)
        {
            _grnService = grnService;
            _productService = productService;
            _productStockSummaryService = productStockSummaryService;
            _productTransactionService = productTransactionService;
            _productTransactionSummaryService = productTransactionSummaryService;
            _dialogService = dialogService;
            _productCategoryService = productCategoryService;
            _productStockService = productStockService;
            _messageBoxService = messageBoxService;
            _mtblReferencesService = mtblReferencesService;

            selectedRows = new();

            PopulateMtblSupplierListAsync();
            PopulateProductCategoryList();
            PopulateUnboundLineDataMap();

            SetHeader();

        }

        private void SetHeader()
        {
            Header = new()
            {
                GrnDate = DateTime.Now,
                DocumentDate = DateTime.Now,
                ItemReceivedDate = DateTime.Now,
                Status = "Open"
            };
        }

        private async void PopulateProductCategoryList()
        {
            var list = await _productCategoryService.GetProductCategoryList();
            ProductCategoryList = new(list.Select(x => x.Name));
        }

        private async void PopulateMtblSupplierListAsync()
        {
            var suppRefServiceList = await _mtblReferencesService.GetReferenceList("SUPPLIERS");
            SupplierReferencesList = new(suppRefServiceList.Select(x => x.RefValue));
        }

        partial void OnSupplierIdChanged(MtblReference? value)
        {
            if (Header is null)
                return;

            Header.SupplierId = value?.RefValue;
        }

        [RelayCommand]
        private async Task FetchProduct(EditValueChangedEventArgs args)
        {

            if (string.IsNullOrEmpty(CategoryUI)) return;

            var product = await _productService.GetByCategory(CategoryUI);

            if (product is null)
            {
                _messageBoxService.ShowMessage("Category " + CategoryUI + " not found., Contact Admin",
                                 "Product not found",
                                 MessageButton.OK);

                return;
            }

            GrnLineSummary grnLineSumry = new GrnLineSummary()
            {
                ProductGkey = product.GKey,
                ProductCategory = CategoryUI,
                SuppliedQty = 1,
                StoneWeight = 0,
                NetWeight = 0
            };

            SetLineSummary(grnLineSumry,product);

            //var NetWeight = grnLineSumry.GrossWeight.GetValueOrDefault() - grnLineSumry.StoneWeight.GetValueOrDefault();
            //grnLineSumry.NetWeight = Math.Round(NetWeight, 3, MidpointRounding.AwayFromZero);

            Header.GrnLineSumry.Add(grnLineSumry);

        }

        public void SetLineSummary(GrnLineSummary line, Product product)
        {
            line.ProductCategory = product.Category;
            line.Uom = product.Uom;
            line.ProductPurity = product.Purity;

           // line.GrossWeight = product.GrossWeight;
           // line.StoneWeight = product.StoneWeight;
           // line.NetWeight = product.GrossWeight - product.StoneWeight;
        }

/*        [RelayCommand]
        private void CellUpdate(CellValueChangedEventArgs args)
        {
            if (args.Row is GrnLineSummary line)
            {
               EvaluateFormula(line);
            }
        }*/


        [RelayCommand]
        private void CellUpdate(CellValueChangedEventArgs args)
        {
            if (args?.Row is not GrnLineSummary line)
                return;

            decimal gross = line.GrossWeight.GetValueOrDefault();
            decimal stone = line.StoneWeight.GetValueOrDefault();

            line.NetWeight = Math.Round(
                gross - stone,
                3,
                MidpointRounding.AwayFromZero);
        }

        [RelayCommand]
        private async Task Submit()
        {
            var header = await _grnService.CreateHeader(Header);

            if (header is not null)
            {
                Header.GKey = header.GKey;
                Header.GrnNbr = header.GrnNbr;
                
                Header.GrnLineSumry.ForEach(x =>
                {
                    x.GrnHdrGkey    = header.GKey;
                    x.LineNbr       = Header.GrnLineSumry.IndexOf(x) + 1;
                });

                await _grnService.CreateGrnLineSummary(Header.GrnLineSumry);

                var summaryKeys = await ProcessStockSummary(Header.GrnLineSumry);

                await CreateTemporaryStockItemsAsync(
                    Header.GrnLineSumry,
                    summaryKeys);

                _messageBoxService.ShowMessage( "GRN " + Header.GrnNbr + " Created Successfully",
                                                "GRN Creation", 
                                                MessageButton.OK, 
                                                MessageIcon.Exclamation);

                ResetGRN();

            }
        }

        private bool CanDeleteRows()
        {
            return SelectedRows?.Any() ?? false;
        }

        [RelayCommand(CanExecute = nameof(CanDeleteRows))]
        private void DeleteRows()
        {
            var result = _messageBoxService.ShowMessage("Delete all selected rows", "Delete Rows", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

            if (result == MessageResult.No)
                return;

            List<int> indexs = new List<int>();
            foreach (var row in SelectedRows)
            {
                indexs.Add(Header.GrnLineSumry.IndexOf(row));
            }

            indexs.ForEach(x =>
            {
                if (x >= 0)
                {
                    Header.GrnLineSumry.RemoveAt(x);
                }
            });

           // EvaluateForAllLines();
           // EvaluateHeader();
        }

        [RelayCommand(CanExecute = nameof(CanDeleteSingleRow))]
        private void DeleteSingleRow(GrnLineSummary grnline)
        {
            var result = _messageBoxService.ShowMessage("Delete current row", "Delete Row", MessageButton.YesNo, 
                                                        MessageIcon.Question, MessageResult.No);

            if (result == MessageResult.No)
                   return;

            var index = Header.GrnLineSumry.Remove(grnline);
        }

        private bool CanDeleteSingleRow(GrnLineSummary grnline)
        {
            return grnline is not null && Header.GrnLineSumry.IndexOf(grnline) > -1;
        }


        [RelayCommand]
        private void ResetGRN()
        {
            SetHeader();

            SupplierId = null;
        }

        private async Task CreateTemporaryStockItemsAsync(
            IEnumerable<GrnLineSummary> summaries,
            IReadOnlyDictionary<string, int> summaryKeys)
        {
            foreach (var line in summaries)
            {
                if (line.GKey <= 0)
                {
                    throw new InvalidOperationException(
                        $"GRN line summary was not saved for category {line.ProductCategory}.");
                }

                if (string.IsNullOrWhiteSpace(line.ProductCategory) ||
                    !summaryKeys.TryGetValue(
                        line.ProductCategory,
                        out int stockSummaryGkey))
                {
                    throw new InvalidOperationException(
                        $"Stock summary not found for category {line.ProductCategory}.");
                }

                int quantity = line.SuppliedQty.GetValueOrDefault();

                for (int i = 0; i < quantity; i++)
                {
                    var stock = new ProductStock
                    {
                        StockSummaryGkey = stockSummaryGkey,
                        GrnLineSummaryGkey = line.GKey,

                        ProductGkey = line.ProductGkey,
                        Category = line.ProductCategory,
                        SupplierId = Header.SupplierId,

                        ProductSku = $"TMP-{Guid.NewGuid():N}",

                        SuppliedQty = 1,
                        StockQty = 1,
                        SoldQty = 0,

                        GrossWeight = null,
                        StoneWeight = 0,
                        NetWeight = null,

                        IsProductSold = false,
                        Status = "Pending Tag",
                        IsBarcodePrinted = false,

                        CreatedOn = DateTime.Now
                    };

                    await _productStockService.CreateProductStock(stock);
                }
            }
        }


        private async void CreateProductTransaction(ProductStockSummary productStockSummary, int suppliedQty, int stockQty)
        {
            ProductTransaction productTransaction = new();

            //Get previous record closing balance to set this record opening - if not found set opening to zero
            var productTrans = await _productTransactionService.GetByCategory(productStockSummary.Category);

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

            productTransaction.ProductSku = productStockSummary.ProductSku;
            productTransaction.RefGkey = productStockSummary.ProductGkey;
            productTransaction.TransactionDate = DateTime.Now;
            productTransaction.ProductCategory = productStockSummary.Category;

            productTransaction.TransactionType = "Receipt";
            productTransaction.DocumentNbr = Header.GrnNbr;
            productTransaction.DocumentDate = Header.GrnDate;
            productTransaction.DocumentType = "GRN";
            productTransaction.VoucherType = "Stock Receipt";

            productTransaction.ObQty = stockQty; 
            productTransaction.TransactionQty = suppliedQty;
            productTransaction.CbQty = stockQty + suppliedQty;

            productTransaction.TransactionGrossWeight = productStockSummary.GrossWeight;
            productTransaction.TransactionStoneWeight = productStockSummary.StoneWeight;
            productTransaction.TransactionNetWeight = productStockSummary.NetWeight;

            productTransaction.ClosingGrossWeight = productTransaction.OpeningGrossWeight + productStockSummary.GrossWeight;
            productTransaction.ClosingStoneWeight = productTransaction.OpeningStoneWeight + productStockSummary.StoneWeight;
            productTransaction.ClosingNetWeight = productTransaction.OpeningNetWeight + productStockSummary.NetWeight;

            await _productTransactionService.CreateProductTransaction(productTransaction);

            createProductTransactionSummary(productTransaction);
        }

        private async void createProductTransactionSummary(ProductTransaction productTransaction)
        {

            ProductTransactionSummary productTransSumry = new();

            SearchOption = new();
            SearchOption.To = DateTime.Today;
            SearchOption.From = DateTime.Today;
            SearchOption.Filter1 ??= productTransaction.ProductCategory;

            var prodTransSumry = await _productTransactionSummaryService.GetAll(SearchOption);

            productTransSumry = prodTransSumry.FirstOrDefault();

            if (productTransSumry is not null)
            {
                // then add up with the existing total

                productTransSumry.StockInQty = productTransSumry.StockInQty.GetValueOrDefault() + productTransaction.TransactionQty;
                productTransSumry.ClosingQty = productTransSumry.ClosingQty.GetValueOrDefault() + productTransaction.TransactionQty;

                productTransSumry.StockInGrossWeight = productTransSumry.StockInGrossWeight.GetValueOrDefault()
                                                                        + productTransaction.TransactionGrossWeight;
                productTransSumry.StockInStoneWeight = productTransSumry.StockOutStoneWeight.GetValueOrDefault()
                                                                        + productTransaction.TransactionStoneWeight;
                productTransSumry.StockInNetWeight = productTransSumry.StockInNetWeight.GetValueOrDefault()
                                                                        + productTransaction.TransactionNetWeight;

                productTransSumry.ClosingGrossWeight = productTransSumry.ClosingGrossWeight.GetValueOrDefault()
                                                                    + productTransaction.TransactionGrossWeight;
                productTransSumry.ClosingStoneWeight = productTransSumry.ClosingStoneWeight.GetValueOrDefault()
                                                                    + productTransaction.TransactionStoneWeight;
                productTransSumry.ClosingNetWeight = productTransSumry.ClosingNetWeight.GetValueOrDefault()
                                                                    + productTransaction.TransactionNetWeight;

                await _productTransactionSummaryService.UpdateProductTransactionSummary(productTransSumry);
            }
            else
            {
                //create new record for the day if not found for todays 
                //get the last transaction of specific category to get opening balance
                ProductTransactionSummary prodTransSumryPrevious = new();

                productTransSumry = new();

                var prodTransSumryPrev = await _productTransactionSummaryService
                                                            .GetLastProductTranSumryByCategory(productTransaction.ProductCategory);
                if (prodTransSumryPrev != null)
                    prodTransSumryPrevious = prodTransSumryPrev;

                productTransSumry.TransactionDate = DateTime.Now;
                productTransSumry.ProductCategory = productTransaction.ProductCategory;
                productTransSumry.ProductSku = productTransaction.ProductSku;


                productTransSumry.StockInGrossWeight = productTransaction.TransactionGrossWeight;
                productTransSumry.StockInStoneWeight = productTransaction.TransactionStoneWeight;
                productTransSumry.StockInNetWeight = productTransaction.TransactionNetWeight;

                productTransSumry.StockOutGrossWeight = 0;
                productTransSumry.StockOutStoneWeight = 0;
                productTransSumry.StockOutNetWeight = 0;

                //Opening
                productTransSumry.OpeningQty = (prodTransSumryPrevious.ClosingQty ?? 0);
                productTransSumry.StockInQty = productTransaction.TransactionQty;
                productTransSumry.StockOutQty = 0;
                productTransSumry.ClosingQty = productTransSumry.OpeningQty.GetValueOrDefault()
                                                            + productTransaction.TransactionQty.GetValueOrDefault();

                productTransSumry.OpeningGrossWeight = (prodTransSumryPrevious.ClosingGrossWeight ?? 0);
                productTransSumry.OpeningStoneWeight = (prodTransSumryPrevious.ClosingStoneWeight ?? 0);
                productTransSumry.OpeningNetWeight = (prodTransSumryPrevious.ClosingNetWeight ?? 0);

                productTransSumry.ClosingGrossWeight = (productTransSumry.OpeningGrossWeight ?? 0) + productTransaction.TransactionGrossWeight;
                productTransSumry.ClosingStoneWeight = (productTransSumry.OpeningStoneWeight ?? 0) + productTransaction.TransactionStoneWeight;
                productTransSumry.ClosingNetWeight = (productTransSumry.OpeningNetWeight ?? 0) + productTransaction.TransactionNetWeight;

                await _productTransactionSummaryService.CreateProductTransactionSummary(productTransSumry);
            }

        }

        private async Task<Dictionary<string, int>> ProcessStockSummary(
            IEnumerable<GrnLineSummary> grnLineSummary)
        {
            var summaryKeys = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var x in grnLineSummary)
            {
                var productStockSummary =
                    await _productStockSummaryService
                        .GetProductStockSummaryByCategory(x.ProductCategory);

                bool createProductStockSummary = productStockSummary is null;

                productStockSummary ??= new();

                int currentStock = productStockSummary.StockQty.GetValueOrDefault();

                productStockSummary.Category = x.ProductCategory;
                productStockSummary.ProductGkey = x.ProductGkey;

                productStockSummary.GrossWeight =
                    productStockSummary.GrossWeight.GetValueOrDefault()
                    + x.GrossWeight.GetValueOrDefault();

                productStockSummary.StoneWeight =
                    productStockSummary.StoneWeight.GetValueOrDefault()
                    + x.StoneWeight.GetValueOrDefault();

                productStockSummary.NetWeight =
                    productStockSummary.NetWeight.GetValueOrDefault()
                    + x.NetWeight.GetValueOrDefault();

                productStockSummary.SuppliedGrossWeight =
                    productStockSummary.SuppliedGrossWeight.GetValueOrDefault()
                    + x.GrossWeight.GetValueOrDefault();

                productStockSummary.AdjustedWeight =
                    productStockSummary.AdjustedWeight.GetValueOrDefault();

                productStockSummary.SoldWeight =
                    productStockSummary.SoldWeight.GetValueOrDefault();

                productStockSummary.BalanceWeight =
                    productStockSummary.BalanceWeight.GetValueOrDefault()
                    + x.NetWeight.GetValueOrDefault();

                productStockSummary.SuppliedQty =
                    productStockSummary.SuppliedQty.GetValueOrDefault()
                    + x.SuppliedQty.GetValueOrDefault();

                productStockSummary.SoldQty =
                    productStockSummary.SoldQty.GetValueOrDefault();

                productStockSummary.StockQty =
                    productStockSummary.StockQty.GetValueOrDefault()
                    + x.SuppliedQty.GetValueOrDefault();

                productStockSummary.AdjustedQty =
                    productStockSummary.AdjustedQty.GetValueOrDefault();

                productStockSummary.Status = "In-Stock";

                if (createProductStockSummary)
                {
                    await _productStockSummaryService
                        .CreateProductStockSummary(productStockSummary);
                }
                else
                {
                    await _productStockSummaryService
                        .UpdateProductStockSummary(productStockSummary);
                }

                // Retrieve the saved record so we have its actual database GKey.
                var savedSummary =
                    await _productStockSummaryService
                        .GetProductStockSummaryByCategory(x.ProductCategory);

                if (savedSummary is null || savedSummary.GKey <= 0)
                {
                    throw new InvalidOperationException(
                        $"Stock summary was not saved for category {x.ProductCategory}.");
                }

                summaryKeys[x.ProductCategory] = savedSummary.GKey;

                // Preserve the existing category-level receipt transaction.
                CreateProductTransaction(
                    productStockSummary,
                    x.SuppliedQty.GetValueOrDefault(),
                    currentStock);
            }

            return summaryKeys;
        }


        private void EvaluateFormula<T>(T item, bool isInit = false)
            where T : class
        {
            if (item is null)
                return;

            if (item is GrnLineSummary line)
            {
                decimal gross = line.GrossWeight.GetValueOrDefault();
                decimal stone = line.StoneWeight.GetValueOrDefault();

                line.NetWeight = Math.Round(
                    gross - stone,
                    3,
                    MidpointRounding.AwayFromZero);

                return;
            }

            var formulas = FormulaStore.Instance.GetFormulas<T>();

            if (formulas is null)
                return;

            foreach (var formula in formulas)
            {
                if (formula is null)
                    continue;

                var val = formula.Evaluate<T, decimal>(item, 0M);

                if (item is GrnLineSummary grnLineSumry &&
                    copyGRNLineSumryExpression is not null &&
                    copyGRNLineSumryExpression.TryGetValue(
                        formula.FieldName,
                        out var setter))
                {
                    setter(grnLineSumry, val);
                }
            }
        }

        private void PopulateUnboundLineDataMap()
        {
            if (copyGRNLineSumryExpression is null) copyGRNLineSumryExpression = new();

            copyGRNLineSumryExpression.Add($"{nameof(GrnLineSummary.NetWeight)}", (item, val) => item.NetWeight = val);
        }

        }
}
