using System.Text.Json.Serialization;

namespace InvEntry.Contracts.Gst.Export;

public sealed class Gstr1GstnHsnRow
{
    [JsonPropertyName("num")]
    public int Number { get; init; }

    [JsonPropertyName("hsn_sc")]
    public string HsnCode { get; init; } = string.Empty;

    [JsonPropertyName("desc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    [JsonPropertyName("uqc")]
    public string Uqc { get; init; } = string.Empty;

    [JsonPropertyName("qty")]
    public decimal Quantity { get; init; }

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