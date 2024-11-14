using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class GrnLine
{
    public int? Gkey { get; set; }

    public int? GrnHdrGkey { get; set; }

    public int? LineNbr { get; set; }

    public string? ProductId { get; set; }

    public string? ProductDesc { get; set; }

    public int ProductGkey { get; set; }

    public string? ProductPurity { get; set; }

    public string? SuppProductNbr { get; set; }

    public string? SuppProductDesc { get; set; }

    public decimal? GrossWeight { get; set; }

    public decimal? StoneWeight { get; set; }

    public decimal? NetWeight { get; set; }

    public string? Uom { get; set; }

    public int? OrderedQty { get; set; }

    public int? SuppliedQty { get; set; }

    public int? ReceivedQty { get; set; }

    public int? AcceptedQty { get; set; }

    public int? RejectedQty { get; set; }

    public int? ReturnedQty { get; set; }

    public string? Status { get; set; }

    public string? Notes { get; set; }

    public decimal? SuppVaPercent { get; set; }

    public decimal? SuppWastagePercent { get; set; }

    public decimal? SuppWastageAmount { get; set; }

    public decimal? SuppRate { get; set; }

    public decimal? SuppMakingCharges { get; set; }

    public int? SizeId { get; set; }

    public string? Size { get; set; }

    public string? SizeUom { get; set; }
}
