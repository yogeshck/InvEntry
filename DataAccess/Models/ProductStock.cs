using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class ProductStock
{
    public int Gkey { get; set; }

    public int? StockSummaryGkey { get; set; }

    public int? ProductGkey { get; set; }

    public string? ProductSku { get; set; }

    public decimal? GrossWeight { get; set; }

    public decimal? StoneWeight { get; set; }

    public decimal? NetWeight { get; set; }

    public decimal? SuppliedGrossWeight { get; set; }

    public decimal? AdjustedWeight { get; set; }

    public decimal? SoldWeight { get; set; }

    public decimal? BalanceWeight { get; set; }

    public int? SuppliedQty { get; set; }

    public int? SoldQty { get; set; }

    public int? StockQty { get; set; }

    public string? Status { get; set; }

    public string? SupplierId { get; set; }

    public decimal? VaPercent { get; set; }

    public decimal? WastagePercent { get; set; }

    public decimal? WastageAmount { get; set; }

    public int? SizeId { get; set; }

    public string? Size { get; set; }

    public string? SizeUom { get; set; }

    public bool? IsProductSold { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public string? Category { get; set; }
}
