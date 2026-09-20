namespace InvEntry.Contracts.Estimates;

public sealed class SaveEstimateRequest
{
    public EstimateHeaderSaveModel Header { get; set; } = new();
    public List<EstimateLineSaveModel> Lines { get; set; } = [];
}

public sealed class EstimateHeaderSaveModel
{
    public DateTime? EstDate { get; set; }
    public string? PaymentMode { get; set; }
    public int? CustGkey { get; set; }
    public string? CustMobile { get; set; }
    public decimal? EstRefund { get; set; }
    public decimal? EstTaxableAmount { get; set; }
    public bool IsTaxApplicable { get; set; }
    public decimal? GrossRcbAmount { get; set; }
    public decimal? OldGoldAmount { get; set; }
    public decimal? OldSilverAmount { get; set; }
    public string? TaxType { get; set; }
    public string? GstLocSeller { get; set; }
    public string? GstLocBuyer { get; set; }
    public decimal? CgstPercent { get; set; }
    public decimal? CgstAmount { get; set; }
    public decimal? SgstPercent { get; set; }
    public decimal? SgstAmount { get; set; }
    public decimal? IgstPercent { get; set; }
    public decimal? IgstAmount { get; set; }
    public decimal? AmountPayable { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? AdvanceAdj { get; set; }
    public DateTime? PaymentDueDate { get; set; }
    public decimal? RdAmountAdj { get; set; }
    public decimal? EstBalance { get; set; }
    public decimal? RecdAmount { get; set; }
    public decimal? RoundOff { get; set; }
    public string? EstNotes { get; set; }
    public string? DeliveryMethod { get; set; }
    public int? DeliveryRef { get; set; }
    public string? OrderNbr { get; set; }
    public DateTime? OrderDate { get; set; }
    public decimal? EstlTaxTotal { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public int? TenantGkey { get; set; }
}

public sealed class EstimateLineSaveModel
{
    public string? HsnCode { get; set; }
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
    public string? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public int? TenantGkey { get; set; }
    public string? ProductSku { get; set; }
}
