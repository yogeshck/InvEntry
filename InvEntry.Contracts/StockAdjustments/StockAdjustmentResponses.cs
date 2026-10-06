namespace InvEntry.Contracts.StockAdjustments;

public sealed class StockAdjustmentResponse
{
    public int Gkey { get; set; }
    public string AdjustmentNbr { get; set; } = string.Empty;
    public DateTime AdjustmentDate { get; set; }
    public string AdjustmentType { get; set; } = string.Empty;
    public string ReasonCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }

    public int TotalQty { get; set; }
    public decimal TotalGrossWeight { get; set; }
    public decimal TotalStoneWeight { get; set; }
    public decimal TotalNetWeight { get; set; }

    public DateTime CreatedOn { get; set; }
    public DateTime? FinalisedOn { get; set; }
    public List<StockAdjustmentLineResponse> Lines { get; set; } = [];
}

public sealed class StockAdjustmentLineResponse
{
    public int Gkey { get; set; }
    public int LineNbr { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string StockLevel { get; set; } = string.Empty;
    public string MovementKind { get; set; } = string.Empty;

    public int ProductGkey { get; set; }
    public int? ProductStockGkey { get; set; }
    public string? ProductSku { get; set; }
    public string ProductCategory { get; set; } = string.Empty;
    public string? Metal { get; set; }
    public string? Purity { get; set; }
    public string? Uom { get; set; }

    public int Qty { get; set; }
    public decimal GrossWeight { get; set; }
    public decimal StoneWeight { get; set; }
    public decimal NetWeight { get; set; }
    public int? PairNbr { get; set; }
    public string? Notes { get; set; }
}
