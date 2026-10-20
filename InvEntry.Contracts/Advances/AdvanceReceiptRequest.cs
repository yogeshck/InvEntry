namespace InvEntry.Contracts.Advances;

/// <summary>
/// Money received from a customer into an advance or RD account.
/// </summary>
public class AdvanceReceiptRequest
{
    /// <summary>
    /// Generated once by the client per receipt and reused on every retry.
    /// The server stores it in ADVANCE_LEDGER.SOURCE_REF, so a retried request
    /// returns the original receipt instead of creating a second one.
    /// </summary>
    public Guid ClientRequestId { get; set; }

    public int CustomerGkey { get; set; }

    /// <summary>
    /// <see cref="AdvanceReceiptType.AdvanceReceipt"/> or
    /// <see cref="AdvanceReceiptType.RecurringDeposit"/>; also the voucher type.
    /// </summary>
    public string ReceiptType { get; set; } = string.Empty;

    /// <summary>
    /// Account to credit. When null, the customer's single active account of the
    /// receipt type is used, and for an advance receipt one is opened if none exists.
    /// </summary>
    public int? AdvanceAccountGkey { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Cash, UPI, Card, Bank, etc.</summary>
    public string Mode { get; set; } = "Cash";

    public string? TransDesc { get; set; }

    public DateTime? TransDate { get; set; }
}

public class AdvanceReceiptResponse
{
    public int VoucherGkey { get; set; }

    public string VoucherNbr { get; set; } = string.Empty;

    public DateTime? VoucherDate { get; set; }

    public int AdvanceAccountGkey { get; set; }

    public string AccountNbr { get; set; } = string.Empty;

    public long AdvanceLedgerGkey { get; set; }

    public decimal Amount { get; set; }

    public decimal BalanceAmount { get; set; }

    /// <summary>True when the request was a retry of an already recorded receipt.</summary>
    public bool IsReplay { get; set; }
}

public static class AdvanceReceiptType
{
    public const string AdvanceReceipt = "Advance Receipt";

    public const string RecurringDeposit = "Recurring Deposit";
}
