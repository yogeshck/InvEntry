using CommunityToolkit.Mvvm.ComponentModel;
using DevExpress.XtraReports.UI;
using InvEntry.Reports;
using mijmsReports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.ViewModels
{
    public partial class ReportDialogViewModel : ObservableObject
    {
        [ObservableProperty]
        private XtraReport _report;

        public void Init(string pInvoiceNbr)
        {
            Report = ReportFactory.CreateInvoiceReport(pInvoiceNbr);
        }
    }
}
