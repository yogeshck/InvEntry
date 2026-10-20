using DataAccess.Models;
using DataAccess.Services.Advances;
using InvEntry.Contracts.Schemes;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services.Schemes;

public interface ISchemePlanService
{
    Task<IReadOnlyList<SchemePlanDto>> GetPlansAsync(
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<SchemePlanDto?> GetPlanAsync(
        int planGkey,
        CancellationToken cancellationToken = default);

    /// <summary>Creates the plan (Gkey 0) or updates it, replacing its benefit rules.</summary>
    Task<SchemePlanDto> SavePlanAsync(
        SchemePlanDto plan,
        CancellationToken cancellationToken = default);

    /// <summary>Metal / purity pairs in DAILY_RATE, for weight scheme plans.</summary>
    Task<IReadOnlyList<SchemeMetalOption>> GetMetalOptionsAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Scheme plan maintenance. Existing enrollments keep the terms they were
/// enrolled on (TERMS_SNAPSHOT), so editing a plan only affects new enrollments.
/// </summary>
public sealed class SchemePlanService : ISchemePlanService
{
    private static readonly string[] SchemeTypes = { SchemeType.Amount, SchemeType.Weight };
    private static readonly string[] Statuses =
        { SchemePlanStatus.Draft, SchemePlanStatus.Active, SchemePlanStatus.Retired };
    private static readonly string[] RefundBases = { "AMOUNT_PAID", "GOLD_VALUE", "LOWER_OF" };
    private static readonly string[] PreclosureBonuses = { "NONE", "PRORATED" };
    private static readonly string[] AppliesOnValues = { "MATURITY", "PRECLOSURE" };
    private static readonly string[] BenefitTypes =
    {
        SchemeTerms.BenefitInstallmentWaiver,
        SchemeTerms.BenefitPercentBonus,
        SchemeTerms.BenefitMakingChargeWaiver,
        SchemeTerms.BenefitBonusGrams
    };

    private readonly MijmsContext _context;
    private readonly IAuditIdentityProvider _auditIdentityProvider;

    public SchemePlanService(
        MijmsContext context,
        IAuditIdentityProvider auditIdentityProvider)
    {
        _context = context;
        _auditIdentityProvider = auditIdentityProvider;
    }

    public async Task<IReadOnlyList<SchemePlanDto>> GetPlansAsync(
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var query = _context.SchemePlans
            .AsNoTracking()
            .Include(x => x.BenefitRules)
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(x =>
                x.Status == SchemePlanStatus.Active &&
                x.ValidFrom <= today &&
                (x.ValidTo == null || x.ValidTo >= today));
        }

        var plans = await query
            .OrderBy(x => x.PlanCode)
            .ToListAsync(cancellationToken);

        var counts = await _context.SchemeEnrollments
            .AsNoTracking()
            .GroupBy(x => x.SchemePlanGkey)
            .Select(x => new { PlanGkey = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.PlanGkey, x => x.Count, cancellationToken);

        return plans
            .Select(x => SchemeTerms.ToDto(x, counts.GetValueOrDefault(x.Gkey)))
            .ToList();
    }

    public async Task<SchemePlanDto?> GetPlanAsync(
        int planGkey,
        CancellationToken cancellationToken = default)
    {
        var plan = await _context.SchemePlans
            .AsNoTracking()
            .Include(x => x.BenefitRules)
            .FirstOrDefaultAsync(x => x.Gkey == planGkey, cancellationToken);

        if (plan is null)
            return null;

        var count = await _context.SchemeEnrollments
            .CountAsync(x => x.SchemePlanGkey == planGkey, cancellationToken);

        return SchemeTerms.ToDto(plan, count);
    }

    public async Task<SchemePlanDto> SavePlanAsync(
        SchemePlanDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        await ValidateAsync(dto, cancellationToken);

        var identity = _auditIdentityProvider.GetCurrentIdentity();
        var now = DateTime.Now;

        SchemePlan plan;

        if (dto.Gkey == 0)
        {
            plan = new SchemePlan { CreatedBy = identity, CreatedOn = now };
            _context.SchemePlans.Add(plan);
        }
        else
        {
            plan = await _context.SchemePlans
                       .Include(x => x.BenefitRules)
                       .FirstOrDefaultAsync(x => x.Gkey == dto.Gkey, cancellationToken)
                   ?? throw new AdvanceAccountBusinessException($"Scheme plan {dto.Gkey} was not found.");

            plan.ModifiedBy = identity;
            plan.ModifiedOn = now;

            _context.SchemeBenefitRules.RemoveRange(plan.BenefitRules);
            plan.BenefitRules.Clear();
        }

        plan.PlanCode = dto.PlanCode.Trim().ToUpperInvariant();
        plan.PlanName = dto.PlanName.Trim();
        plan.SchemeType = dto.SchemeType;
        plan.Metal = dto.SchemeType == SchemeType.Weight ? dto.Metal?.Trim() : null;
        plan.Purity = dto.SchemeType == SchemeType.Weight ? dto.Purity?.Trim() : null;
        plan.TenureMonths = dto.TenureMonths;
        plan.AmountFixed = dto.AmountFixed;
        plan.MinInstallmentAmount = dto.MinInstallmentAmount;
        plan.MaxInstallmentAmount = dto.MaxInstallmentAmount;
        plan.GraceDays = dto.GraceDays;
        plan.MaxMissedForBonus = dto.MaxMissedForBonus;
        plan.MaxMissedBeforeLapse = dto.MaxMissedBeforeLapse;
        plan.MinMonthsForRefund = dto.MinMonthsForRefund;
        plan.RefundDeductionPct = dto.RefundDeductionPct;
        plan.RefundBasis = dto.RefundBasis;
        plan.PreclosureBonus = dto.PreclosureBonus;
        plan.ValidFrom = dto.ValidFrom;
        plan.ValidTo = dto.ValidTo;
        plan.Status = dto.Status;
        plan.TermsText = string.IsNullOrWhiteSpace(dto.TermsText) ? null : dto.TermsText.Trim();

        var seq = 1;
        foreach (var rule in dto.BenefitRules)
        {
            plan.BenefitRules.Add(new SchemeBenefitRule
            {
                SeqNbr = seq++,
                BenefitType = rule.BenefitType,
                BenefitValue = rule.BenefitValue,
                AppliesOn = rule.AppliesOn,
                MinInstallmentsPaid = rule.MinInstallmentsPaid,
                EligibleCategories = string.IsNullOrWhiteSpace(rule.EligibleCategories)
                    ? null
                    : rule.EligibleCategories.Trim(),
                IsActive = rule.IsActive
            });
        }

        // One SaveChanges: the old rules are deleted and the new ones inserted atomically.
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetPlanAsync(plan.Gkey, cancellationToken))!;
    }

    public async Task<IReadOnlyList<SchemeMetalOption>> GetMetalOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var rates = await _context.DailyRates
            .AsNoTracking()
            .Where(x => x.Metal != null && x.Purity != null)
            .Select(x => new { x.Gkey, x.Metal, x.Purity, x.Carat, x.Price, x.EffectiveDate })
            .ToListAsync(cancellationToken);

        return rates
            .GroupBy(x => new { Metal = x.Metal!.Trim(), Purity = x.Purity!.Trim() })
            .Select(g =>
            {
                var latest = g.OrderByDescending(x => x.EffectiveDate).ThenByDescending(x => x.Gkey).First();
                return new SchemeMetalOption
                {
                    Metal = g.Key.Metal,
                    Purity = g.Key.Purity,
                    Carat = latest.Carat,
                    LatestPrice = latest.Price
                };
            })
            .OrderBy(x => x.Metal)
            .ThenBy(x => x.Purity)
            .ToList();
    }

    private async Task ValidateAsync(
        SchemePlanDto dto,
        CancellationToken cancellationToken)
    {
        static void Require(bool condition, string message)
        {
            if (!condition)
                throw new AdvanceAccountBusinessException(message);
        }

        Require(!string.IsNullOrWhiteSpace(dto.PlanCode), "Plan code is required.");
        Require(dto.PlanCode.Trim().Length <= 30, "Plan code can have at most 30 characters.");
        Require(!string.IsNullOrWhiteSpace(dto.PlanName), "Plan name is required.");
        Require(dto.PlanName.Trim().Length <= 100, "Plan name can have at most 100 characters.");
        Require(SchemeTypes.Contains(dto.SchemeType), "Scheme type must be AMOUNT or WEIGHT.");
        Require(dto.TenureMonths is >= 1 and <= 12,
            "Tenure must be between 1 and 12 months (advances must be adjusted within 365 days).");
        Require(dto.MinInstallmentAmount > 0M, "Minimum installment must be greater than zero.");
        Require(dto.MaxInstallmentAmount is null || dto.MaxInstallmentAmount >= dto.MinInstallmentAmount,
            "Maximum installment cannot be less than the minimum.");
        Require(dto.GraceDays >= 0 && dto.MaxMissedForBonus >= 0 && dto.MinMonthsForRefund >= 0 &&
                (dto.MaxMissedBeforeLapse is null || dto.MaxMissedBeforeLapse >= 0),
            "Grace days and missed / refund month limits cannot be negative.");
        Require(dto.RefundDeductionPct is >= 0M and <= 100M, "Refund deduction must be between 0 and 100 %.");
        Require(RefundBases.Contains(dto.RefundBasis), "Refund basis must be AMOUNT_PAID, GOLD_VALUE or LOWER_OF.");
        Require(PreclosureBonuses.Contains(dto.PreclosureBonus), "Pre-closure bonus must be NONE or PRORATED.");
        Require(dto.ValidFrom != default, "Valid from date is required.");
        Require(dto.ValidTo is null || dto.ValidTo >= dto.ValidFrom, "Valid to cannot be before valid from.");
        Require(Statuses.Contains(dto.Status), "Status must be DRAFT, ACTIVE or RETIRED.");

        if (dto.SchemeType == SchemeType.Weight)
        {
            Require(!string.IsNullOrWhiteSpace(dto.Metal) && !string.IsNullOrWhiteSpace(dto.Purity),
                "A weight scheme needs the metal and purity whose daily rate converts payments to grams.");

            var metal = dto.Metal!.Trim();
            var purity = dto.Purity!.Trim();

            Require(await _context.DailyRates.AnyAsync(x => x.Metal == metal && x.Purity == purity, cancellationToken),
                $"No daily rate exists for {metal} {purity}.");
        }
        else
        {
            Require(dto.BenefitRules.All(x => x.BenefitType != SchemeTerms.BenefitBonusGrams),
                "Bonus grams can only be given on a weight scheme.");
        }

        foreach (var rule in dto.BenefitRules)
        {
            Require(BenefitTypes.Contains(rule.BenefitType),
                $"Unknown benefit type '{rule.BenefitType}'.");
            Require(rule.BenefitValue > 0M, "Every benefit needs a value greater than zero.");
            Require(rule.BenefitType is not (SchemeTerms.BenefitPercentBonus or SchemeTerms.BenefitMakingChargeWaiver)
                    || rule.BenefitValue <= 100M,
                "A percentage benefit cannot exceed 100 %.");
            Require(AppliesOnValues.Contains(rule.AppliesOn), "A benefit applies on MATURITY or PRECLOSURE.");
            Require(rule.MinInstallmentsPaid >= 0 && rule.MinInstallmentsPaid <= dto.TenureMonths,
                "Minimum installments paid for a benefit must be between 0 and the tenure.");
        }

        var code = dto.PlanCode.Trim().ToUpperInvariant();
        Require(!await _context.SchemePlans.AnyAsync(x => x.PlanCode == code && x.Gkey != dto.Gkey, cancellationToken),
            $"Plan code {code} is already used.");
    }
}
