﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

public partial class VoucherListViewModel: ObservableObject
{
    private readonly IVoucherService _voucherService;
    private readonly IDialogService _reportDialogService;

    [ObservableProperty]
    private ObservableCollection<Voucher> _vouchers;

    [ObservableProperty]
    private VoucherSearchOption _searchOption;

    [ObservableProperty]
    private Voucher _SelectedVoucher;

    [ObservableProperty]
    private DateTime _Today = DateTime.Today;

    public VoucherListViewModel(IVoucherService voucherService,
        [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
    {
        _voucherService = voucherService;
        _reportDialogService = reportDialogService;
        _searchOption = new();
        SearchOption.To = Today;
        SearchOption.From = Today.AddDays(-7);
        Task.Run(RefreshVoucherAsync).Wait();
    }

    [RelayCommand]
    private async Task RefreshVoucherAsync()
    {
        var vouchersResult = await _voucherService.GetAll(SearchOption);
        if (vouchersResult is not null)
            Vouchers = new(vouchersResult);
    }

    /*    [RelayCommand(CanExecute = nameof(CanPrintInvoice))]
        private void PrintInvoice()
        {
            _reportDialogService.PrintPreview(SelectedVoucher.??);
        }

        private bool CanPrintInvoice()
        {
            return SelectedVoucher is not null;
        }*/

}
