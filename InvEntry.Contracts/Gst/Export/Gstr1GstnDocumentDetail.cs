using System.Text.Json.Serialization;

namespace InvEntry.Contracts.Gst.Export;

public sealed class Gstr1GstnDocumentDetail
{
    [JsonPropertyName("doc_num")]
    public int DocumentNumber { get; init; }

    [JsonPropertyName("docs")]
    public List<Gstr1GstnDocumentSeries> Series { get; init; } = [];

}