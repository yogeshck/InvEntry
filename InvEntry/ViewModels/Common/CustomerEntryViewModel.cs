using CommunityToolkit.Mvvm.ComponentModel;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Tally;
using System.Threading.Tasks;

namespace InvEntry.ViewModels.Common;

public partial class CustomerEntryViewModel : ObservableObject
{
    private readonly ICustomerService _customerService;

    public CustomerEntryViewModel(
        ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [ObservableProperty]
    private Customer? buyer;

    [ObservableProperty]
    private OrgThisCompanyView? company;

    [ObservableProperty]
    private bool customerReadOnly;

    [ObservableProperty]
    private bool isNewCustomer;

    [ObservableProperty]
    private bool isExistingCustomer;

    public async Task<CustomerLookupResult> FetchAsync(
        string phoneNumber,
        OrgThisCompanyView company)
    {
        phoneNumber = phoneNumber?.Trim() ?? string.Empty;

        if (phoneNumber.Length < 10)
        {
            return CustomerLookupResult.Invalid();
        }

        if (Buyer is not null &&
            Buyer.MobileNbr == phoneNumber)
        {
            return CustomerLookupResult.Unchanged(Buyer);
        }

        CustomerReadOnly = false;
        IsNewCustomer = false;
        IsExistingCustomer = false;

        Buyer =
            await _customerService.GetCustomer(phoneNumber);

        if (Buyer is null)
        {
            Buyer = CreateNewCustomer(
                phoneNumber,
                company);

            IsNewCustomer = true;

            return CustomerLookupResult.New(Buyer);
        }

        EnsureAddress(Buyer, company);

        IsExistingCustomer = true;

        return CustomerLookupResult.Existing(Buyer);
    }

    private static Customer CreateNewCustomer(
        string phoneNumber,
        OrgThisCompanyView company)
    {
        var customer = new Customer
        {
            MobileNbr = phoneNumber
        };

        customer.Address ??= new();

        customer.Address.GstStateCode =
            company.GstCode;

        customer.Address.State =
            company.State;

        customer.Address.District =
            company.District;

        return customer;
    }

    private static void EnsureAddress(
    Customer customer,
    OrgThisCompanyView company)
    {
        customer.Address ??= new();

        if (string.IsNullOrWhiteSpace(
                customer.Address.GstStateCode))
        {
            customer.Address.GstStateCode =
                company.GstCode;
        }

        if (string.IsNullOrWhiteSpace(
                customer.Address.State))
        {
            customer.Address.State =
                company.State;
        }

        if (string.IsNullOrWhiteSpace(
                customer.Address.District))
        {
            customer.Address.District =
                company.District;
        }

        // Temporary compatibility
        customer.GstStateCode =
            customer.Address.GstStateCode;
    }

}
