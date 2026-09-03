namespace DataAccess.Inventory.ProductStock;

public interface IStockMovementService
{
    void PostMovement(
        StockMovementRequest request);

    void PostMovements(
        IEnumerable<StockMovementRequest> requests);
}