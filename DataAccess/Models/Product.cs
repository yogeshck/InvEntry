using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class Product
{
    public int Gkey { get; set; }

    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Category { get; set; }

    public string? Purity { get; set; }

    public decimal? PurityPercent { get; set; }

    public string? ImageRef { get; set; }

    public int? SetGkey { get; set; }

    public string? StockGroup { get; set; }

    public string? Brand { get; set; }

    public string? Metal { get; set; }

    public string? HsnCode { get; set; }

    public string? Model { get; set; }

    public string? Uom { get; set; }

    public string? TaxRule { get; set; }

    public string? BaseUnit { get; set; }

    public bool? IsTaxable { get; set; }

    public bool? IsActive { get; set; }

    public int? ProductAttributeGkey { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }
}
