using DataAccess.Models;
using DataAccess.Models.FinanceIntegration;
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

        if (!string.Equals(
                invoice.Status?.Trim(),
                "FINAL",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.InvNbr} is not FINAL.");
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

        return payload;
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

        // Deliberately ignore unmapped voucher types.
        return null;
    }

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