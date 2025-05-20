using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class CustomerOrderLine
{
    public int Gkey { get; set; }

    public string? OrderNbr { get; set; }

    public int? OrderGkey { get; set; }

    public int? OrderLineNbr { get; set; }

    public string? ProdCategory { get; set; }

    public int? ProductGkey { get; set; }

    public string? ProductId { get; set; }

    public string? ProductSku { get; set; }

    public string? ProductName { get; set; }

    public string? ProductDesc { get; set; }

    public string? ProductMetal { get; set; }

    public string? ProductPurity { get; set; }

    public string? OrderSpecification { get; set; }

    public int ProdQty { get; set; }

    public decimal? ProdGrossWeight { get; set; }

    public decimal? ProdStoneWeight { get; set; }

    public decimal? ProdNetWeight { get; set; }

    public string? OrderType { get; set; }

    public string? ItemNotes { get; set; }

    public bool? ItemPacked { get; set; }

    public DateTime? OrderItemDueDate { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public int? OrderItemStatusFlag { get; set; }

    public int? OrderBranch { get; set; }

    public int? ServiceBranch { get; set; }

    public int? DeliveryBranch { get; set; }

    public DateTime? OrderTransferDate { get; set; }

    public decimal? TotalGrossWeight { get; set; }

    public decimal? TotalStoneWeight { get; set; }

    public decimal? TotalNetWeight { get; set; }

    public int? OrderedItems { get; set; }

    public int? FulfilledItems { get; set; }

    public decimal? OldMetalNetWeight { get; set; }

    public decimal? OldMetalFinePercent { get; set; }

    public decimal? OldMetalFineWeight { get; set; }

    public decimal? BalanceWeight { get; set; }

    public decimal? MetalRate { get; set; }

    public decimal? MakingCharges { get; set; }

    public decimal? VaPercent { get; set; }

    public decimal? VaAmount { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal? OrderAmount { get; set; }

    public decimal? AdvancePaidAmount { get; set; }

    public decimal? BalanceAmount { get; set; }

    public string? Remark { get; set; }

    public int? CatalogId { get; set; }

    public string? DesignName { get; set; }

    public int? PageNbr { get; set; }

    public string? ImageName { get; set; }

    public string? ImagePath { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public int? TenantGkey { get; set; }
}
