namespace InvEntry.Contracts.Gst;

public sealed class Gstr1DocumentSeriesResponse
{
    public string DocumentType { get; set; } = string.Empty;

    public string NatureOfDocument { get; set; } = string.Empty;

    public string Series { get; set; } = string.Empty;

    public string FromNumber { get; set; } = string.Empty;

    public string ToNumber { get; set; } = string.Empty;

    public int TotalIssued { get; set; }

    public int Cancelled { get; set; }

    public int NetIssued => TotalIssued - Cancelled;
}
