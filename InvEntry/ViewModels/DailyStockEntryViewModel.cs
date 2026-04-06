using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.Grid;
using Ghostscript.NET.PDFA3Converter.ZUGFeRD;
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
    public partial class DailyStockEntryViewModel : ObservableObject
    {

        private readonly IDailyStockSummaryService _dailyStockSummaryService;
        private readonly IDialogService _reportDialogService;

        [ObservableProperty]
        private DateSearchOption _dateSearchOption;

        [ObservableProperty]
        private ObservableCollection<DailyStockSummary> _dailyStockSummaryList;

        [ObservableProperty]
        private DateTime _startDate = DateTime.Today;

        public DailyStockEntryViewModel(IDailyStockSummaryService dailyStockSummaryService,
                                                [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
        {
            _dailyStockSummaryService = dailyStockSummaryService;
            _reportDialogService = reportDialogService;

            _dateSearchOption = new();
            DateSearchOption.From = StartDate.AddDays(-1);
            DateSearchOption.To = StartDate.AddDays(-1);

            //populate product category
            //populate previous day data - closing balance as todays ob

            Task.Run(RefreshDailyStockSummaryAsync).Wait();
        }


        [RelayCommand]
        private async Task RefreshDailyStockSummaryAsync()
        {

            var prevDaysStockSummaryList = await _dailyStockSummaryService.GetAll(DateSearchOption);

            if (prevDaysStockSummaryList is not null)
                DailyStockSummaryList = null;
                DailyStockSummaryList = new(prevDaysStockSummaryList);


        }
    }
}
