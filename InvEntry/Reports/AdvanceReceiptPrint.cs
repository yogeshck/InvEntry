using DevExpress.Drawing;
using DevExpress.Drawing.Printing;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using InvEntry.Contracts.Advances;
using InvEntry.Models;
using InvEntry.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace InvEntry.Reports;

/// <summary>Paper layout of an advance / RD installment receipt.</summary>
public enum AdvanceReceiptLayout
{
    /// <summary>One copy on A5 portrait.</summary>
    A5,

    /// <summary>Customer copy and shop copy side by side on one A4 landscape sheet (two A5 halves).</summary>
    A4TwoCopies
}

/// <summary>
/// Receipt for an advance or a savings scheme (RD) installment, built from
/// <see cref="AdvanceReceiptPrintDto"/>. The header and logo follow the invoice
/// (<see cref="InvPrint25"/>). Figures are as they stood right after the
/// receipt, so a reprint matches the original.
/// </summary>
public sealed class AdvanceReceiptPrint : XtraReport
{
    // One receipt is an A5 portrait page with 0.3" margins (hundredths of an inch).
    private const float Margin = 30F;
    private const float CopyWidth = 523F;
    private const float LabelWidth = 165F;
    private const float RowHeight = 21F;

    // On A4 landscape the two A5 halves are separated by a cut line.
    private const float A4UsableWidth = 1169F - 2 * Margin;
    private const float SecondCopyX = A4UsableWidth - CopyWidth;

    private readonly DetailBand _detail = new() { HeightF = 0F };
    private float _x;
    private float _y;

    public AdvanceReceiptPrint(
        AdvanceReceiptPrintDto receipt,
        OrgThisCompanyView? company,
        bool isReprint,
        AdvanceReceiptLayout layout = AdvanceReceiptLayout.A5)
    {
        PaperKind = layout == AdvanceReceiptLayout.A4TwoCopies ? DXPaperKind.A4 : DXPaperKind.A5;
        Landscape = layout == AdvanceReceiptLayout.A4TwoCopies;
        Margins = new DXMargins((int)Margin, (int)Margin, (int)Margin, (int)Margin);
        Font = new DXFont("Segoe UI", 9F);
        Bands.Add(_detail);
        Bands.Add(new TopMarginBand { HeightF = Margin });
        Bands.Add(new BottomMarginBand { HeightF = Margin });

        if (layout == AdvanceReceiptLayout.A4TwoCopies)
        {
            var height = Math.Max(
                AddCopy(receipt, company, isReprint, 0F, "CUSTOMER COPY"),
                AddCopy(receipt, company, isReprint, SecondCopyX, "SHOP COPY"));

            AddCutLine(height);
            _detail.HeightF = height + 5F;
        }
        else
        {
            _detail.HeightF = AddCopy(receipt, company, isReprint, 0F, copyLabel: null) + 5F;
        }
    }

    /// <summary>Adds one complete receipt at the given left offset; returns its height.</summary>
    private float AddCopy(
        AdvanceReceiptPrintDto receipt,
        OrgThisCompanyView? company,
        bool isReprint,
        float x,
        string? copyLabel)
    {
        _x = x;
        _y = 0F;

        if (copyLabel is not null)
            AddText(copyLabel, 7.5F, DXFontStyle.Bold, TextAlignment.MiddleRight, 14F, Color.DimGray);

        AddCompanyHeader(company);
        AddTitle(receipt, isReprint);
        AddReceiptDetails(receipt);
        AddAmount(receipt.Amount);

        if (receipt.IsScheme)
            AddSchemeDetails(receipt);
        else
            AddAdvanceDetails(receipt);

        if (!string.IsNullOrWhiteSpace(receipt.Notes))
            AddRow("Notes", receipt.Notes!);

        AddFooter(receipt);

        return _y;
    }

    // =========================================================
    // SECTIONS
    // =========================================================

    /// <summary>Same content, fonts and colours as the invoice header (InvPrint25).</summary>
    private void AddCompanyHeader(OrgThisCompanyView? company)
    {
        _y = InvoiceStyleHeader.Add(_detail, _x, _y, CopyWidth, company);
    }

    private void AddTitle(AdvanceReceiptPrintDto receipt, bool isReprint)
    {
        var title = receipt.IsScheme ? "SCHEME INSTALLMENT RECEIPT" : "ADVANCE RECEIPT";

        AddText(title, 13F, DXFontStyle.Bold, TextAlignment.MiddleCenter, 24F);

        if (receipt.IsReversed)
            AddText("CANCELLED", 12F, DXFontStyle.Bold, TextAlignment.MiddleCenter, 20F, Color.Firebrick);
        else if (isReprint)
            AddText("DUPLICATE COPY", 8.5F, DXFontStyle.Bold, TextAlignment.MiddleCenter, 16F, Color.DimGray);

        _y += 4F;
    }

    private void AddReceiptDetails(AdvanceReceiptPrintDto receipt)
    {
        AddPair("Receipt No", receipt.VoucherNbr, "Date", receipt.VoucherDate?.ToString("dd-MM-yyyy") ?? string.Empty);
        AddPair("Received from", receipt.CustomerName ?? string.Empty, "Mobile", receipt.CustomerMobile ?? string.Empty);
        AddPair("Account No", receipt.AccountNbr, "Mode", receipt.Mode ?? string.Empty);
        _y += 4F;
    }

    private void AddAmount(decimal amount)
    {
        _detail.Controls.Add(new XRLabel
        {
            LocationF = new PointF(_x, _y),
            SizeF = new SizeF(CopyWidth, 34F),
            Text = $"Amount received:  ₹ {amount:N2}",
            Font = new DXFont("Segoe UI", 14F, DXFontStyle.Bold),
            TextAlignment = TextAlignment.MiddleCenter,
            Borders = BorderSide.All,
            BorderWidth = 1F,
            Padding = new PaddingInfo(4, 4, 2, 2)
        });
        _y += 36F;

        AddText(AmountInWords(amount), 8.5F, DXFontStyle.Italic, TextAlignment.MiddleCenter, 18F);
        _y += 4F;
    }

    private void AddSchemeDetails(AdvanceReceiptPrintDto receipt)
    {
        AddSection("Scheme");
        AddRow("Plan", $"{receipt.PlanName} ({receipt.PlanCode})");

        if (receipt.InstallmentAmount is decimal monthly)
            AddRow("Monthly installment", $"₹ {monthly:N2}");

        if (!string.IsNullOrWhiteSpace(receipt.InstallmentsCovered))
            AddRow("Installment(s) paid now", receipt.InstallmentsCovered!);

        AddRow("Installments paid", $"{receipt.InstallmentsPaid} of {receipt.TenureMonths}");
        AddRow("Total paid to date", $"₹ {receipt.TotalReceivedToDate:N2}");

        if (receipt.Weight > 0M)
        {
            AddRow("Gold credited",
                $"{receipt.Weight:N3} g  ({receipt.Metal} {receipt.Purity} @ ₹ {receipt.MetalRate:N2}/g)");
            AddRow("Total gold accumulated", $"{receipt.WeightBalanceAfter:N3} g");
        }

        AddRow("Next installment",
            receipt.NextDueDate is DateOnly due
                ? $"₹ {receipt.NextDueAmount:N2} due on {due:dd-MM-yyyy}"
                : "All installments paid");

        if (receipt.MaturityDate is DateOnly maturity)
            AddRow("Maturity date", maturity.ToString("dd-MM-yyyy"));

        if (!string.IsNullOrWhiteSpace(receipt.NomineeName))
            AddRow("Nominee", receipt.NomineeName!);
    }

    private void AddAdvanceDetails(AdvanceReceiptPrintDto receipt)
    {
        AddSection("Advance");

        if (!string.IsNullOrWhiteSpace(receipt.CustomerOrderNbr))
            AddRow("Against order", receipt.CustomerOrderNbr!);

        AddRow("Total advance received", $"₹ {receipt.TotalReceivedToDate:N2}");
        AddRow("Advance balance", $"₹ {receipt.BalanceAfter:N2}");
    }

    private void AddFooter(AdvanceReceiptPrintDto receipt)
    {
        _y += 8F;

        var note = receipt.IsScheme
            ? "This amount is an advance towards the purchase of jewellery under the scheme terms. " +
              "It is adjusted against your purchase and is not a deposit or investment."
            : "This amount is an advance towards the purchase of jewellery and will be adjusted against your bill.";

        AddText(note, 7.5F, DXFontStyle.Italic, TextAlignment.TopLeft, 30F, Color.DimGray, multiline: true);

        _y += 30F;
        AddCell("Customer signature", 0F, CopyWidth / 2F, 8.5F, DXFontStyle.Regular, TextAlignment.MiddleLeft, BorderSide.Top);
        AddCell("Authorised signatory", CopyWidth / 2F + 60F, CopyWidth / 2F - 60F, 8.5F, DXFontStyle.Regular, TextAlignment.MiddleRight, BorderSide.Top);
        _y += RowHeight + 6F;

        AddText($"Computer generated receipt · printed {DateTime.Now:dd-MM-yyyy HH:mm}",
            7F, DXFontStyle.Regular, TextAlignment.MiddleCenter, 14F, Color.Gray);
    }

    private void AddCutLine(float height)
    {
        var x = CopyWidth + (SecondCopyX - CopyWidth) / 2F;

        _detail.Controls.Add(new XRLine
        {
            LocationF = new PointF(x - 2F, 0F),
            SizeF = new SizeF(4F, height),
            LineDirection = LineDirection.Vertical,
            LineStyle = DXDashStyle.Dot,
            LineWidth = 1F,
            ForeColor = Color.Gray
        });
    }

    // =========================================================
    // LAYOUT HELPERS (positions are relative to the current copy)
    // =========================================================

    private void AddSection(string title)
    {
        _y += 4F;
        AddCell(title, 0F, CopyWidth, 9.5F, DXFontStyle.Bold, TextAlignment.MiddleLeft, BorderSide.Bottom);
        _y += RowHeight + 2F;
    }

    private void AddRow(string label, string value)
    {
        AddCell(label, 0F, LabelWidth, 9F, DXFontStyle.Regular, TextAlignment.MiddleLeft);
        AddCell(value, LabelWidth, CopyWidth - LabelWidth, 9F, DXFontStyle.Bold, TextAlignment.MiddleLeft);
        _y += RowHeight;
    }

    private void AddPair(string label1, string value1, string label2, string value2)
    {
        const float half = CopyWidth / 2F;
        AddCell(label1, 0F, 95F, 9F, DXFontStyle.Regular, TextAlignment.MiddleLeft);
        AddCell(value1, 95F, half - 95F, 9F, DXFontStyle.Bold, TextAlignment.MiddleLeft);
        AddCell(label2, half + 20F, 60F, 9F, DXFontStyle.Regular, TextAlignment.MiddleLeft);
        AddCell(value2, half + 80F, half - 80F, 9F, DXFontStyle.Bold, TextAlignment.MiddleLeft);
        _y += RowHeight;
    }

    private void AddText(
        string text,
        float size,
        DXFontStyle style,
        TextAlignment alignment,
        float height,
        Color? color = null,
        bool multiline = false)
    {
        var label = new XRLabel
        {
            LocationF = new PointF(_x, _y),
            SizeF = new SizeF(CopyWidth, height),
            Text = text,
            Font = new DXFont("Segoe UI", size, style),
            TextAlignment = alignment,
            Multiline = multiline,
            WordWrap = multiline
        };

        if (color is Color foreColor)
            label.ForeColor = foreColor;

        _detail.Controls.Add(label);
        _y += height;
    }

    private void AddCell(
        string text,
        float x,
        float width,
        float size,
        DXFontStyle style,
        TextAlignment alignment,
        BorderSide borders = BorderSide.None)
    {
        _detail.Controls.Add(new XRLabel
        {
            LocationF = new PointF(_x + x, _y),
            SizeF = new SizeF(width, RowHeight),
            Text = text,
            Font = new DXFont("Segoe UI", size, style),
            TextAlignment = alignment,
            Borders = borders,
            BorderWidth = 1F,
            Padding = new PaddingInfo(2, 2, 0, 0)
        });
    }

    /// <summary>"RUPEES FIVE THOUSAND AND FIFTY PAISE ONLY".</summary>
    public static string AmountInWords(decimal amount)
    {
        var rupees = (long)decimal.Truncate(amount);
        var paise = (int)decimal.Round((amount - rupees) * 100M);

        var words = $"RUPEES {NumberToWords.Convert(rupees).Trim()}";

        if (paise > 0)
            words += $" AND {NumberToWords.Convert(paise).Trim()} PAISE";

        return words + " ONLY";
    }
}
