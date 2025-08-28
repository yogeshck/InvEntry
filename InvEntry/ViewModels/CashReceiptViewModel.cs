using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.Editors;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

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
    private Customer _payer;

    [ObservableProperty]
    private OrgThisCompanyView _company;

    [ObservableProperty]
    private LedgersHeader _ledgerHdr;

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


    //private ObservableCollection<KeyValuePair<int,string>> fromLedgerList;

    private bool createCustomer = false;

    private readonly ICustomerService _customerService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IInvoiceArReceiptService _invoiceArReceiptService;
    private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;
    private readonly IMtblReferencesService _mtblReferencesService;
    private readonly IVoucherService _voucherService;
    private readonly IInvoiceService _invoiceService;
    private readonly ILedgerService _ledgerService;
    private readonly IMtblLedgersService _mtblLedgersService;


    public CashReceiptViewModel(ICustomerService customerService,
                IOrgThisCompanyViewService orgThisCompanyViewService,
                IMessageBoxService messageBoxService,
                IVoucherService voucherService,
                ILedgerService ledgerService,
                IMtblLedgersService mtblLedgersService,
                IMtblReferencesService mtblReferencesService,
                IInvoiceService invoiceService,
                IInvoiceArReceiptService invoiceArReceiptService
                )
    {

        FromLedgerList = new();

        _customerService = customerService;
        _messageBoxService = messageBoxService;
        _orgThisCompanyViewService = orgThisCompanyViewService;
        _mtblReferencesService = mtblReferencesService;
        _mtblLedgersService = mtblLedgersService;
        _voucherService = voucherService;
        _ledgerService = ledgerService;
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
        if (SelectedLedger.Equals("Recurring Deposit"))
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
        if (SelectedLedger == "Credit Receipt")
            await FetchOutStandingAsync(customerPhoneNumber);
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

        LedgerHdr = new();
        Voucher = new();
        Payer = new();

        Voucher.Mode = "Cash";
        Voucher.VoucherDate = DateTime.Now;

        Voucher.TransDate = Voucher.VoucherDate;    // DateTime.Now;
        Voucher.TransType = "Receipt";
        //Voucher.VoucherType = "Advance";  //hardcoded
        Voucher.TransDesc = string.Empty;

        VoucherTransDesc = string.Empty;
        CustomerPhoneNumber = string.Empty;
        createCustomer = false;

    }

    [RelayCommand]
    private void Focus(TextEdit sender)
    {
        sender.Focus();
    }

    [RelayCommand]
    private async Task SaveVoucherAsync()
    {
        //SetVoucherType();

        if (createCustomer)
        {
            Payer = await _customerService.CreateCustomer(Payer);
        }

        ProcessLedger();

      //  if (SelectedLedger == "Credit Receipt")
      //  {
      //      await FetchInvoice();
      //  }

        await ProcessVoucher();

        if (SelectedLedger == "Credit Receipt")
        {
            await ProcessCreditReceiptAsync();
        }

        ProcessLedgerTransactions();

        ResetVoucher();

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
        arInvRct.TransactionType = SelectedLedger; // invoiceArReceipt.TransactionType;
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
        Voucher.VoucherType = SelectedLedger;
        Voucher.RefDocGkey = Invoice.GKey;
        Voucher.RefDocNbr = Invoice.InvNbr;
        Voucher.RefDocDate = Invoice.InvDate;
        Voucher.TransDesc = VoucherTransDesc;
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

    private async void ProcessLedger() //(Voucher voucher)
    {

        await SetMasterLedger();
        if (MtblLedger.GKey < 0)
            return;

        //check customer has any ledger entry

        LedgerHdr = new();
        LedgerHdr = await _ledgerService.GetHeader(MtblLedger.GKey, Payer.GKey);

        if (LedgerHdr is not null)
        {
            /*            LedgersTransactions ledgerTrans = new();

                        ledgerTrans.DrCr = "Cr";
                        ledgerTrans.TransactionAmount = Voucher.TransAmount;
                        ledgerTrans.DocumentNbr = Voucher.VoucherNbr;
                        ledgerTrans.DocumentDate = Voucher.VoucherDate;
                        ledgerTrans.LedgerHdrGkey = LedgerHdr.GKey;
                        ledgerTrans.TransactionDate = DateTime.Now;
                        ledgerTrans.Status = true;

                        LedgerHdr.Transactions.Add(ledgerTrans);*/

            //await _ledgerService.CreateLedgersTransactions(LedgerHdr.Transactions);

            LedgerHdr.BalanceAsOn = DateTime.Now;
            LedgerHdr.CurrentBalance = LedgerHdr.CurrentBalance.GetValueOrDefault() +
                                            Voucher.TransAmount.GetValueOrDefault();

            await _ledgerService.UpdateHeader(LedgerHdr);
        }
        else
        {

            LedgerHdr = new();

            LedgerHdr.MtblLedgersGkey = MtblLedger.GKey;
            LedgerHdr.CustGkey = Payer.GKey;
            LedgerHdr.BalanceAsOn = DateTime.Now;
            LedgerHdr.CurrentBalance = Voucher.TransAmount;

            LedgerHdr = await _ledgerService.CreateHeader(LedgerHdr);

        }

    }

    private async void ProcessLedgerTransactions()
    {
        LedgersTransactions ledgerTrans = new();

        ledgerTrans.DrCr = "Cr";
        ledgerTrans.TransactionAmount = Voucher.TransAmount;
        ledgerTrans.DocumentNbr = Voucher.VoucherNbr;
        ledgerTrans.DocumentDate = Voucher.VoucherDate;
        ledgerTrans.LedgerHdrGkey = LedgerHdr.GKey;
        ledgerTrans.TransactionDate = DateTime.Now;
        ledgerTrans.Status = true;

        LedgerHdr.Transactions.Add(ledgerTrans);

        await _ledgerService.CreateLedgersTransactions(LedgerHdr.Transactions);
    }

}