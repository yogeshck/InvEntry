using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class DailyStockMovementDbView
{
    public DateOnly? MovementDate { get; set; }

    public string Type { get; set; } = null!;

    public string? ProductCategory { get; set; }

    public string? RefNbr { get; set; }

    public int StockinQty { get; set; }

    public decimal StockinGrossWeight { get; set; }

    public decimal StockinStoneWeight { get; set; }

    public decimal StockinNetWeight { get; set; }

    public int StockoutQty { get; set; }

    public decimal StockoutGrossWeight { get; set; }

    public decimal StockoutStoneWeight { get; set; }

    public decimal StockoutNetWeight { get; set; }
}
