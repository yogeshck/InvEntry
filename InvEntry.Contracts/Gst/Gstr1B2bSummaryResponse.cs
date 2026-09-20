namespace InvEntry.Contracts.Gst;

public sealed class Gstr1B2bSummaryResponse
{
    public string SupplierGstin { get; set; } = string.Empty;
    public string ReturnPeriod { get; set; } = string.Empty;
    public int InvoiceCount => Invoices.Count;
    public List<Gstr1B2bInvoiceResponse> Invoices { get; set; } = new();
    public decimal TotalInvoiceValue => Invoices.Sum(x => x.InvoiceValue);
    public decimal TotalTaxableValue => Invoices.Sum(x => x.Lines.Sum(y => y.TaxableValue));
    public decimal TotalIgst => Invoices.Sum(x => x.Lines.Sum(y => y.IgstAmount));
    public decimal TotalCgst => Invoices.Sum(x => x.Lines.Sum(y => y.CgstAmount));
    public decimal TotalSgst => Invoices.Sum(x => x.Lines.Sum(y => y.SgstAmount));
    public decimal TotalCess => Invoices.Sum(x => x.Lines.Sum(y => y.CessAmount));
}
