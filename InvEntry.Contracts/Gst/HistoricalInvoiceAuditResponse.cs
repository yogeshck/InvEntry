namespace InvEntry.Contracts.Gst;

public sealed class HistoricalInvoiceAuditResponse
{
    public string SupplierGstin { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int InvoiceCount { get; set; }
    public List<HistoricalInvoiceAuditItemResponse> Items { get; set; } = new();
}

public sealed class HistoricalInvoiceAuditItemResponse
{
    public string SourceFingerprint { get; set; } = string.Empty;
    public int InvoiceGkey { get; set; }
    public string? InvoiceNbr { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public int? CustomerGkey { get; set; }
    public string? CustomerName { get; set; }
    public decimal AmountPayable { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public int InvoiceLineCount { get; set; }
    public int LinkedVoucherCount { get; set; }
    public int LinkedArReceiptCount { get; set; }
    public bool HasGstStagingDocument { get; set; }
    public string? GstStagingStatus { get; set; }
    public string? PlaceOfSupply { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public DateTime? FinalisedOn { get; set; }
    public string CompletionEvidenceClassification { get; set; } = string.Empty;
    public List<string> CompletionEvidenceReasons { get; set; } = new();
    public string ProductionTestProvenance { get; set; } = "Unknown";
    public List<string> ProvenanceReasons { get; set; } = new();
    public string? CustomerGstin { get; set; }
    public string? CustomerRegistrationContext { get; set; }
    public string? SupplierStateCode { get; set; }
    public string? CustomerStateCode { get; set; }
    public bool IsTaxApplicable { get; set; }
    public bool IsRecipientRegistered { get; set; }
    public string? SupplyType { get; set; }
    public string? GstReturnCategory { get; set; }
    public string? GstTaxType { get; set; }
    public bool GstClassificationValid { get; set; }
    public List<string> GstClassificationErrors { get; set; } = new();
    public List<HistoricalInvoiceAuditLineResponse> Lines { get; set; } = new();
    public bool HasOldGoldTransaction { get; set; }
    public List<HistoricalInvoiceAuditOldGoldResponse> OldGoldTransactions { get; set; } = new();
}

public sealed class HistoricalInvoiceAuditOldGoldResponse
{
    public string? DocumentNumber { get; set; }
    public DateTime? TransactionDate { get; set; }
    public string? TransactionType { get; set; }
    public string? Description { get; set; }
    public string? Metal { get; set; }
    public string? Purity { get; set; }
    public string? Uom { get; set; }
    public decimal? GrossWeight { get; set; }
    public decimal? NetWeight { get; set; }
    public decimal? FinalPurchasePrice { get; set; }
}

public sealed class HistoricalInvoiceAuditLineResponse
{
    public int LineNumber { get; set; }
    public string? ItemDescription { get; set; }
    public string? HsnCode { get; set; }
    public string? Uom { get; set; }
    public string? Uqc { get; set; }
    public decimal Quantity { get; set; }
    public decimal? GrossWeight { get; set; }
    public decimal? NetWeight { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal GstRate { get; set; }
    public decimal CgstRate { get; set; }
    public decimal SgstRate { get; set; }
    public decimal IgstRate { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal? TaxAmount { get; set; }
    public string? ProductCategory { get; set; }
    public bool? IsTaxable { get; set; }
}
