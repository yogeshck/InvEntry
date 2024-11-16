using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class ProductView
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Category { get; set; }

    public string? Purity { get; set; }

    public string? Metal { get; set; }

    public string? HsnCode { get; set; }

    public string? Uom { get; set; }

    public bool? IsTaxable { get; set; }

    public string? ProductSku { get; set; }

    public decimal? GrossWeight { get; set; }

    public decimal StoneWeight { get; set; }

    public decimal? NetWeight { get; set; }

    public decimal? VaPercent { get; set; }

    public decimal? WastagePercent { get; set; }

    public decimal? WastageAmount { get; set; }

    public decimal? SoldWeight { get; set; }

    public decimal? BalanceWeight { get; set; }

    public int? SoldQty { get; set; }

    public int? StockQty { get; set; }

    public string? Size { get; set; }

    public int? SizeId { get; set; }

    public string? SizeUom { get; set; }

    public bool? IsProductSold { get; set; }
}
