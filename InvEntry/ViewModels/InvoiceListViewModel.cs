﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

public partial class InvoiceListViewModel : ObservableObject
{
    private readonly IInvoiceService _invoiceService;

    [ObservableProperty]
    private ObservableCollection<InvoiceHeader> _invoices;

    [ObservableProperty]
    private DateTime? _from;

    [ObservableProperty]
    private DateTime? _to;

    public InvoiceListViewModel(IInvoiceService invoiceService) 
    {
        _invoiceService = invoiceService;
        Init();
    }

    private async void Init()
    {
        var invoicesResult = await _invoiceService.GetAll(DateTime.Now.AddDays(-7), DateTime.Now);
        Invoices = new(invoicesResult);
    }

    [RelayCommand]
    private async Task RefreshInvoicesAsync()
    {
        var invoicesResult = await _invoiceService.GetAll(From ?? DateTime.Now.AddDays(-7), To ?? DateTime.Now);
        Invoices = new(invoicesResult);
    }
}
