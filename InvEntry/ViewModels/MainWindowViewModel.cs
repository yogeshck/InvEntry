using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using InvEntry.Extension;
using InvEntry.Models;
using System.Collections.ObjectModel;
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

        public ObservableCollection<DailyRate> HeaderRates
            => _settingsPageViewModel.TodayDailyMetalRate;


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


            // -----------------------------------------------------
            // Wait Indicator Messages
            // -----------------------------------------------------

            Messenger.Default.Register<WaitIndicatorVM>(
                this,
                MessageType.WaitIndicator,
                SetWaitIndicator);


            // -----------------------------------------------------
            // Application Navigation Messages
            //
            // Child ViewModels must NOT inject/use their own
            // INavigationService because that service may not be
            // attached to the MainWindow NavigationFrame.
            //
            // MainWindow owns the actual FrameNavigationService.
            // -----------------------------------------------------

            Messenger.Default.Register<string>(
                this,
                "NavigateToPage",
                NavigateToPage);


            // -----------------------------------------------------
            // Application Version
            // -----------------------------------------------------

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

            OnPropertyChanged(nameof(HeaderRates));


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
        // APPLICATION NAVIGATION
        // =========================================================

        private void NavigateToPage(
            string pageName)
        {
            if (string.IsNullOrWhiteSpace(pageName))
                return;

            _dispatcher.Invoke(() =>
            {
                NavigationService.Navigate(
                    pageName);

                GoBackCommand.NotifyCanExecuteChanged();
            });
        }


        // =========================================================
        // BACK NAVIGATION
        // =========================================================

        [RelayCommand(CanExecute = nameof(CanGoBack))]
        private void GoBack()
        {
            if (NavigationService?.CanGoBack == true)
            {
                NavigationService.GoBack();
            }

            GoBackCommand.NotifyCanExecuteChanged();
        }


        private bool CanGoBack()
        {
            return NavigationService?.CanGoBack == true;
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