namespace InvEntry.Contracts.Gst;

public sealed class Gstr1HsnSummaryRowResponse
{
    // B2B / B2C
    public string SupplyClass { get; set; } = string.Empty;

    public string HsnCode { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Uqc { get; set; } = string.Empty;

    public decimal GstRate { get; set; }

    public decimal TotalQuantity { get; set; }

    public decimal TaxableValue { get; set; }

    public decimal CgstAmount { get; set; }

    public decimal SgstAmount { get; set; }

    public decimal IgstAmount { get; set; }

    public decimal CessAmount { get; set; }
}