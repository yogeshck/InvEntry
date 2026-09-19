namespace InvEntry.Contracts.Gst;

public sealed class Gstr1DocumentsIssuedQuery
{
    public string SupplierGstin { get; set; } = string.Empty;

    public string ReturnPeriod { get; set; } = string.Empty;
}
