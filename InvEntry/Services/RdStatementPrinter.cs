using DevExpress.Mvvm;
using InvEntry.Extension;
using InvEntry.Helpers;
using InvEntry.Reports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace InvEntry.Services;

/// <summary>Opens the print preview of an RD passbook / statement (<see cref="RdStatementPrint"/>).</summary>
public interface IRdStatementPrinter
{
    Task PreviewAsync(int enrollmentGkey);
}

public sealed class RdStatementPrinter : IRdStatementPrinter
{
    private readonly ISchemeService _schemeService;
    private readonly IOrgThisCompanyViewService _companyService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IDialogService _reportDialogService;
    private readonly IConfiguration _configuration;

    public RdStatementPrinter(
        ISchemeService schemeService,
        IOrgThisCompanyViewService companyService,
        IMessageBoxService messageBoxService,
        [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService,
        IConfiguration configuration)
    {
        _schemeService = schemeService;
        _companyService = companyService;
        _messageBoxService = messageBoxService;
        _reportDialogService = reportDialogService;
        _configuration = configuration;
    }

    /// <summary>
    /// appsettings.json ReceiptPrinting:StatementPaper — "A4" (default) or "A5".
    /// Read on every print, so a change applies without restarting.
    /// </summary>
    private RdStatementPaper Paper =>
        Enum.TryParse<RdStatementPaper>(_configuration["ReceiptPrinting:StatementPaper"], ignoreCase: true, out var paper)
            ? paper
            : RdStatementPaper.A4;

    public async Task PreviewAsync(int enrollmentGkey)
    {
        try
        {
            var statement = await _schemeService.GetStatement(enrollmentGkey);
            var company = await _companyService.GetOrgThisCompany();

            var report = new RdStatementPrint(statement, company, Paper);
            report.CreateDocument();

            _reportDialogService.PrintPreviewReceipt(report);
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowMessage(
                "The passbook could not be printed." + Environment.NewLine + ApiErrorMessage.From(ex),
                "RD passbook", MessageButton.OK, MessageIcon.Error);
        }
    }
}
