using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils;
using InvEntry.Utils.Options;
using InvEntry.Views;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IDialogService _reportDialogService;

    [ObservableProperty]
    private ObservableCollection<InvoiceHeader> _invoices;

    [ObservableProperty]
    private InvoiceSearchOption _searchOption;

    [ObservableProperty]
    private InvoiceHeader _SelectedInvoice;

    [ObservableProperty]
    private DateTime _Today = DateTime.Today;

    public InvoiceListViewModel(IInvoiceService invoiceService, 
        [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService) 
    {
        _invoiceService = invoiceService;
        _reportDialogService = reportDialogService;
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

    [RelayCommand(CanExecute = nameof(CanPrintInvoice))]
    private void PrintInvoice()
    {
        _reportDialogService.PrintPreview(SelectedInvoice.InvNbr);
    }

    private bool CanPrintInvoice()
    {
        return SelectedInvoice is not null;
    }

    [RelayCommand]
    private void SelectionChanged() 
    {
        PrintInvoiceCommand.NotifyCanExecuteChanged();
    }
}
