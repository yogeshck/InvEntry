using InvEntry.Contracts.CustomerOrders;
using InvEntry.Models;
using System;
using System.Linq;

namespace InvEntry.Mappers.CustomerOrders;

public static class CustomerOrderRequestMapper
{


    public static SaveCustomerOrderRequest ToSaveRequest(
    CustomerOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        // Header audit fields
        PrepareAuditFields(
            order,
            order.GKey <= 0);

        // Line audit fields
        foreach (var line in order.Lines ?? [])
        {
            PrepareAuditFields(
                line,
                line.GKey <= 0);
        }

        // Old metal audit fields
        foreach (var oldMetal in order.OldMetalTransactions ?? [])
        {
            PrepareAuditFields(
                oldMetal,
                oldMetal.GKey <= 0);
        }

        return new SaveCustomerOrderRequest
        {
            Header = MapHeader(order),

            Lines = order.Lines?
                .Select(MapLine)
                .ToList() ?? [],

            OldMetalTransactions =
                order.OldMetalTransactions?
                    .Select(MapOldMetal)
                    .ToList() ?? [],

            Receipts =
                order.AdvanceReceiptLines?
                    .Where(x =>
                        x.TransactionAmount.GetValueOrDefault() > 0)
                    .Select(MapReceipt)
                    .ToList() ?? []
        };
    }

    private static CustomerOrderSaveModel MapHeader(
        CustomerOrder source)
    {
        return new CustomerOrderSaveModel
        {
            Gkey = source.GKey,
            CustGkey = source.CustGkey,
            CustMobileNbr = source.CustMobileNbr,

            OrderNbr = source.OrderNbr,
            OrderDate = source.OrderDate,
            OrderType = source.OrderType,
            OrderDueDate = source.OrderDueDate,
            DeliveryDate = source.DeliveryDate,

            OrderStatusFlag = source.OrderStatusFlag,

            OrderBranch = source.OrderBranch,
            ServiceBranch = source.ServiceBranch,
            DeliveryBranch = source.DeliveryBranch,

            OrderTransferDate = source.OrderTransferDate,

            BaseMaterial = source.BaseMaterial,

            TotalGrossWeight = source.TotalGrossWeight,
            TotalStoneWeight = source.TotalStoneWeight,
            TotalNetWeight = source.TotalNetWeight,

            OrderedItems = source.OrderedItems,
            FulfilledItems = source.FulfilledItems,

            OldMetalNetWeight = source.OldMetalNetWeight,
            OldMetalFineWeight = source.OldMetalFineWeight,

            BalanceWeight = source.BalanceWeight,
            MetalRate = source.MetalRate,

            TotalMakingCharges = source.TotalMakingCharges,
            TotalTaxAmount = source.TotalTaxAmount,
            TotalOrderAmount = source.TotalOrderAmount,

            AdvancePaidAmount = source.AdvancePaidAmount,
            BalanceAmount = source.BalanceAmount,

            Remark = source.Remark,

            CreatedBy = source.CreatedBy,
            ModifiedBy = source.ModifiedBy,

            TenantGkey = source.TenantGkey,
            OrderRefNbr = source.OrderRefNbr
        };
    }

    private static CustomerOrderLineSaveModel MapLine(
    CustomerOrderLine source)
    {
        return new CustomerOrderLineSaveModel
        {
            Gkey = source.GKey,

            OrderGkey = source.OrderGkey,
            OrderLineNbr = source.OrderLineNbr,

            ProdCategory = source.ProdCategory,
            ProductGkey = source.ProductGkey,
            ProductId = source.ProductId,
            ProductSku = source.ProductSku,
            ProductName = source.ProductName,
            ProductDesc = source.ProductDesc,

            ProductMetal = source.ProductMetal,
            ProductPurity = source.ProductPurity,

            OrderSpecification = source.OrderSpecification,

            ProdQty = source.ProdQty,

            ProdGrossWeight = source.ProdGrossWeight,
            ProdStoneWeight = source.ProdStoneWeight,
            ProdNetWeight = source.ProdNetWeight,

            OrderType = source.OrderType,
            ItemNotes = source.ItemNotes,
            ItemPacked = source.ItemPacked,

            OrderItemDueDate = source.OrderItemDueDate,
            DeliveryDate = source.DeliveryDate,

            OrderItemStatusFlag =
                source.OrderItemStatusFlag,

            OrderBranch = source.OrderBranch,
            ServiceBranch = source.ServiceBranch,
            DeliveryBranch = source.DeliveryBranch,

            OrderTransferDate =
                source.OrderTransferDate,

            MetalRate = source.MetalRate,
            MakingCharges = source.MakingCharges,

            VaPercent = source.VaPercent,
            VaAmount = source.VaAmount,

            TaxAmount = source.TaxAmount,
            OrderAmount = source.OrderAmount,

            AdvancePaidAmount =
                source.AdvancePaidAmount,

            BalanceAmount = source.BalanceAmount,

            Remark = source.Remark,

            CatalogId = source.CatalogId,
            DesignName = source.DesignName,
            PageNbr = source.PageNbr,
            ImageName = source.ImageName,
            ImagePath = source.ImagePath,

            CreatedBy = source.CreatedBy,
            ModifiedBy = source.ModifiedBy,

            TenantGkey = source.TenantGkey
        };
    }

    private static OldMetalTransactionSaveModel MapOldMetal(
    OldMetalTransaction source)
    {
        return new OldMetalTransactionSaveModel
        {
            Gkey = source.GKey,

            TransNbr = source.TransNbr,
            TransDate = source.TransDate,

            // IMPORTANT:
            // this must be the configured VoucherType document name
            TransType = source.TransType,

            CustGkey = source.CustGkey,
            CustMobile = source.CustMobile,

            ProductGkey = source.ProductGkey,
            ProductId = source.ProductId,
            ProductCategory = source.ProductCategory,

            Metal = source.Metal,
            Purity = source.Purity,

            TransactedRate = source.TransactedRate,
            Uom = source.Uom,

            GrossWeight = source.GrossWeight,
            StoneWeight = source.StoneWeight,

            WastagePercent = source.WastagePercent,
            WastageWeight = source.WastageWeight,

            NetWeight = source.NetWeight,

            TotalProposedPrice =
                source.TotalProposedPrice,

            FinalPurchasePrice =
                source.FinalPurchasePrice,

            Remarks = source.Remarks
        };
    }

    private static CustomerOrderReceiptSaveModel MapReceipt(
        LedgersTransactions source)
    {
        return new CustomerOrderReceiptSaveModel
        {
            Gkey = 0,

            TransType = "Receipt",

            VoucherType = "Advance Receipt",

            // Cash / UPI / Bank etc.
            Mode = source.TransType,

            TransAmount = source.TransactionAmount
        };
    }

    private static void PrepareAuditFields(
    BaseEntity entity,
    bool isNew)
    {
        var now = DateTime.Now;

        if (isNew)
        {
            entity.CreatedBy ??= "System";
            entity.CreatedOn ??= now;
        }

        entity.ModifiedBy = "System";
        entity.ModifiedOn = now;
    }


}