using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using InvEntry.Extension;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace InvEntry.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly SettingsPageViewModel _settingsPageViewModel;
        private readonly Dispatcher _dispatcher;


        // =========================================================
        // NAVIGATION
        // =========================================================

        [ObservableProperty]
        private INavigationService navigationService;


        // =========================================================
        // WAIT INDICATOR
        // =========================================================

        [ObservableProperty]
        private bool waitIndicatorVisible;

        [ObservableProperty]
        private string? waitIndicatorContent;


        // =========================================================
        // APPLICATION INFORMATION
        // =========================================================

        [ObservableProperty]
        private string version = string.Empty;


        // =========================================================
        // CURRENT RATES
        // =========================================================

        public decimal? GoldRate =>
            _settingsPageViewModel.Gold22C?.Price;

        public decimal? SilverRate =>
            _settingsPageViewModel.Silver?.Price;

        public decimal? DiamondRate =>
            _settingsPageViewModel.Diamond?.Price;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public MainWindowViewModel(
            INavigationService navigationService,
            SettingsPageViewModel settingsPageViewModel,
            Dispatcher dispatcher)
        {
            NavigationService = navigationService;

            _settingsPageViewModel =
                settingsPageViewModel;

            _dispatcher =
                dispatcher;


            Messenger.Default.Register<WaitIndicatorVM>(
                this,
                MessageType.WaitIndicator,
                SetWaitIndicator);


            Version =
                $"Version : " +
                $"{Assembly.GetEntryAssembly()!.GetName().Version}";
        }


        // =========================================================
        // WINDOW LOADED
        // =========================================================

        [RelayCommand]
        private async Task OnLoaded()
        {
            await _settingsPageViewModel
                .LoadedCommand
                .ExecuteAsync(null);


            // Rates are calculated properties.
            // Notify the UI after Settings have been loaded.

            OnPropertyChanged(nameof(GoldRate));
            OnPropertyChanged(nameof(SilverRate));
            OnPropertyChanged(nameof(DiamondRate));


            // -----------------------------------------------------
            // Startup Navigation
            // -----------------------------------------------------

            if (_settingsPageViewModel.IsAllPriceUpdated())
            {
                NavigationService.Navigate(
                    "InvoiceEntryPage");
            }
            else
            {
                NavigationService.Navigate(
                    "SettingsPage");
            }
        }


        // =========================================================
        // BACK NAVIGATION
        // =========================================================

        [RelayCommand]
        private void GoBack()
        {
            if (NavigationService?.CanGoBack == true)
            {
                NavigationService.GoBack();
            }
        }


        // =========================================================
        // WAIT INDICATOR
        // =========================================================

        private void SetWaitIndicator(
            WaitIndicatorVM vm)
        {
            _dispatcher.Invoke(() =>
            {
                if (vm.IsVisible)
                {
                    SplashScreenManager
                        .CreateWaitIndicator(
                            vm,
                            topmost: true)
                        .Show(
                            owner:
                            Application.Current.MainWindow);
                }
                else
                {
                    SplashScreenManager.CloseAll();
                }
            });


            WaitIndicatorContent =
                vm.Status;

            WaitIndicatorVisible =
                vm.IsVisible;
        }
    }
}