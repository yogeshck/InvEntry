namespace InvEntry.Contracts.Gst;

public sealed class Gstr1B2bLineResponse
{
    public int LineNumber { get; set; }
    public decimal GstRate { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal CessAmount { get; set; }
}
