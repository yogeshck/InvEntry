using DataAccess.Models;
using InvEntry.Contracts.Gst;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public sealed class HistoricalMigrationPreviewService : IHistoricalMigrationPreviewService
{
    private const decimal Tolerance = 0.01m;
    private readonly MijmsContext _context;
    private readonly IHistoricalInvoiceAuditService _audit;
    private readonly IGstr1StagingService _staging;
    private readonly IHistoricalMigrationPreviewTokenStore _tokens;

    public HistoricalMigrationPreviewService(MijmsContext context, IHistoricalInvoiceAuditService audit,
        IGstr1StagingService staging, IHistoricalMigrationPreviewTokenStore tokens)
    {
        _context = context;
        _audit = audit;
        _staging = staging;
        _tokens = tokens;
    }

    public async Task<HistoricalMigrationPreviewResponse> PreviewAsync(
        HistoricalMigrationPreviewRequest request, string administratorIdentity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var current = await _audit.GetAsync(request.SupplierGstin, request.FromDate, request.ToDate, cancellationToken);
        var prior = (request.PriorAuditCandidates ?? new()).GroupBy(x => CandidateKey(x.InvoiceNumber, x.InvoiceDate))
            .ToDictionary(x => x.Key, x => x.Count() == 1 ? x.Single().SourceFingerprint : string.Empty,
                StringComparer.OrdinalIgnoreCase);
        var response = new HistoricalMigrationPreviewResponse
        {
            SupplierGstin = current.SupplierGstin, FromDate = current.FromDate,
            ToDate = current.ToDate, TotalAudited = current.Items.Count
        };

        foreach (var item in current.Items)
        {
            var eligibility = Assess(item);
            if (eligibility == "AlreadyStaged") response.AlreadyStaged++;
            if (eligibility == "QualifiedForStaging") response.Qualified++;

            var result = new HistoricalMigrationPreviewItem
            {
                InvoiceNumber = item.InvoiceNbr ?? string.Empty, InvoiceDate = item.InvoiceDate,
                Customer = item.CustomerName, GstReturnCategory = item.GstReturnCategory,
                SupplyType = item.SupplyType, TaxableAmount = item.TaxableAmount,
                CgstAmount = item.CgstAmount, SgstAmount = item.SgstAmount,
                IgstAmount = item.IgstAmount, TotalGst = item.TaxTotal,
                SourceFingerprint = item.SourceFingerprint
            };

            if (eligibility == "AlreadyStaged")
            {
                result.PreviewResult = "ALREADY STAGED";
                result.Message = "An existing GSTR-1 staging record was found.";
            }
            else if (eligibility != "QualifiedForStaging")
            {
                result.PreviewResult = "NOT QUALIFIED";
                result.Message = $"Current source assessment is {Display(eligibility)}.";
                response.Excluded++;
            }
            else if (!item.InvoiceDate.HasValue || !prior.TryGetValue(CandidateKey(item.InvoiceNbr, item.InvoiceDate.Value), out var fingerprint) ||
                     string.IsNullOrWhiteSpace(fingerprint) || !string.Equals(fingerprint, item.SourceFingerprint, StringComparison.Ordinal))
            {
                result.PreviewResult = "SOURCE DATA CHANGED - REVIEW REQUIRED";
                result.Message = "Current source evidence does not match the latest client audit. Run the audit again before previewing.";
                response.Excluded++;
            }
            else
            {
                try
                {
                    var header = await _context.InvoiceHeaders.AsNoTracking()
                        .SingleAsync(x => x.Gkey == item.InvoiceGkey, cancellationToken);
                    var lines = await _context.InvoiceLines.AsNoTracking()
                        .Where(x => x.InvoiceHdrGkey == item.InvoiceGkey).ToListAsync(cancellationToken);
                    var prepared = _staging.PrepareInvoice(header, lines);
                    result.GstReturnCategory = prepared.ReturnCategory;
                    result.Gstr1Table = prepared.Gstr1Table;
                    result.SupplyType = prepared.SupplyType;
                    result.TaxableAmount = prepared.TaxableValue;
                    result.CgstAmount = prepared.CgstAmount;
                    result.SgstAmount = prepared.SgstAmount;
                    result.IgstAmount = prepared.IgstAmount;
                    result.TotalGst = prepared.CgstAmount + prepared.SgstAmount + prepared.IgstAmount;
                    result.PreviewResult = "READY FOR STAGING";
                    result.Message = "Preview generated from the current staging transformation. No record was created.";
                    response.EligibleForStagingPreview++;
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    result.PreviewResult = "STAGING VALIDATION ERROR";
                    result.Message = ex.Message;
                    response.PreviewValidationErrors++;
                    response.Excluded++;
                }
            }
            response.Items.Add(result);
        }
        var ready = response.Items.Where(x => x.PreviewResult == "READY FOR STAGING" && x.InvoiceDate.HasValue)
            .Select(x => new HistoricalMigrationTokenCandidate(x.InvoiceNumber, x.InvoiceDate!.Value, x.SourceFingerprint)).ToList();
        if (ready.Count > 0)
        {
            var issued = _tokens.Issue(administratorIdentity, response.SupplierGstin,
                response.FromDate, response.ToDate, response.Excluded, ready);
            response.PreviewToken = issued.Token;
            response.PreviewTokenExpiresAt = issued.ExpiresAt;
        }
        return response;
    }

    internal static string Assess(HistoricalInvoiceAuditItemResponse x)
    {
        if (x.HasGstStagingDocument) return "AlreadyStaged";
        if (x.AmountPayable < 0) return "CancelledPendingReview";
        if (x.CompletionEvidenceClassification.Equals("Likely genuine draft", StringComparison.OrdinalIgnoreCase)) return "Incomplete";
        if (!x.CompletionEvidenceClassification.Equals("Completion candidate", StringComparison.OrdinalIgnoreCase) || x.AmountPayable == 0)
            return "NeedsInvestigation";

        var recMismatch = x.Lines.Count > 0 &&
            (Different(x.Lines.Sum(l => l.CgstAmount), x.CgstAmount) ||
             Different(x.Lines.Sum(l => l.SgstAmount), x.SgstAmount) ||
             Different(x.Lines.Sum(l => l.IgstAmount), x.IgstAmount));
        if (x.HasOldGoldTransaction && recMismatch) return "AuditorReview";

        if (string.IsNullOrWhiteSpace(x.PlaceOfSupply) || x.TaxableAmount < 0 || x.TaxTotal < 0 ||
            x.CgstAmount < 0 || x.SgstAmount < 0 || x.IgstAmount < 0 ||
            Different(x.TaxTotal, x.CgstAmount + x.SgstAmount + x.IgstAmount) ||
            x.GstClassificationErrors.Count > 0 ||
            (x.IsRecipientRegistered && string.IsNullOrWhiteSpace(x.CustomerGstin)) ||
            x.Lines.Any(l => x.IsTaxApplicable && (string.IsNullOrWhiteSpace(l.HsnCode) || string.IsNullOrWhiteSpace(l.Uqc))) ||
            x.Lines.Any(l => l.TaxableValue < 0 || l.CgstAmount < 0 || l.SgstAmount < 0 || l.IgstAmount < 0))
            return "NeedsGstCorrection";

        var historicalMismatch = recMismatch || x.Lines.Any(l => x.IsTaxApplicable && l.TaxableValue > 0 && l.GstRate <= 0);
        if (historicalMismatch && x.TaxTotal > 0 && x.TaxableAmount > 0 && x.Lines.Count > 0 &&
            !Different(x.Lines.Sum(l => l.TaxableValue), x.TaxableAmount)) return "HistoricalGstReview";
        if (x.InvoiceLineCount > 0 && x.Lines.Count == 0) return "NeedsInvestigation";
        if (x.TaxTotal == 0) return "NeedsInvestigation";
        return "QualifiedForStaging";
    }

    private static bool Different(decimal a, decimal b) => Math.Abs(a - b) > Tolerance;
    private static string CandidateKey(string? number, DateTime date) => $"{number?.Trim()}|{date:yyyy-MM-dd}";
    private static string Display(string value) => string.Concat(value.Select((c, i) => i > 0 && char.IsUpper(c) ? $" {c}" : c.ToString())).ToUpperInvariant();
}
