using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class CustomerOrderDbView
{
    public int? CustGkey { get; set; }

    public string? CustomerName { get; set; }

    public string? CustMobileNbr { get; set; }

    public string? OrderNbr { get; set; }

    public DateTime? OrderDate { get; set; }

    public string? OrderType { get; set; }

    public DateTime? OrderDueDate { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public int? OrderStatusFlag { get; set; }

    public string? OrderStatus { get; set; }

    public int? OrderBranch { get; set; }

    public decimal? AdvancePaidAmount { get; set; }

    public decimal? BalanceAmount { get; set; }

    public int? OrderLineNbr { get; set; }

    public string? ProdCategory { get; set; }

    public decimal? ProdGrossWeight { get; set; }

    public decimal? ProdStoneWeight { get; set; }

    public decimal? ProdNetWeight { get; set; }

    public int ProdQty { get; set; }
}
