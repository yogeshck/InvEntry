using DevExpress.Mvvm;
using InvEntry.Models;
using InvEntry.ViewModels.Common;
using InvEntry.ViewModels.CustomerOrders;
using InvEntry.Views.Common;
using System;
using System.Threading.Tasks;

namespace InvEntry.Extension;

public static class CustomerDialogExtension
{

    public static async Task<Customer?> EditCustomerAsync(
    this IDialogService dialogService,
    Customer customer,
    bool isNewCustomer)
    {
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(customer);

        var dialogVM =
            DISource.Resolve<CustomerEditViewModel>();

        if (dialogVM is null)
        {
            throw new InvalidOperationException(
                "CustomerEditViewModel could not be resolved from DI.");
        }

        await dialogVM.InitializeAsync(
            customer,
            isNewCustomer);

        dialogService.ShowDialog(
            null,
            isNewCustomer
                ? "New Customer"
                : "Edit Customer",
            $"{nameof(CustomerEditView)}",
            dialogVM);

        if (!dialogVM.WasSaved)
            return null;

        return dialogVM.CurrentCustomer;
    }

    public static void ShowOrderSummary(
        this IDialogService dialogService,
        CustomerOrder order)
    {
        var dialogVM =
            DISource.Resolve<CustomerOrderSummaryViewModel>();

        dialogVM.Initialize(order);

        dialogService.ShowDialog(
            null,
            "Customer Order Summary",
            "CustomerOrderSummaryView",
            dialogVM);
    }

}

/// <summary>
/// Opens the shared customer popup (the one Invoice and Customer Order use)
/// for view models that only need to create or edit a customer.
/// </summary>
public interface ICustomerEditorDialog
{
    Task<Customer?> EditCustomerAsync(Customer customer, bool isNewCustomer);
}

public sealed class CustomerEditorDialog : ICustomerEditorDialog
{
    private readonly IDialogService _dialogService;

    public CustomerEditorDialog(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public Task<Customer?> EditCustomerAsync(Customer customer, bool isNewCustomer)
        => _dialogService.EditCustomerAsync(customer, isNewCustomer);
}