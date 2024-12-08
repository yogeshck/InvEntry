using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class LedgersHeader
{
    public int Gkey { get; set; }

    public int? MtblLedgersGkey { get; set; }

    public int? CustGkey { get; set; }

    public DateTime? BalanceAsOn { get; set; }

    public decimal? CurrentBalance { get; set; }
}
