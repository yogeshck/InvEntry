using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class StockAdjustmentLine
{
    public int Gkey { get; set; }

    public int AdjustmentHdrGkey { get; set; }

    public int LineNbr { get; set; }

    public string Direction { get; set; } = null!;

    public string StockLevel { get; set; } = null!;

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

    public int? PairNbr { get; set; }

    public string? Notes { get; set; }

    public virtual StockAdjustmentHeader AdjustmentHdrGkeyNavigation { get; set; } = null!;

    public virtual ProductStock? ProductStockGkeyNavigation { get; set; }
}
