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
        private GrnHeader _header;

        [ObservableProperty]
        private DateSearchOption _searchOption;

        [ObservableProperty]
        private DateTime _Today = DateTime.Today;

        private readonly IGrnService _grnService;
        private readonly IProductCategoryService _productCategoryService;
        private readonly IProductService _productService;
        private readonly IProductStockService _productStockService;
        private readonly IMessageBoxService _messageBoxService;
        private readonly IDialogService _dialogService;
        private readonly IMtblReferencesService _mtblReferencesService;

        [ObservableProperty]
        private ObservableCollection<string> supplierReferencesList;

        [ObservableProperty]
        private ObservableCollection<GrnHeader> _grnHdrList;

        [ObservableProperty]
        private ObservableCollection<ProductStock> _productStockList;

        [ObservableProperty]
        private ObservableCollection<GrnLine> _grnLineList;

        [ObservableProperty]
        private GrnHeader _grnHeader;

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

        private Dictionary<int, ObservableCollection<GrnLine>> _lineGrnLookup;

        public ProductStockEntryViewModel(  IGrnService grnService,
                                            IProductService productService,
                                            IProductStockService productStockService,
                                            IDialogService dialogService,
                                            IProductCategoryService productCategoryService,
                                            IMessageBoxService messageBoxService,
                                            IMtblReferencesService mtblReferencesService
                                            )
        {
            _grnService             = grnService;
            _productService         = productService;
            _productStockService    = productStockService;
            _dialogService          = dialogService;
            _productCategoryService = productCategoryService;
            _messageBoxService      = messageBoxService;
            _mtblReferencesService  = mtblReferencesService;

            _lineGrnLookup = new();

            PopulateMtblSupplierListAsync();
            PopulateOpenGRN();
            PopulateProductCategoryList();

            SetHeader();

        }

        private void SetHeader()
        {
            Header = new();
            ProductStockList = new();
        }

        private async void PopulateMtblSupplierListAsync()
        {

            var suppRefServiceList = await _mtblReferencesService.GetReferenceList("SUPPLIERS");

            SupplierReferencesList = new(suppRefServiceList.Select(x => x.RefValue));

        }

        private async void PopulateOpenGRN()
        {
            var grnHdrList = await _grnService.GetBySupplier("JP");
        }

        private void PopulateProductCategoryList()
        {
        }

        [RelayCommand]
        private async Task SelectedGRN()
        {
            var grnLineList = await _grnService.GetByHdrGkey(SelectedGrn.GKey);

            var grnHeader = await _grnService.GetByHdrGkey(SelectedGrn.GKey);

        }

        [RelayCommand]
        private async Task SelectionGrnSumryListChangedAsync()
        {

            if (SelectedGrnLineSumry is null) return;


            if ( _lineGrnLookup.TryGetValue(SelectedGrnLineSumry.GKey, out var grnLines) )
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


        [RelayCommand]
        private async Task Submit()
        {

            if (Header is not null)
            {

                GrnLineList.ForEach(x =>
                {
                    x.GrnHdrGkey = Header.GKey;
                    //x.ProductDesc = 
                    //x.ProductPurity =
                    x.Status = "Closed";

                    ProcessStockLinesAsync(x);
                });

                if (GrnLineList is not null)
                    await _grnService.CreateGrnLine(GrnLineList);

                if (ProductStockList is not null)
                {

                    var skuNumber = await _mtblReferencesService.GetReference("PRODUCT_CATEGORY", GrnLineList[0].ProductId);

                    ProductStockList.ForEach(async x =>
                    {
                        await _productStockService.CreateProductStock(x);
                      //  skuNumber = skuNumber + 1;
                    });
                }
                _messageBoxService.ShowMessage("Stock Updated Successfully", "Stock Created",
                                    MessageButton.OK, MessageIcon.Exclamation);

                ResetGrn();
                //.CreateInvoiceLine(Header.Lines);

                // await ProcessOldMetalTransaction();

            }
        }

/*        private async Task<int> GetSKUNumberAsync(string productCategory)
        {
            var skuNumber = await _mtblReferencesService.GetReference("PRODUCT_CATEGORY", productCategory);
            return skuNumber;
        }*/

        private async Task ProcessStockLinesAsync(GrnLine grnLineStock)
        {

           ProductStock productStock = new ProductStock();

           productStock.ProductGkey = grnLineStock.ProductGkey;
           productStock.GrossWeight = grnLineStock.GrossWeight;
            //  productStock.ProductSku = 

            var mtblReference = await _mtblReferencesService.GetReference("PRODUCT_CATEGORY", grnLineStock.ProductId);
            var sku = int.Parse(mtblReference.RefValue) + 1;

            //value.InvNbr = string.Format("{0}{1}", DocumentPrefixFormat, voucherType?.LastUsedNumber?.ToString("D4"));


            if (mtblReference != null)
            {
                productStock.ProductSku = string.Format("{0}{1}", "C", sku);
            }

            ProductStockList.Add(productStock);

            mtblReference.RefValue = sku.ToString();
            await _mtblReferencesService.UpdateReference(mtblReference);
        }

        private void ResetGrn()
        {

            SetHeader();
           
        }

    }
}
