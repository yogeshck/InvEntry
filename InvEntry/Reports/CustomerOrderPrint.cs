using DevExpress.XtraReports.UI;
using InvEntry.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InvEntry.Reports
{
    public partial class CustomerOrderPrint : DevExpress.XtraReports.UI.XtraReport
    {
        private string NumberToWordsFormat = "{0} ONLY";

        public CustomerOrderPrint()
        {
            InitializeComponent();
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
