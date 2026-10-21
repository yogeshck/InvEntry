using InvEntry.ViewModels;
using System.Windows;

namespace InvEntry.Views
{
    public partial class CompanyEditWindow : Window
    {
        public CompanyEditWindow(CompanyEditViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.SaveCompleted += (_, _) =>
            {
                DialogResult = true;
                Close();
            };
        }
    }
}
