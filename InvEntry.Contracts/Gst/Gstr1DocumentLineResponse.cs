namespace InvEntry.Contracts.Gst;

public sealed class Gstr1DocumentLineResponse
{
    public long GstDocumentGkey { get; set; }
    public int LineNbr { get; set; }
    public int? SourceLineGkey { get; set; }
    public string? HsnCode { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal GstRate { get; set; }
    public decimal CgstRate { get; set; }
    public decimal SgstRate { get; set; }
    public decimal IgstRate { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal CessAmount { get; set; }
}
