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

    Task CreateInvoiceReportPdf(string pInvoiceNbr, string filePath);

    XtraReport CreateEstimateReport();

    XtraReport CreateEstimateReport(string pEstimateNbr);

    Task CreateEstimateReportPdf(string pEstimateNbr, string filePath);
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

        report.Parameters["pInvoiceNbr"].Value = pInvoiceNbr;
        report.CreateDocument();
        return report;
    }

    public async Task CreateInvoiceReportPdf(string pInvoiceNbr, string filePath)
    {
        var report = CreateInvoiceReport(pInvoiceNbr);

        await report.ExportToPdfAsync(filePath);
    }

    public XtraReport CreateEstimateReport()
    {
        return new XtraEstimate();
    }

    public XtraReport CreateEstimateReport(string pEstimateNbr)
    {
        var report = CreateEstimateReport();

        report.Parameters["paramEstNbr"].Value = pEstimateNbr;
        report.CreateDocument();
        return report;
    }

    public async Task CreateEstimateReportPdf(string pEstimateNbr, string filePath)
    {
        var report = CreateEstimateReport(pEstimateNbr);

        await report.ExportToPdfAsync(filePath);
    }

}
