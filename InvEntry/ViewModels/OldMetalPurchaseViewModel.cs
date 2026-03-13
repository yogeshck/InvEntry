using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Printing;
using DevExpress.XtraPivotGrid.Data;
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
using System.Printing;
using System.Threading.Tasks;
using IDialogService = DevExpress.Mvvm.IDialogService;

namespace InvEntry.ViewModels;


public partial class OldMetalPurchaseViewModel : ObservableObject
{
    [ObservableProperty]
    private string _customerPhoneNumber;

    private ProductStock ProductSkuStock;

    [ObservableProperty]
    private string _customerState;

    /*    [ObservableProperty]
        private LedgersHeader _ledgerHeader;*/

    [ObservableProperty]
    private string _productIdUI;

    [ObservableProperty]
    private OrgThisCompanyView _company;

    [ObservableProperty]
    private Customer _buyer;

    [ObservableProperty]
    private ObservableCollection<string> productCategoryList;

    [ObservableProperty]
    private ObservableCollection<OldMetalTransaction> _omTransUIList;

    [ObservableProperty]
    private OldMetalTransaction _omTrans;

    [ObservableProperty]
    private ObservableCollection<string> _metalList;

    [ObservableProperty]
    private ObservableCollection<MtblReference> _mtblReferencesList;

    [ObservableProperty]
    private ProductView _oldMetalProduct;

    private readonly ReferenceLoader _referenceLoader;

    private readonly ILedgerService _ledgerService;
    private readonly IDialogService _dialogService;
    private readonly IVoucherService _voucherService;
    private readonly ICustomerService _customerService;
    private readonly IDialogService _reportDialogService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IProductViewService _productViewService;
    private readonly IReportFactoryService _reportFactoryService;
    private readonly IMtblReferencesService _mtblReferencesService;
    private readonly IProductCategoryService _productCategoryService;
    private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;
    private readonly IOldMetalTransactionService _oldMetalTransactionService;
    private readonly IProductStockSummaryService _productStockSummaryService;

    private List<MtblReference> _gstTaxRefList;

    private SettingsPageViewModel _settingsPageViewModel;
    private Dictionary<string, Action<InvoiceLine, decimal?>> copyInvoiceExpression;
    private Dictionary<string, Action<InvoiceHeader, decimal?>> copyHeaderExpression;

    private bool createCustomer = false;

    private decimal IGSTPercent = 0M;
    private decimal SCGSTPercent = 3M;
    private decimal todaysRate;

    public OldMetalPurchaseViewModel(
                IDialogService dialogService,
                ILedgerService ledgerService,
                ICustomerService customerService,
                IMessageBoxService messageBoxService,
                IProductViewService productViewService,
                IProductStockService productStockService,
                IMtblReferencesService mtblReferencesService,
                SettingsPageViewModel settingsPageViewModel,
                IReportFactoryService reportFactoryService,
                IProductCategoryService productCategoryService,
                IProductTransactionService productTransactionService,
                IOrgThisCompanyViewService orgThisCompanyViewService,
                IOldMetalTransactionService oldMetalTransactionService,
                IProductStockSummaryService productStockSummaryService,
                ReferenceLoader referenceLoader,
    [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
    {

        _dialogService = dialogService;
        _ledgerService = ledgerService;
        _customerService = customerService;
        _messageBoxService = messageBoxService;
        _productViewService = productViewService;
        _reportDialogService = reportDialogService;
        _reportFactoryService = reportFactoryService;
        _productCategoryService = productCategoryService;
        _productStockSummaryService = productStockSummaryService;
        _orgThisCompanyViewService = orgThisCompanyViewService;
        _oldMetalTransactionService = oldMetalTransactionService;
        _mtblReferencesService = mtblReferencesService;

        _referenceLoader = referenceLoader;

        //_productTransactionSummaryService = productTransactionSummaryService;

        //selectedRows = new();
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

        OmTrans = new OldMetalTransaction();
        OmTransUIList = new ObservableCollection<OldMetalTransaction>();

    }

    private void displayRateErrorMsg()
    {
        _messageBoxService.ShowMessage($"Todays Rate not entered in system, set the rate and start invoicing....",
                                        "Todays Rate not found", MessageButton.OK, MessageIcon.Error);

    }

    [RelayCommand]
    private async Task FetchCustomerDetails(EditValueChangedEventArgs args)
    {
        if (args.NewValue is not string phoneNumber) return;

        phoneNumber = phoneNumber.Trim();

        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 10)
            return;

        if (Buyer is not null && Buyer.MobileNbr == phoneNumber)
            return;

        //CustomerReadOnly = false;
        createCustomer = false;

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Fetching Customer details..."));

        Buyer = await _customerService.GetCustomer(phoneNumber);

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

        if (Buyer is null)
        {
            _messageBoxService.ShowMessage("No customer details found.", "Customer not found", MessageButton.OK);

            Buyer = new();
            Buyer.MobileNbr = phoneNumber;
            Buyer.Address.GstStateCode = Company.GstCode;
            Buyer.Address.State = Company.State;
            Buyer.Address.District = Company.District;

            createCustomer = true;
            //CustomerState = StateReferencesList.FirstOrDefault(x => x.RefCode == Company.GstCode);
            CustomerState = await _referenceLoader.GetValueAsync("CUST_STATE", Company.GstCode);

            Messenger.Default.Send("CustomerNameUI", MessageType.FocusTextEdit);
        }
        else
        {
            var gstCode = Buyer.Address is null ? Company.GstCode : Buyer.Address.GstStateCode;

            if (Buyer.Address is null)
            {
                Buyer.Address = new();
                Buyer.Address.GstStateCode = Company.GstCode;
            }

            //CustomerState = StateReferencesList.FirstOrDefault(x => x.RefCode == gstCode);
            CustomerState = await _referenceLoader.GetValueAsync("CUST_STATE", gstCode);

           // customerCreditCheck(Buyer);

            Messenger.Default.Send("ProductIdUIName", MessageType.FocusTextEdit);

        }

        //OmHeader.CustMobile = phoneNumber;
    }

    [RelayCommand]
    private void Focus(TextEdit sender)
    {
        sender.Focus();
    }

    [RelayCommand]
    private async Task FetchProduct()
    {
        if (string.IsNullOrEmpty(ProductIdUI)) return;

        var productStk = await _productViewService.GetProduct(ProductIdUI);

        if (productStk is null)
        {
            _messageBoxService.ShowMessage($"No Product found for {ProductIdUI}, Please make sure it exists",
                "Product not found", MessageButton.OK, MessageIcon.Error);
            return;
        }

        var metalPrice = getBilledPrice(productStk.Metal);
        if (metalPrice < 1)
        {
            displayRateErrorMsg();
            return;
        }

        OmTrans = new();

        OmTrans.ProductCategory = productStk.Category;
        //OmTrans.ProductGkey = productStk.ProductSku;
        OmTrans.ProductId = productStk.Id; 
        //productStk.ProductSku;
        OmTrans.TransactedRate = metalPrice;
        OmTrans.Metal = productStk.Metal;
        OmTrans.Purity = productStk.Purity;


        OmTransUIList.Add(OmTrans);


        //EvaluateFormula(invoiceLine, isInit: true);

        //TODO OmHeader.Add(omMetalTrans);

        ProductIdUI = string.Empty;

       //TODO EvaluateHeader();

        /*        if (invoiceLine.ProdGrossWeight > 0)
                {
                } else
                {
                    _messageBoxService.ShowMessage("Gross Weight cannot be zero ....",
                        "Gross Weight", MessageButton.OK, MessageIcon.Error);
                    return;
                }*/
    }

    [RelayCommand]
    private void CellUpdate(CellValueChangedEventArgs args)
    {
        if (args.Row is OldMetalTransaction line)
        {
            EvaluateOldMetalTransaction(line);
        }
        

            
    }

    [RelayCommand]
    private void  EvaluateOldMetalTransaction(OldMetalTransaction oldMetalTransaction)
    {


        //   if (oldMetalTransaction.TransactedRate.GetValueOrDefault() < 1)
        //       oldMetalTransaction.TransactedRate = metalPrice; // todaysRate;

        //  oldMetalTransaction.Purity = OldMetalProduct.Purity;

        oldMetalTransaction.WastagePercent = 0;

        oldMetalTransaction.NetWeight = (
                                           oldMetalTransaction.GrossWeight.GetValueOrDefault() -
                                           oldMetalTransaction.StoneWeight.GetValueOrDefault() -
                                           oldMetalTransaction.WastageWeight.GetValueOrDefault()
                                        );

        oldMetalTransaction.TotalProposedPrice = oldMetalTransaction.NetWeight.GetValueOrDefault() *
                                                    oldMetalTransaction.TransactedRate.GetValueOrDefault();
        oldMetalTransaction.FinalPurchasePrice = oldMetalTransaction.TotalProposedPrice;

        oldMetalTransaction.DocRefType = "Old Purchase";

        oldMetalTransaction.TransType = "OG Purchase";

        oldMetalTransaction.TransDate = DateTime.Now; 
        oldMetalTransaction.DocRefDate = DateTime.Now;
        oldMetalTransaction.CustGkey = Buyer.GKey;
        oldMetalTransaction.CustMobile = Buyer.MobileNbr;

        oldMetalTransaction.Purity = "OG Purchase";

    }

    private async Task ProcessOldMetalTransaction()
    {

        foreach (var omTrans in OmTransUIList)
        {
            await _oldMetalTransactionService.CreateOldMetalTransaction(omTrans);
        }
    }

    [RelayCommand]
    private void ResetForm()
    {
        SetHeader();
        SetThisCompany();
        Buyer = null;
        OmTrans = null;
        CustomerPhoneNumber = null;
        CustomerState = Company.State;
        OmTransUIList = null;

    }

    [RelayCommand(CanExecute = nameof(CanDeleteSingleRow))]
    private void DeleteSingleRow(OldMetalTransaction omTransline)
    {
        var result = _messageBoxService.ShowMessage("Delete current row", "Delete Row", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

        if (result == MessageResult.No)
            return;

        var index = OmTransUIList.Remove(omTransline);
    }

    private bool CanDeleteSingleRow(OldMetalTransaction omTransLine)
    {
        return omTransLine is not null && OmTransUIList.IndexOf(omTransLine) > -1;
    }

    [RelayCommand]  //(CanExecute = nameof(CanCreatePurchase))]
    private async Task CreatePurchaseOrder()
    {

        if (Buyer is null || string.IsNullOrEmpty(Buyer.CustomerName))
        {
            _messageBoxService.ShowMessage("Customer information is not provided", "Customer info",
                                                MessageButton.OK, MessageIcon.Hand);
            return;
        }

        if (createCustomer)
        {
            Buyer = await _customerService.CreateCustomer(Buyer);
        }

        await ProcessOldMetalTransaction();


                  _messageBoxService.ShowMessage("Customer PO " + OmTrans.TransNbr + " Created Successfully", "Cust PO Created",
                                                      MessageButton.OK, MessageIcon.Exclamation);

            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Print Purchase Order..."));

            var waitVM = WaitIndicatorVM.ShowIndicator("Please wait.... preparing print document.... .");

            SplashScreenManager.CreateWaitIndicator(waitVM).Show();

            //TO DO PrintPreviewOMPurchase();

            SplashScreenManager.ActiveSplashScreens.FirstOrDefault(x => x.ViewModel == waitVM).Close();

        //TODO    PrintPreviewPurcahseCommand.NotifyCanExecuteChanged();
        //TODO    PrintPurchaseCommand.NotifyCanExecuteChanged();
            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

        ResetForm();

  //      }
    }
}
