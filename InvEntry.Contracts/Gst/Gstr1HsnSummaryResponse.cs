namespace InvEntry.Contracts.Gst;

public sealed class Gstr1HsnSummaryResponse
{
    public string SupplierGstin { get; set; } = string.Empty;

    public string ReturnPeriod { get; set; } = string.Empty;

    public List<Gstr1HsnSummaryRowResponse> B2B { get; set; } = new();

    public List<Gstr1HsnSummaryRowResponse> B2C { get; set; } = new();

    public int B2BRowCount => B2B.Count;

    public int B2CRowCount => B2C.Count;

    public decimal TotalQuantity =>
        B2B.Sum(x => x.TotalQuantity) +
        B2C.Sum(x => x.TotalQuantity);

    public decimal TotalTaxableValue =>
        B2B.Sum(x => x.TaxableValue) +
        B2C.Sum(x => x.TaxableValue);

    public decimal TotalCgst =>
        B2B.Sum(x => x.CgstAmount) +
        B2C.Sum(x => x.CgstAmount);

    public decimal TotalSgst =>
        B2B.Sum(x => x.SgstAmount) +
        B2C.Sum(x => x.SgstAmount);

    public decimal TotalIgst =>
        B2B.Sum(x => x.IgstAmount) +
        B2C.Sum(x => x.IgstAmount);

    public decimal TotalCess =>
        B2B.Sum(x => x.CessAmount) +
        B2C.Sum(x => x.CessAmount);
}