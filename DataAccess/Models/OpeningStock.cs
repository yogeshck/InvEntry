using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class OpeningStock
{
    public DateTime? TransactionDate { get; set; }

    public string? ProductCategory { get; set; }

    public decimal? OpeningGrossWeight { get; set; }
}
