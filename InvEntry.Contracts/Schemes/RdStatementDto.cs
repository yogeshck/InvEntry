namespace InvEntry.Contracts.Schemes;

/// <summary>
/// An RD passbook / statement: the scheme, its installment schedule and every
/// payment, as of now.
/// </summary>
public class RdStatementDto
{
    public SchemeEnrollmentDto Scheme { get; set; } = new();

    public List<RdStatementPayment> Payments { get; set; } = new();

    /// <summary>Flat bonus of the scheme's terms (FLAT_BONUS rules), in rupees.</summary>
    public decimal BonusAmount { get; set; }

    /// <summary>Installments that must be paid to earn the bonus.</summary>
    public int BonusMinInstallments { get; set; }

    /// <summary>True when the installments needed for the bonus are fully paid.</summary>
    public bool BonusEarned { get; set; }

    /// <summary>Total paid plus the bonus when earned.</summary>
    public decimal MaturityValue { get; set; }

    public DateTime GeneratedOn { get; set; }
}

public class RdStatementPayment
{
    public DateOnly Date { get; set; }

    /// <summary>Voucher number, or the old register's receipt number for imported payments.</summary>
    public string ReceiptNumber { get; set; } = string.Empty;

    public string? Mode { get; set; }

    /// <summary>RECEIPT, OPENING, BONUS, ADJUSTMENT, REFUND, ... (signed amounts).</summary>
    public string EntryType { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public decimal Weight { get; set; }

    public decimal? MetalRate { get; set; }

    /// <summary>Installments the payment went to, e.g. "2, 3 (part)".</summary>
    public string? InstallmentsCovered { get; set; }

    /// <summary>Balance after this entry.</summary>
    public decimal RunningBalance { get; set; }

    public bool IsImported { get; set; }
}
