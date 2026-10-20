namespace DataAccess.Models;

public partial class AdvanceClosure
{
    public int Gkey { get; set; }

    public int AdvanceAccountGkey { get; set; }

    /// <summary>MATURITY_REDEMPTION, PRECLOSURE_REDEMPTION, PRECLOSURE_REFUND or ADVANCE_REFUND.</summary>
    public string ClosureType { get; set; } = null!;

    /// <summary>REQUESTED, APPROVED, COMPLETED or CANCELLED.</summary>
    public string Status { get; set; } = null!;

    public DateOnly ClosureDate { get; set; }

    public decimal TotalPaidAmount { get; set; }

    public decimal TotalPaidWeight { get; set; }

    public decimal? MetalRate { get; set; }

    public int? DailyRateGkey { get; set; }

    public decimal? MetalValue { get; set; }

    public decimal BonusAmount { get; set; }

    public decimal BonusWeight { get; set; }

    public decimal MakingWaiverPct { get; set; }

    public decimal DeductionAmount { get; set; }

    public decimal NetAmount { get; set; }

    public decimal NetWeight { get; set; }

    public string? RefundMode { get; set; }

    public int? RefundVoucherGkey { get; set; }

    /// <summary>JSON line-by-line calculation.</summary>
    public string? CalcBreakdown { get; set; }

    public string RequestedBy { get; set; } = null!;

    public DateTime RequestedOn { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedOn { get; set; }

    public DateTime? CompletedOn { get; set; }

    public string? CancelReason { get; set; }

    public virtual CustomerAdvanceAccount AdvanceAccount { get; set; } = null!;
}
