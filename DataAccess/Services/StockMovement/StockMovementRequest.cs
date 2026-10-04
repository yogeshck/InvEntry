namespace DataAccess.Services.StockMovement;

public sealed class StockMovementRequest
{

    public int ProductGkey { get; init; }

    public string ProductCategory { get; init; } = string.Empty;

    public StockMovementDirection Direction { get; init; }

    public int Quantity { get; init; }

    public decimal GrossWeight { get; init; }

    public decimal StoneWeight { get; init; }

    public decimal NetWeight { get; init; }

    public DateTime TransactionDate { get; init; }

    public int? RefGkey { get; init; }

    public int? RefLineGkey { get; init; }

    public string DocumentNbr { get; init; } = string.Empty;

    public string DocumentType { get; init; } = string.Empty;

    public string TransactionType { get; init; } = string.Empty;

    public string? Notes { get; init; }
}