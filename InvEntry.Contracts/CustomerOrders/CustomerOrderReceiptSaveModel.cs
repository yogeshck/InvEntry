namespace InvEntry.Contracts.CustomerOrders;

public sealed class CustomerOrderReceiptSaveModel
{
    public int Gkey { get; set; }

    public string? TransType { get; set; }

    public string? VoucherType { get; set; }

    public string? Mode { get; set; }

    public decimal? TransAmount { get; set; }

    public DateTime? VoucherDate { get; set; }

    public string? TransDesc { get; set; }

    public DateTime? TransDate { get; set; }

    public int? FromLedgerGkey { get; set; }

    public int? ToLedgerGkey { get; set; }

    public int? FundTransferMode { get; set; }

    public int? FundTransferRefGkey { get; set; }

    public DateTime? FundTransferDate { get; set; }
}