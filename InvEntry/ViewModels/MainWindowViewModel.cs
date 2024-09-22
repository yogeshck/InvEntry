using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpo.Helpers;
using InvEntry.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InvEntry.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private INavigationService _navigationService;

        [ObservableProperty]
        private bool _WaitIndicatorVisible;

        [ObservableProperty]
        private string _WaitIndicatorContent;

        [ObservableProperty]
        private string _Version;

        public decimal? GoldRate
            => _settingsPageViewModel?.Gold22C?.Price;

        public decimal? SilverRate
            => _settingsPageViewModel?.Silver?.Price;

        public decimal? DiamondRate
            => _settingsPageViewModel?.Diamond?.Price;

        private SettingsPageViewModel _settingsPageViewModel;

        public MainWindowViewModel(INavigationService navigationService, SettingsPageViewModel settingsPageViewModel)
        {
            _navigationService = navigationService;
            _settingsPageViewModel = settingsPageViewModel;
            Messenger.Default.Register<WaitIndicatorVM>(this, MessageType.WaitIndicator, SetWaitIndicator);

            Version = "Version : 1.0.0.0";
        }

        [RelayCommand]
        private async Task OnLoaded()
        {
            await _settingsPageViewModel.LoadedCommand.ExecuteAsync(null);

            if(_settingsPageViewModel.IsAllPriceUpdated())
                NavigationService.Navigate("InvoiceEntryPage");
            else
                NavigationService.Navigate("SettingsPage");
        }

        private void SetWaitIndicator(WaitIndicatorVM vm)
        {
            WaitIndicatorContent = vm.Content;
            WaitIndicatorVisible = vm.IsVisible;
        }

    }
}
