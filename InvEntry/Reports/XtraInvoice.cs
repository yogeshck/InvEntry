using DevExpress.XtraReports.UI;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using InvEntry.Utils;
using System.Globalization;

namespace mijmsReports
{
    public partial class XtraInvoice : DevExpress.XtraReports.UI.XtraReport
    {
        private string NumberToWordsFormat = "{0} ONLY";
        public XtraInvoice()
        {
            InitializeComponent();
        }

        private void CalculatedField2_GetValue(object sender, DevExpress.XtraReports.UI.GetValueEventArgs e)
        {
            var words = NumberToWords.Convert(invGrandTotal.Value);

            if (!string.IsNullOrEmpty(words))
                e.Value = string.Format(NumberToWordsFormat, words);
            else
                e.Value = "NIL";
        }

    }
}
