using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Tally;
using InvEntry.Tally.Model;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DevExpress.Xpf.Printing;
using IDialogService = DevExpress.Mvvm.IDialogService;
using InvEntry.Reports;
using System.Windows;
using System.Collections.Generic;
using DevExpress.XtraPivotGrid.Data;
using System.Windows.Documents;
using DevExpress.Mvvm.Native;
using System.Linq;
using DevExpress.XtraGrid.Views.Items;

namespace InvEntry.ViewModels;

public partial class CustomerOrderDBViewListViewModel : ObservableObject
{

    private readonly ICustomerOrderDbViewService _customerOrderDbViewService;
    private readonly IDialogService _reportDialogService;

    [ObservableProperty]
    private ObservableCollection<CustomerOrderDBView> _customerOrderDBViews;

    [ObservableProperty]
    private DateSearchOption _dateSearchOption;

    [ObservableProperty]
    private CustomerOrderDBView _SelectedOrder;

    [ObservableProperty]
    private DateTime _Today = DateTime.Today;

    public CustomerOrderDBViewListViewModel(ICustomerOrderDbViewService customerOrderDbViewService,
                             [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
    {
        _customerOrderDbViewService = customerOrderDbViewService;
        _reportDialogService = reportDialogService;
        _dateSearchOption = new();
        DateSearchOption.To = Today;
        DateSearchOption.From = Today.AddDays(-1);

        Task.Run(RefreshCustomerOrder).Wait();

    }

    [RelayCommand]
    private async Task RefreshCustomerOrder()
    {
        var customerOrders = await _customerOrderDbViewService.GetAll(DateSearchOption);

        if (customerOrders is not null)

            CustomerOrderDBViews = new(customerOrders);

    }

}
