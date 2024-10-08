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
    private string _voucherMode;

    private bool createVoucher = false;

    //private readonly IReportFactoryService _reportFactoryService;
    private readonly IFinDayBookService _finDayBookService;
    private readonly IMessageBoxService _messageBoxService;

    public VoucherEntryViewModel(
            IFinDayBookService finDayBookService,
            IMessageBoxService messageBoxService)

    {
        CashVoucherTypeList = new();
        _finDayBookService = finDayBookService;
        _messageBoxService = messageBoxService;
        
        PopulateReferenceList();
        ResetVoucher();
    }

    private  void PopulateReferenceList()
    {
        CashVoucherTypeList.Add("Cash");
        CashVoucherTypeList.Add("Petty Cash");
    }

    private void SetVoucher(byte mode = 1)
    {
        Voucher.VoucherDate = DateTime.Now;
        Voucher.TransType = 2;         // Trans_type    1 = Receipt,    2 = Payment,    3 = Journal
        Voucher.VoucherType = 3;       // Voucher_type  1 = Sales,      2 = Credit,     3 = Expense
        Voucher.Mode = mode;           // Mode          1 = Cash,       2 = PettyCash,  3 = Bank,       4 = Credit
        Voucher.TransDate = Voucher.VoucherDate;    // DateTime.Now;
        SetVoucherMode();
    }

    private void SetVoucherMode()
    {
        VoucherMode = "Cash";

        if(Voucher.Mode != 1)
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
        Voucher.Mode = 1;
        SetVoucherMode();
    }        


    [RelayCommand]
    private void CreatePettyCashVoucher()
    {
        Voucher.Mode = 2;
        SetVoucherMode();
    }

    [RelayCommand]
    private async Task SaveVoucher()
    {

        if (Voucher.GKey == 0)
        {
            var voucher = await _finDayBookService.CreatVoucher(Voucher);

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
            await _finDayBookService.UpdateVoucher(Voucher);
        }

        ResetVoucher();
    }

}
