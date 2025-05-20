using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.Grid;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

public partial class CustomerOrderListViewModel : ObservableObject
{

    private readonly ICustomerOrderService _custOrderService;
    private readonly IDialogService _reportDialogService;

    [ObservableProperty]
    private ObservableCollection<CustomerOrder> _customerOrder;

    [ObservableProperty]
    private DateSearchOption _searchOption;

    [ObservableProperty]
    private CustomerOrder _SelectedOrder;

    [ObservableProperty]
    private DateTime _Today = DateTime.Today;

    public CustomerOrderListViewModel(  ICustomerOrderService custOrderService,
                                        [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
    {
        _custOrderService = custOrderService;
        _reportDialogService = reportDialogService;

        CustomerOrder = null;
        _searchOption = new();
        SearchOption.To = Today;
        SearchOption.From = Today.AddDays(-1);
        Task.Run(RefreshCustomerOrderAsync).Wait();
    }

    [RelayCommand]
    private async Task RefreshCustomerOrderAsync()
    {
        var custOrderResult = await _custOrderService.GetAll(SearchOption);
        if (custOrderResult is not null)
            CustomerOrder = null;
        CustomerOrder = new(custOrderResult);
    }

    [RelayCommand(CanExecute = nameof(CanPrintCustomerOrder))]
    private void PrintInvoice()
    {
        _reportDialogService.PrintPreview(SelectedOrder.OrderNbr);
    }

    private bool CanPrintCustomerOrder()
    {
        return SelectedOrder is not null;
    }

    [RelayCommand]
    private void SelectionChanged()
    {
      //  PrintCustomerOrderCommand.NotifyCanExecuteChanged();
    }
}
