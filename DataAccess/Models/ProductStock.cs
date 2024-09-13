using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class ProductStock
{
    public long? Gkey { get; set; }

    public byte[]? TenantGkey { get; set; }

    public string? BaseUnit { get; set; }

    public string? Brand { get; set; }

    public long? ProductGkey { get; set; }

    public string? HsnCode { get; set; }

    public string? Metal { get; set; }

    public string? Model { get; set; }

    public string? Uom { get; set; }

    public decimal? GrossWeight { get; set; }

    public decimal? NetWeight { get; set; }

    public decimal? OtherWeight { get; set; }

    public string? ProductDesc { get; set; }

    public string? ProductImageRef { get; set; }

    public int? Qty { get; set; }

    public string? SetIdGkey { get; set; }

    public string? Status { get; set; }

    public long? StockId { get; set; }

    public long? SupplierId { get; set; }

    public string? TaxRule { get; set; }

    public bool? Taxable { get; set; }

    public bool? ActiveForSale { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public bool? DeletedFlag { get; set; }

    public string? PurchaseRef { get; set; }

    public string? ProductId { get; set; }

    public decimal? VaPercent { get; set; }
}
