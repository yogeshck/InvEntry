using System.Text.Json.Serialization;

namespace InvEntry.Contracts.Gst.Export;

public sealed class Gstr1GstnB2csRow
{
    [JsonPropertyName("pos")]
    public string PlaceOfSupplyCode { get; init; } = string.Empty;

    [JsonPropertyName("sply_ty")]
    public string SupplyType { get; init; } = string.Empty;

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

    [JsonPropertyName("typ")]
    public string Type { get; init; } = string.Empty;

}