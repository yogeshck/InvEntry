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

        [ObservableProperty]
        private ObservableCollection<string> _productCategoryList;

        [ObservableProperty]
        private ObservableCollection<GrnLineSummary> selectedRows;

        [ObservableProperty]
        private ObservableCollection<string> _supplierReferencesList;

        /*        [ObservableProperty]
                private ObservableCollection<GrnLine> selectedRows;*/

        private readonly IGrnService _grnService;
        private readonly IProductCategoryService _productCategoryService;
        private readonly IProductService _productService;
        private readonly IProductTransactionService _productTransactionService;
        private readonly IProductStockSummaryService _productStockSummaryService;
        private readonly IMessageBoxService _messageBoxService;
        private readonly IDialogService _dialogService;
        private readonly MtblReferencesService _mtblReferencesService;

        private Dictionary<string, Action<GrnLineSummary, decimal?>> copyGRNLineSumryExpression;

        //private readonly IProductStockService _productStockService;
        //private readonly IDialogService _reportDialogService;
        //private readonly ICustomerService _customerService;

        public GRNViewModel(IGrnService                 grnService,
                            IProductService             productService ,
                            IProductStockSummaryService productStockSummaryService,
                            IProductTransactionService  productTransactionService,
                            IDialogService              dialogService,
                            IProductCategoryService     productCategoryService,
                            IMessageBoxService          messageBoxService ,
                            MtblReferencesService      mtblReferencesService)
        {
            _grnService = grnService;
            _productService = productService;
            _productStockSummaryService = productStockSummaryService;
            _productTransactionService = productTransactionService;
            _dialogService = dialogService;
            _productCategoryService = productCategoryService;
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


        [RelayCommand]
        private async Task FetchProduct(EditValueChangedEventArgs args)
        {

            if (string.IsNullOrEmpty(CategoryUI)) return;

            var product = await _productService.GetByCategory(CategoryUI);

            GrnLineSummary grnLineSumry = new GrnLineSummary()
            {
                ProductGkey = product.GKey,
                ProductCategory = CategoryUI,
                StoneWeight = 0
            };

            SetLineSummary(grnLineSumry,product);

            grnLineSumry.NetWeight = grnLineSumry.GrossWeight - grnLineSumry.StoneWeight;

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

        [RelayCommand]
        private void CellUpdate(CellValueChangedEventArgs args)
        {
            if (args.Row is GrnLineSummary line)
            {
               EvaluateFormula(line);
            }
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
                    x.StoneWeight   = 0;
                });

                await _grnService.CreateGrnLineSummary(Header.GrnLineSumry);

                ProcessStockSummary(Header.GrnLineSumry);

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
            
        }


        private async void CreateProductTransaction(ProductStockSummary productStockSummary)
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

            productTransaction.ObQty = 0;
            productTransaction.TransactionQty = productStockSummary.StockQty;
            productTransaction.CbQty = productStockSummary.SuppliedQty;

            productTransaction.TransactionGrossWeight = productStockSummary.GrossWeight;
            productTransaction.TransactionStoneWeight = productStockSummary.StoneWeight;
            productTransaction.TransactionNetWeight = productStockSummary.NetWeight;

            productTransaction.ClosingGrossWeight = productTransaction.OpeningGrossWeight + productStockSummary.GrossWeight;
            productTransaction.ClosingStoneWeight = productTransaction.OpeningStoneWeight + productStockSummary.StoneWeight;
            productTransaction.ClosingNetWeight = productTransaction.OpeningNetWeight + productStockSummary.NetWeight;

            await _productTransactionService.CreateProductTransaction(productTransaction);
        }

        private async void ProcessStockSummary(IEnumerable<GrnLineSummary> grnLineSummary)
        {

            Header.GrnLineSumry.ForEach(async x =>
            {
                var createProductStockSummary = false;

                var productStockSummary = await _productStockSummaryService.GetProductStockSummaryByCategory(x.ProductCategory);

                if (productStockSummary is null)
                {
                    productStockSummary = new();
                    createProductStockSummary = true;

                }

                productStockSummary.Category            = x.ProductCategory;
                productStockSummary.ProductGkey         = x.ProductGkey;
                //productStockSummary.ProductSku          = productStockSummary.ProductSku;
                productStockSummary.GrossWeight         = (productStockSummary.GrossWeight ?? 0 ) + x.GrossWeight;
                productStockSummary.StoneWeight         = (productStockSummary.StoneWeight ?? 0 ) + x.StoneWeight;
                productStockSummary.NetWeight           = (productStockSummary.NetWeight   ?? 0 ) + x.NetWeight;
                productStockSummary.SuppliedGrossWeight = (productStockSummary.SuppliedGrossWeight ?? 0) + x.GrossWeight;
                productStockSummary.AdjustedWeight      = (productStockSummary.AdjustedWeight ?? 0);
                productStockSummary.SoldWeight          = (productStockSummary.SoldWeight ?? 0 );
                productStockSummary.BalanceWeight       = (productStockSummary.BalanceWeight ?? 0) + x.NetWeight;
                productStockSummary.SuppliedQty         = (productStockSummary.SuppliedQty ?? 0 ) + x.SuppliedQty;
                productStockSummary.SoldQty             = (productStockSummary.SoldQty ?? 0);
                productStockSummary.StockQty            = (productStockSummary.StockQty ?? 0 ) + x.SuppliedQty;
                productStockSummary.AdjustedQty         = (productStockSummary.AdjustedQty ?? 0);
                productStockSummary.Status              = "In-Stock";

                if (createProductStockSummary)
                {
                    await _productStockSummaryService.CreateProductStockSummary(productStockSummary);
                }
                else
                {
                    await _productStockSummaryService.UpdateProductStockSummary(productStockSummary);
                }

                CreateProductTransaction(productStockSummary);

            });

        }

        private void EvaluateFormula<T>(T item, bool isInit = false) where T : class
        {
            var formulas = FormulaStore.Instance.GetFormulas<T>();

            foreach (var formula in formulas)
            {
                //if (!isInit && IGNORE_UPDATE.Contains(formula.FieldName)) continue;

                var val = formula.Evaluate<T, decimal>(item, 0M);

                if (item is GrnLineSummary grnLineSumry)
                    copyGRNLineSumryExpression[formula.FieldName].Invoke(grnLineSumry, val);
            }
        }

        private void PopulateUnboundLineDataMap()
        {
            if (copyGRNLineSumryExpression is null) copyGRNLineSumryExpression = new();

            copyGRNLineSumryExpression.Add($"{nameof(GrnLineSummary.NetWeight)}", (item, val) => item.NetWeight = val);
        }

            //private void EvaluateFormula<T>(T item, string fieldName, bool isInit = false) where T : class
            //{
            //    //if (!isInit && IGNORE_UPDATE.Contains(fieldName)) return;

            //    var formula = FormulaStore.Instance.GetFormula<T>(fieldName);

            //    var val = formula.Evaluate<T, decimal>(item, 0M);

            //    if (item is InvoiceLine invLine)
            //        copyGRNExpression[fieldName].Invoke(invLine, val);
            //    else if (item is InvoiceHeader head)
            //        copyHeaderExpression[fieldName].Invoke(head, val);
            //}
        }
}
