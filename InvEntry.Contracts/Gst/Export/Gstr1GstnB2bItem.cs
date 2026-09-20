using System.Text.Json.Serialization;

namespace InvEntry.Contracts.Gst.Export;

public sealed class Gstr1GstnB2bItem
{
    [JsonPropertyName("num")]
    public int Number { get; init; }
    [JsonPropertyName("itm_det")]
    public Gstr1GstnB2bItemDetail ItemDetail { get; init; } = new();
}
