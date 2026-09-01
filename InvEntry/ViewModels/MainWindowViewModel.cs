using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using InvEntry.Extension;
using InvEntry.Models;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;
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
            // Always establish the application's normal root page.
            // -----------------------------------------------------

            NavigationService.Navigate(
                "InvoiceEntryPage");


            // -----------------------------------------------------
            // If today's rates have not been entered,
            // navigate to Settings on top of Invoice Entry.
            //
            // This preserves Invoice Entry in navigation history,
            // allowing the user to press Back after entering rates.
            // -----------------------------------------------------

            if (!_settingsPageViewModel.IsAllPriceUpdated())
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