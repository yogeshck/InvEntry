namespace DataAccess.Services;

public sealed class FinanceSyncOptions
{
    public const string SectionName = "FinanceTracker";
    public bool Enabled { get; set; }
    public int OrgGkey { get; set; } = 1;
    public int LocationGkey { get; set; } = 1;
}
