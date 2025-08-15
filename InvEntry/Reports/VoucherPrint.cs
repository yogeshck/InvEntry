using DevExpress.XtraReports.UI;
using System;

namespace InvEntry.Reports
{
    public partial class VoucherPrint : DevExpress.XtraReports.UI.XtraReport
    {
        private string NumberToWordsFormat = "{0} ONLY";

        public VoucherPrint()
        {
            InitializeComponent();
        }

        private void TransAmountCalc_GetValue(object sender, DevExpress.XtraReports.UI.GetValueEventArgs e)
        {
            var words = Utils.NumberToWords.Convert(vchTransAmount.Value);

            if (!string.IsNullOrEmpty(words))
                e.Value = string.Format(NumberToWordsFormat, words);
            else
                e.Value = "NIL";
        }

        /*
         *         private void CalculatedField1_GetValue(object sender, DevExpress.XtraReports.UI.GetValueEventArgs e)
        {
            var words = NumberToWords.Convert(invGrandTotal.Value);

            if (!string.IsNullOrEmpty(words))
                e.Value = string.Format(NumberToWordsFormat, words);
            else
                e.Value = "NIL";
        }
        */
    }
}
