using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Xpf.WindowsUI.Navigation;
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
    private SettingsPageViewModel _settingsPageViewModel;


    public VoucherEntryViewModel(
    IFinDayBookService finDayBookService)

    {
        Voucher = new();

        SetVoucher();
        _finDayBookService = finDayBookService;

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
        Voucher.TransType = 2;         // Trans_type    1 = Receipt,    2 = Payment,    3 = Journal
        Voucher.VoucherType = 3;       // Voucher_type  1 = Sales,      2 = Credit,     3 = Expense
        Voucher.Mode = 1;              // Mode          1 = Cash,       2 = Bank,       3 = Credit
        Voucher.TransDate = Voucher.VoucherDate;    // DateTime.Now;

        return;
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
            }
        }
        else
        {
            await _finDayBookService.UpdateVoucher(Voucher);
        }

    }

}
