using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class Voucher
{
    public int Gkey { get; set; }

    public int? SeqNbr { get; set; }

    public int? CustomerGkey { get; set; }

    public string? TransType { get; set; }

    public string? VoucherType { get; set; }

    public string? Mode { get; set; }

    public decimal? TransAmount { get; set; }

    public string? VoucherNbr { get; set; }

    public DateTime? VoucherDate { get; set; }

    public int? RefDocGkey { get; set; }

    public string? RefDocNbr { get; set; }

    public DateTime? RefDocDate { get; set; }

    public string? TransDesc { get; set; }

    public DateTime? TransDate { get; set; }

    public int? FromLedgerGkey { get; set; }

    public int? ToKedgerGkey { get; set; }

    public decimal? ObAmount { get; set; }

    public decimal? CbAmount { get; set; }

    public int? FundTransferMode { get; set; }

    public int? FundTransferRefGkey { get; set; }

    public DateTime? FundTransferDate { get; set; }
}
