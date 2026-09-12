using DataAccess.Inventory.ProductStock;
using DataAccess.Models;
using DataAccess.Repository;
using DataAccess.Services;
using InvEntry.Contracts.StockTransfers;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace DataAccess.Workflows;


// ============================================================
// INTERFACE
// ============================================================

public interface IStockTransferWorkflow
{
    Task<StockTransferDetailResponse> CreateAsync(
        CreateStockTransferRequest request,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<StockTransferListItemResponse>> GetListAsync(
        DateTime? fromDate,
        DateTime? toDate,
        string? status,
        string? transferType,
        int? toReferenceGkey,
        CancellationToken cancellationToken = default);

    Task<StockTransferDetailResponse?> GetDetailAsync(
        int gkey,
        CancellationToken cancellationToken = default);
}


// ============================================================
// WORKFLOW
// ============================================================

public sealed class StockTransferWorkflow
    : IStockTransferWorkflow
{
    // ========================================================
    // CONSTANTS
    // ========================================================

    public const string OrnamentTransferType =
        "ORNAMENT";

    public const string OldMetalTransferType =
        "OLD_METAL";

    public const string PostedStatus =
        "POSTED";

    private const string DocumentType =
        "Stock Transfer";

    private const string DestinationReferenceName =
        "STOCK_TRANSFER";

    private const decimal WeightTolerance =
        0.001M;


    // ========================================================
    // DEPENDENCIES
    // ========================================================

    private readonly MijmsContext _context;

    private readonly IUnitOfWork _unitOfWork;

    private readonly IStockMovementService
        _stockMovementService;

    private readonly IOldMetalTransferPostingService
        _oldMetalTransferPostingService;

    private readonly IVoucherNumberService
        _voucherNumberService;


    // ========================================================
    // CONSTRUCTOR
    // ========================================================

    public StockTransferWorkflow(
        MijmsContext context,
        IUnitOfWork unitOfWork,
        IStockMovementService stockMovementService,
        IOldMetalTransferPostingService oldMetalTransferPostingService,
        IVoucherNumberService voucherNumberService)
    {
        _context =
            context
            ?? throw new ArgumentNullException(
                nameof(context));

        _unitOfWork =
            unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));

        _stockMovementService =
            stockMovementService
            ?? throw new ArgumentNullException(
                nameof(stockMovementService));

        _oldMetalTransferPostingService =
            oldMetalTransferPostingService
            ?? throw new ArgumentNullException(
                nameof(oldMetalTransferPostingService));

        _voucherNumberService =
            voucherNumberService
            ?? throw new ArgumentNullException(
                nameof(voucherNumberService));
    }


    // ========================================================
    // CREATE TRANSFER
    // ========================================================

    public async Task<StockTransferDetailResponse> CreateAsync(
        CreateStockTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        cancellationToken
            .ThrowIfCancellationRequested();


        NormaliseRequest(
            request);

        ValidateHeader(
            request);


        /*
         * SERIALIZABLE is intentionally retained.
         *
         * Stock Transfer number generation and the complete
         * document posting must remain inside one transaction.
         */
        await using var transaction =
            await _context.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);


        try
        {
            // ------------------------------------------------
            // 1. Resolve destination
            // ------------------------------------------------

            var destination =
                await GetDestinationAsync(
                    request.ToReferenceGkey,
                    cancellationToken);


            // ------------------------------------------------
            // 2. Build / validate all lines
            // ------------------------------------------------

            var lines =
                await BuildLinesAsync(
                    request,
                    cancellationToken);


            // ------------------------------------------------
            // 3. Generate Stock Transfer document number
            //
            // Central numbering service now owns:
            //
            // VoucherType lookup
            // LastUsedNumber increment
            // Prefix
            // Number length
            //
            // It must NOT SaveChanges or commit transaction.
            // ------------------------------------------------

            var transferNumber =
                await _voucherNumberService
                    .GetNextNumberAsync(
                        DocumentType,
                        cancellationToken);


            if (string.IsNullOrWhiteSpace(
                    transferNumber))
            {
                throw new InvalidOperationException(
                    "Unable to generate Stock Transfer number.");
            }


            // ------------------------------------------------
            // 4. Create aggregate header
            // ------------------------------------------------

            var header =
                new StockTransferHeader
                {
                    TransferNbr =
                        transferNumber,

                    TransferDate =
                        request.TransferDate,

                    TransferType =
                        request.TransferType,

                    FromBranch =
                        request.FromBranch.Trim(),

                    FromTenantGkey =
                        request.FromTenantGkey,

                    ToReferenceGkey =
                        destination.Gkey,

                    /*
                     * Snapshot destination values.
                     *
                     * This is important because the reference
                     * master can change later.
                     */
                    ToReferenceCode =
                        destination.RefCode,

                    ToReferenceValue =
                        destination.RefValue,

                    Status =
                        PostedStatus,

                    Remarks =
                        NormaliseOptionalText(
                            request.Remarks),

                    TotalQty =
                        lines.Sum(
                            line => line.Qty),

                    TotalGrossWeight =
                        lines.Sum(
                            line => line.GrossWeight),

                    TotalStoneWeight =
                        lines.Sum(
                            line => line.StoneWeight),

                    TotalNetWeight =
                        lines.Sum(
                            line => line.NetWeight),

                    CreatedOn =
                        DateTime.Now,

                    Lines =
                        lines
                };


            _context.StockTransferHeaders
                .Add(
                    header);


            /*
             * IMPORTANT FIRST SAVE
             *
             * We deliberately save the transfer aggregate here
             * while still inside the same DB transaction.
             *
             * SQL Server now generates:
             *
             * StockTransferHeader.Gkey
             * StockTransferLine.Gkey
             *
             * Those keys are required by:
             *
             * Product stock movement
             * Old Metal compatibility posting
             */
            await _unitOfWork
                .SaveChangesAsync(
                    cancellationToken);


            // ------------------------------------------------
            // 5. Post domain-specific stock movement
            // ------------------------------------------------

            if (IsOrnamentTransfer(
                    header.TransferType))
            {
                PostOrnamentStock(
                    header);
            }
            else if (IsOldMetalTransfer(
                         header.TransferType))
            {
                /*
                 * ONE Stock Transfer can contain:
                 *
                 * Gold 916
                 * Gold 750
                 * Silver 925
                 * etc.
                 *
                 * OldMetalTransferPostingService generates ONE
                 * OM Transfer number and applies it to all rows.
                 */
                await _oldMetalTransferPostingService
                    .PostAsync(
                        header,
                        cancellationToken);
            }
            else
            {
                /*
                 * Defensive check.
                 *
                 * ValidateHeader() should prevent this path.
                 */
                throw new InvalidOperationException(
                    $"Unsupported Stock Transfer type " +
                    $"'{header.TransferType}'.");
            }


            // ------------------------------------------------
            // 6. Save stock / old-metal movements
            // ------------------------------------------------

            await _unitOfWork
                .SaveChangesAsync(
                    cancellationToken);


            // ------------------------------------------------
            // 7. Commit everything together
            // ------------------------------------------------

            await transaction
                .CommitAsync(
                    cancellationToken);


            return MapDetail(
                header);
        }
        catch
        {
            await transaction
                .RollbackAsync(
                    cancellationToken);

            throw;
        }
    }


    // ========================================================
    // GET LIST
    // ========================================================

    public async Task<IEnumerable<StockTransferListItemResponse>>
        GetListAsync(
            DateTime? fromDate,
            DateTime? toDate,
            string? status,
            string? transferType,
            int? toReferenceGkey,
            CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();


        var query =
            _context.StockTransferHeaders
                .AsNoTracking()
                .AsQueryable();


        if (fromDate.HasValue)
        {
            var from =
                fromDate.Value.Date;

            query =
                query.Where(
                    header =>
                        header.TransferDate >= from);
        }


        if (toDate.HasValue)
        {
            /*
             * Exclusive upper boundary is normally friendlier to
             * SQL indexes than applying .Date to the DB column.
             */
            var toExclusive =
                toDate.Value.Date
                    .AddDays(1);

            query =
                query.Where(
                    header =>
                        header.TransferDate < toExclusive);
        }


        if (!string.IsNullOrWhiteSpace(
                status))
        {
            var normalisedStatus =
                status.Trim();

            query =
                query.Where(
                    header =>
                        header.Status ==
                        normalisedStatus);
        }


        if (!string.IsNullOrWhiteSpace(
                transferType))
        {
            var normalisedTransferType =
                transferType
                    .Trim()
                    .ToUpperInvariant();

            query =
                query.Where(
                    header =>
                        header.TransferType ==
                        normalisedTransferType);
        }


        if (toReferenceGkey.HasValue)
        {
            query =
                query.Where(
                    header =>
                        header.ToReferenceGkey ==
                        toReferenceGkey.Value);
        }


        return await query
            .OrderByDescending(
                header =>
                    header.TransferDate)
            .ThenByDescending(
                header =>
                    header.Gkey)
            .Select(
                header =>
                    new StockTransferListItemResponse
                    {
                        Gkey =
                            header.Gkey,

                        TransferNbr =
                            header.TransferNbr,

                        TransferDate =
                            header.TransferDate,

                        TransferType =
                            header.TransferType,

                        FromBranch =
                            header.FromBranch,

                        ToReferenceGkey =
                            header.ToReferenceGkey,

                        ToReferenceCode =
                            header.ToReferenceCode,

                        ToReferenceValue =
                            header.ToReferenceValue,

                        Status =
                            header.Status,

                        TotalQty =
                            header.TotalQty,

                        TotalGrossWeight =
                            header.TotalGrossWeight,

                        TotalNetWeight =
                            header.TotalNetWeight
                    })
            .ToListAsync(
                cancellationToken);
    }


    // ========================================================
    // GET DETAIL
    // ========================================================

    public async Task<StockTransferDetailResponse?>
        GetDetailAsync(
            int gkey,
            CancellationToken cancellationToken = default)
    {
        if (gkey <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gkey),
                "Stock Transfer Gkey must be greater than zero.");
        }


        cancellationToken
            .ThrowIfCancellationRequested();


        var header =
            await _context.StockTransferHeaders
                .AsNoTracking()
                .Include(
                    item =>
                        item.Lines)
                .SingleOrDefaultAsync(
                    item =>
                        item.Gkey == gkey,
                    cancellationToken);


        return header is null
            ? null
            : MapDetail(
                header);
    }


    // ========================================================
    // DESTINATION
    // ========================================================

    private async Task<MtblReference>
        GetDestinationAsync(
            int toReferenceGkey,
            CancellationToken cancellationToken)
    {
        var destination =
            await _context.MtblReferences
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    reference =>
                        reference.Gkey ==
                            toReferenceGkey &&
                        reference.RefName ==
                            DestinationReferenceName,
                    cancellationToken);


        if (destination is null)
        {
            throw new KeyNotFoundException(
                "The selected transfer destination is invalid.");
        }


        if (!destination.IsActive)
        {
            throw new InvalidOperationException(
                "The selected transfer destination is inactive.");
        }


        if (string.IsNullOrWhiteSpace(
                destination.RefCode))
        {
            throw new InvalidOperationException(
                "The selected transfer destination has no reference code.");
        }


        if (string.IsNullOrWhiteSpace(
                destination.RefValue))
        {
            throw new InvalidOperationException(
                "The selected transfer destination has no reference value.");
        }


        return destination;
    }


    // ========================================================
    // BUILD LINES
    // ========================================================

    private async Task<List<StockTransferLine>>
        BuildLinesAsync(
            CreateStockTransferRequest request,
            CancellationToken cancellationToken)
    {
        if (IsOrnamentTransfer(
                request.TransferType))
        {
            ValidateOrnamentRequestLines(
                request);
        }


        var result =
            new List<StockTransferLine>(
                request.Lines.Count);


        for (var index = 0;
             index < request.Lines.Count;
             index++)
        {
            cancellationToken
                .ThrowIfCancellationRequested();


            var lineNumber =
                index + 1;


            var requestLine =
                request.Lines[index]
                ?? throw new InvalidOperationException(
                    $"Line {lineNumber} is invalid.");


            ValidateWeights(
                requestLine,
                lineNumber,
                requireNetFormula:
                    IsOldMetalTransfer(
                        request.TransferType));


            StockTransferLine line;


            if (IsOrnamentTransfer(
                    request.TransferType))
            {
                line =
                    await BuildOrnamentLineAsync(
                        requestLine,
                        lineNumber,
                        cancellationToken);
            }
            else
            {
                line =
                    await BuildOldMetalLineAsync(
                        requestLine,
                        lineNumber,
                        cancellationToken);
            }


            result.Add(
                line);
        }


        return result;
    }


    // ========================================================
    // ORNAMENT REQUEST VALIDATION
    // ========================================================

    private static void ValidateOrnamentRequestLines(
        CreateStockTransferRequest request)
    {
        if (request.Lines.Any(
                line =>
                    line is null ||
                    !line.ProductStockGkey.HasValue ||
                    line.ProductStockGkey.Value <= 0))
        {
            throw new InvalidOperationException(
                "Each ORNAMENT line requires a valid ProductStockGkey.");
        }


        var stockKeys =
            request.Lines
                .Select(
                    line =>
                        line.ProductStockGkey!.Value)
                .ToList();


        if (stockKeys.Distinct().Count() !=
            stockKeys.Count)
        {
            throw new InvalidOperationException(
                "The same ProductStockGkey cannot appear more than once " +
                "in a transfer.");
        }
    }


    // ========================================================
    // BUILD ORNAMENT LINE
    // ========================================================

    private async Task<StockTransferLine>
        BuildOrnamentLineAsync(
            CreateStockTransferLineRequest requestLine,
            int lineNumber,
            CancellationToken cancellationToken)
    {
        var stockGkey =
            requestLine.ProductStockGkey!.Value;


        var stock =
            await _context.ProductStocks
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Gkey ==
                        stockGkey,
                    cancellationToken);


        if (stock is null ||
            stock.IsProductSold == true)
        {
            throw new KeyNotFoundException(
                $"Line {lineNumber}: ProductStockGkey " +
                $"{stockGkey} is invalid or unavailable.");
        }


        if (!stock.ProductGkey.HasValue ||
            stock.ProductGkey.Value <= 0)
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: source stock has no ProductGkey.");
        }


        var product =
            await _context.Products
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Gkey ==
                        stock.ProductGkey.Value,
                    cancellationToken);


        if (product is null)
        {
            throw new KeyNotFoundException(
                $"Line {lineNumber}: product for source stock was not found.");
        }


        return new StockTransferLine
        {
            LineNbr =
                lineNumber,

            ProductStockGkey =
                stock.Gkey,

            ProductGkey =
                product.Gkey,

            ProductId =
                product.Id,

            ProductSku =
                stock.ProductSku,

            ProductName =
                product.Name,

            ProductCategory =
                stock.Category ??
                product.Category,

            Metal =
                product.Metal,

            Purity =
                product.Purity,

            Uom =
                product.Uom ??
                product.BaseUnit,

            Qty =
                stock.StockQty
                    .GetValueOrDefault(),

            GrossWeight =
                stock.GrossWeight
                    .GetValueOrDefault(),

            StoneWeight =
                stock.StoneWeight
                    .GetValueOrDefault(),

            NetWeight =
                stock.NetWeight
                    .GetValueOrDefault(),

            Notes =
                NormaliseOptionalText(
                    requestLine.Notes)
        };
    }


    // ========================================================
    // BUILD OLD METAL LINE
    // ========================================================

    private async Task<StockTransferLine>
        BuildOldMetalLineAsync(
            CreateStockTransferLineRequest requestLine,
            int lineNumber,
            CancellationToken cancellationToken)
    {
        if (!requestLine.ProductGkey.HasValue ||
            requestLine.ProductGkey.Value <= 0)
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: ProductGkey is required for OLD_METAL.");
        }


        if (string.IsNullOrWhiteSpace(
                requestLine.ProductId))
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: ProductId is required for OLD_METAL.");
        }


        if (string.IsNullOrWhiteSpace(
                requestLine.Metal))
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: Metal is required for OLD_METAL.");
        }


        if (string.IsNullOrWhiteSpace(
                requestLine.Purity))
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: Purity is required for OLD_METAL.");
        }


        if (string.IsNullOrWhiteSpace(
                requestLine.Uom))
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: UOM is required for OLD_METAL.");
        }


        if (requestLine.GrossWeight <=
            WeightTolerance)
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: Gross weight must be greater than zero.");
        }


        if (requestLine.NetWeight <=
            WeightTolerance)
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: Net weight must be greater than zero.");
        }


        /*
         * Do not trust Metal/Purity/ProductId supplied by WPF.
         *
         * ProductGkey is the identity.
         *
         * We load the product and use authoritative master data
         * below.
         */
        var product =
            await _context.Products
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Gkey ==
                        requestLine.ProductGkey.Value,
                    cancellationToken);


        if (product is null)
        {
            throw new KeyNotFoundException(
                $"Line {lineNumber}: old metal product was not found.");
        }


        if (!string.Equals(
                product.Id,
                requestLine.ProductId.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: ProductId does not match ProductGkey.");
        }


        /*
         * We intentionally persist Metal and Purity from Product
         * master rather than trusting the UI request.
         */
        if (string.IsNullOrWhiteSpace(
                product.Metal))
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: product master has no Metal.");
        }


        if (string.IsNullOrWhiteSpace(
                product.Purity))
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: product master has no Purity.");
        }


        return new StockTransferLine
        {
            LineNbr =
                lineNumber,

            ProductStockGkey =
                null,

            ProductGkey =
                product.Gkey,

            ProductId =
                product.Id,

            ProductSku =
                null,

            ProductName =
                product.Name,

            ProductCategory =
                product.Category,

            Metal =
                product.Metal,

            Purity =
                product.Purity,

            Uom =
                requestLine.Uom.Trim(),

            Qty =
                requestLine.Qty,

            GrossWeight =
                requestLine.GrossWeight,

            StoneWeight =
                requestLine.StoneWeight,

            NetWeight =
                requestLine.NetWeight,

            TransactedRate = null,
               // requestLine.TransactedRate,

            TransferValue = null,
              //  requestLine.TransferValue,

            Notes =
                NormaliseOptionalText(
                    requestLine.Notes)
        };
    }


    // ========================================================
    // HEADER VALIDATION
    // ========================================================

    private static void ValidateHeader(
        CreateStockTransferRequest request)
    {
        if (request.TransferDate ==
            default)
        {
            throw new InvalidOperationException(
                "TransferDate is required.");
        }


        if (!IsOrnamentTransfer(
                request.TransferType) &&
            !IsOldMetalTransfer(
                request.TransferType))
        {
            throw new InvalidOperationException(
                $"TransferType must be {OrnamentTransferType} " +
                $"or {OldMetalTransferType}.");
        }


        if (string.IsNullOrWhiteSpace(
                request.FromBranch))
        {
            throw new InvalidOperationException(
                "FromBranch is required.");
        }


        if (request.ToReferenceGkey <= 0)
        {
            throw new InvalidOperationException(
                "ToReferenceGkey is required.");
        }


        if (request.Lines is null ||
            request.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one transfer line is required.");
        }
    }


    // ========================================================
    // WEIGHT VALIDATION
    // ========================================================

    private static void ValidateWeights(
        CreateStockTransferLineRequest line,
        int lineNumber,
        bool requireNetFormula)
    {
        if (line.Qty < 0)
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: quantity cannot be negative.");
        }


        if (line.GrossWeight < 0M)
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: gross weight cannot be negative.");
        }


        if (line.StoneWeight < 0M)
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: stone weight cannot be negative.");
        }


        if (line.NetWeight < 0M)
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: net weight cannot be negative.");
        }


        if (line.StoneWeight >
            line.GrossWeight +
            WeightTolerance)
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: stone weight cannot exceed gross weight.");
        }


        if (line.Qty == 0 &&
            line.GrossWeight <=
            WeightTolerance)
        {
            throw new InvalidOperationException(
                $"Line {lineNumber}: quantity or gross weight is required.");
        }


        if (requireNetFormula)
        {
            var expectedNet =
                line.GrossWeight -
                line.StoneWeight;


            if (Math.Abs(
                    line.NetWeight -
                    expectedNet) >
                WeightTolerance)
            {
                throw new InvalidOperationException(
                    $"Line {lineNumber}: net weight must equal " +
                    "gross weight minus stone weight.");
            }
        }
    }


    // ========================================================
    // ORNAMENT STOCK POSTING
    // ========================================================

    private void PostOrnamentStock(
        StockTransferHeader header)
    {
        if (header.Lines is null ||
            header.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                "Stock Transfer contains no lines to post.");
        }


        var requests =
            header.Lines
                .OrderBy(
                    line =>
                        line.LineNbr)
                .Select(
                    line =>
                        new StockMovementRequest
                        {
                            DocumentGkey =
                                header.Gkey,

                            DocumentLineGkey =
                                line.Gkey,

                            DocumentNumber =
                                header.TransferNbr,

                            DocumentDate =
                                header.TransferDate,

                            DocumentType =
                                DocumentType,

                            ProductGkey =
                                line.ProductGkey
                                    .GetValueOrDefault(),

                            ProductStockGkey =
                                line.ProductStockGkey,

                            ProductSku =
                                line.ProductSku,

                            ProductCategory =
                                line.ProductCategory,

                            Direction =
                                StockMovementDirection.Out,

                            Purpose =
                                StockMovementPurpose.BranchTransferOut,

                            Quantity =
                                line.Qty,

                            GrossWeight =
                                line.GrossWeight,

                            StoneWeight =
                                line.StoneWeight,

                            NetWeight =
                                line.NetWeight,

                            Notes =
                                line.Notes
                        })
                .ToList();


        _stockMovementService
            .PostMovements(
                requests);
    }


    // ========================================================
    // REQUEST NORMALISATION
    // ========================================================

    private static void NormaliseRequest(
        CreateStockTransferRequest request)
    {
        if (!string.IsNullOrWhiteSpace(
                request.TransferType))
        {
            request.TransferType =
                request.TransferType
                    .Trim()
                    .ToUpperInvariant();
        }


        if (!string.IsNullOrWhiteSpace(
                request.FromBranch))
        {
            request.FromBranch =
                request.FromBranch.Trim();
        }


        request.Remarks =
            NormaliseOptionalText(
                request.Remarks);
    }


    // ========================================================
    // TYPE HELPERS
    // ========================================================

    private static bool IsOrnamentTransfer(
        string? transferType)
    {
        return string.Equals(
            transferType?.Trim(),
            OrnamentTransferType,
            StringComparison.OrdinalIgnoreCase);
    }


    private static bool IsOldMetalTransfer(
        string? transferType)
    {
        return string.Equals(
            transferType?.Trim(),
            OldMetalTransferType,
            StringComparison.OrdinalIgnoreCase);
    }


    // ========================================================
    // STRING NORMALISATION
    // ========================================================

    private static string? NormaliseOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(
                value)
            ? null
            : value.Trim();
    }


    // ========================================================
    // RESPONSE MAPPING
    // ========================================================

    private static StockTransferDetailResponse MapDetail(
        StockTransferHeader header)
    {
        return new StockTransferDetailResponse
        {
            Gkey =
                header.Gkey,

            TransferNbr =
                header.TransferNbr,

            TransferDate =
                header.TransferDate,

            TransferType =
                header.TransferType,

            FromBranch =
                header.FromBranch,

            FromTenantGkey =
                header.FromTenantGkey,

            ToReferenceGkey =
                header.ToReferenceGkey,

            ToReferenceCode =
                header.ToReferenceCode,

            ToReferenceValue =
                header.ToReferenceValue,

            Status =
                header.Status,

            Remarks =
                header.Remarks,

            TotalQty =
                header.TotalQty,

            TotalGrossWeight =
                header.TotalGrossWeight,

            TotalStoneWeight =
                header.TotalStoneWeight,

            TotalNetWeight =
                header.TotalNetWeight,

            CreatedOn =
                header.CreatedOn,

            Lines =
                header.Lines
                    .OrderBy(
                        line =>
                            line.LineNbr)
                    .Select(
                        line =>
                            new StockTransferLineResponse
                            {
                                Gkey =
                                    line.Gkey,

                                LineNbr =
                                    line.LineNbr,

                                ProductStockGkey =
                                    line.ProductStockGkey,

                                ProductGkey =
                                    line.ProductGkey,

                                ProductId =
                                    line.ProductId,

                                ProductSku =
                                    line.ProductSku,

                                ProductName =
                                    line.ProductName,

                                ProductCategory =
                                    line.ProductCategory,

                                Metal =
                                    line.Metal,

                                Purity =
                                    line.Purity,

                                Uom =
                                    line.Uom,

                                Qty =
                                    line.Qty,

                                GrossWeight =
                                    line.GrossWeight,   

                                StoneWeight =
                                    line.StoneWeight,

                                NetWeight =
                                    line.NetWeight,

                                TransactedRate =
                                    line.TransactedRate,

                                TransferValue =
                                    line.TransferValue,

                                Notes =
                                    line.Notes
                            })
                    .ToList()
        };
    }
}