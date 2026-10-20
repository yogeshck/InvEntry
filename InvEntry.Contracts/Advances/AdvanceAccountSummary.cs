namespace InvEntry.Contracts.Advances;

/// <summary>
/// An advance or RD account the customer can use on the billing settlement screen.
/// </summary>
public class AdvanceAccountSummary
{
    public int AdvanceAccountGkey { get; set; }

    public string AccountNbr { get; set; } = string.Empty;

    /// <summary>SCHEME, ORDER, OPEN or RATE_BOOKING.</summary>
    public string AccountType { get; set; } = string.Empty;

    /// <summary>
    /// Settlement payment mode that uses this account: "Advance Adj" or "RD Adj".
    /// </summary>
    public string SettlementMode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public decimal BalanceAmount { get; set; }

    public decimal BalanceWeight { get; set; }

    public DateOnly OpenedOn { get; set; }

    public DateOnly? ValidUntil { get; set; }

    public DateOnly? MaturityDate { get; set; }

    public string? CustomerOrderNbr { get; set; }
}
