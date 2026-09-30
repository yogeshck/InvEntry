namespace DataAccess.Services;

public static class HistoricalInvoiceCompletionClassifier
{
    public const string CompletionCandidate = "Completion candidate";
    public const string LikelyGenuineDraft = "Likely genuine draft";
    public const string NeedsInvestigation = "Needs investigation";

    public static HistoricalInvoiceCompletionClassification Classify(
        HistoricalInvoiceCompletionEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        var hasInvoiceNumber = !string.IsNullOrWhiteSpace(evidence.InvoiceNbr);
        var hasLines = evidence.InvoiceLineCount > 0;
        var hasCustomer = evidence.CustomerExists;
        var hasPlaceOfSupply = !string.IsNullOrWhiteSpace(evidence.PlaceOfSupply);
        var hasVouchers = evidence.LinkedVoucherCount > 0;
        var hasReceipts = evidence.LinkedArReceiptCount > 0;

        if (hasInvoiceNumber && hasLines && hasCustomer && hasPlaceOfSupply && hasVouchers && hasReceipts)
        {
            return new HistoricalInvoiceCompletionClassification(
                CompletionCandidate,
                [
                    "Official invoice number is present.",
                    $"{evidence.InvoiceLineCount} invoice line(s) are linked.",
                    "The linked customer exists.",
                    "Place of Supply is present.",
                    $"{evidence.LinkedVoucherCount} voucher(s) are linked.",
                    $"{evidence.LinkedArReceiptCount} AR receipt(s) are linked."
                ]);
        }

        if (!hasInvoiceNumber && !hasVouchers && !hasReceipts)
        {
            return new HistoricalInvoiceCompletionClassification(
                LikelyGenuineDraft,
                [
                    "Official invoice number is missing.",
                    "No linked vouchers were found.",
                    "No linked AR receipts were found."
                ]);
        }

        var reasons = new List<string>();
        AddReason(reasons, hasInvoiceNumber, "Official invoice number is present.", "Official invoice number is missing.");
        AddReason(reasons, hasLines, $"{evidence.InvoiceLineCount} invoice line(s) are linked.", "No invoice lines are linked.");
        AddReason(reasons, hasCustomer, "The linked customer exists.", "The customer is missing or does not resolve.");
        AddReason(reasons, hasPlaceOfSupply, "Place of Supply is present.", "Place of Supply is missing.");
        AddReason(reasons, hasVouchers, $"{evidence.LinkedVoucherCount} voucher(s) are linked.", "No linked vouchers were found.");
        AddReason(reasons, hasReceipts, $"{evidence.LinkedArReceiptCount} AR receipt(s) are linked.", "No linked AR receipts were found.");

        return new HistoricalInvoiceCompletionClassification(NeedsInvestigation, reasons);
    }

    private static void AddReason(List<string> reasons, bool condition, string yes, string no) =>
        reasons.Add(condition ? yes : no);
}

public sealed record HistoricalInvoiceCompletionEvidence(
    string? InvoiceNbr,
    int InvoiceLineCount,
    bool CustomerExists,
    string? PlaceOfSupply,
    int LinkedVoucherCount,
    int LinkedArReceiptCount);

public sealed record HistoricalInvoiceCompletionClassification(
    string Classification,
    IReadOnlyList<string> Reasons);
