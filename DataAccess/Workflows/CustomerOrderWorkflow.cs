using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Contracts.CustomerOrders;
using System.Linq;

namespace DataAccess.Workflows;

public sealed class CustomerOrderWorkflow : ICustomerOrderWorkflow
{
    private const string DocumentType = "Customer Order";

    private readonly IRepositoryBase<CustomerOrder> _orderRepository;
    private readonly IRepositoryBase<CustomerOrderLine> _lineRepository;
    private readonly IRepositoryBase<VoucherType> _voucherTypeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerOrderWorkflow(
        IRepositoryBase<CustomerOrder> orderRepository,
        IRepositoryBase<CustomerOrderLine> lineRepository,
        IRepositoryBase<VoucherType> voucherTypeRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _lineRepository = lineRepository;
        _voucherTypeRepository = voucherTypeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SaveCustomerOrderResponse> SaveAsync(
        SaveCustomerOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Header);

        if (request.Lines == null ||
            request.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                "Customer order must contain at least one line.");
        }

        var isNewOrder = request.Header.Gkey <= 0;

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                cancellationToken);

        try
        {
            CustomerOrder savedOrder;

            if (isNewOrder)
            {
                savedOrder = await CreateOrderAsync(
                    request.Header,
                    cancellationToken);
            }
            else
            {
                savedOrder = await UpdateOrderAsync(
                    request.Header,
                    cancellationToken);
            }

            await SyncLinesAsync(
                savedOrder,
                request.Lines,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return new SaveCustomerOrderResponse
            {
                Gkey = savedOrder.Gkey,
                OrderNbr = savedOrder.OrderNbr ?? string.Empty,
                IsNew = isNewOrder
            };
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private async Task<CustomerOrder> CreateOrderAsync(
    CustomerOrderSaveModel source,
    CancellationToken cancellationToken)
    {
        var order = new CustomerOrder();

        MapHeader(source, order);

        order.OrderNbr =
            await GenerateOrderNumberAsync(
                cancellationToken);

        order.CreatedOn = DateTime.Now;
        order.ModifiedOn = null;

        _orderRepository.Add(order);

        // Save header first so SQL Server generates Gkey.
        // We are still inside the transaction.
        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return order;
    }

    private async Task<CustomerOrder> UpdateOrderAsync(
        CustomerOrderSaveModel source,
        CancellationToken cancellationToken)
    {
        var existing =
            await _orderRepository.GetAsync(
                x => x.Gkey == source.Gkey);

        if (existing == null)
        {
            throw new InvalidOperationException(
                $"Customer order GKey {source.Gkey} was not found.");
        }

        MapHeader(source, existing);

        existing.ModifiedOn = DateTime.Now;

        return existing;
    }

    private async Task SyncLinesAsync(
        CustomerOrder order,
        IReadOnlyCollection<CustomerOrderLineSaveModel> incomingLines,
        CancellationToken cancellationToken)
    {
        var existingLines =
            (await _lineRepository.GetListAsync(
                x => x.OrderGkey == order.Gkey))
            .ToList();

        DeleteRemovedLines(
            existingLines,
            incomingLines);

        var lineNumber = 1;

        foreach (var incomingLine in incomingLines)
        {
            if (incomingLine.Gkey <= 0)
            {
                AddNewLine(
                    order,
                    incomingLine,
                    lineNumber);
            }
            else
            {
                UpdateExistingLine(
                    order,
                    existingLines,
                    incomingLine,
                    lineNumber);
            }

            lineNumber++;
        }
    }

    private void DeleteRemovedLines(
        IEnumerable<CustomerOrderLine> existingLines,
        IEnumerable<CustomerOrderLineSaveModel> incomingLines)
    {
        var incomingKeys =
            incomingLines
                .Where(x => x.Gkey > 0)
                .Select(x => x.Gkey)
                .ToHashSet();

        foreach (var existingLine in existingLines)
        {
            if (!incomingKeys.Contains(existingLine.Gkey))
            {
                _lineRepository.Remove(existingLine);
            }
        }
    }

    private void AddNewLine(
        CustomerOrder order,
        CustomerOrderLineSaveModel source,
        int lineNumber)
    {
        var line = new CustomerOrderLine();

        MapLine(source, line);

        line.OrderGkey = order.Gkey;
        line.OrderNbr = order.OrderNbr;
        line.OrderLineNbr = lineNumber;

        line.TenantGkey = order.TenantGkey;

        line.CreatedOn = DateTime.Now;
        line.ModifiedOn = null;

        _lineRepository.Add(line);
    }

    private static void UpdateExistingLine(
        CustomerOrder order,
        IEnumerable<CustomerOrderLine> existingLines,
        CustomerOrderLineSaveModel source,
        int lineNumber)
    {
        var existing =
            existingLines.FirstOrDefault(
                x => x.Gkey == source.Gkey);

        if (existing == null)
        {
            throw new InvalidOperationException(
                $"Customer order line GKey {source.Gkey} was not found.");
        }

        if (existing.OrderGkey != order.Gkey)
        {
            throw new InvalidOperationException(
                $"Customer order line {source.Gkey} " +
                "does not belong to this order.");
        }

        MapLine(source, existing);

        existing.OrderGkey = order.Gkey;
        existing.OrderNbr = order.OrderNbr;
        existing.OrderLineNbr = lineNumber;

        existing.TenantGkey = order.TenantGkey;

        existing.ModifiedOn = DateTime.Now;
    }

    private static void MapHeader(
        CustomerOrderSaveModel source,
        CustomerOrder target)
    {
        target.CustGkey = source.CustGkey;
        target.CustMobileNbr = source.CustMobileNbr;

        target.OrderDate = source.OrderDate;
        target.OrderType = source.OrderType;
        target.OrderDueDate = source.OrderDueDate;
        target.DeliveryDate = source.DeliveryDate;

        target.OrderStatusFlag = source.OrderStatusFlag;

        target.OrderBranch = source.OrderBranch;
        target.ServiceBranch = source.ServiceBranch;
        target.DeliveryBranch = source.DeliveryBranch;

        target.OrderTransferDate = source.OrderTransferDate;

        target.BaseMaterial = source.BaseMaterial;

        target.TotalGrossWeight = source.TotalGrossWeight;
        target.TotalStoneWeight = source.TotalStoneWeight;
        target.TotalNetWeight = source.TotalNetWeight;

        target.OrderedItems = source.OrderedItems;
        target.FulfilledItems = source.FulfilledItems;

        target.OldMetalNetWeight = source.OldMetalNetWeight;
        target.OldMetalFineWeight = source.OldMetalFineWeight;

        target.BalanceWeight = source.BalanceWeight;

        target.MetalRate = source.MetalRate;

        target.TotalMakingCharges = source.TotalMakingCharges;
        target.TotalTaxAmount = source.TotalTaxAmount;
        target.TotalOrderAmount = source.TotalOrderAmount;

        target.AdvancePaidAmount = source.AdvancePaidAmount;
        target.BalanceAmount = source.BalanceAmount;

        target.Remark = source.Remark;

        target.TenantGkey = source.TenantGkey;

        target.OrderRefNbr = source.OrderRefNbr;

        if (!string.IsNullOrWhiteSpace(source.ModifiedBy))
        {
            target.ModifiedBy = source.ModifiedBy;
        }

        if (target.Gkey <= 0 &&
            !string.IsNullOrWhiteSpace(source.CreatedBy))
        {
            target.CreatedBy = source.CreatedBy;
        }
    }

    private static void MapLine(
        CustomerOrderLineSaveModel source,
        CustomerOrderLine target)
    {
        target.ProdCategory = source.ProdCategory;

        target.ProductGkey = source.ProductGkey;
        target.ProductId = source.ProductId;
        target.ProductSku = source.ProductSku;
        target.ProductName = source.ProductName;
        target.ProductDesc = source.ProductDesc;

        target.ProductMetal = source.ProductMetal;
        target.ProductPurity = source.ProductPurity;

        target.OrderSpecification = source.OrderSpecification;

        target.ProdQty = source.ProdQty;

        target.ProdGrossWeight = source.ProdGrossWeight;
        target.ProdStoneWeight = source.ProdStoneWeight;
        target.ProdNetWeight = source.ProdNetWeight;

        target.OrderType = source.OrderType;

        target.ItemNotes = source.ItemNotes;
        target.ItemPacked = source.ItemPacked;

        target.OrderItemDueDate = source.OrderItemDueDate;
        target.DeliveryDate = source.DeliveryDate;

        target.OrderItemStatusFlag =
            source.OrderItemStatusFlag;

        target.OrderBranch = source.OrderBranch;
        target.ServiceBranch = source.ServiceBranch;
        target.DeliveryBranch = source.DeliveryBranch;

        target.OrderTransferDate =
            source.OrderTransferDate;

        target.TotalGrossWeight =
            source.TotalGrossWeight;

        target.TotalStoneWeight =
            source.TotalStoneWeight;

        target.TotalNetWeight =
            source.TotalNetWeight;

        target.OrderedItems =
            source.OrderedItems;

        target.FulfilledItems =
            source.FulfilledItems;

        target.OldMetalNetWeight =
            source.OldMetalNetWeight;

        target.OldMetalFinePercent =
            source.OldMetalFinePercent;

        target.OldMetalFineWeight =
            source.OldMetalFineWeight;

        target.BalanceWeight =
            source.BalanceWeight;

        target.MetalRate =
            source.MetalRate;

        target.MakingCharges =
            source.MakingCharges;

        target.VaPercent =
            source.VaPercent;

        target.VaAmount =
            source.VaAmount;

        target.TaxAmount =
            source.TaxAmount;

        target.OrderAmount =
            source.OrderAmount;

        target.AdvancePaidAmount =
            source.AdvancePaidAmount;

        target.BalanceAmount =
            source.BalanceAmount;

        target.Remark =
            source.Remark;

        target.CatalogId =
            source.CatalogId;

        target.DesignName =
            source.DesignName;

        target.PageNbr =
            source.PageNbr;

        target.ImageName =
            source.ImageName;

        target.ImagePath =
            source.ImagePath;

        target.ModifiedBy =
            source.ModifiedBy;

        /*
         * Deliberately NOT copied:
         *
         * Gkey
         * OrderGkey
         * OrderNbr
         * OrderLineNbr
         * CreatedBy
         * CreatedOn
         *
         * Workflow controls those values.
         */
    }

    private async Task<string> GenerateOrderNumberAsync(
        CancellationToken cancellationToken)
    {
        var voucherType =
            await _voucherTypeRepository.GetAsync(
                x => x.DocumentType == DocumentType);

        if (voucherType == null)
        {
            throw new InvalidOperationException(
                $"Voucher type '{DocumentType}' is not configured.");
        }

        var nextNumber =
            (voucherType.LastUsedNumber ?? 0) + 1;

        voucherType.LastUsedNumber = nextNumber;

        var prefix =
            voucherType.DocNbrPrefix ?? string.Empty;

        var length =
            voucherType.DocNbrLength ?? 0;

        var number =
            length > 0
                ? nextNumber.ToString($"D{length}")
                : nextNumber.ToString();

        return $"{prefix}{number}";
    }
}