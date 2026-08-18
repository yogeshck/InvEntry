using InvEntry.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InvEntry.Services.Customers;

public sealed class CustomerLookupService : ICustomerLookupService
{
    private readonly ICustomerService _customerService;

    public CustomerLookupService(
        ICustomerService customerService)
    {
        _customerService = customerService;
    }

    public async Task<CustomerLookupOutcome> ResolveByMobileAsync(
        string mobileNbr,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedMobile =
            NormalizeMobile(mobileNbr);

        if (string.IsNullOrWhiteSpace(normalizedMobile))
        {
            throw new ArgumentException(
                "Mobile number is required.",
                nameof(mobileNbr));
        }

        var customer =
            await _customerService.GetCustomer(
                normalizedMobile);

        cancellationToken.ThrowIfCancellationRequested();

        if (customer is null)
        {
            var draft =
                CreateDraftCustomer(
                    normalizedMobile);

            return new CustomerLookupOutcome(
                draft,
                IsExisting: false,
                NeedsCreate: true);
        }

        EnsureCustomerShape(customer);

        return new CustomerLookupOutcome(
            customer,
            IsExisting: true,
            NeedsCreate: false);
    }

    public async Task<CustomerLookupOutcome> ResolveByGkeyAsync(
        int customerGkey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (customerGkey <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(customerGkey),
                "Customer GKey must be greater than zero.");
        }

        var customer =
            await _customerService.GetCustomerByGkey(
                customerGkey);

        cancellationToken.ThrowIfCancellationRequested();

        if (customer is null)
        {
            throw new InvalidOperationException(
                $"Customer GKey {customerGkey} was not found.");
        }

        EnsureCustomerShape(customer);

        return new CustomerLookupOutcome(
            customer,
            IsExisting: true,
            NeedsCreate: false);
    }

    public async Task<Customer> CreateAsync(
        Customer customer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customer);

        cancellationToken.ThrowIfCancellationRequested();

        PrepareCustomer(customer);

        if (customer.GKey > 0)
        {
            throw new InvalidOperationException(
                "Customer has already been saved.");
        }

        //
        // Cycle-1 duplicate prevention.
        // Later this will move away from MobileNbr
        // when CustomerNbr / OrgContact becomes authoritative.
        //
        var existing =
            await _customerService.GetCustomer(
                customer.MobileNbr!);

        cancellationToken.ThrowIfCancellationRequested();

        if (existing is not null)
        {
            EnsureCustomerShape(existing);

            return existing;
        }

        var created =
            await _customerService.CreateCustomer(
                customer);

        cancellationToken.ThrowIfCancellationRequested();

        if (created is null)
        {
            throw new InvalidOperationException(
                "Customer could not be created.");
        }

        EnsureCustomerShape(created);

        if (created.GKey <= 0)
        {
            throw new InvalidOperationException(
                "Customer was created but no valid GKey was returned.");
        }

        return created;
    }

    public async Task<Customer> UpdateAsync(
        Customer customer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customer);

        cancellationToken.ThrowIfCancellationRequested();

        if (customer.GKey <= 0)
        {
            throw new InvalidOperationException(
                "Cannot update a customer that has not been saved.");
        }

        PrepareCustomer(customer);

        await _customerService.UpdateCustomer(
            customer);

        cancellationToken.ThrowIfCancellationRequested();

        return customer;
    }

    private static Customer CreateDraftCustomer(
        string mobileNbr)
    {
        var customer =
            new Customer
            {
                MobileNbr = mobileNbr
            };

        EnsureCustomerShape(customer);

        return customer;
    }

    private static void PrepareCustomer(
        Customer customer)
    {
        EnsureCustomerShape(customer);

        customer.MobileNbr =
            NormalizeMobile(
                customer.MobileNbr);

        if (string.IsNullOrWhiteSpace(
                customer.MobileNbr))
        {
            throw new InvalidOperationException(
                "Customer mobile number is required.");
        }

        //
        // Compatibility mirror.
        // Address.GstStateCode remains the preferred location.
        //
        if (!string.IsNullOrWhiteSpace(
                customer.Address.GstStateCode))
        {
            customer.GstStateCode =
                customer.Address.GstStateCode;
        }
        else if (!string.IsNullOrWhiteSpace(
                     customer.GstStateCode))
        {
            customer.Address.GstStateCode =
                customer.GstStateCode;
        }
    }

    private static void EnsureCustomerShape(
        Customer customer)
    {
        customer.Address ??=
            new OrgAddress();
    }

    private static string NormalizeMobile(
        string? mobileNbr)
    {
        return mobileNbr?.Trim() ??
               string.Empty;
    }
}