using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Printing;
using InvEntry.Extension;
using InvEntry.Helper;
using InvEntry.Helpers;
using InvEntry.Models;
using InvEntry.Models.Extensions;
using InvEntry.Reports;
using InvEntry.Services;
using InvEntry.Store;
using InvEntry.Tally;
using InvEntry.Utils;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Text;
using System.Linq;
using System.Threading.Tasks;
using IDialogService = DevExpress.Mvvm.IDialogService;

namespace InvEntry.ViewModels;


public partial class OldMetalPurchaseViewModel : ObservableObject
{
    [ObservableProperty]
    private string _customerPhoneNumber;

    [ObservableProperty]
    private InvoiceArReceipt _invoiceArReceipt;

    private ProductStock ProductSkuStock;

    /*    [ObservableProperty]
        private LedgersHeader _ledgerHeader;*/

    [ObservableProperty]
    private string _productIdUI;

    [ObservableProperty]
    private OrgThisCompanyView _company;

    [ObservableProperty]
    private ObservableCollection<string> productCategoryList;

    [ObservableProperty]
    private ObservableCollection<OldMetalTransaction> selectedRows;

    [ObservableProperty]
    private ObservableCollection<string> _metalList;

    [ObservableProperty]
    private ObservableCollection<MtblReference> _mtblReferencesList;

    [ObservableProperty]
    private OldMetalTransaction omHeader;

    private readonly ReferenceLoader _referenceLoader;

    private readonly ILedgerService _ledgerService;
    private readonly IDialogService _dialogService;
    private readonly IVoucherService _voucherService;
    private readonly ICustomerService _customerService;
    private readonly IDialogService _reportDialogService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IMtblLedgersService _mtblLedgersService;
    private readonly IProductViewService _productViewService;
    private readonly IReportFactoryService _reportFactoryService;
    private readonly IMtblReferencesService _mtblReferencesService;
    private readonly IProductCategoryService _productCategoryService;
    private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;
    private readonly IOldMetalTransactionService _oldMetalTransactionService;

    private List<MtblReference> _gstTaxRefList;

    private SettingsPageViewModel _settingsPageViewModel;
    private Dictionary<string, Action<InvoiceLine, decimal?>> copyInvoiceExpression;
    private Dictionary<string, Action<InvoiceHeader, decimal?>> copyHeaderExpression;

    private decimal IGSTPercent = 0M;
    private decimal SCGSTPercent = 3M;
    private decimal todaysRate;
    public OldMetalPurchaseViewModel(
        ICustomerService customerService,
        IProductViewService productViewService,
        IProductStockService productStockService,
        IProductStockSummaryService productStockSummaryService,
        IProductTransactionService productTransactionService,
        //IProductTransactionSummaryService productTransactionSummaryService,
        IDialogService dialogService,
        IInvoiceService invoiceService,
        ILedgerService ledgerService,
        IProductCategoryService productCategoryService,
        IMessageBoxService messageBoxService,
        IVoucherService voucherService,
        IInvoiceArReceiptService invoiceArReceiptService,
        IOrgThisCompanyViewService orgThisCompanyViewService,
        IOldMetalTransactionService oldMetalTransactionService,
        IMtblReferencesService mtblReferencesService,
        IMtblLedgersService mtblLedgersService,
        SettingsPageViewModel settingsPageViewModel,
        IReportFactoryService reportFactoryService,
        ReferenceLoader referenceLoader,
    [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
    {

        _dialogService = dialogService;
        _ledgerService = ledgerService;
        _customerService = customerService;
        _messageBoxService = messageBoxService;
        _productViewService = productViewService;
        _mtblLedgersService = mtblLedgersService;
        _reportDialogService = reportDialogService;
        _reportFactoryService = reportFactoryService;
        _productCategoryService = productCategoryService;
        _orgThisCompanyViewService = orgThisCompanyViewService;



        _oldMetalTransactionService = oldMetalTransactionService;
        _voucherService = voucherService;
        _mtblReferencesService = mtblReferencesService;

        _referenceLoader = referenceLoader;

        //_productTransactionSummaryService = productTransactionSummaryService;

        selectedRows = new();
        //productStockList = new();

        _settingsPageViewModel = settingsPageViewModel;

        SetMetalPrice();
        SetHeader();
        SetThisCompany();
        //SetMasterLedger();   TODO

        _ = LoadReferencesAsync();

        PopulateProductCategoryList();
        //PopulateStateList();
        //PopulateUnboundLineDataMap();
        PopulateMtblRefNameList();
        PopulateMetalList();
        PopulateTaxList();
        //PopulateSalesPersonList();

        //PopulateUnboundHeaderDataMap();
    }

    private async void SetThisCompany()
    {
        Company = new();
        Company = await _orgThisCompanyViewService.GetOrgThisCompany();
        //Header.TenantGkey = Company.TenantGkey;
        //Header.GstLocSeller = Company.GstCode;
    }

    private void SetMetalPrice()
    {
        var metalPrice = getBilledPrice("GOLD");
        if (metalPrice < 1)
        {
            displayRateErrorMsg();
            //return;
        }

        todaysRate = (decimal)metalPrice;
    }

    private decimal getBilledPrice(string metal)
    {
        var metalPrice = _settingsPageViewModel.GetPrice(metal);

        if (metalPrice is null)
        {
            metalPrice = -1;
        }

        return (decimal)metalPrice;
    }

/*    private async void SetMasterLedger()
    {
        MtblLedger = await _mtblLedgersService.GetLedger(1000);   //pass account code
    }*/

    private async void PopulateProductCategoryList()
    {
        var list = await _productCategoryService.GetProductCategoryList();
        if (list is null)
        {
            return;
        }

        ProductCategoryList = new(list
                                .Where(x => !x.Name.StartsWith("OLD"))
                                .Select(x => x.Name));
    }


    private async Task LoadReferencesAsync()
    {

        //CustOrdStatusList = await _referenceLoader.LoadValuesAsync("CUST_ORD_STATUS");

        //StateReferencesList = await _referenceLoader.LoadValuesAsync("CUST_STATE");

       // PaymentModeList = await _referenceLoader.LoadValuesAsync("PAYMENT_MODE");

        // SalesPersonReferencesList = await _referenceLoader.LoadValuesAsync("SALES_PERSON");

    }

    private async void PopulateTaxList()
    {

        var gstTaxRefList = await _mtblReferencesService.GetReferenceList("GST");
        if (gstTaxRefList is not null)
        {
            _gstTaxRefList = new(gstTaxRefList);
        }
    }

    /*    private decimal GetGstTaxRate()
        {
            var taxRate = GstTaxList.FirstOrDefault(x => x.RefCode.Equals("SGST"));
        }*/

    private async void PopulateMetalList()
    {
        var metalRefList = await _mtblReferencesService.GetReferenceList("OLD_METALS");
        MetalList = new(metalRefList.Select(x => x.RefValue));
    }

    private async void PopulateMtblRefNameList()
    {
        var mtblRefList = await _mtblReferencesService.GetReferenceList("PAYMENT_MODE");
        MtblReferencesList = new(mtblRefList);
    }

    private void SetHeader()
    {
        omHeader = new()
        {
            TransDate = DateTime.Now,
            //IsTaxApplicable = true,
            //     GstLocSeller = Company.GstCode,
            //    TenantGkey = Company.TenantGkey
        };
    }

    private void displayRateErrorMsg()
    {
        _messageBoxService.ShowMessage($"Todays Rate not entered in system, set the rate and start invoicing....",
                                        "Todays Rate not found", MessageButton.OK, MessageIcon.Error);

    }

}
