namespace DataAccess.Workflows;

public sealed class InvoiceCustomerBusinessValidationException(string message)
    : InvalidOperationException(message);
