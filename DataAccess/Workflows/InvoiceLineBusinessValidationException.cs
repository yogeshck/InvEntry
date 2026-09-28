namespace DataAccess.Workflows;

public sealed class InvoiceLineBusinessValidationException(
    IReadOnlyList<string> errors)
    : InvalidOperationException(
        "The invoice contains item-weight validation errors.")
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
