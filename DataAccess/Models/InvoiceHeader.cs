using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class InvoiceHeader
{
    public int Gkey { get; set; }

    public string? InvNbr { get; set; }

    public DateTime? InvDate { get; set; }

    public string? PaymentMode { get; set; }

    public int? CustGkey { get; set; }

    public string? CustMobile { get; set; }

    public decimal? InvRefund { get; set; }

    public decimal? InvTaxableAmount { get; set; }

    public bool IsTaxApplicable { get; set; }

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

    public decimal? RecdAmount { get; set; }

    public decimal? InvBalance { get; set; }

    public decimal? RoundOff { get; set; }

    public string? InvNotes { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public int? TenantGkey { get; set; }

    public string? DeliveryMethod { get; set; }

    public int? DeliveryRef { get; set; }

    public string? OrderNbr { get; set; }

    public DateTime? OrderDate { get; set; }

    public decimal? GrossRcbAmount { get; set; }

    public decimal? InvlTaxTotal { get; set; }
}
