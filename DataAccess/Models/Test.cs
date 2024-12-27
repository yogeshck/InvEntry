using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class Test
{
    public DateTime? TransactionDate { get; set; }

    public string? ProductCategory { get; set; }

    public decimal? OpeningQtyFirstTransaction { get; set; }

    public decimal? StockIn { get; set; }

    public decimal? StockOut { get; set; }

    public decimal? TotalTransactedQty { get; set; }
}
