namespace DataAccess.Models.FinanceIntegration;

public sealed class FinanceDocumentPayload
{
    public int OrgGkey { get; set; }
    public int LocationGkey { get; set; }

    public string SourceSystem { get; set; } = "INVENTRY";
    public string SourceEventId { get; set; } = string.Empty;

    public string DocumentType { get; set; } = "INVOICE";
    public string DocumentNo { get; set; } = string.Empty;

    public DateOnly DocumentDate { get; set; }

    public decimal DocumentAmount { get; set; }

    public string? SourceDocumentType { get; set; }
    public string? SourceDocumentNo { get; set; }
    public string? Description { get; set; }

    public List<FinanceMovementPayload> Movements { get; set; } = [];
}

public sealed class FinanceMovementPayload
{
    public string Direction { get; set; } = string.Empty;
    public string PaymentGroup { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? Description { get; set; }
    public string? SourceReference { get; set; }
}