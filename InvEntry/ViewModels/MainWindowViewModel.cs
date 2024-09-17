using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using InvEntry.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private decimal? goldRate;

        [ObservableProperty]
        private decimal? silverRate;

        [ObservableProperty]
        private decimal? diamondRate;

        public MainWindowViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            Messenger.Default.Register<WaitIndicatorVM>(this, MessageType.WaitIndicator, SetWaitIndicator);
        }

        [RelayCommand]
        private void OnLoaded()
        {
            NavigationService.Navigate("InvoiceEntryPage");
        }

        private void SetWaitIndicator(WaitIndicatorVM vm)
        {
            WaitIndicatorContent = vm.Content;
            WaitIndicatorVisible = vm.IsVisible;
        }

    }
}
