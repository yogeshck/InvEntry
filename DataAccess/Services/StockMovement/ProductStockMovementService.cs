using DataAccess.Models;
using DataAccess.Repository;

namespace DataAccess.Services.StockMovement;

public sealed class ProductStockMovementService
    : IProductStockMovementService
{
    private readonly IRepositoryBase<ProductStockSummary> _stockRepository;
    private readonly IRepositoryBase<ProductTransactionSummary> _movementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductStockMovementService> _logger;

    public ProductStockMovementService(
        IRepositoryBase<ProductStockSummary> stockRepository,
        IRepositoryBase<ProductTransactionSummary> movementRepository,
        IUnitOfWork unitOfWork,
        ILogger<ProductStockMovementService> logger)
    {
        _stockRepository = stockRepository;
        _movementRepository = movementRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ProductTransactionSummary> ApplyAsync(
        StockMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // -------------------------------------------------
            // 1. Prevent the same source transaction being
            //    applied to stock more than once.
            // -------------------------------------------------
            var existing = await _movementRepository.GetAsync(x =>
                x.DocumentType == request.DocumentType &&
                x.RefGkey == request.RefGkey &&
                x.RefLineGkey == request.RefLineGkey &&
                x.TransactionType == request.TransactionType);

            if (existing is not null)
            {
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogInformation(
                    "Stock movement already exists for {DocumentType} " +
                    "{DocumentNbr}, reference {RefGkey}/{RefLineGkey}.",
                    request.DocumentType,
                    request.DocumentNbr,
                    request.RefGkey,
                    request.RefLineGkey);

                return existing;
            }

            // -------------------------------------------------
            // 2. Get CURRENT STOCK.
            //
            // IMPORTANT:
            // This is the source of the Opening Balance.
            // We DO NOT read the previous transaction's CB.
            // -------------------------------------------------
            var stock = await _stockRepository.GetAsync(x =>
                x.ProductGkey == request.ProductGkey);

            var isNewStockSummary = stock is null;

            if (isNewStockSummary &&
                 request.Direction == StockMovementDirection.Out)
            {
                throw new InvalidOperationException(
                    $"Stock does not exist for category {request.ProductCategory}. " +
                    "An outward movement cannot be recorded.");
            }

            if (stock is null)
            {
                stock = new ProductStockSummary
                {
                    ProductGkey = request.ProductGkey,
                    Category = request.ProductCategory,

                    StockQty = 0,

                    GrossWeight = 0m,
                    StoneWeight = 0m,
                    NetWeight = 0m,

                    BalanceWeight = 0m,

                    Status = "In-Stock",
                    CreatedOn = DateTime.Now
                };
            }

            var openingQty = stock.StockQty ?? 0;

            var openingGross =
                stock.GrossWeight ?? 0m;

            var openingStone =
                stock.StoneWeight ?? 0m;

            var openingNet =
                stock.NetWeight ?? 0m;

            // -------------------------------------------------
            // 3. Determine IN / OUT for this transaction.
            // -------------------------------------------------
            var stockInQty =
                request.Direction == StockMovementDirection.In
                    ? request.Quantity
                    : 0;

            var stockOutQty =
                request.Direction == StockMovementDirection.Out
                    ? request.Quantity
                    : 0;

            var stockInGross =
                request.Direction == StockMovementDirection.In
                    ? request.GrossWeight
                    : 0m;

            var stockOutGross =
                request.Direction == StockMovementDirection.Out
                    ? request.GrossWeight
                    : 0m;

            var stockInStone =
                request.Direction == StockMovementDirection.In
                    ? request.StoneWeight
                    : 0m;

            var stockOutStone =
                request.Direction == StockMovementDirection.Out
                    ? request.StoneWeight
                    : 0m;

            var stockInNet =
                request.Direction == StockMovementDirection.In
                    ? request.NetWeight
                    : 0m;

            var stockOutNet =
                request.Direction == StockMovementDirection.Out
                    ? request.NetWeight
                    : 0m;

            // -------------------------------------------------
            // 4. Calculate stock AFTER this transaction.
            //
            // CB = OB + IN - OUT
            // -------------------------------------------------
            var closingQty =
                openingQty + stockInQty - stockOutQty;

            var closingGross =
                openingGross + stockInGross - stockOutGross;

            var closingStone =
                openingStone + stockInStone - stockOutStone;

            var closingNet =
                openingNet + stockInNet - stockOutNet;

            // -------------------------------------------------
            // 5. Create NEW movement ledger row.
            // -------------------------------------------------
            var movement = new ProductTransactionSummary
            {
                TransactionDate = request.TransactionDate,

                ProductGkey = request.ProductGkey,
                ProductCategory = request.ProductCategory,

                OpeningQty = openingQty,
                StockInQty = stockInQty,
                StockOutQty = stockOutQty,
                ClosingQty = closingQty,

                OpeningGrossWeight = openingGross,
                StockInGrossWeight = stockInGross,
                StockOutGrossWeight = stockOutGross,
                ClosingGrossWeight = closingGross,

                OpeningStoneWeight = openingStone,
                StockInStoneWeight = stockInStone,
                StockOutStoneWeight = stockOutStone,
                ClosingStoneWeight = closingStone,

                OpeningNetWeight = openingNet,
                StockInNetWeight = stockInNet,
                StockOutNetWeight = stockOutNet,
                ClosingNetWeight = closingNet,

                RefGkey = request.RefGkey,
                RefLineGkey = request.RefLineGkey,

                DocumentNbr = request.DocumentNbr,
                DocumentType = request.DocumentType,
                TransactionType = request.TransactionType,

                Notes = request.Notes
            };

            // -------------------------------------------------
            // 6. Update CURRENT stock using the SAME CB.
            // -------------------------------------------------
            stock.StockQty = closingQty;
            stock.GrossWeight = closingGross;
            stock.StoneWeight = closingStone;
            stock.NetWeight = closingNet;

            stock.BalanceWeight = closingNet;
            stock.ModifiedOn = DateTime.Now;

            if (isNewStockSummary)
            {
                _stockRepository.Add(stock);
            }
            else
            {
                _stockRepository.Update(stock);
            }

            // Always INSERT a new movement.
            _movementRepository.Add(movement);

            // Both changes are saved together.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Stock movement recorded for {Category}. " +
                "Document {DocumentType} {DocumentNbr}. " +
                "OB Qty {OpeningQty}, IN {StockInQty}, OUT {StockOutQty}, CB {ClosingQty}.",
                request.ProductCategory,
                request.DocumentType,
                request.DocumentNbr,
                openingQty,
                stockInQty,
                stockOutQty,
                closingQty);

            return movement;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);

            _unitOfWork.ClearChanges();

            throw;
        }
    }

    private static void Validate(StockMovementRequest request)
    {
        if (request.ProductGkey <= 0)
            throw new ArgumentException(
                "Product GKey must be greater than zero.");

        if (string.IsNullOrWhiteSpace(request.ProductCategory))
            throw new ArgumentException(
                "Product category is required.");

        if (request.Quantity < 0)
            throw new ArgumentException(
                "Movement quantity cannot be negative.");

        if (request.GrossWeight < 0 ||
            request.StoneWeight < 0 ||
            request.NetWeight < 0)
        {
            throw new ArgumentException(
                "Movement weights cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(request.DocumentType))
            throw new ArgumentException(
                "Document type is required.");

        if (string.IsNullOrWhiteSpace(request.TransactionType))
            throw new ArgumentException(
                "Transaction type is required.");
    }
}