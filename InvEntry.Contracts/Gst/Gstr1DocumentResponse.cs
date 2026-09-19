namespace InvEntry.Contracts.Gst;

public sealed class Gstr1DocumentResponse
{
    public long Gkey { get; set; }
    public int SourceGkey { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNbr { get; set; } = string.Empty;
    public DateOnly DocumentDate { get; set; }
    public string? RecipientGstin { get; set; }
    public string? RecipientStateCode { get; set; }
    public bool IsRecipientRegistered { get; set; }
    public string PlaceOfSupplyCode { get; set; } = string.Empty;
    public string SupplyType { get; set; } = string.Empty;
    public string TaxType { get; set; } = string.Empty;
    public string ReturnCategory { get; set; } = string.Empty;
    public string? Gstr1Table { get; set; }
    public bool IsReportable { get; set; }
    public decimal InvoiceValue { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal CessAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}
