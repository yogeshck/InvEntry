using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class SupplierMetalTransaction
{
    public long Gkey { get; set; }

    public int SupplierGkey { get; set; }

    public string? SupplierShortname { get; set; }

    public DateTime TransactionDate { get; set; }

    public int TransactionType { get; set; }

    public string Metal { get; set; } = null!;

    public decimal? MetalFineness { get; set; }

    public decimal? PurityPercent { get; set; }

    public string? Karat { get; set; }

    public decimal? GrossWeight { get; set; }

    public decimal? StoneWeight { get; set; }

    public decimal? NetWeight { get; set; }

    public decimal? TestMetalFineness { get; set; }

    public decimal? TestPurityPercent { get; set; }

    public decimal? PricePerGram { get; set; }

    public decimal? TransactionAmount { get; set; }

    public decimal? PureWeight { get; set; }

    public string? ReferenceNbr { get; set; }

    public string? Remarks { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public string? ModifiedBy { get; set; }
}
