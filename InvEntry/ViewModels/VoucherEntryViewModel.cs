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
    private string _voucherMode;

    private bool createVoucher = false;

    //private readonly IReportFactoryService _reportFactoryService;
    private readonly IVoucherService _voucherService;
    private readonly IMessageBoxService _messageBoxService;

    public VoucherEntryViewModel(
            IVoucherService voucherService,
            IMessageBoxService messageBoxService)

    {
        CashVoucherTypeList = new();
        transactionTypeList = new();
        _voucherService = voucherService;
        _messageBoxService = messageBoxService;
        
        PopulateReferenceList();
        ResetVoucher();
        //Voucher.TransType = "Payment";
    }

    private  void PopulateReferenceList()
    {
        CashVoucherTypeList.Add("Cash");
        CashVoucherTypeList.Add("Petty Cash");

        TransactionTypeList.Add("Payment");
        TransactionTypeList.Add("Receipt");

    }

    private void SetVoucher(byte mode = 1)
    {
        Voucher.VoucherDate = DateTime.Now;
        Voucher.TransDate = Voucher.VoucherDate;    // DateTime.Now;
        SetVoucherMode();
        Voucher.Mode = VoucherMode;
        SetTransType();
        SetVoucherType();
    }

    private void SetVoucherMode()
    {
        VoucherMode = "Cash";

        if(Voucher.Mode != "1")
        {
            VoucherMode = "Petty Cash";
        }
    }

    [RelayCommand]
    private void ResetVoucher()
    {
        Voucher = new();
        SetVoucher();
    }

    [RelayCommand]
    private void CreateCashVoucher()
    {
        Voucher.Mode = "1";
        SetVoucherMode();
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
    private void CreatePettyCashVoucher()
    {
        Voucher.Mode = "2";
        SetVoucherMode();
        SetVoucherType();
    }

    [RelayCommand]
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
        }
        else
        {
            await _voucherService.UpdateVoucher(Voucher);
        }

        ResetVoucher();
    }

}
