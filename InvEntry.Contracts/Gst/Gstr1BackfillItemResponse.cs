namespace InvEntry.Contracts.Gst;

public sealed class Gstr1BackfillItemResponse
{
    public int SourceGkey { get; set; }

    public string DocumentNbr { get; set; } = string.Empty;

    public DateTime? DocumentDate { get; set; }

    public string Outcome { get; set; } = string.Empty;

    public string? Message { get; set; }
}