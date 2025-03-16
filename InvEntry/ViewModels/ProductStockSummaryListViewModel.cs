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

public partial class ProductStockSummaryListViewModel : ObservableObject
{
    private readonly IProductStockSummaryService _pStockSummaryService;
    private readonly IDialogService _reportDialogService;

    [ObservableProperty]
    private ObservableCollection<ProductStockSummary> _pStockSummary;

    /*    [ObservableProperty]
        private DateSearchOption _searchOption;

        [ObservableProperty]
        private InvoiceHeader _SelectedInvoice;

        [ObservableProperty]
        private DateTime _Today = DateTime.Today;*/

    public ProductStockSummaryListViewModel(IProductStockSummaryService pStockSummaryService,
            [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
    {
        _pStockSummaryService = pStockSummaryService;
        _reportDialogService = reportDialogService;

        PStockSummary = null;
        Task.Run(RefreshStockSummaryAsync).Wait();
    }

    [RelayCommand]
    private async Task RefreshStockSummaryAsync()
    {
        var pStockSummaryResult = await _pStockSummaryService.GetAll();
        if (pStockSummaryResult is not null)
            PStockSummary = null;
            PStockSummary = new(pStockSummaryResult);
    }

}
