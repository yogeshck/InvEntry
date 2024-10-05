using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils;
using InvEntry.Utils.Options;
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
    private InvoiceSearchOption _searchOption;

    [ObservableProperty]
    private DateTime _Today = DateTime.Today;

    public InvoiceListViewModel(IInvoiceService invoiceService) 
    {
        _invoiceService = invoiceService;
        _searchOption = new();
        SearchOption.To = Today;
        SearchOption.From = Today.AddDays(-7);
        Task.Run(RefreshInvoicesAsync).Wait();
    }

    [RelayCommand]
    private async Task RefreshInvoicesAsync()
    {
        var invoicesResult = await _invoiceService.GetAll(SearchOption);
        if(invoicesResult is not null)
        Invoices = new(invoicesResult);
    }
}
