using DevExpress.XtraReports.UI;
using InvEntry.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

namespace InvEntry.Reports
{
    public partial class CustomerOrderPrint : DevExpress.XtraReports.UI.XtraReport
    {
        private string NumberToWordsFormat = "{0} ONLY";

        public CustomerOrderPrint()
        {
            InitializeComponent();
            AddCustomerOrderSettlementSections();
        }

        private void AddCustomerOrderSettlementSections()
        {
            Bands.Add(CreateOldMetalBand());
            Bands.Add(CreateReceiptBand());
            Bands.Add(CreateSettlementSummaryBand());
            DetailReport2.Level = 4;
            invTotalTable.Visible = false;
        }

        private DetailReportBand CreateOldMetalBand()
        {
            var band = CreateDetailReportBand("OldMetalDetailReport", "CUSTOMER_ORDER_OLD_METAL", 1);
            band.Bands.Add(CreateSectionHeader("OLD METAL",
                new[] { "Description", "Purity", "Gross Wt.", "Net Wt.", "Rate", "Amount" },
                new[] { 2.3F, .65F, .75F, .75F, 1F, 1.15F }));
            band.Bands.Add(CreateDetailRow("OldMetalDetail",
                new[] { "[PRODUCT_CATEGORY]", "[PURITY]", "[GROSS_WEIGHT]", "[NET_WEIGHT]", "[TRANSACTED_RATE]", "[FINAL_PURCHASE_PRICE]" },
                new[] { 2.3F, .65F, .75F, .75F, 1F, 1.15F },
                new[] { "", "", "{0:#0.000}", "{0:#0.000}", "{0:##,###,##0.00}", "{0:##,###,##0.00}" }));
            return band;
        }

        private DetailReportBand CreateReceiptBand()
        {
            var band = CreateDetailReportBand("ReceiptDetailReport", "CUSTOMER_ORDER_RECEIPTS", 2);
            band.Bands.Add(CreateSectionHeader("ADVANCE / RECEIPTS",
                new[] { "Voucher No.", "Date", "Mode", "Type", "Amount" },
                new[] { 1.2F, 1F, 1F, 2F, 1.2F }));
            band.Bands.Add(CreateDetailRow("ReceiptDetail",
                new[] { "[VOUCHER_NBR]", "[VOUCHER_DATE]", "[MODE]", "[VOUCHER_TYPE]", "[TRANS_AMOUNT]" },
                new[] { 1.2F, 1F, 1F, 2F, 1.2F },
                new[] { "", "{0:dd-MMM-yyyy}", "", "", "{0:##,###,##0.00}" }));
            return band;
        }

        private DetailReportBand CreateSettlementSummaryBand()
        {
            var band = CreateDetailReportBand("SettlementSummaryDetailReport", "CUSTOMER_ORDER_SETTLEMENT", 3);
            var detail = new DetailBand { Name = "SettlementSummaryDetail", HeightF = 84F };
            var table = new XRTable
            {
                Name = "SettlementSummaryTable",
                BoundsF = new RectangleF(460F, 4F, 280F, 80F),
                Borders = DevExpress.XtraPrinting.BorderSide.All,
                Font = new DevExpress.Drawing.DXFont("Segoe UI", 8F)
            };
            AddSummaryRow(table, "Estimated Order Total", "[TOTAL_ORDER_AMOUNT]", true);
            AddSummaryRow(table, "Less: Old Metal", "[OLD_METAL_TOTAL]", false);
            AddSummaryRow(table, "Less: Advance / Receipts", "[ADVANCE_RECEIPT_TOTAL]", false);
            AddBalanceSummaryRow(table);
            detail.Controls.Add(table);
            band.Bands.Add(detail);
            return band;
        }

        private DetailReportBand CreateDetailReportBand(string name, string dataMember, int level) => new()
        {
            Name = name,
            DataSource = sqlDataSource1,
            DataMember = dataMember,
            Level = level
        };

        private static GroupHeaderBand CreateSectionHeader(string title, string[] captions, float[] weights)
        {
            var header = new GroupHeaderBand { HeightF = 42F };
            header.Controls.Add(new XRLabel
            {
                Text = title,
                Font = new DevExpress.Drawing.DXFont("Segoe UI", 9F, DevExpress.Drawing.DXFontStyle.Bold),
                BoundsF = new RectangleF(0F, 4F, 740F, 18F)
            });
            header.Controls.Add(CreateTable(captions, weights, true, 22F));
            return header;
        }

        private static DetailBand CreateDetailRow(string name, string[] expressions, float[] weights, string[] formats)
        {
            var detail = new DetailBand { Name = name, HeightF = 22F };
            var table = CreateTable(expressions, weights, false, 0F);
            for (var index = 0; index < expressions.Length; index++)
            {
                var cell = table.Rows[0].Cells[index];
                cell.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", expressions[index]));
                cell.TextFormatString = formats[index];
            }
            detail.Controls.Add(table);
            return detail;
        }

        private static XRTable CreateTable(string[] values, float[] weights, bool isHeader, float top)
        {
            var table = new XRTable
            {
                BoundsF = new RectangleF(0F, top, 740F, 20F),
                Borders = DevExpress.XtraPrinting.BorderSide.All,
                Font = new DevExpress.Drawing.DXFont("Segoe UI", 8F,
                    isHeader ? DevExpress.Drawing.DXFontStyle.Bold : DevExpress.Drawing.DXFontStyle.Regular)
            };
            var row = new XRTableRow();
            for (var index = 0; index < values.Length; index++)
            {
                row.Cells.Add(new XRTableCell
                {
                    Text = isHeader ? values[index] : string.Empty,
                    Weight = weights[index],
                    Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 100F)
                });
            }
            table.Rows.Add(row);
            return table;
        }

        private static void AddSummaryRow(XRTable table, string caption, string expression, bool bold)
        {
            var row = new XRTableRow();
            var font = new DevExpress.Drawing.DXFont("Segoe UI", 8F,
                bold ? DevExpress.Drawing.DXFontStyle.Bold : DevExpress.Drawing.DXFontStyle.Regular);
            row.Cells.Add(new XRTableCell { Text = caption, Weight = 1.7D, Font = font });
            var value = new XRTableCell
            {
                Weight = 1D,
                Font = font,
                TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight,
                TextFormatString = "{0:##,###,##0.00}"
            };
            value.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", expression));
            row.Cells.Add(value);
            table.Rows.Add(row);
        }

        private static void AddBalanceSummaryRow(XRTable table)
        {
            var row = new XRTableRow();
            var font = new DevExpress.Drawing.DXFont("Segoe UI", 8F,
                DevExpress.Drawing.DXFontStyle.Bold);
            var caption = new XRTableCell { Weight = 1.7D, Font = font };
            caption.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "Iif([BALANCE_AMOUNT] < 0, 'Excess Advance', 'Balance Amount')"));
            row.Cells.Add(caption);

            var value = new XRTableCell
            {
                Weight = 1D,
                Font = font,
                TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight,
                TextFormatString = "{0:##,###,##0.00}"
            };
            value.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "Abs([BALANCE_AMOUNT])"));
            row.Cells.Add(value);
            table.Rows.Add(row);
        }

        internal static decimal CalculateBalance(decimal totalOrderAmount,
            IEnumerable<decimal?> oldMetalAmounts, IEnumerable<decimal?> receiptAmounts) =>
            totalOrderAmount - oldMetalAmounts.Sum(x => x ?? 0M) - receiptAmounts.Sum(x => x ?? 0M);

        private void CalculatedField1_GetValue(object sender, DevExpress.XtraReports.UI.GetValueEventArgs e)
        {
            var words = NumberToWords.Convert(
                GetCurrentColumnValue("TOTAL_ORDER_AMOUNT"));

            e.Value = string.Format(NumberToWordsFormat,
                string.IsNullOrEmpty(words) ? "NIL" : words);
        }
    }
}
