using System.Globalization;
using InvEntry.Contracts.Gst;
using InvEntry.Contracts.Gst.Export;

namespace DataAccess.Services;

/// <summary>Maps prepared reporting values only; no database access or GST calculations.</summary>
public static class Gstr1GstnExportMapper
{
    public static string ToFilingPeriod(string returnPeriod)
    {
        if (returnPeriod is null || returnPeriod.Length != 6 ||
            returnPeriod.Any(c => c < '0' || c > '9') ||
            !DateTime.TryParseExact(returnPeriod + "01", "yyyyMMdd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            throw new ArgumentException("Return period must be a valid yyyyMM period.", nameof(returnPeriod));

        return date.ToString("MMyyyy", CultureInfo.InvariantCulture);
    }

    public static Gstr1GstnExportResponse Map(Gstr1ExportPreparationResponse prepared)
    {
        ArgumentNullException.ThrowIfNull(prepared);
        if (!prepared.Validation.IsExportReady)
            throw new InvalidOperationException(
                $"GSTN export blocked: validation returned {prepared.Validation.ErrorCount} error(s).");

        return new Gstr1GstnExportResponse
        {
            Gstin = prepared.SupplierGstin,
            FilingPeriod = ToFilingPeriod(prepared.ReturnPeriod),
            B2b = MapB2b(prepared.B2b),
            B2cs = prepared.B2cs.Rows.Select(MapB2cs).ToList(),
            Hsn = new Gstr1GstnHsnSection
            {
                B2B = MapHsn(prepared.Hsn.B2B),
                B2C = MapHsn(prepared.Hsn.B2C)
            },
            DocumentsIssued = MapDocuments(prepared.DocumentsIssued)
        };
    }

    private static List<Gstr1GstnHsnRow> MapHsn(
        IEnumerable<Gstr1HsnSummaryRowResponse> rows) =>
        rows
            .OrderBy(x => x.HsnCode, StringComparer.Ordinal)
            .ThenBy(x => x.Uqc, StringComparer.Ordinal)
            .ThenBy(x => x.GstRate)
            .Select((x, index) => new Gstr1GstnHsnRow
            {
                Number = index + 1,
                HsnCode = x.HsnCode,
                Description = x.Description,
                Uqc = x.Uqc,
                Quantity = x.TotalQuantity,
                GstRate = x.GstRate,
                TaxableValue = x.TaxableValue,
                IgstAmount = x.IgstAmount,
                CgstAmount = x.CgstAmount,
                SgstAmount = x.SgstAmount,
                CessAmount = x.CessAmount
            })
            .ToList();

    private static List<Gstr1GstnB2bRecipient> MapB2b(Gstr1B2bSummaryResponse source)
    {
        return source.Invoices
            .GroupBy(x => x.RecipientGstin, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new Gstr1GstnB2bRecipient
            {
                RecipientGstin = group.Key,
                Invoices = group
                    .OrderBy(x => x.DocumentDate)
                    .ThenBy(x => x.DocumentNumber, StringComparer.Ordinal)
                    .ThenBy(x => x.DocumentGkey)
                    .Select(MapB2bInvoice)
                    .ToList()
            })
            .ToList();
    }

    private static Gstr1GstnB2bInvoice MapB2bInvoice(Gstr1B2bInvoiceResponse invoice)
    {
        if (invoice.IsSez || invoice.IsDeemedExport)
            throw new InvalidOperationException(
                $"GSTN export blocked: invoice {invoice.DocumentNumber} is an unsupported special B2B transaction (SEZ or deemed export).");

        return new Gstr1GstnB2bInvoice
        {
            InvoiceNumber = invoice.DocumentNumber,
            InvoiceDate = invoice.DocumentDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
            InvoiceValue = invoice.InvoiceValue,
            PlaceOfSupplyCode = invoice.PlaceOfSupplyCode,
            ReverseCharge = invoice.ReverseCharge ? "Y" : "N",

            // Batch 8B supports ordinary domestic registered-recipient sales only.
            InvoiceType = "R",
            Items = invoice.Lines
                .OrderBy(line => line.LineNumber)
                .Select((line, index) => new Gstr1GstnB2bItem
                {
                Number = index + 1,
                ItemDetail = new Gstr1GstnB2bItemDetail
                {
                    GstRate = line.GstRate,
                    TaxableValue = line.TaxableValue,
                    IgstAmount = line.IgstAmount,
                    CgstAmount = line.CgstAmount,
                    SgstAmount = line.SgstAmount,
                    CessAmount = line.CessAmount
                }
            }).ToList()
        };
    }
    private static Gstr1GstnB2csRow MapB2cs(Gstr1B2csSummaryRowResponse row)
    {
        // Prepared rows do not carry staged SupplyType/TaxType. Use only their
        // unambiguous tax allocation; never infer a type from missing tax amounts.
        var intra = row.CgstAmount >= 0M && row.SgstAmount >= 0M &&
            (row.CgstAmount > 0M || row.SgstAmount > 0M) && row.IgstAmount == 0M;
        var inter = row.IgstAmount > 0M && row.CgstAmount == 0M && row.SgstAmount == 0M;
        if (!intra && !inter)
            throw new InvalidOperationException(
                $"GSTN export blocked: contradictory or indeterminate B2CS tax allocation at POS {row.PlaceOfSupplyCode}, rate {row.GstRate}.");

        // Do not silently drop extra reporting dimensions absent from this initial wire format.
        if (row.Type != "OE" || !string.IsNullOrWhiteSpace(row.ECommerceGstin) ||
            row.ApplicableTaxRatePercentage.HasValue)
            throw new InvalidOperationException(
                "GSTN export blocked: e-commerce or applicable-rate B2CS requires an extended wire contract.");

        return new Gstr1GstnB2csRow
        {
            PlaceOfSupplyCode = row.PlaceOfSupplyCode,
            SupplyType = intra ? "INTRA" : "INTER",
            GstRate = row.GstRate,
            TaxableValue = row.TaxableValue,
            IgstAmount = row.IgstAmount,
            CgstAmount = row.CgstAmount,
            SgstAmount = row.SgstAmount,
            CessAmount = row.CessAmount,
            Type = row.Type
        };
    }

    private static Gstr1GstnDocumentIssueSection MapDocuments(Gstr1DocumentsIssuedResponse source)
    {
        if (source.Series.Any(x => x.DocumentType != "Sales Invoice"))
            throw new InvalidOperationException("GSTN export blocked: only Sales Invoice document series are supported.");

        var sales = source.Series.OrderBy(x => x.Series, StringComparer.Ordinal)
            .ThenBy(x => x.FromNumber, StringComparer.Ordinal)
            .ThenBy(x => x.ToNumber, StringComparer.Ordinal)
            .Select((x, index) => new Gstr1GstnDocumentSeries
            {
                Number = index + 1,
                FromNumber = x.FromNumber,
                ToNumber = x.ToNumber,
                TotalIssued = x.TotalIssued,
                Cancelled = x.Cancelled,
                NetIssued = x.NetIssued
            }).ToList();

        // Wire-format choice isolated here for subsequent GSTN Offline Tool verification.
        // Unused types contain no fabricated document series.
        return new Gstr1GstnDocumentIssueSection
        {
            Details = Enumerable.Range(1, 12).Select(number => new Gstr1GstnDocumentDetail
            {
                DocumentNumber = number,
                Series = number == 1 ? sales : []
            }).ToList()
        };
    }
}
