namespace InvEntry.Contracts.Gst;

public sealed class Gstr1B2csSummaryRowResponse
{
    public string Type { get; set; } = string.Empty;

    public string PlaceOfSupplyCode { get; set; } = string.Empty;

    public decimal GstRate { get; set; }

    public decimal TaxableValue { get; set; }

    public decimal CessAmount { get; set; }

    public string? ECommerceGstin { get; set; }

    public decimal? ApplicableTaxRatePercentage { get; set; }

    public decimal CgstAmount { get; set; }

    public decimal SgstAmount { get; set; }

    public decimal IgstAmount { get; set; }
}
