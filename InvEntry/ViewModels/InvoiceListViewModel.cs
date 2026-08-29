using DevExpress.Mvvm;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Models.UI;
using InvEntry.Services;
using InvEntry.Utils.Options;
using InvEntry.ViewModels.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

public class InvoiceListViewModel
    : BaseListViewModel<InvoiceHeader>
{
    private readonly IInvoiceService _invoiceService;

    private readonly IDialogService _reportDialogService;


    // ============================================================
    // SCREEN
    // ============================================================

    public override string Title =>
        "INVOICE LIST";


    public override string Description =>
        "Browse, search, export and print invoices";


    public override bool SupportsDocumentPrint =>
        true;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public InvoiceListViewModel(
        IInvoiceService invoiceService,
        [FromKeyedServices("ReportDialogService")]
        IDialogService reportDialogService)
    {
        _invoiceService =
            invoiceService;

        _reportDialogService =
            reportDialogService;


        ConfigureFilter(
            new ListFilterDefinition
            {
                Label =
                    "Customer Mobile",

                Placeholder =
                    "Enter mobile number",

                Type =
                    ListFilterType.Text
            });


        ConfigureColumns();
    }


    // ============================================================
    // COLUMNS
    // ============================================================

    private void ConfigureColumns()
    {

        Columns.Add(new()
        {
            FieldName = nameof(InvoiceHeader.CustMobile),
            Header = "Mobile #",
            Width = 110,

            MaskValue = true,
            VisibleLastCharacters = 4
        });


        Columns.Add(
            new()
            {
                FieldName =
                    nameof(InvoiceHeader.InvNbr),

                Header =
                    "Invoice #",

                Width =
                    110
            });


        Columns.Add(
            new()
            {
                FieldName =
                    nameof(InvoiceHeader.InvDate),

                Header =
                    "Invoice Date",

                Width =
                    110,

                ColumnType =
                    ListColumnType.Date
            });


        Columns.Add(
            new()
            {
                FieldName =
                    nameof(InvoiceHeader.GrossRcbAmount),

                Header =
                    "Invoice Amount",

                Width =
                    130,

                ColumnType =
                    ListColumnType.Currency,

                ShowSummary =
                    true,

                SummaryFormat =
                    "₹ {0:N2}"
            });


        Columns.Add(
            new()
            {
                FieldName =
                    nameof(InvoiceHeader.DiscountAmount),

                Header =
                    "Discount",

                Width =
                    105,

                ColumnType =
                    ListColumnType.Currency,

                ShowSummary =
                    true,

                SummaryFormat =
                    "₹ {0:N2}"
            });


        Columns.Add(
            new()
            {
                FieldName =
                    nameof(InvoiceHeader.AdvanceAdj),

                Header =
                    "Advance",

                Width =
                    105,

                ColumnType =
                    ListColumnType.Currency,

                ShowSummary =
                    true,

                SummaryFormat =
                    "₹ {0:N2}"
            });


        Columns.Add(
            new()
            {
                FieldName =
                    nameof(InvoiceHeader.RdAmountAdj),

                Header =
                    "RD",

                Width =
                    115,

                ColumnType =
                    ListColumnType.Currency,

                ShowSummary =
                    true,

                SummaryFormat =
                    "₹ {0:N2}"
            });


        Columns.Add(
            new()
            {
                FieldName =
                    nameof(InvoiceHeader.AmountPayable),

                Header =
                    "Receivable",

                Width =
                    115,

                ColumnType =
                    ListColumnType.Currency,

                ShowSummary =
                    true,

                SummaryFormat =
                    "₹ {0:N2}"
            });


        Columns.Add(
            new()
            {
                FieldName =
                    nameof(InvoiceHeader.RecdAmount),

                Header =
                    "Received",

                Width =
                    115,

                ColumnType =
                    ListColumnType.Currency,

                ShowSummary =
                    true,

                SummaryFormat =
                    "₹ {0:N2}"
            });


        Columns.Add(
            new()
            {
                FieldName =
                    nameof(InvoiceHeader.InvBalance),

                Header =
                    "Balance",

                Width =
                    115,

                ColumnType =
                    ListColumnType.Currency,

                ShowSummary =
                    true,

                SummaryFormat =
                    "₹ {0:N2}"
            });


        Columns.Add(
            new()
            {
                FieldName =
                    nameof(InvoiceHeader.InvRefund),

                Header =
                    "Refund",

                Width =
                    115,

                ColumnType =
                    ListColumnType.Currency,

                ShowSummary =
                    true,

                SummaryFormat =
                    "₹ {0:N2}"
            });


        Columns.Add(
            new()
            {
                FieldName =
                    nameof(InvoiceHeader.PaymentDueDate),

                Header =
                    "Due Date",

                Width =
                    110,

                ColumnType =
                    ListColumnType.Date
            });
    }


    // ============================================================
    // DATA
    // ============================================================

    protected override async Task<IEnumerable<InvoiceHeader>>
        LoadItemsAsync(
            ListSearchOption search)
    {
        /*
         * Preserve your existing service/API contract.
         */

        var option =
            new DateSearchOption
            {
                From =
                    search.From,

                To =
                    search.To,

                Filter1 =
                    search.FilterValue
            };


        var result =
            await _invoiceService
                .GetAll(
                    option);


        return
            result ??
            Enumerable.Empty<InvoiceHeader>();
    }


    // ============================================================
    // PRINT
    // ============================================================

    protected override void PrintItem(
        InvoiceHeader item)
    {
        if (string.IsNullOrWhiteSpace(
                item.InvNbr))
        {
            return;
        }


        _reportDialogService
            .PrintPreview(
                item.InvNbr);
    }
}