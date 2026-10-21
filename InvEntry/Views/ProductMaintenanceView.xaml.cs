using DevExpress.Xpf.WindowsUI;
using InvEntry.ViewModels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace InvEntry.Views
{
    public partial class ProductMaintenanceView : NavigationPage
    {
        public ProductMaintenanceView()
        {
            InitializeComponent();
            Loaded += async (_, _) => await LoadAsync();
        }

        private ProductMaintenanceViewModel? ViewModel => DataContext as ProductMaintenanceViewModel;

        private async Task LoadAsync()
        {
            if (ViewModel is not null)
                await ViewModel.RefreshCommand.ExecuteAsync(null);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e) => OpenEditor();

        private void ProductGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OpenEditor();

        private async void OpenEditor()
        {
            var viewModel = ViewModel;
            if (viewModel is null)
                return;

            var editor = viewModel.CreateEditor();
            if (editor is null)
            {
                MessageBox.Show(Window.GetWindow(this), "Select a product to edit.", "Product",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new ProductEditWindow(editor) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true)
                await LoadAsync();
        }
    }
}
