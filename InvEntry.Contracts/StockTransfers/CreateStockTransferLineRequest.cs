namespace InvEntry.Contracts.StockTransfers;

public sealed class CreateStockTransferLineRequest
{
    public int? ProductStockGkey { get; set; }
    public int? ProductGkey { get; set; }
    public string? ProductId { get; set; }
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