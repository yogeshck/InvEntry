using InvEntry.Utils;
using System;

namespace InvEntry.ViewModels.Invoices;

public readonly record struct InvoiceSaleAmounts(
    decimal TaxableAmount,
    decimal CgstAmount,
    decimal SgstAmount,
    decimal IgstAmount,
    decimal RoundOff,
    decimal GrossInvoiceAmount,
    decimal FinalInvoiceAmount);

public static class InvoiceSaleAmountsCalculator
{
    public static InvoiceSaleAmounts Calculate(
        decimal taxableAmount,
        decimal cgstPercent,
        decimal sgstPercent,
        decimal igstPercent,
        decimal discountAmount)
    {
        var cgstAmount = 0M;
        var sgstAmount = 0M;
        var igstAmount = 0M;

        if (taxableAmount >= 0M)
        {
            cgstAmount = MathUtils.Normalize(
                taxableAmount * Math.Round(cgstPercent / 100M, 3));
            sgstAmount = MathUtils.Normalize(
                taxableAmount * Math.Round(sgstPercent / 100M, 3));
            igstAmount = MathUtils.Normalize(
                taxableAmount * Math.Round(igstPercent / 100M, 3));
        }

        var unroundedGross =
            taxableAmount + cgstAmount + sgstAmount + igstAmount;
        var roundOff = Math.Round(unroundedGross, 0) - unroundedGross;
        var grossInvoiceAmount = MathUtils.Normalize(unroundedGross, 0);
        var finalInvoiceAmount = MathUtils.Normalize(
            grossInvoiceAmount - discountAmount);

        return new InvoiceSaleAmounts(
            taxableAmount,
            cgstAmount,
            sgstAmount,
            igstAmount,
            roundOff,
            grossInvoiceAmount,
            finalInvoiceAmount);
    }
}
