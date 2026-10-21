using InvEntry.ViewModels;
using System.Windows;

namespace InvEntry.Views
{
    public partial class VoucherTypeEditWindow : Window
    {
        public VoucherTypeEditWindow(VoucherTypeEditViewModel viewModel)
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
