using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.Editors;
using InvEntry.Contracts.Advances;
using InvEntry.Contracts.Schemes;
using InvEntry.Extension;
using InvEntry.Helpers;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace InvEntry.ViewModels;

public partial class CashReceiptViewModel : ObservableObject
{

    [ObservableProperty]
    private Voucher _voucher;

    [ObservableProperty]
    private string _customerPhoneNumber;

    [ObservableProperty]
    private string _selectedLedger;

/*    [ObservableProperty]
    private string _selectedInv;*/

    [ObservableProperty]
    private MtblReference _customerState;

    [ObservableProperty]
    private string _voucherTransDesc;

    [ObservableProperty]
    private string _transactionType;

    [ObservableProperty]
    private Customer _payer;

    [ObservableProperty]
    private OrgThisCompanyView _company;

    [ObservableProperty]
    private MtblLedger _mtblLedger;

    [ObservableProperty]
    public bool _customerReadOnly;

    [ObservableProperty]
    private DateSearchOption _searchOption;

    [ObservableProperty]
    private InvoiceHeader _invoice;

    [ObservableProperty]
    private InvoiceArReceipt _invArReceipt;

    [ObservableProperty]
    private ObservableCollection<MtblReference> _stateReferencesList;

    [ObservableProperty]
    private ObservableCollection<MtblReference> _mtblReferencesList;

    [ObservableProperty]
    private ObservableCollection<string> _fromLedgerList;

 //   [ObservableProperty]
 //   private ObservableCollection<string> _osInvoiceList;

    // List of invoices to bind to ComboBox
    [ObservableProperty]
    private ObservableCollection<InvoiceHeader> _osInvoicesList;

    // Selected invoice from ComboBox
    [ObservableProperty]
    private InvoiceHeader _selectedInv;

    [ObservableProperty]
    private Visibility invoiceFieldVisibility = Visibility.Collapsed;

    // Recurring Deposit receipts pay one of the customer's enrolled schemes.
    [ObservableProperty]
    private ObservableCollection<SchemeEnrollmentDto> _customerSchemes = new();

    [ObservableProperty]
    private SchemeEnrollmentDto? _selectedScheme;

    [ObservableProperty]
    private Visibility schemeFieldVisibility = Visibility.Collapsed;

    //private ObservableCollection<KeyValuePair<int,string>> fromLedgerList;

    private bool createCustomer = false;
    private bool creditReceipt = false;

    // Kept until the server confirms the receipt, so a retry after a failure
    // reuses the same ClientRequestId and cannot record the money twice.
    private AdvanceReceiptRequest? _pendingAdvanceReceipt;
        
    private readonly ICustomerService _customerService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IInvoiceArReceiptService _invoiceArReceiptService;
    private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;
    private readonly IMtblReferencesService _mtblReferencesService;
    private readonly IVoucherService _voucherService;
    private readonly IInvoiceService _invoiceService;
    private readonly IMtblLedgersService _mtblLedgersService;
    private readonly IAdvanceAccountService _advanceAccountService;
    private readonly ISchemeService _schemeService;
    private readonly IAdvanceReceiptPrinter _receiptPrinter;

    public CashReceiptViewModel(ICustomerService customerService,
                IOrgThisCompanyViewService orgThisCompanyViewService,
                IMessageBoxService messageBoxService,
                IVoucherService voucherService,
                IMtblLedgersService mtblLedgersService,
                IMtblReferencesService mtblReferencesService,
                IInvoiceService invoiceService,
                IInvoiceArReceiptService invoiceArReceiptService,
                IAdvanceAccountService advanceAccountService,
                ISchemeService schemeService,
                IAdvanceReceiptPrinter receiptPrinter
                )
    {

        FromLedgerList = new();

        _customerService = customerService;
        _messageBoxService = messageBoxService;
        _orgThisCompanyViewService = orgThisCompanyViewService;
        _mtblReferencesService = mtblReferencesService;
        _mtblLedgersService = mtblLedgersService;
        _voucherService = voucherService;
        _advanceAccountService = advanceAccountService;
        _schemeService = schemeService;
        _receiptPrinter = receiptPrinter;
        _invoiceService = invoiceService;
        _invoiceArReceiptService = invoiceArReceiptService;

        SetThisCompany();
        PopulateStateList();
        PopulateReferenceList();
        //SetMasterLedger();

        ResetVoucher();

    }

    private async void SetThisCompany()
    {
        Company = new();
        Company = await _orgThisCompanyViewService.GetOrgThisCompany();
    }

    private async void PopulateStateList()
    {
        var stateRefList = new List<MtblReference>();

        var stateRefServiceList = await _mtblReferencesService.GetReferenceList("CUST_STATE");

        if (stateRefServiceList is null)
        {
            stateRefList.Add(new MtblReference() { RefValue = "Tamil Nadu", RefCode = "33" });
            stateRefList.Add(new MtblReference() { RefValue = "Kerala", RefCode = "32" });
            stateRefList.Add(new MtblReference() { RefValue = "Karnataka", RefCode = "30" });
        }
        else
        {
            stateRefList.AddRange(stateRefServiceList);
        }

        StateReferencesList = new(stateRefList);

        // CustomerState = StateReferencesList.FirstOrDefault(x => x.RefCode.Equals(Company.GstCode));
    }

    private async Task SetMasterLedger()
    {

        //var frLedgername = FromLedgerList.Select(x => x.Value == SelectedLedger);

        var accountCode = 1000;   // Advance Receipt
        if (TransactionType.Equals("Recurring Deposit"))
            accountCode = 3000;

        MtblLedger = new();
        MtblLedger = await _mtblLedgersService.GetLedger(accountCode);      //GetLedger(1000);   //pass account code - Advance Receipt

        if (MtblLedger is null)
            return;
    }

    private void PopulateReferenceList()
    {

        /*  var FromLedgerList = new List<KeyValuePair<int, string>>
      {
              new KeyValuePair<int, string>(2000, "Advance Receipt"),
              new KeyValuePair<int, string>(3000, "RD")
          };*/
        FromLedgerList.Add("Advance Receipt");    //Ensure respective voucher types added in master table
        FromLedgerList.Add("Credit Receipt");
        FromLedgerList.Add("Recurring Deposit");

    }

    [RelayCommand]
    private async Task FetchCustomer(EditValueChangedEventArgs args)
    {
        if (args.NewValue is not string customerPhoneNumber) return;

        if (TransactionType == "Credit Receipt")
            creditReceipt = true;

        customerPhoneNumber = customerPhoneNumber.Trim();

        if (string.IsNullOrEmpty(customerPhoneNumber) || customerPhoneNumber.Length < 10)
            return;

        CustomerReadOnly = false;

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Fetching Customer details..."));

        Payer = await _customerService.GetCustomer(customerPhoneNumber);

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

        if (Payer is null)
        {
            _messageBoxService.ShowMessage("No customer details found.", "Customer not found", MessageButton.OK);

            // to check with user, do we need to create a user ????? if yes, we need to do -- logic later
            Payer = new();
            Payer.MobileNbr = customerPhoneNumber;
            Payer.Address.GstStateCode = Company.GstCode;
            Payer.Address.State = Company.State;
            Payer.Address.District = Company.District;

            createCustomer = true;
            CustomerState = StateReferencesList.FirstOrDefault(x => x.RefCode == Company.GstCode);
        }
        else
        {
            var gstCode = Payer.Address is null ? Company.GstCode : Payer.Address.GstStateCode;

            if (Payer.Address is null)
            {
                Payer.Address = new();
                Payer.Address.GstStateCode = Company.GstCode;
            }

            CustomerState = StateReferencesList.FirstOrDefault(x => x.RefCode == gstCode);

            //Messenger.Default.Send("ProductIdUIName", MessageType.FocusTextEdit);

        }

        if (creditReceipt)
            await FetchOutStandingAsync(customerPhoneNumber);

        await LoadCustomerSchemesAsync();
    }

    /// <summary>
    /// For a Recurring Deposit receipt, lists the customer's enrolled schemes
    /// that still have installments due; a single scheme is selected.
    /// </summary>
    private async Task LoadCustomerSchemesAsync()
    {
        CustomerSchemes = new();
        SelectedScheme = null;

        if (TransactionType != AdvanceReceiptType.RecurringDeposit ||
            Payer is null || Payer.GKey <= 0)
        {
            return;
        }

        try
        {
            var schemes = await _schemeService.GetCustomerEnrollments((int)Payer.GKey);

            CustomerSchemes = new(schemes.Where(x =>
                x.Status == "ACTIVE" && x.NextInstallment is not null));
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowMessage(
                "The customer's RD schemes could not be loaded." + Environment.NewLine + ApiErrorMessage.From(ex),
                "Recurring Deposit", MessageButton.OK, MessageIcon.Warning);
            return;
        }

        if (CustomerSchemes.Count == 0)
        {
            _messageBoxService.ShowMessage(
                "This customer has no active RD scheme. Enroll the customer in a scheme first (Finance > Scheme Enrollment).",
                "Recurring Deposit", MessageButton.OK, MessageIcon.Exclamation);
            return;
        }

        if (CustomerSchemes.Count == 1)
            SelectedScheme = CustomerSchemes[0];
    }

    partial void OnSelectedSchemeChanged(SchemeEnrollmentDto? value)
    {
        // Suggest what is due on the next installment; the cashier can change it.
        if (value?.NextInstallment is { } next && Voucher is not null)
        {
            Voucher.TransAmount = next.AmountOutstanding;
            OnPropertyChanged(nameof(Voucher));
        }
    }

    private async Task FetchOutStandingAsync(String CustMobile)
    {

        SearchOption = new();
        SearchOption.Filter1 = CustMobile;

        var OsInvlist = await _invoiceService.GetOutStanding(SearchOption);

        OsInvoicesList = new(OsInvlist);

/*        if (OsInvlist is not null)
        {
            OsInvoiceList = new();
            foreach (var invoice in OsInvlist)
            {
                OsInvoiceList.Add(invoice.InvNbr);
            }

        }*/
    }

    partial void OnSelectedInvChanged(InvoiceHeader value)
    {
        if (value != null)
        {
            Invoice = value;
            Voucher.TransAmount = value.InvBalance;
        }
    }


    [RelayCommand]
    private void VoucherPrintCommand()
    {

    }

    /*    private void SetVoucherType()
        {
            Voucher.TransType = "Receipt";
            Voucher.VoucherType = "Advance";

        }*/

    [RelayCommand]
    private void ResetVoucher()
    {

        Voucher = new();
        Payer = new();

        Voucher.Mode = "Cash";
        Voucher.VoucherDate = DateTime.Now;

        Voucher.TransDate = Voucher.VoucherDate;    // DateTime.Now;
        Voucher.TransType = "Receipt";
        //Voucher.VoucherType = "Advance";  //hardcoded
        Voucher.TransDesc = string.Empty;

        SelectedInv = null;
        VoucherTransDesc = string.Empty;
        CustomerPhoneNumber = string.Empty;
        createCustomer = false;
        creditReceipt = false;
        _pendingAdvanceReceipt = null;
        CustomerSchemes = new();
        SelectedScheme = null;
    }

    partial void OnTransactionTypeChanged(string value)
    {
        InvoiceFieldVisibility = value == "Credit Receipt" ? Visibility.Visible : Visibility.Collapsed;
        SchemeFieldVisibility = value == AdvanceReceiptType.RecurringDeposit ? Visibility.Visible : Visibility.Collapsed;

        _ = LoadCustomerSchemesAsync();
    }


    [RelayCommand]
    private void Focus(TextEdit sender)
    {
        sender.Focus();
    }

    [RelayCommand]
    private async Task SaveVoucherAsync()
    {
        if (createCustomer)
        {
            Payer = await _customerService.CreateCustomer(Payer);
            createCustomer = false;
        }

        if (IsAdvanceOrRdReceipt)
        {
            // Voucher, advance ledger and balance are written by the server
            // in one transaction; LEDGERS_HEADER is no longer updated.
            if (await SaveAdvanceReceiptAsync())
                ResetVoucher();

            return;
        }

        await SetMasterLedger();

        if (MtblLedger is null)
        {
            _messageBoxService.ShowMessage("Receipt ledger is not configured.", "Error", MessageButton.OK, MessageIcon.Error);
            return;
        }

        await ProcessVoucher();

        if (creditReceipt)
        {
            await ProcessCreditReceiptAsync();
        }

        ResetVoucher();

    }

    private bool IsAdvanceOrRdReceipt =>
        TransactionType == AdvanceReceiptType.AdvanceReceipt ||
        TransactionType == AdvanceReceiptType.RecurringDeposit;

    private async Task<bool> SaveAdvanceReceiptAsync()
    {
        var amount = Voucher.TransAmount.GetValueOrDefault();

        if (Payer is null || Payer.GKey <= 0)
        {
            _messageBoxService.ShowMessage("Select or create the customer first.", "Receipt", MessageButton.OK, MessageIcon.Exclamation);
            return false;
        }

        if (amount <= 0)
        {
            _messageBoxService.ShowMessage("Enter the amount received.", "Receipt", MessageButton.OK, MessageIcon.Exclamation);
            return false;
        }

        var isRd = TransactionType == AdvanceReceiptType.RecurringDeposit;
        int? accountGkey = null;

        if (isRd)
        {
            if (SelectedScheme is null)
            {
                _messageBoxService.ShowMessage(
                    CustomerSchemes.Count == 0
                        ? "This customer has no active RD scheme. Enroll the customer in a scheme first."
                        : "Select the RD scheme this installment is for.",
                    "Recurring Deposit", MessageButton.OK, MessageIcon.Exclamation);
                return false;
            }

            accountGkey = SelectedScheme.AdvanceAccountGkey;
        }

        // Same receipt retried -> same ClientRequestId; anything changed -> new receipt.
        if (_pendingAdvanceReceipt is null ||
            _pendingAdvanceReceipt.CustomerGkey != Payer.GKey ||
            _pendingAdvanceReceipt.ReceiptType != TransactionType ||
            _pendingAdvanceReceipt.AdvanceAccountGkey != accountGkey ||
            _pendingAdvanceReceipt.Amount != amount)
        {
            _pendingAdvanceReceipt = new AdvanceReceiptRequest
            {
                ClientRequestId = Guid.NewGuid(),
                CustomerGkey = Payer.GKey,
                ReceiptType = TransactionType,
                AdvanceAccountGkey = accountGkey,
                Amount = amount
            };
        }

        _pendingAdvanceReceipt.Mode = string.IsNullOrWhiteSpace(Voucher.Mode) ? "Cash" : Voucher.Mode;
        _pendingAdvanceReceipt.TransDesc = VoucherTransDesc;
        _pendingAdvanceReceipt.TransDate = Voucher.TransDate;

        try
        {
            var result = await _advanceAccountService.CreateReceipt(_pendingAdvanceReceipt);

            _pendingAdvanceReceipt = null;
            Voucher.GKey = result.VoucherGkey;
            Voucher.VoucherNbr = result.VoucherNbr;

            var message = $"Voucher {result.VoucherNbr} created. Balance in {result.AccountNbr}: Rs. {result.BalanceAmount:N2}";

            if (result.Weight > 0M)
                message += $"{Environment.NewLine}{result.Weight:N3} g credited at Rs. {result.MetalRate:N2}/g; total {result.BalanceWeight:N3} g.";

            if (result.InstallmentsPaid is int paid && SelectedScheme is not null)
                message += $"{Environment.NewLine}Installments paid: {paid} of {SelectedScheme.TenureMonths}.";

            message += $"{Environment.NewLine}{Environment.NewLine}Print the receipt now?";

            if (_messageBoxService.ShowMessage(message, "Voucher Created", MessageButton.YesNo, MessageIcon.Question)
                == MessageResult.Yes)
            {
                await _receiptPrinter.PreviewAsync(result.VoucherGkey, isReprint: result.IsReplay);
            }

            return true;
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowMessage(
                "Error creating receipt. Saving again will not record it twice." +
                Environment.NewLine + ApiErrorMessage.From(ex),
                "Error", MessageButton.OK, MessageIcon.Error);

            return false;
        }
    }

/*    private async Task FetchInvoice()
    {
        Invoice = new();

        if (SelectedInv is not null)
        {
            Invoice = await _invoiceService.GetHeader(SelectedInv);
        }
        else
        {
            return;
        }
    }*/

    private async Task ProcessCreditReceiptAsync()
    {
        InvArReceipt = CreateArReceipts(Voucher);

        InvBalanceCheck();

        await SaveArReceipts(InvArReceipt);

        await _invoiceService.UpdateHeader(Invoice);
    }

    private InvoiceArReceipt CreateArReceipts(Voucher voucher)
    {

        InvoiceArReceipt arInvRct = new()
        {
            //VoucherDate = DateTime.Now
        };

        arInvRct.SeqNbr = Invoice.ReceiptLines.Count + 1;
        arInvRct.CustGkey = Invoice.CustGkey;
        arInvRct.InvoiceGkey = (int?)Invoice.GKey;
        arInvRct.InvoiceNbr = Invoice.InvNbr;
        arInvRct.InvoiceReceivableAmount = Invoice.InvBalance;
        arInvRct.BalanceAfterAdj = Invoice.InvBalance - voucher.TransAmount;
        arInvRct.TransactionType = TransactionType; // invoiceArReceipt.TransactionType;
                                                   // arInvRct.ModeOfReceipt = invoiceArReceipt.ModeOfReceipt;
        arInvRct.ModeOfReceipt = "Cash";
        arInvRct.BalBeforeAdj = Invoice.InvBalance;
        arInvRct.InternalVoucherNbr = voucher.VoucherNbr;
        arInvRct.InternalVoucherDate = voucher.VoucherDate;
        arInvRct.InvoiceReceiptNbr = Invoice.InvNbr.Replace("B", "R");  //hard coded - future review 
        arInvRct.Status = "Adj";

        //var adjustedAmount = AdjustmentAmount; // getTransAmount(invoiceArReceipt.TransactionType);
        arInvRct.AdjustedAmount = voucher.TransAmount; ;
        //adjustedAmount == 0 ? invoiceArReceipt.AdjustedAmount : adjustedAmount;

        return arInvRct;

    }

    private void InvBalanceCheck()
    {

        if (Invoice == null) return;

        if (Invoice.InvBalance <= 0)
            //show message and return
            return;

        //partial and full payment
        if (Invoice.InvBalance.GetValueOrDefault() >= Voucher.TransAmount)
            Invoice.InvBalance = Invoice.InvBalance.GetValueOrDefault() - Voucher.TransAmount;

        else if (Invoice.InvBalance.GetValueOrDefault() < Voucher.TransAmount)
        {
            Voucher.TransAmount = Voucher.TransAmount - Invoice.InvBalance.GetValueOrDefault();
            Invoice.InvBalance = 0;

            var result = _messageBoxService.ShowMessage(
                                                            "Received Amount is more than the Invoice Amount, " +
                                                            "Excess Amount of Rs. " +
                                                            Voucher.TransAmount + " ?", "Invoice",
                                                            MessageButton.YesNo,
                                                            MessageIcon.Question,
                                                            MessageResult.No
                                                       );
            //in this sceneario - supposed to create two voucher - one is for adjusting the invoice balance and another is for advance
            // we should keep this balance as advance or display to adjust against other outstanding invoice.
            //to be implemented

            if (result == MessageResult.Yes)
            {
            }

        }

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

    private async Task ProcessVoucher()
    {
        Voucher.CustomerGkey = Payer.GKey;
        Voucher.FromLedgerGkey = (await _mtblLedgersService.GetLedger(2000)).GKey;
        Voucher.ToLedgerGkey = MtblLedger.GKey;
        Voucher.VoucherType = TransactionType;
        Voucher.TransDesc = VoucherTransDesc;

        if (creditReceipt)
        {
            Voucher.RefDocGkey = Invoice.GKey;
            Voucher.RefDocNbr = Invoice.InvNbr;
            Voucher.RefDocDate = Invoice.InvDate;
        }

        //Voucher.RefDocNbr = "RD";  //replace with ui user entered field.

        if (Voucher.GKey == 0)
        {
            var voucher = await _voucherService.CreateVoucher(Voucher);

            // (voucher);

            if (voucher != null)
            {
                Voucher = voucher;
                _messageBoxService.ShowMessage("Voucher Created Successfully", "Voucher Created", MessageButton.OK, MessageIcon.Exclamation);

                /*                Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Print Invoice..."));
                                PrintPreviewInvoice();
                                PrintPreviewInvoiceCommand.NotifyCanExecuteChanged();
                                PrintInvoiceCommand.NotifyCanExecuteChanged();
                                Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());*/

            }
            else
            {
                _messageBoxService.ShowMessage("Error creating Voucher", "Error", MessageButton.OK, MessageIcon.Error);
                return;
            }
        }
        else
        {
            await _voucherService.UpdateVoucher(Voucher);
        }
    }

}