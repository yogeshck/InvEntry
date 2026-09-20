using System.Text.Json.Serialization;

namespace InvEntry.Contracts.Gst.Export;

public sealed class Gstr1GstnDocumentIssueSection
{
    [JsonPropertyName("doc_det")]
    public List<Gstr1GstnDocumentDetail> Details { get; init; } = [];

}