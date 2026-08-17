using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class DailyRepStockSumryMovement
{
    public DateOnly ReportDate { get; set; }

    public int BranchId { get; set; }

    public string? Product { get; set; }

    public string? Metal { get; set; }

    public decimal OpeningQty { get; set; }

    public decimal OpeningWeight { get; set; }

    public decimal InQty { get; set; }

    public decimal InGrossWeight { get; set; }

    public decimal InStoneWeight { get; set; }

    public decimal InNetWeight { get; set; }

    public decimal OutQty { get; set; }

    public decimal OutGrossWeight { get; set; }

    public decimal OutStoneWeight { get; set; }

    public decimal OutNetWeight { get; set; }

    public decimal ClosingQty { get; set; }

    public decimal ClosingWeight { get; set; }
}
