namespace InvEntry.Contracts.Invoices;

public sealed class FinaliseInvoiceResponse
{
    public int Gkey { get; set; }

    public string InvNbr { get; set; } = string.Empty;

    public string Status { get; set; } = InvoiceStatus.Final;

    public DateTime? FinalisedOn { get; set; }
}