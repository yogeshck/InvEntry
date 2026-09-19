namespace InvEntry.Contracts.Gst;

public sealed class Gstr1CategorySummaryResponse
{
    public string Category { get; set; } = string.Empty;
    public string? Gstr1Table { get; set; }
    public int DocumentCount { get; set; }
    public decimal InvoiceValue { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal CessAmount { get; set; }
}
