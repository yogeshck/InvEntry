namespace InvEntry.Contracts.Invoices;

public sealed record InvoiceCustomerValidationResult(
    bool IsValid,
    string Message,
    string? FailureCode = null)
{
    public static InvoiceCustomerValidationResult Valid { get; } =
        new(true, string.Empty);
}

public static class InvoiceCustomerAssociationValidator
{
    public const string RequiredMessage =
        "Customer information is required. Enter a valid mobile number.";

    public static InvoiceCustomerValidationResult Validate(
        string? invoiceMobile,
        int? customerGkey,
        string? resolvedMobile,
        string? customerName,
        bool customerExists = true)
    {
        var mobile = invoiceMobile?.Trim();

        // Invoice customer lookup currently starts only after ten characters.
        // Keep that established rule at every entry boundary.
        if (string.IsNullOrWhiteSpace(mobile))
            return new(false, RequiredMessage, "MOBILE_REQUIRED");

        if (mobile.Length < 10)
        {
            return new(
                false,
                "Enter a valid customer mobile number before selecting an invoice item.",
                "MOBILE_INCOMPLETE");
        }

        if (!customerGkey.HasValue || customerGkey.Value <= 0)
        {
            return new(
                false,
                "Select an existing customer or save the new customer details before selecting an invoice item.",
                "CUSTOMER_ID_MISSING");
        }

        if (!customerExists)
        {
            return new(
                false,
                "The selected customer is no longer available. Resolve the customer again before continuing.",
                "CUSTOMER_NOT_AVAILABLE");
        }

        if (string.IsNullOrWhiteSpace(resolvedMobile) ||
            !string.Equals(mobile, resolvedMobile.Trim(), StringComparison.Ordinal))
        {
            return new(
                false,
                "The customer mobile number has changed. Complete customer lookup before continuing.",
                "MOBILE_MISMATCH");
        }

        if (string.IsNullOrWhiteSpace(customerName))
        {
            return new(
                false,
                "Customer name is required. Complete and save the customer details before continuing.",
                "CUSTOMER_NAME_MISSING");
        }

        return InvoiceCustomerValidationResult.Valid;
    }
}
