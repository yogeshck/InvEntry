using InvEntry.Utils;

namespace InvEntry.Reports
{
    public partial class VoucherPrint : DevExpress.XtraReports.UI.XtraReport
    {
        private string NumberToWordsFormat = "{0} ONLY";

        public VoucherPrint()
        {
            InitializeComponent();
        }

    }
}
