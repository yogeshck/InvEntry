using InvEntry.Models;

namespace InvEntry.ViewModels.Common;

public enum CustomerLookupStatus
{
    Invalid,
    Unchanged,
    New,
    Existing
}

public sealed record CustomerLookupResult(
    CustomerLookupStatus Status,
    Customer? Customer)
{
    public static CustomerLookupResult Invalid() =>
        new(CustomerLookupStatus.Invalid, null);

    public static CustomerLookupResult Unchanged(
        Customer customer) =>
        new(CustomerLookupStatus.Unchanged, customer);

    public static CustomerLookupResult New(
        Customer customer) =>
        new(CustomerLookupStatus.New, customer);

    public static CustomerLookupResult Existing(
        Customer customer) =>
        new(CustomerLookupStatus.Existing, customer);
}