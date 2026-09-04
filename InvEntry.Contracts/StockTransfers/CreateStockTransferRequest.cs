namespace InvEntry.Contracts.StockTransfers;

public sealed class CreateStockTransferRequest
{
    public DateTime TransferDate { get; set; }
    public string TransferType { get; set; } = string.Empty;
    public string FromBranch { get; set; } = string.Empty;
    public int? FromTenantGkey { get; set; }
    public int ToReferenceGkey { get; set; }
    public string? Remarks { get; set; }
    public List<CreateStockTransferLineRequest> Lines { get; set; } = [];
}