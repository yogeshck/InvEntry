using DevExpress.XtraReports.UI;
using InvEntry.Utils;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;

namespace InvEntry.Reports
{
    public partial class XrtEstimate24 : DevExpress.XtraReports.UI.XtraReport
    {
        private string NumberToWordsFormat = "{0} ONLY";

        public XrtEstimate24()
        {
            InitializeComponent();
        }

        private void CalculatedField1_GetValue(object sender, DevExpress.XtraReports.UI.GetValueEventArgs e)
        {
            var words = NumberToWords.Convert(invGrandTotal.Value);

            if (!string.IsNullOrEmpty(words))
                e.Value = string.Format(NumberToWordsFormat, words);
            else
                e.Value = "NIL";
        }
    }
}
