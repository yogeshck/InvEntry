using CommunityToolkit.Mvvm.ComponentModel;
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
using System.Windows.Forms;

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

    [ObservableProperty]
    private DateTime _Today = DateTime.Today;

    public InvoiceListViewModel(IInvoiceService invoiceService) 
    {
        _invoiceService = invoiceService;

        To = Today;
        From = Today.AddDays(-7);
        Task.Run(RefreshInvoicesAsync).Wait();
    }

    [RelayCommand]
    private async Task RefreshInvoicesAsync()
    {
        var invoicesResult = await _invoiceService.GetAll(From ?? DateTime.Today.AddDays(-7), To ?? DateTime.Today);
        if(invoicesResult is not null)
        Invoices = new(invoicesResult);
    }
}
