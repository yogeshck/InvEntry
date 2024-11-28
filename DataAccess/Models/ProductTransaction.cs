using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class ProductTransaction
{
    public int Gkey { get; set; }

    public int? RefGkey { get; set; }

    public DateTime? TransactionDate { get; set; }

    public string? ProductCategory { get; set; }

    public string? ProductSku { get; set; }

    public string? TransactionType { get; set; }

    public string? DocumentNbr { get; set; }

    public string? DocumentType { get; set; }

    public string? VoucherType { get; set; }

    public int? ObQty { get; set; }

    public int? TransactionQty { get; set; }

    public int? CbQty { get; set; }

    public decimal? UnitPrice { get; set; }

    public decimal? TransactionValue { get; set; }

    public string? Notes { get; set; }

    public DateTime? DocumentDate { get; set; }

    public decimal? OpeningGrossWeight { get; set; }

    public decimal? OpeningStoneWeight { get; set; }

    public decimal? OpeningNetWeight { get; set; }

    public decimal? TransactionGrossWeight { get; set; }

    public decimal? TransactionStoneWeight { get; set; }

    public decimal? TransactionNetWeight { get; set; }

    public decimal? ClosingGrossWeight { get; set; }

    public decimal? ClosingStoneWeight { get; set; }

    public decimal? ClosingNetWeight { get; set; }
}
