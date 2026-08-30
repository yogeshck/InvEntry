using InvEntry.Models;

namespace InvEntry.Models.UI;

public static class DraftInvoiceListDefinition
{
    public static ListViewDefinition Create()
    {
        return new ListViewDefinition
        {
            Title = "DRAFT INVOICES",

            Description =
                "Open, modify and complete invoices currently in progress",

            SupportsOpen = true,

            OpenButtonText =
                    "Edit Draft",

            // Draft invoices must never print as legal invoices.
            SupportsDocumentPrint = false,

            Filter = new ListFilterDefinition
            {
                Label = "Customer Mobile",
                Placeholder = "Enter mobile number",
                Type = ListFilterType.Text
            },

            Columns =
            {
                new()
                {
                    FieldName =
                        nameof(InvoiceHeader.GKey),

                    Header =
                        "Draft #",

                    Width = 100
                },

                new()
                {
                    FieldName =
                        nameof(InvoiceHeader.InvDate),

                    Header =
                        "Date",

                    Width = 110,

                    ColumnType =
                        ListColumnType.Date
                },

                new()
                {
                    FieldName =
                        nameof(InvoiceHeader.CustMobile),

                    Header =
                        "Mobile #",

                    Width = 115,

                    MaskValue = true,

                    VisibleLastCharacters = 4
                },

                new()
                {
                    FieldName =
                        nameof(InvoiceHeader.GrossRcbAmount),

                    Header =
                        "Invoice Amount",

                    Width = 135,

                    ColumnType =
                        ListColumnType.Currency,

                    ShowSummary = true,

                    SummaryFormat =
                        "₹ {0:N2}"
                },

                new()
                {
                    FieldName =
                        nameof(InvoiceHeader.DiscountAmount),

                    Header =
                        "Discount",

                    Width = 110,

                    ColumnType =
                        ListColumnType.Currency,

                    ShowSummary = true,

                    SummaryFormat =
                        "₹ {0:N2}"
                },

                new()
                {
                    FieldName =
                        nameof(InvoiceHeader.AmountPayable),

                    Header =
                        "Payable",

                    Width = 125,

                    ColumnType =
                        ListColumnType.Currency,

                    ShowSummary = true,

                    SummaryFormat =
                        "₹ {0:N2}"
                },

                new()
                {
                    FieldName =
                        nameof(InvoiceHeader.RecdAmount),

                    Header =
                        "Received",

                    Width = 125,

                    ColumnType =
                        ListColumnType.Currency,

                    ShowSummary = true,

                    SummaryFormat =
                        "₹ {0:N2}"
                },

                new()
                {
                    FieldName =
                        nameof(InvoiceHeader.InvBalance),

                    Header =
                        "Balance",

                    Width = 125,

                    ColumnType =
                        ListColumnType.Currency,

                    ShowSummary = true,

                    SummaryFormat =
                        "₹ {0:N2}"
                },

                new()
                {
                    FieldName =
                        nameof(InvoiceHeader.PaymentDueDate),

                    Header =
                        "Due Date",

                    Width = 110,

                    ColumnType =
                        ListColumnType.Date
                },

                new()
                {
                    FieldName =
                        nameof(InvoiceHeader.Status),

                    Header =
                        "Status",

                    Width = 90
                }
            },

            RowFormats =
            {
                new()
                {
                    Expression =
                        $"[{nameof(InvoiceHeader.InvBalance)}] > 0",

                    PredefinedFormatName =
                        "RedText",

                    ApplyToRow = true
                }
            }
        };
    }
}