using DevExpress.Mvvm;
using InvEntry.Models;
using InvEntry.ViewModels.Common;
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

}