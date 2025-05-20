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

    public int? OpeningStockQty { get; set; }

    public int? StockInQty { get; set; }

    public int? StockTransferInQty { get; set; }

    public int? StockOutQty { get; set; }

    public int? StockTransferOutQty { get; set; }

    public int? ClosingStockQty { get; set; }
}
