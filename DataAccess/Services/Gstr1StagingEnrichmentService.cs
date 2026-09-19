using System.Globalization;
using DataAccess.Models;
using InvEntry.Contracts.Gst;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public sealed class Gstr1StagingEnrichmentService
    : IGstr1StagingEnrichmentService
{
    private readonly MijmsContext _context;

    public Gstr1StagingEnrichmentService(
        MijmsContext context)
    {
        _context = context;
    }

    public async Task<Gstr1EnrichmentResponse> EnrichAsync(
        Gstr1EnrichmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = ValidateRequest(request);

        var response = new Gstr1EnrichmentResponse
        {
            SupplierGstin = scope.SupplierGstin,
            ReturnPeriod = scope.ReturnPeriod,
            DryRun = request.DryRun
        };

        // =====================================================
        // LOAD GST STAGING LINES FOR REQUESTED RETURN
        // =====================================================

        var stagingLines = await (
            from document in _context.GstGstr1Documents
            join line in _context.GstGstr1DocumentLines
                on document.Gkey equals line.GstDocumentGkey
            where
                document.SupplierGstin == scope.SupplierGstin &&
                document.ReturnPeriod == scope.ReturnPeriod &&
                document.IsReportable
            orderby
                document.DocumentDate,
                document.DocumentNbr,
                line.LineNbr
            select new
            {
                Document = document,
                Line = line
            })
            .ToListAsync(cancellationToken);

        response.Examined = stagingLines.Count;

        if (stagingLines.Count == 0)
            return response;

        // =====================================================
        // LOAD SOURCE INVOICE LINES IN ONE QUERY
        // =====================================================

        var sourceLineGkeys = stagingLines
            .Where(x => x.Line.SourceLineGkey.HasValue)
            .Select(x => x.Line.SourceLineGkey!.Value)
            .Distinct()
            .ToList();

        var invoiceLines = await _context.InvoiceLines
            .AsNoTracking()
            .Where(x => sourceLineGkeys.Contains(x.Gkey))
            .ToDictionaryAsync(
                x => x.Gkey,
                cancellationToken);

        // =====================================================
        // LOAD PRODUCTS IN ONE QUERY
        // =====================================================

        var productGkeys = invoiceLines.Values
            .Where(x => x.ProductGkey.HasValue)
            .Select(x => x.ProductGkey!.Value)
            .Distinct()
            .ToList();

        var products = await _context.Products
            .AsNoTracking()
            .Where(x => productGkeys.Contains(x.Gkey))
            .ToDictionaryAsync(
                x => x.Gkey,
                cancellationToken);

        // =====================================================
        // PROCESS EACH STAGING LINE
        // =====================================================

        foreach (var row in stagingLines)
        {
            var document = row.Document;
            var stagingLine = row.Line;

            var item = new Gstr1EnrichmentItemResponse
            {
                DocumentGkey = document.Gkey,
                LineGkey = stagingLine.Gkey,
                SourceGkey = document.SourceGkey,
                SourceLineGkey = stagingLine.SourceLineGkey,
                DocumentNbr = document.DocumentNbr,
                LineNbr = stagingLine.LineNbr,
                HsnCode = stagingLine.HsnCode
            };

            response.Items.Add(item);

            try
            {
                // =================================================
                // ALREADY ENRICHED
                // =================================================

                if (IsAlreadyEnriched(stagingLine))
                {
                    item.Uom = stagingLine.Uom;
                    item.Uqc = stagingLine.Uqc;
                    item.GstQuantity =
                        stagingLine.GstQuantity;

                    item.Status = "AlreadyEnriched";
                    item.Message =
                        "GST staging line already contains " +
                        "UOM, UQC and GST quantity.";

                    response.AlreadyEnriched++;

                    continue;
                }

                // =================================================
                // SOURCE LINE LINK
                // =================================================

                if (!stagingLine.SourceLineGkey.HasValue ||
                    stagingLine.SourceLineGkey.Value <= 0)
                {
                    item.Status = "Skipped";
                    item.Message =
                        "Source invoice line reference is missing.";

                    response.Skipped++;

                    continue;
                }

                if (!invoiceLines.TryGetValue(
                        stagingLine.SourceLineGkey.Value,
                        out var invoiceLine))
                {
                    item.Status = "Skipped";
                    item.Message =
                        $"Source invoice line " +
                        $"{stagingLine.SourceLineGkey.Value} " +
                        $"was not found.";

                    response.Skipped++;

                    continue;
                }

                item.ProductName =
                    invoiceLine.ProductName;

                // =================================================
                // PRODUCT LINK
                // =================================================

                if (!invoiceLine.ProductGkey.HasValue ||
                    invoiceLine.ProductGkey.Value <= 0)
                {
                    item.Status = "Skipped";
                    item.Message =
                        "Product reference is missing on " +
                        "the source invoice line.";

                    response.Skipped++;

                    continue;
                }

                if (!products.TryGetValue(
                        invoiceLine.ProductGkey.Value,
                        out var product))
                {
                    item.Status = "Skipped";
                    item.Message =
                        $"Product {invoiceLine.ProductGkey.Value} " +
                        $"was not found.";

                    response.Skipped++;

                    continue;
                }

                item.ProductName =
                    string.IsNullOrWhiteSpace(product.Name)
                        ? invoiceLine.ProductName
                        : product.Name;

                // =================================================
                // SOURCE UOM
                // =================================================

                var sourceUom =
                    string.IsNullOrWhiteSpace(product.Uom)
                        ? null
                        : product.Uom.Trim();

                if (sourceUom is null)
                {
                    item.Status = "Skipped";
                    item.Message =
                        "Product UOM is missing.";

                    response.Skipped++;

                    continue;
                }

                // =================================================
                // UOM -> GST UQC MAPPING
                //
                // Current supported jewellery rule:
                //
                // Grams -> GMS
                //
                // Do NOT silently convert unsupported units to NOS.
                // =================================================

                if (!string.Equals(
                        sourceUom,
                        "Grams",
                        StringComparison.OrdinalIgnoreCase))
                {
                    item.Uom = sourceUom;

                    item.Status = "Skipped";
                    item.Message =
                        $"No GST UQC mapping has been configured " +
                        $"for product UOM '{sourceUom}'.";

                    response.Skipped++;

                    continue;
                }

                const string gstUqc = "GMS";

                // =================================================
                // GST QUANTITY
                //
                // Jewellery sales are valued using NET WEIGHT.
                // Stone weight is not included in gold quantity.
                // =================================================

                if (!invoiceLine.ProdNetWeight.HasValue ||
                    invoiceLine.ProdNetWeight.Value <= 0M)
                {
                    item.Uom = sourceUom;
                    item.Uqc = gstUqc;

                    item.Status = "Skipped";
                    item.Message =
                        "Source invoice line does not contain " +
                        "a positive net weight.";

                    response.Skipped++;

                    continue;
                }

                var gstQuantity =
                    invoiceLine.ProdNetWeight.Value;

                item.Uom = sourceUom;
                item.Uqc = gstUqc;
                item.GstQuantity = gstQuantity;

                // =================================================
                // DRY RUN
                // =================================================

                if (request.DryRun)
                {
                    item.Status = "WouldEnrich";
                    item.Message =
                        "GST staging line can be enriched.";

                    response.WouldEnrich++;

                    continue;
                }

                // =================================================
                // APPLY ONLY GST ENRICHMENT FIELDS
                //
                // Do not modify:
                // taxable value
                // tax amounts
                // HSN
                // invoice value
                // classification
                // invoice source data
                // =================================================

                stagingLine.Uom = sourceUom;
                stagingLine.Uqc = gstUqc;
                stagingLine.GstQuantity = gstQuantity;

                item.Status = "Enriched";
                item.Message =
                    "GST staging line enriched successfully.";

                response.Enriched++;
            }
            catch (Exception ex)
            {
                item.Status = "Failed";
                item.Message = ex.Message;

                response.Failed++;
            }
        }

        // =====================================================
        // SAVE ONLY WHEN EXECUTING
        // =====================================================

        if (!request.DryRun &&
            response.Enriched > 0)
        {
            try
            {
                await _context.SaveChangesAsync(
                    cancellationToken);
            }
            catch
            {
                /*
                 * Do not return a response claiming that records
                 * were enriched when the database write failed.
                 */
                _context.ChangeTracker.Clear();

                throw;
            }
        }

        return response;
    }

    // =========================================================
    // ALREADY ENRICHED
    // =========================================================

    private static bool IsAlreadyEnriched(
        GstGstr1DocumentLine line)
    {
        return
            !string.IsNullOrWhiteSpace(line.Uom) &&
            !string.IsNullOrWhiteSpace(line.Uqc) &&
            line.GstQuantity.HasValue &&
            line.GstQuantity.Value > 0M;
    }

    // =========================================================
    // REQUEST VALIDATION
    // =========================================================

    private static (
        string SupplierGstin,
        string ReturnPeriod)
        ValidateRequest(
            Gstr1EnrichmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(
                request.SupplierGstin))
        {
            throw new ArgumentException(
                "Supplier GSTIN is required.",
                nameof(request.SupplierGstin));
        }

        var supplierGstin =
            request.SupplierGstin
                .Trim()
                .ToUpperInvariant();

        if (supplierGstin.Length != 15 ||
            !supplierGstin.All(char.IsLetterOrDigit))
        {
            throw new ArgumentException(
                "Supplier GSTIN must contain " +
                "15 alphanumeric characters.",
                nameof(request.SupplierGstin));
        }

        if (string.IsNullOrWhiteSpace(
                request.ReturnPeriod) ||
            request.ReturnPeriod.Length != 6 ||
            !DateTime.TryParseExact(
                request.ReturnPeriod,
                "yyyyMM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            throw new ArgumentException(
                "Return period must be in yyyyMM format.",
                nameof(request.ReturnPeriod));
        }

        return (
            supplierGstin,
            request.ReturnPeriod.Trim());
    }
}