using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class ProductStock
{
    public int Gkey { get; set; }

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

    public int? StockId { get; set; }

    public int? SupplierId { get; set; }

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

    public string? ProductName { get; set; }

    public string? ProductPurity { get; set; }

    public string? ProductCategory { get; set; }

    public decimal? SupplierRate { get; set; }

    public decimal? MakingCharges { get; set; }

    public string? CustomerOrderRefId { get; set; }

    public string? SizeId { get; set; }

    public int? Size { get; set; }

    public string? SizeUom { get; set; }

    public decimal? WastageAmount { get; set; }

    public decimal? WastagePercent { get; set; }

    public string? ProductSku { get; set; }

    public decimal? OriginalGrossWieght { get; set; }

    public decimal? AdjustedWieght { get; set; }

    public string? DocRef { get; set; }

    public DateOnly? DocDate { get; set; }

    public bool? ProductSold { get; set; }
}
