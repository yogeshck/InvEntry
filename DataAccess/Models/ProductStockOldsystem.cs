using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class ProductStockOldsystem
{
    public int? Id { get; set; }

    public DateTime? Created { get; set; }

    public decimal? Rate { get; set; }

    public string? ProdcuctCode { get; set; }

    public string? ProdName { get; set; }

    public string? ProductType { get; set; }

    public string? Categoryname { get; set; }

    public decimal? EstAmt { get; set; }

    public decimal? GrWtGram { get; set; }

    public decimal? MilliGram { get; set; }

    public decimal? GrossWt { get; set; }

    public decimal? StoneGrWtGram { get; set; }

    public decimal? StoneWtMilliGram { get; set; }

    public decimal? StoneWtNet { get; set; }

    public decimal? StoneAmt { get; set; }

    public decimal? Wastage { get; set; }

    public decimal? Mcharge { get; set; }

    public int? Qty { get; set; }

    public string? StoreId { get; set; }

    public string? Purity { get; set; }
}
