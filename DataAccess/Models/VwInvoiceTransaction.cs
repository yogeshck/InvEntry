using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class VwInvoiceTransaction
{
    public string? InvNbr { get; set; }

    public DateTime? InvDate { get; set; }

    public decimal? InvAmount { get; set; }

    public string? TransactionMode { get; set; }

    public decimal? Amount { get; set; }
}
