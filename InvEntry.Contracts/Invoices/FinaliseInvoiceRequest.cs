namespace InvEntry.Contracts.Invoices;

public class FinaliseInvoiceRequest
{
    public int InvoiceGkey { get; set; }

    /// <summary>
    /// Actual amounts received from the customer.
    /// </summary>
    public List<InvoiceSettlementSaveModel> Receipts { get; set; }
        = new();

    /// <summary>
    /// Actual amounts paid/refunded to the customer.
    /// </summary>
    public List<InvoiceSettlementSaveModel> Refunds { get; set; }
        = new();

    /// <summary>
    /// Remaining invoice amount retained as customer receivable.
    /// This is not a cash/bank receipt.
    /// </summary>
    public decimal CreditAmount { get; set; }

    public decimal DiscountAmount { get; set; }

}