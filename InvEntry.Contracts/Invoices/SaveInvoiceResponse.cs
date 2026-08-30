namespace InvEntry.Contracts.Invoices;

public sealed class SaveInvoiceResponse
{
    public int Gkey { get; set; }

    public string InvNbr { get; set; } = string.Empty;

    public bool IsNew { get; set; }

    public string Status { get; set; } = InvoiceStatus.Draft;
}