using InvEntry.Models;
using System;
using System.Threading.Tasks;

namespace InvEntry.Services;

public enum StockMovementDirection
{
    In,
    Out
}

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

public interface IProductStockMovementService
{
    Task ApplyAsync(
        StockMovementRequest request);
}

public sealed class ProductStockMovementService
    : IProductStockMovementService
{
    private readonly IMijmsApiService _mijmsApiService;

    public ProductStockMovementService(
        IMijmsApiService mijmsApiService)
    {
        _mijmsApiService = mijmsApiService;
    }

    public async Task ApplyAsync(
        StockMovementRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var movement =
            await _mijmsApiService
                .Post<StockMovementRequest, ProductTransactionSummary>(
                    "api/product-stock-movement",
                    request);

        if (movement is null)
        {
            throw new InvalidOperationException(
                "Stock movement response was not received.");
        }
    }
}