namespace InvEntry.Contracts.StockTransfers;

public class StockTransferListItemResponse
{
    public int Gkey { get; set; }
    public string TransferNbr { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; }
    public string TransferType { get; set; } = string.Empty;
    public string FromBranch { get; set; } = string.Empty;
    public int ToReferenceGkey { get; set; }
    public string ToReferenceCode { get; set; } = string.Empty;
    public string ToReferenceValue { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalQty { get; set; }
    public decimal TotalGrossWeight { get; set; }
    public decimal TotalNetWeight { get; set; }
}

public sealed class StockTransferDetailResponse : StockTransferListItemResponse
{
    public int? FromTenantGkey { get; set; }
    public string? Remarks { get; set; }
    public decimal TotalStoneWeight { get; set; }
    public DateTime CreatedOn { get; set; }
    public List<StockTransferLineResponse> Lines { get; set; } = [];
}

public sealed class StockTransferLineResponse
{
    public int Gkey { get; set; }
    public int LineNbr { get; set; }
    public int? ProductStockGkey { get; set; }
    public int? ProductGkey { get; set; }
    public string? ProductId { get; set; }
    public string? ProductSku { get; set; }
    public string? ProductName { get; set; }
    public string? ProductCategory { get; set; }
    public string? Metal { get; set; }
    public string? Purity { get; set; }
    public string? Uom { get; set; }
    public int Qty { get; set; }
    public decimal GrossWeight { get; set; }
    public decimal StoneWeight { get; set; }
    public decimal NetWeight { get; set; }
    public decimal? TransactedRate { get; set; }
    public decimal? TransferValue { get; set; }
    public string? Notes { get; set; }
}