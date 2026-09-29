using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using InvEntry.ViewModels;

namespace InvEntry.Views
{
    /// <summary>
    /// Interaction logic for OrderView.xaml
    /// </summary>
    public partial class CustomerOrderView : UserControl
    {
        public CustomerOrderView()
        {
            InitializeComponent();
        }

        private async void SaveOrder_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (DataContext is not CustomerOrderViewModel viewModel ||
                !OrderLinesView.CommitEditing())
            {
                return;
            }

            if (!viewModel.CreateCustomerOrderCommand.CanExecute(null))
                return;

            await viewModel.CreateCustomerOrderCommand.ExecuteAsync(null);

            if (viewModel.HasValidationErrors)
            {
                FocusFirstInvalidOrderLine(viewModel);
            }
        }

        private void ResetCustomerOrder_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (DataContext is not CustomerOrderViewModel viewModel ||
                !viewModel.ConfirmResetCustomerOrder())
            {
                return;
            }

            OrderLinesView.HideEditor();
            OldMetalLinesView.HideEditor();
            AdvanceReceiptLinesView.HideEditor();

            orderLinesUIGrid.CurrentItem = null;
            viewModel.InitializeNewOrder();

            OrderWorkspaceTabs.SelectedTabIndex = 0;
            CustomerMobileNbr.Focus();
        }

        private void FocusFirstInvalidOrderLine(
            CustomerOrderViewModel viewModel)
        {
            var line = viewModel.FirstInvalidOrderLine;

            if (line is null)
                return;

            var lineIndex = viewModel.Header.Lines.IndexOf(line);

            if (lineIndex < 0)
                return;

            var rowHandle =
                orderLinesUIGrid.GetRowHandleByListIndex(lineIndex);

            orderLinesUIGrid.CurrentItem = line;
            OrderLinesView.FocusedRowHandle = rowHandle;
            OrderLinesView.ScrollIntoView(rowHandle);

            if (!string.IsNullOrWhiteSpace(
                    viewModel.FirstInvalidOrderLineFieldName))
            {
                orderLinesUIGrid.CurrentColumn =
                    orderLinesUIGrid.Columns.FirstOrDefault(x =>
                        x.FieldName ==
                        viewModel.FirstInvalidOrderLineFieldName);
            }

            orderLinesUIGrid.Focus();
            OrderLinesView.ShowEditor();
        }
    }
}
