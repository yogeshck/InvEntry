using DevExpress.DataAccess.ConnectionParameters;
using DevExpress.DataAccess.Sql;
using DevExpress.XtraGauges.Core.Model;
using DevExpress.XtraReports.Native;
using DevExpress.XtraReports.UI;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Reports;
using Microsoft.Extensions.Configuration;
//using mijmsReports;
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

    XtraReport CreateCustomerOrderReport(string orderNbr);

    Task CreateInvoiceReportPdf(string pInvoiceNbr, string filePath);

    XtraReport CreateEstimateReport();

    XtraReport CreateEstimateReport(string pEstimateNbr, int estGkey, OrgThisCompanyView orgThisCompany);

    XtraReport CreateOMPurchaseReport(string pDocRefNbr); // int estGkey, OrgThisCompanyView orgThisCompany);

    Task CreateEstimateReportPdf(string pEstimateNbr, int pEstHdrGkey,
                                                OrgThisCompanyView orgThisCompany, string filePath);

    XtraReport CreateDeliveryNoteReport();

    XtraReport CreateDeliveryNoteReport(string pEstimateNbr, int estGkey, OrgThisCompanyView orgThisCompany);

    Task CreateDeliveryNoteReportPdf(string pEstimateNbr, int pEstHdrGkey,
                                                OrgThisCompanyView orgThisCompany, string filePath);

    XtraReport CreateVoucherReport();

    XtraReport CreateVoucherReport(int pVoucherGkey, OrgThisCompanyView orgThisCompany);

    Task CreateVoucherReportPdf(int pVoucherGkey,
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

    /*    public XtraReport CreateInvoiceReport()
        {
            return new InvPrint25().AddDataSource(_appConfigName);
    //            XrNewInvoice24().AddDataSource(_appConfigName);        // XtraInvoice();
        }

        public XtraReport CreateInvoiceReport(string pInvoiceNbr)
        {
            var report = CreateInvoiceReport();

            report.Parameters["pInvNbr"].Value = pInvoiceNbr;
            report.CreateDocument();
            return report;
        }*/

    public XtraReport CreateInvoiceReport()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        Serilog.Log.Information(
            "Invoice Report: creating InvPrint25");

        var report = new InvPrint25();

        Serilog.Log.Information(
            "Invoice Report: InvPrint25 constructed in {ElapsedMs} ms",
            sw.ElapsedMilliseconds);

        report.AddDataSource(_appConfigName);

        Serilog.Log.Information(
            "Invoice Report: datasource configured in {ElapsedMs} ms",
            sw.ElapsedMilliseconds);

        return report;
    }

    public XtraReport CreateInvoiceReport(string pInvoiceNbr)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        Serilog.Log.Information(
            "Invoice Report {InvoiceNumber}: START",
            pInvoiceNbr);

        var report = CreateInvoiceReport();

        Serilog.Log.Information(
            "Invoice Report {InvoiceNumber}: report created in {ElapsedMs} ms",
            pInvoiceNbr,
            sw.ElapsedMilliseconds);

        report.Parameters["pInvNbr"].Value = pInvoiceNbr;

        Serilog.Log.Information(
            "Invoice Report {InvoiceNumber}: parameter assigned in {ElapsedMs} ms",
            pInvoiceNbr,
            sw.ElapsedMilliseconds);

        var documentSw =
            System.Diagnostics.Stopwatch.StartNew();

        report.CreateDocument();

        documentSw.Stop();

        Serilog.Log.Information(
            "Invoice Report {InvoiceNumber}: CreateDocument took {DocumentMs} ms; total {TotalMs} ms",
            pInvoiceNbr,
            documentSw.ElapsedMilliseconds,
            sw.ElapsedMilliseconds);

        return report;
    }

    public XtraReport CreateCustomerOrderReport(string orderNbr)
    {
        var report = PrepareCustomerOrderReport(orderNbr);
        report.CreateDocument();

        return report;
    }

    private CustomerOrderPrint PrepareCustomerOrderReport(string orderNbr)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderNbr);

        var report =
            new CustomerOrderPrint()
                .AddDataSource(_appConfigName);

        report.Parameters["pOrderNbr"].Value = orderNbr.Trim();

        return (CustomerOrderPrint)report;
    }

    public async Task CreateInvoiceReportPdf(string pInvoiceNbr, string filePath)
    {
        var report = CreateInvoiceReport(pInvoiceNbr);

        await report.ExportToPdfAsync(filePath);
    }

    public XtraReport CreateOMPurchaseReport(string pDocRefNbr)
    {
        var report = CreateOMPurchaseReport();

        //await report.ExportToPdfAsync(filePath);
        report.Parameters["pRefDocNbr"].Value = pDocRefNbr;
        
        report.CreateDocument();
        return report;
    }

    public XtraReport CreateOMPurchaseReport()
    {
        return new OldMetalPurchase().AddDataSource(_appConfigName);  //XtraEstimate().AddDataSource(_appConfigName);   
                                                                      //XrNewEstimate24().AddDataSource(_appConfigName);
    }

    public XtraReport CreateEstimateReport()
    {
        return new XrtNewEstimate25().AddDataSource(_appConfigName);  //XtraEstimate().AddDataSource(_appConfigName);   
                                                                     //XrNewEstimate24().AddDataSource(_appConfigName);
    }

    public XtraReport CreateEstimateReport(string pEstimateNbr, int pEstHdrGkey, OrgThisCompanyView orgThisCompany)
    {
        var report = CreateEstimateReport();

        //setReportParametersAsync(report, orgThisCompany);

        report.Parameters["pEstNbr"].Value = pEstimateNbr;    //paramEstNbr
        report.Parameters["pEstHdrGkey"].Value = pEstHdrGkey;
        report.CreateDocument();
        return report;
    }

    public XtraReport CreateDeliveryNoteReport()
    {
        return new DeliveryNote().AddDataSource(_appConfigName); 
                                                                     
    }


    public XtraReport CreateDeliveryNoteReport(string pEstimateNbr, int pEstHdrGkey, OrgThisCompanyView orgThisCompany)
    {
        var report = CreateDeliveryNoteReport();

        report.Parameters["pEstNbr"].Value = pEstimateNbr;    //paramEstNbr
        report.Parameters["pEstHdrGkey"].Value = pEstHdrGkey;
        report.CreateDocument();
        return report;

    }

    public async Task CreateDeliveryNoteReportPdf(string pEstimateNbr, int pEstHdrGkey,
                                            OrgThisCompanyView orgThisCompany, string filePath)
    {
        var report = CreateDeliveryNoteReport(pEstimateNbr, pEstHdrGkey, orgThisCompany);

        await report.ExportToPdfAsync(filePath);

    }

    public XtraReport CreateVoucherReport()
    {
        return new VoucherPrint().AddDataSource(_appConfigName);

    }


    public XtraReport CreateVoucherReport(int pVoucherGkey, OrgThisCompanyView orgThisCompany)
    {
        var report = CreateVoucherReport();

        report.Parameters["pVoucherGkey"].Value = pVoucherGkey;
        report.CreateDocument();
        return report;

    }

    public async Task CreateVoucherReportPdf(int pVoucherGkey,
                                            OrgThisCompanyView orgThisCompany, string filePath)
    {
        var report = CreateVoucherReport(pVoucherGkey, orgThisCompany);

        await report.ExportToPdfAsync(filePath);

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
