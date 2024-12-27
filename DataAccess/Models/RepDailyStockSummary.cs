using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class RepDailyStockSummary
{
    public DateTime? TransactionDate { get; set; }

    public string? ProductCategory { get; set; }

    public decimal? OpeningStock { get; set; }

    public decimal? StockIn { get; set; }

    public decimal? StockTransferIn { get; set; }

    public decimal? StockOut { get; set; }

    public decimal? StockTransferOut { get; set; }

    public decimal? ClosingStock { get; set; }

    public string? Metal { get; set; }

    public int Gkey { get; set; }
}
