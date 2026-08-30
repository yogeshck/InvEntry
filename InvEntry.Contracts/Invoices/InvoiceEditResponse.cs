namespace InvEntry.Contracts.Invoices;

public sealed class InvoiceEditResponse
{
    public InvoiceHeaderSaveModel Header { get; set; } = new();

    public List<InvoiceLineSaveModel> Lines { get; set; } = new();

    public List<InvoiceOldMetalSaveModel> OldMetalTransactions { get; set; } = new();

    public List<InvoiceReceiptSaveModel> Receipts { get; set; } = new();
}