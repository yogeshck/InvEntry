using System.Globalization;
using DataAccess.Models;
using InvEntry.Contracts.Gst;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public sealed class Gstr1ValidationService : IGstr1ValidationService
{
    private const decimal ReconciliationTolerance = 0.01M;

    private readonly MijmsContext _context;

    public Gstr1ValidationService(MijmsContext context)
    {
        _context = context;
    }

    public async Task<Gstr1ValidationResponse> ValidateAsync(
        string supplierGstin,
        string returnPeriod,
        CancellationToken cancellationToken = default)
    {
        var scope = ValidateScope(supplierGstin, returnPeriod);

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
            ReportableDocumentCount = documents.Count(x => x.IsReportable)
        };

        foreach (var document in documents)
        {
            ValidateDocument(document, response);

            if (document.IsReportable)
            {
                ValidateLines(document, response);
                ValidateReconciliation(document, response);
            }
        }

        AddExportPreparationIssues(documents, response);

        return response;
    }

    // =========================================================
    // DOCUMENT VALIDATION
    // =========================================================

    private static void ValidateDocument(
        GstGstr1Document document,
        Gstr1ValidationResponse response)
    {
        if (string.IsNullOrWhiteSpace(document.DocumentNbr))
        {
            AddIssue(
                response,
                document,
                severity: "Error",
                code: "GST-DOC-001",
                message: "Document number is required.",
                fieldName: nameof(document.DocumentNbr));
        }

        if (document.DocumentDate == default)
        {
            AddIssue(
                response,
                document,
                severity: "Error",
                code: "GST-DOC-002",
                message: "Document date is required.",
                fieldName: nameof(document.DocumentDate));
        }

        if (!IsValidStateCode(document.PlaceOfSupplyCode))
        {
            AddIssue(
                response,
                document,
                severity: "Error",
                code: "GST-DOC-003",
                message:
                    $"Place of supply code '{document.PlaceOfSupplyCode}' is invalid.",
                fieldName: nameof(document.PlaceOfSupplyCode));
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
                fieldName: nameof(document.RecipientGstin));
        }

        ValidateCategory(document, response);
        ValidateTaxType(document, response);
    }

    // =========================================================
    // CATEGORY CONSISTENCY
    // =========================================================

    private static void ValidateCategory(
        GstGstr1Document document,
        Gstr1ValidationResponse response)
    {
        var category = document.ReturnCategory?.Trim();

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
                        "B2B supply requires a registered recipient with a valid GSTIN.",
                    fieldName: nameof(document.ReturnCategory));
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
                        "B2CL supply must be an inter-state supply to an unregistered recipient.",
                    fieldName: nameof(document.ReturnCategory));
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
                        "B2CS supply cannot have a registered recipient.",
                    fieldName: nameof(document.ReturnCategory));
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
                        "Intra-state taxable supply must use CGST/SGST tax type.",
                    fieldName: nameof(document.TaxType));
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
                        "Inter-state/export taxable supply must use IGST tax type.",
                    fieldName: nameof(document.TaxType));
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
        var lines = document.GstGstr1DocumentLines;

        if (lines.Count == 0)
        {
            AddIssue(
                response,
                document,
                severity: "Error",
                code: "GST-LINE-001",
                message:
                    "Reportable document does not contain any GST staging lines.");

            return;
        }

        foreach (var line in lines.OrderBy(x => x.LineNbr))
        {
            if (string.IsNullOrWhiteSpace(line.HsnCode))
            {
                AddIssue(
                    response,
                    document,
                    severity: "Error",
                    code: "GST-LINE-002",
                    message:
                        $"HSN code is missing for line {line.LineNbr}.",
                    lineNbr: line.LineNbr,
                    hsnCode: line.HsnCode,
                    fieldName: nameof(line.HsnCode));
            }

            if (line.TaxableValue < 0M)
            {
                AddIssue(
                    response,
                    document,
                    severity: "Error",
                    code: "GST-LINE-003",
                    message:
                        $"Taxable value cannot be negative for line {line.LineNbr}.",
                    lineNbr: line.LineNbr,
                    hsnCode: line.HsnCode,
                    fieldName: nameof(line.TaxableValue));
            }

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
                        $"GST tax amounts cannot be negative for line {line.LineNbr}.",
                    lineNbr: line.LineNbr,
                    hsnCode: line.HsnCode,
                    fieldName: "TaxAmount");
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
            document.GstGstr1DocumentLines.Sum(x => x.TaxableValue);

        var lineCgst =
            document.GstGstr1DocumentLines.Sum(x => x.CgstAmount);

        var lineSgst =
            document.GstGstr1DocumentLines.Sum(x => x.SgstAmount);

        var lineIgst =
            document.GstGstr1DocumentLines.Sum(x => x.IgstAmount);

        var lineCess =
            document.GstGstr1DocumentLines.Sum(x => x.CessAmount);


        if (!Matches(document.TaxableValue, lineTaxable))
        {
            AddIssue(
                response,
                document,
                severity: "Error",
                code: "GST-REC-001",
                message:
                    $"Header taxable value {document.TaxableValue:N2} " +
                    $"does not match line total {lineTaxable:N2}.",
                fieldName: nameof(document.TaxableValue));
        }


        if (!Matches(document.CgstAmount, lineCgst))
        {
            AddIssue(
                response,
                document,
                severity: "Error",
                code: "GST-REC-002",
                message:
                    $"Header CGST {document.CgstAmount:N2} " +
                    $"does not match line total {lineCgst:N2}.",
                fieldName: nameof(document.CgstAmount));
        }


        if (!Matches(document.SgstAmount, lineSgst))
        {
            AddIssue(
                response,
                document,
                severity: "Error",
                code: "GST-REC-003",
                message:
                    $"Header SGST {document.SgstAmount:N2} " +
                    $"does not match line total {lineSgst:N2}.",
                fieldName: nameof(document.SgstAmount));
        }


        if (!Matches(document.IgstAmount, lineIgst))
        {
            AddIssue(
                response,
                document,
                severity: "Error",
                code: "GST-REC-004",
                message:
                    $"Header IGST {document.IgstAmount:N2} " +
                    $"does not match line total {lineIgst:N2}.",
                fieldName: nameof(document.IgstAmount));
        }


        if (!Matches(document.CessAmount, lineCess))
        {
            AddIssue(
                response,
                document,
                severity: "Error",
                code: "GST-REC-005",
                message:
                    $"Header cess {document.CessAmount:N2} " +
                    $"does not match line total {lineCess:N2}.",
                fieldName: nameof(document.CessAmount));
        }
    }

    // =========================================================
    // EXPORT PREPARATION
    // =========================================================

    private static void AddExportPreparationIssues(
        IReadOnlyCollection<GstGstr1Document> documents,
        Gstr1ValidationResponse response)
    {
        var hasReportableLines = documents
            .Where(x => x.IsReportable)
            .SelectMany(x => x.GstGstr1DocumentLines)
            .Any();

        if (!hasReportableLines)
            return;

        /*
         * Current staging schema does not yet contain UQC.
         *
         * Do not create one warning for every line. That would make
         * a monthly return unnecessarily noisy. This return-level
         * warning records the known export-readiness gap once.
         *
         * When UQC is added to staging this rule should become a
         * line-level validation rule.
         */
        response.Issues.Add(
            new Gstr1ValidationIssueResponse
            {
                Severity = "Warning",
                Code = "GST-EXP-001",
                Message =
                    "UQC information is not currently stored in GST staging. " +
                    "UQC must be addressed before HSN/GSTR-1 JSON export.",
                FieldName = "Uqc"
            });
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static bool Matches(decimal headerValue, decimal lineValue)
    {
        return Math.Abs(headerValue - lineValue)
               <= ReconciliationTolerance;
    }

    private static bool IsValidStateCode(string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return false;

        var value = stateCode.Trim();

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

    private static bool IsValidGstin(string? gstin)
    {
        if (string.IsNullOrWhiteSpace(gstin))
            return false;

        var value = gstin.Trim().ToUpperInvariant();

        if (value.Length != 15)
            return false;

        /*
         * Structural GSTIN validation only.
         *
         * Detailed checksum validation belongs in the reusable
         * GST domain/core layer rather than DataAccess.
         */
        return value.All(char.IsLetterOrDigit);
    }

    private static void AddIssue(
        Gstr1ValidationResponse response,
        GstGstr1Document document,
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
                DocumentGkey = document.Gkey,
                SourceGkey = document.SourceGkey,
                DocumentNbr = document.DocumentNbr,
                LineNbr = lineNbr,
                HsnCode = hsnCode,
                FieldName = fieldName
            });
    }

    private static (string SupplierGstin, string ReturnPeriod) ValidateScope(
        string supplierGstin,
        string returnPeriod)
    {
        if (string.IsNullOrWhiteSpace(supplierGstin))
        {
            throw new ArgumentException(
                "Supplier GSTIN is required.",
                nameof(supplierGstin));
        }

        if (string.IsNullOrWhiteSpace(returnPeriod) ||
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
            supplierGstin.Trim().ToUpperInvariant(),
            returnPeriod.Trim());
    }
}