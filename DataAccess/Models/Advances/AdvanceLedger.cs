namespace DataAccess.Models;

/// <summary>
/// Append-only movement of a customer advance account. Credits to the customer
/// are positive (OPENING, RECEIPT, BONUS); uses of the balance are negative
/// (ADJUSTMENT, REFUND, DEDUCTION, FORFEITURE). Corrections are REVERSAL rows.
/// </summary>
public partial class AdvanceLedger
{
    public long Gkey { get; set; }

    public int AdvanceAccountGkey { get; set; }

    public string EntryType { get; set; } = null!;

    public DateOnly EntryDate { get; set; }

    public decimal Amount { get; set; }

    public decimal Weight { get; set; }

    public decimal? MetalRate { get; set; }

    public int? DailyRateGkey { get; set; }

    public int? VoucherGkey { get; set; }

    public int? InvoiceGkey { get; set; }

    public string? InvoiceNbr { get; set; }

    public int? AdvanceClosureGkey { get; set; }

    public long? ReversesLedgerGkey { get; set; }

    /// <summary>
    /// Idempotency key of the business event, unique across the table.
    /// </summary>
    public string SourceRef { get; set; } = null!;

    public string? Narration { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public virtual CustomerAdvanceAccount AdvanceAccount { get; set; } = null!;

    public virtual ICollection<SchemeInstallmentAllocation> InstallmentAllocations { get; set; } =
        new List<SchemeInstallmentAllocation>();
}

public static class AdvanceLedgerEntryType
{
    public const string Opening = "OPENING";
    public const string Receipt = "RECEIPT";
    public const string Bonus = "BONUS";
    public const string Adjustment = "ADJUSTMENT";
    public const string Refund = "REFUND";
    public const string Deduction = "DEDUCTION";
    public const string Forfeiture = "FORFEITURE";
    public const string Reversal = "REVERSAL";
}
