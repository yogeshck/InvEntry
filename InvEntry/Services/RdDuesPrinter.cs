#nullable enable
using DevExpress.Mvvm;
using InvEntry.Contracts.Schemes;
using InvEntry.Extension;
using InvEntry.Helpers;
using InvEntry.Reports;
using InvEntry.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InvEntry.Services;

/// <summary>Opens the print preview of the monthly RD dues list (<see cref="RdDuesPrint"/>).</summary>
public interface IRdDuesPrinter
{
    Task PreviewAsync(RdDueReportDto report, IReadOnlyList<RdDueRowDto> rows, RdDueFilter filter);
}

public sealed class RdDuesPrinter : IRdDuesPrinter
{
    private readonly IOrgThisCompanyViewService _companyService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IDialogService _reportDialogService;

    public RdDuesPrinter(
        IOrgThisCompanyViewService companyService,
        IMessageBoxService messageBoxService,
        [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
    {
        _companyService = companyService;
        _messageBoxService = messageBoxService;
        _reportDialogService = reportDialogService;
    }

    public async Task PreviewAsync(RdDueReportDto report, IReadOnlyList<RdDueRowDto> rows, RdDueFilter filter)
    {
        try
        {
            var company = await _companyService.GetOrgThisCompany();

            var print = new RdDuesPrint(report, rows, filter == RdDueFilter.All ? "Dues and collections" : filter.ToString(), company);
            print.CreateDocument();

            _reportDialogService.PrintPreviewReceipt(print);
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowMessage(
                "The RD dues list could not be printed." + Environment.NewLine + ApiErrorMessage.From(ex),
                "RD dues", MessageButton.OK, MessageIcon.Error);
        }
    }
}
