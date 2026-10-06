using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Inventory.ProductStock;

public sealed class StockMovementService
    : IStockMovementService
{
    private const decimal WeightTolerance = 0.001M;

    private readonly IRepositoryBase<Models.ProductStock>
        _productStockRepository;

    private readonly IRepositoryBase<ProductStockSummary>
        _productStockSummaryRepository;

    private readonly IRepositoryBase<ProductTransaction>
        _productTransactionRepository;

    private readonly IRepositoryBase<ProductTransactionSummary>
        _productTransactionSummaryRepository;

    private readonly MijmsContext _context;


    public StockMovementService(
        IRepositoryBase<Models.ProductStock> productStockRepository,
        IRepositoryBase<ProductStockSummary> productStockSummaryRepository,
        IRepositoryBase<ProductTransaction> productTransactionRepository,
        IRepositoryBase<ProductTransactionSummary> productTransactionSummaryRepository,
        MijmsContext context)
    {
        _productStockRepository =
            productStockRepository;

        _productStockSummaryRepository =
            productStockSummaryRepository;

        _productTransactionRepository =
            productTransactionRepository;

        _productTransactionSummaryRepository =
            productTransactionSummaryRepository;

        _context = context;
    }


    // =========================================================
    // POST MULTIPLE MOVEMENTS
    // =========================================================

    public void PostMovements(
        IEnumerable<StockMovementRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        var movementRequests = requests.ToList();

        ValidateMovementsBeforePosting(movementRequests);

        foreach (var request in movementRequests)
        {
            PostMovement(request);
        }
    }

    private void ValidateMovementsBeforePosting(
        IReadOnlyCollection<StockMovementRequest> requests)
    {
        foreach (var request in requests)
        {
            ValidateRequest(request);
            EnsureNotAlreadyPosted(request);
        }

        var duplicateLine = requests
            .Where(x => x.DocumentLineGkey.HasValue)
            .GroupBy(x => new { x.DocumentType, x.DocumentLineGkey })
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicateLine != null)
        {
            throw new InvalidOperationException(
                $"Stock movement was supplied more than once for line " +
                $"{duplicateLine.Key.DocumentLineGkey}.");
        }

        // Validate only strict OUT movements against the opening summary.
        //
        // Reallocation may contain an OUT and an IN for the same ProductGkey.
        // Combining both directions would incorrectly validate the IN as though
        // it were also an OUT. Therefore only strict OUT requests are grouped.
        foreach (var productGroup in requests
                     .Where(IsStrictStockOut)
                     .GroupBy(x => x.ProductGkey))
        {
            var first = productGroup.First();
            var summary = GetProductStockSummary(first);
            ValidateSummaryStockOut(summary, CombineRequests(productGroup));
        }

        // Tagged availability is always strict for OUT movements. As above,
        // exclude any matching IN movement from the opening-stock validation.
        foreach (var skuGroup in requests
                     .Where(x =>
                         x.Direction == StockMovementDirection.Out &&
                         !string.IsNullOrWhiteSpace(x.ProductSku))
                     .GroupBy(x => new
                     {
                         x.ProductGkey,
                         ProductSku = x.ProductSku.Trim().ToUpperInvariant()
                     }))
        {
            var first = skuGroup.First();
            var stock = GetTaggedProductStock(first);
            ValidateTaggedStockOut(stock!, CombineRequests(skuGroup));
        }
    }

    private static StockMovementRequest CombineRequests(
        IEnumerable<StockMovementRequest> requests)
    {
        var items = requests.ToList();
        var first = items[0];

        return new StockMovementRequest
        {
            DocumentGkey = first.DocumentGkey,
            DocumentLineGkey = first.DocumentLineGkey,
            DocumentNumber = first.DocumentNumber,
            DocumentDate = first.DocumentDate,
            DocumentType = first.DocumentType,
            ProductGkey = first.ProductGkey,
            ProductSku = first.ProductSku,
            ProductCategory = first.ProductCategory,
            Direction = first.Direction,
            Purpose = first.Purpose,
            Quantity = items.Sum(x => x.Quantity),
            GrossWeight = items.Sum(x => x.GrossWeight),
            StoneWeight = items.Sum(x => x.StoneWeight),
            NetWeight = items.Sum(x => x.NetWeight)
        };
    }


    // =========================================================
    // POST SINGLE MOVEMENT
    // =========================================================

    public void PostMovement(
        StockMovementRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        EnsureNotAlreadyPosted(request);


        // =====================================================
        // 1. PRODUCT-LEVEL CURRENT BALANCE
        // =====================================================

        var stockSummary =
            GetProductStockSummary(request);

        var summaryOpening =
            CaptureOpeningBalance(stockSummary);


        // =====================================================
        // 2. OPTIONAL TAGGED ITEM
        // =====================================================

        var taggedStock =
            GetTaggedProductStock(request);

        StockBalanceSnapshot? tagOpening = null;

        if (taggedStock != null)
        {
            tagOpening =
                CaptureTaggedOpeningBalance(
                    taggedStock);
        }


        // =====================================================
        // 3. APPLY STOCK MOVEMENT
        // =====================================================

        switch (request.Direction)
        {
            case StockMovementDirection.Out:

                ApplyStockOut(
                    stockSummary,
                    taggedStock,
                    request);

                break;

            case StockMovementDirection.In:

                ApplyStockIn(
                    stockSummary,
                    taggedStock,
                    request);

                break;


            default:

                throw new InvalidOperationException(
                    $"Unsupported stock direction: " +
                    $"{request.Direction}.");
        }


        // =====================================================
        // 4. PRODUCT-LEVEL MOVEMENT HISTORY
        //    ALWAYS INSERT
        // =====================================================

        var transactionSummary =
            CreateProductTransactionSummary(
                request,
                summaryOpening,
                stockSummary);

        _productTransactionSummaryRepository.Add(
            transactionSummary);


        // =====================================================
        // 5. TAG-LEVEL MOVEMENT HISTORY
        //    ONLY FOR TAGGED ITEMS
        // =====================================================

        if (taggedStock != null &&
            tagOpening != null)
        {
            var transaction =
                CreateProductTransaction(
                    request,
                    tagOpening,
                    taggedStock);

            _productTransactionRepository.Add(
                transaction);
        }
    }

    private void ApplyStockIn(
    ProductStockSummary summary,
    Models.ProductStock? taggedStock,
    StockMovementRequest request)
    {
        // =========================================================
        // PRODUCT SUMMARY
        // =========================================================

        summary.StockQty =
            summary.StockQty.GetValueOrDefault()
            + request.Quantity;

        summary.GrossWeight =
            NormaliseWeight(
                summary.GrossWeight.GetValueOrDefault()
                + request.GrossWeight);

        summary.StoneWeight =
            NormaliseWeight(
                summary.StoneWeight.GetValueOrDefault()
                + request.StoneWeight);

        summary.NetWeight =
            NormaliseWeight(
                summary.NetWeight.GetValueOrDefault()
                + request.NetWeight);

        summary.BalanceWeight =
            summary.NetWeight;


        // =========================================================
        // PURPOSE-SPECIFIC TOTALS
        // =========================================================

        switch (request.Purpose)
        {
            case StockMovementPurpose.PurchaseReceipt:
            case StockMovementPurpose.MaterialReceipt:

                summary.SuppliedQty =
                    summary.SuppliedQty.GetValueOrDefault()
                    + request.Quantity;

                summary.SuppliedGrossWeight =
                    NormaliseWeight(
                        summary.SuppliedGrossWeight.GetValueOrDefault()
                        + request.GrossWeight);

                break;


            case StockMovementPurpose.StockAdjustmentIncrease:

                summary.AdjustedQty =
                    summary.AdjustedQty.GetValueOrDefault()
                    + request.Quantity;

                summary.AdjustedWeight =
                    NormaliseWeight(
                        summary.AdjustedWeight.GetValueOrDefault()
                        + request.GrossWeight);

                break;


            default:

                // Transfer/workshop receipts affect current stock,
                // but no legacy cumulative total is changed here.
                break;
        }


        summary.Status = "In-Stock";

        summary.ModifiedOn =
            DateTime.Now;

        _productStockSummaryRepository.Update(
            summary);


        // =========================================================
        // TAGGED STOCK
        // =========================================================
        //
        // Do NOT create/update tagged ProductStock here for GRN yet.
        //
        // GRN currently creates its Pending Tag ProductStock records
        // separately after the category receipt is posted.
        // =========================================================

        if (taggedStock != null)
        {
            ApplyTaggedStockIn(
                taggedStock,
                request);
        }
    }

    private void ApplyTaggedStockIn(
    Models.ProductStock stock,
    StockMovementRequest request)
    {
        stock.StockQty =
            stock.StockQty.GetValueOrDefault()
            + request.Quantity;

        stock.GrossWeight =
            NormaliseWeight(
                stock.GrossWeight.GetValueOrDefault()
                + request.GrossWeight);

        stock.StoneWeight =
            NormaliseWeight(
                stock.StoneWeight.GetValueOrDefault()
                + request.StoneWeight);

        stock.NetWeight =
            NormaliseWeight(
                stock.NetWeight.GetValueOrDefault()
                + request.NetWeight);

        stock.BalanceWeight =
            NormaliseWeight(
                stock.BalanceWeight.GetValueOrDefault()
                + request.GrossWeight);

        stock.IsProductSold = false;
        stock.Status = "In-Stock";
        stock.ModifiedOn = DateTime.Now;

        _productStockRepository.Update(stock);
    }

    // =========================================================
    // VALIDATE REQUEST
    // =========================================================

    private static void ValidateRequest(
        StockMovementRequest request)
    {
        if (request.DocumentGkey <= 0)
        {
            throw new InvalidOperationException(
                "A valid source document Gkey is required.");
        }


        if (request.DocumentLineGkey.GetValueOrDefault() <= 0)
        {
            throw new InvalidOperationException(
                "A valid source document line Gkey is required.");
        }


        if (string.IsNullOrWhiteSpace(
                request.DocumentNumber))
        {
            throw new InvalidOperationException(
                "Source document number is required.");
        }


        if (string.IsNullOrWhiteSpace(
                request.DocumentType))
        {
            throw new InvalidOperationException(
                "Source document type is required.");
        }


        if (request.ProductGkey <= 0)
        {
            throw new InvalidOperationException(
                "A valid ProductGkey is required.");
        }


        // ProductSku is intentionally optional.
        // If supplied, exact tagged ProductStock will also
        // be updated and ProductTransaction will be inserted.


        if (request.Quantity < 0)
        {
            throw new InvalidOperationException(
                $"Stock movement quantity cannot be negative " +
                $"for product {GetProductReference(request)}.");
        }


        if (request.GrossWeight < 0M ||
            request.StoneWeight < 0M ||
            request.NetWeight < 0M)
        {
            throw new InvalidOperationException(
                $"Stock movement weight cannot be negative " +
                $"for product {GetProductReference(request)}.");
        }


        if (request.StoneWeight >
            request.GrossWeight + WeightTolerance)
        {
            throw new InvalidOperationException(
                $"Stone weight cannot exceed gross weight " +
                $"for product {GetProductReference(request)}.");
        }


        if (request.Quantity == 0 &&
            request.GrossWeight <= WeightTolerance)
        {
            throw new InvalidOperationException(
                $"Stock movement contains neither quantity " +
                $"nor weight for product " +
                $"{GetProductReference(request)}.");
        }
    }


    // =========================================================
    // DUPLICATE PROTECTION
    // =========================================================

    private void EnsureNotAlreadyPosted(
        StockMovementRequest request)
    {
        if (!request.DocumentLineGkey.HasValue)
        {
            return;
        }

        var existing =
            _productTransactionSummaryRepository.Get(
                x =>
                    x.RefLineGkey ==
                        request.DocumentLineGkey.Value &&
                    x.DocumentType ==
                        request.DocumentType);

        if (existing != null)
        {
            throw new InvalidOperationException(
                $"Stock movement has already been posted. " +
                $"Document: {request.DocumentNumber}, " +
                $"Line: {request.DocumentLineGkey}.");
        }
    }


    // =========================================================
    // GET PRODUCT STOCK SUMMARY
    // Product-level authoritative stock balance
    // =========================================================

    private ProductStockSummary GetProductStockSummary(
        StockMovementRequest request)
    {
        if (RequiresSummaryUpdateLock(request))
        {
            var lockedSummary = _context.ProductStockSummaries
                .FromSqlInterpolated($"SELECT * FROM PRODUCT_STOCK_SUMMARY WITH (UPDLOCK, HOLDLOCK) WHERE PRODUCT_GKEY = {request.ProductGkey}")
                .SingleOrDefault();

            if (lockedSummary == null)
            {
                throw new InvalidOperationException(
                    $"Stock summary was not found for " +
                    $"ProductGkey {request.ProductGkey}, " +
                    $"Product {GetProductReference(request)}.");
            }

            return lockedSummary;
        }

        var summary =
            _productStockSummaryRepository.Get(
                x =>
                    x.ProductGkey ==
                    request.ProductGkey);

        if (summary == null)
        {
            throw new InvalidOperationException(
                $"Stock summary was not found for " +
                $"ProductGkey {request.ProductGkey}, " +
                $"Product {GetProductReference(request)}.");
        }

        return summary;
    }


    // =========================================================
    // GET OPTIONAL TAGGED PRODUCT STOCK
    // =========================================================

    private Models.ProductStock? GetTaggedProductStock(
        StockMovementRequest request)
    {
        if (request.ProductStockGkey.HasValue)
        {
            var lockedStock = _context.ProductStocks
                .FromSqlInterpolated($"SELECT * FROM PRODUCT_STOCK WITH (UPDLOCK, HOLDLOCK) WHERE GKEY = {request.ProductStockGkey.Value}")
                .SingleOrDefault();

            if (lockedStock == null ||
                lockedStock.ProductGkey != request.ProductGkey)
            {
                throw new InvalidOperationException(
                    $"Tagged stock Gkey '{request.ProductStockGkey}' " +
                    $"was not found for ProductGkey " +
                    $"{request.ProductGkey}.");
            }

            return lockedStock;
        }

        if (string.IsNullOrWhiteSpace(
                request.ProductSku))
        {
            return null;
        }

        var stock =
            _productStockRepository.Get(
                x =>
                    x.ProductGkey ==
                        request.ProductGkey &&
                    x.ProductSku ==
                        request.ProductSku);

        if (stock == null)
        {
            throw new InvalidOperationException(
                $"Tagged stock '{request.ProductSku}' " +
                $"was not found for ProductGkey " +
                $"{request.ProductGkey}.");
        }

        return stock;
    }


    // =========================================================
    // STOCK OUT
    // =========================================================

    private void ApplyStockOut(
        ProductStockSummary summary,
        Models.ProductStock? taggedStock,
        StockMovementRequest request)
    {
        // =========================================================
        // IMPORTANT BUSINESS RULE
        // =========================================================
        //
        // Product-level summary shortage does NOT block a sale.
        //
        // Physical stock may be present in the shop while its stock
        // receipt/data-entry is still pending.
        //
        // ProductStockSummary is therefore allowed to temporarily
        // become negative.
        // =========================================================


        // Exact tagged item validation remains strict.
        if (taggedStock != null)
        {
            ValidateTaggedStockOut(
                taggedStock,
                request);
        }

        if (IsStrictStockOut(request))
        {
            ValidateSummaryStockOut(
                summary,
                request);
        }


        // Product summary is always updated.
        ApplySummaryStockOut(
            summary,
            request);


        // Exact tagged item is updated when applicable.
        if (taggedStock != null)
        {
            ApplyTaggedStockOut(
                taggedStock,
                request);
        }
    }


    // =========================================================
    // VALIDATE PRODUCT SUMMARY STOCK OUT
    // =========================================================

    private static void ValidateSummaryStockOut(
        ProductStockSummary summary,
        StockMovementRequest request)
    {
        if (!IsStrictStockOut(request))
        {
            return;
        }

        if (request.Quantity > summary.StockQty.GetValueOrDefault() ||
            request.GrossWeight > summary.GrossWeight.GetValueOrDefault() + WeightTolerance ||
            request.StoneWeight > summary.StoneWeight.GetValueOrDefault() + WeightTolerance ||
            request.NetWeight > summary.NetWeight.GetValueOrDefault() + WeightTolerance ||
            request.NetWeight > summary.BalanceWeight.GetValueOrDefault() + WeightTolerance)
        {
            throw new InvalidOperationException(
                $"Insufficient category stock for {GetProductReference(request)}.");
        }
    }


    private static bool IsStrictStockOut(
        StockMovementRequest request)
    {
        if (request.Direction != StockMovementDirection.Out)
        {
            return false;
        }

        return request.Purpose is
            StockMovementPurpose.BranchTransferOut or
            StockMovementPurpose.StockAdjustmentDecrease or
            StockMovementPurpose.StockReallocationOut;
    }


    private static bool RequiresSummaryUpdateLock(
        StockMovementRequest request)
    {
        return request.Purpose is
            StockMovementPurpose.BranchTransferOut or
            StockMovementPurpose.StockAdjustmentIncrease or
            StockMovementPurpose.StockAdjustmentDecrease or
            StockMovementPurpose.StockReallocationOut or
            StockMovementPurpose.StockReallocationIn;
    }


    // =========================================================
    // VALIDATE TAGGED STOCK OUT
    // =========================================================

    private static void ValidateTaggedStockOut(
        Models.ProductStock stock,
        StockMovementRequest request)
    {
        if (stock.IsProductSold == true)
        {
            throw new InvalidOperationException(
                $"Tagged item '{request.ProductSku}' " +
                $"has already been sold.");
        }


        var availableQty =
            stock.StockQty.GetValueOrDefault();

        var availableWeight =
            stock.BalanceWeight.GetValueOrDefault();


        if (request.Quantity > availableQty)
        {
            throw new InvalidOperationException(
                $"Insufficient tagged stock quantity for " +
                $"'{request.ProductSku}'. " +
                $"Available: {availableQty}, " +
                $"Required: {request.Quantity}.");
        }


        if (request.GrossWeight >
            availableWeight + WeightTolerance)
        {
            throw new InvalidOperationException(
                $"Requested weight exceeds tagged stock weight " +
                $"for '{request.ProductSku}'. " +
                $"Available: {availableWeight:N3}, " +
                $"Required: {request.GrossWeight:N3}.");
        }

        if (request.Purpose is
            StockMovementPurpose.StockAdjustmentDecrease or
            StockMovementPurpose.StockReallocationOut)
        {
            if (request.GrossWeight >
                    stock.GrossWeight.GetValueOrDefault() + WeightTolerance ||
                request.StoneWeight >
                    stock.StoneWeight.GetValueOrDefault() + WeightTolerance ||
                request.NetWeight >
                    stock.NetWeight.GetValueOrDefault() + WeightTolerance)
            {
                throw new InvalidOperationException(
                    $"Insufficient tagged item weight for " +
                    $"'{request.ProductSku}'.");
            }
        }
    }

    // =========================================================
    // APPLY PRODUCT SUMMARY STOCK OUT
    // =========================================================

    private void ApplySummaryStockOut(
        ProductStockSummary summary,
        StockMovementRequest request)
    {
        // =========================================================
        // AUTHORITATIVE CURRENT STOCK
        // =========================================================

        summary.StockQty =
            summary.StockQty.GetValueOrDefault()
            - request.Quantity;

        summary.GrossWeight =
            NormaliseWeight(
                summary.GrossWeight.GetValueOrDefault()
                - request.GrossWeight);

        summary.StoneWeight =
            NormaliseWeight(
                summary.StoneWeight.GetValueOrDefault()
                - request.StoneWeight);

        summary.NetWeight =
            NormaliseWeight(
                summary.NetWeight.GetValueOrDefault()
                - request.NetWeight);

        // BalanceWeight mirrors current NetWeight.
        summary.BalanceWeight =
            summary.NetWeight;


        // =========================================================
        // PURPOSE-SPECIFIC LEGACY TOTALS
        // =========================================================

        switch (request.Purpose)
        {
            case StockMovementPurpose.Sale:

                summary.SoldQty =
                    summary.SoldQty.GetValueOrDefault()
                    + request.Quantity;

                summary.SoldWeight =
                    NormaliseWeight(
                        summary.SoldWeight.GetValueOrDefault()
                        + request.NetWeight);

                break;


            case StockMovementPurpose.StockAdjustmentDecrease:

                summary.AdjustedQty =
                    summary.AdjustedQty.GetValueOrDefault()
                    - request.Quantity;

                summary.AdjustedWeight =
                    NormaliseWeight(
                        summary.AdjustedWeight.GetValueOrDefault()
                        - request.GrossWeight);

                break;


            default:

                // Transfer, workshop issue and other OUT movements
                // affect current stock but are not sales.
                break;
        }


        // =========================================================
        // STATUS
        // =========================================================

        var empty =
            IsEmpty(
                summary.StockQty.GetValueOrDefault(),
                summary.NetWeight.GetValueOrDefault());

        summary.Status =
            empty
                ? "Out-of-Stock"
                : "In-Stock";

        summary.ModifiedOn =
            DateTime.Now;

        _productStockSummaryRepository.Update(
            summary);
    }

    // =========================================================
    // APPLY TAGGED PRODUCT STOCK OUT
    // =========================================================

    private void ApplyTaggedStockOut(
        Models.ProductStock stock,
        StockMovementRequest request)
    {
        // Stock Adjustment / Reallocation weight movements are different
        // from a sale. The tagged item remains an inventory item and its
        // actual recorded weights must change.
        if (request.Purpose is
            StockMovementPurpose.StockAdjustmentDecrease or
            StockMovementPurpose.StockReallocationOut)
        {
            stock.StockQty =
                stock.StockQty.GetValueOrDefault()
                - request.Quantity;

            stock.GrossWeight =
                NormaliseWeight(
                    stock.GrossWeight.GetValueOrDefault()
                    - request.GrossWeight);

            stock.StoneWeight =
                NormaliseWeight(
                    stock.StoneWeight.GetValueOrDefault()
                    - request.StoneWeight);

            stock.NetWeight =
                NormaliseWeight(
                    stock.NetWeight.GetValueOrDefault()
                    - request.NetWeight);

            // ProductStock.BalanceWeight is treated by the existing generic
            // stock engine as the remaining tagged gross weight.
            stock.BalanceWeight =
                NormaliseWeight(
                    stock.BalanceWeight.GetValueOrDefault()
                    - request.GrossWeight);

            var adjustmentEmpty =
                IsEmpty(
                    stock.StockQty.GetValueOrDefault(),
                    stock.BalanceWeight.GetValueOrDefault());

            stock.IsProductSold = false;
            stock.Status =
                adjustmentEmpty
                    ? "Out-of-Stock"
                    : "In-Stock";
            stock.ModifiedOn = DateTime.Now;

            _productStockRepository.Update(stock);
            return;
        }

        if (request.Purpose ==
            StockMovementPurpose.BranchTransferOut)
        {
            stock.SoldWeight = request.GrossWeight;
            stock.BalanceWeight = 0M;
            stock.SoldQty = request.Quantity;
            stock.StockQty = 0;
            stock.Status = "Sold";
            stock.IsProductSold = true;
            stock.ModifiedOn = DateTime.Now;
            _productStockRepository.Update(stock);
            return;
        }

        stock.StockQty =
            stock.StockQty.GetValueOrDefault()
            - request.Quantity;


        stock.BalanceWeight =
            NormaliseWeight(
                stock.BalanceWeight.GetValueOrDefault()
                - request.GrossWeight);


        switch (request.Purpose)
        {
            case StockMovementPurpose.Sale:

                stock.SoldQty =
                    stock.SoldQty.GetValueOrDefault()
                    + request.Quantity;

                stock.SoldWeight =
                    stock.SoldWeight.GetValueOrDefault()
                    + request.GrossWeight;

                break;


            default:

                // Other OUT purposes should not be marked
                // permanently sold.
                break;
        }


        var empty =
            IsEmpty(
                stock.StockQty.GetValueOrDefault(),
                stock.BalanceWeight.GetValueOrDefault());


        if (request.Purpose ==
            StockMovementPurpose.Sale)
        {
            stock.IsProductSold =
                empty;

            stock.Status =
                empty
                    ? "Sold"
                    : "In-Stock";
        }
        else
        {
            stock.IsProductSold =
                false;

            stock.Status =
                empty
                    ? "Out-of-Stock"
                    : "In-Stock";
        }


        stock.ModifiedOn =
            DateTime.Now;


        _productStockRepository.Update(
            stock);
    }


    // =========================================================
    // IS STOCK EMPTY
    // =========================================================

    private static bool IsEmpty(
        int quantity,
        decimal weight)
    {
        return
            quantity <= 0 &&
            weight <= WeightTolerance;
    }


    // =========================================================
    // CAPTURE PRODUCT-LEVEL OPENING BALANCE
    // =========================================================

    private static StockBalanceSnapshot
        CaptureOpeningBalance(
            ProductStockSummary summary)
    {
        return new StockBalanceSnapshot
        {
            Quantity =
                summary.StockQty.GetValueOrDefault(),

            GrossWeight =
                summary.GrossWeight.GetValueOrDefault(),

            StoneWeight =
                summary.StoneWeight.GetValueOrDefault(),

            NetWeight =
                summary.NetWeight.GetValueOrDefault()
        };
    }


    // =========================================================
    // CAPTURE TAG-LEVEL OPENING BALANCE
    // =========================================================

    private static StockBalanceSnapshot
        CaptureTaggedOpeningBalance(
            Models.ProductStock stock)
    {
        return new StockBalanceSnapshot
        {
            Quantity =
                stock.StockQty.GetValueOrDefault(),

            GrossWeight =
                stock.BalanceWeight.GetValueOrDefault(),

            StoneWeight =
                stock.StoneWeight.GetValueOrDefault(),

            NetWeight =
                stock.NetWeight.GetValueOrDefault()
        };
    }


    // =========================================================
    // CREATE PRODUCT-LEVEL TRANSACTION HISTORY
    // ALWAYS INSERT
    // =========================================================

    private static ProductTransactionSummary
        CreateProductTransactionSummary(
            StockMovementRequest request,
            StockBalanceSnapshot opening,
            ProductStockSummary closing)
    {
        var isStockIn =
            request.Direction ==
            StockMovementDirection.In;

        var isStockOut =
            request.Direction ==
            StockMovementDirection.Out;


        return new ProductTransactionSummary
        {
            // -------------------------------------------------
            // SOURCE / AUDIT
            // -------------------------------------------------

            TransactionDate =
                request.DocumentDate,

            ProductGkey =
                request.ProductGkey,

            ProductSku =
                string.IsNullOrWhiteSpace(
                    request.ProductSku)
                    ? null
                    : request.ProductSku,

            ProductCategory =
                request.ProductCategory,

            RefGkey =
                request.DocumentGkey,

            RefLineGkey =
                request.DocumentLineGkey,

            DocumentNbr =
                request.DocumentNumber,

            DocumentType =
                request.DocumentType,

            TransactionType =
                GetTransactionType(
                    request.Purpose),

            Notes =
                request.Notes ??
                request.Reason,


            // -------------------------------------------------
            // QUANTITY
            // -------------------------------------------------

            OpeningQty =
                opening.Quantity,

            StockInQty =
                isStockIn
                    ? request.Quantity
                    : 0,

            StockOutQty =
                isStockOut
                    ? request.Quantity
                    : 0,

            ClosingQty =
                closing.StockQty.GetValueOrDefault(),


            // -------------------------------------------------
            // OPENING WEIGHTS
            // -------------------------------------------------

            OpeningGrossWeight =
                opening.GrossWeight,

            OpeningStoneWeight =
                opening.StoneWeight,

            OpeningNetWeight =
                opening.NetWeight,


            // -------------------------------------------------
            // STOCK-IN WEIGHTS
            // -------------------------------------------------

            StockInGrossWeight =
                isStockIn
                    ? request.GrossWeight
                    : 0M,

            StockInStoneWeight =
                isStockIn
                    ? request.StoneWeight
                    : 0M,

            StockInNetWeight =
                isStockIn
                    ? request.NetWeight
                    : 0M,


            // -------------------------------------------------
            // STOCK-OUT WEIGHTS
            // -------------------------------------------------

            StockOutGrossWeight =
                isStockOut
                    ? request.GrossWeight
                    : 0M,

            StockOutStoneWeight =
                isStockOut
                    ? request.StoneWeight
                    : 0M,

            StockOutNetWeight =
                isStockOut
                    ? request.NetWeight
                    : 0M,


            // -------------------------------------------------
            // CLOSING WEIGHTS
            // -------------------------------------------------

            ClosingGrossWeight =
                closing.GrossWeight.GetValueOrDefault(),

            ClosingStoneWeight =
                CalculateClosingWeight(
                    opening.StoneWeight,
                    request.StoneWeight,
                    request.Direction),

            ClosingNetWeight =
                CalculateClosingWeight(
                    opening.NetWeight,
                    request.NetWeight,
                    request.Direction)
        };
    }


    // =========================================================
    // CREATE TAG-LEVEL PRODUCT TRANSACTION
    // ONLY FOR TAGGED ITEMS
    // =========================================================

    private static ProductTransaction
        CreateProductTransaction(
            StockMovementRequest request,
            StockBalanceSnapshot opening,
            Models.ProductStock closing)
    {
        return new ProductTransaction
        {
            RefGkey =
                request.DocumentLineGkey,

            TransactionDate =
                request.DocumentDate,

            ProductCategory =
                request.ProductCategory,

            ProductSku =
                request.ProductSku,

            TransactionType =
                GetTransactionType(
                    request.Purpose),

            DocumentNbr =
                request.DocumentNumber,

            DocumentType =
                request.DocumentType,

            VoucherType =
                request.DocumentType,

            ObQty =
                opening.Quantity,

            TransactionQty =
                request.Quantity,

            CbQty =
                closing.StockQty.GetValueOrDefault(),

            UnitPrice =
                request.UnitPrice,

            TransactionValue =
                request.TransactionValue,

            Notes =
                request.Notes ??
                request.Reason,

            DocumentDate =
                request.DocumentDate,

            OpeningGrossWeight =
                opening.GrossWeight,

            OpeningStoneWeight =
                opening.StoneWeight,

            OpeningNetWeight =
                opening.NetWeight,

            TransactionGrossWeight =
                request.GrossWeight,

            TransactionStoneWeight =
                request.StoneWeight,

            TransactionNetWeight =
                request.NetWeight,

            ClosingGrossWeight =
                closing.BalanceWeight.GetValueOrDefault(),

            ClosingStoneWeight =
                CalculateClosingWeight(
                    opening.StoneWeight,
                    request.StoneWeight,
                    request.Direction),

            ClosingNetWeight =
                CalculateClosingWeight(
                    opening.NetWeight,
                    request.NetWeight,
                    request.Direction)
        };
    }


    // =========================================================
    // CALCULATE CLOSING WEIGHT
    // =========================================================

    private static decimal CalculateClosingWeight(
        decimal opening,
        decimal movement,
        StockMovementDirection direction)
    {
        var closing =
            direction == StockMovementDirection.In
                ? opening + movement
                : opening - movement;

        return NormaliseWeight(closing);
    }


    // =========================================================
    // TRANSACTION TYPE
    // =========================================================

    private static string GetTransactionType(
        StockMovementPurpose purpose)
    {
        return purpose switch
        {
            StockMovementPurpose.Sale =>
                "SALE",

            StockMovementPurpose.PurchaseReceipt =>
                "PURCHASE_RECEIPT",

            StockMovementPurpose.StockAdjustmentIncrease =>
                "STOCK_ADJUSTMENT_IN",

            StockMovementPurpose.StockAdjustmentDecrease =>
                "STOCK_ADJUSTMENT_OUT",

            StockMovementPurpose.StockReallocationOut =>
                "STOCK_REALLOCATION_OUT",

            StockMovementPurpose.StockReallocationIn =>
                "STOCK_REALLOCATION_IN",

            StockMovementPurpose.MaterialIssue =>
                "MATERIAL_ISSUE",

            StockMovementPurpose.MaterialReceipt =>
                "MATERIAL_RECEIPT",

            StockMovementPurpose.BranchTransferOut =>
                "Issue",

            StockMovementPurpose.BranchTransferIn =>
                "TRANSFER_IN",

            StockMovementPurpose.WorkshopIssue =>
                "WORKSHOP_ISSUE",

            StockMovementPurpose.WorkshopReceipt =>
                "WORKSHOP_RECEIPT",

            StockMovementPurpose.OldMetalPurchase => 
                "OLD_METAL_PURCHASE",

            StockMovementPurpose.OldMetalTransferOut => 
                "OLD_METAL_TRANSFER_OUT",

            _ =>
                throw new InvalidOperationException(
                    $"Unsupported stock movement purpose: " +
                    $"{purpose}.")
        };
    }


    // =========================================================
    // PRODUCT REFERENCE FOR ERROR MESSAGES
    // =========================================================

    private static string GetProductReference(
        StockMovementRequest request)
    {
        if (!string.IsNullOrWhiteSpace(
                request.ProductSku))
        {
            return request.ProductSku;
        }

        if (!string.IsNullOrWhiteSpace(
                request.ProductCategory))
        {
            return
                $"{request.ProductCategory} " +
                $"(ProductGkey {request.ProductGkey})";
        }

        return
            $"ProductGkey {request.ProductGkey}";
    }


    // =========================================================
    // NORMALISE DECIMAL WEIGHT
    // =========================================================

    private static decimal NormaliseWeight(
        decimal value)
    {
        return Math.Abs(value) <= WeightTolerance
            ? 0M
            : value;
    }


    // =========================================================
    // INTERNAL BALANCE SNAPSHOT
    // =========================================================

    private sealed class StockBalanceSnapshot
    {
        public int Quantity { get; init; }

        public decimal GrossWeight { get; init; }

        public decimal StoneWeight { get; init; }

        public decimal NetWeight { get; init; }
    }
}