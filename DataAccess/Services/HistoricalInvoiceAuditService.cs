using DataAccess.Models;
using InvEntry.Contracts.Gst;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public sealed class HistoricalInvoiceAuditService : IHistoricalInvoiceAuditService
{
    private const string SalesInvoiceDocumentType = "SalesInvoice";
    private readonly MijmsContext _context;
    private readonly ICurrentCompanyGstinProvider _currentCompanyGstinProvider;

    public HistoricalInvoiceAuditService(
        MijmsContext context,
        ICurrentCompanyGstinProvider currentCompanyGstinProvider)
    {
        _context = context;
        _currentCompanyGstinProvider = currentCompanyGstinProvider;
    }

    public async Task<HistoricalInvoiceAuditResponse> GetAsync(
        string supplierGstin,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var normalizedGstin = ValidateRequest(supplierGstin, fromDate, toDate);
        var startDate = fromDate.Date;
        var toExclusive = toDate.Date.AddDays(1);

        var configuredGstin = await _currentCompanyGstinProvider.GetAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(configuredGstin))
        {
            throw new InvalidOperationException("The current company GSTIN is not configured.");
        }

        configuredGstin = configuredGstin.Trim().ToUpperInvariant();
        if (!string.Equals(normalizedGstin, configuredGstin, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Requested GSTIN '{normalizedGstin}' does not match the current company's GSTIN '{configuredGstin}'.");
        }

        var rows = await (
            from invoice in _context.InvoiceHeaders.AsNoTracking()
            where invoice.InvDate.HasValue &&
                  invoice.InvDate.Value >= startDate &&
                  invoice.InvDate.Value < toExclusive
            join customer in _context.OrgCustomers.AsNoTracking()
                on invoice.CustGkey equals (int?)customer.Gkey into customerJoin
            from customer in customerJoin.DefaultIfEmpty()
            let staging = _context.GstGstr1Documents
                .AsNoTracking()
                .Where(x => x.SourceGkey == invoice.Gkey &&
                            x.DocumentType == SalesInvoiceDocumentType &&
                            x.SupplierGstin == normalizedGstin)
                .OrderBy(x => x.Gkey)
                .FirstOrDefault()
            orderby invoice.InvDate, invoice.Gkey
            select new AuditRow
            {
                InvoiceGkey = invoice.Gkey,
                InvoiceNbr = invoice.InvNbr,
                InvoiceDate = invoice.InvDate,
                CurrentStatus = invoice.Status,
                CustomerGkey = invoice.CustGkey,
                CustomerName = customer == null ? null : customer.CustomerName,
                CustomerExists = customer != null,
                AmountPayable = invoice.AmountPayable ?? 0M,
                TaxableAmount = invoice.InvTaxableAmount ?? 0M,
                TaxTotal = (invoice.CgstAmount ?? 0M) +
                           (invoice.SgstAmount ?? 0M) +
                           (invoice.IgstAmount ?? 0M),
                CgstAmount = invoice.CgstAmount ?? 0M,
                SgstAmount = invoice.SgstAmount ?? 0M,
                IgstAmount = invoice.IgstAmount ?? 0M,
                InvoiceLineCount = _context.InvoiceLines.Count(x => x.InvoiceHdrGkey == invoice.Gkey),
                LineTaxableAmount = _context.InvoiceLines
                    .Where(x => x.InvoiceHdrGkey == invoice.Gkey)
                    .Sum(x => x.InvlTaxableAmount ?? 0M),
                LinkedVoucherCount = _context.Vouchers.Count(x => x.RefDocGkey == invoice.Gkey),
                LinkedArReceiptCount = _context.InvoiceArReceipts.Count(x => x.InvoiceGkey == invoice.Gkey),
                GstStagingGkey = staging == null ? null : staging.Gkey,
                GstStagingStatus = staging == null ? null : staging.Status,
                PlaceOfSupply = invoice.GstLocBuyer,
                CreatedBy = invoice.CreatedBy,
                CreatedOn = invoice.CreatedOn,
                ModifiedBy = invoice.ModifiedBy,
                ModifiedOn = invoice.ModifiedOn,
                FinalisedOn = invoice.FinalisedOn
            }).ToListAsync(cancellationToken);

        var response = new HistoricalInvoiceAuditResponse
        {
            SupplierGstin = normalizedGstin,
            FromDate = startDate,
            ToDate = toDate.Date,
            InvoiceCount = rows.Count
        };

        foreach (var row in rows)
        {
            var classification = HistoricalInvoiceCompletionClassifier.Classify(
                new HistoricalInvoiceCompletionEvidence(
                    row.InvoiceNbr,
                    row.InvoiceLineCount,
                    row.CustomerExists,
                    row.PlaceOfSupply,
                    row.LinkedVoucherCount,
                    row.LinkedArReceiptCount));

            response.Items.Add(new HistoricalInvoiceAuditItemResponse
            {
                InvoiceGkey = row.InvoiceGkey,
                InvoiceNbr = row.InvoiceNbr,
                InvoiceDate = row.InvoiceDate,
                CurrentStatus = row.CurrentStatus,
                CustomerGkey = row.CustomerGkey,
                CustomerName = row.CustomerName,
                AmountPayable = row.AmountPayable,
                TaxableAmount = ResolveTaxableAmount(
                    row.TaxableAmount,
                    row.LineTaxableAmount,
                    row.TaxTotal),
                TaxTotal = row.TaxTotal,
                CgstAmount = row.CgstAmount,
                SgstAmount = row.SgstAmount,
                IgstAmount = row.IgstAmount,
                InvoiceLineCount = row.InvoiceLineCount,
                LinkedVoucherCount = row.LinkedVoucherCount,
                LinkedArReceiptCount = row.LinkedArReceiptCount,
                HasGstStagingDocument = row.GstStagingGkey.HasValue,
                GstStagingStatus = row.GstStagingStatus,
                PlaceOfSupply = row.PlaceOfSupply,
                CreatedBy = row.CreatedBy,
                CreatedOn = row.CreatedOn,
                ModifiedBy = row.ModifiedBy,
                ModifiedOn = row.ModifiedOn,
                FinalisedOn = row.FinalisedOn,
                CompletionEvidenceClassification = classification.Classification,
                CompletionEvidenceReasons = classification.Reasons.ToList(),
                ProductionTestProvenance = "Unknown",
                ProvenanceReasons =
                [
                    "No dedicated import batch, import source, or production/test marker exists on the invoice.",
                    "Creation and modification metadata are reported as evidence but are not sufficient to classify production versus test data."
                ]
            });
        }

        return response;
    }

    private static string ValidateRequest(string supplierGstin, DateTime fromDate, DateTime toDate)
    {
        if (string.IsNullOrWhiteSpace(supplierGstin))
            throw new ArgumentException("Supplier GSTIN is required.", nameof(supplierGstin));
        if (fromDate == default)
            throw new ArgumentException("From date is required.", nameof(fromDate));
        if (toDate == default)
            throw new ArgumentException("To date is required.", nameof(toDate));
        if (fromDate.Date > toDate.Date)
            throw new ArgumentException("From date cannot be later than To date.");
        if ((toDate.Date - fromDate.Date).TotalDays > 366)
            throw new ArgumentException("A historical invoice audit cannot exceed 366 days.");

        return supplierGstin.Trim().ToUpperInvariant();
    }

    internal static decimal ResolveTaxableAmount(
        decimal headerTaxableAmount,
        decimal lineTaxableAmount,
        decimal taxTotal)
    {
        if (headerTaxableAmount != 0M)
            return headerTaxableAmount;

        return taxTotal != 0M && lineTaxableAmount != 0M
            ? lineTaxableAmount
            : headerTaxableAmount;
    }

    private sealed class AuditRow
    {
        public int InvoiceGkey { get; init; }
        public string? InvoiceNbr { get; init; }
        public DateTime? InvoiceDate { get; init; }
        public string CurrentStatus { get; init; } = string.Empty;
        public int? CustomerGkey { get; init; }
        public string? CustomerName { get; init; }
        public bool CustomerExists { get; init; }
        public decimal AmountPayable { get; init; }
        public decimal TaxableAmount { get; init; }
        public decimal TaxTotal { get; init; }
        public decimal CgstAmount { get; init; }
        public decimal SgstAmount { get; init; }
        public decimal IgstAmount { get; init; }
        public decimal LineTaxableAmount { get; init; }
        public int InvoiceLineCount { get; init; }
        public int LinkedVoucherCount { get; init; }
        public int LinkedArReceiptCount { get; init; }
        public long? GstStagingGkey { get; init; }
        public string? GstStagingStatus { get; init; }
        public string? PlaceOfSupply { get; init; }
        public string? CreatedBy { get; init; }
        public DateTime? CreatedOn { get; init; }
        public string? ModifiedBy { get; init; }
        public DateTime? ModifiedOn { get; init; }
        public DateTime? FinalisedOn { get; init; }
    }
}
