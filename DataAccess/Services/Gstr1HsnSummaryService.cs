using DataAccess.Models;
using InvEntry.Contracts.Gst;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public sealed class Gstr1HsnSummaryService : IGstr1HsnSummaryService
{
    private readonly MijmsContext _context;
    private readonly Gstr1Table12Policy _policy;

    public Gstr1HsnSummaryService(
        MijmsContext context,
        Gstr1Table12Policy policy)
    {
        _context = context;
        _policy = policy;
    }

    public async Task<Gstr1HsnSummaryResponse> GetSummaryAsync(
        Gstr1HsnSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidateQuery(query);

        var supplierGstin = query.SupplierGstin.Trim().ToUpperInvariant();
        var returnPeriod = query.ReturnPeriod.Trim();

        var documents = await _context.GstGstr1Documents
            .AsNoTracking()
            .Include(x => x.GstGstr1DocumentLines)
            .Where(x =>
                x.SupplierGstin == supplierGstin &&
                x.ReturnPeriod == returnPeriod &&
                x.IsReportable)
            .ToListAsync(cancellationToken);

        var sourceRows = new List<HsnSourceRow>();

        foreach (var document in documents)
        {
            var supplyClass = _policy.Resolve(
                document.ReturnCategory,
                document.IsRecipientRegistered);

            if (supplyClass == Gstr1Table12SupplyClass.Excluded)
                continue;

            var supplyClassText =
                supplyClass == Gstr1Table12SupplyClass.B2B
                    ? "B2B"
                    : "B2C";

            foreach (var line in document.GstGstr1DocumentLines)
            {
                // Validation should normally catch these.
                // The summary service does not invent missing GST data.
                if (string.IsNullOrWhiteSpace(line.HsnCode) ||
                    string.IsNullOrWhiteSpace(line.Uqc) ||
                    !line.GstQuantity.HasValue ||
                    line.GstQuantity.Value <= 0M)
                {
                    continue;
                }

                sourceRows.Add(new HsnSourceRow
                {
                    SupplyClass = supplyClassText,
                    HsnCode = line.HsnCode.Trim(),
                    Description = NormalizeDescription(line.Description),
                    Uqc = line.Uqc.Trim().ToUpperInvariant(),
                    GstRate = line.GstRate,
                    Quantity = line.GstQuantity.Value,
                    TaxableValue = line.TaxableValue,
                    CgstAmount = line.CgstAmount,
                    SgstAmount = line.SgstAmount,
                    IgstAmount = line.IgstAmount,
                    CessAmount = line.CessAmount
                });
            }
        }

        var rows = sourceRows
            .GroupBy(x => new
            {
                x.SupplyClass,
                x.HsnCode,
                x.Description,
                x.Uqc,
                x.GstRate
            })
            .Select(g => new Gstr1HsnSummaryRowResponse
            {
                SupplyClass = g.Key.SupplyClass,
                HsnCode = g.Key.HsnCode,
                Description = g.Key.Description,
                Uqc = g.Key.Uqc,
                GstRate = g.Key.GstRate,

                TotalQuantity = g.Sum(x => x.Quantity),
                TaxableValue = g.Sum(x => x.TaxableValue),
                CgstAmount = g.Sum(x => x.CgstAmount),
                SgstAmount = g.Sum(x => x.SgstAmount),
                IgstAmount = g.Sum(x => x.IgstAmount),
                CessAmount = g.Sum(x => x.CessAmount)
            })
            .OrderBy(x => x.SupplyClass)
            .ThenBy(x => x.HsnCode)
            .ThenBy(x => x.Uqc)
            .ThenBy(x => x.GstRate)
            .ThenBy(x => x.Description)
            .ToList();

        return new Gstr1HsnSummaryResponse
        {
            SupplierGstin = supplierGstin,
            ReturnPeriod = returnPeriod,

            B2B = rows
                .Where(x => x.SupplyClass == "B2B")
                .ToList(),

            B2C = rows
                .Where(x => x.SupplyClass == "B2C")
                .ToList()
        };
    }

    private static void ValidateQuery(Gstr1HsnSummaryQuery query)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        if (string.IsNullOrWhiteSpace(query.SupplierGstin))
            throw new ArgumentException(
                "Supplier GSTIN is required.",
                nameof(query.SupplierGstin));

        if (string.IsNullOrWhiteSpace(query.ReturnPeriod) ||
            query.ReturnPeriod.Length != 6 ||
            !int.TryParse(query.ReturnPeriod, out _))
        {
            throw new ArgumentException(
                "Return period must be in yyyyMM format.",
                nameof(query.ReturnPeriod));
        }

        if (!int.TryParse(query.ReturnPeriod[..4], out var year) ||
            !int.TryParse(query.ReturnPeriod[4..], out var month) ||
            year < 2017 ||
            month < 1 ||
            month > 12)
        {
            throw new ArgumentException(
                "Return period must be a valid yyyyMM period.",
                nameof(query.ReturnPeriod));
        }
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        return description.Trim();
    }

    private sealed class HsnSourceRow
    {
        public string SupplyClass { get; init; } = string.Empty;

        public string HsnCode { get; init; } = string.Empty;

        public string? Description { get; init; }

        public string Uqc { get; init; } = string.Empty;

        public decimal GstRate { get; init; }

        public decimal Quantity { get; init; }

        public decimal TaxableValue { get; init; }

        public decimal CgstAmount { get; init; }

        public decimal SgstAmount { get; init; }

        public decimal IgstAmount { get; init; }

        public decimal CessAmount { get; init; }
    }
}