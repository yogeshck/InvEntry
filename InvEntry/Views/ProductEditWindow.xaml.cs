using InvEntry.ViewModels;
using System.Windows;

namespace InvEntry.Views
{
    public partial class ProductEditWindow : Window
    {
        public ProductEditWindow(ProductEditViewModel viewModel)
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
