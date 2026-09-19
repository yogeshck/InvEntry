namespace InvEntry.Contracts.Gst;

public sealed class Gstr1ReturnSummaryResponse
{
    public string SupplierGstin { get; set; } = string.Empty;
    public string ReturnPeriod { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
    public int ReportableDocumentCount { get; set; }
    public decimal TotalInvoiceValue { get; set; }
    public decimal TotalTaxableValue { get; set; }
    public decimal TotalCgst { get; set; }
    public decimal TotalSgst { get; set; }
    public decimal TotalIgst { get; set; }
    public decimal TotalCess { get; set; }
    public List<Gstr1CategorySummaryResponse> Categories { get; set; } = [];
}
