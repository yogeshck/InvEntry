using DataAccess.Models;

namespace DataAccess.Services.StockMovement;

public interface IProductStockMovementService
{
    Task<ProductTransactionSummary> ApplyAsync(
        StockMovementRequest request,
        CancellationToken cancellationToken = default);
}