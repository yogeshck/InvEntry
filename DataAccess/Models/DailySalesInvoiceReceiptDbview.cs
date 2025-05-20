using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class DailySalesInvoiceReceiptDbview
{
    public string? InvNbr { get; set; }

    public DateTime? InvDate { get; set; }

    public decimal? InvAmount { get; set; }

    public decimal? AdvanceAmt { get; set; }

    public decimal? DiscountAmt { get; set; }

    public decimal? Refund { get; set; }

    public decimal? Rd { get; set; }

    public decimal? Cash { get; set; }

    public decimal? Gpay { get; set; }

    public decimal? CreditCard { get; set; }

    public decimal? DebitCard { get; set; }

    public decimal? Bank { get; set; }

    public decimal? Credit { get; set; }
}
