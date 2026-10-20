#nullable enable
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

/// <summary>
/// RD dues / collections list for a month on A4 landscape: invoice-style header,
/// summary, one row per installment (headings repeated on every page), totals and
/// a blank Remarks column for follow-up notes.
/// </summary>
public sealed class RdDuesPrint : XtraReport
{
    private const float Margin = 28F;
    private const float RowHeight = 18F;
    private const float FontSize = 8F;
    private const float ContentWidth = 1169F - 2 * Margin;   // A4 landscape at 1/100 inch

    public RdDuesPrint(
        RdDueReportDto report,
        IReadOnlyList<RdDueRowDto> rows,
        string filterText,
        OrgThisCompanyView? company)
    {
        PaperKind = DXPaperKind.A4;
        Landscape = true;
        Margins = new DXMargins((int)Margin, (int)Margin, (int)Margin, (int)Margin);
        Font = new DXFont("Segoe UI", FontSize);

        var month = new DateTime(report.Year, report.Month, 1);

        Bands.Add(new TopMarginBand { HeightF = Margin });
        Bands.Add(new BottomMarginBand { HeightF = Margin });
        Bands.Add(new DetailBand { HeightF = 0F });
        Bands.Add(BuildHeader(report, rows, month, filterText, company));
        Bands.Add(BuildList(rows));
        Bands.Add(BuildPageFooter(report, month));
    }

    private static ReportHeaderBand BuildHeader(
        RdDueReportDto report,
        IReadOnlyList<RdDueRowDto> rows,
        DateTime month,
        string filterText,
        OrgThisCompanyView? company)
    {
        var band = new ReportHeaderBand();

        var y = InvoiceStyleHeader.Add(band, 0F, 0F, ContentWidth, company);

        var title = $"RD {filterText.ToUpperInvariant()} — {month:MMMM yyyy}".ToUpperInvariant();
        y = AddText(band, title, 0F, y, ContentWidth, 22F, 12.5F, DXFontStyle.Bold, TextAlignment.MiddleCenter);

        var scope = report.IncludesArrears
            ? "Installments due in the month, earlier unpaid installments, and payments received in the month."
            : "Installments due in the month and payments received in the month.";
        y = AddText(band, scope, 0F, y, ContentWidth, 16F, FontSize, DXFontStyle.Italic, TextAlignment.MiddleCenter, Color.DimGray);

        var dueAmount = rows.Where(x => x.IsDue).Sum(x => x.Balance);
        var arrears = rows.Where(x => x.IsArrear).Sum(x => x.Balance);
        var collected = rows.Sum(x => x.CollectedInMonth);
        var customers = rows.Select(x => x.CustomerGkey).Distinct().Count();

        y += 4F;
        var cell = ContentWidth / 4F;
        var top = y;

        void Summary(int column, string label, string value, Color color)
        {
            AddText(band, label, column * cell, top + 2F, cell, 14F, FontSize - 0.5F, DXFontStyle.Regular, TextAlignment.MiddleCenter, Color.DimGray);
            AddText(band, value, column * cell, top + 16F, cell, 20F, FontSize + 3F, DXFontStyle.Bold, TextAlignment.MiddleCenter, color);
        }

        Summary(0, "Installments / customers", $"{rows.Count} / {customers}", Color.Black);
        Summary(1, "Still due", $"₹ {dueAmount:N2}", Color.Firebrick);
        Summary(2, "of which arrears", $"₹ {arrears:N2}", Color.Black);
        Summary(3, $"Collected in {month:MMM yyyy}", $"₹ {collected:N2}", Color.ForestGreen);

        band.Controls.Add(new XRLabel
        {
            LocationF = new PointF(0F, top),
            SizeF = new SizeF(ContentWidth, 38F),
            Borders = BorderSide.All,
            BorderWidth = 1F,
            BorderColor = Color.Goldenrod
        });

        band.HeightF = top + 46F;
        return band;
    }

    private sealed class PrintRow
    {
        public string Sl { get; init; } = string.Empty;
        public string RdNo { get; init; } = string.Empty;
        public string Customer { get; init; } = string.Empty;
        public string Mobile { get; init; } = string.Empty;
        public string Inst { get; init; } = string.Empty;
        public string Due { get; init; } = string.Empty;
        public string Amount { get; init; } = string.Empty;
        public string Paid { get; init; } = string.Empty;
        public string Balance { get; init; } = string.Empty;
        public string Collected { get; init; } = string.Empty;
        public string LastPaid { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string Remarks { get; init; } = string.Empty;
    }

    private DetailReportBand BuildList(IReadOnlyList<RdDueRowDto> rows)
    {
        static string Money(decimal value) => value == 0M ? "-" : value.ToString("N2");

        var data = rows
            .Select((x, i) => new PrintRow
            {
                Sl = (i + 1).ToString(),
                RdNo = x.AccountNbr,
                Customer = x.CustomerName ?? string.Empty,
                Mobile = x.CustomerMobile ?? string.Empty,
                Inst = x.InstallmentText,
                Due = x.DueDate.ToString("dd-MM-yyyy"),
                Amount = x.AmountDue.ToString("N2"),
                Paid = Money(x.AmountPaid),
                Balance = Money(x.Balance),
                Collected = Money(x.CollectedInMonth),
                LastPaid = x.LastPaidOn?.ToString("dd-MM-yyyy") ?? string.Empty,
                Status = x.DaysOverdue > 0 ? $"{x.StatusText} ({x.DaysOverdue}d)" : x.StatusText
            })
            .ToList();

        var columns = new List<(string Title, string Field, float Share, TextAlignment Align)>
        {
            ("#", nameof(PrintRow.Sl), 0.035F, TextAlignment.MiddleCenter),
            ("RD No", nameof(PrintRow.RdNo), 0.09F, TextAlignment.MiddleLeft),
            ("Customer", nameof(PrintRow.Customer), 0.18F, TextAlignment.MiddleLeft),
            ("Mobile", nameof(PrintRow.Mobile), 0.08F, TextAlignment.MiddleLeft),
            ("Inst.", nameof(PrintRow.Inst), 0.045F, TextAlignment.MiddleCenter),
            ("Due date", nameof(PrintRow.Due), 0.07F, TextAlignment.MiddleCenter),
            ("Installment", nameof(PrintRow.Amount), 0.07F, TextAlignment.MiddleRight),
            ("Paid", nameof(PrintRow.Paid), 0.07F, TextAlignment.MiddleRight),
            ("Balance", nameof(PrintRow.Balance), 0.07F, TextAlignment.MiddleRight),
            ("Collected", nameof(PrintRow.Collected), 0.07F, TextAlignment.MiddleRight),
            ("Last paid", nameof(PrintRow.LastPaid), 0.07F, TextAlignment.MiddleCenter),
            ("Status", nameof(PrintRow.Status), 0.10F, TextAlignment.MiddleLeft),
            ("Remarks", nameof(PrintRow.Remarks), 0.08F, TextAlignment.MiddleLeft)
        };

        var shareTotal = columns.Sum(x => x.Share);
        float WidthOf(int index) => ContentWidth * columns[index].Share / shareTotal;

        var list = new DetailReportBand { DataSource = data, Level = 0 };
        var header = new GroupHeaderBand { RepeatEveryPage = true, HeightF = RowHeight + 4F };
        var detail = new DetailBand { HeightF = RowHeight };
        var footer = new ReportFooterBand { HeightF = RowHeight + 6F };

        var x = 0F;
        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            var width = WidthOf(i);
            var padding = new PaddingInfo(column.Align == TextAlignment.MiddleLeft ? 6 : 3, 3, 0, 0);

            header.Controls.Add(new XRLabel
            {
                LocationF = new PointF(x, 4F),
                SizeF = new SizeF(width, RowHeight),
                Text = column.Title,
                Font = new DXFont("Segoe UI", FontSize, DXFontStyle.Bold),
                BackColor = Color.FromArgb(255, 248, 225),
                Borders = BorderSide.Top | BorderSide.Bottom,
                BorderColor = Color.Goldenrod,
                TextAlignment = column.Align,
                Padding = padding
            });

            var cell = new XRLabel
            {
                LocationF = new PointF(x, 0F),
                SizeF = new SizeF(width, RowHeight),
                Font = new DXFont("Segoe UI", FontSize),
                Borders = column.Field == nameof(PrintRow.Remarks) ? BorderSide.Bottom | BorderSide.Left : BorderSide.Bottom,
                BorderColor = Color.Gainsboro,
                TextAlignment = column.Align,
                Padding = padding,
                CanGrow = false
            };
            cell.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", $"[{column.Field}]"));

            if (column.Field == nameof(PrintRow.Status))
            {
                cell.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "ForeColor",
                    $"Iif(StartsWith([{column.Field}], 'Overdue'), 'Firebrick', Iif([{column.Field}] = 'Paid', 'ForestGreen', 'Black'))"));
            }

            detail.Controls.Add(cell);
            x += width;
        }

        // Totals under the money columns.
        var totals = new Dictionary<string, decimal>
        {
            [nameof(PrintRow.Amount)] = rows.Sum(r => r.AmountDue),
            [nameof(PrintRow.Paid)] = rows.Sum(r => r.AmountPaid),
            [nameof(PrintRow.Balance)] = rows.Sum(r => r.Balance),
            [nameof(PrintRow.Collected)] = rows.Sum(r => r.CollectedInMonth)
        };

        x = 0F;
        for (var i = 0; i < columns.Count; i++)
        {
            var width = WidthOf(i);
            var field = columns[i].Field;

            string? text = field == nameof(PrintRow.Customer)
                ? "Total"
                : totals.TryGetValue(field, out var total) ? total.ToString("N2") : null;

            if (text is not null)
            {
                footer.Controls.Add(new XRLabel
                {
                    LocationF = new PointF(x, 2F),
                    SizeF = new SizeF(width, RowHeight),
                    Text = text,
                    Font = new DXFont("Segoe UI", FontSize, DXFontStyle.Bold),
                    TextAlignment = columns[i].Align,
                    Padding = new PaddingInfo(columns[i].Align == TextAlignment.MiddleLeft ? 6 : 3, 3, 0, 0)
                });
            }

            x += width;
        }

        footer.Controls.Add(new XRLabel
        {
            LocationF = new PointF(0F, 1F),
            SizeF = new SizeF(ContentWidth, 1F),
            Borders = BorderSide.Top,
            BorderColor = Color.Goldenrod,
            BorderWidth = 1F
        });

        list.Bands.Add(header);
        list.Bands.Add(detail);
        list.Bands.Add(footer);
        return list;
    }

    private static PageFooterBand BuildPageFooter(RdDueReportDto report, DateTime month)
    {
        var band = new PageFooterBand { HeightF = 18F };

        band.Controls.Add(new XRLabel
        {
            LocationF = new PointF(0F, 2F),
            SizeF = new SizeF(ContentWidth * 0.7F, 14F),
            Text = $"RD dues {month:MMMM yyyy} · printed {report.GeneratedOn:dd-MM-yyyy HH:mm}",
            Font = new DXFont("Segoe UI", 7F),
            ForeColor = Color.Gray
        });

        band.Controls.Add(new XRPageInfo
        {
            LocationF = new PointF(ContentWidth * 0.7F, 2F),
            SizeF = new SizeF(ContentWidth * 0.3F, 14F),
            PageInfo = PageInfo.NumberOfTotal,
            TextFormatString = "Page {0} of {1}",
            Font = new DXFont("Segoe UI", 7F),
            ForeColor = Color.Gray,
            TextAlignment = TextAlignment.MiddleRight
        });

        return band;
    }

    private static float AddText(
        Band band,
        string text,
        float x,
        float y,
        float width,
        float height,
        float size,
        DXFontStyle style,
        TextAlignment alignment = TextAlignment.MiddleLeft,
        Color? color = null)
    {
        var label = new XRLabel
        {
            LocationF = new PointF(x, y),
            SizeF = new SizeF(width, height),
            Text = text,
            Font = new DXFont("Segoe UI", size, style),
            TextAlignment = alignment,
            Padding = new PaddingInfo(2, 2, 0, 0)
        };

        if (color is Color foreColor)
            label.ForeColor = foreColor;

        band.Controls.Add(label);
        return y + height;
    }
}
