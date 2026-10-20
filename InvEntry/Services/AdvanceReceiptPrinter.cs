using DevExpress.Mvvm;
using InvEntry.Contracts.Advances;
using InvEntry.Extension;
using InvEntry.Helpers;
using InvEntry.Reports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace InvEntry.Services;

/// <summary>
/// Opens the print preview of an advance / RD installment receipt
/// (<see cref="AdvanceReceiptPrint"/>) for printing or reprinting.
/// </summary>
public interface IAdvanceReceiptPrinter
{
    /// <summary>True for vouchers this printer handles (advance and RD receipts).</summary>
    bool Handles(string? transType, string? voucherType);

    Task PreviewAsync(int voucherGkey, bool isReprint);
}

public sealed class AdvanceReceiptPrinter : IAdvanceReceiptPrinter
{
    private readonly IAdvanceAccountService _advanceAccountService;
    private readonly IOrgThisCompanyViewService _companyService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IDialogService _reportDialogService;
    private readonly IConfiguration _configuration;

    public AdvanceReceiptPrinter(
        IAdvanceAccountService advanceAccountService,
        IOrgThisCompanyViewService companyService,
        IMessageBoxService messageBoxService,
        [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService,
        IConfiguration configuration)
    {
        _advanceAccountService = advanceAccountService;
        _companyService = companyService;
        _messageBoxService = messageBoxService;
        _reportDialogService = reportDialogService;
        _configuration = configuration;
    }

    /// <summary>
    /// appsettings.json ReceiptPrinting:Layout — "A4TwoCopies" (default: customer
    /// and shop copy on one A4 sheet) or "A5" (one copy). Read on every print,
    /// so a change applies without restarting.
    /// </summary>
    private AdvanceReceiptLayout Layout =>
        Enum.TryParse<AdvanceReceiptLayout>(_configuration["ReceiptPrinting:Layout"], ignoreCase: true, out var layout)
            ? layout
            : AdvanceReceiptLayout.A4TwoCopies;

    public bool Handles(string? transType, string? voucherType) =>
        string.Equals(transType?.Trim(), "Receipt", StringComparison.OrdinalIgnoreCase) &&
        (string.Equals(voucherType?.Trim(), AdvanceReceiptType.AdvanceReceipt, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(voucherType?.Trim(), AdvanceReceiptType.RecurringDeposit, StringComparison.OrdinalIgnoreCase));

    public async Task PreviewAsync(int voucherGkey, bool isReprint)
    {
        try
        {
            var receipt = await _advanceAccountService.GetReceiptPrint(voucherGkey);
            var company = await _companyService.GetOrgThisCompany();

            var report = new AdvanceReceiptPrint(receipt, company, isReprint, Layout);
            report.CreateDocument();

            _reportDialogService.PrintPreviewReceipt(report);
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowMessage(
                "The receipt could not be printed." + Environment.NewLine + ApiErrorMessage.From(ex),
                "Print receipt", MessageButton.OK, MessageIcon.Error);
        }
    }
}
