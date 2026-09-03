namespace InvEntry.Contracts.Invoices;

public sealed class CancelInvoiceResponse
{
    public int InvoiceGkey { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? ModifiedOn { get; set; }
}