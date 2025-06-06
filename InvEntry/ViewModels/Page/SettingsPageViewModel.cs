﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Utils.Html.Internal;
using DevExpress.Xpf.Core;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InvEntry.ViewModels
{
    public partial class SettingsPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<DailyRate> dailyMetalRate;

        [ObservableProperty]
        private ObservableCollection<DailyRate> todayDailyMetalRate;

        [ObservableProperty]
        private ObservableCollection<DailyRate> historyDailyMetalRate;

        [ObservableProperty]
        private DailyRate _Gold22C;

        [ObservableProperty]
        private DailyRate _Gold18KT;

        [ObservableProperty]
        private DailyRate _Silver;

        [ObservableProperty]
        private DailyRate _Diamond;

        private readonly IMijmsApiService _mijmsApiService;

        public SettingsPageViewModel(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
            _Gold22C = new DailyRate()
            {
                Metal = "GOLD",
                Purity = "916",
                Carat = "22 KT",
                EffectiveDate = DateTime.Now.Date,
                IsDisplay = true
            };

            _Gold18KT = new DailyRate()
            {
                Metal = "GOLD.18KT",
                Purity = "750",
                Carat = "18 KT",
                EffectiveDate = DateTime.Now.Date,
                IsDisplay = true
            };

            _Silver = new DailyRate()
            {
                Metal = "SILVER",
                Purity = "XX",
                Carat = null,
                EffectiveDate = DateTime.Now.Date,
                IsDisplay = true
            };

            _Diamond = new DailyRate()
            {
                Metal = "Diamond",
                Purity = "XX",
                Carat = null,
                EffectiveDate = DateTime.Now.Date,
                IsDisplay = true
            };
        }

        [RelayCommand]
        private async Task OnLoaded()
        {
            var vm = WaitIndicatorVM.ShowIndicator("Fetching Daily rate details...");
            Messenger.Default.Send(MessageType.WaitIndicator, vm);

            var dailyRates = await _mijmsApiService.GetEnumerable<DailyRate>("api/dailyrate/latest");

            if (dailyRates is null)
            {
                DailyMetalRate = new();
                TodayDailyMetalRate = new();
                HistoryDailyMetalRate = new();

            }
            else {
                DailyMetalRate = new(dailyRates);

                TodayDailyMetalRate = new(DailyMetalRate.Where(x => x.EffectiveDate.Date == DateTime.Now.Date));
                HistoryDailyMetalRate = new(DailyMetalRate.Where(x => x.EffectiveDate.Date != DateTime.Now.Date));

            }
            GenerateTodayRate();

            Messenger.Default.Send(MessageType.WaitIndicator, vm);
        }

        private async Task DailyRateUpdate()
        {
            foreach( var dailyRate in TodayDailyMetalRate )
            {
                await SaveDailyRate(dailyRate);
            }
        }

        [RelayCommand]
        private async Task SaveAllDailyRate()
        {
            if (!TodayDailyMetalRate.Where(x => x.GKey == 0).Any())
                 await DailyRateUpdate(); // return;

            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Saving..."));

            var savedRates = await _mijmsApiService.PostList<DailyRate>("api/dailyrate/save", TodayDailyMetalRate.Where(x => x.GKey == 0));

            if (savedRates is null) return;

            foreach(var savedRate in savedRates)
            {
                if(TodayDailyMetalRate.Any(x => IsSame(x, savedRate)))
                {
                    TodayDailyMetalRate.First(x => IsSame(x, savedRate)).GKey = savedRate.GKey;
                }
            }

            GenerateTodayRate();

            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

            DXMessageBox.Show("Sucessfully saved...", "Success", 
                                MessageBoxButton.OK, MessageBoxImage.Information, 
                                MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }

        [RelayCommand]
        private async Task SaveDailyRate(DailyRate rate)
        {
            if (rate is null) return;

            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.ShowIndicator("Saving..."));

            if (rate.GKey == 0)
            {
                var savedRate = await _mijmsApiService.Post("api/dailyrate/", rate);
                TodayDailyMetalRate.FirstOrDefault(x => IsSame(x, savedRate)).GKey = savedRate.GKey;
            }
            else
            {
                await _mijmsApiService.Put($"api/dailyrate/{rate.GKey}", rate);
            }

            GenerateTodayRate();

            Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

           // DXMessageBox.Show("Sucessfully saved rates", "Sucess", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }

        private void GenerateTodayRate()
        {
            if (!TodayDailyMetalRate.Any(x => IsSame(x, Gold22C)))
                TodayDailyMetalRate.Add(Gold22C);
            else
                Gold22C = TodayDailyMetalRate.First(x => IsSame(x, Gold22C));

            if (!TodayDailyMetalRate.Any(x => IsSame(x, Gold18KT)))
                TodayDailyMetalRate.Add(Gold18KT);
            else
                Gold18KT = TodayDailyMetalRate.First(x => IsSame(x, Gold18KT));

            if (!TodayDailyMetalRate.Any(x => IsSame(x, Silver)))
                TodayDailyMetalRate.Add(Silver);
            else
                Silver = TodayDailyMetalRate.First(x => IsSame(x, Silver));

            if (!TodayDailyMetalRate.Any(x => IsSame(x, Diamond)))
                TodayDailyMetalRate.Add(Diamond);
            else
                Diamond = TodayDailyMetalRate.First(x => IsSame(x, Diamond));
        }

        private bool IsSame(DailyRate x, DailyRate y)
            => x is not null
                && y is not null
                && x.Metal.Equals(y.Metal, StringComparison.OrdinalIgnoreCase)
                && ((x.Carat is null && y.Carat is null)  || x.Carat.Equals(y.Carat, StringComparison.OrdinalIgnoreCase))
                && x.Purity.Equals(y.Purity, StringComparison.OrdinalIgnoreCase);

        public bool IsAllPriceUpdated()
        {
            var date = DateTime.Now.Date;

            return Gold22C.EffectiveDate.Date == date && Gold22C.Price.HasValue
                    && Gold18KT.EffectiveDate.Date == date && Gold18KT.Price.HasValue
                    && Silver.EffectiveDate.Date == date && Silver.Price.HasValue
                    && Diamond.EffectiveDate.Date == date && Diamond.Price.HasValue;
        }

        public decimal? GetPrice(MetalType metalType)
        {
            return metalType switch
            {
                MetalType.Gold => Gold22C.Price,
                MetalType.Gold18KT => Gold18KT.Price,
                MetalType.Silver => Silver.Price,
                MetalType.Diamond => Diamond.Price,
                _ => 0M
            };
        }

        public decimal? GetPrice(string metalType)
        {
            return metalType switch
            {
                var s when s.Equals("GOLD", StringComparison.OrdinalIgnoreCase) => Gold22C.Price,
                var s when s.Equals("GOLD.18KT", StringComparison.OrdinalIgnoreCase) => Gold18KT.Price,
                var s when s.Equals("Silver", StringComparison.OrdinalIgnoreCase) => Silver.Price,
                var s when s.Equals("Diamond", StringComparison.OrdinalIgnoreCase) => Diamond.Price,
                _ => 0M
            };
        }
    }
}
