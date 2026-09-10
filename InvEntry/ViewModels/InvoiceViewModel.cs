using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Printing;
using InvEntry.Contracts.Invoices;
using InvEntry.Extension;
using InvEntry.Helper;
using InvEntry.Helpers;
using InvEntry.Mappers.Invoices;
using InvEntry.Models;
using InvEntry.Models.Extensions;
using InvEntry.Reports;
using InvEntry.Services;
using InvEntry.Store;
using InvEntry.Utils;
using InvEntry.Utils.Options;
using InvEntry.ViewModels.Invoices;
using InvEntry.Views.Invoice;
using InvEntry.Views.Invoices;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using IDialogService = DevExpress.Mvvm.IDialogService;

namespace InvEntry.ViewModels;

public partial class InvoiceViewModel : ObservableObject
{
    [ObservableProperty]
    private string _customerPhoneNumber;

    [ObservableProperty]
    private string _customerState;
    //private MtblReference _customerState;

    [ObservableProperty]
    private MtblReference _salesPerson;

    [ObservableProperty]
    private Customer _buyer;

    [ObservableProperty]
    private OrgThisCompanyView _company;

    [ObservableProperty]
    private InvoiceHeader _header;

    [ObservableProperty]
    private InvoiceArReceipt _invoiceArReceipt;

    private ProductStock ProductSkuStock;

    /*    [ObservableProperty]
        private LedgersHeader _ledgerHeader;*/

    [ObservableProperty]
    private string _productIdUI;

    [ObservableProperty]
    private MtblLedger _mtblLedger;

    [ObservableProperty]
    private string _productSku;

    [ObservableProperty]
    private string _oldMetalIdUI;

    [ObservableProperty]
    public bool _customerReadOnly;

    [ObservableProperty]
    public bool _isRefund;

    [ObservableProperty]
    public bool _isBalance;

    [ObservableProperty]
    private ObservableCollection<InvoiceLine> selectedRows;

    /*    [ObservableProperty]
        private ObservableCollection<ProductView> productStockList;*/

    [ObservableProperty]
    private ObservableCollection<string> productCategoryList;

    [ObservableProperty]
    private ObservableCollection<string> metalList;

    [ObservableProperty]
    private ObservableCollection<string> _paymentModeList;

    [ObservableProperty]
    private ObservableCollection<MtblReference> mtblReferencesList;

    [ObservableProperty]
    private ObservableCollection<string> _salesPersonReferencesList;
    // private ObservableCollection<MtblReference> salesPersonReferencesList;

    [ObservableProperty]
    private ObservableCollection<string> _stateReferencesList;
    //    private ObservableCollection<MtblReference> stateReferencesList;

    [ObservableProperty]
    private DateSearchOption _searchOption;

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    private List<MtblReference> _gstTaxRefList;

    private bool createCustomer = false;
    private bool updateCustomer = false;
    private bool invBalanceChk = false;
    private bool InvLineChk = false;
    private bool PayRctChk = false;
    private bool IsBarCodeEnabled = false;

    private string CustName;
    private string CustCity;
    private readonly ReferenceLoader _referenceLoader;

    private readonly ICustomerService _customerService;
    private readonly IProductViewService _productViewService;
    private readonly IAddressService _addressService;
    private readonly IProductStockService _productStockService;
    private readonly IProductStockSummaryService _productStockSummaryService;
    private readonly IProductTransactionService _productTransactionService;
    //private readonly IProductTransactionSummaryService _productTransactionSummaryService;
    private readonly IDialogService _dialogService;
    private readonly IDialogService _reportDialogService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IVoucherService _voucherService;
    private readonly IInvoiceService _invoiceService;
    private readonly ILedgerService _ledgerService;
    private readonly IProductCategoryService _productCategoryService;
    private readonly IInvoiceArReceiptService _invoiceArReceiptService;
    private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;
    private readonly IOldMetalTransactionService _oldMetalTransactionService;
    private readonly IMtblReferencesService _mtblReferencesService;
    private readonly IMtblLedgersService _mtblLedgersService;
    private readonly IReportFactoryService _reportFactoryService;
    private readonly InvoiceEditSession _invoiceEditSession;
    private readonly IServiceProvider _serviceProvider;

    private bool _isLoadingDraft;

    private SettingsPageViewModel _settingsPageViewModel;
    private Dictionary<string, Action<InvoiceLine, decimal?>> copyInvoiceExpression;
    private Dictionary<string, Action<InvoiceHeader, decimal?>> copyHeaderExpression;

    private decimal IGSTPercent = 0M;
    private decimal SCGSTPercent = 3M;
    private decimal todaysRate;

    private List<string> IGNORE_UPDATE = new List<string>
    {
        nameof(InvoiceLine.VaAmount)
    };

    private ProductView OldMetalProductView;

    public InvoiceViewModel(ICustomerService customerService,
        IProductViewService productViewService,
        IAddressService addressService,
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
        InvoiceEditSession invoiceEditSession,
        ReferenceLoader referenceLoader,
        IServiceProvider serviceProvider,
        [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
    {

        _orgThisCompanyViewService = orgThisCompanyViewService;
        _customerService = customerService;
        _addressService = addressService;
        _productViewService = productViewService;
        _productStockService = productStockService;
        _productStockSummaryService = productStockSummaryService;
        _productTransactionService = productTransactionService;
        _productCategoryService = productCategoryService;
        _dialogService = dialogService;
        _invoiceService = invoiceService;
        _ledgerService = ledgerService;
        _messageBoxService = messageBoxService;
        _mtblLedgersService = mtblLedgersService;
        _reportDialogService = reportDialogService;
        _reportFactoryService = reportFactoryService;
        _oldMetalTransactionService = oldMetalTransactionService;
        _voucherService = voucherService;
        _invoiceArReceiptService = invoiceArReceiptService;
        _mtblReferencesService = mtblReferencesService;
        _invoiceEditSession = invoiceEditSession;
        _referenceLoader = referenceLoader;
        _serviceProvider = serviceProvider;

        //_productTransactionSummaryService = productTransactionSummaryService;

        selectedRows = new();
        //productStockList = new();

        _customerReadOnly = false;

        _isBalance = true;
        _isRefund = false;
        _settingsPageViewModel = settingsPageViewModel;

        SetMetalPrice();
        SetHeader();
        SetThisCompany();
        SetMasterLedger();

        _ = LoadReferencesAsync();

        PopulateProductCategoryList();
        //PopulateStateList();
        PopulateUnboundLineDataMap();
        PopulateMtblRefNameList();
        PopulateMetalList();
        PopulateTaxList();


        //PopulateSalesPersonList();

        //PopulateUnboundHeaderDataMap();
    }

    private async Task LoadDraftSafeAsync(
    InvoiceEditResponse draft)
    {
        try
        {
            await Task.Yield();

            await LoadDraftAsync(draft);
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowMessage(
                $"Unable to load draft invoice.\n\n{ex.Message}",
                "Draft Invoice",
                MessageButton.OK,
                MessageIcon.Error);
        }
    }

    public async Task LoadPendingDraftAsync()
    {
        var draft =
            _invoiceEditSession.TakeDraft();

        if (draft is null)
            return;

        await LoadDraftSafeAsync(draft);
    }

    private async Task LoadDraftAsync(
        InvoiceEditResponse draft)
    {
        if (draft?.Header is null)
            return;

        _isLoadingDraft = true;

        Messenger.Default.Send(
            MessageType.WaitIndicator,
            WaitIndicatorVM.ShowIndicator(
                "Loading draft invoice..."));

        try
        {
            Header = MapDraftHeader(draft.Header);

            foreach (var source in draft.Lines)
            {
                Header.Lines.Add(
                    MapDraftLine(source));
            }

            foreach (var source in draft.OldMetalTransactions)
            {
                Header.OldMetalTransactions.Add(
                    MapDraftOldMetal(source));
            }

            /*            foreach (var source in draft.Receipts)
                        {
                            Header.ReceiptLines.Add(
                                MapDraftReceipt(source));
                        }*/

            CustomerPhoneNumber =
                Header.CustMobile;

            if (!string.IsNullOrWhiteSpace(Header.CustMobile))
            {
                Buyer =
                    await _customerService
                        .GetCustomer(Header.CustMobile);
            }

            if (Buyer is not null)
            {
                createCustomer = false;
                updateCustomer = true;

                CustName = Buyer.CustomerName;
                CustCity = Buyer.Address?.City;

                if (Buyer.Address is not null)
                {
                    CustomerState =
                        await _referenceLoader
                            .GetValueAsync(
                                "CUST_STATE",
                                Buyer.Address.GstStateCode);
                }
            }

            InvLineChk =
                Header.Lines.Count > 0;

            PayRctChk = false;
            //   Header.ReceiptLines.Count > 0;

            invBalanceChk = false;
        }
        finally
        {

            _isLoadingDraft = false;

            // The invoice has just been loaded from the database.
            // Therefore the UI exactly represents the persisted Draft.
            HasUnsavedChanges = false;

            SaveDraftInvoiceCommand.NotifyCanExecuteChanged();
            FinaliseInvoiceCommand.NotifyCanExecuteChanged();
            CancelInvoiceCommand.NotifyCanExecuteChanged();

            CreateInvoiceCommand.NotifyCanExecuteChanged();

            PrintInvoiceCommand.NotifyCanExecuteChanged();
            PrintPreviewInvoiceCommand.NotifyCanExecuteChanged();
            ExportToPdfCommand.NotifyCanExecuteChanged();

            Messenger.Default.Send(
                MessageType.WaitIndicator,
                WaitIndicatorVM.HideIndicator());

        }
    }

    [RelayCommand]
    private async Task OpenDraftInvoice()
    {
        try
        {
            // ---------------------------------------------------------
            // Protect current unsaved work
            // ---------------------------------------------------------

            if (HasUnsavedChanges)
            {
                var discardResult =
                    DXMessageBox.Show(
                        "The current invoice contains unsaved changes.\n\n" +
                        "Opening another Draft will discard those changes.\n\n" +
                        "Do you want to continue?",
                        "Unsaved Changes",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                if (discardResult != MessageBoxResult.Yes)
                    return;
            }

            // ---------------------------------------------------------
            // CREATE PICKER
            // ---------------------------------------------------------

            var picker =
                _serviceProvider
                    .GetRequiredService<DraftInvoicePickerView>();

            if (Application.Current?.MainWindow != picker)
            {
                picker.Owner =
                    Application.Current?.MainWindow;
            }

            // ---------------------------------------------------------
            // SHOW MODAL
            // ---------------------------------------------------------

            var dialogResult =
                picker.ShowDialog();

            if (dialogResult != true)
                return;

            // ---------------------------------------------------------
            // GET SELECTED AGGREGATE
            // ---------------------------------------------------------

            if (picker.DataContext is not
                DraftInvoicePickerViewModel vm)
            {
                return;
            }

            if (vm.SelectedInvoice is null)
                return;

            // ---------------------------------------------------------
            // LOAD INTO EXISTING INVOICE SCREEN
            // ---------------------------------------------------------

            await LoadDraftAsync(
                vm.SelectedInvoice);

            // ---------------------------------------------------------
            // CLEAN STATE AFTER LOADING
            // ---------------------------------------------------------

            HasUnsavedChanges = false;

            SaveDraftInvoiceCommand
                .NotifyCanExecuteChanged();

            FinaliseInvoiceCommand
                .NotifyCanExecuteChanged();

            CancelInvoiceCommand
                .NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowMessage(
                $"Unable to open draft invoice.\n\n{ex.Message}",
                "Draft Invoice",
                MessageButton.OK,
                MessageIcon.Error);
        }
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

    private async void SetMasterLedger()
    {
        MtblLedger = await _mtblLedgersService.GetLedger(1000);   //pass account code
    }

    private async void PopulateProductCategoryList()
    {
        var list = await _productCategoryService.GetProductCategoryList();
        ProductCategoryList = new(list
                                .Where(x => !x.Name.StartsWith("OLD"))
                                .Select(x => x.Name));
    }


    private async Task LoadReferencesAsync()
    {

        //CustOrdStatusList = await _referenceLoader.LoadValuesAsync("CUST_ORD_STATUS");

        StateReferencesList = await _referenceLoader.LoadValuesAsync("CUST_STATE");

        PaymentModeList = await _referenceLoader.LoadValuesAsync("PAYMENT_MODE");

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

    private void PopulateUnboundLineDataMap()
    {
        if (copyInvoiceExpression is null) copyInvoiceExpression = new();

        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlTaxableAmount)}", (item, val) => item.InvlTaxableAmount = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.ProdNetWeight)}", (item, val) => item.ProdNetWeight = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlGrossAmt)}", (item, val) => item.InvlGrossAmt = val * (item.Metal.Equals("DIAMOND") ? 100 : 1));
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.VaAmount)}", (item, val) => item.VaAmount = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlCgstAmount)}", (item, val) => item.InvlCgstAmount = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlSgstAmount)}", (item, val) => item.InvlSgstAmount = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlIgstAmount)}", (item, val) => item.InvlIgstAmount = val);
        copyInvoiceExpression.Add($"{nameof(InvoiceLine.InvlTotal)}", (item, val) => item.InvlTotal = val);
    }

    private void PopulateUnboundHeaderDataMap()
    {
        if (copyHeaderExpression is null) copyHeaderExpression = new();

        copyHeaderExpression.Add($"{nameof(InvoiceHeader.RoundOff)}", (item, val) => item.RoundOff = val);
        copyHeaderExpression.Add($"{nameof(InvoiceHeader.GrossRcbAmount)}", (item, val) => item.GrossRcbAmount = val);
        copyHeaderExpression.Add($"{nameof(InvoiceHeader.AmountPayable)}", (item, val) => item.AmountPayable = val);
        copyHeaderExpression.Add($"{nameof(InvoiceHeader.InvBalance)}", (item, val) => item.InvBalance = val);
    }

    private async Task<string> GetStateRefCodeAsync(string state)
    {
        var stateCode = await _referenceLoader.GetCodeAsync("CUST_STATE", state);
        return stateCode;
    }

    partial void OnCustomerStateChanged(string value)            //MtblReference value)
    {
        if (Buyer is null) return;

        //need to review
        Buyer.GstStateCode = GetStateRefCodeAsync(value).GetAwaiter().GetResult();

        Header.CgstPercent = GetGSTPercent("CGST");
        Header.SgstPercent = GetGSTPercent("SGST");
        Header.IgstPercent = GetGSTPercent("IGST");

        //Need to fetch based on pincode - future change
        Header.GstLocBuyer = Buyer.GstStateCode; // value;

        EvaluateForAllLines();
        EvaluateHeader();

        MarkDraftAsModified();

    }

    partial void OnSalesPersonChanged(MtblReference value)
    {
        if (Buyer is null) return;

        if (value.RefValue is not null)
        {
            Header.SalesPerson = value.RefValue;

            MarkDraftAsModified();

        }
    }

    //might be introduced when SKU implmeneted
    private ProductView? ProductStockSelection()
    {

        var vm = DISource.Resolve<InvoiceProductSelectionViewModel>();
        vm.Category = ProductIdUI;

        var result = _dialogService.ShowDialog(MessageButton.OKCancel, "Product",
                                                        "InvoiceProductSelectionView", vm);

        if (result == MessageResult.OK)
        {
            return vm.SelectedProduct;
        }
        return null;
    }

    [RelayCommand]
    private async Task FetchCustomer(EditValueChangedEventArgs args)
    {
        if (args.NewValue is not string phoneNumber) return;

        phoneNumber = phoneNumber.Trim();

        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 10)
            return;

        if (Buyer is not null && Buyer.MobileNbr == phoneNumber)
            return;

        CustomerReadOnly = false;
        createCustomer = false;
        updateCustomer = false;

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

            updateCustomer = true;

            CustName = Buyer.CustomerName;
            CustCity = Buyer.Address.City;

            //CustomerState = StateReferencesList.FirstOrDefault(x => x.RefCode == gstCode);
            CustomerState = await _referenceLoader.GetValueAsync("CUST_STATE", gstCode);

            customerCreditCheck(Buyer);

            Messenger.Default.Send("ProductIdUIName", MessageType.FocusTextEdit);

        }

        Header.CustMobile = phoneNumber;

        MarkDraftAsModified();

    }

    private bool customerCreditCheck(Customer buyer)
    {
        if (buyer.CreditAvailed == "YES")
        {
            showCreditBalanceMsg();
            return true;
        }
        else
            return false;
    }

    private void showCreditBalanceMsg()
    {
        _messageBoxService.ShowMessage($"Customer has Credit Balance, Do you want to check the details......",
                                        "Customer Credit Balance not found", MessageButton.OK, MessageIcon.Error);

    }

    [RelayCommand]
    private async Task FetchProduct()
    {
        var tagError = false;

        if (string.IsNullOrEmpty(ProductIdUI)) return;

        ProductIdUI = ProductIdUI.ToUpper();

        //var waitVM = WaitIndicatorVM.ShowIndicator("Fetching product details...");

        //SplashScreenManager.CreateWaitIndicator(waitVM).Show();

        //var product = await _productViewService.GetProduct(ProductIdUI);

        // await Task.Delay(30000);

        //SplashScreenManager.ActiveSplashScreens.FirstOrDefault(x => x.ViewModel == waitVM).Close();

        //this code will be re-introduce once SKU/Barcode is implmeneted
        //var product = ProductStockSelection();

        //ProductStock productSkuStock = new ProductStock();

        ProductSkuStock = new();
        ProductSkuStock = await _productStockService.GetProductStock(ProductIdUI);
        if (ProductSkuStock is not null)
        {
            IsBarCodeEnabled = true;
            ProductIdUI = ProductSkuStock.Category;
        }
        else
        {
            IsBarCodeEnabled = false;
            tagError = true;
            //return;
        }

        //this should be set as summary stock to avoid confusion
        var productStk = await _productViewService.GetProduct(ProductIdUI);

        if (productStk is null)
        {
            if (tagError)
            {
                _messageBoxService.ShowMessage($"Product Tag not found for {ProductIdUI}, clear and select from list",
                "Product Tag not found", MessageButton.OK, MessageIcon.Error);
            }
            else
            {
                _messageBoxService.ShowMessage($"No Product found for {ProductIdUI}, Please make sure it exists",
                    "Product not found", MessageButton.OK, MessageIcon.Error);
               // return;
            }
            return;
        }


        //might introduce agains when barcode implemented
        //if (productStk is null)
        //{
        //    //No stock to be handled - let the user enter manually all the details of billing item
        //}

        var metalPrice = getBilledPrice(productStk.Metal);
        if (metalPrice < 1)
        {
            displayRateErrorMsg();
            return;
        }

        InvoiceLine invoiceLine = new InvoiceLine()
        {
            ProdQty = 1,
            InvlBilledPrice = metalPrice,
            InvlCgstPercent = Header.CgstPercent,
            InvlSgstPercent = Header.SgstPercent,
            InvlIgstPercent = Header.IgstPercent,
            InvlStoneAmount = 0M,
            TaxType = "GST"
        };

        invoiceLine.SetProductDetails(productStk);

        if (ProductSkuStock is not null)
        {
            invoiceLine.ProductSku = ProductSkuStock.ProductSku;
            invoiceLine.ProdQty = ProductSkuStock.StockQty;
            invoiceLine.ProdGrossWeight = ProductSkuStock.GrossWeight;
            invoiceLine.ProdStoneWeight = ProductSkuStock.StoneWeight;
            invoiceLine.ProdNetWeight = ProductSkuStock.NetWeight;

        }

        EvaluateFormula(invoiceLine, isInit: true);

        Header.Lines.Add(invoiceLine);

        ProductIdUI = string.Empty;

        EvaluateHeader();

        MarkDraftAsModified();

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

    private InvoiceSettlementViewModel ShowSettlementDialog()
    {
        if (Header is null || Header.GKey <= 0)
        {
            _messageBoxService.ShowMessage(
                "Please save the invoice as Draft before finalising.",
                "Invoice Not Saved",
                MessageButton.OK,
                MessageIcon.Warning);

            return null;
        }

        // Make sure the latest invoice calculations are reflected.
        EvaluateHeader();

        var settlementViewModel =
            new InvoiceSettlementViewModel
            {
                InvoiceGkey = Header.GKey,

                // Draft invoices intentionally don't have an official
                // invoice number yet.
                InvoiceNumber =
                    !string.IsNullOrWhiteSpace(Header.InvNbr)
                        ? Header.InvNbr
                        : $"DRAFT-{Header.GKey}",

                CustomerName = CustName,
                CustomerMobile = Header.CustMobile,

                // We'll refine this summary mapping after testing.
                InvoiceAmount =
                    Header.GrossRcbAmount.GetValueOrDefault(),

                OldGoldAdjustment =
                    Header.OldGoldAmount.GetValueOrDefault(),

                OldSilverAdjustment =
                    Header.OldSilverAmount.GetValueOrDefault(),

                AdvanceAdjustment =
                    Header.AdvanceAdj.GetValueOrDefault(),

                RdAdjustment =
                    Header.RdAmountAdj.GetValueOrDefault(),

                // IMPORTANT:
                // This is the existing Invoice calculation's signed
                // settlement position.
                NetSettlementAmount =
                    Header.AmountPayable.GetValueOrDefault()
            };

        if (PaymentModeList is not null)
        {
            foreach (var mode in PaymentModeList)
            {
                if (!string.IsNullOrWhiteSpace(mode))
                {
                    AddPaymentModeIfMissing(
                        settlementViewModel.PaymentModes,
                        mode);
                }
            }
        }

        // Temporary release implementation.
        //
        // Advance Adj already exists in PAYMENT_MODE.
        // RD Adj is added here as a fallback until it is
        // added permanently to the reference table.
        AddPaymentModeIfMissing(
            settlementViewModel.PaymentModes,
            "RD Adj");

        var settlementView =
            new InvoiceSettlementView(
                settlementViewModel)
            {
                Owner = Application.Current.MainWindow
            };

        var confirmed =
            settlementView.ShowDialog() == true;

        return confirmed
            ? settlementViewModel
            : null;

    }

    private static void AddPaymentModeIfMissing(
    ICollection<string> modes,
    string mode)
    {
        if (!modes.Any(
                x => string.Equals(
                    x,
                    mode,
                    StringComparison.OrdinalIgnoreCase)))
        {
            modes.Add(mode);
        }
    }

    private void displayRateErrorMsg()
    {
        _messageBoxService.ShowMessage($"Todays Rate not entered in system, set the rate and start invoicing....",
                                        "Todays Rate not found", MessageButton.OK, MessageIcon.Error);

    }


    private async Task EvaluateOldMetalTransactionLineAsync(OldMetalTransaction oldMetalTransaction)
    {

        if (string.IsNullOrEmpty(oldMetalTransaction.ProductId)) return;

        OldMetalProductView = await _productViewService.GetProduct(oldMetalTransaction.ProductId);

        if (OldMetalProductView is null)
        {
            _messageBoxService.ShowMessage(
                $"No Product found for {oldMetalTransaction.ProductId}. Please make sure it exists.",
                "Product not found",
                MessageButton.OK,
                MessageIcon.Error);

            return;
        }

        var metalPrice = _settingsPageViewModel.GetPrice(OldMetalProductView.Metal);

        if (metalPrice < 1)
        {
            displayRateErrorMsg();
            //return;
        }


        if (oldMetalTransaction.TransactedRate.GetValueOrDefault() < 1)
            oldMetalTransaction.TransactedRate = metalPrice; // todaysRate;

        oldMetalTransaction.Purity = OldMetalProductView.Purity;

        oldMetalTransaction.NetWeight = (
                                           oldMetalTransaction.GrossWeight.GetValueOrDefault() -
                                           oldMetalTransaction.StoneWeight.GetValueOrDefault() -
                                           oldMetalTransaction.WastageWeight.GetValueOrDefault()
                                        );

        oldMetalTransaction.TotalProposedPrice = oldMetalTransaction.NetWeight.GetValueOrDefault() *
                                                    oldMetalTransaction.TransactedRate.GetValueOrDefault();
        oldMetalTransaction.FinalPurchasePrice = oldMetalTransaction.TotalProposedPrice;

        oldMetalTransaction.DocRefType = "Invoice";

        oldMetalTransaction.EnrichOldMetalProductDetails(OldMetalProductView);

    }

    [RelayCommand]
    private Task EvaluateOldMetalTransaction()
    {

        //   var billedPrice = _settingsPageViewModel.GetPrice(product.Metal);

        OldMetalTransaction oldMetalTransactionLine = new OldMetalTransaction()
        {
            CustGkey = Header.CustGkey,
            CustMobile = Header.CustMobile,
            TransType = "OM Purchase",
            TransDate = DateTime.Now,
            //   Uom = "Grams"
        };

        Header.OldMetalTransactions.Add(oldMetalTransactionLine);

        MarkDraftAsModified();

        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanProcessArReceipts))]
    private async Task ProcessArReceipts()
    {
        //  var paymentMode = await _mtblReferencesService.GetReference("PAYMENT_MODE");
        // _productService.GetProduct(ProductIdUI);

        // var waitVM = WaitIndicatorVM.ShowIndicator("Fetching Invoice Receipt details...");

        var noOfLines = Header.ReceiptLines.Count;

        InvoiceArReceipt arInvRctLine = new InvoiceArReceipt()
        {
            CustGkey = Header.CustGkey,
            Status = "Open",    //Status Open - Before Adjustment
            SeqNbr = noOfLines + 1
        };

        Header.ReceiptLines.Add(arInvRctLine);
    }

    private bool CanProcessArReceipts()
    {
        //return !Header.DiscountAmount.HasValue || Header.DiscountAmount.Value.GetDecimalValue() == 0;
        return (Header.DiscountAmount ?? 0) == 0;

    }

    [RelayCommand]
    private void AddOldJewel(string type)
    {
        var enumVal = Enum.Parse<MetalType>(type);

        var vm = new DialogOldJewelVM();

        if (_dialogService.ShowDialog(MessageButton.OKCancel, "Exchange", "DialogOldJewel", vm) == MessageResult.OK)
        {
            if (enumVal == MetalType.Gold)
            {
                Header.OldGoldAmount = vm.Rate * vm.Weight;
            }
            else
            {
                Header.OldSilverAmount = vm.Rate * vm.Weight;
            }
        }
    }

    private bool CanFinaliseInvoice()
    {
        if (Header is null)
            return false;

        return Header.GKey > 0 &&
               InvoiceStatus.IsDraft(Header.Status) &&
               !HasUnsavedChanges;
    }

    [RelayCommand(CanExecute = nameof(CanFinaliseInvoice))]
    private async Task FinaliseInvoice()
    {
        if (Header is null || Header.GKey <= 0)
        {
            _messageBoxService.ShowMessage(
                "Please save the invoice as Draft before finalising.",
                "Invoice Not Saved",
                MessageButton.OK,
                MessageIcon.Warning);

            return;
        }

        if (!InvoiceStatus.IsDraft(Header.Status))
        {
            _messageBoxService.ShowMessage(
                "Only Draft invoices can be finalised.",
                "Invalid Invoice Status",
                MessageButton.OK,
                MessageIcon.Warning);

            return;
        }

        // Recalculate before opening settlement.
        EvaluateHeader();

        var settlement = ShowSettlementDialog();

        if (settlement is null)
            return;

        var request =
            InvoiceRequestMapper.ToFinaliseRequest(
                Header.GKey,
                settlement);

        try
        {
            var result =
                await _invoiceService.FinaliseAsync(request);

            Header.InvNbr = result.InvNbr;
            Header.Status = result.Status;

            FinaliseInvoiceCommand.NotifyCanExecuteChanged();
            SaveDraftInvoiceCommand.NotifyCanExecuteChanged();
            CancelInvoiceCommand.NotifyCanExecuteChanged();
            CreateInvoiceCommand.NotifyCanExecuteChanged();
            PrintInvoiceCommand.NotifyCanExecuteChanged();
            PrintPreviewInvoiceCommand.NotifyCanExecuteChanged();

            _messageBoxService.ShowMessage(
                $"Invoice {result.InvNbr} has been finalised successfully.",
                "Invoice Finalised",
                MessageButton.OK,
                MessageIcon.Information);

            _reportDialogService.PrintPreview(result.InvNbr);

            // Start next invoice after preview is closed.
            ResetInvoice();

        }
        catch (Exception ex)
        {
            _messageBoxService.ShowMessage(
                $"Invoice could not be finalised.\n\n{ex.Message}",
                "Finalisation Failed",
                MessageButton.OK,
                MessageIcon.Error);
        }
    }

    private void MarkDraftAsModified()
    {
        if (_isLoadingDraft)
            return;

        if (Header is null ||
            Header.GKey <= 0 ||
            !InvoiceStatus.IsDraft(Header.Status))
        {
            return;
        }

        if (HasUnsavedChanges)
            return;

        HasUnsavedChanges = true;

        FinaliseInvoiceCommand.NotifyCanExecuteChanged();
        CancelInvoiceCommand.NotifyCanExecuteChanged();
    }


    private void MarkDraftAsSaved()
    {
        HasUnsavedChanges = false;

        SaveDraftInvoiceCommand.NotifyCanExecuteChanged();
        FinaliseInvoiceCommand.NotifyCanExecuteChanged();
        CancelInvoiceCommand.NotifyCanExecuteChanged();
    }

    private bool CanSaveDraftInvoice()
    {
        if (Header is null)
            return false;

        return !InvoiceStatus.IsFinal(Header.Status) &&
               !InvoiceStatus.IsCancelled(Header.Status);
    }

    [RelayCommand(CanExecute = nameof(CanSaveDraftInvoice))]
    private async Task SaveDraftInvoice()
    {
        try
        {
            // =========================================================
            // VALIDATION
            // =========================================================

            if (Header is null)
                return;

            if (Header.Lines is null ||
                Header.Lines.Count == 0)
            {
                DXMessageBox.Show(
                    "Please enter at least one invoice item before saving the draft.",
                    "Draft Invoice",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (Buyer is null ||
                string.IsNullOrWhiteSpace(Buyer.CustomerName))
            {
                _messageBoxService.ShowMessage(
                    "Please enter customer information before saving the draft.",
                    "Customer Information",
                    MessageButton.OK,
                    MessageIcon.Warning);

                return;
            }


            // =========================================================
            // PROCESSING INDICATOR
            //
            // Start this AFTER validation so we don't briefly display
            // the wait indicator when validation fails.
            // =========================================================

            await ShowProcessingAsync(
                "Saving draft invoice. Please wait...");

            // =========================================================
            // CUSTOMER
            // =========================================================

            if (createCustomer)
            {
                Buyer =
                    await _customerService
                        .CreateCustomer(Buyer);

                createCustomer = false;
                updateCustomer = true;
            }
            else if (updateCustomer)
            {
                if (CustName != Buyer.CustomerName)
                {
                    await _customerService
                        .UpdateCustomer(Buyer);
                }

                if (Buyer.Address is not null &&
                    CustCity != Buyer.Address.City)
                {
                    await _addressService
                        .UpdateAddress(Buyer.Address);
                }
            }


            // =========================================================
            // UPDATE CUSTOMER REFERENCES ON INVOICE
            // =========================================================

            Header.CustGkey =
                (int?)Buyer.GKey;

            Header.CustMobile =
                Buyer.MobileNbr;


            // =========================================================
            // DRAFT STATE
            //
            // Backend will also enforce DRAFT, but keep the client model
            // consistent before mapping.
            // =========================================================

            Header.Status =
                InvoiceStatus.Draft;

            if (Header.GKey <= 0)
            {
                // A new Draft must NOT consume an official invoice number.
                Header.InvNbr = null;
            }


            // =========================================================
            // LINE NUMBERS
            // =========================================================

            for (var index = 0;
                 index < Header.Lines.Count;
                 index++)
            {
                Header.Lines[index].InvLineNbr =
                    index + 1;
            }

            var isNewDraft = Header.GKey <= 0;

            // =========================================================
            // MAP COMPLETE INVOICE AGGREGATE
            // =========================================================

            var request =
                InvoiceRequestMapper
                    .ToDraftSaveRequest(Header);


            // =========================================================
            // SAVE
            //
            // IMPORTANT:
            // Exactly ONE backend workflow call.
            // =========================================================

            var result =
                await _invoiceService
                    .SaveDraftAsync(request);


            // =========================================================
            // UPDATE CURRENT UI MODEL
            // =========================================================

            Header.GKey =
                result.Gkey;

            Header.InvNbr =
                string.IsNullOrWhiteSpace(result.InvNbr)
                    ? null
                    : result.InvNbr;

            Header.Status =
                result.Status;


            // =========================================================
            // CUSTOMER STATE
            // =========================================================

            createCustomer = false;
            updateCustomer = true;

            CustName =
                Buyer.CustomerName;

            CustCity =
                Buyer.Address?.City;


            // =========================================================
            // REFRESH COMMAND STATE
            // =========================================================

            MarkDraftAsSaved();

            // =========================================================
            // SUCCESS
            // =========================================================

            /*            DXMessageBox.Show(
                            $"Draft invoice saved successfully.\n\n" +
                            $"Draft Number: DRAFT-{result.Gkey}",
                            "Draft Saved",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);*/

            // Start a clean invoice only after the Draft
            // has been successfully persisted.
            if (isNewDraft)
            {
                var finaliseNow =
                    DXMessageBox.Show(
                        $"Draft invoice saved successfully.\n\n" +
                        $"Draft Number: DRAFT-{result.Gkey}\n\n" +
                        "Do you want to settle and finalise this invoice now?",
                        "Draft Saved",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                if (finaliseNow == MessageBoxResult.Yes)
                {
                    await FinaliseInvoice();
                }
                else
                {
                    ResetInvoice();
                }
            }
            else
            {
                DXMessageBox.Show(
                    $"Draft invoice DRAFT-{result.Gkey} updated successfully.",
                    "Draft Saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

        }
        catch (HttpRequestException ex)
        {
            DXMessageBox.Show(
                $"Unable to save draft invoice.\n\n" +
                $"{ex.Message}",
                "Save Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            DXMessageBox.Show(
                $"Draft invoice could not be saved.\n\n" +
                $"{ex.Message}",
                "Save Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            // Always close the processing indicator:
            // success, validation return after ShowProcessing,
            // API failure, DB failure, etc.

            HideProcessing();
        }
    }

    private bool CanCreateInvoice()
    {
        return string.IsNullOrEmpty(Header?.InvNbr);

    }

    [RelayCommand(CanExecute = nameof(CanCreateInvoice))]
    private async Task CreateInvoice()
    {
        LedgerHelper ledgerHelper = new(_ledgerService, _messageBoxService, _mtblLedgersService);   //is this a right way????? 

        //validate to fit to save invoice
        if (!InvLineChk)
        {
            _messageBoxService.ShowMessage("Please enter Invoice details and then Save, ", "Missing Invoice Details", MessageButton.OK, MessageIcon.Error);

            return;
        }

        //validation - give warning to user, if payment details are not entered
        if (!PayRctChk)
        {
            _messageBoxService.ShowMessage("No Customer Payment details entered....", "Missing Customer Payment Details", MessageButton.OK, MessageIcon.Error);

            // return;
        }

        invBalanceChk = true;  //is this a right place to fix
        var isSuccess = ProcessInvBalance();

        if (!isSuccess) return;

        if (!string.IsNullOrEmpty(Header.InvNbr))
        {
            var result = _messageBoxService.ShowMessage("Invoice already exists, Do you want to print preview the invoice ?", "Invoice",
                                                            MessageButton.OKCancel,
                                                            MessageIcon.Question,
                                                            MessageResult.Cancel);

            if (result == MessageResult.OK)
            {
                PrintPreviewInvoice();
            }
            return;
        }

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
        else if (updateCustomer)
        {
            if (CustName != Buyer.CustomerName)
            {
                await _customerService.UpdateCustomer(Buyer);
            }

            if (CustCity != Buyer.Address.City)
            {
                await _addressService.UpdateAddress(Buyer.Address);
            }
        }

        //Header.InvNbr = InvoiceNumberGenerator.Generate();
        Header.CustGkey = (int?)Buyer.GKey;

        // this loop required? - repetition???
        Header.Lines.ForEach(x =>
        {
            x.InvLineNbr = Header.Lines.IndexOf(x) + 1;
            x.InvoiceId = Header.InvNbr;
        });

        var header = await _invoiceService.CreateHeader(Header);

        if (header is not null)
        {
            Header.GKey = header.GKey;
            Header.InvNbr = header.InvNbr;

            Header.Lines.ForEach(x =>
            {
                x.InvoiceHdrGkey = header.GKey;
                x.InvoiceId = header.InvNbr;
                x.TenantGkey = header.TenantGkey;
            });

            // loop for validation check for customer
            await _invoiceService.CreateInvoiceLine(Header.Lines);

            await ProcessProductTransaction(Header.Lines);

            await ProcessOldMetalTransaction();

            //Invoice header details needs to be saved alongwith receipts, hence calling from here.
            await ProcessReceipts();

        //    if ((Header.AdvanceAdj > 0) || (Header.RdAmountAdj > 0))    //blocked this on 18-Mar - need to workout in detail
        //        await ledgerHelper.ProcessInvoiceAdvanceAsync(Header);  // this code causing crashes of application

            _messageBoxService.ShowMessage("Invoice " + Header.InvNbr + " Created Successfully", "Invoice Created",
                                                MessageButton.OK, MessageIcon.Exclamation);

            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Print Invoice..."));

            var waitVM = WaitIndicatorVM.ShowIndicator("Please wait.... preparing print document.... .");

            SplashScreenManager.CreateWaitIndicator(waitVM).Show();

            PrintPreviewInvoice();

            SplashScreenManager.ActiveSplashScreens.FirstOrDefault(x => x.ViewModel == waitVM).Close();

            PrintPreviewInvoiceCommand.NotifyCanExecuteChanged();
            PrintInvoiceCommand.NotifyCanExecuteChanged();
            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

        }
    }

    private async Task ProcessProductTransaction(IEnumerable<InvoiceLine> invLines)
    {
        foreach (var line in invLines)
        {

            await ProductStockSummaryUpdate(line);

            if (IsBarCodeEnabled)
                await ProductStockUpdate(line);  

            //await CreateProductTransaction(line);
        }
    }

    private async Task ProductStockUpdate(InvoiceLine line)
    {

        var productStk = await _productStockService.GetProductStock(line.ProductSku);

        if (productStk is null)
        {
            IsBarCodeEnabled = false;
            return;
        }

        productStk.SoldWeight = line.ProdGrossWeight;
        productStk.BalanceWeight = 0;
        productStk.SoldQty = line.ProdQty;
        productStk.StockQty = 0;
        productStk.Status = "Sold";
        productStk.IsProductSold = true;

        await _productStockService.UpdateProductStock(productStk);

    }

    //Consolidated stock line in product stock summary - stock qty / weight has to be reduced based on invoiced qty
    //this logic would be revisited/changed once product sku - barcode feature introduced
    //WARN :- Ensure all product category must have one record in table
    private async Task ProductStockSummaryUpdate(InvoiceLine line)
    {
        ProductTransaction productTransaction = new();

        var productSumryStk = await _productStockSummaryService.GetByProductGkey(line.ProductGkey);

        //if (productSumryStk is null)
        //{
        //    await _productStockSummaryService.CreateProductStockSummary(productStockSummary);
        //}
        //else
        //{
        //    await _productStockSummaryService.UpdateProductStockSummary(productStockSummary);
        //}

        if (productSumryStk is not null)
        {
            //Set Product Transaction
            productTransaction.OpeningGrossWeight = productSumryStk.GrossWeight.GetValueOrDefault();
            productTransaction.OpeningStoneWeight = productSumryStk.StoneWeight.GetValueOrDefault();
            productTransaction.OpeningNetWeight = productSumryStk.NetWeight.GetValueOrDefault();

            productTransaction.ObQty = productSumryStk.StockQty.GetValueOrDefault();

            productTransaction.ProductSku = line.ProductSku;
            productTransaction.RefGkey = line.GKey;
            productTransaction.TransactionDate = DateTime.Now;
            productTransaction.ProductCategory = line.ProdCategory;

            productTransaction.TransactionType = "Issue";
            productTransaction.DocumentNbr = line.InvoiceId;
            productTransaction.DocumentDate = DateTime.Now;
            productTransaction.DocumentType = "Sales Invoice";
            productTransaction.VoucherType = "Sales";
            productTransaction.TransactionQty = line.ProdQty.GetValueOrDefault();
            productTransaction.CbQty = productSumryStk.StockQty.GetValueOrDefault() - line.ProdQty.GetValueOrDefault();

            productTransaction.TransactionGrossWeight = line.ProdGrossWeight.GetValueOrDefault();
            productTransaction.TransactionStoneWeight = line.ProdStoneWeight.GetValueOrDefault();
            productTransaction.TransactionNetWeight = line.ProdNetWeight.GetValueOrDefault();

            productTransaction.ClosingGrossWeight = productSumryStk.GrossWeight.GetValueOrDefault()
                                                            - line.ProdGrossWeight.GetValueOrDefault();
            productTransaction.ClosingStoneWeight = productSumryStk.StoneWeight.GetValueOrDefault()
                                                            - line.ProdStoneWeight.GetValueOrDefault();
            productTransaction.ClosingNetWeight = productSumryStk.NetWeight.GetValueOrDefault()
                                                            - line.ProdNetWeight.GetValueOrDefault();

            //Set Product Stock Summary
            productSumryStk.GrossWeight = (productSumryStk.GrossWeight ?? 0) - line.ProdGrossWeight;
            productSumryStk.StoneWeight = (productSumryStk.StoneWeight ?? 0) - line.ProdStoneWeight;
            productSumryStk.NetWeight = (productSumryStk.NetWeight ?? 0) - line.ProdNetWeight;
            productSumryStk.SuppliedGrossWeight = 0; //need to work ntw-- (productSumryStk.SuppliedGrossWeight ?? 0) - line.ProdGrossWeight;
            //productSumryStk.AdjustedWeight = (productSumryStk.AdjustedWeight ?? 0);
            productSumryStk.SoldWeight = 0; //ntw (productSumryStk.SoldWeight ?? 0) + line.ProdNetWeight;
            productSumryStk.BalanceWeight = (productSumryStk.BalanceWeight ?? 0) - line.ProdNetWeight;
            //productSumryStk.SuppliedQty = (productSumryStk.SuppliedQty ?? 0) + x.SuppliedQty;
            productSumryStk.SoldQty = 0; //ntw (productSumryStk.SoldQty ?? 0) + line.ProdQty;
            productSumryStk.StockQty = (productSumryStk.StockQty ?? 0) - line.ProdQty;
            //productSumryStk.AdjustedQty = (productSumryStk.AdjustedQty ?? 0);

            await _productStockSummaryService.UpdateProductStockSummary(productSumryStk);

            productTransaction = await _productTransactionService.CreateProductTransaction(productTransaction);

            //await CreateProductTransaction(line, productSumryStk);
        }
    }

    /* private async void createProductTransactionSummary(ProductTransaction productTransaction)
     {

         ProductTransactionSummary productTransSumry = new();

         //Fetch last records of the day to set ob, cb etc
         SearchOption = new();
         SearchOption.To = DateTime.Today;
         SearchOption.From = DateTime.Today;
         SearchOption.Filter1 ??= productTransaction.ProductCategory;

         var prodTransSumry = await _productTransactionSummaryService.GetAll(SearchOption);

         productTransSumry = prodTransSumry.FirstOrDefault();

         if (productTransSumry != null)
         {
             // then add up with the existing total

             productTransSumry.StockOutQty = productTransSumry.StockOutQty.GetValueOrDefault() 
                                                 + productTransaction.TransactionQty.GetValueOrDefault();
             productTransSumry.ClosingQty = productTransSumry.ClosingQty.GetValueOrDefault() 
                                                 - productTransaction.TransactionQty.GetValueOrDefault();

             productTransSumry.StockOutGrossWeight = productTransSumry.StockOutGrossWeight.GetValueOrDefault() 
                                                                     + productTransaction.TransactionGrossWeight.GetValueOrDefault();
             productTransSumry.StockOutStoneWeight = productTransSumry.StockOutStoneWeight.GetValueOrDefault()
                                                                     + productTransaction.TransactionStoneWeight.GetValueOrDefault();
             productTransSumry.StockOutNetWeight = productTransSumry.StockOutNetWeight.GetValueOrDefault() 
                                                                     + productTransaction.TransactionNetWeight.GetValueOrDefault();

             productTransSumry.ClosingGrossWeight = productTransSumry.ClosingGrossWeight.GetValueOrDefault()
                                                                 - productTransaction.TransactionGrossWeight.GetValueOrDefault();
             productTransSumry.ClosingStoneWeight = productTransSumry.ClosingStoneWeight.GetValueOrDefault()
                                                                 - productTransaction.TransactionStoneWeight.GetValueOrDefault();
             productTransSumry.ClosingNetWeight = productTransSumry.ClosingNetWeight.GetValueOrDefault()
                                                                 - productTransaction.TransactionNetWeight.GetValueOrDefault();

             await _productTransactionSummaryService.UpdateProductTransactionSummary(productTransSumry);
         }
         else
         {
             //create new record for the day if not found for todays 
             //get the last transaction of specific category to get opening balance
             ProductTransactionSummary prodTransSumryPrevious = new();

             productTransSumry = new();

             var prodTransSumryPrev = await _productTransactionSummaryService
                                                         .GetLastProductTranSumryByCategory(productTransaction.ProductCategory);
             if (prodTransSumryPrev != null) 
                 prodTransSumryPrevious = prodTransSumryPrev;

             productTransSumry.TransactionDate = DateTime.Now;
             productTransSumry.ProductCategory = productTransaction.ProductCategory;
             productTransSumry.ProductSku = productTransaction.ProductSku;


             productTransSumry.StockInGrossWeight = 0;   //only stock entry
             productTransSumry.StockInStoneWeight = 0;
             productTransSumry.StockInNetWeight = 0;

             productTransSumry.StockOutGrossWeight = productTransaction.TransactionGrossWeight;
             productTransSumry.StockOutStoneWeight = productTransaction.TransactionStoneWeight;
             productTransSumry.StockOutNetWeight = productTransaction.TransactionNetWeight;

             //Opening
             productTransSumry.OpeningQty    = (prodTransSumryPrevious.ClosingQty ?? 0);
             productTransSumry.StockInQty    = 0;
             productTransSumry.StockOutQty   = productTransaction.TransactionQty.GetValueOrDefault();
             productTransSumry.ClosingQty    = productTransSumry.OpeningQty.GetValueOrDefault() 
                                                         - productTransaction.TransactionQty.GetValueOrDefault();

             productTransSumry.OpeningGrossWeight = (prodTransSumryPrevious.ClosingGrossWeight ?? 0);
             productTransSumry.OpeningStoneWeight = (prodTransSumryPrevious.ClosingStoneWeight ?? 0);
             productTransSumry.OpeningNetWeight = (prodTransSumryPrevious.ClosingNetWeight ?? 0);

             productTransSumry.ClosingGrossWeight = productTransSumry.OpeningGrossWeight.GetValueOrDefault()
                                                                     - productTransaction.TransactionGrossWeight;
             productTransSumry.ClosingStoneWeight = productTransSumry.OpeningStoneWeight.GetValueOrDefault()
                                                                     - productTransaction.TransactionStoneWeight;
             productTransSumry.ClosingNetWeight = productTransSumry.OpeningNetWeight.GetValueOrDefault()
                                                                     - productTransaction.TransactionNetWeight;

             await _productTransactionSummaryService.CreateProductTransactionSummary(productTransSumry);
         }

     }*/


    [RelayCommand(CanExecute = nameof(CanPrintInvoice))]
    private void PrintInvoice()
    {
        var printed = PrintHelper.Print(_reportFactoryService.CreateInvoiceReport(Header.InvNbr));

        if (printed.HasValue && printed.Value)
            _messageBoxService.ShowMessage("Invoice printed Successfully", "Invoice print",
                                                MessageButton.OK, MessageIcon.None);
    }

    private bool CanPrintInvoice()
    {
        if (Header is null)
            return false;

        return InvoiceStatus.IsFinal(Header.Status) &&
               !string.IsNullOrWhiteSpace(Header.InvNbr);
    }

    [RelayCommand(CanExecute = nameof(CanPrintInvoice))]
    private void PrintPreviewInvoice()
    {
        _reportDialogService.PrintPreview(Header.InvNbr);
        ResetInvoice();
    }

    [RelayCommand(CanExecute = nameof(CanPrintInvoice))]
    private void ExportToPdf()
    {
        _reportFactoryService.CreateInvoiceReportPdf(Header.InvNbr, "C:\\Madrone\\Invoice\\");
    }

    [RelayCommand]
    private async Task CellUpdate(
        CellValueChangedEventArgs args)
    {
        if (_isLoadingDraft)
            return;

        if (args?.Row is null)
            return;

        if (args.Row is InvoiceLine line)
        {
            EvaluateFormula(line);
        }
        else if (args.Row is InvoiceArReceipt arInvRctLine)
        {
            // Temporary.
            // Receipt handling will be removed from Draft
            // in the next step.
            EvaluateArRctLine(arInvRctLine);
        }
        else if (
            args.Row is OldMetalTransaction oldMetalTransaction &&
            args.Column?.FieldName !=
                nameof(OldMetalTransaction.FinalPurchasePrice))
        {
            await EvaluateOldMetalTransactionLineAsync(
                oldMetalTransaction);
        }

        EvaluateHeader();

        MarkDraftAsModified();

    }


    [RelayCommand]
    private void EvaluateOldMetalTransactions(OldMetalTransaction oldMetalTransaction)
    {

        /*        if (oldMetalTransaction.ProductId is null)
                {
                    return;
                }*/

        oldMetalTransaction.TransactedRate = todaysRate;

        oldMetalTransaction.NetWeight = (
                                           oldMetalTransaction.GrossWeight.GetValueOrDefault() -
                                           oldMetalTransaction.StoneWeight.GetValueOrDefault() -
                                           oldMetalTransaction.WastageWeight.GetValueOrDefault()
                                        );

        oldMetalTransaction.TotalProposedPrice = oldMetalTransaction.NetWeight.GetValueOrDefault() *
                                                    oldMetalTransaction.TransactedRate.GetValueOrDefault();
        oldMetalTransaction.FinalPurchasePrice = oldMetalTransaction.TotalProposedPrice;

        oldMetalTransaction.DocRefType = "Invoice";

        oldMetalTransaction.EnrichOldMetalProductDetails(OldMetalProductView);
    }

    private async Task ShowProcessingAsync(string message)
    {
        Messenger.Default.Send(
            MessageType.WaitIndicator,
            WaitIndicatorVM.ShowIndicator(message));

        // Allow WPF to perform a render pass before
        // starting the operation.
        await Application.Current.Dispatcher.InvokeAsync(
            () => { },
            DispatcherPriority.Render);
    }

    private void HideProcessing()
    {
        Messenger.Default.Send(
            MessageType.WaitIndicator,
            WaitIndicatorVM.HideIndicator());
    }


    [RelayCommand]
    private void EvaluateArRctLine(InvoiceArReceipt arInvRctLine)
    {

        if (string.IsNullOrEmpty(arInvRctLine.TransactionType))
        {
            return;
        }

        if (!arInvRctLine.BalBeforeAdj.HasValue)
            arInvRctLine.BalBeforeAdj = Header.InvBalance.GetValueOrDefault();

        arInvRctLine.BalanceAfterAdj = arInvRctLine.BalBeforeAdj.GetValueOrDefault() -
                                        arInvRctLine.AdjustedAmount.GetValueOrDefault();

        if (arInvRctLine.TransactionType == "Cash" || arInvRctLine.TransactionType == "Refund"
                            || arInvRctLine.TransactionType == "Advance Receipt")
        {
            arInvRctLine.ModeOfReceipt = "Cash";
        }
        else if (arInvRctLine.TransactionType == "Credit")
        {
            arInvRctLine.ModeOfReceipt = "Credit";
        }
        else 
        {
            arInvRctLine.ModeOfReceipt = "Bank";
        }

        if (arInvRctLine.ModeOfReceipt is not null)
        {
            PayRctChk = true;
        }

/*        if (arInvRctLine.AdjustedAmount > 0 && arInvRctLine.ExternalTransactionId is null)
        {
            if (arInvRctLine.ModeOfReceipt == "Bank" || arInvRctLine.ModeOfReceipt == "GPAY")
            {
                _messageBoxService.ShowMessage("Please enter transaction reference ", "Transaction Ref",
                                                    MessageButton.OK, MessageIcon.None);
                //arInvRctLine = ShowTransactionDetailsPopup(arInvRctLine);

            }
        }*/

        EvaluateHeader();

    }

    private InvoiceArReceipt? ShowTransactionDetailsPopup(InvoiceArReceipt arInvRctLine)
    {
        var vm = new ReceiptAccountingViewModel(); // pass model directly

        var result = _dialogService.ShowDialog(MessageButton.OKCancel,
                                               "Product",
                                               "ReceiptAccountingView",
                                               vm);

        if (result == MessageResult.OK)
        {
            arInvRctLine.BankName = vm.BankName;
            arInvRctLine.ExternalTransactionId = vm.ExtTransactionRefId;
            arInvRctLine.ExternalTransactionDate = DateTime.Now;
            arInvRctLine.OtherReference = vm.ChequeNumber;
            

            return arInvRctLine;
        }

        return null;

    }

/*    private InvoiceArReceipt? ShowTransactionDetailsPopup(InvoiceArReceipt arInvRctLine) 
    {

        var vm = DISource.Resolve<ReceiptAccountingViewModel>(arInvRctLine);
       // vm.Category = ProductIdUI;

        var result = _dialogService.ShowDialog(MessageButton.OKCancel, "Product",
                                                        "ReceiptAccountingView", vm);

        if (result == MessageResult.OK)
        {
            return arInvRctLine; //  vm.SelectedProduct;
        }
        return null;
    }*/

    private decimal FilterReceiptTransactions(string transType)
    {
        return (decimal)Header.ReceiptLines
                        .Where(x => transType.Equals(x.TransactionType, StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.AdjustedAmount)
                        .Sum();
    }

    private decimal? FilterMetalTransactions(string productId)
    {
        return Header.OldMetalTransactions
                        .Where(x => productId.Equals(x.ProductId, StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.FinalPurchasePrice)
                        .Sum();
    }

    [RelayCommand]
    private void EvaluateHeader()
    {

        if (_isLoadingDraft)
            return;

        if (Header is null)
            return;

        // Header.AdvanceAdj = FilterReceiptTransactions("Advance");
        // Header.RdAmountAdj = FilterReceiptTransactions("RD");

        // =========================================================
        // PAYMENT / RECEIPT VALUES
        //
        // A Draft invoice contains commercial values only.
        // Payment settlement happens only during Finalisation.
        // =========================================================

        if (InvoiceStatus.IsDraft(Header.Status))
        {
            Header.RecdAmount = 0M;
        }
        else
        {
            Header.RecdAmount =
                Header.ReceiptLines
                    .Select(x => x.AdjustedAmount)
                    .Sum();
        }

        Header.OldGoldAmount = FilterMetalTransactions("OLD GOLD 18KT") + FilterMetalTransactions("OLD GOLD 22KT") + FilterMetalTransactions("OLD GOLD 916-22KT");

        Header.OldSilverAmount = FilterMetalTransactions("OLD SILVER");

        // TaxableTotal from line without tax value
        Header.InvlTaxTotal = Header.Lines.Select(x => x.InvlTotal).Sum();

        // Line Taxable Total minus Old Gold & Silver Amount
        decimal BeforeTax = 0;
        BeforeTax = Header.InvlTaxTotal.GetValueOrDefault() -
                    Header.OldGoldAmount.GetValueOrDefault() -
                    Header.OldSilverAmount.GetValueOrDefault();

        if (BeforeTax >= 0)
        {
            Header.CgstAmount = MathUtils.Normalize(BeforeTax * Math.Round(Header.CgstPercent.GetValueOrDefault() / 100, 3));
            Header.SgstAmount = MathUtils.Normalize(BeforeTax * Math.Round(Header.SgstPercent.GetValueOrDefault() / 100, 3));
            Header.IgstAmount = MathUtils.Normalize(BeforeTax * Math.Round(Header.IgstPercent.GetValueOrDefault() / 100, 3));

        }
        else
        {
            Header.CgstAmount = 0;
            Header.SgstAmount = 0;
            Header.IgstAmount = 0;
        }

        Header.InvlTaxableAmount = BeforeTax;

        // After Tax Gross Value
        Header.GrossRcbAmount = BeforeTax +
                                Header.CgstAmount.GetValueOrDefault() +
                                Header.SgstAmount.GetValueOrDefault() +
                                Header.IgstAmount.GetValueOrDefault();

        decimal roundOff = 0;
        roundOff = Math.Round(Header.GrossRcbAmount.GetValueOrDefault(), 0) -
                        Header.GrossRcbAmount.GetValueOrDefault();

        Header.RoundOff = roundOff;

        Header.GrossRcbAmount = MathUtils.Normalize(Header.GrossRcbAmount.GetValueOrDefault(), 0);

        decimal payableValue = 0;
        payableValue = Header.GrossRcbAmount.GetValueOrDefault() -
                        Header.DiscountAmount.GetValueOrDefault();

        Header.AmountPayable = MathUtils.Normalize(payableValue);


        // =========================================================
        // DRAFT BALANCE
        //
        // During Draft:
        //   Amount Payable = commercial invoice amount
        //   Received       = 0
        //   Balance        = full Amount Payable
        //   Refund         = 0
        //
        // Actual settlement is performed only when Finalising.
        // =========================================================

        if (InvoiceStatus.IsDraft(Header.Status))
        {
            Header.RecdAmount = 0M;
            Header.InvBalance =
                Header.AmountPayable.GetValueOrDefault();

            Header.InvRefund = 0M;

            return;
        }


        // =========================================================
        // LEGACY / NON-DRAFT CALCULATION
        //
        // Retained temporarily until the new Settlement workflow
        // completely replaces the old receipt processing.
        // =========================================================

        Header.InvBalance =
            MathUtils.Normalize(
                Header.AmountPayable.GetValueOrDefault()) -
            (
                Header.RecdAmount.GetValueOrDefault() +
                Header.RdAmountAdj.GetValueOrDefault()
            );

        if (invBalanceChk)
        {
            ProcessInvBalance();
        }


    }

    private bool ProcessInvBalance()
    {

        ProcessSettlements();

        //Note if inv balance is greater than zero - we need to show message to get confirmation from user
        // and warn to check there is unpaid balance........ 

/*        if (Header.InvBalance.ToString().Length != Header.DiscountAmount.ToString().Length)  == not working, inserting new record as discount - check
            return false;*/

        if (Header.InvBalance > 0)
        {
            var result = _messageBoxService.ShowMessage("Amount received is short, " +
                "Apply the balance as Credit Rs. " + Header.InvBalance + " ?", "Invoice", 
                MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

            if (result == MessageResult.Yes)
            {
                Header.PaymentDueDate = Header.InvDate.Value.AddDays(7);
                Header.InvRefund = 0M;
                BalanceVisible();
                SetReceipts("Credit");
            }
            else
            {
                return false;
            }
        }
        else if (Header.InvBalance == 0)
        {
            Header.PaymentDueDate = null;
            BalanceVisible();

        }
        else if (Header.InvBalance < 0)
        {
            var result = _messageBoxService.ShowMessage("Excess Invoice Amount received, " +
                "Do you want to Refund Rs. " + Header.InvBalance + " ?", "Invoice", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

            if (result == MessageResult.Yes)
            {
                Header.PaymentDueDate = null;
                Header.InvRefund = Header.InvBalance * -1;
                Header.InvBalance = 0M;
                RefundVisible();
                SetReceipts("Refund");
            }
            else
            {
                return false;
            }

        }

        return true;
    }

    private void RefreshInvoiceCommands()
    {
        SaveDraftInvoiceCommand.NotifyCanExecuteChanged();
        FinaliseInvoiceCommand.NotifyCanExecuteChanged();
        CancelInvoiceCommand.NotifyCanExecuteChanged();

        PrintInvoiceCommand.NotifyCanExecuteChanged();
        PrintPreviewInvoiceCommand.NotifyCanExecuteChanged();
        ExportToPdfCommand.NotifyCanExecuteChanged();
    }

    private void ProcessSettlements()
    {
        if (Header.DiscountAmount > 0)
        {
            SetReceipts("Discount");
        }
 //       if (Header.AdvanceAdj > 0)                    // blocked 18-Mar-2026 Allowed user to enter in AR receipts using drop down 
 //       {
 //           SetReceipts("Advance Receipt");
 //
 //       }
        if (Header.RdAmountAdj > 0)
        {
            SetReceipts("R.D.");
        }
    }

    /*    private async void ProcessAdvance()
        {
            //check customer has already ledger entry
            LedgerHeader = await _ledgerService.GetHeader(MtblLedger.GKey, Buyer.GKey);   //hard coded to be fixed

            //{
            //    _messageBoxService.ShowMessage($"Available Advance Balance is  {ProductIdUI}, Please make sure it exists",
            //        "Product not found", MessageButton.OK, MessageIcon.Error);
            //    return;
            //}

            if (LedgerHeader is not null)
            {

                if ((LedgerHeader.CurrentBalance < 1) || (LedgerHeader.CurrentBalance < Header.AdvanceAdj.GetValueOrDefault()))
                 {
                    _messageBoxService.ShowMessage($"Available Advance Balance is Rs.  {LedgerHeader.CurrentBalance} only...",
                                "Insufficient Balance", MessageButton.OK, MessageIcon.Error);
                    return;
                }

                LedgerHeader.CurrentBalance = LedgerHeader.CurrentBalance.GetValueOrDefault() - Header.AdvanceAdj.GetValueOrDefault();

                LedgersTransactions ledgerTrans = new();

                ledgerTrans.DrCr = "Cr";
                ledgerTrans.TransactionAmount = Header.AdvanceAdj;
                ledgerTrans.DocumentNbr = Header.InvNbr;
                ledgerTrans.DocumentDate = Header.InvDate;
                ledgerTrans.LedgerHdrGkey = LedgerHeader.GKey;
                ledgerTrans.TransactionDate = DateTime.Now;
                ledgerTrans.Status = true;

                LedgerHeader.Transactions.Add(ledgerTrans);

                await _ledgerService.CreateLedgersTransactions(LedgerHeader.Transactions);

                if (LedgerHeader.CurrentBalance < 0)
                    LedgerHeader.CurrentBalance = 0;

                await _ledgerService.UpdateHeader(LedgerHeader);
            }
            else
            {

                LedgerHeader = new();

                LedgerHeader.MtblLedgersGkey = MtblLedger.GKey;
                LedgerHeader.CustGkey = Header.CustGkey;
                LedgerHeader.BalanceAsOn = DateTime.Now;

                LedgerHeader.CurrentBalance = 0; // Header.AdvanceAdj.GetValueOrDefault();

                if (LedgerHeader.CurrentBalance < 0)
                    LedgerHeader.CurrentBalance = 0;

                LedgerHeader = await _ledgerService.CreateHeader(LedgerHeader);

                LedgersTransactions ledgerTrans = new();

                ledgerTrans.DrCr = "Cr";
                ledgerTrans.TransactionAmount = Header.AdvanceAdj.GetValueOrDefault();
                ledgerTrans.DocumentNbr = Header.InvNbr;
                ledgerTrans.DocumentDate = Header.InvDate;
                ledgerTrans.LedgerHdrGkey = LedgerHeader.GKey;
                ledgerTrans.TransactionDate = DateTime.Now;
                ledgerTrans.Status = true;

                LedgerHeader.Transactions.Add(ledgerTrans);

                await _ledgerService.CreateLedgersTransactions(LedgerHeader.Transactions);


            }

        }*/

    private void SetReceipts(String str)
    {

        var noOfLines = Header.ReceiptLines.Count;

        InvoiceArReceipt arInvRct = new InvoiceArReceipt();

        arInvRct.TransactionType = str;
        arInvRct.ModeOfReceipt = str;
        arInvRct.SeqNbr = noOfLines + 1;
        var adjustedAmount = getTransAmount(str);
        arInvRct.AdjustedAmount = adjustedAmount;

        Header.ReceiptLines.Add(arInvRct);
    }

    private async Task ProcessReceipts()
    {
        try
        {
            // Work on a snapshot to avoid collection modification issues
            var receiptsSnapshot = Header.ReceiptLines.ToList();


            //For each Receipts row - seperate Voucher has to be created
            foreach (var receipts in receiptsSnapshot ) //Header.ReceiptLines)
            {
                if (receipts is null) continue;

                var voucher = CreateVoucher(receipts);
                voucher = await SaveVoucher(voucher);

                var arReceipts = CreateArReceipts(receipts, voucher);
                await SaveArReceipts(arReceipts);

            }
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowMessage($"Error processing receipts: {ex.Message}",
                                       "Receipts Error", MessageButton.OK, MessageIcon.Error);
        }

    }
    

    private async Task ProcessOldMetalTransaction()
    {

        foreach (var omTrans in Header.OldMetalTransactions)
        {
            omTrans.EnrichInvHeaderDetails(Header);
            //omTrans.EnrichProductDetails(OldMetalProduct);
        }

        await _oldMetalTransactionService.CreateOldMetalTransaction(Header.OldMetalTransactions);
    }

    private InvoiceArReceipt CreateArReceipts(InvoiceArReceipt invoiceArReceipt, Voucher voucher)
    {

        InvoiceArReceipt arInvRct = new()
        {
            //VoucherDate = DateTime.Now
        };

        arInvRct.SeqNbr = invoiceArReceipt.SeqNbr;
        arInvRct.CustGkey = invoiceArReceipt.CustGkey;
        arInvRct.InvoiceGkey = (int?)Header.GKey;
        arInvRct.InvoiceNbr = Header.InvNbr;
        arInvRct.InvoiceReceivableAmount = invoiceArReceipt.InvoiceReceivableAmount;
        arInvRct.BalanceAfterAdj = invoiceArReceipt.BalanceAfterAdj;
        arInvRct.TransactionType = invoiceArReceipt.TransactionType;
        arInvRct.ModeOfReceipt = invoiceArReceipt.ModeOfReceipt;
        arInvRct.BalBeforeAdj = invoiceArReceipt.BalBeforeAdj;
        arInvRct.InternalVoucherNbr = voucher.VoucherNbr;
        arInvRct.InternalVoucherDate = voucher.VoucherDate;
        arInvRct.InvoiceReceiptNbr = Header.InvNbr.Replace("B", "R");  //hard coded - future review 
        arInvRct.Status = "Adj";
        arInvRct.BankName = invoiceArReceipt.BankName;
        arInvRct.ExternalTransactionId = invoiceArReceipt.ExternalTransactionId;
        arInvRct.ExternalTransactionDate = DateTime.Now;
        arInvRct.OtherReference = invoiceArReceipt.OtherReference;

        var adjustedAmount = getTransAmount(invoiceArReceipt.TransactionType);
        arInvRct.AdjustedAmount = adjustedAmount == 0 ? invoiceArReceipt.AdjustedAmount : adjustedAmount;

        return arInvRct;

    }

    private Voucher CreateVoucher(InvoiceArReceipt invoiceArReceipt)
    {

        Voucher Voucher = new()
        {
            VoucherDate = DateTime.Now
        };

        Voucher.SeqNbr = 1;
        Voucher.CustomerGkey = Header.CustGkey;
        Voucher.VoucherDate = Header.InvDate;
        Voucher.TransType = "Receipt";         // Trans_type    1 = Receipt,    2 = Payment,    3 = Journal
        Voucher.VoucherType = invoiceArReceipt.TransactionType; // Voucher_type  1 = Sales,      2 = Credit,     3 = Expense
        Voucher.Mode = invoiceArReceipt.ModeOfReceipt; // Mode          1 = Cash,       2 = Bank,       3 = Credit
        Voucher.TransDate = Voucher.VoucherDate;    // DateTime.Now;
        Voucher.VoucherNbr = Header.InvNbr;
        Voucher.RefDocNbr = Header.InvNbr;
        Voucher.RefDocDate = Header.InvDate;
        Voucher.RefDocGkey = Header.GKey;
        Voucher.TransDesc = invoiceArReceipt.TransactionType + "/ " + invoiceArReceipt.ExternalTransactionId + "/ " +
                            invoiceArReceipt.OtherReference + "/ " + invoiceArReceipt.BankName;
        //Voucher.VoucherType + "-" + Voucher.TransType + "-" + Voucher.Mode;

        var transAmount = getTransAmount(invoiceArReceipt.TransactionType);
        Voucher.TransAmount = transAmount == 0 ? invoiceArReceipt.AdjustedAmount : transAmount;

        return Voucher;

    }

    private decimal? getTransAmount(string transType)
    {
        return transType switch
        {
            var s when s.Equals("R.D.", StringComparison.OrdinalIgnoreCase) => Header.RdAmountAdj,
            var s when s.Equals("Refund", StringComparison.OrdinalIgnoreCase) => Header.InvRefund,
            var s when s.Equals("Credit", StringComparison.OrdinalIgnoreCase) => Header.InvBalance,
            var s when s.Equals("Discount", StringComparison.OrdinalIgnoreCase) => Header.DiscountAmount,
        //    var s when s.Equals("Advance Receipt", StringComparison.OrdinalIgnoreCase) => Header.AdvanceAdj,
            _ => 0M
        };

    }

    private async Task SaveArReceipts(InvoiceArReceipt invoiceArReceipt)
    {
        if (invoiceArReceipt.GKey == 0)
        {
            try
            {
                var voucherResult = await _invoiceArReceiptService.CreateInvArReceipt(invoiceArReceipt);

                if (voucherResult != null)
                {
                    invoiceArReceipt = voucherResult;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }
        else
        {
            await _invoiceArReceiptService.UpdateInvArReceipt(invoiceArReceipt);
        }

    }

    //[RelayCommand]
    private async Task<Voucher> SaveVoucher(Voucher voucher)
    {
        if (voucher.GKey == 0)
        {
            var voucherResult = await _voucherService.CreateVoucher(voucher);

            if (voucherResult != null)
            {
                voucher = voucherResult;
                //  _messageBoxService.ShowMessage("Voucher Created Successfully", "Voucher Created",
                //      MessageButton.OK, MessageIcon.Exclamation);
            }
        }
        else
        {
            await _voucherService.UpdateVoucher(voucher);
        }

        return voucher;

    }

    [RelayCommand]
    private void Focus(TextEdit sender)
    {
        sender.Focus();
    }

    [RelayCommand]
    private void ResetInvoice()
    {

        HasUnsavedChanges = false; 
        
        SetHeader();

        Buyer = null;

        CustomerPhoneNumber = null;
        CustomerState = Company?.State;

        SalesPerson = null;

        ProductIdUI = null;
        ProductSku = null;
        OldMetalIdUI = null;

        createCustomer = false;
        updateCustomer = false;

        InvLineChk = false;
        PayRctChk = false;
        invBalanceChk = false;

        CustName = null;
        CustCity = null;

        CustomerReadOnly = false;

        IsBalance = true;
        IsRefund = false;

        SaveDraftInvoiceCommand
            .NotifyCanExecuteChanged();

        FinaliseInvoiceCommand
            .NotifyCanExecuteChanged();

        CreateInvoiceCommand
            .NotifyCanExecuteChanged();

        PrintInvoiceCommand
            .NotifyCanExecuteChanged();

        PrintPreviewInvoiceCommand
            .NotifyCanExecuteChanged();

        CancelInvoiceCommand
            .NotifyCanExecuteChanged();

    }

    [RelayCommand(CanExecute = nameof(CanDeleteRows))]
    private void DeleteRows()
    {
        var result = _messageBoxService.ShowMessage("Delete all selected rows", "Delete Rows", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

        if (result == MessageResult.No)
            return;

        List<int> indexs = new List<int>();
        foreach (var row in SelectedRows)
        {
            indexs.Add(Header.Lines.IndexOf(row));
        }

        indexs.ForEach(x =>
        {
            if (x >= 0)
            {
                Header.Lines.RemoveAt(x);
            }
        });

        EvaluateForAllLines();
        EvaluateHeader();

        MarkDraftAsModified();

    }

    private bool CanDeleteRows()
    {
        return SelectedRows?.Any() ?? false;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSingleRow))]
    private void DeleteSingleRow(InvoiceLine line)
    {
        var result = _messageBoxService.ShowMessage("Delete current row", "Delete Row", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

        if (result == MessageResult.No)
            return;

        var index = Header.Lines.Remove(line);
    }

    private bool CanDeleteSingleRow(InvoiceLine line)
    {
        return line is not null && Header.Lines.IndexOf(line) > -1;
    }

    private void SetHeader()
    {
        Header = new()
        {
            InvDate = DateTime.Now,
            IsTaxApplicable = true,
            Status = InvoiceStatus.Draft
          
        };
    }

    private async void SetThisCompany()
    {
        Company = new();
        Company = await _orgThisCompanyViewService.GetOrgThisCompany();
        Header.TenantGkey = Company.TenantGkey;
        Header.GstLocSeller = Company.GstCode;
    }

    private decimal GetGSTPercent(string taxType = "SGST")
    {

        var gstPercent = _gstTaxRefList.FirstOrDefault
            (x => x.RefCode.Equals(taxType, StringComparison.OrdinalIgnoreCase));

        if (taxType.Equals("IGST", StringComparison.OrdinalIgnoreCase))
        {
            if (Buyer.Address.GstStateCode != Company.GstCode &&
                decimal.TryParse(gstPercent.RefValue.ToString(), out var igstPercent))
            {
                return igstPercent;
            }
            return 0M;
        }

        if (Buyer.Address.GstStateCode == Company.GstCode &&
            decimal.TryParse(gstPercent.RefValue.ToString(), out var result))
        {
            return result;
        }
        return 0M;
    }

    private void EvaluateForAllLines()
    {
        foreach (var line in Header.Lines)
        {
            EvaluateFormula(line);
        }
    }

    private void EvaluateFormula<T>(T item, bool isInit = false) where T : class
    {

        if (_isLoadingDraft)
            return;

        if (item is null)
            return;

        var formulas = FormulaStore.Instance.GetFormulas<T>();

        foreach (var formula in formulas)
        {
            //if (!isInit && IGNORE_UPDATE.Contains(formula.FieldName)) continue;

            var val = formula.Evaluate<T, decimal>(item, 0M);

            if (item is InvoiceLine invLine)
            {
                copyInvoiceExpression[formula.FieldName].Invoke(invLine, val);
                if (invLine.ProdNetWeight > 0)
                {
                    InvLineChk = true;
                }
            }
        }
    }

    private void EvaluateFormula<T>(T item, string fieldName, bool isInit = false) where T : class
    {
        //if (!isInit && IGNORE_UPDATE.Contains(fieldName)) return;

        var formula = FormulaStore.Instance.GetFormula<T>(fieldName);

        var val = formula.Evaluate<T, decimal>(item, 0M);

        if (item is InvoiceLine invLine)
            copyInvoiceExpression[fieldName].Invoke(invLine, val);
        else if (item is InvoiceHeader head)
            copyHeaderExpression[fieldName].Invoke(head, val);
    }

    private void RefundVisible() => SetVisibilityForRefund();
    private void BalanceVisible() => SetVisibilityForRefund(isVisible: false);

    private void SetVisibilityForRefund(bool isVisible = true)
    {
        IsRefund = isVisible;
        IsBalance = !isVisible;
    }

    private bool CustomerCheck()
    {
        if (Buyer is null)
        {
            _messageBoxService.ShowMessage("Please enter customer details to proceed", "Missing Customer", MessageButton.OK, MessageIcon.Error);
            return false;
        }

        return true;
    }

    private InvoiceHeader MapDraftHeader(
    InvoiceHeaderSaveModel source)
    {
        return new InvoiceHeader
        {
            GKey = source.Gkey,
            InvNbr = source.InvNbr,
            InvDate = source.InvDate,
            CustMobile = source.CustMobile,
            CustGkey = source.CustGkey,

            PlaceOfSeller = source.PlaceOfSeller,
            PlaceOfSupply = source.PlaceOfSupply,
            PaymentDueDate = source.PaymentDueDate,

            InvlTaxableAmount = source.InvlTaxableAmount,

            AdvanceAdj = source.AdvanceAdj,
            RdAmountAdj = source.RdAmountAdj,

            OldGoldAmount = source.OldGoldAmount,
            OldSilverAmount = source.OldSilverAmount,

            DiscountPercent = source.DiscountPercent,
            DiscountAmount = source.DiscountAmount,

            RoundOff = source.RoundOff,
            AmountPayable = source.AmountPayable,
            RecdAmount = source.RecdAmount,

            InvBalance = source.InvBalance,
            InvRefund = source.InvRefund,
            InvNotes = source.InvNotes,

            IsTaxApplicable = source.IsTaxApplicable,
            TaxType = source.TaxType,

            CgstPercent = source.CgstPercent,
            SgstPercent = source.SgstPercent,
            IgstPercent = source.IgstPercent,

            CgstAmount = source.CgstAmount,
            SgstAmount = source.SgstAmount,
            IgstAmount = source.IgstAmount,

            PaymentMode = source.PaymentMode,

            GrossRcbAmount = source.GrossRcbAmount,
            InvlTaxTotal = source.InvlTaxTotal,

            TenantGkey = source.TenantGkey,

            GstLocSeller = source.GstLocSeller,
            GstLocBuyer = source.GstLocBuyer,

            SalesPerson = source.SalesPerson,

            Status = source.Status
        };
    }

    private bool CanCancelInvoice()
    {
        if (Header is null)
            return false;

        return Header.GKey > 0 &&
               InvoiceStatus.IsDraft(Header.Status);
    }


    [RelayCommand(CanExecute = nameof(CanCancelInvoice))]
    private async Task CancelInvoice()
    {
        if (Header is null ||
            Header.GKey <= 0 ||
            !InvoiceStatus.IsDraft(Header.Status))
        {
            return;
        }

        var draftNumber =
            $"DRAFT-{Header.GKey}";

        var confirmation =
            _messageBoxService.ShowMessage(
                $"Do you want to cancel {draftNumber}?\n\n" +
                "The cancelled invoice cannot be edited or finalised.",
                "Cancel Draft Invoice",
                MessageButton.YesNo,
                MessageIcon.Warning,
                MessageResult.No);

        if (confirmation != MessageResult.Yes)
            return;

        try
        {
            await ShowProcessingAsync(
                "Cancelling draft invoice. Please wait...");

            var result =
                await _invoiceService.CancelAsync(
                    Header.GKey);

            Header.Status = result.Status;

            _messageBoxService.ShowMessage(
                $"{draftNumber} has been cancelled successfully.",
                "Invoice Cancelled",
                MessageButton.OK,
                MessageIcon.Information);

            ResetInvoice();
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowMessage(
                $"Invoice could not be cancelled.\n\n{ex.Message}",
                "Cancellation Failed",
                MessageButton.OK,
                MessageIcon.Error);
        }
        finally
        {
            HideProcessing();
        }
    }

    private InvoiceLine MapDraftLine(
    InvoiceLineSaveModel source)
    {
        return new InvoiceLine
        {
            GKey = source.Gkey,

            HsnCode = source.HsnCode,
            InvLineNbr = source.InvLineNbr,
            InvNote = source.InvNote,

            InvlBilledPrice = source.InvlBilledPrice,
            InvlGrossAmt = source.InvlGrossAmt,
            InvlMakingCharges = source.InvlMakingCharges,
            InvlOtherCharges = source.InvlOtherCharges,
            InvlPayableAmt = source.InvlPayableAmt,
            InvlStoneAmount = source.InvlStoneAmount,
            InvlTaxableAmount = source.InvlTaxableAmount,
            InvlWastageAmt = source.InvlWastageAmt,

            IsTaxable = source.IsTaxable,

            ItemNotes = source.ItemNotes,
            ItemPacked = source.ItemPacked,

            ProdCategory = source.ProdCategory,
            ProdGrossWeight = source.ProdGrossWeight,
            ProdNetWeight = source.ProdNetWeight,
            ProdQty = source.ProdQty,
            ProdStoneWeight = source.ProdStoneWeight,

            ProductDesc = source.ProductDesc,
            ProductGkey = source.ProductGkey,
            ProductName = source.ProductName,
            ProdPackCode = source.ProdPackCode,
            ProductPurity = source.ProductPurity,

            TaxAmount = source.TaxAmount,
            TaxPercent = source.TaxPercent,
            TaxType = source.TaxType,

            VaAmount = source.VaAmount,
            VaPercent = source.VaPercent,

            InvoiceHdrGkey = source.InvoiceHdrGkey,
            InvoiceId = source.InvoiceId,

            TenantGkey = source.TenantGkey,

            InvlCgstPercent = source.InvlCgstPercent,
            InvlCgstAmount = source.InvlCgstAmount,

            InvlIgstPercent = source.InvlIgstPercent,
            InvlIgstAmount = source.InvlIgstAmount,

            InvlSgstPercent = source.InvlSgstPercent,
            InvlSgstAmount = source.InvlSgstAmount,

            InvlTotal = source.InvlTotal,

            ProductId = source.ProductId,
            ProductSku = source.ProductSku,

            Metal = source.Metal
        };
    }

    private OldMetalTransaction MapDraftOldMetal(
    InvoiceOldMetalSaveModel source)
    {
        return new OldMetalTransaction
        {
            GKey = source.Gkey,

            TransNbr = source.TransNbr,
            TransDate = source.TransDate,
            TransType = source.TransType,

            DocRefGkey = source.DocRefGkey,
            DocRefNbr = source.DocRefNbr,
            DocRefDate = source.DocRefDate,

            CustGkey = source.CustGkey,
            CustMobile = source.CustMobile,

            ProductGkey = source.ProductGkey,
            ProductId = source.ProductId,
            ProductCategory = source.ProductCategory,

            Metal = source.Metal,
            Purity = source.Purity,

            TransactedRate = source.TransactedRate,

            GrossWeight = source.GrossWeight,
            StoneWeight = source.StoneWeight,
            WastageWeight = source.WastageWeight,
            NetWeight = source.NetWeight,

            TotalProposedPrice = source.TotalProposedPrice,
            FinalPurchasePrice = source.FinalPurchasePrice,

            DocRefType = source.DocRefType,

           // TenantGkey = source.TenantGkey
        };
    }

    private InvoiceArReceipt MapDraftReceipt(
    InvoiceReceiptSaveModel source)
    {
        return new InvoiceArReceipt
        {
            GKey = source.Gkey,

            SeqNbr = source.SeqNbr,

            CustGkey = source.CustGkey,

            InvoiceGkey = source.InvoiceGkey,
            InvoiceNbr = source.InvoiceNbr,

            InvoiceReceivableAmount =
                source.InvoiceReceivableAmount,

            BalBeforeAdj =
                source.BalBeforeAdj,

            AdjustedAmount =
                source.AdjustedAmount,

            BalanceAfterAdj =
                source.BalanceAfterAdj,

            TransactionType =
                source.TransactionType,

            ModeOfReceipt =
                source.ModeOfReceipt,

            InternalVoucherNbr =
                source.InternalVoucherNbr,

            InternalVoucherDate =
                source.InternalVoucherDate,

            InvoiceReceiptNbr =
                source.InvoiceReceiptNbr,

            Status =
                source.Status,

            BankName =
                source.BankName,

            ExternalTransactionId =
                source.ExternalTransactionId,

            ExternalTransactionDate =
                source.ExternalTransactionDate,

            OtherReference =
                source.OtherReference,

        //    TenantGkey =
        //        source.TenantGkey
        };
    }


}