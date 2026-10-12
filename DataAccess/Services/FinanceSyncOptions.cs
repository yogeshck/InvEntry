namespace DataAccess.Services;

public sealed class FinanceSyncOptions
{
    public const string SectionName = "FinanceTracker";
    public bool Enabled { get; set; }
    public int OrgGkey { get; set; } = 1;
    public int LocationGkey { get; set; } = 1;
    // Set only after deployment ownership has verified that this DataAccess
    // instance serves exactly the configured FinanceTracker branch.
    public bool ClosingEligibilityScopeVerified { get; set; }
    // Earliest business date for which invoice/outbox coverage is known to be
    // complete. A missing value deliberately prevents a CLEAR decision.
    public DateOnly? ClosingEligibilityCoverageStartDate { get; set; }
}
