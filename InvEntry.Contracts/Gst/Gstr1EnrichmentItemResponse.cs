namespace InvEntry.Contracts.Gst;

public sealed class Gstr1EnrichmentItemResponse
{
    public long DocumentGkey { get; set; }

    public long LineGkey { get; set; }

    public int SourceGkey { get; set; }

    public int? SourceLineGkey { get; set; }

    public string? DocumentNbr { get; set; }

    public int LineNbr { get; set; }

    public string? HsnCode { get; set; }

    public string? ProductName { get; set; }

    public string? Uom { get; set; }

    public string? Uqc { get; set; }

    public decimal? GstQuantity { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Message { get; set; }
}