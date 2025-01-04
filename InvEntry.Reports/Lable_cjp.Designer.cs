namespace InvEntry.Reports
{
    partial class Lable_cjp
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
            this.Area3 = new DevExpress.XtraReports.UI.DetailBand();
            this.Area1 = new DevExpress.XtraReports.UI.ReportHeaderBand();
            this.Area2 = new DevExpress.XtraReports.UI.PageHeaderBand();
            this.Area4 = new DevExpress.XtraReports.UI.ReportFooterBand();
            this.Area5 = new DevExpress.XtraReports.UI.PageFooterBand();
            this.pcode1 = new DevExpress.XtraReports.UI.XRLabel();
            this.pcode2 = new DevExpress.XtraReports.UI.XRLabel();
            this.dt1 = new DevExpress.XtraReports.UI.XRLabel();
            this.pn1 = new DevExpress.XtraReports.UI.XRLabel();
            this.mc_1 = new DevExpress.XtraReports.UI.XRLabel();
            this.wst_1 = new DevExpress.XtraReports.UI.XRLabel();
            this.wts_1 = new DevExpress.XtraReports.UI.XRLabel();
            this.Text1 = new DevExpress.XtraReports.UI.XRLabel();
            this.wts = new DevExpress.XtraReports.UI.CalculatedField();
            this.mc = new DevExpress.XtraReports.UI.CalculatedField();
            this.wst = new DevExpress.XtraReports.UI.CalculatedField();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // Area3
            // 
            this.Area3.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.pcode1,
            this.pcode2,
            this.dt1,
            this.pn1,
            this.mc_1,
            this.wst_1,
            this.wts_1,
            this.Text1});
            this.Area3.HeightF = 51F;
            this.Area3.KeepTogether = true;
            this.Area3.Name = "Area3";
            this.Area3.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Area3.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // Area1
            // 
            this.Area1.HeightF = 0F;
            this.Area1.KeepTogether = true;
            this.Area1.Name = "Area1";
            this.Area1.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Area1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.Area1.Visible = false;
            // 
            // Area2
            // 
            this.Area2.HeightF = 0F;
            this.Area2.Name = "Area2";
            this.Area2.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Area2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // Area4
            // 
            this.Area4.HeightF = 0F;
            this.Area4.KeepTogether = true;
            this.Area4.Name = "Area4";
            this.Area4.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Area4.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.Area4.Visible = false;
            // 
            // Area5
            // 
            this.Area5.HeightF = 15F;
            this.Area5.Name = "Area5";
            this.Area5.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Area5.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // pcode1
            // 
            this.pcode1.BackColor = System.Drawing.Color.Transparent;
            this.pcode1.BorderColor = System.Drawing.Color.Black;
            this.pcode1.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.pcode1.BorderWidth = 1F;
            this.pcode1.CanGrow = false;
            this.pcode1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[pcode]")});
            this.pcode1.Font = new DevExpress.Drawing.DXFont("Microsoft Sans Serif", 15F);
            this.pcode1.ForeColor = System.Drawing.Color.Black;
            this.pcode1.LocationFloat = new DevExpress.Utils.PointFloat(183.3333F, 4.166667F);
            this.pcode1.Name = "pcode1";
            this.pcode1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.pcode1.SizeF = new System.Drawing.SizeF(91.66666F, 19.44444F);
            this.pcode1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // pcode2
            // 
            this.pcode2.BackColor = System.Drawing.Color.Transparent;
            this.pcode2.BorderColor = System.Drawing.Color.Black;
            this.pcode2.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.pcode2.BorderWidth = 1F;
            this.pcode2.CanGrow = false;
            this.pcode2.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[pcode]")});
            this.pcode2.Font = new DevExpress.Drawing.DXFont("Arial", 6F, DevExpress.Drawing.DXFontStyle.Bold);
            this.pcode2.ForeColor = System.Drawing.Color.Black;
            this.pcode2.LocationFloat = new DevExpress.Utils.PointFloat(183.3333F, 25F);
            this.pcode2.Name = "pcode2";
            this.pcode2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.pcode2.SizeF = new System.Drawing.SizeF(58.33333F, 9.027778F);
            this.pcode2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // dt1
            // 
            this.dt1.BackColor = System.Drawing.Color.Transparent;
            this.dt1.BorderColor = System.Drawing.Color.Black;
            this.dt1.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.dt1.BorderWidth = 1F;
            this.dt1.CanGrow = false;
            this.dt1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[dt]")});
            this.dt1.Font = new DevExpress.Drawing.DXFont("Arial", 6F, DevExpress.Drawing.DXFontStyle.Bold);
            this.dt1.ForeColor = System.Drawing.Color.Black;
            this.dt1.LocationFloat = new DevExpress.Utils.PointFloat(241.6667F, 25F);
            this.dt1.Name = "dt1";
            this.dt1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.dt1.SizeF = new System.Drawing.SizeF(25F, 9.027778F);
            this.dt1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            // 
            // pn1
            // 
            this.pn1.BackColor = System.Drawing.Color.Transparent;
            this.pn1.BorderColor = System.Drawing.Color.Black;
            this.pn1.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.pn1.BorderWidth = 1F;
            this.pn1.CanGrow = false;
            this.pn1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[pn]")});
            this.pn1.Font = new DevExpress.Drawing.DXFont("Arial", 7F, DevExpress.Drawing.DXFontStyle.Bold);
            this.pn1.ForeColor = System.Drawing.Color.Black;
            this.pn1.LocationFloat = new DevExpress.Utils.PointFloat(283.3333F, 5.208333F);
            this.pn1.Name = "pn1";
            this.pn1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.pn1.SizeF = new System.Drawing.SizeF(100F, 10.41667F);
            this.pn1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // mc_1
            // 
            this.mc_1.BackColor = System.Drawing.Color.Transparent;
            this.mc_1.BorderColor = System.Drawing.Color.Black;
            this.mc_1.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.mc_1.BorderWidth = 1F;
            this.mc_1.CanGrow = false;
            this.mc_1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[mc]")});
            this.mc_1.Font = new DevExpress.Drawing.DXFont("Arial", 7F, DevExpress.Drawing.DXFontStyle.Bold);
            this.mc_1.ForeColor = System.Drawing.Color.Black;
            this.mc_1.LocationFloat = new DevExpress.Utils.PointFloat(283.3333F, 27.43056F);
            this.mc_1.Name = "mc_1";
            this.mc_1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.mc_1.SizeF = new System.Drawing.SizeF(100F, 11.11111F);
            this.mc_1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // wst_1
            // 
            this.wst_1.BackColor = System.Drawing.Color.Transparent;
            this.wst_1.BorderColor = System.Drawing.Color.Black;
            this.wst_1.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.wst_1.BorderWidth = 1F;
            this.wst_1.CanGrow = false;
            this.wst_1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[wst]")});
            this.wst_1.Font = new DevExpress.Drawing.DXFont("Arial", 7F, DevExpress.Drawing.DXFontStyle.Bold);
            this.wst_1.ForeColor = System.Drawing.Color.Black;
            this.wst_1.LocationFloat = new DevExpress.Utils.PointFloat(283.3333F, 38.19444F);
            this.wst_1.Name = "wst_1";
            this.wst_1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.wst_1.SizeF = new System.Drawing.SizeF(100F, 11.11111F);
            this.wst_1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // wts_1
            // 
            this.wts_1.BackColor = System.Drawing.Color.Transparent;
            this.wts_1.BorderColor = System.Drawing.Color.Black;
            this.wts_1.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.wts_1.BorderWidth = 1F;
            this.wts_1.CanGrow = false;
            this.wts_1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[wts]")});
            this.wts_1.Font = new DevExpress.Drawing.DXFont("Arial", 7F, DevExpress.Drawing.DXFontStyle.Bold);
            this.wts_1.ForeColor = System.Drawing.Color.Black;
            this.wts_1.LocationFloat = new DevExpress.Utils.PointFloat(283.3333F, 16.66667F);
            this.wts_1.Name = "wts_1";
            this.wts_1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.wts_1.SizeF = new System.Drawing.SizeF(100F, 11.80556F);
            this.wts_1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // Text1
            // 
            this.Text1.BackColor = System.Drawing.Color.Transparent;
            this.Text1.BorderColor = System.Drawing.Color.Black;
            this.Text1.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.Text1.BorderWidth = 1F;
            this.Text1.CanGrow = false;
            this.Text1.Font = new DevExpress.Drawing.DXFont("Arial", 7F, DevExpress.Drawing.DXFontStyle.Bold);
            this.Text1.ForeColor = System.Drawing.Color.Black;
            this.Text1.LocationFloat = new DevExpress.Utils.PointFloat(183.3333F, 36.11111F);
            this.Text1.Name = "Text1";
            this.Text1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.Text1.SizeF = new System.Drawing.SizeF(98.26389F, 9.722222F);
            this.Text1.Text = "MATHA JEWELLERY";
            this.Text1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // wts
            // 
            this.wts.Expression = "\'Gwt: \' + FormatString(\'{0:0.000}\', ToDouble([jtable.wt]))";
            this.wts.FieldType = DevExpress.XtraReports.UI.FieldType.String;
            this.wts.Name = "wts";
            // 
            // mc
            // 
            this.mc.Expression = "\'MC:\' + [jtable.make]";
            this.mc.FieldType = DevExpress.XtraReports.UI.FieldType.String;
            this.mc.Name = "mc";
            // 
            // wst
            // 
            this.wst.Expression = "\'Purity:\' + [jtable.purity]";
            this.wst.FieldType = DevExpress.XtraReports.UI.FieldType.String;
            this.wst.Name = "wst";
            // 
            // Lable_cjp
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Area3,
            this.Area1,
            this.Area2,
            this.Area4,
            this.Area5});
            this.CalculatedFields.AddRange(new DevExpress.XtraReports.UI.CalculatedField[] {
            this.wts,
            this.mc,
            this.wst});
            this.Margins = new DevExpress.Drawing.DXMargins(0F, 0F, 0F, 0F);
            this.PageHeight = 4054;
            this.PageWidth = 2865;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.Custom;
            this.Version = "24.2";
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        #endregion

        private DevExpress.XtraReports.UI.DetailBand Area3;
        private DevExpress.XtraReports.UI.XRLabel pcode1;
        private DevExpress.XtraReports.UI.XRLabel pcode2;
        private DevExpress.XtraReports.UI.XRLabel dt1;
        private DevExpress.XtraReports.UI.XRLabel pn1;
        private DevExpress.XtraReports.UI.XRLabel mc_1;
        private DevExpress.XtraReports.UI.XRLabel wst_1;
        private DevExpress.XtraReports.UI.XRLabel wts_1;
        private DevExpress.XtraReports.UI.XRLabel Text1;
        private DevExpress.XtraReports.UI.ReportHeaderBand Area1;
        private DevExpress.XtraReports.UI.PageHeaderBand Area2;
        private DevExpress.XtraReports.UI.ReportFooterBand Area4;
        private DevExpress.XtraReports.UI.PageFooterBand Area5;
        private DevExpress.XtraReports.UI.CalculatedField wts;
        private DevExpress.XtraReports.UI.CalculatedField mc;
        private DevExpress.XtraReports.UI.CalculatedField wst;
    }
}
