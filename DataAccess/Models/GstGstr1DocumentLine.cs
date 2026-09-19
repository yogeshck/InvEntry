using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class GstGstr1DocumentLine
{
    public long Gkey { get; set; }

    public long GstDocumentGkey { get; set; }

    public int? SourceLineGkey { get; set; }

    public int LineNbr { get; set; }

    public string? HsnCode { get; set; }

    public string? Description { get; set; }

    public decimal Quantity { get; set; }

    public decimal TaxableValue { get; set; }

    public decimal GstRate { get; set; }

    public decimal CgstRate { get; set; }

    public decimal SgstRate { get; set; }

    public decimal IgstRate { get; set; }

    public decimal CgstAmount { get; set; }

    public decimal SgstAmount { get; set; }

    public decimal IgstAmount { get; set; }

    public decimal CessAmount { get; set; }

    public string? Uqc { get; set; }

    public string? Uom { get; set; }

    public decimal? GstQuantity { get; set; }

    public virtual GstGstr1Document GstDocumentGkeyNavigation { get; set; } = null!;
}
