namespace InvEntry.Contracts.Gst;

public sealed class Gstr1B2bInvoiceResponse
{
    public long DocumentGkey { get; set; }
    public int SourceGkey { get; set; }
    public string RecipientGstin { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public DateOnly DocumentDate { get; set; }
    public string PlaceOfSupplyCode { get; set; } = string.Empty;
    public decimal InvoiceValue { get; set; }
    public string SupplyType { get; set; } = string.Empty;
    public string TaxType { get; set; } = string.Empty;
    public bool ReverseCharge { get; set; }
    public List<Gstr1B2bLineResponse> Lines { get; set; } = new();
}
