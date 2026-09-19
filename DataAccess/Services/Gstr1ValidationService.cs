using System.Globalization;
using DataAccess.Models;
using InvEntry.Contracts.Gst;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public sealed class Gstr1ValidationService : IGstr1ValidationService
{
    private const decimal ReconciliationTolerance = 0.01M;

    private readonly MijmsContext _context;
    private readonly IGstr1HsnSummaryService _hsnSummaryService;
    private readonly Gstr1Table12Policy _table12Policy;
    private readonly IGstr1DocumentsIssuedService _documentsIssuedService;

    public Gstr1ValidationService(
        MijmsContext context,
        IGstr1HsnSummaryService hsnSummaryService,
        Gstr1Table12Policy table12Policy,
        IGstr1DocumentsIssuedService documentsIssuedService)
    {
        _context = context;
        _hsnSummaryService = hsnSummaryService;
        _table12Policy = table12Policy;
        _documentsIssuedService = documentsIssuedService;
    }

    public async Task<Gstr1ValidationResponse> ValidateAsync(
        string supplierGstin,
        string returnPeriod,
        CancellationToken cancellationToken = default)
    {
        var scope = ValidateScope(
            supplierGstin,
            returnPeriod);

        var documents = await _context.GstGstr1Documents
            .AsNoTracking()
            .Include(x => x.GstGstr1DocumentLines)
            .Where(x =>
                x.SupplierGstin == scope.SupplierGstin &&
                x.ReturnPeriod == scope.ReturnPeriod)
            .OrderBy(x => x.DocumentDate)
            .ThenBy(x => x.DocumentNbr)
            .ThenBy(x => x.Gkey)
            .ToListAsync(cancellationToken);

        var response = new Gstr1ValidationResponse
        {
            SupplierGstin = scope.SupplierGstin,
            ReturnPeriod = scope.ReturnPeriod,
            DocumentCount = documents.Count,
            ReportableDocumentCount =
                documents.Count(x => x.IsReportable)
        };

        foreach (var document in documents)
        {
            ValidateDocument(
                document,
                response);

            if (document.IsReportable)
            {
                ValidateLines(
                    document,
                    response);

                ValidateReconciliation(
                    document,
                    response);
            }
        }

        await ValidateTable12Async(documents, scope.SupplierGstin, scope.ReturnPeriod, response, cancellationToken);
        await ValidateTable13Async(documents, scope.SupplierGstin, scope.ReturnPeriod, response, cancellationToken);

        return response;
    }

    private async Task ValidateTable13Async(
        IReadOnlyList<GstGstr1Document> documents,
        string supplierGstin,
        string returnPeriod,
        Gstr1ValidationResponse response,
        CancellationToken cancellationToken)
    {
        var issued = await _documentsIssuedService.GetAsync(
            supplierGstin,
            returnPeriod,
            cancellationToken);

        var issueDocument = documents.FirstOrDefault(x => x.IsReportable)
            ?? documents.FirstOrDefault();

        var seriesKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var series in issued.Series)
        {
            if (string.IsNullOrWhiteSpace(series.Series) ||
                string.IsNullOrWhiteSpace(series.FromNumber) ||
                string.IsNullOrWhiteSpace(series.ToNumber))
            {
                AddIssue(response, issueDocument, "Error", "GST-DOCISS-001",
                    "Table 13 series identity and number range are required.");
            }

            if (series.TotalIssued < 0)
            {
                AddIssue(response, issueDocument, "Error", "GST-DOCISS-002",
                    $"Table 13 series '{series.Series}' has a negative TotalIssued count.",
                    fieldName: nameof(series.TotalIssued));
            }

            if (series.Cancelled < 0 || series.Cancelled > series.TotalIssued)
            {
                AddIssue(response, issueDocument, "Error", "GST-DOCISS-003",
                    $"Table 13 series '{series.Series}' has an invalid Cancelled count.",
                    fieldName: nameof(series.Cancelled));
            }

            if (series.NetIssued != series.TotalIssued - series.Cancelled)
            {
                AddIssue(response, issueDocument, "Error", "GST-DOCISS-004",
                    $"Table 13 series '{series.Series}' has an inconsistent NetIssued count.",
                    fieldName: nameof(series.NetIssued));
            }

            var seriesKey = $"{series.DocumentType.Trim()}\u001f{series.Series.Trim()}";
            if (!seriesKeys.Add(seriesKey))
            {
                AddIssue(response, issueDocument, "Error", "GST-DOCISS-005",
                    $"Table 13 contains duplicate rows for document type '{series.DocumentType}' and series '{series.Series}'.");
            }
        }

        if (issued.TotalIssued != issued.Series.Sum(x => x.TotalIssued) ||
            issued.TotalCancelled != issued.Series.Sum(x => x.Cancelled) ||
            issued.TotalNetIssued != issued.Series.Sum(x => x.NetIssued))
        {
            AddIssue(response, issueDocument, "Error", "GST-DOCISS-006",
                "Table 13 response totals do not match the returned series totals.");
        }

        if (response.ReportableDocumentCount > 0 && issued.TotalIssued == 0)
        {
            AddIssue(response, issueDocument, "Error", "GST-DOCISS-007",
                "Reportable GSTR-1 sales documents exist, but Table 13 contains no issued Sales Invoices.");
        }
    }
    private async Task ValidateTable12Async(
        IReadOnlyList<GstGstr1Document> documents,
        string supplierGstin,
        string returnPeriod,
        Gstr1ValidationResponse response,
        CancellationToken cancellationToken)
    {
        var summary = await _hsnSummaryService.GetSummaryAsync(
            new Gstr1HsnSummaryQuery { SupplierGstin = supplierGstin, ReturnPeriod = returnPeriod }, cancellationToken);
        var summaryRows = summary.B2B.Concat(summary.B2C).ToList();
        var issueDocument = documents.FirstOrDefault(x => x.IsReportable);
        if (issueDocument is not null)
        {
            foreach (var duplicate in summaryRows.GroupBy(x => new { x.SupplyClass, HsnCode = x.HsnCode.Trim(), Uqc = x.Uqc.Trim().ToUpperInvariant(), x.GstRate }).Where(x => x.Count() > 1))
                AddIssue(response, issueDocument, "Error", "GST-HSN-002", $"Duplicate Table 12 aggregation key '{duplicate.Key.SupplyClass}/{duplicate.Key.HsnCode}/{duplicate.Key.Uqc}/{duplicate.Key.GstRate}'.");
        }
        var expectedRows = documents.Where(x => x.IsReportable).SelectMany(document => document.GstGstr1DocumentLines.Select(line => new { Document = document, Line = line, SupplyClass = _table12Policy.Resolve(document.ReturnCategory, document.IsRecipientRegistered) })).Where(x => x.SupplyClass != Gstr1Table12SupplyClass.Excluded && !string.IsNullOrWhiteSpace(x.Line.HsnCode) && !string.IsNullOrWhiteSpace(x.Line.Uqc) && x.Line.GstQuantity.HasValue && x.Line.GstQuantity.Value > 0M && x.Line.TaxableValue >= 0M && x.Line.CgstAmount >= 0M && x.Line.SgstAmount >= 0M && x.Line.IgstAmount >= 0M && x.Line.CessAmount >= 0M).GroupBy(x => new { SupplyClass = x.SupplyClass == Gstr1Table12SupplyClass.B2B ? "B2B" : "B2C", HsnCode = x.Line.HsnCode!.Trim(), Uqc = x.Line.Uqc!.Trim().ToUpperInvariant(), x.Line.GstRate }).Select(g => new { g.Key, Quantity = g.Sum(x => x.Line.GstQuantity!.Value), TaxableValue = g.Sum(x => x.Line.TaxableValue), CgstAmount = g.Sum(x => x.Line.CgstAmount), SgstAmount = g.Sum(x => x.Line.SgstAmount), IgstAmount = g.Sum(x => x.Line.IgstAmount), CessAmount = g.Sum(x => x.Line.CessAmount), Document = g.Select(x => x.Document).First() }).ToList();
        foreach (var expected in expectedRows)
        {
            var actual = summaryRows.FirstOrDefault(x => string.Equals(x.SupplyClass, expected.Key.SupplyClass, StringComparison.OrdinalIgnoreCase) && string.Equals(x.HsnCode.Trim(), expected.Key.HsnCode, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Uqc.Trim(), expected.Key.Uqc, StringComparison.OrdinalIgnoreCase) && x.GstRate == expected.Key.GstRate);
            if (actual is null)
            {
                AddIssue(response, expected.Document, "Error", "GST-HSN-001", $"Table 12 aggregation row is missing for key '{expected.Key.SupplyClass}/{expected.Key.HsnCode}/{expected.Key.Uqc}/{expected.Key.GstRate}'.");
                continue;
            }
            AddTable12Mismatch(response, expected.Document, expected.Key.ToString()!, "GST-HSN-003", "quantity", expected.Quantity, actual.TotalQuantity);
            AddTable12Mismatch(response, expected.Document, expected.Key.ToString()!, "GST-HSN-004", "taxable value", expected.TaxableValue, actual.TaxableValue);
            AddTable12Mismatch(response, expected.Document, expected.Key.ToString()!, "GST-HSN-005", "CGST", expected.CgstAmount, actual.CgstAmount);
            AddTable12Mismatch(response, expected.Document, expected.Key.ToString()!, "GST-HSN-006", "SGST", expected.SgstAmount, actual.SgstAmount);
            AddTable12Mismatch(response, expected.Document, expected.Key.ToString()!, "GST-HSN-007", "IGST", expected.IgstAmount, actual.IgstAmount);
            AddTable12Mismatch(response, expected.Document, expected.Key.ToString()!, "GST-HSN-008", "cess", expected.CessAmount, actual.CessAmount);
        }
    }

    private static void AddTable12Mismatch(Gstr1ValidationResponse response, GstGstr1Document document, string key, string code, string fieldName, decimal expected, decimal actual)
    {
        if (Math.Abs(expected - actual) <= ReconciliationTolerance) return;
        AddIssue(response, document, "Error", code, $"Table 12 {fieldName} for aggregation key '{key}' expected {expected:N2} but was {actual:N2}.", fieldName: fieldName);
    }
    // =========================================================
    // DOCUMENT VALIDATION
    // =========================================================

    private static void ValidateDocument(
        GstGstr1Document document,
        Gstr1ValidationResponse response)
    {
        if (string.IsNullOrWhiteSpace(
                document.DocumentNbr))
        {
            AddIssue(
                response,
                document,
                severity: "Error",
                code: "GST-DOC-001",
                message:
                    "Document number is required.",
                fieldName:
                    nameof(document.DocumentNbr));
        }

        if (document.DocumentDate == default)
        {
            AddIssue(
                response,
                document,
                severity: "Error",
                code: "GST-DOC-002",
                message:
                    "Document date is required.",
                fieldName:
                    nameof(document.DocumentDate));
        }

        if (!IsValidStateCode(
                document.PlaceOfSupplyCode))
        {
            AddIssue(
                response,
                document,
                severity: "Error",
                code: "GST-DOC-003",
                message:
                    $"Place of supply code " +
                    $"'{document.PlaceOfSupplyCode}' is invalid.",
                fieldName:
                    nameof(document.PlaceOfSupplyCode));
        }

        if (document.IsRecipientRegistered &&
            !IsValidGstin(document.RecipientGstin))
        {
            AddIssue(
                response,
                document,
                severity: "Error",
                code: "GST-DOC-004",
                message:
                    "A registered recipient must have a valid GSTIN.",
                fieldName:
                    nameof(document.RecipientGstin));
        }

        ValidateCategory(
            document,
            response);

        ValidateTaxType(
            document,
            response);
    }

    // =========================================================
    // CATEGORY CONSISTENCY
    // =========================================================

    private static void ValidateCategory(
        GstGstr1Document document,
        Gstr1ValidationResponse response)
    {
        var category =
            document.ReturnCategory?.Trim();

        if (string.Equals(
                category,
                "B2B",
                StringComparison.OrdinalIgnoreCase))
        {
            if (!document.IsRecipientRegistered ||
                !IsValidGstin(document.RecipientGstin))
            {
                AddIssue(
                    response,
                    document,
                    severity: "Error",
                    code: "GST-DOC-005",
                    message:
                        "B2B supply requires a registered " +
                        "recipient with a valid GSTIN.",
                    fieldName:
                        nameof(document.ReturnCategory));
            }

            return;
        }

        if (string.Equals(
                category,
                "B2CL",
                StringComparison.OrdinalIgnoreCase))
        {
            if (document.IsRecipientRegistered ||
                !string.Equals(
                    document.SupplyType,
                    "InterState",
                    StringComparison.OrdinalIgnoreCase))
            {
                AddIssue(
                    response,
                    document,
                    severity: "Error",
                    code: "GST-DOC-005",
                    message:
                        "B2CL supply must be an inter-state " +
                        "supply to an unregistered recipient.",
                    fieldName:
                        nameof(document.ReturnCategory));
            }

            return;
        }

        if (string.Equals(
                category,
                "B2CS",
                StringComparison.OrdinalIgnoreCase))
        {
            if (document.IsRecipientRegistered)
            {
                AddIssue(
                    response,
                    document,
                    severity: "Error",
                    code: "GST-DOC-005",
                    message:
                        "B2CS supply cannot have a " +
                        "registered recipient.",
                    fieldName:
                        nameof(document.ReturnCategory));
            }
        }
    }

    // =========================================================
    // TAX TYPE CONSISTENCY
    // =========================================================

    private static void ValidateTaxType(
        GstGstr1Document document,
        Gstr1ValidationResponse response)
    {
        if (!document.IsReportable)
            return;

        if (string.Equals(
                document.SupplyType,
                "IntraState",
                StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(
                    document.TaxType,
                    "CgstSgst",
                    StringComparison.OrdinalIgnoreCase))
            {
                AddIssue(
                    response,
                    document,
                    severity: "Error",
                    code: "GST-DOC-006",
                    message:
                        "Intra-state taxable supply must " +
                        "use CGST/SGST tax type.",
                    fieldName:
                        nameof(document.TaxType));
            }

            return;
        }

        if (string.Equals(
                document.SupplyType,
                "InterState",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                document.SupplyType,
                "Export",
                StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(
                    document.TaxType,
                    "Igst",
                    StringComparison.OrdinalIgnoreCase))
            {
                AddIssue(
                    response,
                    document,
                    severity: "Error",
                    code: "GST-DOC-006",
                    message:
                        "Inter-state/export taxable supply " +
                        "must use IGST tax type.",
                    fieldName:
                        nameof(document.TaxType));
            }
        }
    }

    // =========================================================
    // LINE VALIDATION
    // =========================================================

    private static void ValidateLines(
        GstGstr1Document document,
        Gstr1ValidationResponse response)
    {
        var lines =
            document.GstGstr1DocumentLines;

        if (lines.Count == 0)
        {
            AddIssue(
                response,
                document,
                severity: "Error",
                code: "GST-LINE-001",
                message:
                    "Reportable document does not contain " +
                    "any GST staging lines.");

            return;
        }

        foreach (var line in
                 lines.OrderBy(x => x.LineNbr))
        {
            // -------------------------------------------------
            // HSN
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    line.HsnCode))
            {
                AddIssue(
                    response,
                    document,
                    severity: "Error",
                    code: "GST-LINE-002",
                    message:
                        $"HSN code is missing for " +
                        $"line {line.LineNbr}.",
                    lineNbr:
                        line.LineNbr,
                    hsnCode:
                        line.HsnCode,
                    fieldName:
                        nameof(line.HsnCode));
            }

            // -------------------------------------------------
            // TAXABLE VALUE
            // -------------------------------------------------

            if (line.TaxableValue < 0M)
            {
                AddIssue(
                    response,
                    document,
                    severity: "Error",
                    code: "GST-LINE-003",
                    message:
                        $"Taxable value cannot be negative " +
                        $"for line {line.LineNbr}.",
                    lineNbr:
                        line.LineNbr,
                    hsnCode:
                        line.HsnCode,
                    fieldName:
                        nameof(line.TaxableValue));
            }

            // -------------------------------------------------
            // TAX AMOUNTS
            // -------------------------------------------------

            if (line.CgstAmount < 0M ||
                line.SgstAmount < 0M ||
                line.IgstAmount < 0M ||
                line.CessAmount < 0M)
            {
                AddIssue(
                    response,
                    document,
                    severity: "Error",
                    code: "GST-LINE-004",
                    message:
                        $"GST tax amounts cannot be negative " +
                        $"for line {line.LineNbr}.",
                    lineNbr:
                        line.LineNbr,
                    hsnCode:
                        line.HsnCode,
                    fieldName:
                        "TaxAmount");
            }

            // -------------------------------------------------
            // GST UQC
            //
            // UQC is required for GST HSN reporting.
            // Do not silently substitute NOS or another UQC.
            // The staging process must derive the correct UQC
            // from the application's product/UOM information.
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    line.Uqc))
            {
                AddIssue(
                    response,
                    document,
                    severity: "Error",
                    code: "GST-LINE-005",
                    message:
                        $"GST UQC is required for reportable " +
                        $"line {line.LineNbr}.",
                    lineNbr:
                        line.LineNbr,
                    hsnCode:
                        line.HsnCode,
                    fieldName:
                        nameof(line.Uqc));
            }

            // -------------------------------------------------
            // GST QUANTITY
            //
            // This is intentionally separate from Quantity.
            //
            // Quantity represents the application's business
            // quantity (for example one ornament).
            //
            // GstQuantity represents the quantity corresponding
            // to the GST UQC (for example net grams).
            // -------------------------------------------------

            if (!line.GstQuantity.HasValue ||
                line.GstQuantity.Value <= 0M)
            {
                AddIssue(
                    response,
                    document,
                    severity: "Error",
                    code: "GST-LINE-006",
                    message:
                        $"GST quantity must be greater than " +
                        $"zero for reportable line " +
                        $"{line.LineNbr}.",
                    lineNbr:
                        line.LineNbr,
                    hsnCode:
                        line.HsnCode,
                    fieldName:
                        nameof(line.GstQuantity));
            }

            // -------------------------------------------------
            // SOURCE UOM
            //
            // UOM is retained for traceability back to the
            // application/product master.
            //
            // Missing source UOM does not by itself make the
            // GST return invalid if valid UQC/GST quantity are
            // available, therefore this remains a warning.
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    line.Uom))
            {
                AddIssue(
                    response,
                    document,
                    severity: "Warning",
                    code: "GST-LINE-007",
                    message:
                        $"Source UOM is not available for " +
                        $"line {line.LineNbr}.",
                    lineNbr:
                        line.LineNbr,
                    hsnCode:
                        line.HsnCode,
                    fieldName:
                        nameof(line.Uom));
            }
        }
    }

    // =========================================================
    // HEADER ↔ LINE RECONCILIATION
    // =========================================================

    private static void ValidateReconciliation(
        GstGstr1Document document,
        Gstr1ValidationResponse response)
    {
        if (document.GstGstr1DocumentLines.Count == 0)
            return;

        var lineTaxable =
            document.GstGstr1DocumentLines
                .Sum(x => x.TaxableValue);

        var lineCgst =
            document.GstGstr1DocumentLines
                .Sum(x => x.CgstAmount);

        var lineSgst =
            document.GstGstr1DocumentLines
                .Sum(x => x.SgstAmount);

        var lineIgst =
            document.GstGstr1DocumentLines
                .Sum(x => x.IgstAmount);

        var lineCess =
            document.GstGstr1DocumentLines
                .Sum(x => x.CessAmount);

        if (!Matches(
                document.TaxableValue,
                lineTaxable))
        {
            AddIssue(
                response,
                document,
                severity: "Error",
                code: "GST-REC-001",
                message:
                    $"Header taxable value " +
                    $"{document.TaxableValue:N2} " +
                    $"does not match line total " +
                    $"{lineTaxable:N2}.",
                fieldName:
                    nameof(document.TaxableValue));
        }

        if (!Matches(
                document.CgstAmount,
                lineCgst))
        {
            AddIssue(
                response,
                document,
                severity: "Error",
                code: "GST-REC-002",
                message:
                    $"Header CGST " +
                    $"{document.CgstAmount:N2} " +
                    $"does not match line total " +
                    $"{lineCgst:N2}.",
                fieldName:
                    nameof(document.CgstAmount));
        }

        if (!Matches(
                document.SgstAmount,
                lineSgst))
        {
            AddIssue(
                response,
                document,
                severity: "Error",
                code: "GST-REC-003",
                message:
                    $"Header SGST " +
                    $"{document.SgstAmount:N2} " +
                    $"does not match line total " +
                    $"{lineSgst:N2}.",
                fieldName:
                    nameof(document.SgstAmount));
        }

        if (!Matches(
                document.IgstAmount,
                lineIgst))
        {
            AddIssue(
                response,
                document,
                severity: "Error",
                code: "GST-REC-004",
                message:
                    $"Header IGST " +
                    $"{document.IgstAmount:N2} " +
                    $"does not match line total " +
                    $"{lineIgst:N2}.",
                fieldName:
                    nameof(document.IgstAmount));
        }

        if (!Matches(
                document.CessAmount,
                lineCess))
        {
            AddIssue(
                response,
                document,
                severity: "Error",
                code: "GST-REC-005",
                message:
                    $"Header cess " +
                    $"{document.CessAmount:N2} " +
                    $"does not match line total " +
                    $"{lineCess:N2}.",
                fieldName:
                    nameof(document.CessAmount));
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static bool Matches(
        decimal headerValue,
        decimal lineValue)
    {
        return Math.Abs(
                   headerValue - lineValue)
               <= ReconciliationTolerance;
    }

    private static bool IsValidStateCode(
        string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(
                stateCode))
        {
            return false;
        }

        var value =
            stateCode.Trim();

        if (value.Length != 2)
            return false;

        return int.TryParse(
                   value,
                   NumberStyles.None,
                   CultureInfo.InvariantCulture,
                   out var numericCode)
               && numericCode > 0
               && numericCode <= 38;
    }

    private static bool IsValidGstin(
        string? gstin)
    {
        if (string.IsNullOrWhiteSpace(
                gstin))
        {
            return false;
        }

        var value =
            gstin.Trim()
                .ToUpperInvariant();

        if (value.Length != 15)
            return false;

        /*
         * Structural GSTIN validation only.
         *
         * Detailed checksum validation belongs in the reusable
         * GST domain/core layer rather than DataAccess.
         */
        return value.All(
            char.IsLetterOrDigit);
    }

    private static void AddIssue(
        Gstr1ValidationResponse response,
        GstGstr1Document? document,
        string severity,
        string code,
        string message,
        int? lineNbr = null,
        string? hsnCode = null,
        string? fieldName = null)
    {
        response.Issues.Add(
            new Gstr1ValidationIssueResponse
            {
                Severity = severity,
                Code = code,
                Message = message,
                DocumentGkey =
                    document?.Gkey,
                SourceGkey =
                    document?.SourceGkey,
                DocumentNbr =
                    document?.DocumentNbr,
                LineNbr =
                    lineNbr,
                HsnCode =
                    hsnCode,
                FieldName =
                    fieldName
            });
    }

    private static (
        string SupplierGstin,
        string ReturnPeriod)
        ValidateScope(
            string supplierGstin,
            string returnPeriod)
    {
        if (string.IsNullOrWhiteSpace(
                supplierGstin))
        {
            throw new ArgumentException(
                "Supplier GSTIN is required.",
                nameof(supplierGstin));
        }

        if (string.IsNullOrWhiteSpace(
                returnPeriod) ||
            returnPeriod.Length != 6 ||
            !DateTime.TryParseExact(
                returnPeriod,
                "yyyyMM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            throw new ArgumentException(
                "Return period must be in yyyyMM format.",
                nameof(returnPeriod));
        }

        return (
            supplierGstin
                .Trim()
                .ToUpperInvariant(),
            returnPeriod.Trim());
    }
}