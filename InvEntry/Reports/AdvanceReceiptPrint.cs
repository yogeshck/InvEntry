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

/// <summary>
/// A5 receipt for an advance or a savings scheme (RD) installment, built from
/// <see cref="AdvanceReceiptPrintDto"/>. Figures are as they stood right after
/// the receipt, so a reprint matches the original.
/// </summary>
public sealed class AdvanceReceiptPrint : XtraReport
{
    // A5 portrait in hundredths of an inch, with 0.3" margins.
    private const float Width = 523F;
    private const float LabelWidth = 165F;
    private const float RowHeight = 21F;

    private readonly DetailBand _detail = new() { HeightF = 0F };
    private float _y;

    public AdvanceReceiptPrint(
        AdvanceReceiptPrintDto receipt,
        OrgThisCompanyView? company,
        bool isReprint)
    {
        PaperKind = DXPaperKind.A5;
        Margins = new DXMargins(30, 30, 30, 30);
        Font = new DXFont("Segoe UI", 9F);
        Bands.Add(_detail);
        Bands.Add(new TopMarginBand { HeightF = 30F });
        Bands.Add(new BottomMarginBand { HeightF = 30F });

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

        _detail.HeightF = _y + 5F;
    }

    private void AddCompanyHeader(OrgThisCompanyView? company)
    {
        if (company is null)
            return;

        AddText(company.CompanyName ?? string.Empty, 16F, DXFontStyle.Bold, TextAlignment.MiddleCenter, 28F);

        if (!string.IsNullOrWhiteSpace(company.Tagline))
            AddText(company.Tagline!, 8.5F, DXFontStyle.Italic, TextAlignment.MiddleCenter, 16F);

        var address = Join(", ", company.AddressLine1, company.AddressLine2, company.Area,
            company.District, company.State, company.Pincode?.ToString());
        if (address.Length > 0)
            AddText(address, 8.5F, DXFontStyle.Regular, TextAlignment.MiddleCenter, 16F);

        var contact = Join("   ",
            Join(" / ", company.ContactNbr1, company.ContactNbr2) is { Length: > 0 } phones ? $"Ph: {phones}" : null,
            string.IsNullOrWhiteSpace(company.GstNbr) ? null : $"GSTIN: {company.GstNbr}");
        if (contact.Length > 0)
            AddText(contact, 8.5F, DXFontStyle.Regular, TextAlignment.MiddleCenter, 16F);

        AddLine();
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
        var box = new XRLabel
        {
            LocationF = new PointF(0F, _y),
            SizeF = new SizeF(Width, 34F),
            Text = $"Amount received:  ₹ {amount:N2}",
            Font = new DXFont("Segoe UI", 14F, DXFontStyle.Bold),
            TextAlignment = TextAlignment.MiddleCenter,
            Borders = BorderSide.All,
            BorderWidth = 1F,
            Padding = new PaddingInfo(4, 4, 2, 2)
        };
        _detail.Controls.Add(box);
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
        AddCell("Customer signature", 0F, Width / 2F, 8.5F, DXFontStyle.Regular, TextAlignment.MiddleLeft, BorderSide.Top);
        AddCell("Authorised signatory", Width / 2F + 60F, Width / 2F - 60F, 8.5F, DXFontStyle.Regular, TextAlignment.MiddleRight, BorderSide.Top);
        _y += RowHeight + 6F;

        AddText($"Computer generated receipt · printed {DateTime.Now:dd-MM-yyyy HH:mm}",
            7F, DXFontStyle.Regular, TextAlignment.MiddleCenter, 14F, Color.Gray);
    }

    // =========================================================
    // LAYOUT HELPERS
    // =========================================================

    private void AddSection(string title)
    {
        _y += 4F;
        AddCell(title, 0F, Width, 9.5F, DXFontStyle.Bold, TextAlignment.MiddleLeft, BorderSide.Bottom);
        _y += RowHeight + 2F;
    }

    private void AddRow(string label, string value)
    {
        AddCell(label, 0F, LabelWidth, 9F, DXFontStyle.Regular, TextAlignment.MiddleLeft);
        AddCell(value, LabelWidth, Width - LabelWidth, 9F, DXFontStyle.Bold, TextAlignment.MiddleLeft);
        _y += RowHeight;
    }

    private void AddPair(string label1, string value1, string label2, string value2)
    {
        const float half = Width / 2F;
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
            LocationF = new PointF(0F, _y),
            SizeF = new SizeF(Width, height),
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
            LocationF = new PointF(x, _y),
            SizeF = new SizeF(width, RowHeight),
            Text = text,
            Font = new DXFont("Segoe UI", size, style),
            TextAlignment = alignment,
            Borders = borders,
            BorderWidth = 1F,
            Padding = new PaddingInfo(2, 2, 0, 0)
        });
    }

    private void AddLine()
    {
        _detail.Controls.Add(new XRLine
        {
            LocationF = new PointF(0F, _y + 2F),
            SizeF = new SizeF(Width, 4F),
            LineWidth = 1F
        });
        _y += 8F;
    }

    private static string Join(string separator, params string?[] parts)
    {
        var list = new List<string>();
        foreach (var part in parts)
        {
            if (!string.IsNullOrWhiteSpace(part))
                list.Add(part.Trim());
        }

        return string.Join(separator, list);
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
