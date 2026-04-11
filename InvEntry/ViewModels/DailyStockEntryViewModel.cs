using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.Editors.Helpers;
using DevExpress.Xpf.Grid;
using DevExpress.Xpo.DB;
using Ghostscript.NET.PDFA3Converter.ZUGFeRD;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Threading;

namespace InvEntry.ViewModels
{
    public partial class DailyStockEntryViewModel : ObservableObject
    {

        private readonly IDailyStockSummaryService _dailyStockSummaryService;
        private readonly IDialogService _reportDialogService;
        private readonly IProductCategoryService _productCategoryService;
        private readonly IMetalsService _metalsService;

        [ObservableProperty]
        private DateSearchOption _dateSearchOption;

        [ObservableProperty]
        private ObservableCollection<string> _productCategoryList;


        [ObservableProperty]
        private ObservableCollection<string> _metalsList;

        [ObservableProperty]
        private ObservableCollection<DailyStockSummary> _dailyStockSummaryList;

        [ObservableProperty]
        private DateTime _startDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Today;

        [ObservableProperty]
        private string _statusMessage;

        [ObservableProperty]
        public bool _isOpeningEditable = false;

        public bool CanGoNext => SelectedDate < DateTime.Today;
        public bool prevDate = false;
        public bool nextDate = false;

        public IEnumerable<string> AvailableProductCategories;
            
                //    ProductCategoryList.Except(DailyStockSummaryList.Select(x => x.ProductCategory));

        public DailyStockEntryViewModel(IDailyStockSummaryService dailyStockSummaryService,
                            IProductCategoryService productCategoryService,
                            IMetalsService metalsService,
                    [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
        {
            _dailyStockSummaryService = dailyStockSummaryService;
            _reportDialogService = reportDialogService;
            _productCategoryService = productCategoryService;
            _metalsService = metalsService;

            DailyStockSummaryList = new ObservableCollection<DailyStockSummary>();

            _dateSearchOption = new();
            DateSearchOption.From = SelectedDate;
            //DateSearchOption.To = StartDate.AddDays(-1);

            PopulateMetalsList();
            PopulateProductCategoryList();

            //populate product category
            //populate previous day data - closing balance as todays ob

            Task.Run(RefreshDailyStockSummary).Wait();
        }

        private async void PopulateProductCategoryList()
        {
            var list = await _productCategoryService.GetProductCategoryList();
            ProductCategoryList = new(list
                                    .Where(x => !x.Name.StartsWith("OLD"))
                                    .Select(x => x.Name));
        }

        private async void PopulateMetalsList()
        {
            var list = await _metalsService.GetMetalList();
            MetalsList = new(list .Select(x => x.MetalName));
        }

        [RelayCommand]
        private async Task RefreshDailyStockSummary()
        {

            try
            {
                DailyStockSummaryList.Clear();
                StatusMessage = "";

                // Fetch previous day’s closing
                if (prevDate)
                {
                    SelectedDate = SelectedDate.AddDays(-1);
                    prevDate = false;
                }
                else if (nextDate)
                {
                    SelectedDate = SelectedDate.AddDays(1);
                    nextDate = false;
                }

                DateSearchOption.From = SelectedDate;
                DateSearchOption.To = SelectedDate;
                var prevData = await _dailyStockSummaryService.GetAll(DateSearchOption);

                IsOpeningEditable = !(prevData != null && prevData.Any());

                if (prevData != null && prevData.Any())
                {
                    foreach (var item in prevData)
                    {
                        DailyStockSummaryList.Add(item);
                    }


                };

                //StatusMessage = "Data refreshed successfully.";

               // FilterOutCategories();

                OnPropertyChanged(nameof(CanGoNext)); // update button state
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Error at refresh..................");
                StatusMessage = $"Error refreshing data: {ex.Message}";

            }
        }

        private async Task<List<DailyStockSummary>> FetchDataAsync()
        {
            var fetchedData = await _dailyStockSummaryService.GetAll(DateSearchOption);
            // Return the list (could be empty if no records)
            return fetchedData?.ToList() ?? new List<DailyStockSummary>();

        }

        [RelayCommand]
        private async Task AddRowAsync()
        {

            // Check if there is at least one row
            if (DailyStockSummaryList.Any())
            {
                var lastRow = DailyStockSummaryList.Last();

                // Example validation: block if ProductCategory is empty or OpeningStockQty is 0
                if (string.IsNullOrWhiteSpace(lastRow.ProductCategory))
                {
                    StatusMessage = "Please complete the last row before adding a new one.";
                    return;
                }

                if (lastRow.OpeningStockQty.GetValueOrDefault() < 1 || lastRow.ClosingStockQty.GetValueOrDefault() < 1)
                {
                    StatusMessage = "Please enter values.... .";
                    return;
                }


                var newRow = new DailyStockSummary
                {
                    TransactionDate = SelectedDate,
                    Metal = "Gold", // default or user choice
                    ProductCategory = string.Empty,
                    IsObEditable = true
                };

                DailyStockSummaryList.Add(newRow);

                IsOpeningEditable = true;
            }
            else
            {

                DateSearchOption.From = SelectedDate.AddDays(-1);
                DateSearchOption.To = DateSearchOption.From;

                var prevDateDataList = await FetchDataAsync();

                if (prevDateDataList != null && prevDateDataList.Any())
                {
                    // Use previous closing as opening for selected date
                    foreach (var item in prevDateDataList)
                    {
                        DailyStockSummaryList.Add(new DailyStockSummary
                        {
                            Metal = item.Metal,
                            TransactionDate = SelectedDate,
                            ProductCategory = item.ProductCategory,
                            OpeningStockQty = item.ClosingStockQty,
                            OpeningStockGrossWeight = item.ClosingStockGrossWeight,
                            OpeningStockStoneWeight = item.ClosingStockStoneWeight,
                            OpeningStockNetWeight = item.ClosingStockNetWeight,
                            StockInGrossWeight = 0,
                            StockInStoneWeight = 0,
                            StockInNetWeight = 0,
                            StockOutGrossWeight = 0,
                            StockOutStoneWeight = 0,
                            StockOutNetWeight = 0,
                            IsObEditable = false,
                        });
                    }
                    ;

                }
                else
                {
                    var newRow = new DailyStockSummary
                    {
                        TransactionDate = SelectedDate,
                        Metal = "Gold", // default or user choice
                        ProductCategory = string.Empty,
                        IsObEditable = true
                    };

                    DailyStockSummaryList.Add(newRow);

                    IsOpeningEditable = true;
                }
            }

            FilterOutCategories();

        }

        private void FilterOutCategories()
        {
            AvailableProductCategories =
                        ProductCategoryList.Except(DailyStockSummaryList.Select(x => x.ProductCategory));
        }

        [RelayCommand]
        private void OnCellUpdate(CellValueChangedEventArgs e)
        {
            if (e.Row is DailyStockSummary row)
            {
                // Closing = Opening + In – Out
                row.ClosingStockQty = row.OpeningStockQty + row.StockInQty - row.StockOutQty;
                row.ClosingStockNetWeight = row.OpeningStockNetWeight + row.StockInNetWeight - row.StockOutNetWeight;
            }
        }

        [RelayCommand]
        private async Task RefreshPrevDate()
        {
            // SelectedDate = SelectedDate.AddDays(-1);
            prevDate = true;
            nextDate = false;
            await RefreshDailyStockSummary();
            OnPropertyChanged(nameof(CanGoNext)); // notify UI
        }

        [RelayCommand]
        private async Task RefreshNextDate()
        {

            if (CanGoNext)
            {
                // SelectedDate = SelectedDate.AddDays(1);
                nextDate = true;
                prevDate = false;
                await RefreshDailyStockSummary();
                OnPropertyChanged(nameof(CanGoNext)); // notify UI
            }
        }

        [RelayCommand]
        private void Save()
        {

            _dailyStockSummaryService.CreateDailyStockSummary(DailyStockSummaryList.ToList());
            //display save message
            DailyStockSummaryList.Clear();
        }

    }
}
