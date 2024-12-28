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

namespace InvEntry.ViewModels
{
    public partial class DailyStockSummaryListViewModel : ObservableObject
    {
        private readonly IDailyStockSummaryService _dailyStockSummaryService;
        private readonly IDialogService _reportDialogService;

        [ObservableProperty]
        private ObservableCollection<RepDailyStockSummary> _dailyStockStockSummary;

        /*    [ObservableProperty]
            private DateSearchOption _searchOption;

            [ObservableProperty]
            private InvoiceHeader _SelectedInvoice;

            [ObservableProperty]
            private DateTime _Today = DateTime.Today;*/

        public DailyStockSummaryListViewModel(IDailyStockSummaryService dailyStockSummaryService,
                                                [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
        {
            _dailyStockSummaryService = dailyStockSummaryService;
            _reportDialogService = reportDialogService;

            Task.Run(RefreshDailyStockSummaryAsync).Wait();
        }

        [RelayCommand]
        private async Task RefreshDailyStockSummaryAsync()
        {
            var dailyStockSummaryResult = await _dailyStockSummaryService.GetAll();
            if (dailyStockSummaryResult is not null)
                DailyStockStockSummary = new(dailyStockSummaryResult);
        }
    }

}
