namespace InvEntry.Contracts.Gst;

public sealed class Gstr1BackfillResponse
{
    public string SupplierGstin { get; set; } = string.Empty;

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public bool DryRun { get; set; }

    public int ExaminedCount { get; set; }

    public int WouldStageCount { get; set; }

    public int StagedCount { get; set; }

    public int AlreadyStagedCount { get; set; }

    public int SkippedCount { get; set; }

    public int FailedCount { get; set; }

    public List<Gstr1BackfillItemResponse> Items { get; set; } = new();
}