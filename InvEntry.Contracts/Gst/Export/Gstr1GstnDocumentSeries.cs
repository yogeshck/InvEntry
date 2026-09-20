using System.Text.Json.Serialization;

namespace InvEntry.Contracts.Gst.Export;

public sealed class Gstr1GstnDocumentSeries
{
    [JsonPropertyName("num")]
    public int Number { get; init; }

    [JsonPropertyName("from")]
    public string FromNumber { get; init; } = string.Empty;

    [JsonPropertyName("to")]
    public string ToNumber { get; init; } = string.Empty;

    [JsonPropertyName("totnum")]
    public int TotalIssued { get; init; }

    [JsonPropertyName("cancel")]
    public int Cancelled { get; init; }

    [JsonPropertyName("net_issue")]
    public int NetIssued { get; init; }

}