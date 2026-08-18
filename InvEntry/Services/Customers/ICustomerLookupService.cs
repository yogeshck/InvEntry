using InvEntry.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InvEntry.Services.Customers;

public interface ICustomerLookupService
{
    Task<CustomerLookupOutcome> ResolveByMobileAsync(
        string mobileNbr,
        CancellationToken cancellationToken = default);

    Task<CustomerLookupOutcome> ResolveByGkeyAsync(
        int customerGkey,
        CancellationToken cancellationToken = default);

    Task<Customer> CreateAsync(
        Customer customer,
        CancellationToken cancellationToken = default);

    Task<Customer> UpdateAsync(
        Customer customer,
        CancellationToken cancellationToken = default);
}