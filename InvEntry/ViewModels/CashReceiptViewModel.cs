using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Printing;
using DevExpress.XtraRichEdit.Model;
using DevExpress.XtraTreeList.Filtering.Provider;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Models.Extensions;
using InvEntry.Reports;
using InvEntry.Services;
using InvEntry.Store;
using InvEntry.Tally;
using InvEntry.Utils;
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

public partial class CashReceiptViewModel : ObservableObject
{

    [ObservableProperty]
    private Voucher _voucher;

    [ObservableProperty]
    private string _customerPhoneNumber;

    [ObservableProperty]
    private string _selectedLedger;

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
    private ObservableCollection<MtblReference> _stateReferencesList;

    [ObservableProperty]
    private ObservableCollection<MtblReference> _mtblReferencesList;

    [ObservableProperty]
    private ObservableCollection<string> _fromLedgerList;
    
    //private ObservableCollection<KeyValuePair<int,string>> fromLedgerList;

    private bool createCustomer = false;

    private readonly ICustomerService _customerService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IInvoiceArReceiptService _invoiceArReceiptService;
    private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;
    private readonly IMtblReferencesService _mtblReferencesService;
    private readonly IVoucherService _voucherService;
    private readonly ILedgerService _ledgerService;
    private readonly IMtblLedgersService _mtblLedgersService;


    public CashReceiptViewModel(ICustomerService customerService,
                IOrgThisCompanyViewService orgThisCompanyViewService,
                IMessageBoxService messageBoxService,
                IVoucherService voucherService,
                ILedgerService ledgerService,
                IMtblLedgersService mtblLedgersService,
                IMtblReferencesService mtblReferencesService
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
        FromLedgerList.Add("Advance Receipt");
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

    }

    [RelayCommand]
    private void VoucherPrintCommand()
    {

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

        Voucher.CustomerGkey = Payer.GKey;
        Voucher.FromLedgerGkey = (await _mtblLedgersService.GetLedger(2000)).GKey;
        Voucher.ToLedgerGkey = MtblLedger.GKey;
        Voucher.VoucherType = SelectedLedger;
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

        ProcessLedgerTransactions();

        ResetVoucher();


    }
    
}
