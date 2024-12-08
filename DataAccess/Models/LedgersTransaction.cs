using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class LedgersTransaction
{
    public int Gkey { get; set; }

    public int LedgerHdrGkey { get; set; }

    public string? DrCr { get; set; }

    public DateTime? TransactionDate { get; set; }

    public decimal? TransactionAmount { get; set; }

    public bool? Status { get; set; }

    public string? DocumentNbr { get; set; }

    public DateTime? DocumentDate { get; set; }
}
