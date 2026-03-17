using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Data;
using DevExpress.Mvvm;
using DevExpress.Xpf.Bars;
using DevExpress.XtraLayout.Customization;
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

        // private readonly ReferenceLoader _referenceLoader;

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
        private ProductStock _SelectedGridLine;

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

        private int productSkuSeq = 0;
        private MtblReference mtblReference;
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

            //_referenceLoader = referenceLoader;

            SetThisCompany();

            _ = PopulateProductCategoryLst();
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
                // Load all stock only products for choosen category
                var product = await _productStockService.GetCategoryList(SelectedCategory);

                // Build a lookup of duplicate SKUs
                var duplicateSkus = product
                    .GroupBy(p => p.ProductSku)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToHashSet();

                // Mark each product with flag
                foreach (var prods in product)
                {
                    prods.DuplicateFlag = duplicateSkus.Contains(prods.ProductSku) ? "D" : "";
                }

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

        private ProductStock SetProduct(ProductStock prdStk)
        {
            // var prdStk = await _productStockService.GetProductStock(SelectedGridLine.GKey);
            //  if (prdStk is not null)
            //    return;     //avoid duplication of product stock

            ProductStock newProductStock = new ProductStock();

            newProductStock.ProductGkey = prdStk.ProductGkey;
            newProductStock.GrossWeight = prdStk.GrossWeight;
            newProductStock.StoneWeight = prdStk.StoneWeight;
            newProductStock.NetWeight = prdStk.NetWeight;
            newProductStock.SuppliedGrossWeight = prdStk.GrossWeight;
            newProductStock.AdjustedWeight = 0;
            newProductStock.SoldWeight = 0;
            newProductStock.BalanceWeight = prdStk.NetWeight;
            newProductStock.SuppliedQty = prdStk.SuppliedQty;
            newProductStock.SoldQty = 0;
            newProductStock.StockQty = 1; //hardcoded to be reviewed later >>>> grnLineStock.AcceptedQty;
            newProductStock.Status = "In-Stock";
            newProductStock.SupplierId = prdStk.SupplierId;
            newProductStock.IsProductSold = false;
            newProductStock.Category = prdStk.Category;
            newProductStock.ProductSku = prdStk.ProductSku;
            newProductStock.IsBarcodePrinted = true;
            newProductStock.CreatedOn = DateTime.Now;
            newProductStock.CreatedBy = "ReCreated";
            newProductStock.WastageAmount = 0;
            newProductStock.WastagePercent = 0;

            return newProductStock;

            //await PrintTagAsync(productStock);


        }

        [RelayCommand]
        private async Task PrintTagAsync(ProductStock productStock)
        {
            
            var newPrdStk = productStock;

            var productView = await _productViewService.GetByCategory(productStock.Category);

            if (productStock.IsReAssign)
            {
                mtblReference = await _mtblReferencesService.GetReference("PRODUCT_CATEGORY", SelectedCategory);

                productSkuSeq = int.Parse(mtblReference.RefValue);

                var oldPrdStk = productStock;

                newPrdStk = SetProduct(productStock);

                newPrdStk = SetProductSku(newPrdStk, productView);


                //save to db immediate - if list has 100 or more nos, it takes lots of time
                await _productStockService.CreateProductStock(newPrdStk);

                oldPrdStk.Status = "InActive";
                oldPrdStk.IsProductSold = true;
                oldPrdStk.NetWeight = 0;
                oldPrdStk.BalanceWeight = 0;
                oldPrdStk.StockQty = 0;
                oldPrdStk.ModifiedBy = "DeActivated";
                oldPrdStk.ModifiedOn = DateTime.Now;
                await _productStockService.UpdateProductStock(oldPrdStk);

                //if user maintains seq nbr for product sku - this needs to be executed - but in difference place - need to fix
                mtblReference.RefValue = productSkuSeq.ToString();
                await _mtblReferencesService.UpdateReference(mtblReference);

            }

                PrintTag(newPrdStk, productView);

                //after modification refresh the list
                await RefreshBarcodeAsync();
            
        }

        private void PrintTag(ProductStock newPrdStk, ProductView productView)
        {

            if (newPrdStk.NetWeight > 0.00m)
            {

                var result = BarCodePrint.ProcessBarCode(newPrdStk.ProductSku, productView.Description,
                                                                              newPrdStk.VaPercent.Value,
                                                                              newPrdStk.NetWeight.Value,
                                                                              newPrdStk.StoneWeight.Value,
                                                                              productView.Purity,
                                                                              Company.CompanyName);

            }

        }

        private ProductStock SetProductSku(ProductStock prdSku, ProductView prdView)
        {

            var tagPurityCode = "";
            if (prdView.Purity == "916")
                tagPurityCode = "2";
            else if (prdView.Purity == "750")
                tagPurityCode = "8";

            productSkuSeq++;

            var productSku = string.Format("{0}{1}{2}{3}", mtblReference.RefDesc, tagPurityCode, "-", productSkuSeq.ToString("D4")); //, grnLine.NetWeight);
            prdSku.ProductSku = productSku;

            return prdSku;

        }
    }
}
