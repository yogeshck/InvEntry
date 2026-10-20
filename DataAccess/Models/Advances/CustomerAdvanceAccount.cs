namespace DataAccess.Models;

/// <summary>
/// One customer advance or scheme (RD) enrollment. BALANCE_* and TOTAL_RECEIVED_*
/// are a cache of the ADVANCE_LEDGER rows and are changed only in the same
/// transaction as a ledger insert.
/// </summary>
public partial class CustomerAdvanceAccount
{
    public int Gkey { get; set; }

    public int? TenantGkey { get; set; }

    public int? LocationGkey { get; set; }

    public string AccountNbr { get; set; } = null!;

    /// <summary>SCHEME, ORDER, OPEN or RATE_BOOKING.</summary>
    public string AccountType { get; set; } = null!;

    public int CustGkey { get; set; }

    public int MtblLedgersGkey { get; set; }

    public int? CustomerOrderGkey { get; set; }

    /// <summary>ACTIVE, MATURED, LAPSED, CLOSURE_PENDING or CLOSED.</summary>
    public string Status { get; set; } = null!;

    public DateOnly OpenedOn { get; set; }

    public DateOnly? ClosedOn { get; set; }

    public string? Metal { get; set; }

    public string? Purity { get; set; }

    public decimal? BookedRate { get; set; }

    public DateOnly? ValidUntil { get; set; }

    public decimal BalanceAmount { get; set; }

    public decimal BalanceWeight { get; set; }

    public decimal TotalReceivedAmount { get; set; }

    public decimal TotalReceivedWeight { get; set; }

    public bool IsOpeningBalance { get; set; }

    public string? Remarks { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual SchemeEnrollment? SchemeEnrollment { get; set; }

    public virtual ICollection<AdvanceLedger> LedgerEntries { get; set; } = new List<AdvanceLedger>();

    public virtual ICollection<AdvanceClosure> Closures { get; set; } = new List<AdvanceClosure>();
}
