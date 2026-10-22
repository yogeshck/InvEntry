namespace InvEntry.Contracts.Invoices;

/// <summary>
/// Receipts collected later against a finalised credit invoice, saved together:
/// every line gets its voucher and receipt, and the invoice balance is reduced,
/// in one transaction.
/// </summary>
public sealed class SaveInvoiceCollectionRequest
{
    public int InvoiceGkey { get; set; }
    public string InvNbr { get; set; } = string.Empty;
    public DateTime ReceivedOn { get; set; }
    public List<InvoiceCollectionLineModel> Lines { get; set; } = [];
}

public sealed class InvoiceCollectionLineModel
{
    public string PaymentType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    /// <summary>UTR, card slip or cheque number; optional.</summary>
    public string? Reference { get; set; }
}

public sealed class SaveInvoiceCollectionResponse
{
    public string InvNbr { get; set; } = string.Empty;
    public decimal RecdAmount { get; set; }
    public decimal InvBalance { get; set; }
    public List<string> VoucherNumbers { get; set; } = [];
}

public static class InvoiceCollectionRules
{
    // Settled from the customer's advance / RD account or left on credit:
    // those need their own posting, so they are not collections.
    private static readonly string[] NotCollectable = ["Credit", "Advance Adj", "RD Adj"];

    public static bool IsCollectable(string? paymentType) =>
        !string.IsNullOrWhiteSpace(paymentType) &&
        !NotCollectable.Any(x => string.Equals(x, paymentType.Trim(), StringComparison.OrdinalIgnoreCase));

    public static bool IsDiscount(string? paymentType) =>
        string.Equals(paymentType?.Trim(), "Discount", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(paymentType?.Trim(), "CR DISCOUNT", StringComparison.OrdinalIgnoreCase);

    /// <summary>The VOUCHER_TYPES document type numbering the voucher.</summary>
    public static string VoucherType(string paymentType) =>
        IsDiscount(paymentType) ? "Discount" : paymentType.Trim();

    /// <summary>
    /// Same convention as invoice settlement vouchers: the mode is the payment mode itself,
    /// and discounts use "Discount" so finance sync posts them as write-offs, never as money.
    /// </summary>
    public static string VoucherMode(string paymentType) =>
        IsDiscount(paymentType) ? "Discount" : paymentType.Trim();

    public static string ModeOfReceipt(string paymentType) =>
        string.Equals(paymentType.Trim(), "Cash", StringComparison.OrdinalIgnoreCase) ? "Cash" : "Bank";

    /// <summary>Null when the lines can be saved against this balance; otherwise the reason.</summary>
    public static string? Validate(decimal invoiceBalance, IReadOnlyCollection<InvoiceCollectionLineModel> lines)
    {
        if (invoiceBalance <= 0)
            return "This invoice has no balance to collect.";
        if (lines.Count == 0)
            return "Add at least one receipt line.";

        var position = 0;
        foreach (var line in lines)
        {
            position++;
            if (string.IsNullOrWhiteSpace(line.PaymentType))
                return $"Line {position}: choose a payment type.";
            if (!IsCollectable(line.PaymentType))
                return $"Line {position}: {line.PaymentType} cannot be collected here.";
            if (line.Amount <= 0)
                return $"Line {position}: enter an amount greater than zero.";
        }

        var total = lines.Sum(x => x.Amount);
        if (total > invoiceBalance)
            return $"The receipts total {total:N2}, more than the balance of {invoiceBalance:N2}. " +
                   "Reduce an amount; any excess must be taken as an advance or refunded.";

        return null;
    }
}
