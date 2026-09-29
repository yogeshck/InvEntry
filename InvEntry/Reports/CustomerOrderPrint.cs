using DevExpress.XtraReports.UI;
using DevExpress.DataAccess;
using DevExpress.DataAccess.Sql;
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
            AddCustomerOrderSettlementQueries();
            AddCustomerOrderSettlementSections();
        }

        private void AddCustomerOrderSettlementQueries()
        {
            sqlDataSource1.Queries.Add(CreateOrderQuery("CUSTOMER_ORDER_OLD_METAL", """
                select OMT.GKEY, OMT.TRANS_NBR, OMT.PRODUCT_CATEGORY, OMT.METAL, OMT.PURITY,
                       OMT.TRANSACTED_RATE, OMT.GROSS_WEIGHT, OMT.STONE_WEIGHT, OMT.NET_WEIGHT,
                       OMT.FINAL_PURCHASE_PRICE
                  from dbo.OLD_METAL_TRANSACTION OMT
                  join dbo.CUSTOMER_ORDER CO on CO.GKEY = OMT.DOC_REF_GKEY
                 where CO.ORDER_NBR = @paramOrderNbr
                   and OMT.DOC_REF_NBR = @paramOrderNbr
                   and upper(ltrim(rtrim(OMT.DOC_REF_TYPE))) = 'CUSTOMER ORDER'
                 order by OMT.GKEY
                """));

            sqlDataSource1.Queries.Add(CreateOrderQuery("CUSTOMER_ORDER_RECEIPTS", """
                select V.GKEY, V.VOUCHER_NBR, V.VOUCHER_DATE, V.MODE, V.VOUCHER_TYPE,
                       V.TRANS_TYPE, V.TRANS_AMOUNT
                  from dbo.VOUCHER V
                  join dbo.CUSTOMER_ORDER CO on CO.GKEY = V.REF_DOC_GKEY
                 where CO.ORDER_NBR = @paramOrderNbr
                   and V.REF_DOC_NBR = @paramOrderNbr
                   and upper(ltrim(rtrim(V.TRANS_TYPE))) = 'RECEIPT'
                   and upper(ltrim(rtrim(V.VOUCHER_TYPE))) = 'ADVANCE RECEIPT'
                 order by V.VOUCHER_DATE, V.SEQ_NBR, V.GKEY
                """));

            sqlDataSource1.Queries.Add(CreateOrderQuery("CUSTOMER_ORDER_SETTLEMENT", """
                select CO.TOTAL_ORDER_AMOUNT,
                       coalesce(OM.OLD_METAL_TOTAL, 0) as OLD_METAL_TOTAL,
                       coalesce(RC.ADVANCE_RECEIPT_TOTAL, 0) as ADVANCE_RECEIPT_TOTAL,
                       coalesce(CO.TOTAL_ORDER_AMOUNT, 0)
                         - coalesce(OM.OLD_METAL_TOTAL, 0)
                         - coalesce(RC.ADVANCE_RECEIPT_TOTAL, 0) as BALANCE_AMOUNT
                  from dbo.CUSTOMER_ORDER CO
                  outer apply (
                       select sum(OMT.FINAL_PURCHASE_PRICE) as OLD_METAL_TOTAL
                         from dbo.OLD_METAL_TRANSACTION OMT
                        where OMT.DOC_REF_GKEY = CO.GKEY
                          and OMT.DOC_REF_NBR = @paramOrderNbr
                          and upper(ltrim(rtrim(OMT.DOC_REF_TYPE))) = 'CUSTOMER ORDER'
                  ) OM
                  outer apply (
                       select sum(V.TRANS_AMOUNT) as ADVANCE_RECEIPT_TOTAL
                         from dbo.VOUCHER V
                        where V.REF_DOC_GKEY = CO.GKEY
                          and V.REF_DOC_NBR = @paramOrderNbr
                          and upper(ltrim(rtrim(V.TRANS_TYPE))) = 'RECEIPT'
                          and upper(ltrim(rtrim(V.VOUCHER_TYPE))) = 'ADVANCE RECEIPT'
                  ) RC
                 where CO.ORDER_NBR = @paramOrderNbr
                """));
        }

        private static CustomSqlQuery CreateOrderQuery(string name, string sql)
        {
            var query = new CustomSqlQuery(name, sql);
            query.Parameters.Add(new QueryParameter
            {
                Name = "paramOrderNbr",
                Type = typeof(Expression),
                Value = new Expression("?pOrderNbr", typeof(string))
            });
            return query;
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
            AddSummaryRow(table, "Less: Old Metal", "[OLD_METAL_TOTAL]", false,
                "[OLD_METAL_TOTAL] != 0");
            AddSummaryRow(table, "Less: Advance / Receipts", "[ADVANCE_RECEIPT_TOTAL]", false,
                "[ADVANCE_RECEIPT_TOTAL] != 0");
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

        private static void AddSummaryRow(XRTable table, string caption, string expression, bool bold,
            string visibilityExpression = null)
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
            if (!string.IsNullOrEmpty(visibilityExpression))
                row.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Visible", visibilityExpression));
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
