using DevExpress.XtraReports.UI;
using InvEntry.Utils;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;

namespace InvEntry.Reports
{
    public partial class OldMetalPurchase : DevExpress.XtraReports.UI.XtraReport
    {
        private string NumberToWordsFormat = "{0} ONLY";
        public OldMetalPurchase()
        {
            InitializeComponent();
        }

        private void CalculatedField1_GetValue(object sender, DevExpress.XtraReports.UI.GetValueEventArgs e)
        {
            var words = NumberToWords.Convert(purLineAmount.Value);

            if (!string.IsNullOrEmpty(words))
                e.Value = string.Format(NumberToWordsFormat, words);
            else
                e.Value = "NIL";
        }
    }
}
