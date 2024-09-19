using DevExpress.XtraGauges.Core.Model;
using DevExpress.XtraReports.UI;
using mijmsReports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Reports
{
    public class ReportFactory
    {
        public static XtraReport CreateInvoiceReport()
        {
            return new XtraInvoice();
        }

        public static XtraReport CreateInvoiceReport(string pInvoiceNbr)
        {
            var report = CreateInvoiceReport();
            report.Parameters["pInvoiceNbr"].Value = pInvoiceNbr;

            return report;
        }
    }
}
