namespace DataAccess.Models;

public partial class StockTransferLine
{
    public int Gkey { get; set; }
    public int TransferHdrGkey { get; set; }
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
    public virtual StockTransferHeader Header { get; set; } = null!;
    public virtual ProductStock? ProductStock { get; set; }
}