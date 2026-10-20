namespace DataAccess.Models;

public partial class SchemePlan
{
    public int Gkey { get; set; }

    public int? TenantGkey { get; set; }

    public string PlanCode { get; set; } = null!;

    public string PlanName { get; set; } = null!;

    /// <summary>AMOUNT or WEIGHT.</summary>
    public string SchemeType { get; set; } = null!;

    public string? Metal { get; set; }

    public string? Purity { get; set; }

    public int TenureMonths { get; set; }

    public bool AmountFixed { get; set; }

    public decimal MinInstallmentAmount { get; set; }

    public decimal? MaxInstallmentAmount { get; set; }

    public int GraceDays { get; set; }

    public int MaxMissedForBonus { get; set; }

    public int? MaxMissedBeforeLapse { get; set; }

    public int MinMonthsForRefund { get; set; }

    public decimal RefundDeductionPct { get; set; }

    /// <summary>AMOUNT_PAID, GOLD_VALUE or LOWER_OF.</summary>
    public string RefundBasis { get; set; } = null!;

    /// <summary>NONE or PRORATED.</summary>
    public string PreclosureBonus { get; set; } = null!;

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    /// <summary>DRAFT, ACTIVE or RETIRED.</summary>
    public string Status { get; set; } = null!;

    public string? TermsText { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public virtual ICollection<SchemeBenefitRule> BenefitRules { get; set; } = new List<SchemeBenefitRule>();

    public virtual ICollection<SchemeEnrollment> Enrollments { get; set; } = new List<SchemeEnrollment>();
}

public partial class SchemeBenefitRule
{
    public int Gkey { get; set; }

    public int SchemePlanGkey { get; set; }

    public int SeqNbr { get; set; }

    /// <summary>INSTALLMENT_WAIVER, PERCENT_BONUS, MAKING_CHARGE_WAIVER or BONUS_GRAMS.</summary>
    public string BenefitType { get; set; } = null!;

    public decimal BenefitValue { get; set; }

    /// <summary>MATURITY or PRECLOSURE.</summary>
    public string AppliesOn { get; set; } = null!;

    public int MinInstallmentsPaid { get; set; }

    public string? EligibleCategories { get; set; }

    public bool IsActive { get; set; }

    public virtual SchemePlan SchemePlan { get; set; } = null!;
}
