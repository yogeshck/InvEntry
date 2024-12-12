using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class ProductTransactionSummary
{
    public int Gkey { get; set; }

    public DateTime? TransactionDate { get; set; }

    public string? ProductSku { get; set; }

    public string? ProductCategory { get; set; }

    public int? OpeningQty { get; set; }

    public int? StockInQty { get; set; }

    public int? StockOutQty { get; set; }

    public int? ClosingQty { get; set; }

    public decimal? OpeningGrossWeight { get; set; }

    public decimal? OpeningStoneWeight { get; set; }

    public decimal? OpeningNetWeight { get; set; }

    public decimal? StockInGrossWeight { get; set; }

    public decimal? StockInStoneWeight { get; set; }

    public decimal? StockInNetWeight { get; set; }

    public decimal? StockOutGrossWeight { get; set; }

    public decimal? StockOutStoneWeight { get; set; }

    public decimal? StockOutNetWeight { get; set; }

    public decimal? ClosingGrossWeight { get; set; }

    public decimal? ClosingStoneWeight { get; set; }

    public decimal? ClosingNetWeight { get; set; }
}
