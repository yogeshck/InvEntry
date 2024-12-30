using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class Grndbview
{
    public string? GrnNbr { get; set; }

    public DateTime? GrnDate { get; set; }

    public string? DocumentType { get; set; }

    public string? OrderNbr { get; set; }

    public DateTime? OrderDate { get; set; }

    public string? SupplierId { get; set; }

    public string? SupplierRefNbr { get; set; }

    public DateTime? ItemReceivedDate { get; set; }

    public int Gkey { get; set; }

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
