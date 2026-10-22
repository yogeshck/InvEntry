namespace InvEntry.Contracts.Gst;

/// <summary>
/// Operator correction of the GST reporting fields of one staged
/// GSTR-1 line. Values, rates and tax amounts are never changed here.
/// </summary>
public sealed class Gstr1LineCorrectionRequest
{
    public string SupplierGstin { get; set; } = string.Empty;

    public string ReturnPeriod { get; set; } = string.Empty;

    public string? HsnCode { get; set; }

    public string? Uom { get; set; }

    public string? Uqc { get; set; }

    public decimal? GstQuantity { get; set; }
}
