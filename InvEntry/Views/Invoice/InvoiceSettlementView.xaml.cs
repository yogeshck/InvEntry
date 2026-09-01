using DevExpress.Xpf.Core;
using InvEntry.ViewModels.Invoices;
using System.Windows;

namespace InvEntry.Views.Invoice
{
    /// <summary>
    /// Interaction logic for InvoiceSettlementView.xaml
    /// </summary>
    public partial class InvoiceSettlementView : ThemedWindow
    {
        public InvoiceSettlementView()
        {
            InitializeComponent();
        }

        public InvoiceSettlementView(
        InvoiceSettlementViewModel viewModel)
        : this()
        {
            DataContext = viewModel;
        }

        private void ConfirmSettlement_Click(
        object sender,
        RoutedEventArgs e)
        {
            if (DataContext is not InvoiceSettlementViewModel vm)
                return;

            if (!vm.CanFinalise)
                return;

            DialogResult = true;
        }

    }
}







