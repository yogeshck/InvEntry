using DevExpress.Xpf.Docking;
using InvEntry.ViewModels;
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

namespace InvEntry.Views
{
    /// <summary>
    /// Interaction logic for InvoiceView.xaml
    /// </summary>
    public partial class InvoiceView : UserControl
    {
        public static readonly RoutedCommand SaveDraftWithGridValidationCommand = new();
        public static readonly RoutedCommand FinaliseWithGridValidationCommand = new();

        public InvoiceView()
        {
            InitializeComponent();
        }

        private void SaveDraftWithGridValidation_CanExecute(
            object sender,
            CanExecuteRoutedEventArgs e)
        {
            e.CanExecute =
                DataContext is InvoiceViewModel viewModel &&
                viewModel.SaveDraftInvoiceCommand.CanExecute(null);
        }

        private void SaveDraftWithGridValidation_Executed(
            object sender,
            ExecutedRoutedEventArgs e)
        {
            if (!TryCommitInvoiceGridEdit())
                return;

            if (DataContext is InvoiceViewModel viewModel &&
                viewModel.SaveDraftInvoiceCommand.CanExecute(null))
            {
                viewModel.SaveDraftInvoiceCommand.Execute(null);
            }
        }

        private void FinaliseWithGridValidation_CanExecute(
            object sender,
            CanExecuteRoutedEventArgs e)
        {
            e.CanExecute =
                DataContext is InvoiceViewModel viewModel &&
                viewModel.FinaliseInvoiceCommand.CanExecute(null);
        }

        private void FinaliseWithGridValidation_Executed(
            object sender,
            ExecutedRoutedEventArgs e)
        {
            if (!TryCommitInvoiceGridEdit())
                return;

            if (DataContext is InvoiceViewModel viewModel &&
                viewModel.FinaliseInvoiceCommand.CanExecute(null))
            {
                viewModel.FinaliseInvoiceCommand.Execute(null);
            }
        }

        private bool TryCommitInvoiceGridEdit()
        {
            // DevExpress retains a rejected editor value separately from the
            // bound row. CommitEditing re-runs ValidateCell and returns false
            // without closing the editor when that value cannot be posted.
            return InvoiceLinesView.CommitEditing();
        }
    }
}
