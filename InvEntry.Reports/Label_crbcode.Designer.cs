namespace InvEntry.Reports
{
    partial class Label_crbcode
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
            this.mrp_1 = new DevExpress.XtraReports.UI.XRLabel();
            this.pname1 = new DevExpress.XtraReports.UI.XRLabel();
            this.pcode6 = new DevExpress.XtraReports.UI.XRLabel();
            this.Text2 = new DevExpress.XtraReports.UI.XRLabel();
            this.Text4 = new DevExpress.XtraReports.UI.XRLabel();
            this.mrp_2 = new DevExpress.XtraReports.UI.XRLabel();
            this.pcode2 = new DevExpress.XtraReports.UI.XRLabel();
            this.pcode3 = new DevExpress.XtraReports.UI.XRLabel();
            this.pname2 = new DevExpress.XtraReports.UI.XRLabel();
            this.wp1 = new DevExpress.XtraReports.UI.XRLabel();
            this.wp2 = new DevExpress.XtraReports.UI.XRLabel();
            this.mrp = new DevExpress.XtraReports.UI.CalculatedField();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // Area3
            // 
            this.Area3.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.pcode1,
            this.mrp_1,
            this.pname1,
            this.pcode6,
            this.Text2,
            this.Text4,
            this.mrp_2,
            this.pcode2,
            this.pcode3,
            this.pname2,
            this.wp1,
            this.wp2});
            this.Area3.HeightF = 98F;
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
            this.Area2.Visible = false;
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
            this.Area5.HeightF = 0F;
            this.Area5.Name = "Area5";
            this.Area5.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Area5.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.Area5.Visible = false;
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
            this.pcode1.Font = new DevExpress.Drawing.DXFont("Microsoft Sans Serif", 18F);
            this.pcode1.ForeColor = System.Drawing.Color.Black;
            this.pcode1.LocationFloat = new DevExpress.Utils.PointFloat(216.6667F, 41.66667F);
            this.pcode1.Name = "pcode1";
            this.pcode1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.pcode1.SizeF = new System.Drawing.SizeF(183.3333F, 25F);
            this.pcode1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // mrp_1
            // 
            this.mrp_1.BackColor = System.Drawing.Color.Transparent;
            this.mrp_1.BorderColor = System.Drawing.Color.Black;
            this.mrp_1.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.mrp_1.BorderWidth = 1F;
            this.mrp_1.CanGrow = false;
            this.mrp_1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[mrp]")});
            this.mrp_1.Font = new DevExpress.Drawing.DXFont("Arial", 8F, DevExpress.Drawing.DXFontStyle.Bold);
            this.mrp_1.ForeColor = System.Drawing.Color.Black;
            this.mrp_1.LocationFloat = new DevExpress.Utils.PointFloat(325F, 69.44444F);
            this.mrp_1.Name = "mrp_1";
            this.mrp_1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.mrp_1.SizeF = new System.Drawing.SizeF(75F, 15.34722F);
            this.mrp_1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // pname1
            // 
            this.pname1.BackColor = System.Drawing.Color.Transparent;
            this.pname1.BorderColor = System.Drawing.Color.Black;
            this.pname1.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.pname1.BorderWidth = 1F;
            this.pname1.CanGrow = false;
            this.pname1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[pname]")});
            this.pname1.Font = new DevExpress.Drawing.DXFont("Arial", 6F);
            this.pname1.ForeColor = System.Drawing.Color.Black;
            this.pname1.LocationFloat = new DevExpress.Utils.PointFloat(216.6667F, 29.16667F);
            this.pname1.Name = "pname1";
            this.pname1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.pname1.SizeF = new System.Drawing.SizeF(183.3333F, 15.34722F);
            this.pname1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // pcode6
            // 
            this.pcode6.BackColor = System.Drawing.Color.Transparent;
            this.pcode6.BorderColor = System.Drawing.Color.Black;
            this.pcode6.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.pcode6.BorderWidth = 1F;
            this.pcode6.CanGrow = false;
            this.pcode6.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[pcode]")});
            this.pcode6.Font = new DevExpress.Drawing.DXFont("Arial", 8F, DevExpress.Drawing.DXFontStyle.Bold);
            this.pcode6.ForeColor = System.Drawing.Color.Black;
            this.pcode6.LocationFloat = new DevExpress.Utils.PointFloat(216.6667F, 69.44444F);
            this.pcode6.Name = "pcode6";
            this.pcode6.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.pcode6.SizeF = new System.Drawing.SizeF(108.3333F, 15.76389F);
            this.pcode6.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // Text2
            // 
            this.Text2.BackColor = System.Drawing.Color.Transparent;
            this.Text2.BorderColor = System.Drawing.Color.Black;
            this.Text2.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.Text2.BorderWidth = 1F;
            this.Text2.CanGrow = false;
            this.Text2.Font = new DevExpress.Drawing.DXFont("Arial", 8F);
            this.Text2.ForeColor = System.Drawing.Color.Black;
            this.Text2.LocationFloat = new DevExpress.Utils.PointFloat(216.6667F, 16.66667F);
            this.Text2.Name = "Text2";
            this.Text2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.Text2.SizeF = new System.Drawing.SizeF(83.33334F, 15.76389F);
            this.Text2.Text = "STEPHEN TEX";
            this.Text2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // Text4
            // 
            this.Text4.BackColor = System.Drawing.Color.Transparent;
            this.Text4.BorderColor = System.Drawing.Color.Black;
            this.Text4.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.Text4.BorderWidth = 1F;
            this.Text4.CanGrow = false;
            this.Text4.Font = new DevExpress.Drawing.DXFont("Arial", 8F);
            this.Text4.ForeColor = System.Drawing.Color.Black;
            this.Text4.LocationFloat = new DevExpress.Utils.PointFloat(441.6667F, 16.66667F);
            this.Text4.Name = "Text4";
            this.Text4.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.Text4.SizeF = new System.Drawing.SizeF(83.33334F, 15.76389F);
            this.Text4.Text = "STEPHEN TEX\n";
            this.Text4.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // mrp_2
            // 
            this.mrp_2.BackColor = System.Drawing.Color.Transparent;
            this.mrp_2.BorderColor = System.Drawing.Color.Black;
            this.mrp_2.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.mrp_2.BorderWidth = 1F;
            this.mrp_2.CanGrow = false;
            this.mrp_2.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[mrp]")});
            this.mrp_2.Font = new DevExpress.Drawing.DXFont("Arial", 8F, DevExpress.Drawing.DXFontStyle.Bold);
            this.mrp_2.ForeColor = System.Drawing.Color.Black;
            this.mrp_2.LocationFloat = new DevExpress.Utils.PointFloat(541.6667F, 69.44444F);
            this.mrp_2.Name = "mrp_2";
            this.mrp_2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.mrp_2.SizeF = new System.Drawing.SizeF(83.33334F, 15.34722F);
            this.mrp_2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
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
            this.pcode2.Font = new DevExpress.Drawing.DXFont("Arial", 8F, DevExpress.Drawing.DXFontStyle.Bold);
            this.pcode2.ForeColor = System.Drawing.Color.Black;
            this.pcode2.LocationFloat = new DevExpress.Utils.PointFloat(441.6667F, 69.44444F);
            this.pcode2.Name = "pcode2";
            this.pcode2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.pcode2.SizeF = new System.Drawing.SizeF(100F, 15.76389F);
            this.pcode2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // pcode3
            // 
            this.pcode3.BackColor = System.Drawing.Color.Transparent;
            this.pcode3.BorderColor = System.Drawing.Color.Black;
            this.pcode3.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.pcode3.BorderWidth = 1F;
            this.pcode3.CanGrow = false;
            this.pcode3.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[pcode]")});
            this.pcode3.Font = new DevExpress.Drawing.DXFont("Microsoft Sans Serif", 18F);
            this.pcode3.ForeColor = System.Drawing.Color.Black;
            this.pcode3.LocationFloat = new DevExpress.Utils.PointFloat(441.6667F, 41.66667F);
            this.pcode3.Name = "pcode3";
            this.pcode3.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.pcode3.SizeF = new System.Drawing.SizeF(183.3333F, 25F);
            this.pcode3.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // pname2
            // 
            this.pname2.BackColor = System.Drawing.Color.Transparent;
            this.pname2.BorderColor = System.Drawing.Color.Black;
            this.pname2.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.pname2.BorderWidth = 1F;
            this.pname2.CanGrow = false;
            this.pname2.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[pname]")});
            this.pname2.Font = new DevExpress.Drawing.DXFont("Arial", 6F);
            this.pname2.ForeColor = System.Drawing.Color.Black;
            this.pname2.LocationFloat = new DevExpress.Utils.PointFloat(441.6667F, 29.16667F);
            this.pname2.Name = "pname2";
            this.pname2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.pname2.SizeF = new System.Drawing.SizeF(183.3333F, 15.34722F);
            this.pname2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // wp1
            // 
            this.wp1.BackColor = System.Drawing.Color.Transparent;
            this.wp1.BorderColor = System.Drawing.Color.Black;
            this.wp1.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.wp1.BorderWidth = 1F;
            this.wp1.CanGrow = false;
            this.wp1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[wp]")});
            this.wp1.Font = new DevExpress.Drawing.DXFont("Arial", 8F);
            this.wp1.ForeColor = System.Drawing.Color.Black;
            this.wp1.LocationFloat = new DevExpress.Utils.PointFloat(525F, 16.66667F);
            this.wp1.Name = "wp1";
            this.wp1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.wp1.SizeF = new System.Drawing.SizeF(100F, 15.34722F);
            this.wp1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            // 
            // wp2
            // 
            this.wp2.BackColor = System.Drawing.Color.Transparent;
            this.wp2.BorderColor = System.Drawing.Color.Black;
            this.wp2.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.wp2.BorderWidth = 1F;
            this.wp2.CanGrow = false;
            this.wp2.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[wp]")});
            this.wp2.Font = new DevExpress.Drawing.DXFont("Arial", 8F);
            this.wp2.ForeColor = System.Drawing.Color.Black;
            this.wp2.LocationFloat = new DevExpress.Utils.PointFloat(300F, 16.66667F);
            this.wp2.Name = "wp2";
            this.wp2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.wp2.SizeF = new System.Drawing.SizeF(100F, 15.34722F);
            this.wp2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            // 
            // mrp
            // 
            this.mrp.Expression = "\' Rs. \' + Iif(True, \'#NOT_SUPPORTED#\', \'[Round]([dtbcode.mrp], 2)\')";
            this.mrp.FieldType = DevExpress.XtraReports.UI.FieldType.String;
            this.mrp.Name = "mrp";
            // 
            // Label_crbcode
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Area3,
            this.Area1,
            this.Area2,
            this.Area4,
            this.Area5});
            this.CalculatedFields.AddRange(new DevExpress.XtraReports.UI.CalculatedField[] {
            this.mrp});
            this.Margins = new DevExpress.Drawing.DXMargins(0F, 0F, 0F, 0F);
            this.PageHeight = 3937;
            this.PageWidth = 2783;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.Custom;
            this.Version = "24.2";
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        #endregion

        private DevExpress.XtraReports.UI.DetailBand Area3;
        private DevExpress.XtraReports.UI.XRLabel pcode1;
        private DevExpress.XtraReports.UI.XRLabel mrp_1;
        private DevExpress.XtraReports.UI.XRLabel pname1;
        private DevExpress.XtraReports.UI.XRLabel pcode6;
        private DevExpress.XtraReports.UI.XRLabel Text2;
        private DevExpress.XtraReports.UI.XRLabel Text4;
        private DevExpress.XtraReports.UI.XRLabel mrp_2;
        private DevExpress.XtraReports.UI.XRLabel pcode2;
        private DevExpress.XtraReports.UI.XRLabel pcode3;
        private DevExpress.XtraReports.UI.XRLabel pname2;
        private DevExpress.XtraReports.UI.XRLabel wp1;
        private DevExpress.XtraReports.UI.XRLabel wp2;
        private DevExpress.XtraReports.UI.ReportHeaderBand Area1;
        private DevExpress.XtraReports.UI.PageHeaderBand Area2;
        private DevExpress.XtraReports.UI.ReportFooterBand Area4;
        private DevExpress.XtraReports.UI.PageFooterBand Area5;
        private DevExpress.XtraReports.UI.CalculatedField mrp;
    }
}
