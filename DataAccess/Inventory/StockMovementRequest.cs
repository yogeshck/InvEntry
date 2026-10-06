namespace DataAccess.Inventory.ProductStock;

public sealed class StockMovementRequest
{
    public int DocumentGkey { get; init; }
    public int? DocumentLineGkey { get; init; }
    public string DocumentNumber { get; init; } = string.Empty;
    public DateTime DocumentDate { get; init; }
    public string DocumentType { get; init; } = string.Empty;

    public int? BranchGkey { get; init; }

    public int ProductGkey { get; init; }
    public int? ProductStockGkey { get; init; }
    public string ProductSku { get; init; } = string.Empty;
    public string? ProductCategory { get; init; }

    public StockMovementDirection Direction { get; init; }
    public StockMovementPurpose Purpose { get; init; }

    public int Quantity { get; init; }
    public decimal GrossWeight { get; init; }
    public decimal StoneWeight { get; init; }
    public decimal NetWeight { get; init; }

    public decimal? UnitPrice { get; init; }
    public decimal? TransactionValue { get; init; }

    public string? Reason { get; init; }
    public string? Notes { get; init; }
}
