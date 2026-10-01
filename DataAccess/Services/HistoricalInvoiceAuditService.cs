using DataAccess.Models;
using InvEntry.Contracts.Gst;
using InvEntry.Gst.Core.Classification;
using InvEntry.Gst.Core.Models;
using InvEntry.Gst.Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public sealed class HistoricalInvoiceAuditService : IHistoricalInvoiceAuditService
{
    private const string SalesInvoiceDocumentType = "SalesInvoice";
    private readonly MijmsContext _context;
    private readonly ICurrentCompanyGstinProvider _currentCompanyGstinProvider;
    private readonly IGstClassificationService _gstClassificationService;

    public HistoricalInvoiceAuditService(
        MijmsContext context,
        ICurrentCompanyGstinProvider currentCompanyGstinProvider,
        IGstClassificationService gstClassificationService)
    {
        _context = context;
        _currentCompanyGstinProvider = currentCompanyGstinProvider;
        _gstClassificationService = gstClassificationService;
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

        var supplierStateCode = await _context.OrgThisCompanyViews
            .AsNoTracking()
            .Where(x => x.ThisCompany == true)
            .Select(x => x.GstCode)
            .SingleOrDefaultAsync(cancellationToken);
        supplierStateCode = string.IsNullOrWhiteSpace(supplierStateCode)
            ? GstinHelper.GetStateCode(normalizedGstin)
            : supplierStateCode.Trim();

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
                CustomerGstin = customer == null ? null : customer.GstinNbr,
                CustomerRegistrationContext = customer == null ? null : customer.CustomerType,
                CustomerStateCode = customer == null ? null : customer.GstStateCode,
                CustomerExists = customer != null,
                IsTaxApplicable = invoice.IsTaxApplicable,
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

        var invoiceGkeys = rows.Select(x => x.InvoiceGkey).ToList();
        var lineRows = await (
            from line in _context.InvoiceLines.AsNoTracking()
            where line.InvoiceHdrGkey.HasValue && invoiceGkeys.Contains(line.InvoiceHdrGkey.Value)
            join product in _context.Products.AsNoTracking()
                on line.ProductGkey equals (int?)product.Gkey into productJoin
            from product in productJoin.DefaultIfEmpty()
            orderby line.InvoiceHdrGkey, line.InvLineNbr, line.Gkey
            select new AuditLineRow
            {
                InvoiceGkey = line.InvoiceHdrGkey!.Value,
                ItemDescription = !string.IsNullOrWhiteSpace(line.ProductDesc) ? line.ProductDesc :
                    !string.IsNullOrWhiteSpace(line.ProductName) ? line.ProductName : line.ItemNotes,
                HsnCode = line.HsnCode,
                Uom = product == null ? null : product.Uom,
                Quantity = line.ProdQty,
                GrossWeight = line.ProdGrossWeight,
                NetWeight = line.ProdNetWeight,
                TaxableValue = line.InvlTaxableAmount ?? 0M,
                CgstRate = line.InvlCgstPercent ?? 0M,
                SgstRate = line.InvlSgstPercent ?? 0M,
                IgstRate = line.InvlIgstPercent ?? 0M,
                CgstAmount = line.InvlCgstAmount ?? 0M,
                SgstAmount = line.InvlSgstAmount ?? 0M,
                IgstAmount = line.InvlIgstAmount ?? 0M,
                TaxAmount = line.TaxAmount,
                ProductCategory = line.ProdCategory,
                IsTaxable = line.IsTaxable
            }).ToListAsync(cancellationToken);

        var linesByInvoice = lineRows.GroupBy(x => x.InvoiceGkey)
            .ToDictionary(x => x.Key, x => x.ToList());

        var oldGoldRows = await _context.OldMetalTransactions
            .AsNoTracking()
            .Where(x => x.DocRefGkey.HasValue && invoiceGkeys.Contains(x.DocRefGkey.Value))
            .OrderBy(x => x.DocRefGkey)
            .ThenBy(x => x.TransNbr)
            .ThenBy(x => x.Gkey)
            .Select(x => new AuditOldGoldRow
            {
                InvoiceGkey = x.DocRefGkey!.Value,
                DocumentNumber = x.TransNbr,
                TransactionDate = x.TransDate,
                TransactionType = x.TransType,
                Description = x.Remarks ?? x.ProductCategory,
                Metal = x.Metal,
                Purity = x.Purity,
                Uom = x.Uom,
                GrossWeight = x.GrossWeight,
                NetWeight = x.NetWeight,
                FinalPurchasePrice = x.FinalPurchasePrice
            })
            .ToListAsync(cancellationToken);

        var oldGoldByInvoice = oldGoldRows.GroupBy(x => x.InvoiceGkey)
            .ToDictionary(x => x.Key, x => x.ToList());

        var response = new HistoricalInvoiceAuditResponse
        {
            SupplierGstin = normalizedGstin,
            FromDate = startDate,
            ToDate = toDate.Date,
            InvoiceCount = rows.Count
        };

        foreach (var row in rows)
        {
            var sourceLines = linesByInvoice.GetValueOrDefault(row.InvoiceGkey) ?? [];
            var sourceOldGold = oldGoldByInvoice.GetValueOrDefault(row.InvoiceGkey) ?? [];
            var gstTaxableValue = sourceLines.Sum(x => x.TaxableValue);
            var gstCgstAmount = sourceLines.Sum(x => x.CgstAmount);
            var gstSgstAmount = sourceLines.Sum(x => x.SgstAmount);
            var gstIgstAmount = sourceLines.Sum(x => x.IgstAmount);
            var classification = _gstClassificationService.Classify(new GstClassificationRequest
            {
                DocumentType = GstDocumentType.SalesInvoice,
                DocumentNumber = row.InvoiceNbr,
                DocumentDate = row.InvoiceDate.GetValueOrDefault(),
                SupplierGstin = normalizedGstin,
                SupplierStateCode = supplierStateCode?.Trim(),
                RecipientGstin = Normalize(row.CustomerGstin),
                RecipientStateCode = Normalize(row.CustomerStateCode),
                PlaceOfSupplyCode = Normalize(row.PlaceOfSupply),
                TaxableValue = gstTaxableValue,
                InvoiceValue = gstTaxableValue + gstCgstAmount + gstSgstAmount + gstIgstAmount,
                CgstAmount = gstCgstAmount,
                SgstAmount = gstSgstAmount,
                IgstAmount = gstIgstAmount,
                TaxTreatment = row.IsTaxApplicable ? GstTaxTreatment.Taxable : GstTaxTreatment.NonGst
            });

            var completionClassification = HistoricalInvoiceCompletionClassifier.Classify(
                new HistoricalInvoiceCompletionEvidence(
                    row.InvoiceNbr,
                    row.InvoiceLineCount,
                    row.CustomerExists,
                    row.PlaceOfSupply,
                    row.LinkedVoucherCount,
                    row.LinkedArReceiptCount));

            var auditItem = new HistoricalInvoiceAuditItemResponse
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
                CompletionEvidenceClassification = completionClassification.Classification,
                CompletionEvidenceReasons = completionClassification.Reasons.ToList(),
                ProductionTestProvenance = "Unknown",
                ProvenanceReasons =
                [
                    "No dedicated import batch, import source, or production/test marker exists on the invoice.",
                    "Creation and modification metadata are reported as evidence but are not sufficient to classify production versus test data."
                ],
                CustomerGstin = Normalize(row.CustomerGstin),
                CustomerRegistrationContext = row.CustomerRegistrationContext,
                SupplierStateCode = Normalize(supplierStateCode),
                CustomerStateCode = Normalize(row.CustomerStateCode),
                IsTaxApplicable = row.IsTaxApplicable,
                IsRecipientRegistered = classification.IsRecipientRegistered,
                SupplyType = classification.SupplyType.ToString(),
                GstReturnCategory = classification.ReturnCategory.ToString(),
                GstTaxType = classification.TaxType.ToString(),
                GstClassificationValid = classification.IsValid,
                GstClassificationErrors = classification.Errors.ToList(),
                Lines = sourceLines.Select((line, index) => new HistoricalInvoiceAuditLineResponse
                {
                    LineNumber = index + 1,
                    ItemDescription = Normalize(line.ItemDescription),
                    HsnCode = Normalize(line.HsnCode),
                    Uom = Normalize(line.Uom),
                    Uqc = string.Equals(line.Uom?.Trim(), "Grams", StringComparison.OrdinalIgnoreCase) ? "GMS" : null,
                    Quantity = line.Quantity,
                    GrossWeight = line.GrossWeight,
                    NetWeight = line.NetWeight,
                    TaxableValue = line.TaxableValue,
                    GstRate = line.IgstRate > 0M ? line.IgstRate : line.CgstRate + line.SgstRate,
                    CgstRate = line.CgstRate,
                    SgstRate = line.SgstRate,
                    IgstRate = line.IgstRate,
                    CgstAmount = line.CgstAmount,
                    SgstAmount = line.SgstAmount,
                    IgstAmount = line.IgstAmount,
                    TaxAmount = line.TaxAmount,
                    ProductCategory = Normalize(line.ProductCategory),
                    IsTaxable = line.IsTaxable
                }).ToList(),
                HasOldGoldTransaction = sourceOldGold.Count > 0,
                OldGoldTransactions = sourceOldGold.Select(x => new HistoricalInvoiceAuditOldGoldResponse
                {
                    DocumentNumber = Normalize(x.DocumentNumber),
                    TransactionDate = x.TransactionDate,
                    TransactionType = Normalize(x.TransactionType),
                    Description = Normalize(x.Description),
                    Metal = Normalize(x.Metal),
                    Purity = Normalize(x.Purity),
                    Uom = Normalize(x.Uom),
                    GrossWeight = x.GrossWeight,
                    NetWeight = x.NetWeight,
                    FinalPurchasePrice = x.FinalPurchasePrice
                }).ToList()
            };
            auditItem.SourceFingerprint = HistoricalAuditFingerprint.Create(auditItem);
            response.Items.Add(auditItem);
        }

        return response;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
        public string? CustomerGstin { get; init; }
        public string? CustomerRegistrationContext { get; init; }
        public string? CustomerStateCode { get; init; }
        public bool CustomerExists { get; init; }
        public bool IsTaxApplicable { get; init; }
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

    private sealed class AuditLineRow
    {
        public int InvoiceGkey { get; init; }
        public string? ItemDescription { get; init; }
        public string? HsnCode { get; init; }
        public string? Uom { get; init; }
        public decimal Quantity { get; init; }
        public decimal? GrossWeight { get; init; }
        public decimal? NetWeight { get; init; }
        public decimal TaxableValue { get; init; }
        public decimal CgstRate { get; init; }
        public decimal SgstRate { get; init; }
        public decimal IgstRate { get; init; }
        public decimal CgstAmount { get; init; }
        public decimal SgstAmount { get; init; }
        public decimal IgstAmount { get; init; }
        public decimal? TaxAmount { get; init; }
        public string? ProductCategory { get; init; }
        public bool? IsTaxable { get; init; }
    }

    private sealed class AuditOldGoldRow
    {
        public int InvoiceGkey { get; init; }
        public string? DocumentNumber { get; init; }
        public DateTime? TransactionDate { get; init; }
        public string? TransactionType { get; init; }
        public string? Description { get; init; }
        public string? Metal { get; init; }
        public string? Purity { get; init; }
        public string? Uom { get; init; }
        public decimal? GrossWeight { get; init; }
        public decimal? NetWeight { get; init; }
        public decimal? FinalPurchasePrice { get; init; }
    }
}
