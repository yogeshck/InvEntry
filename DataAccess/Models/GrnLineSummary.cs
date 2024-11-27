using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class GrnLineSummary
{
    public int Gkey { get; set; }

    public int? GrnHdrGkey { get; set; }

    public int? LineNbr { get; set; }

    public int? ProductGkey { get; set; }

    public decimal? GrossWeight { get; set; }

    public decimal? StoneWeight { get; set; }

    public decimal? NetWeight { get; set; }

    public int? SuppliedQty { get; set; }

    public string? ProductCategory { get; set; }

    public string? ProductPurity { get; set; }

    public string? Uom { get; set; }
}
