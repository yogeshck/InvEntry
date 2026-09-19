namespace InvEntry.Contracts.Gst;

public sealed class Gstr1BackfillRequest
{
    public string SupplierGstin { get; set; } = string.Empty;

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public bool DryRun { get; set; } = true;
}