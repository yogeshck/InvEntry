using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.PivotGrid.CustomFunctions;
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
    private Customer _seller;

    [ObservableProperty]
    private ObservableCollection<string> productCategoryList;

    [ObservableProperty]
    private ObservableCollection<OldMetalTransaction> _omTransUIList;

    [ObservableProperty]
    private ObservableCollection<OldMetalTransaction> selectedRows;

    [ObservableProperty]
    private OldMetalTransaction _omTrans;

    [ObservableProperty]
    private ObservableCollection<string> _metalList;

    [ObservableProperty]
    private ObservableCollection<MtblReference> _mtblReferencesList;

    [ObservableProperty]
    private ObservableCollection<string> _stateReferencesList;

    [ObservableProperty]
    private ProductView _oldMetalProduct;

    [ObservableProperty]
    public bool _customerReadOnly;

    [ObservableProperty]
    private bool _isCustomerModified;

    private string CustName;
    private string CustCity;

    private readonly ReferenceLoader _referenceLoader;

    private readonly ILedgerService _ledgerService;
    private readonly IDialogService _dialogService;
    private readonly IAddressService _addressService;
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

    private bool createSeller = false;
    private bool updateSeller = false;
    private string omOrderNbr;

    private decimal IGSTPercent = 0M;
    private decimal SCGSTPercent = 3M;
    private decimal todaysRate;

    public OldMetalPurchaseViewModel(
                IDialogService dialogService,
                ILedgerService ledgerService,
                IAddressService addressService,
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
        _addressService = addressService;
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

        selectedRows = new();
        //productStockList = new();

        _settingsPageViewModel = settingsPageViewModel;


        _customerReadOnly = false;

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

        StateReferencesList = await _referenceLoader.LoadValuesAsync("CUST_STATE");

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

        if (Seller is not null && Seller.MobileNbr == phoneNumber)
            return;

        CustomerReadOnly = false;
        createSeller = false;
        updateSeller = false;

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Fetching Customer details..."));

        Seller = await _customerService.GetCustomer(phoneNumber);

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

        if (Seller is null)
        {
            _messageBoxService.ShowMessage("No customer details found.", "Customer not found", MessageButton.OK);

            Seller = new();
            Seller.MobileNbr = phoneNumber;
            Seller.Address.GstStateCode = Company.GstCode;
            Seller.Address.State = Company.State;
            Seller.Address.District = Company.District;

            createSeller = true;
            //CustomerState = StateReferencesList.FirstOrDefault(x => x.RefCode == Company.GstCode);
            CustomerState = await _referenceLoader.GetValueAsync("CUST_STATE", Company.GstCode);

            Messenger.Default.Send("CustomerNameUI", MessageType.FocusTextEdit);
        }
        else
        {
            var gstCode = Seller.Address is null ? Company.GstCode : Seller.Address.GstStateCode;

            CustName = Seller.CustomerName;
            CustCity = Seller.Address.City;

            updateSeller = true;

            if (Seller.Address is null)
            {
                Seller.Address = new();
                Seller.Address.GstStateCode = Company.GstCode;
                Seller.Address.City = Company.City;
                Seller.Address.Area = Company.Area;
            }

            //CustomerState = StateReferencesList.FirstOrDefault(x => x.RefCode == gstCode);
            CustomerState = await _referenceLoader.GetValueAsync("CUST_STATE", gstCode);

            // customerCreditCheck(Seller);

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

        var prdView = await _productViewService.GetProduct(ProductIdUI);

        if (prdView is null)
        {
            _messageBoxService.ShowMessage($"No Product found for {ProductIdUI}, Please make sure it exists",
                "Product not found", MessageButton.OK, MessageIcon.Error);
            return;
        }

        var metalPrice = getBilledPrice(prdView.Metal);
        if (metalPrice < 1)
        {
            displayRateErrorMsg();
            return;
        }

        OmTrans = new();

        OmTrans.ProductCategory = prdView.Category;
        //OmTrans.ProductGkey = prdView.ProductSku;
        OmTrans.ProductId = prdView.Id;
        //productStk.ProductSku;
        OmTrans.TransactedRate = metalPrice;
        OmTrans.Metal = prdView.Metal;
        OmTrans.Purity = prdView.Purity;


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
    private void EvaluateOldMetalTransaction(OldMetalTransaction oldMetalTransaction)
    {


        //   if (oldMetalTransaction.TransactedRate.GetValueOrDefault() < 1)
        //       oldMetalTransaction.TransactedRate = metalPrice; // todaysRate;

        //  oldMetalTransaction.Purity = OldMetalProduct.Purity;

        oldMetalTransaction.WastagePercent = 0;
        oldMetalTransaction.WastageWeight = 0;

        oldMetalTransaction.NetWeight = (
                                           oldMetalTransaction.GrossWeight.GetValueOrDefault() -
                                           oldMetalTransaction.StoneWeight.GetValueOrDefault() -
                                           oldMetalTransaction.WastageWeight.GetValueOrDefault()
                                        );

        oldMetalTransaction.TotalProposedPrice = oldMetalTransaction.NetWeight.GetValueOrDefault() *
                                                    oldMetalTransaction.TransactedRate.GetValueOrDefault();

        if (oldMetalTransaction.FinalPurchasePrice is null)
        {
            oldMetalTransaction.FinalPurchasePrice = oldMetalTransaction.TotalProposedPrice;
        }

        oldMetalTransaction.DocRefType = "Old Purchase";

        oldMetalTransaction.TransType = "OG Purchase";

        oldMetalTransaction.TransDate = DateTime.Now;
        oldMetalTransaction.DocRefDate = DateTime.Now;
        oldMetalTransaction.CustGkey = Seller.GKey;
        oldMetalTransaction.CustMobile = Seller.MobileNbr;

        //oldMetalTransaction.Purity = "OG Purchase";

    }

    private async Task ProcessOldMetalTransactionAsync()
    {

        foreach (var omTrans in OmTransUIList)
        {
            omTrans.CustGkey = Seller.GKey;
        }

        var result = await _oldMetalTransactionService.CreateOldMetalTransaction(OmTransUIList);

        omOrderNbr = result.ToString();


    }

    [RelayCommand]
    private void ResetForm()
    {
        SetHeader();
        SetThisCompany();
        Seller = null;
        OmTrans = null;
        CustomerPhoneNumber = null;
        CustomerState = Company.State;
        OmTransUIList.Clear();
        CustName = null;
        CustCity = null;

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

        if (Seller is null || string.IsNullOrEmpty(Seller.CustomerName))
        {
            _messageBoxService.ShowMessage("Customer information is not provided", "Customer info",
                                                MessageButton.OK, MessageIcon.Hand);
            return;
        }

        if (createSeller)
        {
            Seller = await _customerService.CreateCustomer(Seller);
        }
        else if (updateSeller)
        {
            if (CustName != Seller.CustomerName)
            {
                await _customerService.UpdateCustomer(Seller);
            }

            if (CustCity != Seller.Address.City)
            {
                await _addressService.UpdateAddress(Seller.Address);
            }
        }
        await ProcessOldMetalTransactionAsync();


        _messageBoxService.ShowMessage("Customer PO " + omOrderNbr + " Created Successfully", "Cust PO Created",
                                            MessageButton.OK, MessageIcon.Exclamation);

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Print Purchase Order..."));

        var waitVM = WaitIndicatorVM.ShowIndicator("Please wait.... preparing print document.... .");

        SplashScreenManager.CreateWaitIndicator(waitVM).Show();

        PrintPreviewOMPurchase();

        SplashScreenManager.ActiveSplashScreens.FirstOrDefault(x => x.ViewModel == waitVM).Close();

        //TODO    PrintPreviewPurcahseCommand.NotifyCanExecuteChanged();
        //TODO    PrintPurchaseCommand.NotifyCanExecuteChanged();
        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

        ResetForm();

        //      }
    }


    private bool CanPrintCustomerPurchase()
    {
        return string.IsNullOrEmpty(OmTrans?.TransNbr);
    }

    [RelayCommand(CanExecute = nameof(CanPrintCustomerPurchase))]
    private void PrintPreviewOMPurchase()
    {
        _reportDialogService.PrintPreviewOMPurchase(omOrderNbr);

    }

}
