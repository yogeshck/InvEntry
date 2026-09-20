using System.Globalization;
using DataAccess.Models;
using InvEntry.Contracts.Gst;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public sealed class Gstr1B2csSummaryService : IGstr1B2csSummaryService
{
    private const decimal ReconciliationTolerance = 0.01M;

    private readonly MijmsContext _context;

    public Gstr1B2csSummaryService(MijmsContext context)
    {
        _context = context;
    }

    public async Task<Gstr1B2csSummaryResponse> GetSummaryAsync(
        Gstr1B2csSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidateQuery(query);

        var supplierGstin = query.SupplierGstin.Trim().ToUpperInvariant();
        var returnPeriod = query.ReturnPeriod.Trim();

        var sourceRows = await _context.GstGstr1Documents
            .AsNoTracking()
            .Where(x =>
                x.SupplierGstin == supplierGstin &&
                x.ReturnPeriod == returnPeriod &&
                x.IsReportable &&
                x.ReturnCategory == "B2CS" &&
                x.Gstr1Table == "7")
            .SelectMany(x => x.GstGstr1DocumentLines.Select(line => new B2csSourceRow
            {
                Type = x.IsEcommerceSupply ? "E" : "OE",
                PlaceOfSupplyCode = x.PlaceOfSupplyCode,
                GstRate = line.GstRate,
                TaxableValue = line.TaxableValue,
                CessAmount = line.CessAmount,
                ECommerceGstin = x.EcommerceOperatorGstin,
                CgstAmount = line.CgstAmount,
                SgstAmount = line.SgstAmount,
                IgstAmount = line.IgstAmount
            }))
            .ToListAsync(cancellationToken);

        var rows = sourceRows
            .GroupBy(x => new
            {
                x.Type,
                PlaceOfSupplyCode = x.PlaceOfSupplyCode.Trim(),
                x.GstRate,
                ECommerceGstin = Normalize(x.ECommerceGstin)
            })
            .OrderBy(x => x.Key.Type, StringComparer.Ordinal)
            .ThenBy(x => x.Key.PlaceOfSupplyCode, StringComparer.Ordinal)
            .ThenBy(x => x.Key.GstRate)
            .ThenBy(x => x.Key.ECommerceGstin, StringComparer.Ordinal)
            .Select(x => new Gstr1B2csSummaryRowResponse
            {
                Type = x.Key.Type,
                PlaceOfSupplyCode = x.Key.PlaceOfSupplyCode,
                GstRate = x.Key.GstRate,
                TaxableValue = x.Sum(y => y.TaxableValue),
                CessAmount = x.Sum(y => y.CessAmount),
                ECommerceGstin = x.Key.ECommerceGstin,
                ApplicableTaxRatePercentage = null,
                CgstAmount = x.Sum(y => y.CgstAmount),
                SgstAmount = x.Sum(y => y.SgstAmount),
                IgstAmount = x.Sum(y => y.IgstAmount)
            })
            .ToList();

        var response = new Gstr1B2csSummaryResponse
        {
            SupplierGstin = supplierGstin,
            ReturnPeriod = returnPeriod,
            Rows = rows
        };

        EnsureReconciled(sourceRows, response);
        return response;
    }

    private static void EnsureReconciled(
        IReadOnlyCollection<B2csSourceRow> sourceRows,
        Gstr1B2csSummaryResponse response)
    {
        if (Math.Abs(sourceRows.Sum(x => x.TaxableValue) - response.TotalTaxableValue) > ReconciliationTolerance ||
            Math.Abs(sourceRows.Sum(x => x.CessAmount) - response.TotalCess) > ReconciliationTolerance ||
            Math.Abs(sourceRows.Sum(x => x.CgstAmount) - response.TotalCgst) > ReconciliationTolerance ||
            Math.Abs(sourceRows.Sum(x => x.SgstAmount) - response.TotalSgst) > ReconciliationTolerance ||
            Math.Abs(sourceRows.Sum(x => x.IgstAmount) - response.TotalIgst) > ReconciliationTolerance)
        {
            throw new InvalidOperationException(
                "B2CS aggregation totals do not reconcile with the staged source documents.");
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant();
    }

    private static void ValidateQuery(Gstr1B2csSummaryQuery query)
    {
        if (query is null)
            throw new ArgumentNullException(nameof(query));

        if (string.IsNullOrWhiteSpace(query.SupplierGstin))
            throw new ArgumentException("Supplier GSTIN is required.", nameof(query.SupplierGstin));

        if (string.IsNullOrWhiteSpace(query.ReturnPeriod) ||
            query.ReturnPeriod.Length != 6 ||
            !DateTime.TryParseExact(
                query.ReturnPeriod,
                "yyyyMM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            throw new ArgumentException(
                "Return period must be a valid yyyyMM period.",
                nameof(query.ReturnPeriod));
        }
    }

    private sealed class B2csSourceRow
    {
        public string Type { get; init; } = string.Empty;

        public string PlaceOfSupplyCode { get; init; } = string.Empty;

        public decimal GstRate { get; init; }

        public decimal TaxableValue { get; init; }

        public decimal CessAmount { get; init; }

        public string? ECommerceGstin { get; init; }

        public decimal CgstAmount { get; init; }

        public decimal SgstAmount { get; init; }

        public decimal IgstAmount { get; init; }
    }
}
