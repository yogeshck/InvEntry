using DataAccess.Inventory.ProductStock;
using DataAccess.Models;
using DataAccess.Repository;
using DataAccess.Services;
using InvEntry.Contracts.StockAdjustments;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace DataAccess.Workflows;

public interface IStockAdjustmentWorkflow
{
    Task<StockAdjustmentResponse> CreateAsync(
        CreateStockAdjustmentRequest request,
        CancellationToken cancellationToken = default);

    Task<StockAdjustmentResponse?> GetDetailAsync(
        int gkey,
        CancellationToken cancellationToken = default);
}

public sealed class StockAdjustmentWorkflow
    : IStockAdjustmentWorkflow
{
    private const string NumberDocumentType = "Stock Adjustment";
    private const string LedgerDocumentType = "STOCK_ADJUSTMENT";
    private const decimal WeightTolerance = 0.001M;

    private readonly MijmsContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStockMovementService _stockMovementService;
    private readonly IVoucherNumberService _voucherNumberService;
    private readonly IAuditIdentityProvider _auditIdentityProvider;

    public StockAdjustmentWorkflow(
        MijmsContext context,
        IUnitOfWork unitOfWork,
        IStockMovementService stockMovementService,
        IVoucherNumberService voucherNumberService,
        IAuditIdentityProvider auditIdentityProvider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _stockMovementService = stockMovementService ?? throw new ArgumentNullException(nameof(stockMovementService));
        _voucherNumberService = voucherNumberService ?? throw new ArgumentNullException(nameof(voucherNumberService));
        _auditIdentityProvider = auditIdentityProvider ?? throw new ArgumentNullException(nameof(auditIdentityProvider));
    }

    public async Task<StockAdjustmentResponse> CreateAsync(
        CreateStockAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        NormaliseRequest(request);
        ValidateHeader(request);
        var auditIdentity = GetAuditIdentity();

        // Number generation, document persistence and all stock movements are
        // committed as one unit. A posting error rolls back the document too.
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            var preparedLines =
                await PrepareLinesAsync(request, cancellationToken);

            ValidatePreparedDocument(request, preparedLines);

            var adjustmentNumber =
                await _voucherNumberService.GetNextNumberAsync(
                    NumberDocumentType,
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(adjustmentNumber))
            {
                throw new InvalidOperationException(
                    "Unable to generate Stock Adjustment number.");
            }

            var totals = CalculateTotals(request, preparedLines);
            var now = DateTime.Now;

            var header = new StockAdjustmentHeader
            {
                AdjustmentNbr = adjustmentNumber,
                AdjustmentDate = request.AdjustmentDate,
                AdjustmentType = request.AdjustmentType,
                ReasonCode = request.ReasonCode,
                Status = StockAdjustmentStatuses.Draft,
                Remarks = NullIfWhiteSpace(request.Remarks),
                TotalQty = totals.Qty,
                TotalGrossWeight = totals.Gross,
                TotalStoneWeight = totals.Stone,
                TotalNetWeight = totals.Net,
                CreatedBy = auditIdentity,
                CreatedOn = now
            };

            foreach (var prepared in preparedLines.OrderBy(x => x.LineNbr))
            {
                header.StockAdjustmentLines.Add(new StockAdjustmentLine
                {
                    LineNbr = prepared.LineNbr,
                    Direction = prepared.Direction,
                    StockLevel = prepared.StockLevel,
                    MovementKind = prepared.MovementKind,
                    ProductGkey = prepared.ProductGkey,
                    ProductStockGkey = prepared.ProductStockGkey,
                    ProductSku = prepared.ProductSku,
                    ProductCategory = prepared.ProductCategory,
                    Metal = prepared.Metal,
                    Purity = prepared.Purity,
                    Uom = prepared.Uom,
                    Qty = prepared.Qty,
                    GrossWeight = prepared.GrossWeight,
                    StoneWeight = prepared.StoneWeight,
                    NetWeight = prepared.NetWeight,
                    PairNbr = prepared.PairNbr,
                    Notes = prepared.Notes
                });
            }

            _context.StockAdjustmentHeaders.Add(header);

            // Obtain header/line Gkeys before creating movement ledger rows.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var movementLines =
                header.AdjustmentType == StockAdjustmentTypes.Reallocation
                    ? header.StockAdjustmentLines
                        .OrderBy(x => x.Direction == StockAdjustmentDirections.Out ? 0 : 1)
                        .ThenBy(x => x.LineNbr)
                    : header.StockAdjustmentLines
                        .OrderBy(x => x.LineNbr);

            var movementRequests =
                movementLines
                    .Select(line => BuildMovementRequest(header, line))
                    .ToList();

            _stockMovementService.PostMovements(movementRequests);

            header.Status = StockAdjustmentStatuses.Posted;
            header.FinalisedOn = DateTime.Now;
            header.ModifiedBy = auditIdentity;
            header.ModifiedOn = header.FinalisedOn;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return MapResponse(header);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            _unitOfWork.ClearChanges();
            throw;
        }
    }

    private string GetAuditIdentity()
    {
        var identity = _auditIdentityProvider.GetCurrentIdentity();

        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new InvalidOperationException(
                "The audit identity provider returned an empty identity.");
        }

        identity = identity.Trim();

        if (identity.Length > 50)
        {
            throw new InvalidOperationException(
                "The audit identity provider returned an identity longer than 50 characters.");
        }

        return identity;
    }

    public async Task<StockAdjustmentResponse?> GetDetailAsync(
        int gkey,
        CancellationToken cancellationToken = default)
    {
        if (gkey <= 0)
        {
            return null;
        }

        var header =
            await _context.StockAdjustmentHeaders
                .AsNoTracking()
                .Include(x => x.StockAdjustmentLines)
                .SingleOrDefaultAsync(
                    x => x.Gkey == gkey,
                    cancellationToken);

        return header == null
            ? null
            : MapResponse(header);
    }

    private async Task<List<PreparedLine>> PrepareLinesAsync(
        CreateStockAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = new List<PreparedLine>(request.Lines.Count);

        foreach (var line in request.Lines.OrderBy(x => x.LineNbr))
        {
            ValidateBasicLine(line);

            var product =
                await _context.Products
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        x => x.Gkey == line.ProductGkey,
                        cancellationToken);

            if (product == null)
            {
                throw new KeyNotFoundException(
                    $"Product {line.ProductGkey} was not found.");
            }

            ProductStock? taggedStock = null;

            if (line.StockLevel == StockAdjustmentStockLevels.Sku)
            {
                taggedStock =
                    await _context.ProductStocks
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            x =>
                                x.Gkey == line.ProductStockGkey!.Value &&
                                x.ProductGkey == line.ProductGkey &&
                                x.ProductSku == line.ProductSku,
                            cancellationToken);

                if (taggedStock == null)
                {
                    throw new InvalidOperationException(
                        $"SKU '{line.ProductSku}' does not match the selected product stock record.");
                }
            }

            result.Add(new PreparedLine
            {
                LineNbr = line.LineNbr,
                Direction = line.Direction,
                StockLevel = line.StockLevel,
                MovementKind = line.MovementKind,
                ProductGkey = product.Gkey,
                ProductStockGkey = taggedStock?.Gkey,
                ProductSku = taggedStock?.ProductSku,
                ProductCategory =
                    FirstNonBlank(product.Category, taggedStock?.Category, line.ProductCategory)
                    ?? throw new InvalidOperationException(
                        $"Product category is required for product {product.Gkey}."),
                Metal = FirstNonBlank(product.Metal, line.Metal),
                Purity = FirstNonBlank(product.Purity, line.Purity),
                Uom = FirstNonBlank(product.Uom, line.Uom),
                Qty = line.Qty,
                GrossWeight = line.GrossWeight,
                StoneWeight = line.StoneWeight,
                NetWeight = line.NetWeight,
                PairNbr = line.PairNbr,
                Notes = NullIfWhiteSpace(line.Notes),
                TaggedIsSold = taggedStock?.IsProductSold == true,
                TaggedStockQty = taggedStock?.StockQty.GetValueOrDefault() ?? 0
            });
        }

        return result;
    }

    private static void ValidateHeader(
        CreateStockAdjustmentRequest request)
    {
        if (request.AdjustmentDate == default)
        {
            throw new InvalidOperationException(
                "Adjustment date is required.");
        }

        if (request.AdjustmentType != StockAdjustmentTypes.Increase &&
            request.AdjustmentType != StockAdjustmentTypes.Decrease &&
            request.AdjustmentType != StockAdjustmentTypes.Reallocation)
        {
            throw new InvalidOperationException(
                "Adjustment type must be INCREASE, DECREASE or REALLOCATION.");
        }

        if (string.IsNullOrWhiteSpace(request.ReasonCode))
        {
            throw new InvalidOperationException(
                "Adjustment reason is required.");
        }

        if (!StockAdjustmentReasonCodes.IsValidForAdjustmentType(
                request.AdjustmentType,
                request.ReasonCode))
        {
            var adjustmentTypeName =
                request.AdjustmentType switch
                {
                    StockAdjustmentTypes.Increase => "Increase",
                    StockAdjustmentTypes.Decrease => "Decrease",
                    StockAdjustmentTypes.Reallocation => "Reallocation",
                    _ => request.AdjustmentType
                };
            var article =
                request.AdjustmentType == StockAdjustmentTypes.Increase
                    ? "an"
                    : "a";

            throw new InvalidOperationException(
                $"The selected reason is not valid for {article} {adjustmentTypeName} stock adjustment.");
        }

        if (string.Equals(
                request.ReasonCode,
                StockAdjustmentReasonCodes.Other,
                StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(request.Remarks))
        {
            throw new InvalidOperationException(
                "Remarks are required when reason is OTHER.");
        }

        if (request.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one stock adjustment line is required.");
        }

        var duplicateLineNbr =
            request.Lines
                .GroupBy(x => x.LineNbr)
                .FirstOrDefault(x => x.Count() > 1);

        if (duplicateLineNbr != null)
        {
            throw new InvalidOperationException(
                $"Adjustment line number {duplicateLineNbr.Key} is duplicated.");
        }
    }

    private static void ValidateBasicLine(
        CreateStockAdjustmentLineRequest line)
    {
        if (line.LineNbr <= 0)
        {
            throw new InvalidOperationException(
                "Adjustment line number must be greater than zero.");
        }

        if (line.Direction != StockAdjustmentDirections.In &&
            line.Direction != StockAdjustmentDirections.Out)
        {
            throw new InvalidOperationException(
                $"Line {line.LineNbr}: direction must be IN or OUT.");
        }

        if (line.StockLevel != StockAdjustmentStockLevels.Consolidated &&
            line.StockLevel != StockAdjustmentStockLevels.Sku)
        {
            throw new InvalidOperationException(
                $"Line {line.LineNbr}: stock level must be CONSOLIDATED or SKU.");
        }

        if (line.MovementKind != StockAdjustmentMovementKinds.Weight &&
            line.MovementKind != StockAdjustmentMovementKinds.WholeItem)
        {
            throw new InvalidOperationException(
                $"Line {line.LineNbr}: movement kind must be WEIGHT or WHOLE_ITEM.");
        }

        if (line.ProductGkey <= 0)
        {
            throw new InvalidOperationException(
                $"Line {line.LineNbr}: a valid product is required.");
        }

        if (line.StockLevel == StockAdjustmentStockLevels.Consolidated)
        {
            if (line.ProductStockGkey.HasValue ||
                !string.IsNullOrWhiteSpace(line.ProductSku))
            {
                throw new InvalidOperationException(
                    $"Line {line.LineNbr}: consolidated stock must not contain a SKU.");
            }
        }
        else
        {
            if (!line.ProductStockGkey.HasValue ||
                line.ProductStockGkey.Value <= 0 ||
                string.IsNullOrWhiteSpace(line.ProductSku))
            {
                throw new InvalidOperationException(
                    $"Line {line.LineNbr}: SKU stock requires ProductStockGkey and ProductSku.");
            }
        }

        if (line.Qty < 0 ||
            line.GrossWeight < 0M ||
            line.StoneWeight < 0M ||
            line.NetWeight < 0M)
        {
            throw new InvalidOperationException(
                $"Line {line.LineNbr}: quantity and weights cannot be negative.");
        }

        if (line.StoneWeight > line.GrossWeight + WeightTolerance)
        {
            throw new InvalidOperationException(
                $"Line {line.LineNbr}: stone weight cannot exceed gross weight.");
        }

        if (Math.Abs(
                line.NetWeight -
                (line.GrossWeight - line.StoneWeight)) >
            WeightTolerance)
        {
            throw new InvalidOperationException(
                $"Line {line.LineNbr}: net weight must equal gross weight less stone weight.");
        }

        if (line.Qty == 0 &&
            line.GrossWeight <= WeightTolerance)
        {
            throw new InvalidOperationException(
                $"Line {line.LineNbr}: enter a quantity or weight.");
        }

        if (line.MovementKind == StockAdjustmentMovementKinds.Weight &&
            line.Qty != 0)
        {
            throw new InvalidOperationException(
                $"Line {line.LineNbr}: a WEIGHT movement must use Qty = 0.");
        }

        if (line.MovementKind == StockAdjustmentMovementKinds.WholeItem &&
            line.Qty <= 0)
        {
            throw new InvalidOperationException(
                $"Line {line.LineNbr}: a WHOLE_ITEM movement requires Qty greater than zero.");
        }
    }

    private static void ValidatePreparedDocument(
        CreateStockAdjustmentRequest request,
        IReadOnlyList<PreparedLine> lines)
    {
        switch (request.AdjustmentType)
        {
            case StockAdjustmentTypes.Increase:
                if (lines.Any(x => x.Direction != StockAdjustmentDirections.In))
                {
                    throw new InvalidOperationException(
                        "Increase adjustment can contain only IN lines.");
                }
                break;

            case StockAdjustmentTypes.Decrease:
                if (lines.Any(x => x.Direction != StockAdjustmentDirections.Out))
                {
                    throw new InvalidOperationException(
                        "Decrease adjustment can contain only OUT lines.");
                }
                break;

            case StockAdjustmentTypes.Reallocation:
                ValidateReallocation(lines);
                break;
        }

        // Never use stock adjustment to reopen an item that is already sold.
        // A sales return must use its own business workflow.
        var soldSku =
            lines.FirstOrDefault(x =>
                x.StockLevel == StockAdjustmentStockLevels.Sku &&
                x.TaggedIsSold);

        if (soldSku != null)
        {
            throw new InvalidOperationException(
                $"SKU '{soldSku.ProductSku}' is already sold and cannot be adjusted.");
        }

        // A SKU receiving a whole item must represent an empty/recoverable
        // existing tag. A normal active one-item barcode must not become Qty 2.
        foreach (var line in lines.Where(x =>
                     x.StockLevel == StockAdjustmentStockLevels.Sku &&
                     x.MovementKind == StockAdjustmentMovementKinds.WholeItem &&
                     x.Direction == StockAdjustmentDirections.In))
        {
            if (!line.TaggedIsSold && line.TaggedStockQty > 0)
            {
                throw new InvalidOperationException(
                    $"SKU '{line.ProductSku}' already contains an active item.");
            }
        }
    }

    private static void ValidateReallocation(
        IReadOnlyList<PreparedLine> lines)
    {
        if (lines.Count != 2)
        {
            throw new InvalidOperationException(
                "Reallocation requires exactly one OUT line and one IN line.");
        }

        var outLine =
            lines.SingleOrDefault(x => x.Direction == StockAdjustmentDirections.Out);
        var inLine =
            lines.SingleOrDefault(x => x.Direction == StockAdjustmentDirections.In);

        if (outLine == null || inLine == null)
        {
            throw new InvalidOperationException(
                "Reallocation requires one OUT line and one IN line.");
        }

        if (!outLine.PairNbr.HasValue ||
            outLine.PairNbr != inLine.PairNbr)
        {
            throw new InvalidOperationException(
                "Reallocation OUT and IN lines must share the same PairNbr.");
        }

        if (outLine.MovementKind != inLine.MovementKind ||
            outLine.Qty != inLine.Qty ||
            !SameWeight(outLine.GrossWeight, inLine.GrossWeight) ||
            !SameWeight(outLine.StoneWeight, inLine.StoneWeight) ||
            !SameWeight(outLine.NetWeight, inLine.NetWeight))
        {
            throw new InvalidOperationException(
                "Reallocation OUT and IN quantity/weights must be identical.");
        }

        if (outLine.StockLevel == StockAdjustmentStockLevels.Sku &&
            inLine.StockLevel == StockAdjustmentStockLevels.Sku &&
            outLine.ProductStockGkey == inLine.ProductStockGkey)
        {
            throw new InvalidOperationException(
                "Reallocation source and destination SKU cannot be the same item.");
        }

        if (outLine.StockLevel == StockAdjustmentStockLevels.Consolidated &&
            inLine.StockLevel == StockAdjustmentStockLevels.Consolidated &&
            outLine.ProductGkey == inLine.ProductGkey)
        {
            throw new InvalidOperationException(
                "Reallocation between the same consolidated stock has no stock effect.");
        }

        ValidateMaterialCompatibility(outLine, inLine);
    }

    private static void ValidateMaterialCompatibility(
        PreparedLine source,
        PreparedLine destination)
    {
        EnsureCompatible("metal", source.Metal, destination.Metal);
        EnsureCompatible("purity", source.Purity, destination.Purity);
        EnsureCompatible("UOM", source.Uom, destination.Uom);
    }

    private static void EnsureCompatible(
        string field,
        string? source,
        string? destination)
    {
        if (!string.IsNullOrWhiteSpace(source) &&
            !string.IsNullOrWhiteSpace(destination) &&
            !string.Equals(
                source.Trim(),
                destination.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Reallocation source and destination {field} do not match.");
        }
    }

    private static StockMovementRequest BuildMovementRequest(
        StockAdjustmentHeader header,
        StockAdjustmentLine line)
    {
        var direction =
            line.Direction == StockAdjustmentDirections.In
                ? StockMovementDirection.In
                : StockMovementDirection.Out;

        StockMovementPurpose purpose;

        if (header.AdjustmentType == StockAdjustmentTypes.Reallocation)
        {
            purpose =
                direction == StockMovementDirection.Out
                    ? StockMovementPurpose.StockReallocationOut
                    : StockMovementPurpose.StockReallocationIn;
        }
        else
        {
            purpose =
                direction == StockMovementDirection.Out
                    ? StockMovementPurpose.StockAdjustmentDecrease
                    : StockMovementPurpose.StockAdjustmentIncrease;
        }

        return new StockMovementRequest
        {
            DocumentGkey = header.Gkey,
            DocumentLineGkey = line.Gkey,
            DocumentNumber = header.AdjustmentNbr,
            DocumentDate = header.AdjustmentDate,
            DocumentType = LedgerDocumentType,
            ProductGkey = line.ProductGkey,
            ProductStockGkey = line.ProductStockGkey,
            ProductSku = line.ProductSku ?? string.Empty,
            ProductCategory = line.ProductCategory,
            Direction = direction,
            Purpose = purpose,
            Quantity = line.Qty,
            GrossWeight = line.GrossWeight,
            StoneWeight = line.StoneWeight,
            NetWeight = line.NetWeight,
            Reason = header.ReasonCode,
            Notes = string.IsNullOrWhiteSpace(line.Notes)
                ? header.Remarks
                : line.Notes
        };
    }

    private static MovementTotals CalculateTotals(
        CreateStockAdjustmentRequest request,
        IReadOnlyList<PreparedLine> lines)
    {
        // Reallocation is zero-sum. Header totals describe the transferred
        // magnitude once, rather than OUT + IN doubled.
        var totalLines =
            request.AdjustmentType == StockAdjustmentTypes.Reallocation
                ? lines.Where(x => x.Direction == StockAdjustmentDirections.Out)
                : lines;

        return new MovementTotals(
            totalLines.Sum(x => x.Qty),
            totalLines.Sum(x => x.GrossWeight),
            totalLines.Sum(x => x.StoneWeight),
            totalLines.Sum(x => x.NetWeight));
    }

    private static StockAdjustmentResponse MapResponse(
        StockAdjustmentHeader header)
    {
        return new StockAdjustmentResponse
        {
            Gkey = header.Gkey,
            AdjustmentNbr = header.AdjustmentNbr,
            AdjustmentDate = header.AdjustmentDate,
            AdjustmentType = header.AdjustmentType,
            ReasonCode = header.ReasonCode,
            Status = header.Status,
            Remarks = header.Remarks,
            TotalQty = header.TotalQty,
            TotalGrossWeight = header.TotalGrossWeight,
            TotalStoneWeight = header.TotalStoneWeight,
            TotalNetWeight = header.TotalNetWeight,
            CreatedOn = header.CreatedOn,
            FinalisedOn = header.FinalisedOn,
            Lines = header.StockAdjustmentLines
                .OrderBy(x => x.LineNbr)
                .Select(x => new StockAdjustmentLineResponse
                {
                    Gkey = x.Gkey,
                    LineNbr = x.LineNbr,
                    Direction = x.Direction,
                    StockLevel = x.StockLevel,
                    MovementKind = x.MovementKind,
                    ProductGkey = x.ProductGkey,
                    ProductStockGkey = x.ProductStockGkey,
                    ProductSku = x.ProductSku,
                    ProductCategory = x.ProductCategory,
                    Metal = x.Metal,
                    Purity = x.Purity,
                    Uom = x.Uom,
                    Qty = x.Qty,
                    GrossWeight = x.GrossWeight,
                    StoneWeight = x.StoneWeight,
                    NetWeight = x.NetWeight,
                    PairNbr = x.PairNbr,
                    Notes = x.Notes
                })
                .ToList()
        };
    }

    private static void NormaliseRequest(
        CreateStockAdjustmentRequest request)
    {
        request.AdjustmentType =
            request.AdjustmentType.Trim().ToUpperInvariant();
        request.ReasonCode =
            request.ReasonCode.Trim().ToUpperInvariant();
        request.Remarks = NullIfWhiteSpace(request.Remarks);

        foreach (var line in request.Lines)
        {
            line.Direction =
                line.Direction.Trim().ToUpperInvariant();
            line.StockLevel =
                line.StockLevel.Trim().ToUpperInvariant();
            line.MovementKind =
                line.MovementKind.Trim().ToUpperInvariant();
            line.ProductSku =
                NullIfWhiteSpace(line.ProductSku)?.ToUpperInvariant();
            line.ProductCategory =
                line.ProductCategory.Trim();
            line.Metal = NullIfWhiteSpace(line.Metal);
            line.Purity = NullIfWhiteSpace(line.Purity);
            line.Uom = NullIfWhiteSpace(line.Uom);
            line.Notes = NullIfWhiteSpace(line.Notes);
        }
    }

    private static bool SameWeight(
        decimal left,
        decimal right) =>
        Math.Abs(left - right) <= WeightTolerance;

    private static string? NullIfWhiteSpace(
        string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

    private static string? FirstNonBlank(
        params string?[] values) =>
        values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();

    private sealed class PreparedLine
    {
        public int LineNbr { get; init; }
        public string Direction { get; init; } = string.Empty;
        public string StockLevel { get; init; } = string.Empty;
        public string MovementKind { get; init; } = string.Empty;
        public int ProductGkey { get; init; }
        public int? ProductStockGkey { get; init; }
        public string? ProductSku { get; init; }
        public string ProductCategory { get; init; } = string.Empty;
        public string? Metal { get; init; }
        public string? Purity { get; init; }
        public string? Uom { get; init; }
        public int Qty { get; init; }
        public decimal GrossWeight { get; init; }
        public decimal StoneWeight { get; init; }
        public decimal NetWeight { get; init; }
        public int? PairNbr { get; init; }
        public string? Notes { get; init; }
        public bool TaggedIsSold { get; init; }
        public int TaggedStockQty { get; init; }
    }

    private readonly record struct MovementTotals(
        int Qty,
        decimal Gross,
        decimal Stone,
        decimal Net);
}
