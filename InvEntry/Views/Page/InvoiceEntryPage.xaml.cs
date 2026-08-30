using DevExpress.Xpf.WindowsUI;
using InvEntry.ViewModels;
using System.Windows;

namespace InvEntry.Views.Page
{
    public partial class InvoiceEntryPage : NavigationPage
    {
        public InvoiceEntryPage()
        {
            InitializeComponent();

            Loaded += InvoiceEntryPage_Loaded;
        }

        private async void InvoiceEntryPage_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            if (InvoiceViewControl?.DataContext
                is not InvoiceViewModel viewModel)
            {
                return;
            }

            await viewModel.LoadPendingDraftAsync();
        }
    }
}