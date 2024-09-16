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

        private readonly IMijmsApiService _mijmsApiService;

        public SettingsPageViewModel(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        private async Task OnLoaded()
        {
            var dialyRates = await _mijmsApiService.GetEnumerable<DailyRate>("api/dailyrate/latest");

            DailyMetalRate = new(dialyRates);
        }

        [RelayCommand]
        private void SaveDailyRate()
        {

        }
    }
}
