using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Charts.Designer.Native;
using DevExpress.Mvvm;
using InvEntry.Helpers;
using InvEntry.Models;
using InvEntry.Services;
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
    private string selectedCategoryId;

    [ObservableProperty]
    private string selectedProductSku;

    public StockAdjustmentViewModel(
                        IProductStockService productStockService,
                        IProductCategoryService productCategoryService,
                        IMtblReferencesService mtblReferencesService,
                        IMessageBoxService messageBoxService,
                        ReferenceLoader referenceLoader)
    {
        _productStockService = productStockService;
        _productCategoryService = productCategoryService;
        _messageBoxService = messageBoxService;
        _mtblReferencesService = mtblReferencesService;

        _referenceLoader = referenceLoader;

        _ = LoadReferencesAsync();

        PopulateProductCategoryList();
        _ = PopulateProductSkuList();

        ProductStock = new();
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

        _ = GetProductInfo(selectedProductSku);
    }

    private async Task GetProductInfo(string productSku)
    {
        var prdSku = await _productStockService.GetProductStock(productSku);

       // ProductStock.prod
        ProductStock.GrossWeight = prdSku.GrossWeight;
        ProductStock.StoneWeight = prdSku.StoneWeight;
        ProductStock.NetWeight = prdSku.NetWeight;
    }


    private async Task LoadReferencesAsync()
    {

        //CustOrdStatusList = await _referenceLoader.LoadValuesAsync("CUST_ORD_STATUS");

        StockAdjReasonList = await _referenceLoader.LoadValuesAsync("STOCK_ADJUSTMENTS");

        // PaymentModeList = await _referenceLoader.LoadValuesAsync("PAYMENT_MODE");

        // SalesPersonReferencesList = await _referenceLoader.LoadValuesAsync("SALES_PERSON");

    }

    private void ApplyAdjustment_Click(object sender, RoutedEventArgs e)
    {
        // decimal reductionWeight = Convert.ToDecimal(txtReductionWeight.EditValue);
        //  string targetItemId = cmbTargetItem.EditValue?.ToString();
        //  string reasonCode = cmbReason.EditValue?.ToString();

        //  if (string.IsNullOrEmpty(targetItemId) || string.IsNullOrEmpty(reasonCode))
        {
            MessageBox.Show("Please select target item and reason.");
            return;
        }

        // var service = new StockService(new StockRepository(), new TagPrinter());
        //  service.AdjustStock(txtProductId.Text, targetItemId, reductionWeight, reasonCode, Environment.UserName);

        MessageBox.Show("Stock adjustment applied successfully.");
    }

    private void ReprintTag_Click(object sender, RoutedEventArgs e)
    {
        //   var service = new StockService(new StockRepository(), new TagPrinter());
        //   var sourceItem = service.GetItemById(txtProductId.Text);
        //    service.ReprintTag(sourceItem, Environment.UserName);

        MessageBox.Show("Tag reprinted successfully.");
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


}
