namespace InvEntry.Contracts.Schemes;

/// <summary>
/// A savings scheme (RD) plan. Enrollments copy the plan and its benefit rules
/// at enrollment time, so later edits never change an existing enrollment.
/// </summary>
public class SchemePlanDto
{
    public int Gkey { get; set; }

    public string PlanCode { get; set; } = string.Empty;

    public string PlanName { get; set; } = string.Empty;

    /// <summary><see cref="SchemeType.Amount"/> or <see cref="SchemeType.Weight"/>.</summary>
    public string SchemeType { get; set; } = Schemes.SchemeType.Amount;

    /// <summary>DAILY_RATE.METAL of the rate used for weight schemes, e.g. GOLD.</summary>
    public string? Metal { get; set; }

    /// <summary>DAILY_RATE.PURITY of the rate used for weight schemes, e.g. 916.</summary>
    public string? Purity { get; set; }

    /// <summary>1 to 12 months.</summary>
    public int TenureMonths { get; set; } = 12;

    /// <summary>
    /// True: every month the enrolled installment amount is due and payments
    /// are spread over the installments in order. False: each payment pays one
    /// month and may be any amount between the minimum and maximum.
    /// </summary>
    public bool AmountFixed { get; set; } = true;

    public decimal MinInstallmentAmount { get; set; }

    public decimal? MaxInstallmentAmount { get; set; }

    public int GraceDays { get; set; }

    public int MaxMissedForBonus { get; set; }

    public int? MaxMissedBeforeLapse { get; set; }

    public int MinMonthsForRefund { get; set; }

    public decimal RefundDeductionPct { get; set; }

    /// <summary>AMOUNT_PAID, GOLD_VALUE or LOWER_OF.</summary>
    public string RefundBasis { get; set; } = "AMOUNT_PAID";

    /// <summary>NONE or PRORATED.</summary>
    public string PreclosureBonus { get; set; } = "NONE";

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    /// <summary>DRAFT, ACTIVE or RETIRED. Only ACTIVE plans take new enrollments.</summary>
    public string Status { get; set; } = SchemePlanStatus.Draft;

    public string? TermsText { get; set; }

    public int EnrollmentCount { get; set; }

    public List<SchemeBenefitRuleDto> BenefitRules { get; set; } = new();
}

public class SchemeBenefitRuleDto
{
    public int Gkey { get; set; }

    public int SeqNbr { get; set; }

    /// <summary>INSTALLMENT_WAIVER, PERCENT_BONUS, MAKING_CHARGE_WAIVER, BONUS_GRAMS or FLAT_BONUS.</summary>
    public string BenefitType { get; set; } = string.Empty;

    /// <summary>Installments, percent, grams or rupees (FLAT_BONUS), by benefit type.</summary>
    public decimal BenefitValue { get; set; }

    /// <summary>MATURITY or PRECLOSURE.</summary>
    public string AppliesOn { get; set; } = "MATURITY";

    public int MinInstallmentsPaid { get; set; }

    /// <summary>Comma-separated product category codes; empty = all.</summary>
    public string? EligibleCategories { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>A metal / purity pair available in DAILY_RATE.</summary>
public class SchemeMetalOption
{
    public string Metal { get; set; } = string.Empty;

    public string Purity { get; set; } = string.Empty;

    public string? Carat { get; set; }

    public decimal? LatestPrice { get; set; }
}

public static class SchemeType
{
    public const string Amount = "AMOUNT";
    public const string Weight = "WEIGHT";
}

public static class SchemePlanStatus
{
    public const string Draft = "DRAFT";
    public const string Active = "ACTIVE";
    public const string Retired = "RETIRED";
}
