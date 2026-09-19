namespace InvEntry.Contracts.Gst;

public sealed class Gstr1EnrichmentResponse
{
    public string SupplierGstin { get; set; } = string.Empty;

    public string ReturnPeriod { get; set; } = string.Empty;

    public bool DryRun { get; set; }

    public int Examined { get; set; }

    public int WouldEnrich { get; set; }

    public int Enriched { get; set; }

    public int AlreadyEnriched { get; set; }

    public int Skipped { get; set; }

    public int Failed { get; set; }

    public List<Gstr1EnrichmentItemResponse> Items { get; set; } = new();
}