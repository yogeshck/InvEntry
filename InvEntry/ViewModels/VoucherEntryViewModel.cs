﻿using CommunityToolkit.Mvvm.ComponentModel;
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
    }

    private  void PopulateReferenceList()
    {
        CashVoucherTypeList.Add("Cash");
        CashVoucherTypeList.Add("Petty Cash");

        TransactionTypeList.Add("Payment");
        TransactionTypeList.Add("Receipt");

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
    }

    [RelayCommand]
    private void CreateCashVoucher()
    {
        Voucher.Mode = "Petty Cash";
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
    private void CreatePettyCashVoucher()
    {
        Voucher.Mode = "Cash";
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
