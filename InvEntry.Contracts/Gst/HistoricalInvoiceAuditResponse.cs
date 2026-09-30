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
}
