using System;

namespace InvEntry.Models.Settlements;

/// <summary>
/// A customer advance / RD account offered on an "Advance Adj" or "RD Adj"
/// settlement line (from api/AdvanceAccount/customer/{gkey}/available).
/// </summary>
public sealed class AdvanceAccountOption
{
    public int AdvanceAccountGkey { get; init; }

    public string AccountNbr { get; init; } = string.Empty;

    /// <summary>SCHEME, ORDER, OPEN or RATE_BOOKING.</summary>
    public string AccountType { get; init; } = string.Empty;

    /// <summary>"Advance Adj" or "RD Adj": the payment mode that uses this account.</summary>
    public string SettlementMode { get; init; } = string.Empty;

    public decimal BalanceAmount { get; init; }

    public DateOnly? MaturityDate { get; init; }

    public string? CustomerOrderNbr { get; init; }

    public string DisplayText =>
        $"{AccountNbr} ({Kind}) · ₹{BalanceAmount:N2}";

    private string Kind => AccountType?.ToUpperInvariant() switch
    {
        "SCHEME" => MaturityDate is DateOnly maturity ? $"RD, matures {maturity:dd-MM-yyyy}" : "RD",
        "ORDER" => string.IsNullOrWhiteSpace(CustomerOrderNbr) ? "Order advance" : $"Order {CustomerOrderNbr}",
        "RATE_BOOKING" => "Rate booking",
        _ => "Advance"
    };
}
