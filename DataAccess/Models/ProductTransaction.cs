using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class ProductTransaction
{
    public int Gkey { get; set; }

    public double? RefGkey { get; set; }

    public DateTime? TransDate { get; set; }

    public string? DocRefNbr { get; set; }

    public string? DocType { get; set; }

    public int ProductRefGkey { get; set; }

    public decimal? Column1 { get; set; }

    public decimal? TransQty { get; set; }

    public decimal? CbQty { get; set; }

    public decimal? UnitTransPrice { get; set; }

    public decimal? TransactionValue { get; set; }

    public string? TransNote { get; set; }
}
