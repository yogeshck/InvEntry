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

            if (!TryCommitSettlementGridEdit())
                return;

            if (!vm.ValidateForFinalise())
            {
                FocusIncompleteLine(vm);
                return;
            }

            DialogResult = true;
        }

        private void AddPayment_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (DataContext is not InvoiceSettlementViewModel vm ||
                !ReceiptTableView.CommitEditing())
            {
                return;
            }

            vm.AddReceiptCommand.Execute(null);

            if (vm.ReceiptRequiringCompletion is not null)
            {
                ReceiptGrid.CurrentItem = vm.ReceiptRequiringCompletion;
                ReceiptGrid.Focus();
            }
        }

        private void AddRefund_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (DataContext is not InvoiceSettlementViewModel vm ||
                !RefundTableView.CommitEditing())
            {
                return;
            }

            vm.AddRefundCommand.Execute(null);

            if (vm.RefundRequiringCompletion is not null)
            {
                RefundGrid.CurrentItem = vm.RefundRequiringCompletion;
                RefundGrid.Focus();
            }
        }

        private bool TryCommitSettlementGridEdit()
        {
            return ReceiptTableView.CommitEditing() &&
                   RefundTableView.CommitEditing();
        }

        private void FocusIncompleteLine(
            InvoiceSettlementViewModel vm)
        {
            if (vm.ReceiptRequiringCompletion is not null)
            {
                ReceiptGrid.CurrentItem = vm.ReceiptRequiringCompletion;
                ReceiptGrid.Focus();
            }
            else if (vm.RefundRequiringCompletion is not null)
            {
                RefundGrid.CurrentItem = vm.RefundRequiringCompletion;
                RefundGrid.Focus();
            }
        }

    }
}







