using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class ProductStock
{
    public decimal? Gkey { get; set; }

    public byte[]? TenantGkey { get; set; }

    public string? BaseUnit { get; set; }

    public string? Brand { get; set; }

    public decimal? ProductGkey { get; set; }

    public string? HsnCode { get; set; }

    public string? Metal { get; set; }

    public string? Model { get; set; }

    public string? Uom { get; set; }

    public decimal? GrossWeight { get; set; }

    public decimal? NetWeight { get; set; }

    public decimal? OtherWeight { get; set; }

    public string? ProductDesc { get; set; }

    public string? ProductImageRef { get; set; }

    public decimal? Qty { get; set; }

    public string? SetIdGkey { get; set; }

    public string? Status { get; set; }

    public decimal? StockId { get; set; }

    public decimal? SupplierId { get; set; }

    public decimal? TaxRule { get; set; }

    public string? Taxable { get; set; }

    public short? ActiveForSale { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public decimal? DeletedFlag { get; set; }

    public decimal? PurchaseRef { get; set; }

    public string? ProductId { get; set; }

    public int? VaPercent { get; set; }
}
