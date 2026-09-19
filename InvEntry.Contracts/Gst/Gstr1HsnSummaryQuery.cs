namespace InvEntry.Contracts.Gst;

public sealed class Gstr1HsnSummaryQuery
{
    public string SupplierGstin { get; set; } = string.Empty;

    // yyyyMM
    public string ReturnPeriod { get; set; } = string.Empty;
}