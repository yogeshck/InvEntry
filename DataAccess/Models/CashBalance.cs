using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class CashBalance
{
    public int Gkey { get; set; }

    public string? Book { get; set; }

    public DateTime? Date { get; set; }

    public decimal? OpeningBalance { get; set; }

    public decimal? ClosingBalance { get; set; }

    public string? Status { get; set; }
}
