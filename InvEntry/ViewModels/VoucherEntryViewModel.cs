using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.WindowsUI.Navigation;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Reports;
using InvEntry.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

public partial class VoucherEntryViewModel: ObservableObject
{
    [ObservableProperty]
    private Voucher _voucher;

    [ObservableProperty]
    private ObservableCollection<string> cashVoucherTypeList;

    [ObservableProperty]
    private ObservableCollection<string> transactionTypeList;

    [ObservableProperty]
    private ObservableCollection<string> accountGroupList;

    [ObservableProperty]
    private string _voucherTransDesc;

    private bool createVoucher = false;

    //private readonly IReportFactoryService _reportFactoryService;
    private readonly IVoucherService _voucherService;
    private readonly IMtblLedgersService _mtblLedgersService;
    private readonly IMessageBoxService _messageBoxService;

    public VoucherEntryViewModel(
            IVoucherService voucherService,
            IMtblLedgersService mtblLedgersService,
            IMessageBoxService messageBoxService)
    {
        CashVoucherTypeList = new();
        transactionTypeList = new();

        _voucherService = voucherService;
        _mtblLedgersService = mtblLedgersService;
        _messageBoxService = messageBoxService;
        
        PopulateReferenceList();
        PopulateAccountrGroupList();
        ResetVoucher();
    }

    private  void PopulateReferenceList()
    {
        CashVoucherTypeList.Add("Cash");
        CashVoucherTypeList.Add("Petty Cash");

        TransactionTypeList.Add("Payment");
        TransactionTypeList.Add("Receipt");

    }

    private async void PopulateAccountrGroupList()
    {
        var list = await _mtblLedgersService.GetLedgerList("Indirect Expenses");
        AccountGroupList = new(list.Select(x => x.LedgerName));
    }


    private void SetVoucher()
    {
        Voucher.VoucherDate = DateTime.Now;
        Voucher.TransDate = Voucher.VoucherDate;    // DateTime.Now;
        SetVoucherType();
    }

    [RelayCommand]
    private void ResetVoucher()
    {
        Voucher = new();
        Voucher.Mode = "Petty Cash";
        SetVoucher();
        VoucherTransDesc = string.Empty;
    }

    [RelayCommand]
    private void CreateCashVoucher()
    {
        Voucher.Mode = "Cash";
        SetVoucherType();
    }

    private void SetTransType()
    {
        if (Voucher.TransType is null)
        {
            Voucher.TransType = "Payment";
        }
    }

    private void SetVoucherType()
    {
        SetTransType();

        if (Voucher.TransType == "Payment")
        {
            Voucher.VoucherType = "Expense";
        }
        else
        {
            Voucher.VoucherType = "Receipt";
        }
    }

    [RelayCommand]
    private void VoucherPrint()
    {

    }


    [RelayCommand]
    private void CreatePettyCashVoucher()
    {
        Voucher.Mode = "Petty Cash";
        SetVoucherType();
    }

    partial void OnVoucherTransDescChanged(string value)
    {
        Voucher.TransDesc = value;
        SaveVoucherCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanSaveVoucher))]
    private async Task SaveVoucher()
    {
        SetVoucherType();

        if (Voucher.GKey == 0)
        {
            var voucher = await _voucherService.CreatVoucher(Voucher);

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

    private bool CanSaveVoucher()
    {
        return !string.IsNullOrEmpty(Voucher.TransDesc);
    }
}
