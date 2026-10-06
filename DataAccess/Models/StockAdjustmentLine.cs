namespace DataAccess.Models;

public partial class StockAdjustmentLine
{
    public int Gkey { get; set; }
    public int AdjustmentHdrGkey { get; set; }
    public int LineNbr { get; set; }

    // IN or OUT.
    public string Direction { get; set; } = null!;

    // CONSOLIDATED or SKU. ProductSku remains NULL for consolidated stock.
    public string StockLevel { get; set; } = null!;

    // WEIGHT or WHOLE_ITEM. Weight-only changes normally carry Qty = 0.
    public string MovementKind { get; set; } = null!;

    public int ProductGkey { get; set; }
    public int? ProductStockGkey { get; set; }
    public string? ProductSku { get; set; }
    public string ProductCategory { get; set; } = null!;
    public string? Metal { get; set; }
    public string? Purity { get; set; }
    public string? Uom { get; set; }

    public int Qty { get; set; }
    public decimal GrossWeight { get; set; }
    public decimal StoneWeight { get; set; }
    public decimal NetWeight { get; set; }

    // Links the OUT and IN lines of one reallocation pair.
    public int? PairNbr { get; set; }
    public string? Notes { get; set; }

    public virtual StockAdjustmentHeader AdjustmentHdrGkeyNavigation { get; set; } = null!;
    public virtual ProductStock? ProductStockGkeyNavigation { get; set; }
}
