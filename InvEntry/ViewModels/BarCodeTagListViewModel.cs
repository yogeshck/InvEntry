using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.Bars;
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
        private ObservableCollection<ProductStock> _prdStockList;

        [ObservableProperty]
        private string _selectedCategory;

        [ObservableProperty]
        private string _selectedProductSku;

        [ObservableProperty]
        private string _optionsStr;

        public BarCodeTagListViewModel(
                            IProductViewService productViewService,
                            IProductStockService productStockService,
                            IProductCategoryService productCategoryService,
                            IMtblReferencesService mtblReferencesService,
                            IMessageBoxService messageBoxService,
                            IProductTransactionService productTransactionService,
                            IOrgThisCompanyViewService orgThisCompanyViewService,
                            ReferenceLoader referenceLoader)
        {
            _productStockService = productStockService;
            _productViewService = productViewService;
            _productCategoryService = productCategoryService;
            _messageBoxService = messageBoxService;
            _mtblReferencesService = mtblReferencesService;
            _productTransactionService = productTransactionService;
            _orgThisCompanyViewService = orgThisCompanyViewService;


            _referenceLoader = referenceLoader;

            SetThisCompany();

            _ = PopulateProductCategoryLst();
            //_ = LoadReferencesAsync();

            _ = PopulateProductSkuList();


        }

        private async void SetThisCompany()
        {
            Company = new();
            Company = await _orgThisCompanyViewService.GetOrgThisCompany();
            //Header.TenantGkey = Company.TenantGkey;
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
        private void ResetForm()
        {
            ProductStockList = null;
            SelectedProductSku = null;
        }

        [RelayCommand]
        private async Task RefreshBarcodeAsync()
        {

            ProductStockList = new();

            if (SelectedCategory is not null && SelectedProductSku is null)
            {
                // Load all products in the category
                //var prd = await _productStockService.GetCategoryList(SelectedCategory);
                var product = await _productStockService.GetCategoryList(SelectedCategory);
                ProductStockList = new(product);
            }

            else if (!string.IsNullOrEmpty(SelectedProductSku))
            {
                // Load single product by ID/SKU
                var product = await _productStockService.GetProductStock(SelectedProductSku);
                if (product is not null)
                {
                    ProductStockList = new ObservableCollection<ProductStock> { product };
                }
            }

            //    else if (productStock is not null)
            //     {
            // Reset and show only this product
            //         ProductStockList = new List<ProductStock> { productStock };
            //     }
        }


        [RelayCommand]
        private async Task PrintTagAsync(ProductStock productStock)
        {

            var productView = await _productViewService.GetByCategory(productStock.Category);

            if (productStock.NetWeight > 0.00m)
            {

                var result = BarCodePrint.ProcessBarCode(productStock.ProductSku, productView.Description,
                                                                              productStock.VaPercent.Value,
                                                                              productStock.NetWeight.Value,
                                                                              productStock.StoneWeight.Value,
                                                                              productView.Purity, 
                                                                              Company.CompanyName);

            }
        }
    }
}
