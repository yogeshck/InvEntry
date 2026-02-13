using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DevExpress.Xpo.Helpers;
using InvEntry.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
  
using System.Windows.Threading;

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
        private Dispatcher Dispatcher;

        public MainWindowViewModel(INavigationService navigationService, 
            SettingsPageViewModel settingsPageViewModel,
            Dispatcher dispatcher)
        {
            _navigationService = navigationService;
            _settingsPageViewModel = settingsPageViewModel;
            Messenger.Default.Register<WaitIndicatorVM>(this, MessageType.WaitIndicator, SetWaitIndicator);

            Version = $"Version : {Assembly.GetEntryAssembly()!.GetName().Version}";
            Dispatcher = dispatcher;
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
            Dispatcher.Invoke(() =>
            {
                if (vm.IsVisible)
                    SplashScreenManager.CreateWaitIndicator(vm, topmost: true).Show(owner: Application.Current.MainWindow);
                else
                    SplashScreenManager.CloseAll();
            });

            WaitIndicatorContent = vm.Status;
            WaitIndicatorVisible = vm.IsVisible;
        }

    }
}
