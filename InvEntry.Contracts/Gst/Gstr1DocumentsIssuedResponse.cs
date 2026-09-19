namespace InvEntry.Contracts.Gst;

public sealed class Gstr1DocumentsIssuedResponse
{
    public string SupplierGstin { get; set; } = string.Empty;

    public string ReturnPeriod { get; set; } = string.Empty;

    public List<Gstr1DocumentSeriesResponse> Series { get; set; } = new();

    public int TotalIssued => Series.Sum(x => x.TotalIssued);

    public int TotalCancelled => Series.Sum(x => x.Cancelled);

    public int TotalNetIssued => Series.Sum(x => x.NetIssued);
}
