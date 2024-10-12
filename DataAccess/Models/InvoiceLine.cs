using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class InvoiceLine
{
    public int Gkey { get; set; }

    public string? HsnCode { get; set; }

    public int? InvLineNbr { get; set; }

    public string? InvNote { get; set; }

    public decimal? InvlBilledPrice { get; set; }

    public decimal? InvlGrossAmt { get; set; }

    public decimal? InvlMakingCharges { get; set; }

    public decimal? InvlOtherCharges { get; set; }

    public decimal? InvlPayableAmt { get; set; }

    public decimal? InvlStoneAmount { get; set; }

    public decimal? InvlTaxableAmount { get; set; }

    public decimal? InvlWastageAmt { get; set; }

    public bool? IsTaxable { get; set; }

    public string? ItemNotes { get; set; }

    public bool? ItemPacked { get; set; }

    public string? ProdCategory { get; set; }

    public decimal? ProdGrossWeight { get; set; }

    public decimal? ProdNetWeight { get; set; }

    public int ProdQty { get; set; }

    public decimal? ProdStoneWeight { get; set; }

    public string? ProductDesc { get; set; }

    public long? ProductGkey { get; set; }

    public string? ProductName { get; set; }

    public string? ProdPackCode { get; set; }

    public string? ProductPurity { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal? TaxPercent { get; set; }

    public string? TaxType { get; set; }

    public decimal? VaAmount { get; set; }

    public decimal? VaPercent { get; set; }

    public long? InvoiceHdrGkey { get; set; }

    public string? InvoiceId { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public string? TenantGkey { get; set; }

    public decimal? InvlCgstPercent { get; set; }

    public decimal? InvlCgstAmount { get; set; }

    public decimal? InvlIgstPercent { get; set; }

    public decimal? InvlIgstAmount { get; set; }

    public decimal? InvlTotal { get; set; }

    public decimal? InvlSgstAmount { get; set; }

    public decimal? InvlSgstPercent { get; set; }

    public string? ProductId { get; set; }

    public string? Metal { get; set; }
}
