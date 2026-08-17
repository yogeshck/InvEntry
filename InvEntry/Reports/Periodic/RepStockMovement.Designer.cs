namespace InvEntry.Reports.Periodic
{
	partial class RepStockMovement
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery1 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter1 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter2 = new DevExpress.DataAccess.Sql.QueryParameter();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RepStockMovement));
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrTable2 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow3 = new DevExpress.XtraReports.UI.XRTableRow();
            this.Product = new DevExpress.XtraReports.UI.XRTableCell();
            this.Ob_Wt = new DevExpress.XtraReports.UI.XRTableCell();
            this.Ob_Qty = new DevExpress.XtraReports.UI.XRTableCell();
            this.StkOut_Wt = new DevExpress.XtraReports.UI.XRTableCell();
            this.StkOut_Qty = new DevExpress.XtraReports.UI.XRTableCell();
            this.StkIn_Wt = new DevExpress.XtraReports.UI.XRTableCell();
            this.StkIn_Qty = new DevExpress.XtraReports.UI.XRTableCell();
            this.Cb_Wt = new DevExpress.XtraReports.UI.XRTableCell();
            this.Cb_Qty = new DevExpress.XtraReports.UI.XRTableCell();
            this.sqlDataSource1 = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.GroupHeader1 = new DevExpress.XtraReports.UI.GroupHeaderBand();
            this.xrTable1 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow1 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell1 = new DevExpress.XtraReports.UI.XRTableCell();
            this.Hdr_Opening = new DevExpress.XtraReports.UI.XRTableCell();
            this.Hdr_StockOut = new DevExpress.XtraReports.UI.XRTableCell();
            this.Hdr_StockIn = new DevExpress.XtraReports.UI.XRTableCell();
            this.Hdr_Closing = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell14 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell16 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow2 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell4 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell5 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell6 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell9 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell11 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell15 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell17 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell20 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell21 = new DevExpress.XtraReports.UI.XRTableCell();
            this.pFromDate = new DevExpress.XtraReports.Parameters.Parameter();
            this.pToDate = new DevExpress.XtraReports.Parameters.Parameter();
            this.ReportHeader = new DevExpress.XtraReports.UI.ReportHeaderBand();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // TopMargin
            // 
            this.TopMargin.Name = "TopMargin";
            // 
            // BottomMargin
            // 
            this.BottomMargin.HeightF = 198.3333F;
            this.BottomMargin.Name = "BottomMargin";
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrTable2});
            this.Detail.HeightF = 31.66667F;
            this.Detail.Name = "Detail";
            // 
            // xrTable2
            // 
            this.xrTable2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrTable2.Name = "xrTable2";
            this.xrTable2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable2.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow3});
            this.xrTable2.SizeF = new System.Drawing.SizeF(649.0491F, 25F);
            // 
            // xrTableRow3
            // 
            this.xrTableRow3.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.Product,
            this.Ob_Wt,
            this.Ob_Qty,
            this.StkOut_Wt,
            this.StkOut_Qty,
            this.StkIn_Wt,
            this.StkIn_Qty,
            this.Cb_Wt,
            this.Cb_Qty});
            this.xrTableRow3.Name = "xrTableRow3";
            this.xrTableRow3.Weight = 11.5D;
            // 
            // Product
            // 
            this.Product.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[PRODUCT_CATEGORY]")});
            this.Product.Font = new DevExpress.Drawing.DXFont("Arial", 8F);
            this.Product.Multiline = true;
            this.Product.Name = "Product";
            this.Product.StylePriority.UseFont = false;
            this.Product.Text = "Product";
            this.Product.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.Product.Weight = 0.2681587852060362D;
            // 
            // Ob_Wt
            // 
            this.Ob_Wt.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "0")});
            this.Ob_Wt.Font = new DevExpress.Drawing.DXFont("Arial", 8F);
            this.Ob_Wt.Multiline = true;
            this.Ob_Wt.Name = "Ob_Wt";
            this.Ob_Wt.StylePriority.UseFont = false;
            this.Ob_Wt.Text = "Ob_Wt";
            this.Ob_Wt.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.Ob_Wt.Weight = 0.32134798255982855D;
            // 
            // Ob_Qty
            // 
            this.Ob_Qty.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "0")});
            this.Ob_Qty.Font = new DevExpress.Drawing.DXFont("Arial", 8F);
            this.Ob_Qty.Multiline = true;
            this.Ob_Qty.Name = "Ob_Qty";
            this.Ob_Qty.StylePriority.UseFont = false;
            this.Ob_Qty.Text = "Ob_Qty";
            this.Ob_Qty.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.Ob_Qty.Weight = 0.17672622428571075D;
            // 
            // StkOut_Wt
            // 
            this.StkOut_Wt.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[STOCKOUT_NETWT]")});
            this.StkOut_Wt.Font = new DevExpress.Drawing.DXFont("Arial", 8F);
            this.StkOut_Wt.Multiline = true;
            this.StkOut_Wt.Name = "StkOut_Wt";
            this.StkOut_Wt.StylePriority.UseFont = false;
            this.StkOut_Wt.Text = "StkOut_Wt";
            this.StkOut_Wt.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.StkOut_Wt.Weight = 0.36676719225917859D;
            // 
            // StkOut_Qty
            // 
            this.StkOut_Qty.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[STOCKOUT_QTY]")});
            this.StkOut_Qty.Font = new DevExpress.Drawing.DXFont("Arial", 8F);
            this.StkOut_Qty.Multiline = true;
            this.StkOut_Qty.Name = "StkOut_Qty";
            this.StkOut_Qty.StylePriority.UseFont = false;
            this.StkOut_Qty.Text = "StkOut_Qty";
            this.StkOut_Qty.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.StkOut_Qty.Weight = 0.19001098364175906D;
            // 
            // StkIn_Wt
            // 
            this.StkIn_Wt.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[STOCKIN_NETWT]")});
            this.StkIn_Wt.Font = new DevExpress.Drawing.DXFont("Arial", 8F);
            this.StkIn_Wt.Multiline = true;
            this.StkIn_Wt.Name = "StkIn_Wt";
            this.StkIn_Wt.StylePriority.UseFont = false;
            this.StkIn_Wt.Text = "StkIn_Wt";
            this.StkIn_Wt.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.StkIn_Wt.Weight = 0.37485598123107611D;
            // 
            // StkIn_Qty
            // 
            this.StkIn_Qty.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[STOCKIN_QTY]")});
            this.StkIn_Qty.Font = new DevExpress.Drawing.DXFont("Arial", 8F);
            this.StkIn_Qty.Multiline = true;
            this.StkIn_Qty.Name = "StkIn_Qty";
            this.StkIn_Qty.StylePriority.UseFont = false;
            this.StkIn_Qty.Text = "StkIn_Qty";
            this.StkIn_Qty.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.StkIn_Qty.Weight = 0.19453275636141407D;
            // 
            // Cb_Wt
            // 
            this.Cb_Wt.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "0")});
            this.Cb_Wt.Font = new DevExpress.Drawing.DXFont("Arial", 8F);
            this.Cb_Wt.Multiline = true;
            this.Cb_Wt.Name = "Cb_Wt";
            this.Cb_Wt.StylePriority.UseFont = false;
            this.Cb_Wt.Text = "Cb_Wt";
            this.Cb_Wt.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.Cb_Wt.Weight = 0.34824344692533438D;
            // 
            // Cb_Qty
            // 
            this.Cb_Qty.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "0")});
            this.Cb_Qty.Font = new DevExpress.Drawing.DXFont("Arial", 8F);
            this.Cb_Qty.Multiline = true;
            this.Cb_Qty.Name = "Cb_Qty";
            this.Cb_Qty.StylePriority.UseFont = false;
            this.Cb_Qty.Text = "Cb_Qty";
            this.Cb_Qty.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.Cb_Qty.Weight = 0.21729371001336861D;
            // 
            // sqlDataSource1
            // 
            this.sqlDataSource1.ConnectionName = "ReportDBCon01";
            this.sqlDataSource1.Name = "sqlDataSource1";
            customSqlQuery1.Name = "Query";
            queryParameter1.Name = "pFrDate";
            queryParameter1.Type = typeof(global::DevExpress.DataAccess.Expression);
            queryParameter1.Value = new DevExpress.DataAccess.Expression("?pFromDate", typeof(System.DateOnly));
            queryParameter2.Name = "pToDate";
            queryParameter2.Type = typeof(global::DevExpress.DataAccess.Expression);
            queryParameter2.Value = new DevExpress.DataAccess.Expression("?pToDate", typeof(System.DateOnly));
            customSqlQuery1.Parameters.AddRange(new DevExpress.DataAccess.Sql.QueryParameter[] {
            queryParameter1,
            queryParameter2});
            customSqlQuery1.Sql = resources.GetString("customSqlQuery1.Sql");
            this.sqlDataSource1.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            customSqlQuery1});
            this.sqlDataSource1.ResultSchemaSerializable = resources.GetString("sqlDataSource1.ResultSchemaSerializable");
            // 
            // GroupHeader1
            // 
            this.GroupHeader1.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrTable1});
            this.GroupHeader1.Expanded = false;
            this.GroupHeader1.HeightF = 48.33333F;
            this.GroupHeader1.Name = "GroupHeader1";
            // 
            // xrTable1
            // 
            this.xrTable1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrTable1.Name = "xrTable1";
            this.xrTable1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable1.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow1,
            this.xrTableRow2});
            this.xrTable1.SizeF = new System.Drawing.SizeF(650F, 35F);
            // 
            // xrTableRow1
            // 
            this.xrTableRow1.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell1,
            this.Hdr_Opening,
            this.Hdr_StockOut,
            this.Hdr_StockIn,
            this.Hdr_Closing,
            this.xrTableCell14,
            this.xrTableCell16});
            this.xrTableRow1.Name = "xrTableRow1";
            this.xrTableRow1.Weight = 1D;
            // 
            // xrTableCell1
            // 
            this.xrTableCell1.Multiline = true;
            this.xrTableCell1.Name = "xrTableCell1";
            this.xrTableCell1.Weight = 0.6059684398991283D;
            // 
            // Hdr_Opening
            // 
            this.Hdr_Opening.Font = new DevExpress.Drawing.DXFont("Arial", 8F, DevExpress.Drawing.DXFontStyle.Bold);
            this.Hdr_Opening.Multiline = true;
            this.Hdr_Opening.Name = "Hdr_Opening";
            this.Hdr_Opening.StylePriority.UseFont = false;
            this.Hdr_Opening.StylePriority.UseTextAlignment = false;
            this.Hdr_Opening.Text = "Opening";
            this.Hdr_Opening.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.Hdr_Opening.Weight = 1.125516199861178D;
            // 
            // Hdr_StockOut
            // 
            this.Hdr_StockOut.Font = new DevExpress.Drawing.DXFont("Arial", 8F, DevExpress.Drawing.DXFontStyle.Bold);
            this.Hdr_StockOut.Multiline = true;
            this.Hdr_StockOut.Name = "Hdr_StockOut";
            this.Hdr_StockOut.StylePriority.UseFont = false;
            this.Hdr_StockOut.StylePriority.UseTextAlignment = false;
            this.Hdr_StockOut.Text = "Stock Out";
            this.Hdr_StockOut.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.Hdr_StockOut.Weight = 1.2581726313298813D;
            // 
            // Hdr_StockIn
            // 
            this.Hdr_StockIn.Font = new DevExpress.Drawing.DXFont("Arial", 8F, DevExpress.Drawing.DXFontStyle.Bold);
            this.Hdr_StockIn.Multiline = true;
            this.Hdr_StockIn.Name = "Hdr_StockIn";
            this.Hdr_StockIn.StylePriority.UseFont = false;
            this.Hdr_StockIn.StylePriority.UseTextAlignment = false;
            this.Hdr_StockIn.Text = "Stock In";
            this.Hdr_StockIn.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.Hdr_StockIn.Weight = 1.2866684130029631D;
            // 
            // Hdr_Closing
            // 
            this.Hdr_Closing.Font = new DevExpress.Drawing.DXFont("Arial", 8F, DevExpress.Drawing.DXFontStyle.Bold);
            this.Hdr_Closing.Multiline = true;
            this.Hdr_Closing.Name = "Hdr_Closing";
            this.Hdr_Closing.StylePriority.UseFont = false;
            this.Hdr_Closing.StylePriority.UseTextAlignment = false;
            this.Hdr_Closing.Text = "Closing";
            this.Hdr_Closing.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.Hdr_Closing.Weight = 1.2608500323140226D;
            // 
            // xrTableCell14
            // 
            this.xrTableCell14.Multiline = true;
            this.xrTableCell14.Name = "xrTableCell14";
            this.xrTableCell14.Text = "xrTableCell14";
            this.xrTableCell14.Weight = 0.012626026441128934D;
            // 
            // xrTableCell16
            // 
            this.xrTableCell16.Multiline = true;
            this.xrTableCell16.Name = "xrTableCell16";
            this.xrTableCell16.Text = "xrTableCell16";
            this.xrTableCell16.Weight = 0.012626026441128934D;
            // 
            // xrTableRow2
            // 
            this.xrTableRow2.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell4,
            this.xrTableCell5,
            this.xrTableCell6,
            this.xrTableCell9,
            this.xrTableCell11,
            this.xrTableCell15,
            this.xrTableCell17,
            this.xrTableCell20,
            this.xrTableCell21});
            this.xrTableRow2.Name = "xrTableRow2";
            this.xrTableRow2.Weight = 1D;
            // 
            // xrTableCell4
            // 
            this.xrTableCell4.Font = new DevExpress.Drawing.DXFont("Arial", 8F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell4.Multiline = true;
            this.xrTableCell4.Name = "xrTableCell4";
            this.xrTableCell4.StylePriority.UseFont = false;
            this.xrTableCell4.StylePriority.UseTextAlignment = false;
            this.xrTableCell4.Text = "Product";
            this.xrTableCell4.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell4.Weight = 0.77559246879316257D;
            // 
            // xrTableCell5
            // 
            this.xrTableCell5.Font = new DevExpress.Drawing.DXFont("Arial", 8F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell5.Multiline = true;
            this.xrTableCell5.Name = "xrTableCell5";
            this.xrTableCell5.StylePriority.UseFont = false;
            this.xrTableCell5.StylePriority.UseTextAlignment = false;
            this.xrTableCell5.Text = "Wt.";
            this.xrTableCell5.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell5.Weight = 0.9294302336143121D;
            // 
            // xrTableCell6
            // 
            this.xrTableCell6.Font = new DevExpress.Drawing.DXFont("Arial", 8F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell6.Multiline = true;
            this.xrTableCell6.Name = "xrTableCell6";
            this.xrTableCell6.StylePriority.UseFont = false;
            this.xrTableCell6.StylePriority.UseTextAlignment = false;
            this.xrTableCell6.Text = "Qty";
            this.xrTableCell6.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell6.Weight = 0.51114303741369926D;
            // 
            // xrTableCell9
            // 
            this.xrTableCell9.Font = new DevExpress.Drawing.DXFont("Arial", 8F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell9.Multiline = true;
            this.xrTableCell9.Name = "xrTableCell9";
            this.xrTableCell9.StylePriority.UseFont = false;
            this.xrTableCell9.StylePriority.UseTextAlignment = false;
            this.xrTableCell9.Text = "Wt";
            this.xrTableCell9.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell9.Weight = 1.0607953293042871D;
            // 
            // xrTableCell11
            // 
            this.xrTableCell11.Font = new DevExpress.Drawing.DXFont("Arial", 8F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell11.Multiline = true;
            this.xrTableCell11.Name = "xrTableCell11";
            this.xrTableCell11.StylePriority.UseFont = false;
            this.xrTableCell11.StylePriority.UseTextAlignment = false;
            this.xrTableCell11.Text = "Qty";
            this.xrTableCell11.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell11.Weight = 0.54956684132853451D;
            // 
            // xrTableCell15
            // 
            this.xrTableCell15.Font = new DevExpress.Drawing.DXFont("Arial", 8F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell15.Multiline = true;
            this.xrTableCell15.Name = "xrTableCell15";
            this.xrTableCell15.StylePriority.UseFont = false;
            this.xrTableCell15.StylePriority.UseTextAlignment = false;
            this.xrTableCell15.Text = "Wt.";
            this.xrTableCell15.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell15.Weight = 1.08419141535812D;
            // 
            // xrTableCell17
            // 
            this.xrTableCell17.Font = new DevExpress.Drawing.DXFont("Arial", 8F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell17.Multiline = true;
            this.xrTableCell17.Name = "xrTableCell17";
            this.xrTableCell17.StylePriority.UseFont = false;
            this.xrTableCell17.StylePriority.UseTextAlignment = false;
            this.xrTableCell17.Text = "Qty";
            this.xrTableCell17.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell17.Weight = 0.56264447902999226D;
            // 
            // xrTableCell20
            // 
            this.xrTableCell20.Font = new DevExpress.Drawing.DXFont("Arial", 8F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell20.Multiline = true;
            this.xrTableCell20.Name = "xrTableCell20";
            this.xrTableCell20.StylePriority.UseFont = false;
            this.xrTableCell20.StylePriority.UseTextAlignment = false;
            this.xrTableCell20.Text = "Wt.";
            this.xrTableCell20.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell20.Weight = 1.0072195650966123D;
            // 
            // xrTableCell21
            // 
            this.xrTableCell21.Font = new DevExpress.Drawing.DXFont("Arial", 8F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell21.Multiline = true;
            this.xrTableCell21.Name = "xrTableCell21";
            this.xrTableCell21.StylePriority.UseFont = false;
            this.xrTableCell21.StylePriority.UseTextAlignment = false;
            this.xrTableCell21.Text = "Qty";
            this.xrTableCell21.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell21.Weight = 0.63889140249083542D;
            // 
            // pFromDate
            // 
            this.pFromDate.Description = "From Date";
            this.pFromDate.Name = "pFromDate";
            this.pFromDate.Type = typeof(global::System.DateOnly);
            this.pFromDate.ValueInfo = "2026-04-01";
            // 
            // pToDate
            // 
            this.pToDate.Description = "To Date";
            this.pToDate.Name = "pToDate";
            this.pToDate.Type = typeof(global::System.DateOnly);
            this.pToDate.ValueInfo = "2026-04-29";
            // 
            // ReportHeader
            // 
            this.ReportHeader.HeightF = 21.66667F;
            this.ReportHeader.Name = "ReportHeader";
            // 
            // RepStockMovement
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.TopMargin,
            this.BottomMargin,
            this.Detail,
            this.GroupHeader1,
            this.ReportHeader});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.sqlDataSource1});
            this.DataMember = "Query";
            this.DataSource = this.sqlDataSource1;
            this.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.Margins = new DevExpress.Drawing.DXMargins(100F, 100F, 100F, 198.3333F);
            this.ParameterPanelLayoutItems.AddRange(new DevExpress.XtraReports.Parameters.ParameterPanelLayoutItem[] {
            new DevExpress.XtraReports.Parameters.ParameterLayoutItem(this.pFromDate, DevExpress.XtraReports.Parameters.Orientation.Horizontal),
            new DevExpress.XtraReports.Parameters.ParameterLayoutItem(this.pToDate, DevExpress.XtraReports.Parameters.Orientation.Horizontal)});
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.pFromDate,
            this.pToDate});
            this.Version = "24.2";
            ((System.ComponentModel.ISupportInitialize)(this.xrTable2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

		}

        #endregion

        private DevExpress.XtraReports.UI.TopMarginBand TopMargin;
        private DevExpress.XtraReports.UI.BottomMarginBand BottomMargin;
        private DevExpress.XtraReports.UI.DetailBand Detail;
        private DevExpress.DataAccess.Sql.SqlDataSource sqlDataSource1;
        private DevExpress.XtraReports.UI.GroupHeaderBand GroupHeader1;
        private DevExpress.XtraReports.UI.XRTable xrTable2;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow3;
        private DevExpress.XtraReports.UI.XRTableCell Product;
        private DevExpress.XtraReports.UI.XRTableCell Ob_Qty;
        private DevExpress.XtraReports.UI.XRTable xrTable1;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow1;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell1;
        private DevExpress.XtraReports.UI.XRTableCell Hdr_Opening;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow2;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell4;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell5;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell6;
        private DevExpress.XtraReports.UI.XRTableCell StkOut_Wt;
        private DevExpress.XtraReports.UI.XRTableCell StkOut_Qty;
        private DevExpress.XtraReports.UI.XRTableCell Hdr_StockOut;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell9;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell11;
        private DevExpress.XtraReports.UI.XRTableCell Hdr_StockIn;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell14;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell16;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell15;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell17;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell20;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell21;
        private DevExpress.XtraReports.UI.XRTableCell Hdr_Closing;
        private DevExpress.XtraReports.UI.XRTableCell StkIn_Wt;
        private DevExpress.XtraReports.UI.XRTableCell StkIn_Qty;
        private DevExpress.XtraReports.UI.XRTableCell Cb_Wt;
        private DevExpress.XtraReports.UI.XRTableCell Cb_Qty;
        private DevExpress.XtraReports.UI.XRTableCell Ob_Wt;
        private DevExpress.XtraReports.Parameters.Parameter pFromDate;
        private DevExpress.XtraReports.Parameters.Parameter pToDate;
        private DevExpress.XtraReports.UI.ReportHeaderBand ReportHeader;
    }
}
