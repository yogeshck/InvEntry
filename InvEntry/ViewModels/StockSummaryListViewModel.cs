using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.Grid;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

public partial class StockSummaryListViewModel : ObservableObject
{
    private readonly IInvoiceService _invoiceService;
    private readonly IDialogService _reportDialogService;

    [ObservableProperty]
    private ObservableCollection<InvoiceHeader> _invoices;

    [ObservableProperty]
    private DateSearchOption _searchOption;

    [ObservableProperty]
    private InvoiceHeader _SelectedInvoice;

    [ObservableProperty]
    private DateTime _Today = DateTime.Today;

}
