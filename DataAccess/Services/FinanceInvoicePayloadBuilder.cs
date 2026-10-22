using DataAccess.Models;
using DataAccess.Models.FinanceIntegration;
using InvEntry.Contracts.Invoices;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public sealed class FinanceInvoicePayloadBuilder
{
    private readonly MijmsContext _context;

    public FinanceInvoicePayloadBuilder(MijmsContext context)
    {
        _context = context;
    }

    public async Task<FinanceDocumentPayload> BuildAsync(
        int invoiceGkey,
        int orgGkey,
        int locationGkey,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _context.InvoiceHeaders
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Gkey == invoiceGkey,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Invoice {invoiceGkey} was not found.");

        if (!InvoiceStatus.IsFinalised(invoice.Status?.Trim()))
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.InvNbr} is not FINALISED.");
        }

        var vouchers = await _context.Vouchers
            .AsNoTracking()
            .Where(x => x.RefDocGkey == invoiceGkey)
            .OrderBy(x => x.SeqNbr)
            .ThenBy(x => x.Gkey)
            .ToListAsync(cancellationToken);

        var payload = new FinanceDocumentPayload
        {
            OrgGkey = orgGkey,
            LocationGkey = locationGkey,

            SourceSystem = "INVENTRY",
            SourceEventId = $"INVOICE-{invoice.Gkey}-FINAL",

            DocumentType = "INVOICE",
            DocumentNo = invoice.InvNbr,
            DocumentDate = DateOnly.FromDateTime(
                                invoice.InvDate
                                    ?? throw new InvalidOperationException(
                                        $"Invoice {invoice.InvNbr} has no invoice date.")),

            DocumentAmount = invoice.AmountPayable
                                    ?? throw new InvalidOperationException(
                                        $"Invoice {invoice.InvNbr} has no amount payable."),

            SourceDocumentType = "INVOICE",
            SourceDocumentNo = invoice.InvNbr,

            Description = $"InvEntry Invoice {invoice.InvNbr}"
        };

        foreach (var voucher in vouchers)
        {
            var movement = MapVoucher(voucher);

            if (movement != null)
                payload.Movements.Add(movement);
        }

        if (payload.Movements.Any(x => x.Amount <= 0))
            throw new InvalidOperationException(
                $"Invoice {invoice.InvNbr} contains an eligible settlement voucher with a non-positive amount.");

        if (payload.Movements.Any(x => string.IsNullOrWhiteSpace(x.SourceReference)))
            throw new InvalidOperationException(
                $"Invoice {invoice.InvNbr} contains an eligible settlement voucher without a source reference.");

        return payload;
    }

    public const string CollectionDocumentType = "COLLECTION";

    /// <summary>
    /// A receipt voucher entered against a finalised invoice after finalisation
    /// (outstanding-invoice adjustment, cash receipt against an invoice). Posted
    /// on its own, on the day it was received, by usp_SyncInvEntryCollection.
    /// Returns null when the voucher is not such a collection.
    /// </summary>
    public async Task<FinanceDocumentPayload?> BuildCollectionAsync(
        int voucherGkey,
        int orgGkey,
        int locationGkey,
        CancellationToken cancellationToken = default)
    {
        var voucher = await _context.Vouchers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Gkey == voucherGkey, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Voucher {voucherGkey} was not found.");

        if (!EqualsIgnoreCase(voucher.TransType?.Trim(), "Receipt") ||
            voucher.RefDocGkey is null ||
            string.IsNullOrWhiteSpace(voucher.RefDocNbr))
        {
            return null;
        }

        // Invoice and estimate gkeys overlap, so match the number as well.
        var invoiceNumber = voucher.RefDocNbr.Trim();
        var invoice = await _context.InvoiceHeaders
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Gkey == voucher.RefDocGkey && x.InvNbr == invoiceNumber,
                cancellationToken);

        if (invoice is null || !InvoiceStatus.IsFinalised(invoice.Status?.Trim()))
            return null;

        var movement = MapCollection(voucher);
        if (movement is null)
            return null;

        var collectedOn = voucher.TransDate ?? voucher.VoucherDate
            ?? throw new InvalidOperationException(
                $"Voucher {voucher.VoucherNbr} has no date.");

        return new FinanceDocumentPayload
        {
            OrgGkey = orgGkey,
            LocationGkey = locationGkey,

            SourceSystem = "INVENTRY",
            SourceEventId = FinanceSyncService.CollectionEventId(voucher.Gkey),

            DocumentType = CollectionDocumentType,
            DocumentNo = string.IsNullOrWhiteSpace(voucher.VoucherNbr)
                ? $"V-{voucher.Gkey}"
                : voucher.VoucherNbr.Trim(),
            DocumentDate = DateOnly.FromDateTime(collectedOn),
            DocumentAmount = movement.Amount,

            SourceDocumentType = "INVOICE",
            SourceDocumentNo = invoice.InvNbr,

            Description = $"InvEntry collection on invoice {invoice.InvNbr}",
            Movements = [movement]
        };
    }

    private static FinanceMovementPayload? MapCollection(Voucher voucher)
    {
        var voucherType = voucher.VoucherType?.Trim();
        var mode = voucher.Mode?.Trim();
        var amount = voucher.TransAmount ?? 0M;

        // Discount allowed when the balance is collected: a write-off, not money.
        // Checked first, because some older discount vouchers carry Mode = Cash.
        string? paymentGroup =
            EqualsIgnoreCase(voucherType, "Discount") ||
            EqualsIgnoreCase(voucherType, "CR DISCOUNT") ||
            EqualsIgnoreCase(mode, "Discount")
                ? "WRITE_OFF"
                : EqualsIgnoreCase(mode, "Cash")
                    ? "CASH"
                    : IsNonCashReceiptMode(mode) ? "NON_CASH" : null;

        if (paymentGroup is null || amount <= 0)
            return null;

        return new FinanceMovementPayload
        {
            Direction = "IN",
            PaymentGroup = paymentGroup,
            Amount = amount,
            Description = voucher.TransDesc,
            SourceReference = voucher.Gkey.ToString()
        };
    }

    private static FinanceMovementPayload? MapVoucher(Voucher voucher)
    {
        var transType = voucher.TransType?.Trim();
        var voucherType = voucher.VoucherType?.Trim();
        var mode = voucher.Mode?.Trim();

        // CASH RECEIVED
        if (EqualsIgnoreCase(transType, "Receipt") &&
            EqualsIgnoreCase(mode, "Cash"))
        {
            return new FinanceMovementPayload
            {
                Direction = "IN",
                PaymentGroup = "CASH",
                Amount = voucher.TransAmount
                                ?? throw new InvalidOperationException(
                                    $"Voucher {voucher.Gkey} has no transaction amount."),
                Description = voucher.TransDesc,
                SourceReference = voucher.Gkey.ToString()
            };
        }

        // CASH REFUND TO CUSTOMER
        if (EqualsIgnoreCase(transType, "Payment") &&
            EqualsIgnoreCase(voucherType, "Refund") &&
            EqualsIgnoreCase(mode, "Cash"))
        {
            return new FinanceMovementPayload
            {
                Direction = "OUT",
                PaymentGroup = "CASH",
                Amount = voucher.TransAmount
                                ?? throw new InvalidOperationException(
                                    $"Voucher {voucher.Gkey} has no transaction amount."),
                Description = voucher.TransDesc,
                SourceReference = voucher.Gkey.ToString()
            };
        }

        // OLD METAL USED TO SETTLE THE INVOICE
        if (EqualsIgnoreCase(transType, "Journal") &&
            EqualsIgnoreCase(voucherType, "OM Purchase"))
        {
            return new FinanceMovementPayload
            {
                Direction = "IN",
                PaymentGroup = "OTHERS",
                Amount = voucher.TransAmount
                                ?? throw new InvalidOperationException(
                                    $"Voucher {voucher.Gkey} has no transaction amount."),
                Description = voucher.TransDesc,
                SourceReference = voucher.Gkey.ToString()
            };
        }

        // NON-CASH RECEIPTS. These must match the modes FinanceTracker
        // accepts, otherwise the receipt is skipped on the FinanceTracker side.
        if (EqualsIgnoreCase(transType, "Receipt") &&
            IsNonCashReceiptMode(mode))
        {
            return new FinanceMovementPayload
            {
                Direction = "IN",
                PaymentGroup = "NON_CASH",
                Amount = voucher.TransAmount
                                ?? throw new InvalidOperationException(
                                    $"Voucher {voucher.Gkey} has no transaction amount."),
                Description = voucher.TransDesc,
                SourceReference = voucher.Gkey.ToString()
            };
        }

        // NON-CASH REFUND
        if (EqualsIgnoreCase(transType, "Payment") &&
            EqualsIgnoreCase(voucherType, "Refund") &&
            IsNonCashReceiptMode(mode))
        {
            return new FinanceMovementPayload
            {
                Direction = "OUT",
                PaymentGroup = "NON_CASH",
                Amount = voucher.TransAmount
                                ?? throw new InvalidOperationException(
                                    $"Voucher {voucher.Gkey} has no transaction amount."),
                Description = voucher.TransDesc,
                SourceReference = voucher.Gkey.ToString()
            };
        }

        // CREDIT SALE: the receivable left on the invoice (Journal/Credit).
        // It is a sale, not cash, and FinanceTracker posts it as CREDIT_SALE.
        if (EqualsIgnoreCase(transType, "Journal") &&
            EqualsIgnoreCase(voucherType, "Credit"))
        {
            return new FinanceMovementPayload
            {
                Direction = "IN",
                PaymentGroup = "CREDIT",
                Amount = voucher.TransAmount
                                ?? throw new InvalidOperationException(
                                    $"Voucher {voucher.Gkey} has no transaction amount."),
                Description = voucher.TransDesc,
                SourceReference = voucher.Gkey.ToString()
            };
        }

        // Unmapped voucher types are left out of the payload. An invoice whose
        // vouchers are all unmapped still gets an event, which shows as an
        // error on the Synchronization page rather than disappearing.
        return null;
    }

    private static readonly string[] NonCashReceiptModes =
    [
        "UPI", "Bank Scanner", "Bank", "Bank Transfer", "Card", "Credit Card",
        "Debit Card", "Cheque", "GPAY", "PhonePe"
    ];

    private static bool IsNonCashReceiptMode(string? mode) =>
        NonCashReceiptModes.Any(x => EqualsIgnoreCase(mode, x));

    private static bool EqualsIgnoreCase(
        string? value,
        string expected)
    {
        return string.Equals(
            value,
            expected,
            StringComparison.OrdinalIgnoreCase);
    }
}
