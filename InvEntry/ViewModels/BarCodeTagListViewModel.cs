using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using Ghostscript.NET.PDFA3Converter.ZUGFeRD;
using InvEntry.Helpers;
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
    public partial class BarCodeTagListViewModel : ObservableObject
    {

        private readonly ReferenceLoader _referenceLoader;

        [ObservableProperty]
        private ObservableCollection<MtblReference> mtblReferencesList;

        [ObservableProperty]
        private ObservableCollection<string> _productSkuStrList;

        private readonly IProductViewService _productViewService;
        private readonly IProductTransactionService _productTransactionService;
        private readonly IProductStockService _productStockService;
        private readonly IMessageBoxService _messageBoxService;
        private readonly IDialogService _dialogService;
        private readonly IMtblReferencesService _mtblReferencesService;
        private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;
        private readonly IProductCategoryService _productCategoryService;


        [ObservableProperty]
        private ObservableCollection<string> _productCategoryLst;

        [ObservableProperty]
        private ObservableCollection<string> _productCategoryList;

        [ObservableProperty]
        private ObservableCollection<ProductStock> _productStockList;

        [ObservableProperty]
        private OrgThisCompanyView _company;

       // [ObservableProperty]
       // private Product _product;

        [ObservableProperty]
        private string _selectedCategory;

        [ObservableProperty]
        private string _selectedProductSku;

        public BarCodeTagListViewModel(
                            IProductStockService productStockService,
                            IProductCategoryService productCategoryService,
                            IMtblReferencesService mtblReferencesService,
                            IMessageBoxService messageBoxService,
                            IProductTransactionService productTransactionService,
                            ReferenceLoader referenceLoader)
        {
            _productStockService = productStockService;
            _productCategoryService = productCategoryService;
            _messageBoxService = messageBoxService;
            _mtblReferencesService = mtblReferencesService;
            _productTransactionService = productTransactionService;

            _referenceLoader = referenceLoader;

            _ = PopulateProductCategoryLst();
            //_ = LoadReferencesAsync();

            _ = PopulateProductSkuList();


        }

        private async Task PopulateProductCategoryLst()
        {
            var categoryList = await _productCategoryService.GetProductCategoryList();

            ProductCategoryList = new(categoryList.
                                            Select(x => x.Name));

        }

        private async Task PopulateProductSkuList(string category = "RING")
        {
            var skuList = await _productStockService.GetCategoryList(category);
            ProductSkuStrList = new(skuList
                                   .Select(x => x.ProductSku));
        }

        partial void OnSelectedCategoryChanged(string value)
        {
            _ = PopulateProductSkuList(value);
        }

        [RelayCommand]
        private async Task RefreshBarcodeAsync()
        {

            var productStock = await _productStockService.GetProductStock(SelectedProductSku);

/*            var invoicesResult = await _invoiceService.GetAll(SearchOption);
            if (invoicesResult is not null)
                Invoices = null;
            Invoices = new(invoicesResult);*/
        }
    }
}
