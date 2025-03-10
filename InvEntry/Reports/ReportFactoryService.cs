using DevExpress.DataAccess.ConnectionParameters;
using DevExpress.DataAccess.Sql;
using DevExpress.XtraGauges.Core.Model;
using DevExpress.XtraReports.Native;
using DevExpress.XtraReports.UI;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Reports;
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

    XtraReport CreateEstimateReport(string pEstimateNbr, int estGkey, OrgThisCompanyView orgThisCompany);

    Task CreateEstimateReportPdf(string pEstimateNbr, int pEstHdrGkey,
                                                OrgThisCompanyView orgThisCompany, string filePath);

    XtraReport CreateFinStatementReport();

    XtraReport CreateFinStatementReport(DateTime pFromDate, DateTime pToDate, string statementType);

    Task CreateFinStatementReportPdf(DateTime pFromDate, DateTime pToDate, string statementType, string filePath);

}

public class ReportFactoryService : IReportFactoryService
{
    private readonly string _connectionString;
    private readonly string _appConfigName;
    private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;

    public ReportFactoryService(IConfiguration configuration,
                                IOrgThisCompanyViewService orgThisCompanyViewService) 
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        _orgThisCompanyViewService = orgThisCompanyViewService;
        _appConfigName = "ReportDBCon01";
    }

    public XtraReport CreateInvoiceReport()
    {
        return new XrNewInvoice24().AddDataSource(_appConfigName);        // XtraInvoice();
    }

    public XtraReport CreateInvoiceReport(string pInvoiceNbr)
    {
        var report = CreateInvoiceReport();

        report.Parameters["pInvNbr"].Value = pInvoiceNbr;
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
        return new XrNewEstimate24().AddDataSource(_appConfigName);  //XtraEstimate().AddDataSource(_appConfigName);   
                                                                     //XrNewEstimate24().AddDataSource(_appConfigName);
    }

    public XtraReport CreateEstimateReport(string pEstimateNbr, int pEstHdrGkey, OrgThisCompanyView orgThisCompany)
    {
        var report = CreateEstimateReport();

        //setReportParametersAsync(report, orgThisCompany);

        report.Parameters["pEstNbr"].Value = pEstimateNbr;    //paramEstNbr
      //  report.Parameters["pEstHdrGkey"].Value = pEstHdrGkey;
        report.CreateDocument();
        return report;
    }

    private void setReportParametersAsync(XtraReport report, OrgThisCompanyView orgThisCompany)
    {
        report.Parameters["pCompanyName"].Value = orgThisCompany.CompanyName;
        report.Parameters["pTagLine"].Value = orgThisCompany.Tagline;
        report.Parameters["pAddressLine1"].Value = orgThisCompany.AddressLine1;
        report.Parameters["pContactNbr1"].Value = orgThisCompany.ContactNbr1;
        report.Parameters["pContactNbr2"].Value = orgThisCompany.ContactNbr2;
        report.Parameters["pArea"].Value = orgThisCompany.Area;
        report.Parameters["pDistrict"].Value = orgThisCompany.District;
        report.Parameters["pState"].Value = orgThisCompany.State;
        report.Parameters["pEmailId"].Value = orgThisCompany.EmailId;
        report.Parameters["pShopLicNbr"].Value = orgThisCompany.ServiceTaxNbr;

    }

 
    public async Task CreateEstimateReportPdf(string pEstimateNbr, int pEstHdrGkey,
                                                OrgThisCompanyView orgThisCompany, string filePath)
    {
        var report = CreateEstimateReport(pEstimateNbr, pEstHdrGkey, orgThisCompany);

        await report.ExportToPdfAsync(filePath);
    }

    public XtraReport CreateFinStatementReport()
    {
        return new PettyCashReport().AddDataSource(_appConfigName);
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


public static class ReportExtensions
{
    public static XtraReport AddDataSource(this XtraReport report, string appConfigName)
    {
        if(report.DataSource is SqlDataSource sqlds)
        {
            sqlds.ConnectionName = appConfigName;
        }

        return report;
    }
}