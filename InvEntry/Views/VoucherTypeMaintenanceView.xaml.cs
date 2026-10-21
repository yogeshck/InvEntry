using DevExpress.Xpf.WindowsUI;
using InvEntry.ViewModels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace InvEntry.Views
{
    public partial class VoucherTypeMaintenanceView : NavigationPage
    {
        public VoucherTypeMaintenanceView()
        {
            InitializeComponent();
            Loaded += async (_, _) => await LoadAsync();
        }

        private VoucherTypeMaintenanceViewModel? ViewModel => DataContext as VoucherTypeMaintenanceViewModel;

        private async Task LoadAsync()
        {
            if (ViewModel is not null)
                await ViewModel.RefreshCommand.ExecuteAsync(null);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e) => OpenEditor();

        private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OpenEditor();

        private async void OpenEditor()
        {
            var editor = ViewModel?.CreateEditor();
            if (editor is null)
            {
                MessageBox.Show(Window.GetWindow(this), "Select a voucher type to edit.", "Voucher Type",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new VoucherTypeEditWindow(editor) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true)
                await LoadAsync();
        }
    }
}
