using DataAccess.Models;
using DataAccess.Repository;

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


    public StockMovementService(
        IRepositoryBase<Models.ProductStock> productStockRepository,
        IRepositoryBase<ProductStockSummary> productStockSummaryRepository,
        IRepositoryBase<ProductTransaction> productTransactionRepository,
        IRepositoryBase<ProductTransactionSummary> productTransactionSummaryRepository)
    {
        _productStockRepository =
            productStockRepository;

        _productStockSummaryRepository =
            productStockSummaryRepository;

        _productTransactionRepository =
            productTransactionRepository;

        _productTransactionSummaryRepository =
            productTransactionSummaryRepository;
    }


    // =========================================================
    // POST MULTIPLE MOVEMENTS
    // =========================================================

    public void PostMovements(
        IEnumerable<StockMovementRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        foreach (var request in requests)
        {
            PostMovement(request);
        }
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

                throw new NotSupportedException(
                    "Stock IN will be enabled when " +
                    "GRN/material receipt is integrated.");


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
        // ---------------------------------------------------------
        // IMPORTANT BUSINESS RULE
        // ---------------------------------------------------------
        //
        // ProductStockSummary represents system-recorded stock.
        //
        // In some branches, physical stock may already be available
        // for sale while GRN / stock-entry work is still pending
        // because of operational/staff constraints.
        //
        // Therefore insufficient summary quantity/weight must NOT
        // prevent an invoice from being finalised.
        //
        // Negative summary stock is allowed and will expose the
        // pending stock-entry discrepancy for later reconciliation.
        // ---------------------------------------------------------

        var availableQty =
            summary.StockQty.GetValueOrDefault();

        var availableWeight =
            summary.BalanceWeight.GetValueOrDefault();

        // Intentionally no exception for insufficient summary stock.
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
                $"Invoice weight exceeds tagged stock weight " +
                $"for '{request.ProductSku}'. " +
                $"Tag weight: {availableWeight:N3}, " +
                $"Invoice weight: {request.GrossWeight:N3}.");
        }
    }


    // =========================================================
    // APPLY PRODUCT SUMMARY STOCK OUT
    // =========================================================

    private void ApplySummaryStockOut(
        ProductStockSummary summary,
        StockMovementRequest request)
    {
        summary.StockQty =
            summary.StockQty.GetValueOrDefault()
            - request.Quantity;


        summary.BalanceWeight =
            NormaliseWeight(
                summary.BalanceWeight.GetValueOrDefault()
                - request.GrossWeight);


        // -----------------------------------------------------
        // PURPOSE-SPECIFIC TOTALS
        // -----------------------------------------------------

        switch (request.Purpose)
        {
            case StockMovementPurpose.Sale:

                summary.SoldQty =
                    summary.SoldQty.GetValueOrDefault()
                    + request.Quantity;

                summary.SoldWeight =
                    summary.SoldWeight.GetValueOrDefault()
                    + request.GrossWeight;

                break;


            case StockMovementPurpose.StockAdjustmentDecrease:

                summary.AdjustedQty =
                    summary.AdjustedQty.GetValueOrDefault()
                    - request.Quantity;

                summary.AdjustedWeight =
                    summary.AdjustedWeight.GetValueOrDefault()
                    - request.GrossWeight;

                break;


            default:

                // Other OUT purposes currently reduce only
                // the current stock balance.
                break;
        }


        var empty =
            IsEmpty(
                summary.StockQty.GetValueOrDefault(),
                summary.BalanceWeight.GetValueOrDefault());


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
                summary.BalanceWeight.GetValueOrDefault(),

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

            StockMovementPurpose.MaterialIssue =>
                "MATERIAL_ISSUE",

            StockMovementPurpose.MaterialReceipt =>
                "MATERIAL_RECEIPT",

            StockMovementPurpose.BranchTransferOut =>
                "TRANSFER_OUT",

            StockMovementPurpose.BranchTransferIn =>
                "TRANSFER_IN",

            StockMovementPurpose.WorkshopIssue =>
                "WORKSHOP_ISSUE",

            StockMovementPurpose.WorkshopReceipt =>
                "WORKSHOP_RECEIPT",

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