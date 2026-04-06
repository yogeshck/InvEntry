using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class DailyStockSummary
{
    public int Gkey { get; set; }

    public DateTime? TransactionDate { get; set; }

    public string? Metal { get; set; }

    public string? ProductCategory { get; set; }

    public decimal? OpeningStockGrossWeight { get; set; }

    public decimal? OpeningStockStoneWeight { get; set; }

    public decimal? OpeningStockNetWeight { get; set; }

    public decimal? StockInGrossWeight { get; set; }

    public decimal? StockInStoneWeight { get; set; }

    public decimal? StockInNetWeight { get; set; }

    public decimal? StockOutGrossWeight { get; set; }

    public decimal? StockOutStoneWeight { get; set; }

    public decimal? StockOutNetWeight { get; set; }

    public decimal? ClosingStockGrossWeight { get; set; }

    public decimal? ClosingStockStoneWeight { get; set; }

    public decimal? ClosingStockNetWeight { get; set; }

    public int? OpeningStockQty { get; set; }

    public int? StockInQty { get; set; }

    public int? StockOutQty { get; set; }

    public int? ClosingStockQty { get; set; }
}
