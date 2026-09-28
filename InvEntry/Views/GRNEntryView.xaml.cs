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
    /// Interaction logic for GRNEntryView.xaml
    /// </summary>
    public partial class GRNEntryView : UserControl
    {
        public static readonly RoutedCommand SaveWithGridValidationCommand = new();

        public GRNEntryView()
        {
            InitializeComponent();
            DataContextChanged += GRNEntryView_DataContextChanged;
        }

        private void GRNEntryView_DataContextChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is GRNViewModel oldViewModel)
                oldViewModel.SubmitCommand.CanExecuteChanged -= SubmitCommand_CanExecuteChanged;

            if (e.NewValue is GRNViewModel newViewModel)
                newViewModel.SubmitCommand.CanExecuteChanged += SubmitCommand_CanExecuteChanged;

            CommandManager.InvalidateRequerySuggested();
        }

        private static void SubmitCommand_CanExecuteChanged(object? sender, EventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        private void SaveWithGridValidation_CanExecute(
            object sender,
            CanExecuteRoutedEventArgs e)
        {
            e.CanExecute =
                DataContext is GRNViewModel viewModel &&
                viewModel.SubmitCommand.CanExecute(null);
        }

        private void SaveWithGridValidation_Executed(
            object sender,
            ExecutedRoutedEventArgs e)
        {
            if (!GrnLinesView.CommitEditing())
                return;

            if (DataContext is GRNViewModel viewModel &&
                viewModel.SubmitCommand.CanExecute(null))
            {
                viewModel.SubmitCommand.Execute(null);
            }
        }
    }
}
