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
    private ObservableCollection<string> productCategoryList;

    private bool createVoucher = false;

    //private readonly IReportFactoryService _reportFactoryService;
    private readonly IFinDayBookService _finDayBookService;
    private readonly IMessageBoxService _messageBoxService;
    private SettingsPageViewModel _settingsPageViewModel;

    public VoucherEntryViewModel(
            IFinDayBookService finDayBookService,
            IMessageBoxService messageBoxService)

    {
        Voucher = new();
        Voucher.VoucherDate = DateTime.Now;
        SetVoucher();
        _finDayBookService = finDayBookService;
        _messageBoxService = messageBoxService;

        PopulateReferenceList();
    }

    private  void PopulateReferenceList()
    {
        return; 
      //  var list = await _referenceService.GetProductCategoryList();
      //  ProductCategoryList = new(list.Select(x => x.Name));
    }

    private void SetVoucher()
    {
        Voucher.SeqNbr = 1;
        Voucher.CustomerGkey = 100;
        Voucher.VoucherDate = DateTime.Now;
        Voucher.TransType = 2;         // Trans_type    1 = Receipt,    2 = Payment,    3 = Journal
        Voucher.VoucherType = 3;       // Voucher_type  1 = Sales,      2 = Credit,     3 = Expense
        Voucher.Mode = 1;              // Mode          1 = Cash,       2 = Bank,       3 = Credit
        Voucher.TransDate = Voucher.VoucherDate;    // DateTime.Now;

        return;

    }

    [RelayCommand]
    private void ResetVoucher()
    {

        Voucher = new()
        {
            VoucherDate = DateTime.Now
        };

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
