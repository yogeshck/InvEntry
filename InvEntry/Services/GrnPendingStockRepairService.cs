using InvEntry.Models;
using System;
using System.Threading.Tasks;

namespace InvEntry.Services;

public interface IGrnPendingStockRepairService
{
    /// <summary>
    /// Completes GRN follow-up processing that did not finish when the GRN was
    /// saved: posts the line's stock receipt (if missing) and creates the
    /// missing "Pending Tag" ProductStock items so the line can be weighed.
    /// Returns the number of pending items created.
    /// </summary>
    Task<int> RepairAsync(GrnHeader header, GrnLineSummary line, int existingItemCount);
}

public sealed class GrnPendingStockRepairService : IGrnPendingStockRepairService
{
    private readonly IProductStockMovementService _productStockMovementService;
    private readonly IProductStockSummaryService _productStockSummaryService;
    private readonly IProductStockService _productStockService;

    public GrnPendingStockRepairService(
        IProductStockMovementService productStockMovementService,
        IProductStockSummaryService productStockSummaryService,
        IProductStockService productStockService)
    {
        _productStockMovementService = productStockMovementService;
        _productStockSummaryService = productStockSummaryService;
        _productStockService = productStockService;
    }

    public async Task<int> RepairAsync(GrnHeader header, GrnLineSummary line, int existingItemCount)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(line);

        if (header.GKey <= 0)
            throw new InvalidOperationException("The GRN has not been saved.");

        if (line.GKey <= 0)
            throw new InvalidOperationException("The GRN line has not been saved.");

        if (!line.ProductGkey.HasValue || line.ProductGkey.Value <= 0)
            throw new InvalidOperationException(
                $"Product was not identified for category {line.ProductCategory}.");

        if (string.IsNullOrWhiteSpace(line.ProductCategory))
            throw new InvalidOperationException("Product category is required.");

        int missingCount = line.SuppliedQty.GetValueOrDefault() - existingItemCount;
        if (missingCount <= 0)
            return 0;

        // Same request GRNViewModel posts when the GRN is saved. The movement API
        // is idempotent on DocumentType + RefGkey + RefLineGkey + TransactionType,
        // so if the receipt was already posted it is returned, not added again.
        await _productStockMovementService.ApplyAsync(new StockMovementRequest
        {
            ProductGkey = line.ProductGkey.Value,
            ProductCategory = line.ProductCategory,
            Direction = StockMovementDirection.In,
            Quantity = line.SuppliedQty.GetValueOrDefault(),
            GrossWeight = line.GrossWeight.GetValueOrDefault(),
            StoneWeight = line.StoneWeight.GetValueOrDefault(),
            NetWeight = line.NetWeight.GetValueOrDefault(),
            TransactionDate = header.GrnDate ?? DateTime.Now,
            RefGkey = header.GKey,
            RefLineGkey = line.GKey,
            DocumentNbr = header.GrnNbr ?? string.Empty,
            DocumentType = "GRN",
            TransactionType = "Receipt",
            Notes = $"Material receipt {header.GrnNbr}"
        });

        var stockSummary = await _productStockSummaryService
            .GetProductStockSummaryByCategory(line.ProductCategory);

        if (stockSummary is null || stockSummary.GKey <= 0)
            throw new InvalidOperationException(
                $"Stock summary was not found for category {line.ProductCategory}.");

        // Same shape as GRNViewModel.BuildTemporaryStockItems: one item per piece.
        for (int i = 0; i < missingCount; i++)
        {
            await _productStockService.CreateProductStock(new ProductStock
            {
                StockSummaryGkey = stockSummary.GKey,
                GrnLineSummaryGkey = line.GKey,
                ProductGkey = line.ProductGkey,
                Category = line.ProductCategory,
                SupplierId = header.SupplierId,
                ProductSku = $"TMP-{Guid.NewGuid():N}",
                SuppliedQty = 1,
                StockQty = 1,
                SoldQty = 0,
                GrossWeight = null,
                StoneWeight = 0,
                NetWeight = null,
                IsProductSold = false,
                Status = "Pending Tag",
                IsBarcodePrinted = false,
                CreatedOn = DateTime.Now
            });
        }

        return missingCount;
    }
}
