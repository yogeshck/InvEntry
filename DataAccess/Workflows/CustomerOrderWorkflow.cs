using Azure.Core;
using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Contracts.CustomerOrders;
using System.Linq;
using System.Threading;

namespace DataAccess.Workflows;

public sealed class CustomerOrderWorkflow : ICustomerOrderWorkflow
{
    private const string DocumentType = "Customer Order";

    private readonly IRepositoryBase<CustomerOrder> _orderRepository;
    private readonly IRepositoryBase<CustomerOrderLine> _lineRepository;
    private readonly IRepositoryBase<Voucher> _voucherRepository;
    private readonly IRepositoryBase<VoucherType> _voucherTypeRepository;
    private readonly IRepositoryBase<OldMetalTransaction> _oldMetalRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerOrderWorkflow(
        IRepositoryBase<CustomerOrder> orderRepository,
        IRepositoryBase<CustomerOrderLine> lineRepository,
        IRepositoryBase<OldMetalTransaction> oldMetalRepository,
        IRepositoryBase<Voucher> voucherRepository,
        IRepositoryBase<VoucherType> voucherTypeRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _lineRepository = lineRepository;
        _oldMetalRepository = oldMetalRepository;
        _voucherRepository = voucherRepository;
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

            // ------------------------------------------------
            // Product Lines
            // ------------------------------------------------
            await SyncLinesAsync(
                savedOrder,
                request.Lines,
                cancellationToken);

            // ------------------------------------------------
            // Old Metal Transactions
            // ------------------------------------------------
            await SyncOldMetalAsync(
                savedOrder,
                request.OldMetalTransactions,
                cancellationToken);

            // ------------------------------------------------
            // Voucher 
            // ------------------------------------------------
            await SyncReceiptsAsync(
                savedOrder,
                request.Receipts,
                cancellationToken);

            // ------------------------------------------------
            // Save everything together
            // ------------------------------------------------
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

    private async Task SyncOldMetalAsync(
        CustomerOrder order,
        IReadOnlyCollection<OldMetalTransactionSaveModel> incomingItems,
        CancellationToken cancellationToken)
    {
        var existingItems =
            (await _oldMetalRepository.GetListAsync(
                x =>
                    x.DocRefGkey == order.Gkey &&
                    x.DocRefType == DocumentType))
            .ToList();

        DeleteRemovedOldMetal(
            existingItems,
            incomingItems);

        foreach (var incoming in incomingItems)
        {
            if (incoming.Gkey <= 0)
            {
                await AddNewOldMetalAsync(
                    order,
                    incoming,
                    cancellationToken);
            }
            else
            {
                UpdateExistingOldMetal(
                    order,
                    existingItems,
                    incoming);
            }
        }
    }

    private void DeleteRemovedOldMetal(
    IEnumerable<OldMetalTransaction> existingItems,
    IEnumerable<OldMetalTransactionSaveModel> incomingItems)
    {
        var incomingKeys =
            incomingItems
                .Where(x => x.Gkey > 0)
                .Select(x => x.Gkey)
                .ToHashSet();

        foreach (var existing in existingItems)
        {
            if (!incomingKeys.Contains(existing.Gkey))
            {
                _oldMetalRepository.Remove(existing);
            }
        }
    }

    private async Task AddNewOldMetalAsync(
    CustomerOrder order,
    OldMetalTransactionSaveModel source,
    CancellationToken cancellationToken)
    {
        var entity = new OldMetalTransaction();

        MapOldMetal(
            source,
            entity);

        SetOldMetalOrderReference(
            order,
            entity);

        entity.TransNbr =
            await GenerateOldMetalTransactionNumberAsync(
                source.TransType,
                cancellationToken);

        entity.TransDate ??= DateTime.Now;

        _oldMetalRepository.Add(entity);
    }

    private static void UpdateExistingOldMetal(
    CustomerOrder order,
    IEnumerable<OldMetalTransaction> existingItems,
    OldMetalTransactionSaveModel source)
    {
        var existing =
            existingItems.FirstOrDefault(
                x => x.Gkey == source.Gkey);

        if (existing == null)
        {
            throw new InvalidOperationException(
                $"Old metal transaction GKey {source.Gkey} was not found.");
        }

        if (existing.DocRefGkey != order.Gkey)
        {
            throw new InvalidOperationException(
                $"Old metal transaction {source.Gkey} does not belong to this customer order.");
        }

        MapOldMetal(
            source,
            existing);

        SetOldMetalOrderReference(
            order,
            existing);
    }

    private async Task SyncReceiptsAsync(
    CustomerOrder order,
    IReadOnlyCollection<CustomerOrderReceiptSaveModel> incomingReceipts,
    CancellationToken cancellationToken)
    {
        var existingReceipts =
            (await _voucherRepository.GetListAsync(
                x => x.RefDocGkey == order.Gkey &&
                     x.RefDocNbr == order.OrderNbr))
            .ToList();

        foreach (var incoming in incomingReceipts)
        {
            if (incoming.Gkey <= 0)
            {
                await AddNewReceiptAsync(
                    order,
                    incoming,
                    cancellationToken);
            }
            else
            {
                UpdateExistingReceipt(
                    order,
                    existingReceipts,
                    incoming);
            }
        }
    }

    private static void MapReceipt(
    CustomerOrderReceiptSaveModel source,
    Voucher target)
    {
        target.VoucherType = source.VoucherType;

        target.Mode = source.Mode;

        target.TransAmount =
            source.TransAmount;

        target.VoucherDate =
            source.VoucherDate;

        target.TransDate =
            source.TransDate;

        target.TransDesc =
            source.TransDesc;

        target.FromLedgerGkey =
            source.FromLedgerGkey;

        target.ToLedgerGkey =
            source.ToLedgerGkey;

        target.FundTransferMode =
            source.FundTransferMode;

        target.FundTransferRefGkey =
            source.FundTransferRefGkey;

        target.FundTransferDate =
            source.FundTransferDate;
    }

    private static void UpdateExistingReceipt(
    CustomerOrder order,
    IEnumerable<Voucher> existingReceipts,
    CustomerOrderReceiptSaveModel source)
    {
        var existing =
            existingReceipts.FirstOrDefault(
                x => x.Gkey == source.Gkey);

        if (existing == null)
        {
            throw new InvalidOperationException(
                $"Receipt GKey {source.Gkey} was not found for " +
                $"customer order '{order.OrderNbr}'.");
        }

        MapReceipt(
            source,
            existing);

        existing.CustomerGkey =
            order.CustGkey;

        existing.RefDocGkey =
            order.Gkey;

        existing.RefDocNbr =
            order.OrderNbr;

        existing.RefDocDate =
            order.OrderDate;
    }

    private async Task<string> GenerateVoucherNumberAsync(
    string voucherTypeName,
    CancellationToken cancellationToken)
    {
        var voucherType =
            await _voucherTypeRepository.GetAsync(
                x => x.DocumentType == voucherTypeName);

        if (voucherType == null)
        {
            throw new InvalidOperationException(
                $"Voucher type '{voucherTypeName}' is not configured.");
        }

        var nextNumber =
            (voucherType.LastUsedNumber ?? 0) + 1;

        voucherType.LastUsedNumber =
            nextNumber;

        var prefix =
            voucherType.DocNbrPrefix ?? string.Empty;

        var length =
            voucherType.DocNbrLength ?? 4;

        return
            $"{prefix}{nextNumber.ToString($"D{length}")}";
    }

    private async Task AddNewReceiptAsync(
                            CustomerOrder order,
                            CustomerOrderReceiptSaveModel source,
                            CancellationToken cancellationToken)
    {
        if (!source.TransAmount.HasValue ||
            source.TransAmount.Value <= 0)
        {
            throw new InvalidOperationException(
                "Receipt amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(source.VoucherType))
        {
            throw new InvalidOperationException(
                "Voucher type is required for receipt.");
        }

        var entity = new Voucher();

        MapReceipt(source, entity);

        // Customer/order references are controlled by server.
        entity.CustomerGkey = order.CustGkey;

        entity.RefDocGkey = order.Gkey;
        entity.RefDocNbr = order.OrderNbr;
        entity.RefDocDate = order.OrderDate;

        entity.VoucherDate ??= DateTime.Now;
        entity.TransDate ??= DateTime.Now;

        // Receipt / money received.
        entity.TransType = "RECEIPT";

        // Cash / UPI / Bank etc.
        entity.VoucherType = source.VoucherType;

        entity.SeqNbr =
            await GenerateVoucherSequenceAsync(
                entity.VoucherDate.Value,
                entity.VoucherType);

        entity.VoucherNbr =
            await GenerateVoucherNumberAsync(
                entity.VoucherType,
                cancellationToken);

        _voucherRepository.Add(entity);
    }

    private async Task<int> GenerateVoucherSequenceAsync(
        DateTime voucherDate,
        string voucherType)
    {
        var vouchers =
            await _voucherRepository.GetListAsync(
                x =>
                    x.VoucherDate.HasValue &&
                    x.VoucherDate.Value.Date == voucherDate.Date &&
                    x.VoucherType == voucherType);

        return
            (vouchers.Max(x => x.SeqNbr) ?? 0) + 1;
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

    private static void SetOldMetalOrderReference(
    CustomerOrder order,
    OldMetalTransaction entity)
    {
        entity.DocRefGkey = order.Gkey;
        entity.DocRefNbr = order.OrderNbr;
        entity.DocRefDate = order.OrderDate;
        entity.DocRefType = DocumentType;
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

    private static void MapOldMetal(
    OldMetalTransactionSaveModel source,
    OldMetalTransaction target)
    {
        target.TransDate = source.TransDate;
        target.TransType = source.TransType;

        target.CustGkey = source.CustGkey;
        target.CustMobile = source.CustMobile;

        target.ProductGkey = source.ProductGkey;
        target.ProductId = source.ProductId;
        target.ProductCategory = source.ProductCategory;

        target.Metal = source.Metal;
        target.Purity = source.Purity;

        target.TransactedRate = source.TransactedRate;

        target.Uom = source.Uom;

        target.GrossWeight = source.GrossWeight;
        target.StoneWeight = source.StoneWeight;

        target.WastagePercent = source.WastagePercent;
        target.WastageWeight = source.WastageWeight;

        target.NetWeight = source.NetWeight;

        target.TotalProposedPrice =
            source.TotalProposedPrice;

        target.FinalPurchasePrice =
            source.FinalPurchasePrice;

        target.Remarks = source.Remarks;
    }

    private async Task<string> GenerateOldMetalTransactionNumberAsync(
    string? transactionType,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(transactionType))
        {
            throw new InvalidOperationException(
                "Old metal transaction type is required.");
        }

        var voucherType =
            await _voucherTypeRepository.GetAsync(
                x => x.DocumentType == transactionType);

        if (voucherType == null)
        {
            throw new InvalidOperationException(
                $"Voucher type '{transactionType}' is not configured.");
        }

        var nextNumber =
            (voucherType.LastUsedNumber ?? 0) + 1;

        voucherType.LastUsedNumber =
            nextNumber;

        var prefix =
            voucherType.DocNbrPrefix ?? string.Empty;

        var length =
            voucherType.DocNbrLength ?? 4;

        var formattedNumber =
            nextNumber.ToString($"D{length}");

        return $"{prefix}{formattedNumber}";
    }

}