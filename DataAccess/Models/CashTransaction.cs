using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class CashTransaction
{
    public int Gkey { get; set; }

    public DateTime? TransactionDate { get; set; }

    public string? LedgerBook { get; set; }

    public int? LedgerKey { get; set; }

    public decimal? Receipts { get; set; }

    public decimal? Payments { get; set; }

    public decimal? OpeningBalance { get; set; }

    public decimal? ClosingBalance { get; set; }
}
