using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class EstimateLine
{
    public int Gkey { get; set; }

    public string? HsnCode { get; set; }

    public int? EstLineNbr { get; set; }

    public decimal? EstlBilledPrice { get; set; }

    public decimal? EstlGrossAmt { get; set; }

    public decimal? EstlMakingCharges { get; set; }

    public decimal? EstlOtherCharges { get; set; }

    public decimal? EstlPayableAmt { get; set; }

    public decimal? EstlStoneAmount { get; set; }

    public decimal? EstlTaxableAmount { get; set; }

    public decimal? EstlWastageAmt { get; set; }

    public string? ProdCategory { get; set; }

    public decimal? ProdGrossWeight { get; set; }

    public decimal? ProdNetWeight { get; set; }

    public int ProdQty { get; set; }

    public decimal? ProdStoneWeight { get; set; }

    public string? ProductDesc { get; set; }

    public int? ProductGkey { get; set; }

    public string? ProductName { get; set; }

    public string? ProdPackCode { get; set; }

    public string? ProductPurity { get; set; }

    public bool? IsTaxable { get; set; }

    public string? ItemNotes { get; set; }

    public bool? ItemPacked { get; set; }

    public decimal? EstlCgstPercent { get; set; }

    public decimal? EstlCgstAmount { get; set; }

    public decimal? EstlIgstPercent { get; set; }

    public decimal? EstlIgstAmount { get; set; }

    public decimal? EstlTotal { get; set; }

    public decimal? EstlSgstAmount { get; set; }

    public decimal? EstlSgstPercent { get; set; }

    public string? ProductId { get; set; }

    public string? Metal { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal? TaxPercent { get; set; }

    public string? TaxType { get; set; }

    public decimal? VaAmount { get; set; }

    public decimal? VaPercent { get; set; }

    public string? EstNote { get; set; }

    public int? EstimateHdrGkey { get; set; }

    public string? EstimateId { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public int? TenantGkey { get; set; }
}
