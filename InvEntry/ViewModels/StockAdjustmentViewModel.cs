using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Charts.Designer.Native;
using DevExpress.Data.Browsing;
using DevExpress.DataAccess.Sql;
using DevExpress.Mvvm;
using InvEntry.Helpers;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Tally;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace InvEntry.ViewModels;
public partial class StockAdjustmentViewModel : ObservableObject
{
    private ProductStock ProductSkuStock;
    private readonly IProductStockService _productStockService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IMtblReferencesService _mtblReferencesService;
    private readonly IProductCategoryService _productCategoryService;
    private readonly IProductTransactionService _productTransactionService;

    [ObservableProperty]
    private ObservableCollection<string> _stockAdjReasonList;

    private readonly ReferenceLoader _referenceLoader;

    [ObservableProperty]
    private ObservableCollection<MtblReference> mtblReferencesList;

    [ObservableProperty]
    private ObservableCollection<string> _productCategoryList;

    [ObservableProperty]
    private ObservableCollection<string> _productSkuStrList;

    [ObservableProperty]
    private ProductStock _productStock;

    [ObservableProperty]
    private ProductStock _userEntryStock;

    [ObservableProperty]
    private ProductStock _modifiedProductStock;

    [ObservableProperty]
    private string selectedCategoryId;

    [ObservableProperty]
    private string selectedProductSku;

    [ObservableProperty]
    private string _selectedReasonCode;

    [ObservableProperty]
    private bool isAddSelected = true;

    [ObservableProperty]
    private bool isReduceSelected;

    public StockAdjustmentViewModel(
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

        _ = LoadReferencesAsync();

        PopulateProductCategoryList();
        _ = PopulateProductSkuList();

        SetBase();

    }

    private void SetBase()
    {
        ProductStock = new ProductStock();
        UserEntryStock = new ProductStock();
        ModifiedProductStock = new ProductStock();

    }

    partial void OnIsAddSelectedChanged(bool value)
    {
        if (value)
            IsReduceSelected = false;


        Recalculate();
    }

    partial void OnIsReduceSelectedChanged(bool value)
    {
        if (value)
            IsAddSelected = false;

        Recalculate();
    }

    private async void PopulateProductCategoryList()
    {
        var list = await _productCategoryService.GetProductCategoryList();
        ProductCategoryList = new(list
                                .Select(x => x.Name));
    }

    private async Task PopulateProductSkuList(string category = "RING")
    {
        var skuList = await _productStockService.GetCategoryList(category);
        ProductSkuStrList = new(skuList
                               .Select(x => x.ProductSku));
    }

    partial void OnSelectedCategoryIdChanged(string value)
    {
        _ = PopulateProductSkuList(value);
    }

    partial void OnSelectedProductSkuChanged(string value)
    {

        _ = LoadStockAsync(selectedProductSku);

    }

    private bool Validate()
    {
        if (SelectedReasonCode is null)
        {
            MessageBox.Show("Please select reason to continue....");
            return false;
        }

        //need to work out - stone modification
        //if (ProductStock.StoneWeight > 0 && ProductStock.StoneWeight < ProductStock.GrossWeight )
        //    { }

        return true;
    }

    private async Task LoadStockAsync(string productSku)
    {
        var prdSku = await _productStockService.GetProductStock(productSku);

        // Always create fresh objects so bindings update correctly
        ProductStock = new ProductStock
        {
            GrossWeight = prdSku.GrossWeight,
            StoneWeight = prdSku.StoneWeight,
            NetWeight = prdSku.NetWeight,
            ProductSku = prdSku.ProductSku,
            Category = prdSku.Category
        };

        ModifiedProductStock = new ProductStock
        {
            GrossWeight = prdSku.GrossWeight,
            StoneWeight = prdSku.StoneWeight,
            NetWeight = prdSku.NetWeight,
            ProductSku = prdSku.ProductSku,
            Category = prdSku.Category
        };

        // UserEntryStock stays as a fresh object from SetBase()
        Recalculate();
    }




    private async Task LoadReferencesAsync()
    {
        StockAdjReasonList = await _referenceLoader.LoadValuesAsync("STOCK_ADJUSTMENTS");
    }

    private void ReprintTag_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Tag reprinted successfully.");
    }

    /*   private async Task<string> FetchReasonDesc()
       {
           var notes = "";

           if (SelectedReasonCode is not null)
           {
               notes = await _referenceLoader.GetValueAsync("STOCK_ADJUSTMENT", SelectedReasonCode);

           }
           return notes;
       }
   */
    private void Recalculate()
    {
        if (ProductStock is null || UserEntryStock is null || ProductStock.GrossWeight == 0
                    || SelectedProductSku is null)
            return;

        decimal grossInput = (decimal)UserEntryStock.GrossWeight.GetValueOrDefault();
        decimal stoneInput = (decimal)UserEntryStock.StoneWeight.GetValueOrDefault();

        decimal modifiedGross;
        decimal modifiedStone;
        decimal modifiedNet;

        if (IsAddSelected)
        {
            modifiedGross = (decimal)(ProductStock.GrossWeight + grossInput);
            modifiedStone = (decimal)ProductStock.StoneWeight + stoneInput;
        }
        else
        {
            modifiedGross = (decimal)ProductStock.GrossWeight - grossInput;
            modifiedStone = (decimal)ProductStock.StoneWeight - stoneInput;
        }

        modifiedNet = modifiedGross - modifiedStone;

        ModifiedProductStock.GrossWeight = modifiedGross;
        ModifiedProductStock.StoneWeight = modifiedStone;
        ModifiedProductStock.NetWeight = modifiedNet;
        ModifiedProductStock.AdjustedWeight = UserEntryStock.GrossWeight;
        ModifiedProductStock.BalanceWeight = modifiedGross;

        OnPropertyChanged(nameof(CanApplyAdjustment));
    }

    public bool CanApplyAdjustment
    {
        get
        {
            if (SelectedProductSku == null)
                return false;

            if (UserEntryStock == null || UserEntryStock.GrossWeight <= 0)
                return false;

            if (IsReduceSelected && ProductStock != null &&
                UserEntryStock.GrossWeight > ProductStock.GrossWeight)
                return false;

            if (ModifiedProductStock == null)
                return false;

            if (ModifiedProductStock.StoneWeight > ModifiedProductStock.GrossWeight)
                return false;

            return true;
        }

    }


    [RelayCommand]
    private async Task ApplyAdjustment()
    {

        if (!Validate())
            return;

        if (!CanApplyAdjustment)
            return;

        // we need to create product transaction record
        // how about summary - if category changes summary has to be updated ??? to be study the reason

        await _productStockService.UpdateProductStock(ModifiedProductStock);

        await CreateProductTransaction(ModifiedProductStock);

        _messageBoxService.ShowMessage("Stock " + ModifiedProductStock.ProductSku + " has been adjusted", "Stock Adjusted",
                                    MessageButton.OK, MessageIcon.Exclamation);

        // Reload stock from DB
        //will review await LoadStockAsync(adjustment.ProductSku);
        ResetSAN();

    }

    [RelayCommand]
    private void ResetSAN()
    {
        // Reinitialize base objects for the next adjustment
        SetBase();

        // Clear selection state
        SelectedCategoryId = null;
        SelectedProductSku = null;
        SelectedReasonCode = null;
        IsAddSelected = true;
        IsReduceSelected = false;

    }


    [RelayCommand]
    private void ReprintTag()
    {
        // Call tag service
    }

    private async Task CreateProductTransaction(ProductStock prdStock)
    {
        ProductTransaction productTrans = new();

        productTrans.OpeningGrossWeight = prdStock.GrossWeight.GetValueOrDefault();
        productTrans.OpeningStoneWeight = prdStock.StoneWeight.GetValueOrDefault();
        productTrans.OpeningNetWeight = prdStock.NetWeight.GetValueOrDefault();

        productTrans.ObQty = prdStock.StockQty.GetValueOrDefault();

        productTrans.ProductSku = prdStock.ProductSku;
        //productTrans.RefGkey = line.GKey;
        productTrans.TransactionDate = DateTime.Now;
        productTrans.ProductCategory = prdStock.Category;

        productTrans.TransactionType = "Adjustment";
        productTrans.DocumentNbr = "SAN";  //doc to be generated 
        //productTrans.DocumentDate = DateTime.Now;
        productTrans.DocumentType = "SAN";
        productTrans.VoucherType = "SAN";

        productTrans.Notes = SelectedReasonCode;

        productTrans.TransactionQty = prdStock.StockQty.GetValueOrDefault();
        productTrans.CbQty = prdStock.StockQty.GetValueOrDefault();

        productTrans.TransactionGrossWeight = UserEntryStock.GrossWeight.GetValueOrDefault();
        productTrans.TransactionStoneWeight = UserEntryStock.StoneWeight.GetValueOrDefault();
        productTrans.TransactionNetWeight = UserEntryStock.NetWeight.GetValueOrDefault();

        productTrans.CreatedOn = DateTime.Now;

        /*            productTrans.ClosingGrossWeight = productSumryStk.GrossWeight.GetValueOrDefault()
                                                                    - line.ProdGrossWeight.GetValueOrDefault();
                    productTrans.ClosingStoneWeight = productSumryStk.StoneWeight.GetValueOrDefault()
                                                                    - line.ProdStoneWeight.GetValueOrDefault();
                    productTrans.ClosingNetWeight = productSumryStk.NetWeight.GetValueOrDefault()
                                                                    - line.ProdNetWeight.GetValueOrDefault();*/

        await _productTransactionService.CreateProductTransaction(productTrans);


    }
}

/*
- Transfer – reducing weight to add into another item
- Damage – item partially damaged, weight reduced
- Loss – weight lost during handling/storage
- Polish – reduction due to polishing or finishing
- Repair – adjustment made during repair process
- Error – correction of a previous entry mistake
- Sample – weight reduced for customer sample/testing
- Return – adjustment due to customer return/exchange
- Scrap – unusable portion removed as scrap
- StoneOut – stone removed, reducing gross weight
*/



