using DevExpress.DataAccess.ConnectionParameters;
using DevExpress.DataAccess.Sql;
using DevExpress.XtraGauges.Core.Model;
using DevExpress.XtraReports.Native;
using DevExpress.XtraReports.UI;
using InvEntry.Services;
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

    XtraReport CreateFinStatementReport();

    XtraReport CreateFinStatementReport(DateTime pFromDate, DateTime pToDate, string statementType);

    Task CreateFinStatementReportPdf(DateTime pFromDate, DateTime pToDate, string statementType, string filePath);

}

public class ReportFactoryService : IReportFactoryService
{
    private readonly string _connectionString;
    private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;

    public ReportFactoryService(IConfiguration configuration,
                                IOrgThisCompanyViewService orgThisCompanyViewService) 
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        _orgThisCompanyViewService = orgThisCompanyViewService;
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

        setReportParametersAsync(report);

        report.Parameters["paramEstNbr"].Value = pEstimateNbr;
        report.CreateDocument();
        return report;
    }

    private async Task setReportParametersAsync(XtraReport report)
    {
        var orgThisCompany = await _orgThisCompanyViewService.GetOrgThisCompany();
        report.Parameters["pCompanyName"].Value = orgThisCompany.CompanyName;

    }

    public async Task CreateEstimateReportPdf(string pEstimateNbr, string filePath)
    {
        var report = CreateEstimateReport(pEstimateNbr);

        await report.ExportToPdfAsync(filePath);
    }

    public XtraReport CreateFinStatementReport()
    {
        return new PettyCashReport();
    }

    public XtraReport CreateFinStatementReport(DateTime pFromDate, DateTime pToDate, string statementType)
    {
        var report = CreateFinStatementReport();

        report.Parameters["FromDate"].Value = pFromDate;
        report.Parameters["ToDate"].Value = pToDate;
        report.Parameters["StatementType"].Value = statementType;
        report.CreateDocument();
        return report;
    }

    public async Task CreateFinStatementReportPdf(DateTime pFromDate, DateTime pToDate,
                                                    string statementType, string filePath)
    {
        var report = CreateFinStatementReport(pFromDate, pToDate, statementType);

        await report.ExportToPdfAsync(filePath);
    }

}
