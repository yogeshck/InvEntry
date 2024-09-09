using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class InvoiceLine
{
    public decimal? Gkey { get; set; }

    public string? HsnCode { get; set; }

    public long? InvLineNbr { get; set; }

    public string? InvNote { get; set; }

    public double? InvlBilledPrice { get; set; }

    public double? InvlGrossAmt { get; set; }

    public double? InvlMakingCharges { get; set; }

    public double? InvlOtherCharges { get; set; }

    public double? InvlPayableAmt { get; set; }

    public double? InvlStoneAmount { get; set; }

    public double? InvlTaxableAmount { get; set; }

    public double? InvlWastageAmt { get; set; }

    public short? IsTaxable { get; set; }

    public string? ItemNotes { get; set; }

    public short? ItemPacked { get; set; }

    public string? ProdCategory { get; set; }

    public double? ProdGrossWeight { get; set; }

    public double? ProdNetWeight { get; set; }

    public long? ProdQty { get; set; }

    public double? ProdStoneWeight { get; set; }

    public string? ProductDesc { get; set; }

    public decimal? ProductGkey { get; set; }

    public string? ProductName { get; set; }

    public string? ProdPackCode { get; set; }

    public string? ProductPurity { get; set; }

    public double? TaxAmount { get; set; }

    public double? TaxPercent { get; set; }

    public string? TaxType { get; set; }

    public double? VaAmount { get; set; }

    public double? VaPercent { get; set; }

    public decimal? InvoiceHdrGkey { get; set; }

    public decimal? InvoiceId { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public byte[]? TenantGkey { get; set; }
}
