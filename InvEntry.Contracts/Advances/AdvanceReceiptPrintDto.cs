namespace InvEntry.Contracts.Advances;

/// <summary>
/// Everything printed on an advance / RD installment receipt. Balances and
/// installment figures are as they stood right after this receipt, so a
/// reprint shows the same numbers as the original.
/// </summary>
public class AdvanceReceiptPrintDto
{
    public int VoucherGkey { get; set; }

    public string VoucherNbr { get; set; } = string.Empty;

    public DateTime? VoucherDate { get; set; }

    /// <summary>"Advance Receipt" or "Recurring Deposit".</summary>
    public string ReceiptType { get; set; } = string.Empty;

    public string? Mode { get; set; }

    public decimal Amount { get; set; }

    public string? Notes { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerMobile { get; set; }

    public string AccountNbr { get; set; } = string.Empty;

    /// <summary>SCHEME, ORDER, OPEN or RATE_BOOKING.</summary>
    public string AccountType { get; set; } = string.Empty;

    public string? CustomerOrderNbr { get; set; }

    /// <summary>Account balance right after this receipt.</summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>Total received into the account up to and including this receipt.</summary>
    public decimal TotalReceivedToDate { get; set; }

    // Weight schemes

    public decimal Weight { get; set; }

    public decimal? MetalRate { get; set; }

    public string? Metal { get; set; }

    public string? Purity { get; set; }

    public decimal WeightBalanceAfter { get; set; }

    // Schemes

    public bool IsScheme { get; set; }

    public string? PlanCode { get; set; }

    public string? PlanName { get; set; }

    public decimal? InstallmentAmount { get; set; }

    public int? TenureMonths { get; set; }

    /// <summary>Installments this receipt paid, e.g. "2, 3 (part)".</summary>
    public string? InstallmentsCovered { get; set; }

    /// <summary>Installments fully paid right after this receipt.</summary>
    public int? InstallmentsPaid { get; set; }

    public DateOnly? NextDueDate { get; set; }

    public decimal? NextDueAmount { get; set; }

    public DateOnly? MaturityDate { get; set; }

    public string? NomineeName { get; set; }

    /// <summary>True when the receipt has been reversed / cancelled.</summary>
    public bool IsReversed { get; set; }
}
