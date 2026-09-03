namespace DataAccess.Inventory.ProductStock;

public sealed class StockMovementRequest
{
    // =========================================================
    // SOURCE DOCUMENT
    // =========================================================

    public int DocumentGkey { get; init; }

    public int? DocumentLineGkey { get; init; }

    public string DocumentNumber { get; init; }
        = string.Empty;

    public DateTime DocumentDate { get; init; }

    public string DocumentType { get; init; }
        = string.Empty;


    // =========================================================
    // LOCATION
    // =========================================================

    public int? BranchGkey { get; init; }


    // =========================================================
    // PRODUCT
    // =========================================================

    public int ProductGkey { get; init; }

    public string ProductSku { get; init; }
        = string.Empty;

    public string? ProductCategory { get; init; }


    // =========================================================
    // MOVEMENT
    // =========================================================

    public StockMovementDirection Direction { get; init; }

    public StockMovementPurpose Purpose { get; init; }


    // =========================================================
    // QUANTITY / WEIGHT
    // =========================================================

    public int Quantity { get; init; }

    public decimal GrossWeight { get; init; }

    public decimal StoneWeight { get; init; }

    public decimal NetWeight { get; init; }


    // =========================================================
    // COMMERCIAL VALUE
    // =========================================================

    public decimal? UnitPrice { get; init; }

    public decimal? TransactionValue { get; init; }


    // =========================================================
    // ADDITIONAL INFORMATION
    // =========================================================

    public string? Reason { get; init; }

    public string? Notes { get; init; }
}