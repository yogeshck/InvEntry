using DataAccess.Models;
using InvEntry.Contracts.Gst;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace DataAccess.Services;

public sealed class Gstr1ReportQueryService : IGstr1ReportQueryService
{
    private readonly MijmsContext _context;
    private static readonly string[] CategoryOrder =
    [
        "B2B", "B2CL", "B2CS", "Export", "NilRated", "Exempt",
        "NonGst", "CreditDebitNote", "NotApplicable"
    ];

    public Gstr1ReportQueryService(MijmsContext context)
    {
        _context = context;
    }

    private IQueryable<GstGstr1Document> GetScope(
        string supplierGstin, string returnPeriod)
    {
        if (string.IsNullOrWhiteSpace(supplierGstin))
            throw new ArgumentException("Supplier GSTIN is required.", nameof(supplierGstin));

        if (returnPeriod is null || returnPeriod.Length != 6 ||
            returnPeriod.Any(c => c < '0' || c > '9') ||
            !DateTime.TryParseExact(returnPeriod + "01", "yyyyMMdd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            throw new ArgumentException(
                "Return period must be six digits yyyyMM with a valid calendar month.",
                nameof(returnPeriod));
        }

        var normalizedGstin = supplierGstin.Trim().ToUpperInvariant();
        return _context.GstGstr1Documents.AsNoTracking()
            .Where(d => d.SupplierGstin == normalizedGstin && d.ReturnPeriod == returnPeriod);
    }

    public async Task<Gstr1ReturnSummaryResponse> GetSummaryAsync(
        string supplierGstin, string returnPeriod,
        CancellationToken cancellationToken = default)
    {
        var scope = GetScope(supplierGstin, returnPeriod);
        var summary = await scope.GroupBy(d => 1)
            .Select(g => new Gstr1ReturnSummaryResponse
            {
                DocumentCount = g.Count(),
                ReportableDocumentCount = g.Count(d => d.IsReportable),
                TotalInvoiceValue = g.Sum(d => d.IsReportable ? d.InvoiceValue : 0M),
                TotalTaxableValue = g.Sum(d => d.IsReportable ? d.TaxableValue : 0M),
                TotalCgst = g.Sum(d => d.IsReportable ? d.CgstAmount : 0M),
                TotalSgst = g.Sum(d => d.IsReportable ? d.SgstAmount : 0M),
                TotalIgst = g.Sum(d => d.IsReportable ? d.IgstAmount : 0M),
                TotalCess = g.Sum(d => d.IsReportable ? d.CessAmount : 0M)
            }).SingleOrDefaultAsync(cancellationToken)
            ?? new Gstr1ReturnSummaryResponse();

        summary.SupplierGstin = supplierGstin.Trim().ToUpperInvariant();
        summary.ReturnPeriod = returnPeriod;

        var categories = await scope.Where(d => d.IsReportable)
            .GroupBy(d => new { d.ReturnCategory, d.Gstr1Table })
            .Select(g => new Gstr1CategorySummaryResponse
            {
                Category = g.Key.ReturnCategory,
                Gstr1Table = g.Key.Gstr1Table,
                DocumentCount = g.Count(),
                InvoiceValue = g.Sum(d => d.InvoiceValue),
                TaxableValue = g.Sum(d => d.TaxableValue),
                CgstAmount = g.Sum(d => d.CgstAmount),
                SgstAmount = g.Sum(d => d.SgstAmount),
                IgstAmount = g.Sum(d => d.IgstAmount),
                CessAmount = g.Sum(d => d.CessAmount)
            }).ToListAsync(cancellationToken);

        foreach (var category in CategoryOrder.Take(8))
        {
            if (!categories.Any(c => c.Category == category))
                categories.Add(new Gstr1CategorySummaryResponse { Category = category });
        }

        summary.Categories = categories
            .OrderBy(c => Array.IndexOf(CategoryOrder, c.Category) is var index && index >= 0
                ? index : CategoryOrder.Length)
            .ThenBy(c => c.Category, StringComparer.Ordinal)
            .ThenBy(c => c.Gstr1Table, StringComparer.Ordinal)
            .ToList();
        return summary;
    }

    public async Task<IReadOnlyList<Gstr1DocumentResponse>> GetDocumentsAsync(
        string supplierGstin, string returnPeriod,
        CancellationToken cancellationToken = default)
    {
        return await GetScope(supplierGstin, returnPeriod)
            .OrderBy(d => d.DocumentDate).ThenBy(d => d.DocumentNbr).ThenBy(d => d.Gkey)
            .Select(d => new Gstr1DocumentResponse
            {
                Gkey = d.Gkey,
                SourceGkey = d.SourceGkey,
                DocumentType = d.DocumentType,
                DocumentNbr = d.DocumentNbr,
                DocumentDate = d.DocumentDate,
                RecipientGstin = d.RecipientGstin,
                RecipientStateCode = d.RecipientStateCode,
                IsRecipientRegistered = d.IsRecipientRegistered,
                PlaceOfSupplyCode = d.PlaceOfSupplyCode,
                SupplyType = d.SupplyType,
                TaxType = d.TaxType,
                ReturnCategory = d.ReturnCategory,
                Gstr1Table = d.Gstr1Table,
                IsReportable = d.IsReportable,
                InvoiceValue = d.InvoiceValue,
                TaxableValue = d.TaxableValue,
                CgstAmount = d.CgstAmount,
                SgstAmount = d.SgstAmount,
                IgstAmount = d.IgstAmount,
                CessAmount = d.CessAmount,
                Status = d.Status
            }).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Gstr1DocumentLineResponse>?> GetDocumentLinesAsync(
        long documentGkey, string supplierGstin, string returnPeriod,
        CancellationToken cancellationToken = default)
    {
        if (!await GetScope(supplierGstin, returnPeriod)
            .AnyAsync(d => d.Gkey == documentGkey, cancellationToken))
            return null;

        return await _context.GstGstr1DocumentLines.AsNoTracking()
            .Where(l => l.GstDocumentGkey == documentGkey)
            .OrderBy(l => l.LineNbr).ThenBy(l => l.Gkey)
            .Select(l => new Gstr1DocumentLineResponse
            {
                GstDocumentGkey = l.GstDocumentGkey,
                LineNbr = l.LineNbr,
                SourceLineGkey = l.SourceLineGkey,
                HsnCode = l.HsnCode,
                Description = l.Description,
                Quantity = l.Quantity,
                TaxableValue = l.TaxableValue,
                GstRate = l.GstRate,
                CgstRate = l.CgstRate,
                SgstRate = l.SgstRate,
                IgstRate = l.IgstRate,
                CgstAmount = l.CgstAmount,
                SgstAmount = l.SgstAmount,
                IgstAmount = l.IgstAmount,
                CessAmount = l.CessAmount
            }).ToListAsync(cancellationToken);
    }
}
