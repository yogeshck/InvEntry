using System.Text.Json.Serialization;

namespace InvEntry.Contracts.Gst.Export;

public sealed class Gstr1GstnB2bItemDetail
{
    [JsonPropertyName("rt")]
    public decimal GstRate { get; init; }
    [JsonPropertyName("txval")]
    public decimal TaxableValue { get; init; }
    [JsonPropertyName("iamt")]
    public decimal IgstAmount { get; init; }
    [JsonPropertyName("camt")]
    public decimal CgstAmount { get; init; }
    [JsonPropertyName("samt")]
    public decimal SgstAmount { get; init; }
    [JsonPropertyName("csamt")]
    public decimal CessAmount { get; init; }
}
