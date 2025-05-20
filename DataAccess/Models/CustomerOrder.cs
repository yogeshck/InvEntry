using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class CustomerOrder
{
    public int Gkey { get; set; }

    public int? CustGkey { get; set; }

    public string? CustMobileNbr { get; set; }

    public string? OrderNbr { get; set; }

    public DateTime? OrderDate { get; set; }

    public string? OrderType { get; set; }

    public DateTime? OrderDueDate { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public int? OrderStatusFlag { get; set; }

    public int? OrderBranch { get; set; }

    public int? ServiceBranch { get; set; }

    public int? DeliveryBranch { get; set; }

    public DateTime? OrderTransferDate { get; set; }

    public string? BaseMaterial { get; set; }

    public decimal? TotalGrossWeight { get; set; }

    public decimal? TotalStoneWeight { get; set; }

    public decimal? TotalNetWeight { get; set; }

    public int? OrderedItems { get; set; }

    public int? FulfilledItems { get; set; }

    public decimal? OldMetalNetWeight { get; set; }

    public decimal? OldMetalFineWeight { get; set; }

    public decimal? BalanceWeight { get; set; }

    public decimal? MetalRate { get; set; }

    public decimal? TotalMakingCharges { get; set; }

    public decimal? TotalTaxAmount { get; set; }

    public decimal? TotalOrderAmount { get; set; }

    public decimal? AdvancePaidAmount { get; set; }

    public decimal? BalanceAmount { get; set; }

    public string? Remark { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public int? TenantGkey { get; set; }
}
