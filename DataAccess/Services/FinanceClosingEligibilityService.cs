using DataAccess.Models;
using InvEntry.Contracts.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data.Common;

namespace DataAccess.Services;

public sealed record FinanceClosingEligibilityResult(
    int OrgGkey,
    int LocationGkey,
    DateOnly BusinessDate,
    string Decision,
    bool ScopeVerified,
    bool CoverageComplete,
    int RequiredCount,
    int SynchronizedCount,
    int PendingCount,
    int RetryableCount,
    int UncertainCount,
    int BusinessErrorCount,
    int HistoricalErrorCount,
    int MissingOutboxCount,
    int UnresolvedEarlierCount,
    IReadOnlyList<string> ReasonCodes,
    DateTime EvaluatedAtUtc);

public sealed class FinanceClosingEligibilityService
{
    private const int MaximumEvaluationDays = 3660;
    private const int MaximumDocuments = 10_000;
    private readonly MijmsContext _context;
    private readonly FinanceSyncOptions _options;
    private readonly IFinanceClosingFinancialEvidenceReader _financialEvidence;

    public FinanceClosingEligibilityService(MijmsContext context,
        IOptions<FinanceSyncOptions> options,
        IFinanceClosingFinancialEvidenceReader financialEvidence) =>
        (_context, _options, _financialEvidence) =
            (context, options.Value, financialEvidence);

    public (int OrgGkey, int LocationGkey) Scope =>
        (_options.OrgGkey, _options.LocationGkey);

    public async Task<FinanceClosingEligibilityResult> EvaluateAsync(
        int orgGkey, int locationGkey, DateOnly businessDate,
        CancellationToken cancellationToken = default)
    {
        if (businessDate > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            throw new ArgumentException("The business date is outside the permitted range.");

        var reasons = new HashSet<string>(StringComparer.Ordinal);
        var scopeVerified = _options.ClosingEligibilityScopeVerified &&
            Scope == (orgGkey, locationGkey);
        if (!scopeVerified) reasons.Add("SCOPE_UNVERIFIED");

        var coverageStart = _options.ClosingEligibilityCoverageStartDate;
        var coverageComplete = coverageStart.HasValue &&
            coverageStart.Value <= businessDate &&
            businessDate.DayNumber - coverageStart.Value.DayNumber <= MaximumEvaluationDays;
        if (!coverageStart.HasValue) reasons.Add("COVERAGE_START_UNVERIFIED");
        else if (coverageStart > businessDate) reasons.Add("DATE_OUTSIDE_VERIFIED_COVERAGE");
        else if (!coverageComplete) reasons.Add("COVERAGE_WINDOW_EXCEEDED");

        if (!scopeVerified || !coverageComplete)
            return Result("INDETERMINATE", scopeVerified, coverageComplete,
                orgGkey, locationGkey, businessDate, reasons);

        var from = coverageStart!.Value.ToDateTime(TimeOnly.MinValue);
        var through = businessDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var invoices = await _context.InvoiceHeaders.AsNoTracking()
            .Where(x => x.InvDate >= from && x.InvDate < through &&
                (x.Status == InvoiceStatus.Final || x.Status == "FINALISED"))
            .OrderBy(x => x.Gkey)
            .Take(MaximumDocuments + 1)
            .Select(x => new { x.Gkey, x.InvNbr, x.InvDate })
            .ToListAsync(cancellationToken);
        if (invoices.Count > MaximumDocuments)
        {
            reasons.Add("DOCUMENT_LIMIT_EXCEEDED");
            return Result("INDETERMINATE", scopeVerified, false,
                orgGkey, locationGkey, businessDate, reasons);
        }

        var invoiceIds = invoices.Select(x => x.Gkey).ToArray();
        var vouchers = await _context.Vouchers.AsNoTracking()
            .Where(x => x.RefDocGkey.HasValue && invoiceIds.Contains(x.RefDocGkey.Value))
            .Select(x => new Settlement(
                x.RefDocGkey!.Value, x.TransType, x.VoucherType, x.Mode,
                x.TransAmount))
            .ToListAsync(cancellationToken);
        var voucherGroups = vouchers.GroupBy(x => x.InvoiceGkey)
            .ToDictionary(x => x.Key, x => x.ToList());

        var required = new HashSet<int>();
        var unknownEligibility = false;
        foreach (var invoice in invoices)
        {
            if (!voucherGroups.TryGetValue(invoice.Gkey, out var settlements) || settlements.Count == 0)
                continue; // A finalized credit-only invoice has no settlement movement.

            var hasMappedMovement = settlements.Any(IsFundMovement);
            var hasUnmappedMovement = settlements.Any(x =>
                !IsFundMovement(x) && !IsNonCashSettlement(x));
            if (hasMappedMovement) required.Add(invoice.Gkey);
            if (hasUnmappedMovement) unknownEligibility = true;
        }
        if (unknownEligibility)
        {
            coverageComplete = false;
            reasons.Add("UNKNOWN_FINANCIAL_ELIGIBILITY");
        }

        var outboxes = await _context.FinanceSyncOutboxes.AsNoTracking()
            .Where(x => x.SourceType == "INVOICE" && required.Contains(x.SourceGkey))
            .Select(x => new { x.SourceGkey, x.SourceEventId, x.Status })
            .ToListAsync(cancellationToken);
        var byInvoice = outboxes.GroupBy(x => x.SourceGkey)
            .ToDictionary(x => x.Key, x => x.ToList());

        var requirements = invoices.Where(x => required.Contains(x.Gkey))
            .Select(x => new FinanceClosingInvoiceRequirement(
                x.Gkey, x.InvNbr!, DateOnly.FromDateTime(x.InvDate!.Value),
                $"INVOICE-{x.Gkey}-FINAL",
                voucherGroups[x.Gkey].Where(IsFundMovement)
                    .Select(ToMovement).ToList()))
            .ToList();
        IReadOnlyDictionary<int, FinanceClosingEvidenceDecision> financialEvidence;
        try
        {
            financialEvidence = await _financialEvidence.ReadAsync(orgGkey,
                locationGkey, requirements, cancellationToken);
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException)
        {
            reasons.Add("FINANCIAL_EVIDENCE_UNAVAILABLE");
            coverageComplete = false;
            financialEvidence = new Dictionary<int, FinanceClosingEvidenceDecision>();
        }

        var synchronized = 0;
        var pending = 0;
        var retryable = 0;
        var uncertain = 0;
        var businessError = 0;
        var historicalError = 0;
        var missing = 0;
        var unknownStatus = false;
        var unresolvedEarlier = 0;
        var ambiguousEvidence = false;
        var mismatchedEvidence = false;

        foreach (var invoice in invoices.Where(x => required.Contains(x.Gkey)))
        {
            var expectedEventId = $"INVOICE-{invoice.Gkey}-FINAL";
            var evidence = financialEvidence.GetValueOrDefault(invoice.Gkey,
                FinanceClosingEvidenceDecision.Missing);
            if (evidence == FinanceClosingEvidenceDecision.Verified)
            {
                synchronized++;
                continue;
            }
            if (evidence == FinanceClosingEvidenceDecision.Ambiguous)
                ambiguousEvidence = true;
            if (evidence == FinanceClosingEvidenceDecision.Mismatched)
                mismatchedEvidence = true;

            var matching = byInvoice.GetValueOrDefault(invoice.Gkey)?
                .SingleOrDefault(x => x.SourceEventId == expectedEventId);
            var isEarlier = DateOnly.FromDateTime(invoice.InvDate!.Value) < businessDate;
            if (matching == null)
            {
                missing++;
                if (isEarlier) unresolvedEarlier++;
                continue;
            }

            switch (matching.Status)
            {
                case FinanceSyncService.Sent: synchronized++; break;
                case FinanceSyncService.Pending: pending++; if (isEarlier) unresolvedEarlier++; break;
                case FinanceSyncService.RetryableError: retryable++; if (isEarlier) unresolvedEarlier++; break;
                case FinanceSyncService.Uncertain: uncertain++; if (isEarlier) unresolvedEarlier++; break;
                case FinanceSyncService.BusinessError: businessError++; if (isEarlier) unresolvedEarlier++; break;
                case "ERROR": historicalError++; if (isEarlier) unresolvedEarlier++; break;
                default: unknownStatus = true; if (isEarlier) unresolvedEarlier++; break;
            }
        }

        AddReason(reasons, pending > 0, "PENDING_EVENTS");
        AddReason(reasons, retryable > 0, "RETRYABLE_EVENTS");
        AddReason(reasons, uncertain > 0, "UNCERTAIN_EVENTS");
        AddReason(reasons, businessError > 0, "BUSINESS_ERROR_EVENTS");
        AddReason(reasons, historicalError > 0, "HISTORICAL_ERROR_EVENTS");
        AddReason(reasons, missing > 0, "MISSING_OUTBOX");
        AddReason(reasons, unresolvedEarlier > 0, "UNRESOLVED_EARLIER_EVENTS");
        AddReason(reasons, unknownStatus, "UNKNOWN_OUTBOX_STATUS");
        AddReason(reasons, ambiguousEvidence, "AMBIGUOUS_FINANCIAL_EVIDENCE");
        AddReason(reasons, mismatchedEvidence, "MISMATCHED_FINANCIAL_EVIDENCE");

        var decision = unknownEligibility || unknownStatus
            ? "INDETERMINATE"
            : reasons.Count == 0 ? "CLEAR" : "BLOCKED";
        return new(orgGkey, locationGkey, businessDate, decision, scopeVerified,
            coverageComplete, required.Count, synchronized, pending, retryable,
            uncertain, businessError, historicalError, missing,
            unresolvedEarlier, reasons.Order().ToArray(), DateTime.UtcNow);
    }

    private static bool IsFundMovement(Settlement voucher)
    {
        static bool Eq(string? value, string expected) =>
            string.Equals(value?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
        return voucher.Amount is > 0 &&
            (Eq(voucher.TransType, "Receipt") && Eq(voucher.Mode, "Cash") ||
             Eq(voucher.TransType, "Payment") &&
             Eq(voucher.VoucherType, "Refund") && Eq(voucher.Mode, "Cash"));
    }

    private static bool IsNonCashSettlement(Settlement voucher)
    {
        static bool Eq(string? value, string expected) =>
            string.Equals(value?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
        return Eq(voucher.TransType, "Journal") &&
                Eq(voucher.VoucherType, "OM Purchase") ||
            Eq(voucher.TransType, "Receipt") &&
                Eq(voucher.VoucherType, "Bank Scanner") &&
                Eq(voucher.Mode, "Bank Scanner");
    }

    private static FinanceClosingMovement ToMovement(Settlement voucher)
    {
        static bool Eq(string? value, string expected) =>
            string.Equals(value?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
        var direction = Eq(voucher.TransType, "Receipt") ? "IN" : "OUT";
        var mode = voucher.Mode?.Trim().ToUpperInvariant() switch
        {
            "BANK SCANNER" => "UPI",
            "BANK TRANSFER" => "BANK",
            var value => value ?? string.Empty
        };
        return new(direction, mode, voucher.Amount!.Value);
    }

    private static void AddReason(ISet<string> reasons, bool condition, string reason)
    {
        if (condition) reasons.Add(reason);
    }

    private static FinanceClosingEligibilityResult Result(string decision,
        bool scopeVerified, bool coverageComplete, int org, int location,
        DateOnly date, IReadOnlyCollection<string> reasons) =>
        new(org, location, date, decision, scopeVerified, coverageComplete,
            0, 0, 0, 0, 0, 0, 0, 0, 0,
            reasons.Order().ToArray(), DateTime.UtcNow);

    private sealed record Settlement(int InvoiceGkey, string? TransType,
        string? VoucherType, string? Mode, decimal? Amount);
}
