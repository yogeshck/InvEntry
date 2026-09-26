using System.Text.Json.Serialization;

namespace InvEntry.Contracts.Gst.Export;

public sealed class Gstr1GstnHsnSection
{
    [JsonPropertyName("hsn_b2b")]
    public List<Gstr1GstnHsnRow> B2B { get; init; } = [];

    [JsonPropertyName("hsn_b2c")]
    public List<Gstr1GstnHsnRow> B2C { get; init; } = [];

}
