using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class StockAdjustmentHeader
{
    public int Gkey { get; set; }
    public string AdjustmentNbr { get; set; } = null!;
    public DateTime AdjustmentDate { get; set; }
    public string AdjustmentType { get; set; } = null!;
    public string ReasonCode { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Remarks { get; set; }

    // Movement magnitude. For REALLOCATION this is the shared transfer amount,
    // not OUT + IN added together.
    public int TotalQty { get; set; }
    public decimal TotalGrossWeight { get; set; }
    public decimal TotalStoneWeight { get; set; }
    public decimal TotalNetWeight { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public DateTime? FinalisedOn { get; set; }

    public virtual ICollection<StockAdjustmentLine> StockAdjustmentLines { get; set; }
        = new List<StockAdjustmentLine>();
}
