using System.Text.Json.Serialization;

namespace InvEntry.Contracts.Gst.Export;

public sealed class Gstr1GstnB2bInvoice
{
    [JsonPropertyName("inum")]
    public string InvoiceNumber { get; init; } = string.Empty;
    [JsonPropertyName("idt")]
    public string InvoiceDate { get; init; } = string.Empty;
    [JsonPropertyName("val")]
    public decimal InvoiceValue { get; init; }
    [JsonPropertyName("pos")]
    public string PlaceOfSupplyCode { get; init; } = string.Empty;
    [JsonPropertyName("rchrg")]
    public string ReverseCharge { get; init; } = string.Empty;
    [JsonPropertyName("inv_typ")]
    public string InvoiceType { get; init; } = string.Empty;
    [JsonPropertyName("itms")]
    public List<Gstr1GstnB2bItem> Items { get; init; } = [];
}
