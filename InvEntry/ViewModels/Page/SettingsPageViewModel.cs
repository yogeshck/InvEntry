using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Utils.Html.Internal;
using InvEntry.Models;
using InvEntry.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                Carat = "22 C",
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
            var dialyRates = await _mijmsApiService.GetEnumerable<DailyRate>("api/dailyrate/latest");

            DailyMetalRate = new(dialyRates);

            TodayDailyMetalRate = new(DailyMetalRate.Where(x => x.EffectiveDate.Date == DateTime.Now.Date));
            HistoryDailyMetalRate = new(DailyMetalRate.Where(x => x.EffectiveDate.Date != DateTime.Now.Date));

            GenerateTodayRate();
        }

        [RelayCommand]
        private async Task SaveAllDailyRate()
        {
            if (!TodayDailyMetalRate.Any()) return;

            await _mijmsApiService.Post<IEnumerable<DailyRate>>("api/dailyrate/save", TodayDailyMetalRate);
        }

        [RelayCommand]
        private async Task SaveDailyRate(DailyRate rate)
        {
            if (rate is null) return;

            if (rate.GKey == 0)
            {
                var savedRate = await _mijmsApiService.Post("api/dailyrate/", rate);
                TodayDailyMetalRate.FirstOrDefault(x => IsSame(x, savedRate)).GKey = savedRate.GKey;
            }
            else
            {
                await _mijmsApiService.Put($"api/dailyrate/{rate.GKey}", rate);
            }
        }

        private void GenerateTodayRate()
        {
            if (!TodayDailyMetalRate.Any(x => IsSame(x, Gold22C)))
                TodayDailyMetalRate.Add(Gold22C);
            if (!TodayDailyMetalRate.Any(x => IsSame(x, Silver)))
                TodayDailyMetalRate.Add(Silver);
            if (!TodayDailyMetalRate.Any(x => IsSame(x, Diamond)))
                TodayDailyMetalRate.Add(Diamond);
        }

        private bool IsSame(DailyRate x, DailyRate y)
            => x is not null
                && y is not null
                && x.Metal.Equals(y.Metal, StringComparison.OrdinalIgnoreCase)
                && ((x.Carat is null && y.Carat is null)  || x.Carat.Equals(y.Carat, StringComparison.OrdinalIgnoreCase))
                && x.Purity.Equals(y.Purity, StringComparison.OrdinalIgnoreCase);
    }
}
