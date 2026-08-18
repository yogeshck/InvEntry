using InvEntry.Models;

namespace InvEntry.Services.Customers;

public sealed record CustomerLookupOutcome(
    Customer Customer,
    bool IsExisting,
    bool NeedsCreate);