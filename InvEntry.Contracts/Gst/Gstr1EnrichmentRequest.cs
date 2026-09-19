namespace InvEntry.Contracts.Gst;

public sealed class Gstr1EnrichmentRequest
{
    public string SupplierGstin { get; set; } = string.Empty;

    public string ReturnPeriod { get; set; } = string.Empty;

    public bool DryRun { get; set; } = true;
}