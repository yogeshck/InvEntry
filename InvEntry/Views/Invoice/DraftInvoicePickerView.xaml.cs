using DevExpress.Xpf.Core;
using InvEntry.ViewModels.Invoices;
using System.Windows;

namespace InvEntry.Views.Invoices;

public partial class DraftInvoicePickerView : ThemedWindow
{
    public DraftInvoicePickerView(
        DraftInvoicePickerViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        viewModel.RequestClose +=
            ViewModel_RequestClose;

        Loaded +=
            DraftInvoicePickerView_Loaded;

        Closed +=
            DraftInvoicePickerView_Closed;
    }


    private async void DraftInvoicePickerView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is
            DraftInvoicePickerViewModel vm)
        {
            await vm.LoadAsync();
        }
    }


    private void ViewModel_RequestClose(
        bool? dialogResult)
    {
        DialogResult = dialogResult;
    }


    private void DraftInvoicePickerView_Closed(
        object? sender,
        System.EventArgs e)
    {
        if (DataContext is
            DraftInvoicePickerViewModel vm)
        {
            vm.RequestClose -=
                ViewModel_RequestClose;
        }
    }
}