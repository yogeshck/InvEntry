using System.Text.Json.Serialization;

namespace InvEntry.Contracts.Gst.Export;

public sealed class Gstr1GstnB2bRecipient
{
    [JsonPropertyName("ctin")]
    public string RecipientGstin { get; init; } = string.Empty;

    [JsonPropertyName("inv")]
    public List<Gstr1GstnB2bInvoice> Invoices { get; init; } = [];
}
