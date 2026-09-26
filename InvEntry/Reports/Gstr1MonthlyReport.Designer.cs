namespace InvEntry.Reports
{
    partial class Gstr1MonthlyReport
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Designer generated code

        private void InitializeComponent()
        {
            this.topMarginBand = new DevExpress.XtraReports.UI.TopMarginBand();
            this.bottomMarginBand = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.reportHeaderBand = new DevExpress.XtraReports.UI.ReportHeaderBand();
            this.titleLabel = new DevExpress.XtraReports.UI.XRLabel();
            this.companyNameLabel = new DevExpress.XtraReports.UI.XRLabel();
            this.branchLabel = new DevExpress.XtraReports.UI.XRLabel();
            this.gstinLabel = new DevExpress.XtraReports.UI.XRLabel();
            this.financialYearLabel = new DevExpress.XtraReports.UI.XRLabel();
            this.returnMonthLabel = new DevExpress.XtraReports.UI.XRLabel();
            this.scopeLabel = new DevExpress.XtraReports.UI.XRLabel();
            this.overallTotalsLabel = new DevExpress.XtraReports.UI.XRLabel();
            this.pageHeaderBand = new DevExpress.XtraReports.UI.PageHeaderBand();
            this.headerTable = new DevExpress.XtraReports.UI.XRTable();
            this.headerRow = new DevExpress.XtraReports.UI.XRTableRow();
            this.headerCategoryCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.headerGstr1TableCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.headerDocumentCountCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.headerDocumentNbrCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.headerDocumentDateCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.headerRecipientCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.headerInvoiceValueCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.headerTaxableValueCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.headerCgstCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.headerSgstCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.headerIgstCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.headerCessCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.detailCategoryCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.detailGstr1TableCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.detailDocumentCountCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.detailDocumentNbrCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.detailDocumentDateCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.detailRecipientCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.detailInvoiceValueCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.detailTaxableValueCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.detailCgstCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.detailSgstCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.detailIgstCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.detailCessCell = new DevExpress.XtraReports.UI.XRTableCell();
            this.detailBand = new DevExpress.XtraReports.UI.DetailBand();
            this.detailTable = new DevExpress.XtraReports.UI.XRTable();
            this.detailRow = new DevExpress.XtraReports.UI.XRTableRow();
            this.reportFooterBand = new DevExpress.XtraReports.UI.ReportFooterBand();
            this.reportTotalsLabel = new DevExpress.XtraReports.UI.XRLabel();
            this.pageFooterBand = new DevExpress.XtraReports.UI.PageFooterBand();
            this.generatedAtLabel = new DevExpress.XtraReports.UI.XRLabel();
            this.pageInfo = new DevExpress.XtraReports.UI.XRPageInfo();
            ((System.ComponentModel.ISupportInitialize)(this.headerTable)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.detailTable)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // topMarginBand
            // 
            this.topMarginBand.HeightF = 30F;
            this.topMarginBand.Name = "topMarginBand";
            // 
            // bottomMarginBand
            // 
            this.bottomMarginBand.HeightF = 30F;
            this.bottomMarginBand.Name = "bottomMarginBand";
            // 
            // reportHeaderBand
            // 
            this.reportHeaderBand.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.titleLabel,
            this.companyNameLabel,
            this.branchLabel,
            this.gstinLabel,
            this.financialYearLabel,
            this.returnMonthLabel,
            this.scopeLabel,
            this.overallTotalsLabel});
            this.reportHeaderBand.Expanded = false;
            this.reportHeaderBand.HeightF = 128F;
            this.reportHeaderBand.Name = "reportHeaderBand";
            // 
            // titleLabel
            // 
            this.titleLabel.Font = new DevExpress.Drawing.DXFont("Segoe UI", 16F, DevExpress.Drawing.DXFontStyle.Bold);
            this.titleLabel.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.SizeF = new System.Drawing.SizeF(1110F, 28F);
            this.titleLabel.Text = "GSTR-1 MONTHLY RETURN";
            this.titleLabel.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // companyNameLabel
            // 
            this.companyNameLabel.Font = new DevExpress.Drawing.DXFont("Segoe UI", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.companyNameLabel.LocationFloat = new DevExpress.Utils.PointFloat(0F, 30F);
            this.companyNameLabel.Name = "companyNameLabel";
            this.companyNameLabel.SizeF = new System.Drawing.SizeF(650F, 20F);
            this.companyNameLabel.Text = "Organisation";
            // 
            // branchLabel
            // 
            this.branchLabel.Font = new DevExpress.Drawing.DXFont("Segoe UI", 9F);
            this.branchLabel.LocationFloat = new DevExpress.Utils.PointFloat(650F, 30F);
            this.branchLabel.Name = "branchLabel";
            this.branchLabel.SizeF = new System.Drawing.SizeF(460F, 20F);
            this.branchLabel.Text = "Branch / Location:";
            // 
            // gstinLabel
            // 
            this.gstinLabel.Font = new DevExpress.Drawing.DXFont("Segoe UI", 9F);
            this.gstinLabel.LocationFloat = new DevExpress.Utils.PointFloat(0F, 52F);
            this.gstinLabel.Name = "gstinLabel";
            this.gstinLabel.SizeF = new System.Drawing.SizeF(370F, 18F);
            this.gstinLabel.Text = "GSTIN:";
            // 
            // financialYearLabel
            // 
            this.financialYearLabel.Font = new DevExpress.Drawing.DXFont("Segoe UI", 9F);
            this.financialYearLabel.LocationFloat = new DevExpress.Utils.PointFloat(370F, 52F);
            this.financialYearLabel.Name = "financialYearLabel";
            this.financialYearLabel.SizeF = new System.Drawing.SizeF(300F, 18F);
            this.financialYearLabel.Text = "Financial Year:";
            // 
            // returnMonthLabel
            // 
            this.returnMonthLabel.Font = new DevExpress.Drawing.DXFont("Segoe UI", 9F);
            this.returnMonthLabel.LocationFloat = new DevExpress.Utils.PointFloat(670F, 52F);
            this.returnMonthLabel.Name = "returnMonthLabel";
            this.returnMonthLabel.SizeF = new System.Drawing.SizeF(300F, 18F);
            this.returnMonthLabel.Text = "Return Month:";
            // 
            // scopeLabel
            // 
            this.scopeLabel.Font = new DevExpress.Drawing.DXFont("Segoe UI", 9F);
            this.scopeLabel.LocationFloat = new DevExpress.Utils.PointFloat(0F, 72F);
            this.scopeLabel.Name = "scopeLabel";
            this.scopeLabel.SizeF = new System.Drawing.SizeF(1110F, 18F);
            this.scopeLabel.Text = "Scope:";
            // 
            // overallTotalsLabel
            // 
            this.overallTotalsLabel.Font = new DevExpress.Drawing.DXFont("Segoe UI", 9F, DevExpress.Drawing.DXFontStyle.Bold);
            this.overallTotalsLabel.LocationFloat = new DevExpress.Utils.PointFloat(0F, 94F);
            this.overallTotalsLabel.Name = "overallTotalsLabel";
            this.overallTotalsLabel.SizeF = new System.Drawing.SizeF(1110F, 24F);
            this.overallTotalsLabel.Text = "Overall totals";
            this.overallTotalsLabel.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // pageHeaderBand
            // 
            this.pageHeaderBand.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.headerTable});
            this.pageHeaderBand.HeightF = 28F;
            this.pageHeaderBand.Name = "pageHeaderBand";
            // 
            // headerTable
            // 
            this.headerTable.BackColor = System.Drawing.Color.LightGray;
            this.headerTable.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.headerTable.BorderWidth = 0.5F;
            this.headerTable.Font = new DevExpress.Drawing.DXFont("Segoe UI", 7.5F, DevExpress.Drawing.DXFontStyle.Bold);
            this.headerTable.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.headerTable.Name = "headerTable";
            this.headerTable.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.headerRow});
            this.headerTable.SizeF = new System.Drawing.SizeF(1110F, 28F);
            // 
            // headerRow
            // 
            this.headerRow.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.headerCategoryCell,
            this.headerGstr1TableCell,
            this.headerDocumentCountCell,
            this.headerDocumentNbrCell,
            this.headerDocumentDateCell,
            this.headerRecipientCell,
            this.headerInvoiceValueCell,
            this.headerTaxableValueCell,
            this.headerCgstCell,
            this.headerSgstCell,
            this.headerIgstCell,
            this.headerCessCell});
            this.headerRow.Name = "headerRow";
            this.headerRow.Weight = 1D;
            // 
            // headerCategoryCell
            // 
            this.headerCategoryCell.Name = "headerCategoryCell";
            this.headerCategoryCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.headerCategoryCell.Text = "Category";
            this.headerCategoryCell.Weight = 90D;
            // 
            // headerGstr1TableCell
            // 
            this.headerGstr1TableCell.Name = "headerGstr1TableCell";
            this.headerGstr1TableCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.headerGstr1TableCell.Text = "Table";
            this.headerGstr1TableCell.Weight = 45D;
            // 
            // headerDocumentCountCell
            // 
            this.headerDocumentCountCell.Name = "headerDocumentCountCell";
            this.headerDocumentCountCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.headerDocumentCountCell.Text = "Docs";
            this.headerDocumentCountCell.Weight = 55D;
            // 
            // headerDocumentNbrCell
            // 
            this.headerDocumentNbrCell.Name = "headerDocumentNbrCell";
            this.headerDocumentNbrCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.headerDocumentNbrCell.Text = "Document No.";
            this.headerDocumentNbrCell.Weight = 100D;
            // 
            // headerDocumentDateCell
            // 
            this.headerDocumentDateCell.Name = "headerDocumentDateCell";
            this.headerDocumentDateCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.headerDocumentDateCell.Text = "Date";
            this.headerDocumentDateCell.Weight = 65D;
            // 
            // headerRecipientCell
            // 
            this.headerRecipientCell.Name = "headerRecipientCell";
            this.headerRecipientCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.headerRecipientCell.Text = "Recipient GSTIN / State";
            this.headerRecipientCell.Weight = 115D;
            // 
            // headerInvoiceValueCell
            // 
            this.headerInvoiceValueCell.Name = "headerInvoiceValueCell";
            this.headerInvoiceValueCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.headerInvoiceValueCell.Text = "Invoice Value";
            this.headerInvoiceValueCell.Weight = 90D;
            // 
            // headerTaxableValueCell
            // 
            this.headerTaxableValueCell.Name = "headerTaxableValueCell";
            this.headerTaxableValueCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.headerTaxableValueCell.Text = "Taxable";
            this.headerTaxableValueCell.Weight = 90D;
            // 
            // headerCgstCell
            // 
            this.headerCgstCell.Name = "headerCgstCell";
            this.headerCgstCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.headerCgstCell.Text = "CGST";
            this.headerCgstCell.Weight = 75D;
            // 
            // headerSgstCell
            // 
            this.headerSgstCell.Name = "headerSgstCell";
            this.headerSgstCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.headerSgstCell.Text = "SGST";
            this.headerSgstCell.Weight = 75D;
            // 
            // headerIgstCell
            // 
            this.headerIgstCell.Name = "headerIgstCell";
            this.headerIgstCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.headerIgstCell.Text = "IGST";
            this.headerIgstCell.Weight = 75D;
            // 
            // headerCessCell
            // 
            this.headerCessCell.Name = "headerCessCell";
            this.headerCessCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.headerCessCell.Text = "Cess";
            this.headerCessCell.Weight = 70D;
            // 
            // detailCategoryCell
            // 
            this.detailCategoryCell.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Category]")});
            this.detailCategoryCell.Name = "detailCategoryCell";
            this.detailCategoryCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.detailCategoryCell.Weight = 90D;
            // 
            // detailGstr1TableCell
            // 
            this.detailGstr1TableCell.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Gstr1Table]")});
            this.detailGstr1TableCell.Name = "detailGstr1TableCell";
            this.detailGstr1TableCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.detailGstr1TableCell.Weight = 45D;
            // 
            // detailDocumentCountCell
            // 
            this.detailDocumentCountCell.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[DocumentCount]")});
            this.detailDocumentCountCell.Name = "detailDocumentCountCell";
            this.detailDocumentCountCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.detailDocumentCountCell.TextFormatString = "{0:N0}";
            this.detailDocumentCountCell.Weight = 55D;
            // 
            // detailDocumentNbrCell
            // 
            this.detailDocumentNbrCell.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[DocumentNbr]")});
            this.detailDocumentNbrCell.Name = "detailDocumentNbrCell";
            this.detailDocumentNbrCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.detailDocumentNbrCell.Weight = 100D;
            // 
            // detailDocumentDateCell
            // 
            this.detailDocumentDateCell.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[DocumentDate]")});
            this.detailDocumentDateCell.Name = "detailDocumentDateCell";
            this.detailDocumentDateCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.detailDocumentDateCell.TextFormatString = "{0:dd-MMM-yyyy}";
            this.detailDocumentDateCell.Weight = 65D;
            // 
            // detailRecipientCell
            // 
            this.detailRecipientCell.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Recipient]")});
            this.detailRecipientCell.Name = "detailRecipientCell";
            this.detailRecipientCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.detailRecipientCell.Weight = 115D;
            // 
            // detailInvoiceValueCell
            // 
            this.detailInvoiceValueCell.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[InvoiceValue]")});
            this.detailInvoiceValueCell.Name = "detailInvoiceValueCell";
            this.detailInvoiceValueCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.detailInvoiceValueCell.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.detailInvoiceValueCell.TextFormatString = "{0:N2}";
            this.detailInvoiceValueCell.Weight = 90D;
            // 
            // detailTaxableValueCell
            // 
            this.detailTaxableValueCell.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[TaxableValue]")});
            this.detailTaxableValueCell.Name = "detailTaxableValueCell";
            this.detailTaxableValueCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.detailTaxableValueCell.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.detailTaxableValueCell.TextFormatString = "{0:N2}";
            this.detailTaxableValueCell.Weight = 90D;
            // 
            // detailCgstCell
            // 
            this.detailCgstCell.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CgstAmount]")});
            this.detailCgstCell.Name = "detailCgstCell";
            this.detailCgstCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.detailCgstCell.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.detailCgstCell.TextFormatString = "{0:N2}";
            this.detailCgstCell.Weight = 75D;
            // 
            // detailSgstCell
            // 
            this.detailSgstCell.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[SgstAmount]")});
            this.detailSgstCell.Name = "detailSgstCell";
            this.detailSgstCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.detailSgstCell.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.detailSgstCell.TextFormatString = "{0:N2}";
            this.detailSgstCell.Weight = 75D;
            // 
            // detailIgstCell
            // 
            this.detailIgstCell.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[IgstAmount]")});
            this.detailIgstCell.Name = "detailIgstCell";
            this.detailIgstCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.detailIgstCell.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.detailIgstCell.TextFormatString = "{0:N2}";
            this.detailIgstCell.Weight = 75D;
            // 
            // detailCessCell
            // 
            this.detailCessCell.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CessAmount]")});
            this.detailCessCell.Name = "detailCessCell";
            this.detailCessCell.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 1, 1, 96F);
            this.detailCessCell.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.detailCessCell.TextFormatString = "{0:N2}";
            this.detailCessCell.Weight = 70D;
            // 
            // detailBand
            // 
            this.detailBand.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.detailTable});
            this.detailBand.HeightF = 24F;
            this.detailBand.KeepTogether = true;
            this.detailBand.Name = "detailBand";
            // 
            // detailTable
            // 
            this.detailTable.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.detailTable.BorderWidth = 0.5F;
            this.detailTable.Font = new DevExpress.Drawing.DXFont("Segoe UI", 7.5F);
            this.detailTable.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.detailTable.Name = "detailTable";
            this.detailTable.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.detailRow});
            this.detailTable.SizeF = new System.Drawing.SizeF(1110F, 24F);
            // 
            // detailRow
            // 
            this.detailRow.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.detailCategoryCell,
            this.detailGstr1TableCell,
            this.detailDocumentCountCell,
            this.detailDocumentNbrCell,
            this.detailDocumentDateCell,
            this.detailRecipientCell,
            this.detailInvoiceValueCell,
            this.detailTaxableValueCell,
            this.detailCgstCell,
            this.detailSgstCell,
            this.detailIgstCell,
            this.detailCessCell});
            this.detailRow.Name = "detailRow";
            this.detailRow.Weight = 1D;
            // 
            // reportFooterBand
            // 
            this.reportFooterBand.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.reportTotalsLabel});
            this.reportFooterBand.HeightF = 34F;
            this.reportFooterBand.Name = "reportFooterBand";
            // 
            // reportTotalsLabel
            // 
            this.reportTotalsLabel.Font = new DevExpress.Drawing.DXFont("Segoe UI", 9F, DevExpress.Drawing.DXFontStyle.Bold);
            this.reportTotalsLabel.LocationFloat = new DevExpress.Utils.PointFloat(0F, 6F);
            this.reportTotalsLabel.Name = "reportTotalsLabel";
            this.reportTotalsLabel.SizeF = new System.Drawing.SizeF(1110F, 24F);
            this.reportTotalsLabel.Text = "TOTAL";
            this.reportTotalsLabel.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // pageFooterBand
            // 
            this.pageFooterBand.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.generatedAtLabel,
            this.pageInfo});
            this.pageFooterBand.HeightF = 25F;
            this.pageFooterBand.Name = "pageFooterBand";
            // 
            // generatedAtLabel
            // 
            this.generatedAtLabel.Font = new DevExpress.Drawing.DXFont("Segoe UI", 8F);
            this.generatedAtLabel.LocationFloat = new DevExpress.Utils.PointFloat(0F, 3F);
            this.generatedAtLabel.Name = "generatedAtLabel";
            this.generatedAtLabel.SizeF = new System.Drawing.SizeF(500F, 18F);
            this.generatedAtLabel.Text = "Generated:";
            // 
            // pageInfo
            // 
            this.pageInfo.Font = new DevExpress.Drawing.DXFont("Segoe UI", 8F);
            this.pageInfo.LocationFloat = new DevExpress.Utils.PointFloat(810F, 3F);
            this.pageInfo.Name = "pageInfo";
            this.pageInfo.SizeF = new System.Drawing.SizeF(300F, 18F);
            this.pageInfo.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.pageInfo.TextFormatString = "Page {0} of {1}";
            // 
            // Gstr1MonthlyReport
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.topMarginBand,
            this.bottomMarginBand,
            this.reportHeaderBand,
            this.pageHeaderBand,
            this.detailBand,
            this.reportFooterBand,
            this.pageFooterBand});
            this.Font = new DevExpress.Drawing.DXFont("Segoe UI", 9.75F);
            this.Landscape = true;
            this.Margins = new DevExpress.Drawing.DXMargins(25F, 25F, 30F, 30F);
            this.PageHeight = 827;
            this.PageWidth = 1169;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Version = "24.2";
            ((System.ComponentModel.ISupportInitialize)(this.headerTable)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.detailTable)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        #endregion

        private DevExpress.XtraReports.UI.TopMarginBand topMarginBand;
        private DevExpress.XtraReports.UI.BottomMarginBand bottomMarginBand;
        private DevExpress.XtraReports.UI.ReportHeaderBand reportHeaderBand;
        private DevExpress.XtraReports.UI.XRLabel titleLabel;
        private DevExpress.XtraReports.UI.XRLabel companyNameLabel;
        private DevExpress.XtraReports.UI.XRLabel branchLabel;
        private DevExpress.XtraReports.UI.XRLabel gstinLabel;
        private DevExpress.XtraReports.UI.XRLabel financialYearLabel;
        private DevExpress.XtraReports.UI.XRLabel returnMonthLabel;
        private DevExpress.XtraReports.UI.XRLabel scopeLabel;
        private DevExpress.XtraReports.UI.XRLabel overallTotalsLabel;
        private DevExpress.XtraReports.UI.PageHeaderBand pageHeaderBand;
        private DevExpress.XtraReports.UI.XRTable headerTable;
        private DevExpress.XtraReports.UI.XRTableRow headerRow;
        private DevExpress.XtraReports.UI.XRTableCell headerCategoryCell;
        private DevExpress.XtraReports.UI.XRTableCell headerGstr1TableCell;
        private DevExpress.XtraReports.UI.XRTableCell headerDocumentCountCell;
        private DevExpress.XtraReports.UI.XRTableCell headerDocumentNbrCell;
        private DevExpress.XtraReports.UI.XRTableCell headerDocumentDateCell;
        private DevExpress.XtraReports.UI.XRTableCell headerRecipientCell;
        private DevExpress.XtraReports.UI.XRTableCell headerInvoiceValueCell;
        private DevExpress.XtraReports.UI.XRTableCell headerTaxableValueCell;
        private DevExpress.XtraReports.UI.XRTableCell headerCgstCell;
        private DevExpress.XtraReports.UI.XRTableCell headerSgstCell;
        private DevExpress.XtraReports.UI.XRTableCell headerIgstCell;
        private DevExpress.XtraReports.UI.XRTableCell headerCessCell;
        private DevExpress.XtraReports.UI.XRTableCell detailCategoryCell;
        private DevExpress.XtraReports.UI.XRTableCell detailGstr1TableCell;
        private DevExpress.XtraReports.UI.XRTableCell detailDocumentCountCell;
        private DevExpress.XtraReports.UI.XRTableCell detailDocumentNbrCell;
        private DevExpress.XtraReports.UI.XRTableCell detailDocumentDateCell;
        private DevExpress.XtraReports.UI.XRTableCell detailRecipientCell;
        private DevExpress.XtraReports.UI.XRTableCell detailInvoiceValueCell;
        private DevExpress.XtraReports.UI.XRTableCell detailTaxableValueCell;
        private DevExpress.XtraReports.UI.XRTableCell detailCgstCell;
        private DevExpress.XtraReports.UI.XRTableCell detailSgstCell;
        private DevExpress.XtraReports.UI.XRTableCell detailIgstCell;
        private DevExpress.XtraReports.UI.XRTableCell detailCessCell;
        private DevExpress.XtraReports.UI.DetailBand detailBand;
        private DevExpress.XtraReports.UI.XRTable detailTable;
        private DevExpress.XtraReports.UI.XRTableRow detailRow;
        private DevExpress.XtraReports.UI.ReportFooterBand reportFooterBand;
        private DevExpress.XtraReports.UI.XRLabel reportTotalsLabel;
        private DevExpress.XtraReports.UI.PageFooterBand pageFooterBand;
        private DevExpress.XtraReports.UI.XRLabel generatedAtLabel;
        private DevExpress.XtraReports.UI.XRPageInfo pageInfo;
    }
}
