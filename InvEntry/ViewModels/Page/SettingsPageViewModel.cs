using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        private readonly IMijmsApiService _mijmsApiService;

        public SettingsPageViewModel(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        [RelayCommand]
        private async Task OnLoaded()
        {
            var dialyRates = await _mijmsApiService.GetEnumerable<DailyRate>("api/dailyrate/latest");

            DailyMetalRate = new(dialyRates);

            TodayDailyMetalRate = new(DailyMetalRate.Where(x => x.EffectiveDate.Date == DateTime.Now.Date));
            HistoryDailyMetalRate = new(DailyMetalRate.Where(x => x.EffectiveDate.Date != DateTime.Now.Date));
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

            await _mijmsApiService.Post<DailyRate>("api/dailyrate/save", rate);
        }

        private ObservableCollection<DailyRate> GenerateTodayRate()
        {
            ObservableCollection<DailyRate> rates = new();

            rates.Add(new DailyRate()
            {
                
            });

            return rates;
        }
    }
}
