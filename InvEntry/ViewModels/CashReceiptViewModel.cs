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
    private Customer _payer;

    [ObservableProperty]
    private OrgThisCompanyView _company;

    [ObservableProperty]
    private ObservableCollection<MtblReference> _stateReferencesList;

    [ObservableProperty]
    private ObservableCollection<MtblReference> mtblReferencesList;

    private bool createCustomer = false;
    private bool invBalanceChk = false;

    private readonly ICustomerService _customerService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IInvoiceArReceiptService _invoiceArReceiptService;
    private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;
    private readonly MtblReferencesService _mtblReferencesService;
    private readonly IVoucherService _voucherService;
    private readonly ILedgerService _ledgerService;


    public CashReceiptViewModel(ICustomerService customerService,
                IOrgThisCompanyViewService orgThisCompanyViewService,
                IMessageBoxService messageBoxService,
                IVoucherService voucherService,
                ILedgerService ledgerService,
                MtblReferencesService mtblReferencesService
                )
    {
        _customerService = customerService;
        _messageBoxService = messageBoxService;
        _orgThisCompanyViewService = orgThisCompanyViewService;
        _mtblReferencesService = mtblReferencesService;
        _voucherService = voucherService;
        _ledgerService = ledgerService;

        SetThisCompany();
        PopulateStateList();
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


    [RelayCommand]
    private async Task FetchCustomer(EditValueChangedEventArgs args)
    {
        if (args.NewValue is not string phoneNumber) return;

        phoneNumber = phoneNumber.Trim();

        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 10)
            return;

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Fetching Customer details..."));

        Payer = await _customerService.GetCustomer(phoneNumber);

        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

        if (Payer is null)
        {
            _messageBoxService.ShowMessage("No customer details found.", "Customer not found", MessageButton.OK);

            /*            Payer = new();
                        Payer.MobileNbr = phoneNumber;
                        Payer.Address.GstStateCode = Company.GstCode;
                        Payer.Address.State = Company.State;
                        Payer.Address.District = Company.District;

                        createCustomer = true;
                        CustomerState = StateReferencesList.FirstOrDefault(x => x.RefCode == Company.GstCode);*/
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

    private async void ProcessLedger(Voucher voucher)
    {

        //check customer has already ledger entry
        var ledgerHdr = await _ledgerService.GetHeader(voucher.CustomerGkey);

        if (ledgerHdr is not null)
        {
            LedgersTransactions ledgerTrans = new();

            ledgerTrans.DrCr = "Cr";
            ledgerTrans.TransactionAmount = Voucher.TransAmount;
            ledgerTrans.DocumentNbr = "Doc";
            ledgerTrans.DocumentDate = DateTime.Now;

            ledgerHdr.CurrentBalance = voucher.TransAmount;

            ledgerHdr.Transactions.Add(ledgerTrans);

            await _ledgerService.CreateLedgersTransactions(ledgerHdr.Transactions);

            ledgerHdr.CurrentBalance = ledgerHdr.CurrentBalance - voucher.TransAmount;

            await _ledgerService.UpdateHeader(ledgerHdr);
        }
        else
        {
            LedgersHeader ledgersHeader = new();
            ledgersHeader.BalanceAsOn = DateTime.Now;


            LedgersTransactions ledgerTrans = new();

            ledgerTrans.DrCr = "Cr";
            ledgerTrans.TransactionAmount = Voucher.TransAmount;
            ledgerTrans.DocumentNbr = "Doc";
            ledgerTrans.DocumentDate = DateTime.Now;

            ledgersHeader.Transactions.Add(ledgerTrans);

            await _ledgerService.CreateLedgersTransactions(ledgersHeader.Transactions);

            await _ledgerService.UpdateHeader(ledgersHeader);
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
        Voucher = new();
        Voucher.Mode = "Cash";
        Voucher.VoucherDate = DateTime.Now;
        Voucher.TransDate = Voucher.VoucherDate;    // DateTime.Now;
        Voucher.TransType = "Receipt";
        Voucher.VoucherType = "Advance";
        //Voucher.CustomerGkey = Payer.GKey;
        Voucher.TransDesc = string.Empty;
    }

    [RelayCommand]
    private async Task SaveVoucherAsync()
    {
        //SetVoucherType();

        if (Voucher.GKey == 0)
        {
            var voucher = await _voucherService.CreatVoucher(Voucher);

            ProcessLedger(voucher);


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
