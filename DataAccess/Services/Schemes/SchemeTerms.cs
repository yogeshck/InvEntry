using System.Text.Json;
using DataAccess.Models;
using InvEntry.Contracts.Schemes;

namespace DataAccess.Services.Schemes;

/// <summary>
/// Converts plans to their DTO / TERMS_SNAPSHOT form and builds the
/// installment schedule. Shared by plan maintenance, enrollment and receipts.
/// </summary>
public static class SchemeTerms
{
    public const string BenefitInstallmentWaiver = "INSTALLMENT_WAIVER";
    public const string BenefitPercentBonus = "PERCENT_BONUS";
    public const string BenefitMakingChargeWaiver = "MAKING_CHARGE_WAIVER";
    public const string BenefitBonusGrams = "BONUS_GRAMS";

    public const string InstallmentPending = "PENDING";
    public const string InstallmentPartial = "PARTIAL";
    public const string InstallmentPaid = "PAID";
    public const string InstallmentOverdue = "OVERDUE";
    public const string InstallmentWaived = "WAIVED";

    private static readonly JsonSerializerOptions SnapshotJson = new()
    {
        WriteIndented = false
    };

    public static SchemePlanDto ToDto(SchemePlan plan, int enrollmentCount = 0) => new()
    {
        Gkey = plan.Gkey,
        PlanCode = plan.PlanCode,
        PlanName = plan.PlanName,
        SchemeType = plan.SchemeType,
        Metal = plan.Metal,
        Purity = plan.Purity,
        TenureMonths = plan.TenureMonths,
        AmountFixed = plan.AmountFixed,
        MinInstallmentAmount = plan.MinInstallmentAmount,
        MaxInstallmentAmount = plan.MaxInstallmentAmount,
        GraceDays = plan.GraceDays,
        MaxMissedForBonus = plan.MaxMissedForBonus,
        MaxMissedBeforeLapse = plan.MaxMissedBeforeLapse,
        MinMonthsForRefund = plan.MinMonthsForRefund,
        RefundDeductionPct = plan.RefundDeductionPct,
        RefundBasis = plan.RefundBasis,
        PreclosureBonus = plan.PreclosureBonus,
        ValidFrom = plan.ValidFrom,
        ValidTo = plan.ValidTo,
        Status = plan.Status,
        TermsText = plan.TermsText,
        EnrollmentCount = enrollmentCount,
        BenefitRules = plan.BenefitRules
            .OrderBy(x => x.SeqNbr)
            .Select(x => new SchemeBenefitRuleDto
            {
                Gkey = x.Gkey,
                SeqNbr = x.SeqNbr,
                BenefitType = x.BenefitType,
                BenefitValue = x.BenefitValue,
                AppliesOn = x.AppliesOn,
                MinInstallmentsPaid = x.MinInstallmentsPaid,
                EligibleCategories = x.EligibleCategories,
                IsActive = x.IsActive
            })
            .ToList()
    };

    /// <summary>The plan as agreed at enrollment (SCHEME_ENROLLMENT.TERMS_SNAPSHOT).</summary>
    public static string Snapshot(SchemePlan plan)
    {
        var terms = ToDto(plan);
        terms.EnrollmentCount = 0;
        return JsonSerializer.Serialize(terms, SnapshotJson);
    }

    public static SchemePlanDto ReadSnapshot(string termsSnapshot) =>
        JsonSerializer.Deserialize<SchemePlanDto>(termsSnapshot, SnapshotJson)
        ?? throw new InvalidOperationException("The scheme terms snapshot is empty.");

    /// <summary>
    /// Monthly due dates from the start date. Each date is StartDate plus
    /// (n - 1) months, so a 31st start falls due on the last day of shorter
    /// months and returns to the 31st afterwards.
    /// </summary>
    public static IReadOnlyList<(int SeqNbr, DateOnly DueDate)> Schedule(
        DateOnly startDate,
        int tenureMonths) =>
        Enumerable.Range(1, tenureMonths)
            .Select(seq => (seq, startDate.AddMonths(seq - 1)))
            .ToList();

    public static DateOnly MaturityDate(DateOnly startDate, int tenureMonths) =>
        startDate.AddMonths(tenureMonths);

    /// <summary>Grams bought by an amount at a rate, truncated (never rounded up) to 3 decimals.</summary>
    public static decimal Grams(decimal amount, decimal ratePerGram) =>
        ratePerGram <= 0M
            ? 0M
            : decimal.Floor(amount / ratePerGram * 1000M) / 1000M;
}
