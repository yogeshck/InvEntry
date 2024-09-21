using DevExpress.DataAccess.ConnectionParameters;
using DevExpress.DataAccess.Sql;
using DevExpress.XtraGauges.Core.Model;
using DevExpress.XtraReports.Native;
using DevExpress.XtraReports.UI;
using Microsoft.Extensions.Configuration;
using mijmsReports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Reports;

public interface IReportFactoryService
{
    XtraReport CreateInvoiceReport();

    XtraReport CreateInvoiceReport(string pInvoiceNbr);
}

public class ReportFactoryService : IReportFactoryService
{
    private readonly string _connectionString;

    public ReportFactoryService(IConfiguration configuration) 
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public XtraReport CreateInvoiceReport()
    {
        return new XtraInvoice();
    }

    public XtraReport CreateInvoiceReport(string pInvoiceNbr)
    {
        var report = CreateInvoiceReport();

        var customStringConnection =  new CustomStringConnectionParameters(_connectionString);
        report.DataSource = new SqlDataSource(customStringConnection);
        report.Parameters["pInvoiceNbr"].Value = pInvoiceNbr;
        return report;
    }
}
