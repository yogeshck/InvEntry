using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class OldMetalTransaction
{
    public int Gkey { get; set; }

    public string? TransNbr { get; set; }

    public DateTime? TransDate { get; set; }

    public string? TransType { get; set; }

    public int? DocRefGkey { get; set; }

    public string? DocRefNbr { get; set; }

    public DateTime? DocRefDate { get; set; }

    public int? CustGkey { get; set; }

    public string? CustMobile { get; set; }

    public int? ProductGkey { get; set; }

    public string? ProductId { get; set; }

    public string? ProductCategory { get; set; }

    public string? Metal { get; set; }

    public string? Purity { get; set; }

    public decimal? TransactedRate { get; set; }

    public string? Uom { get; set; }

    public decimal? GrossWeight { get; set; }

    public decimal? StoneWeight { get; set; }

    public decimal? WastagePercent { get; set; }

    public decimal? WastageWeight { get; set; }

    public decimal? NetWeight { get; set; }

    public decimal? TotalProposedPrice { get; set; }

    public decimal? FinalPurchasePrice { get; set; }

    public string? Remarks { get; set; }

    public string? DocRefType { get; set; }
}
