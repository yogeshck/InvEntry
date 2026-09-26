using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Services;
using System;
using System.Collections.ObjectModel;
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

        public IApplicationIdentityService Identity { get; }


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
            Dispatcher dispatcher,
            IApplicationIdentityService applicationIdentity)
        {
            NavigationService = navigationService;

            _settingsPageViewModel =
                settingsPageViewModel;

            _dispatcher =
                dispatcher;

            Identity = applicationIdentity;


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


        }


        // =========================================================
        // WINDOW LOADED
        // =========================================================

        [RelayCommand]
        private async Task OnLoaded()
        {
            while (true)
            {
                try
                {
                    await _settingsPageViewModel
                        .LoadedCommand
                        .ExecuteAsync(null);

                    break;
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(
                        ex,
                        "Unable to verify daily rates during application startup");

                    var retry = DXMessageBox.Show(
                        "Today's prices could not be verified because the server or database is unavailable.\n\n" +
                        "No Price Update page will be opened until the check succeeds.\n\n" +
                        "Select Yes to retry.",
                        "Price Check Failed",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Error);

                    if (retry != MessageBoxResult.Yes)
                        return;
                }
            }

            // Rates are calculated properties.
            // Notify the UI after Settings have been loaded.

            OnPropertyChanged(nameof(HeaderRates));


            // -----------------------------------------------------
            // Navigate only after the price check itself has succeeded.
            // -----------------------------------------------------

            if (_settingsPageViewModel.IsAllPriceUpdated())
            {
                NavigationService.Navigate(
                    "InvoiceEntryPage");

                return;
            }

            var missingPrices =
                _settingsPageViewModel
                    .GetMissingRequiredPriceNames();

            DXMessageBox.Show(
                "Enter today's required prices for: " +
                string.Join(", ", missingPrices),
                "Today's Prices Required",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            _settingsPageViewModel
                .NavigateToInvoiceWhenPricesComplete = true;

            NavigationService.Navigate(
                "SettingsPage");
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

               // GoBackCommand.NotifyCanExecuteChanged();
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
             
        }


        private bool CanGoBack()
        {
            return NavigationService?.CanGoBack == true;
        }

        // =========================================================
        // NAVIGATION COMPLETED
        // =========================================================

        [RelayCommand]
        private void NavigationCompleted()
        {
            GoBackCommand.NotifyCanExecuteChanged();
        }

        // =========================================================
        // WAIT INDICATOR
        // =========================================================

        private void SetWaitIndicator(WaitIndicatorVM vm)
        {
            if (vm is null)
                return;

            void UpdateIndicator()
            {
                WaitIndicatorContent = vm.Status;
                WaitIndicatorVisible = vm.IsVisible;
            }

            if (_dispatcher.CheckAccess())
            {
                UpdateIndicator();
            }
            else
            {
                _dispatcher.Invoke(UpdateIndicator);
            }
        }

    }


}
