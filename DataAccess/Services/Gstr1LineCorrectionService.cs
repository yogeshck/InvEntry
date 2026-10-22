using System.Globalization;
using DataAccess.Models;
using InvEntry.Contracts.Gst;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

/// <summary>
/// Lets the operator correct the GST reporting fields (HSN, UOM, UQC and
/// GST quantity) of a staged GSTR-1 line flagged by validation.
///
/// Taxable value, rates, tax amounts, classification and the source
/// invoice are never modified here.
/// </summary>
public sealed class Gstr1LineCorrectionService : IGstr1LineCorrectionService
{
    private const int UomMaxLength = 20;

    private readonly MijmsContext _context;

    public Gstr1LineCorrectionService(MijmsContext context)
    {
        _context = context;
    }

    public async Task<Gstr1LineCorrectionResponse?> GetAsync(
        long documentGkey,
        int lineNbr,
        string supplierGstin,
        string returnPeriod,
        CancellationToken cancellationToken = default)
    {
        var scope = ValidateScope(supplierGstin, returnPeriod);

        var found = await FindLineAsync(
            documentGkey, lineNbr, scope, tracking: false, cancellationToken);

        return found is null
            ? null
            : await BuildResponseAsync(found.Value.Document, found.Value.Line, cancellationToken);
    }

    public async Task<Gstr1LineCorrectionResponse?> SaveAsync(
        long documentGkey,
        int lineNbr,
        Gstr1LineCorrectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var scope = ValidateScope(request.SupplierGstin, request.ReturnPeriod);
        var correction = ValidateCorrection(request);

        var found = await FindLineAsync(
            documentGkey, lineNbr, scope, tracking: true, cancellationToken);

        if (found is null)
            return null;

        var (document, line) = found.Value;

        line.HsnCode = correction.HsnCode;
        line.Uom = correction.Uom;
        line.Uqc = correction.Uqc;
        line.GstQuantity = correction.GstQuantity;
        document.UpdatedOn = DateTime.Now;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            _context.ChangeTracker.Clear();
            throw;
        }

        return await BuildResponseAsync(document, line, cancellationToken);
    }

    // =========================================================
    // LOOKUP
    // =========================================================

    private async Task<(GstGstr1Document Document, GstGstr1DocumentLine Line)?> FindLineAsync(
        long documentGkey,
        int lineNbr,
        (string SupplierGstin, string ReturnPeriod) scope,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var documents = _context.GstGstr1Documents.AsQueryable();
        if (!tracking)
            documents = documents.AsNoTracking();

        var document = await documents
            .Include(x => x.GstGstr1DocumentLines)
            .FirstOrDefaultAsync(x =>
                x.Gkey == documentGkey &&
                x.SupplierGstin == scope.SupplierGstin &&
                x.ReturnPeriod == scope.ReturnPeriod,
                cancellationToken);

        if (document is null)
            return null;

        var lines = document.GstGstr1DocumentLines
            .Where(x => x.LineNbr == lineNbr)
            .ToList();

        if (lines.Count > 1)
        {
            throw new InvalidOperationException(
                $"Document {document.DocumentNbr} has more than one line {lineNbr}; " +
                "it cannot be corrected safely.");
        }

        return lines.Count == 0 ? null : (document, lines[0]);
    }

    // =========================================================
    // RESPONSE + SUGGESTIONS
    // =========================================================

    private async Task<Gstr1LineCorrectionResponse> BuildResponseAsync(
        GstGstr1Document document,
        GstGstr1DocumentLine line,
        CancellationToken cancellationToken)
    {
        InvoiceLine? invoiceLine = null;
        Product? product = null;

        if (line.SourceLineGkey is > 0)
        {
            invoiceLine = await _context.InvoiceLines
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Gkey == line.SourceLineGkey.Value, cancellationToken);
        }

        if (invoiceLine?.ProductGkey is > 0)
        {
            product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Gkey == invoiceLine.ProductGkey.Value, cancellationToken);
        }

        var response = new Gstr1LineCorrectionResponse
        {
            DocumentGkey = document.Gkey,
            DocumentNbr = document.DocumentNbr,
            DocumentDate = document.DocumentDate,
            LineNbr = line.LineNbr,
            Description = line.Description,
            Quantity = line.Quantity,
            TaxableValue = line.TaxableValue,
            HsnCode = line.HsnCode,
            Uom = line.Uom,
            Uqc = line.Uqc,
            GstQuantity = line.GstQuantity,
            ProductName = !string.IsNullOrWhiteSpace(product?.Name)
                ? product.Name
                : invoiceLine?.ProductName,
            ProductUom = Trim(product?.Uom),
            ProductHsnCode = Trim(product?.HsnCode),
            InvoiceHsnCode = Trim(invoiceLine?.HsnCode),
            NetWeight = invoiceLine?.ProdNetWeight,
            GrossWeight = invoiceLine?.ProdGrossWeight
        };

        ApplySuggestions(response);

        return response;
    }

    private static void ApplySuggestions(Gstr1LineCorrectionResponse response)
    {
        var notes = new List<string>();

        response.SuggestedHsnCode =
            FirstNonEmpty(response.HsnCode, response.InvoiceHsnCode, response.ProductHsnCode);

        // Product master UOM is the source of truth; fall back to the staged value.
        response.SuggestedUom = FirstNonEmpty(response.ProductUom, response.Uom);

        var mappedUqc = GstUqc.MapFromUom(response.SuggestedUom);

        if (mappedUqc is not null)
        {
            response.SuggestedUqc = mappedUqc;
        }
        else if (GstUqc.IsValid(response.Uqc))
        {
            response.SuggestedUqc = response.Uqc!.Trim().ToUpperInvariant();
        }
        else
        {
            notes.Add(string.IsNullOrWhiteSpace(response.SuggestedUom)
                ? "Product UOM is missing; choose the UOM and UQC."
                : $"No GST UQC mapping is configured for UOM '{response.SuggestedUom}'; choose the UQC.");
        }

        if (GstUqc.IsMismatch(response.Uom, response.Uqc))
        {
            notes.Add($"Staged UOM '{response.Uom}' does not match staged UQC '{response.Uqc}'.");
        }

        if (!string.IsNullOrWhiteSpace(response.ProductUom) &&
            !string.IsNullOrWhiteSpace(response.Uom) &&
            !string.Equals(response.ProductUom, response.Uom.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            notes.Add($"Staged UOM '{response.Uom}' differs from product UOM '{response.ProductUom}'.");
        }

        response.SuggestedGstQuantity =
            GstUqc.SuggestGstQuantity(response.SuggestedUqc, response.Quantity, response.NetWeight)
            ?? (response.GstQuantity is > 0M ? response.GstQuantity : null);

        if (response.SuggestedUqc is "GMS" or "KGS" &&
            response.SuggestedGstQuantity is null)
        {
            notes.Add("Source invoice line has no positive net weight; enter the GST quantity.");
        }

        response.SuggestionNote = notes.Count == 0 ? null : string.Join(" ", notes);
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private static (string HsnCode, string Uom, string Uqc, decimal GstQuantity) ValidateCorrection(
        Gstr1LineCorrectionRequest request)
    {
        var errors = new List<string>();

        var hsn = Trim(request.HsnCode);
        if (hsn is null)
            errors.Add("HSN code is required.");
        else if (!hsn.All(char.IsAsciiDigit) || hsn.Length is not (4 or 6 or 8))
            errors.Add("HSN code must be 4, 6 or 8 digits.");

        var uom = Trim(request.Uom);
        if (uom is null)
            errors.Add("UOM is required.");
        else if (uom.Length > UomMaxLength)
            errors.Add($"UOM cannot exceed {UomMaxLength} characters.");

        var uqc = Trim(request.Uqc)?.ToUpperInvariant();
        if (uqc is null)
            errors.Add("GST UQC is required.");
        else if (!GstUqc.IsValid(uqc))
            errors.Add($"'{uqc}' is not a valid GST UQC.");

        if (uom is not null && uqc is not null && GstUqc.IsMismatch(uom, uqc))
            errors.Add($"UOM '{uom}' must be reported as UQC '{GstUqc.MapFromUom(uom)}', not '{uqc}'.");

        if (request.GstQuantity is not > 0M)
            errors.Add("GST quantity must be greater than zero.");
        else if (request.GstQuantity.Value != Math.Round(request.GstQuantity.Value, 3))
            errors.Add("GST quantity cannot have more than 3 decimal places.");

        if (errors.Count > 0)
            throw new ArgumentException(string.Join(" ", errors));

        return (hsn!, uom!, uqc!, request.GstQuantity!.Value);
    }

    private static (string SupplierGstin, string ReturnPeriod) ValidateScope(
        string supplierGstin,
        string returnPeriod)
    {
        if (string.IsNullOrWhiteSpace(supplierGstin))
            throw new ArgumentException("Supplier GSTIN is required.", nameof(supplierGstin));

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

        return (supplierGstin.Trim().ToUpperInvariant(), returnPeriod.Trim());
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? FirstNonEmpty(params string?[] values) =>
        values.Select(Trim).FirstOrDefault(x => x is not null);
}
