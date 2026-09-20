using System.Globalization;
using DataAccess.Models;
using InvEntry.Contracts.Gst;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public sealed class Gstr1B2bSummaryService : IGstr1B2bSummaryService
{
    private const decimal ReconciliationTolerance = 0.01M;
    private readonly MijmsContext _context;

    public Gstr1B2bSummaryService(MijmsContext context) => _context = context;

    public async Task<Gstr1B2bSummaryResponse> GetSummaryAsync(
        Gstr1B2bSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidateQuery(query);
        var supplierGstin = query.SupplierGstin.Trim().ToUpperInvariant();
        var returnPeriod = query.ReturnPeriod.Trim();

        var documents = await _context.GstGstr1Documents
            .AsNoTracking()
            .Where(x =>
                x.SupplierGstin == supplierGstin &&
                x.ReturnPeriod == returnPeriod &&
                x.IsReportable &&
                x.IsRecipientRegistered &&
                x.RecipientGstin != null &&
                x.RecipientGstin.Trim() != "" &&
                x.ReturnCategory == "B2B")
            .OrderBy(x => x.DocumentDate)
            .ThenBy(x => x.DocumentNbr)
            .ThenBy(x => x.Gkey)
            .Select(x => new B2bSourceDocument
            {
                DocumentGkey = x.Gkey,
                SourceGkey = x.SourceGkey,
                RecipientGstin = x.RecipientGstin!,
                DocumentNumber = x.DocumentNbr,
                DocumentDate = x.DocumentDate,
                PlaceOfSupplyCode = x.PlaceOfSupplyCode,
                InvoiceValue = x.InvoiceValue,
                SupplyType = x.SupplyType,
                TaxType = x.TaxType,
                ReverseCharge = x.IsReverseCharge,
                TaxableValue = x.TaxableValue,
                IgstAmount = x.IgstAmount,
                CgstAmount = x.CgstAmount,
                SgstAmount = x.SgstAmount,
                CessAmount = x.CessAmount,
                Lines = x.GstGstr1DocumentLines
                    .OrderBy(line => line.LineNbr)
                    .ThenBy(line => line.Gkey)
                    .Select(line => new B2bSourceLine
                    {
                        LineGkey = line.Gkey,
                        LineNumber = line.LineNbr,
                        GstRate = line.GstRate,
                        TaxableValue = line.TaxableValue,
                        IgstAmount = line.IgstAmount,
                        CgstAmount = line.CgstAmount,
                        SgstAmount = line.SgstAmount,
                        CessAmount = line.CessAmount
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return Prepare(supplierGstin, returnPeriod, documents);
    }

    internal static Gstr1B2bSummaryResponse Prepare(
        string supplierGstin,
        string returnPeriod,
        IReadOnlyCollection<B2bSourceDocument> documents)
    {
        var response = new Gstr1B2bSummaryResponse
        {
            SupplierGstin = supplierGstin,
            ReturnPeriod = returnPeriod,
            Invoices = documents
                .OrderBy(x => x.DocumentDate)
                .ThenBy(x => x.DocumentNumber, StringComparer.Ordinal)
                .ThenBy(x => x.DocumentGkey)
                .Select(x => new Gstr1B2bInvoiceResponse
                {
                    DocumentGkey = x.DocumentGkey,
                    SourceGkey = x.SourceGkey,
                    RecipientGstin = x.RecipientGstin.Trim().ToUpperInvariant(),
                    DocumentNumber = x.DocumentNumber,
                    DocumentDate = x.DocumentDate,
                    PlaceOfSupplyCode = x.PlaceOfSupplyCode,
                    InvoiceValue = x.InvoiceValue,
                    SupplyType = x.SupplyType,
                    TaxType = x.TaxType,
                    ReverseCharge = x.ReverseCharge,
                    Lines = x.Lines
                        .OrderBy(line => line.LineNumber)
                        .ThenBy(line => line.LineGkey)
                        .Select(line => new Gstr1B2bLineResponse
                        {
                            LineNumber = line.LineNumber,
                            GstRate = line.GstRate,
                            TaxableValue = line.TaxableValue,
                            IgstAmount = line.IgstAmount,
                            CgstAmount = line.CgstAmount,
                            SgstAmount = line.SgstAmount,
                            CessAmount = line.CessAmount
                        })
                        .ToList()
                })
                .ToList()
        };

        EnsureReconciled(documents, response);
        return response;
    }

    private static void EnsureReconciled(
        IReadOnlyCollection<B2bSourceDocument> documents,
        Gstr1B2bSummaryResponse response)
    {
        if (Math.Abs(documents.Sum(x => x.TaxableValue) - response.TotalTaxableValue) > ReconciliationTolerance ||
            Math.Abs(documents.Sum(x => x.IgstAmount) - response.TotalIgst) > ReconciliationTolerance ||
            Math.Abs(documents.Sum(x => x.CgstAmount) - response.TotalCgst) > ReconciliationTolerance ||
            Math.Abs(documents.Sum(x => x.SgstAmount) - response.TotalSgst) > ReconciliationTolerance ||
            Math.Abs(documents.Sum(x => x.CessAmount) - response.TotalCess) > ReconciliationTolerance)
        {
            throw new InvalidOperationException(
                "B2B preparation totals do not reconcile with the staged source documents.");
        }
    }

    private static void ValidateQuery(Gstr1B2bSummaryQuery query)
    {
        if (query is null)
            throw new ArgumentNullException(nameof(query));
        if (string.IsNullOrWhiteSpace(query.SupplierGstin))
            throw new ArgumentException("Supplier GSTIN is required.", nameof(query.SupplierGstin));
        if (string.IsNullOrWhiteSpace(query.ReturnPeriod) ||
            query.ReturnPeriod.Length != 6 ||
            !DateTime.TryParseExact(query.ReturnPeriod, "yyyyMM", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _))
        {
            throw new ArgumentException(
                "Return period must be a valid yyyyMM period.", nameof(query.ReturnPeriod));
        }
    }

    internal sealed class B2bSourceDocument
    {
        public long DocumentGkey { get; init; }
        public int SourceGkey { get; init; }
        public string RecipientGstin { get; init; } = string.Empty;
        public string DocumentNumber { get; init; } = string.Empty;
        public DateOnly DocumentDate { get; init; }
        public string PlaceOfSupplyCode { get; init; } = string.Empty;
        public decimal InvoiceValue { get; init; }
        public string SupplyType { get; init; } = string.Empty;
        public string TaxType { get; init; } = string.Empty;
        public bool ReverseCharge { get; init; }
        public decimal TaxableValue { get; init; }
        public decimal IgstAmount { get; init; }
        public decimal CgstAmount { get; init; }
        public decimal SgstAmount { get; init; }
        public decimal CessAmount { get; init; }
        public List<B2bSourceLine> Lines { get; init; } = new();
    }

    internal sealed class B2bSourceLine
    {
        public long LineGkey { get; init; }
        public int LineNumber { get; init; }
        public decimal GstRate { get; init; }
        public decimal TaxableValue { get; init; }
        public decimal IgstAmount { get; init; }
        public decimal CgstAmount { get; init; }
        public decimal SgstAmount { get; init; }
        public decimal CessAmount { get; init; }
    }
}
