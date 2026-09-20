namespace InvEntry.Contracts.Estimates;

public sealed class SaveEstimateResponse
{
    public int Gkey { get; set; }
    public string EstNbr { get; set; } = string.Empty;
    public int? TenantGkey { get; set; }
}
