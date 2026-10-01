namespace InvEntry.Contracts.Gst;

public sealed class HistoricalMigrationPreviewRequest
{
    public string SupplierGstin { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<HistoricalMigrationPreviewCandidate> PriorAuditCandidates { get; set; } = new();
}

public sealed class HistoricalMigrationPreviewCandidate
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string SourceFingerprint { get; set; } = string.Empty;
}

public sealed class HistoricalMigrationPreviewResponse
{
    public string? PreviewToken { get; set; }
    public DateTimeOffset? PreviewTokenExpiresAt { get; set; }
    public string SupplierGstin { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalAudited { get; set; }
    public int Qualified { get; set; }
    public int AlreadyStaged { get; set; }
    public int EligibleForStagingPreview { get; set; }
    public int Excluded { get; set; }
    public int PreviewValidationErrors { get; set; }
    public List<HistoricalMigrationPreviewItem> Items { get; set; } = new();
}

public sealed class HistoricalMigrationStageRequest
{
    public string PreviewToken { get; set; } = string.Empty;
}

public sealed class HistoricalMigrationStageResponse
{
    public string SupplierGstin { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int Previewed { get; set; }
    public int StagedSuccessfully { get; set; }
    public int AlreadyStaged { get; set; }
    public int SourceChanged { get; set; }
    public int NoLongerQualified { get; set; }
    public int ValidationRequired { get; set; }
    public int Errors { get; set; }
    public int HistoricalInvoicesModified { get; set; }
    public List<HistoricalMigrationStageItem> Items { get; set; } = new();
}

public sealed class HistoricalMigrationStageItem
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime? InvoiceDate { get; set; }
    public string Result { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class HistoricalMigrationPreviewItem
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime? InvoiceDate { get; set; }
    public string? Customer { get; set; }
    public string? GstReturnCategory { get; set; }
    public string? Gstr1Table { get; set; }
    public string? SupplyType { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal TotalGst { get; set; }
    public string PreviewResult { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string SourceFingerprint { get; set; } = string.Empty;
}
