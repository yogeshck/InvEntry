using CommunityToolkit.Mvvm.ComponentModel;
using InvEntry.Models;
using System;

namespace InvEntry.ViewModels.CustomerOrders;

public partial class CustomerOrderSummaryViewModel : ObservableObject
{
    [ObservableProperty]
    private CustomerOrder? order;

    public void Initialize(CustomerOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        Order = order;
    }
}