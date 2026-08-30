namespace InvEntry.Contracts.Invoices;

public static class InvoiceStatus
{
    public const string Draft = "DRAFT";
    public const string Final = "FINAL";
    public const string Cancelled = "CANCELLED";

    public static bool IsDraft(string? status)
    {
        return string.Equals(
            status,
            Draft,
            StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsFinal(string? status)
    {
        return string.Equals(
            status,
            Final,
            StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsCancelled(string? status)
    {
        return string.Equals(
            status,
            Cancelled,
            StringComparison.OrdinalIgnoreCase);
    }
}