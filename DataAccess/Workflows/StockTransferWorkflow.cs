using DataAccess.Models;
using DataAccess.Repository;
using DataAccess.Inventory.ProductStock;
using InvEntry.Contracts.StockTransfers;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace DataAccess.Workflows;

public interface IStockTransferWorkflow
{
    Task<StockTransferDetailResponse> CreateAsync(CreateStockTransferRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockTransferListItemResponse>> GetListAsync(DateTime? fromDate, DateTime? toDate, string? status, string? transferType, int? toReferenceGkey, CancellationToken cancellationToken = default);
    Task<StockTransferDetailResponse?> GetDetailAsync(int gkey, CancellationToken cancellationToken = default);
}

public sealed class StockTransferWorkflow : IStockTransferWorkflow
{
    public const string OrnamentTransferType = "ORNAMENT";
    public const string OldMetalTransferType = "OLD_METAL";
    public const string PostedStatus = "POSTED";
    private const string DocumentType = "Stock Transfer";
    private const decimal WeightTolerance = 0.001M;

    private readonly MijmsContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStockMovementService _stockMovementService;

    public StockTransferWorkflow(
        MijmsContext context,
        IUnitOfWork unitOfWork,
        IStockMovementService stockMovementService)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _stockMovementService = stockMovementService;
    }

    public async Task<StockTransferDetailResponse> CreateAsync(CreateStockTransferRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateHeader(request);

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var destination = await _context.MtblReferences.SingleOrDefaultAsync(
                reference => reference.Gkey == request.ToReferenceGkey && reference.RefName == "STOCK_TRANSFER",
                cancellationToken);

            if (destination is null)
                throw new KeyNotFoundException("The selected transfer destination is invalid.");

            if (!destination.IsActive)
                throw new InvalidOperationException("The selected transfer destination is inactive.");

            var lines = await BuildLinesAsync(request, cancellationToken);
            var voucherType = await _context.VoucherTypes.SingleOrDefaultAsync(
                voucher => voucher.DocumentType == DocumentType,
                cancellationToken);

            if (voucherType is null || string.IsNullOrWhiteSpace(voucherType.DocNbrPrefix) || voucherType.DocNbrLength.GetValueOrDefault() <= 0)
                throw new InvalidOperationException("Branch Transfer VoucherType configuration is missing or incomplete.");

            var documentNumberLength = voucherType.DocNbrLength.GetValueOrDefault();
            voucherType.LastUsedNumber = voucherType.LastUsedNumber.GetValueOrDefault() + 1;
            var header = new StockTransferHeader
            {
                TransferNbr = $"{voucherType.DocNbrPrefix}{voucherType.LastUsedNumber.Value.ToString($"D{documentNumberLength}")}",
                TransferDate = request.TransferDate,
                TransferType = request.TransferType,
                FromBranch = request.FromBranch.Trim(),
                FromTenantGkey = request.FromTenantGkey,
                ToReferenceGkey = destination.Gkey,
                ToReferenceCode = destination.RefCode,
                ToReferenceValue = destination.RefValue,
                Status = PostedStatus,
                Remarks = request.Remarks?.Trim(),
                TotalQty = lines.Sum(line => line.Qty),
                TotalGrossWeight = lines.Sum(line => line.GrossWeight),
                TotalStoneWeight = lines.Sum(line => line.StoneWeight),
                TotalNetWeight = lines.Sum(line => line.NetWeight),
                CreatedOn = DateTime.Now,
                Lines = lines
            };

            _context.StockTransferHeaders.Add(header);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (request.TransferType == OrnamentTransferType)
            {
                PostOrnamentStock(header);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return MapDetail(header);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IEnumerable<StockTransferListItemResponse>> GetListAsync(DateTime? fromDate, DateTime? toDate, string? status, string? transferType, int? toReferenceGkey, CancellationToken cancellationToken = default)
    {
        var query = _context.StockTransferHeaders.AsNoTracking().AsQueryable();
        if (fromDate.HasValue) query = query.Where(header => header.TransferDate.Date >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(header => header.TransferDate.Date <= toDate.Value.Date);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(header => header.Status == status.Trim());
        if (!string.IsNullOrWhiteSpace(transferType)) query = query.Where(header => header.TransferType == transferType.Trim());
        if (toReferenceGkey.HasValue) query = query.Where(header => header.ToReferenceGkey == toReferenceGkey.Value);

        return await query.OrderByDescending(header => header.TransferDate).ThenByDescending(header => header.Gkey)
            .Select(header => new StockTransferListItemResponse
            {
                Gkey = header.Gkey, TransferNbr = header.TransferNbr, TransferDate = header.TransferDate,
                TransferType = header.TransferType, FromBranch = header.FromBranch, ToReferenceGkey = header.ToReferenceGkey,
                ToReferenceCode = header.ToReferenceCode, ToReferenceValue = header.ToReferenceValue, Status = header.Status,
                TotalQty = header.TotalQty, TotalGrossWeight = header.TotalGrossWeight, TotalNetWeight = header.TotalNetWeight
            }).ToListAsync(cancellationToken);
    }

    public async Task<StockTransferDetailResponse?> GetDetailAsync(int gkey, CancellationToken cancellationToken = default)
    {
        var header = await _context.StockTransferHeaders.AsNoTracking().Include(header => header.Lines)
            .SingleOrDefaultAsync(header => header.Gkey == gkey, cancellationToken);
        return header is null ? null : MapDetail(header);
    }

    private async Task<List<StockTransferLine>> BuildLinesAsync(CreateStockTransferRequest request, CancellationToken cancellationToken)
    {
        if (request.TransferType == OrnamentTransferType)
        {
            if (request.Lines.Any(line => !line.ProductStockGkey.HasValue || line.ProductStockGkey <= 0))
                throw new InvalidOperationException("Each ORNAMENT line requires a valid ProductStockGkey.");
            if (request.Lines.Select(line => line.ProductStockGkey!.Value).Distinct().Count() != request.Lines.Count)
                throw new InvalidOperationException("The same ProductStockGkey cannot appear more than once in a transfer.");
        }

        var result = new List<StockTransferLine>();
        for (var index = 0; index < request.Lines.Count; index++)
        {
            var requestLine = request.Lines[index] ?? throw new InvalidOperationException($"Line {index + 1} is invalid.");
            ValidateWeights(requestLine, index + 1, request.TransferType == OldMetalTransferType);

            if (request.TransferType == OrnamentTransferType)
            {
                var stock = await _context.ProductStocks.AsNoTracking().SingleOrDefaultAsync(item => item.Gkey == requestLine.ProductStockGkey!.Value, cancellationToken);
                if (stock is null || stock.IsProductSold == true)
                    throw new KeyNotFoundException($"Line {index + 1}: ProductStockGkey is invalid or unavailable.");
                if (!stock.ProductGkey.HasValue)
                    throw new InvalidOperationException($"Line {index + 1}: source stock has no ProductGkey.");
                var product = await _context.Products.AsNoTracking().SingleOrDefaultAsync(item => item.Gkey == stock.ProductGkey.Value, cancellationToken)
                    ?? throw new KeyNotFoundException($"Line {index + 1}: product for source stock was not found.");
                result.Add(new StockTransferLine
                {
                    LineNbr = index + 1, ProductStockGkey = stock.Gkey, ProductGkey = product.Gkey, ProductId = product.Id,
                    ProductSku = stock.ProductSku, ProductName = product.Name, ProductCategory = stock.Category ?? product.Category,
                    Metal = product.Metal, Purity = product.Purity, Uom = product.Uom ?? product.BaseUnit,
                    Qty = stock.StockQty.GetValueOrDefault(), GrossWeight = stock.GrossWeight.GetValueOrDefault(),
                    StoneWeight = stock.StoneWeight.GetValueOrDefault(), NetWeight = stock.NetWeight.GetValueOrDefault(), Notes = requestLine.Notes?.Trim()
                });
            }
            else
            {
                if (!requestLine.ProductGkey.HasValue || requestLine.ProductGkey <= 0 || string.IsNullOrWhiteSpace(requestLine.ProductId) || string.IsNullOrWhiteSpace(requestLine.Metal) || string.IsNullOrWhiteSpace(requestLine.Purity) || string.IsNullOrWhiteSpace(requestLine.Uom))
                    throw new InvalidOperationException($"Line {index + 1}: ProductGkey, ProductId, Metal, Purity, and Uom are required for OLD_METAL.");
                var product = await _context.Products.SingleOrDefaultAsync(item => item.Gkey == requestLine.ProductGkey.Value, cancellationToken);
                if (product is null || product.Id != requestLine.ProductId)
                    throw new KeyNotFoundException($"Line {index + 1}: old metal product information is invalid.");
                result.Add(new StockTransferLine
                {
                    LineNbr = index + 1, ProductGkey = product.Gkey, ProductId = product.Id, ProductName = product.Name,
                    ProductCategory = product.Category, Metal = product.Metal, Purity = product.Purity,
                    Uom = requestLine.Uom.Trim(), Qty = requestLine.Qty, GrossWeight = requestLine.GrossWeight,
                    StoneWeight = requestLine.StoneWeight, NetWeight = requestLine.NetWeight,
                    TransactedRate = requestLine.TransactedRate, TransferValue = requestLine.TransferValue, Notes = requestLine.Notes?.Trim()
                });
            }
        }
        return result;
    }

    private static void ValidateHeader(CreateStockTransferRequest request)
    {
        if (request.TransferDate == default) throw new InvalidOperationException("TransferDate is required.");
        if (request.TransferType is not (OrnamentTransferType or OldMetalTransferType)) throw new InvalidOperationException("TransferType must be ORNAMENT or OLD_METAL.");
        if (string.IsNullOrWhiteSpace(request.FromBranch)) throw new InvalidOperationException("FromBranch is required.");
        if (request.ToReferenceGkey <= 0) throw new InvalidOperationException("ToReferenceGkey is required.");
        if (request.Lines is null || request.Lines.Count == 0) throw new InvalidOperationException("At least one transfer line is required.");
    }

    private static void ValidateWeights(CreateStockTransferLineRequest line, int lineNumber, bool requireNetFormula)
    {
        if (line.Qty < 0 || line.GrossWeight < 0M || line.StoneWeight < 0M || line.NetWeight < 0M)
            throw new InvalidOperationException($"Line {lineNumber}: quantity and weights cannot be negative.");
        if (line.StoneWeight > line.GrossWeight + WeightTolerance)
            throw new InvalidOperationException($"Line {lineNumber}: stone weight cannot exceed gross weight.");
        if (line.Qty == 0 && line.GrossWeight <= WeightTolerance)
            throw new InvalidOperationException($"Line {lineNumber}: quantity or gross weight is required.");
        if (requireNetFormula && Math.Abs(line.NetWeight - (line.GrossWeight - line.StoneWeight)) > WeightTolerance)
            throw new InvalidOperationException($"Line {lineNumber}: net weight must equal gross weight minus stone weight.");
    }

    private void PostOrnamentStock(StockTransferHeader header)
    {
        var requests = header.Lines
            .OrderBy(line => line.ProductStockGkey)
            .Select(line => new StockMovementRequest
        {
            DocumentGkey = header.Gkey,
            DocumentLineGkey = line.Gkey,
            DocumentNumber = header.TransferNbr,
            DocumentDate = header.TransferDate,
            DocumentType = DocumentType,
            ProductGkey = line.ProductGkey.GetValueOrDefault(),
            ProductStockGkey = line.ProductStockGkey,
            ProductSku = line.ProductSku ?? string.Empty,
            ProductCategory = line.ProductCategory,
            Direction = StockMovementDirection.Out,
            Purpose = StockMovementPurpose.BranchTransferOut,
            Quantity = line.Qty,
            GrossWeight = line.GrossWeight,
            StoneWeight = line.StoneWeight,
            NetWeight = line.NetWeight,
            Notes = line.Notes
        });

        _stockMovementService.PostMovements(requests);
    }

    private static StockTransferDetailResponse MapDetail(StockTransferHeader header) => new()
    {
        Gkey = header.Gkey, TransferNbr = header.TransferNbr, TransferDate = header.TransferDate, TransferType = header.TransferType,
        FromBranch = header.FromBranch, FromTenantGkey = header.FromTenantGkey, ToReferenceGkey = header.ToReferenceGkey,
        ToReferenceCode = header.ToReferenceCode, ToReferenceValue = header.ToReferenceValue, Status = header.Status,
        Remarks = header.Remarks, TotalQty = header.TotalQty, TotalGrossWeight = header.TotalGrossWeight,
        TotalStoneWeight = header.TotalStoneWeight, TotalNetWeight = header.TotalNetWeight, CreatedOn = header.CreatedOn,
        Lines = header.Lines.OrderBy(line => line.LineNbr).Select(line => new StockTransferLineResponse
        {
            Gkey = line.Gkey, LineNbr = line.LineNbr, ProductStockGkey = line.ProductStockGkey, ProductGkey = line.ProductGkey,
            ProductId = line.ProductId, ProductSku = line.ProductSku, ProductName = line.ProductName, ProductCategory = line.ProductCategory,
            Metal = line.Metal, Purity = line.Purity, Uom = line.Uom, Qty = line.Qty, GrossWeight = line.GrossWeight,
            StoneWeight = line.StoneWeight, NetWeight = line.NetWeight, TransactedRate = line.TransactedRate,
            TransferValue = line.TransferValue, Notes = line.Notes
        }).ToList()
    };
}