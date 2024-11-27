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
        private ObservableCollection<GrnLineSummary> _selectedRows;

        [ObservableProperty]
        private ObservableCollection<string> _supplierReferencesList;

        /*        [ObservableProperty]
                private ObservableCollection<GrnLine> selectedRows;*/

        private readonly IGrnService _grnService;
        private readonly IProductCategoryService _productCategoryService;
        private readonly IProductService _productService;
        private readonly IMessageBoxService _messageBoxService;
        private readonly IDialogService _dialogService;
        private readonly IMtblReferencesService _mtblReferencesService;

        private Dictionary<string, Action<GrnLineSummary, decimal?>> copyGRNLineExpression;

        //private readonly IProductStockService _productStockService;
        //private readonly IDialogService _reportDialogService;
        //private readonly ICustomerService _customerService;

        public GRNViewModel(IGrnService             grnService,
                            IProductService         productService ,
                            IDialogService          dialogService,
                            IProductCategoryService productCategoryService,
                            IMessageBoxService      messageBoxService ,
                            IMtblReferencesService  mtblReferencesService)
        {
            _grnService = grnService;
            _productService = productService;
            _dialogService = dialogService;
            _productCategoryService = productCategoryService;
            _messageBoxService = messageBoxService;
            _mtblReferencesService = mtblReferencesService;

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
                ProductCategory = CategoryUI
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
                });

                await _grnService.CreateGrnLineSummary(Header.GrnLineSumry);

                _messageBoxService.ShowMessage( "GRN " + Header.GrnNbr + " Created Successfully",
                                                "GRN Creation", 
                                                MessageButton.OK, 
                                                MessageIcon.Exclamation);

                ResetGRN();

            }
        }

        private void ResetGRN()
        {

            SetHeader();

        }

        private void EvaluateFormula<T>(T item, bool isInit = false) where T : class
        {
            var formulas = FormulaStore.Instance.GetFormulas<T>();

            foreach (var formula in formulas)
            {
                //if (!isInit && IGNORE_UPDATE.Contains(formula.FieldName)) continue;

                var val = formula.Evaluate<T, decimal>(item, 0M);

                if (item is GrnLineSummary grnLineSumry)
                    copyGRNLineExpression[formula.FieldName].Invoke(grnLineSumry, val);
            }
        }

        private void PopulateUnboundLineDataMap()
        {
            if (copyGRNLineExpression is null) copyGRNLineExpression = new();

            //copyGRNLineExpression.Add($"{nameof(InvoiceLine.InvlTaxableAmount)}", (item, val) => item.InvlTaxableAmount = val);
            copyGRNLineExpression.Add($"{nameof(GrnLineSummary.NetWeight)}", (item, val) => item.NetWeight = val);
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
