using DevExpress.Drawing;
using DevExpress.Drawing.Printing;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using InvEntry.Contracts.Schemes;
using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace InvEntry.Reports;

/// <summary>Paper of the RD passbook / statement.</summary>
public enum RdStatementPaper
{
    /// <summary>A4 portrait statement.</summary>
    A4,

    /// <summary>A5 portrait passbook page.</summary>
    A5
}

/// <summary>
/// RD passbook / statement built from <see cref="RdStatementDto"/>: invoice-style
/// header, scheme details, summary, the installment schedule and every payment.
/// The schedule and payments are data-bound bands, so a long history continues
/// on the next page with its column headings repeated.
/// </summary>
public sealed class RdStatementPrint : XtraReport
{
    private const float Margin = 30F;
    private const float RowHeight = 18F;

    private readonly float _width;
    private readonly float _fontSize;
    private readonly bool _isWeight;

    public RdStatementPrint(
        RdStatementDto statement,
        OrgThisCompanyView? company,
        RdStatementPaper paper = RdStatementPaper.A4)
    {
        PaperKind = paper == RdStatementPaper.A4 ? DXPaperKind.A4 : DXPaperKind.A5;
        Margins = new DXMargins((int)Margin, (int)Margin, (int)Margin, (int)Margin);
        Font = new DXFont("Segoe UI", 9F);

        _width = (paper == RdStatementPaper.A4 ? 827F : 583F) - 2 * Margin;
        _fontSize = paper == RdStatementPaper.A4 ? 9F : 7.5F;
        _isWeight = statement.Scheme.SchemeType == SchemeType.Weight;

        Bands.Add(new TopMarginBand { HeightF = Margin });
        Bands.Add(new BottomMarginBand { HeightF = Margin });
        Bands.Add(new DetailBand { HeightF = 0F });

        Bands.Add(BuildHeader(statement, company));
        Bands.Add(BuildSchedule(statement));
        Bands.Add(BuildPayments(statement));
        Bands.Add(BuildFooter(statement));
        Bands.Add(BuildPageFooter(statement));
    }

    // =========================================================
    // HEADER: company, title, scheme details, summary
    // =========================================================

    private ReportHeaderBand BuildHeader(RdStatementDto statement, OrgThisCompanyView? company)
    {
        var band = new ReportHeaderBand();
        var detail = band;
        var scheme = statement.Scheme;

        var y = InvoiceStyleHeader.Add(detail, 0F, 0F, _width, company);

        y = Text(detail, "RD PASSBOOK / STATEMENT", 0F, y, _width, 22F, 12.5F, DXFontStyle.Bold, TextAlignment.MiddleCenter);
        y += 4F;

        var half = _width / 2F;
        var labelWidth = half * 0.38F;

        void Pair(string label1, string value1, string label2, string value2)
        {
            Text(detail, label1, 0F, y, labelWidth, RowHeight, _fontSize, DXFontStyle.Regular);
            Text(detail, value1, labelWidth, y, half - labelWidth, RowHeight, _fontSize, DXFontStyle.Bold);
            Text(detail, label2, half + 10F, y, labelWidth, RowHeight, _fontSize, DXFontStyle.Regular);
            Text(detail, value2, half + 10F + labelWidth, y, half - labelWidth - 10F, RowHeight, _fontSize, DXFontStyle.Bold);
            y += RowHeight;
        }

        Pair("RD No", scheme.AccountNbr, "Plan", $"{scheme.PlanName}");
        Pair("Customer", scheme.CustomerName ?? string.Empty, "Mobile", scheme.CustomerMobile ?? string.Empty);
        Pair("Monthly installment", $"₹ {scheme.InstallmentAmount:N2}", "Installments", $"{scheme.TenureMonths} months");
        Pair("Start date", scheme.StartDate.ToString("dd-MM-yyyy"), "Maturity date", scheme.MaturityDate.ToString("dd-MM-yyyy"));
        Pair("Nominee", Nominee(scheme), "Status", StatusText(statement));

        if (_isWeight)
            Pair("Metal", $"{scheme.Metal} {scheme.Purity}", "Gold accumulated", $"{scheme.BalanceWeight:N3} g");

        // Summary box.
        y += 6F;
        var boxTop = y;
        var cell = _width / 4F;

        void Summary(int column, string label, string value)
        {
            Text(detail, label, column * cell, boxTop + 2F, cell, 15F, _fontSize - 0.5F, DXFontStyle.Regular, TextAlignment.MiddleCenter, Color.DimGray);
            Text(detail, value, column * cell, boxTop + 17F, cell, 20F, _fontSize + 2F, DXFontStyle.Bold, TextAlignment.MiddleCenter);
        }

        Summary(0, "Total paid", $"₹ {scheme.TotalPaid:N2}");
        Summary(1, "Scheme total", $"₹ {scheme.TotalDue:N2}");
        Summary(2, "Still to pay", $"₹ {Math.Max(0M, scheme.TotalDue - scheme.TotalPaid):N2}");
        Summary(3, "Installments paid", $"{scheme.InstallmentsPaid} of {scheme.TenureMonths}");

        detail.Controls.Add(new XRLabel
        {
            LocationF = new PointF(0F, boxTop),
            SizeF = new SizeF(_width, 40F),
            Borders = BorderSide.All,
            BorderWidth = 1F
        });
        y = boxTop + 44F;

        y = Text(detail, BonusText(statement), 0F, y, _width, RowHeight, _fontSize, DXFontStyle.Bold, TextAlignment.MiddleLeft,
            statement.BonusEarned ? Color.ForestGreen : Color.DimGray);

        if (scheme.NextInstallment is { } next)
        {
            y = Text(detail, $"Next installment: ₹ {next.AmountOutstanding:N2} due on {next.DueDate:dd-MM-yyyy}",
                0F, y, _width, RowHeight, _fontSize, DXFontStyle.Regular);
        }

        band.HeightF = y + 6F;
        return band;
    }

    // =========================================================
    // SCHEDULE
    // =========================================================

    private sealed class ScheduleRow
    {
        public string Seq { get; init; } = string.Empty;
        public string Due { get; init; } = string.Empty;
        public string AmountDue { get; init; } = string.Empty;
        public string Paid { get; init; } = string.Empty;
        public string PaidOn { get; init; } = string.Empty;
        public string Grams { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
    }

    private DetailReportBand BuildSchedule(RdStatementDto statement)
    {
        var today = DateOnly.FromDateTime(statement.GeneratedOn);

        var rows = statement.Scheme.Installments
            .OrderBy(x => x.SeqNbr)
            .Select(x => new ScheduleRow
            {
                Seq = x.SeqNbr.ToString(),
                Due = x.DueDate.ToString("dd-MM-yyyy"),
                AmountDue = x.AmountDue.ToString("N2"),
                Paid = x.AmountPaid > 0M ? x.AmountPaid.ToString("N2") : "-",
                PaidOn = x.PaidOn?.ToString("dd-MM-yyyy") ?? string.Empty,
                Grams = x.WeightCredited > 0M ? x.WeightCredited.ToString("N3") : string.Empty,
                Status = InstallmentStatus(x, today)
            })
            .ToList();

        var columns = new List<(string Title, string Field, float Share, TextAlignment Align)>
        {
            ("#", nameof(ScheduleRow.Seq), 0.06F, TextAlignment.MiddleCenter),
            ("Due date", nameof(ScheduleRow.Due), 0.17F, TextAlignment.MiddleLeft),
            ("Amount due", nameof(ScheduleRow.AmountDue), 0.17F, TextAlignment.MiddleRight),
            ("Paid", nameof(ScheduleRow.Paid), 0.17F, TextAlignment.MiddleRight),
            ("Paid on", nameof(ScheduleRow.PaidOn), 0.17F, TextAlignment.MiddleCenter)
        };

        if (_isWeight)
            columns.Add(("Grams", nameof(ScheduleRow.Grams), 0.10F, TextAlignment.MiddleRight));

        columns.Add(("Status", nameof(ScheduleRow.Status), _isWeight ? 0.16F : 0.26F, TextAlignment.MiddleLeft));

        return Table("Installment schedule", rows, columns, highlightField: nameof(ScheduleRow.Status));
    }

    // =========================================================
    // PAYMENTS
    // =========================================================

    private sealed class PaymentRow
    {
        public string Date { get; init; } = string.Empty;
        public string Receipt { get; init; } = string.Empty;
        public string Mode { get; init; } = string.Empty;
        public string Amount { get; init; } = string.Empty;
        public string Grams { get; init; } = string.Empty;
        public string Covered { get; init; } = string.Empty;
        public string Balance { get; init; } = string.Empty;
    }

    private DetailReportBand BuildPayments(RdStatementDto statement)
    {
        var rows = statement.Payments
            .Select(x => new PaymentRow
            {
                Date = x.Date.ToString("dd-MM-yyyy"),
                Receipt = x.EntryType == "RECEIPT" ? x.ReceiptNumber : $"{x.ReceiptNumber} ({x.EntryType.ToLowerInvariant()})",
                Mode = x.Mode ?? string.Empty,
                Amount = x.Amount.ToString("N2"),
                Grams = x.Weight != 0M ? x.Weight.ToString("N3") : string.Empty,
                Covered = x.InstallmentsCovered ?? string.Empty,
                Balance = x.RunningBalance.ToString("N2")
            })
            .ToList();

        var columns = new List<(string Title, string Field, float Share, TextAlignment Align)>
        {
            ("Date", nameof(PaymentRow.Date), 0.15F, TextAlignment.MiddleLeft),
            ("Receipt No", nameof(PaymentRow.Receipt), 0.17F, TextAlignment.MiddleLeft),
            ("Mode", nameof(PaymentRow.Mode), 0.11F, TextAlignment.MiddleLeft),
            ("Amount", nameof(PaymentRow.Amount), 0.15F, TextAlignment.MiddleRight)
        };

        if (_isWeight)
            columns.Add(("Grams", nameof(PaymentRow.Grams), 0.10F, TextAlignment.MiddleRight));

        columns.Add(("Installments", nameof(PaymentRow.Covered), _isWeight ? 0.16F : 0.24F, TextAlignment.MiddleLeft));
        columns.Add(("Balance", nameof(PaymentRow.Balance), 0.16F, TextAlignment.MiddleRight));

        return Table(rows.Count == 0 ? "Payments (none yet)" : "Payments", rows, columns);
    }

    // =========================================================
    // FOOTERS
    // =========================================================

    private ReportFooterBand BuildFooter(RdStatementDto statement)
    {
        var band = new ReportFooterBand();
        var detail = band;
        var y = 8F;

        y = Text(detail,
            "Payments under this scheme are advances towards the purchase of jewellery and are adjusted against your purchase. " +
            "They are not a deposit or investment.",
            0F, y, _width, 28F, _fontSize - 1F, DXFontStyle.Italic, TextAlignment.TopLeft, Color.DimGray, multiline: true);

        y += 26F;
        Text(detail, "Customer signature", 0F, y, _width / 2F - 40F, RowHeight, _fontSize, DXFontStyle.Regular,
            TextAlignment.MiddleLeft, borders: BorderSide.Top);
        Text(detail, "Authorised signatory", _width / 2F + 40F, y, _width / 2F - 40F, RowHeight, _fontSize, DXFontStyle.Regular,
            TextAlignment.MiddleRight, borders: BorderSide.Top);

        band.HeightF = y + RowHeight + 4F;
        return band;
    }

    private PageFooterBand BuildPageFooter(RdStatementDto statement)
    {
        var band = new PageFooterBand { HeightF = 18F };

        band.Controls.Add(new XRLabel
        {
            LocationF = new PointF(0F, 2F),
            SizeF = new SizeF(_width * 0.7F, 14F),
            Text = $"{statement.Scheme.AccountNbr} · computer generated statement · printed {statement.GeneratedOn:dd-MM-yyyy HH:mm}",
            Font = new DXFont("Segoe UI", 7F),
            ForeColor = Color.Gray
        });

        band.Controls.Add(new XRPageInfo
        {
            LocationF = new PointF(_width * 0.7F, 2F),
            SizeF = new SizeF(_width * 0.3F, 14F),
            PageInfo = PageInfo.NumberOfTotal,
            TextFormatString = "Page {0} of {1}",
            Font = new DXFont("Segoe UI", 7F),
            ForeColor = Color.Gray,
            TextAlignment = TextAlignment.MiddleRight
        });

        return band;
    }

    // =========================================================
    // TABLE AND TEXT HELPERS
    // =========================================================

    private DetailReportBand Table<T>(
        string title,
        List<T> rows,
        List<(string Title, string Field, float Share, TextAlignment Align)> columns,
        string? highlightField = null)
    {
        var report = new DetailReportBand { DataSource = rows, Level = Bands.OfType<DetailReportBand>().Count() };
        var shareTotal = columns.Sum(x => x.Share);

        // Section title and column headings, repeated on every page.
        var header = new GroupHeaderBand { RepeatEveryPage = true, HeightF = 22F + RowHeight };
        Text(header, title, 0F, 4F, _width, 18F, _fontSize + 1F, DXFontStyle.Bold);

        var detail = new DetailBand { HeightF = RowHeight };
        var x = 0F;

        foreach (var column in columns)
        {
            var width = _width * column.Share / shareTotal;

            header.Controls.Add(new XRLabel
            {
                LocationF = new PointF(x, 22F),
                SizeF = new SizeF(width, RowHeight),
                Text = column.Title,
                Font = new DXFont("Segoe UI", _fontSize, DXFontStyle.Bold),
                BackColor = Color.FromArgb(255, 248, 225),
                Borders = BorderSide.Top | BorderSide.Bottom,
                BorderColor = Color.Goldenrod,
                TextAlignment = column.Align,
                Padding = new PaddingInfo(column.Align == TextAlignment.MiddleLeft ? 10 : 3, 3, 0, 0)
            });

            var cell = new XRLabel
            {
                LocationF = new PointF(x, 0F),
                SizeF = new SizeF(width, RowHeight),
                Font = new DXFont("Segoe UI", _fontSize),
                Borders = BorderSide.Bottom,
                BorderColor = Color.Gainsboro,
                TextAlignment = column.Align,
                // Left-aligned text after a right-aligned number needs a gap.
                Padding = new PaddingInfo(column.Align == TextAlignment.MiddleLeft ? 10 : 3, 3, 0, 0)
            };
            cell.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", $"[{column.Field}]"));

            if (column.Field == highlightField)
            {
                // Overdue installments in red, paid ones in green.
                cell.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "ForeColor",
                    $"Iif(StartsWith([{column.Field}], 'Overdue'), 'Firebrick', Iif([{column.Field}] = 'Paid', 'ForestGreen', 'Black'))"));
            }

            detail.Controls.Add(cell);
            x += width;
        }

        report.Bands.Add(header);
        report.Bands.Add(detail);
        report.Bands.Add(new ReportFooterBand { HeightF = 6F });
        return report;
    }

    private static float Text(
        Band band,
        string text,
        float x,
        float y,
        float width,
        float height,
        float size,
        DXFontStyle style,
        TextAlignment alignment = TextAlignment.MiddleLeft,
        Color? color = null,
        bool multiline = false,
        BorderSide borders = BorderSide.None)
    {
        var label = new XRLabel
        {
            LocationF = new PointF(x, y),
            SizeF = new SizeF(width, height),
            Text = text,
            Font = new DXFont("Segoe UI", size, style),
            TextAlignment = alignment,
            Multiline = multiline,
            WordWrap = multiline,
            Borders = borders,
            BorderWidth = 1F,
            Padding = new PaddingInfo(2, 2, 0, 0)
        };

        if (color is Color foreColor)
            label.ForeColor = foreColor;

        band.Controls.Add(label);
        return y + height;
    }

    private static string Nominee(SchemeEnrollmentDto scheme) =>
        string.IsNullOrWhiteSpace(scheme.NomineeName)
            ? "-"
            : string.IsNullOrWhiteSpace(scheme.NomineeRelation)
                ? scheme.NomineeName!
                : $"{scheme.NomineeName} ({scheme.NomineeRelation})";

    private static string StatusText(RdStatementDto statement)
    {
        var scheme = statement.Scheme;

        if (scheme.Status != "ACTIVE")
            return scheme.Status;

        if (scheme.NextInstallment is null)
            return "All installments paid";

        var today = DateOnly.FromDateTime(statement.GeneratedOn);
        var overdue = scheme.Installments.Count(x =>
            x.DueDate < today && x.Status != "PAID" && x.Status != "WAIVED");

        return overdue == 0 ? "Active, up to date" : $"Active, {overdue} overdue";
    }

    private static string BonusText(RdStatementDto statement)
    {
        if (statement.BonusAmount <= 0M)
            return "No bonus on this scheme.";

        return statement.BonusEarned
            ? $"Bonus ₹ {statement.BonusAmount:N2} earned. Maturity value: ₹ {statement.MaturityValue:N2}."
            : $"Bonus ₹ {statement.BonusAmount:N2} is earned when all {statement.BonusMinInstallments} installments are paid " +
              $"({statement.Scheme.InstallmentsPaid} paid so far).";
    }

    private static string InstallmentStatus(SchemeInstallmentDto installment, DateOnly today) =>
        installment.Status switch
        {
            "PAID" => "Paid",
            "WAIVED" => "Waived",
            "PARTIAL" when installment.DueDate < today => "Overdue (part paid)",
            "PARTIAL" => "Part paid",
            _ when installment.DueDate < today => "Overdue",
            _ => "Pending"
        };
}
