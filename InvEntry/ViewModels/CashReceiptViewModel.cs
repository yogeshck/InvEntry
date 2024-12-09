using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Printing;
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
    private ObservableCollection<MtblReference> mtblReferencesList;

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
        _customerService = customerService;
        _messageBoxService = messageBoxService;
        _orgThisCompanyViewService = orgThisCompanyViewService;
        _mtblReferencesService = mtblReferencesService;
        _mtblLedgersService = mtblLedgersService;
        _voucherService = voucherService;
        _ledgerService = ledgerService;

        SetThisCompany();
        PopulateStateList();
        SetMasterLedger();

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
        MtblLedger = await _mtblLedgersService.GetLedger(1000);   //pass account code
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

        //check customer has any ledger entry
        LedgerHdr = await _ledgerService.GetHeader(MtblLedger.GKey, Payer.GKey);

        if (LedgerHdr is not null)
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

            LedgerHdr.CurrentBalance = LedgerHdr.CurrentBalance.GetValueOrDefault() + Voucher.TransAmount.GetValueOrDefault();

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
        Voucher.VoucherType = "Advance";  //hardcoded
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

        if (Voucher.GKey == 0)
        {
            var voucher = await _voucherService.CreatVoucher(Voucher);

            ProcessLedger();   // (voucher);

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

        ResetVoucher();


    }
    
}
