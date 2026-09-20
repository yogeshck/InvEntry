namespace InvEntry.Contracts.Gst;

public sealed class Gstr1B2csSummaryResponse
{
    public string SupplierGstin { get; set; } = string.Empty;

    public string ReturnPeriod { get; set; } = string.Empty;

    public List<Gstr1B2csSummaryRowResponse> Rows { get; set; } = new();

    public decimal TotalTaxableValue => Rows.Sum(x => x.TaxableValue);

    public decimal TotalCess => Rows.Sum(x => x.CessAmount);

    public decimal TotalCgst => Rows.Sum(x => x.CgstAmount);

    public decimal TotalSgst => Rows.Sum(x => x.SgstAmount);

    public decimal TotalIgst => Rows.Sum(x => x.IgstAmount);
}
