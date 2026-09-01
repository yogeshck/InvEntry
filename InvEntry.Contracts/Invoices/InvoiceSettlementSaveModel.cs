namespace InvEntry.Contracts.Invoices;

public class InvoiceSettlementSaveModel
{
    /// <summary>
    /// RECEIPT = Customer pays shop.
    /// REFUND  = Shop pays customer.
    /// </summary>
    public string SettlementType { get; set; } = string.Empty;

    /// <summary>
    /// CASH, UPI, CARD, NEFT, IMPS, RTGS, CHEQUE, DD, etc.
    /// </summary>
    public string PaymentMode { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    /// <summary>
    /// UPI transaction ID, UTR, card transaction ID, etc.
    /// </summary>
    public string? TransactionId { get; set; }

    public DateTime? TransactionDate { get; set; }

    /// <summary>
    /// Cheque number, DD number, etc.
    /// </summary>
    public string? InstrumentNumber { get; set; }

    /// <summary>
    /// Cheque date, DD date, etc.
    /// </summary>
    public DateTime? InstrumentDate { get; set; }

    public string? BankName { get; set; }

    public string? CompanyBankAccountNbr { get; set; }

    public string? OtherReference { get; set; }
}