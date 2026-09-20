using System.Text.Json.Serialization;

namespace InvEntry.Contracts.Gst.Export;

public sealed class Gstr1GstnExportResponse
{
    [JsonPropertyName("gstin")]
    public string Gstin { get; init; } = string.Empty;

    [JsonPropertyName("fp")]
    public string FilingPeriod { get; init; } = string.Empty;

    [JsonPropertyName("b2cs")]
    public List<Gstr1GstnB2csRow> B2cs { get; init; } = [];

    [JsonPropertyName("hsn")]
    public Gstr1GstnHsnSection Hsn { get; init; } = new();

    [JsonPropertyName("doc_issue")]
    public Gstr1GstnDocumentIssueSection DocumentsIssued { get; init; } = new();

}