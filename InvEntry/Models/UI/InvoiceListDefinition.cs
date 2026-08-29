using InvEntry.Models;

namespace InvEntry.Models.UI;

public static class InvoiceListDefinition
{
    public static ListViewDefinition Create()
    {
        return new ListViewDefinition
        {
            Title = "INVOICE LIST",

            Description =
                "Browse, search and manage invoice transactions",

            SupportsDocumentPrint = true,

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
                    FieldName = nameof(InvoiceHeader.InvNbr),
                    Header = "Invoice #",
                    Width = 110
                },

                new()
                {
                    FieldName = nameof(InvoiceHeader.InvDate),
                    Header = "Date",
                    Width = 110,
                    ColumnType = ListColumnType.Date
                },

                new()
                {
                    FieldName = nameof(InvoiceHeader.CustMobile),
                    Header = "Mobile #",
                    Width = 110,
                    MaskValue = true,
                    VisibleLastCharacters = 4
                },

                new()
                {
                    FieldName = nameof(InvoiceHeader.GrossRcbAmount),
                    Header = "Invoice Amount",
                    Width = 130,
                    ColumnType = ListColumnType.Currency,
                    ShowSummary = true,
                    SummaryFormat = "₹ {0:N2}"
                },

                new()
                {
                    FieldName = nameof(InvoiceHeader.DiscountAmount),
                    Header = "Discount",
                    Width = 105,
                    ColumnType = ListColumnType.Currency,
                    ShowSummary = true,
                    SummaryFormat = "₹ {0:N2}"
                },

                new()
                {
                    FieldName = nameof(InvoiceHeader.AdvanceAdj),
                    Header = "Advance",
                    Width = 105,
                    ColumnType = ListColumnType.Currency,
                    ShowSummary = true,
                    SummaryFormat = "₹ {0:N2}"
                },

                new()
                {
                    FieldName = nameof(InvoiceHeader.RdAmountAdj),
                    Header = "RD",
                    Width = 115,
                    ColumnType = ListColumnType.Currency,
                    ShowSummary = true,
                    SummaryFormat = "₹ {0:N2}"
                },

                new()
                {
                    FieldName = nameof(InvoiceHeader.AmountPayable),
                    Header = "Receivable",
                    Width = 115,
                    ColumnType = ListColumnType.Currency,
                    ShowSummary = true,
                    SummaryFormat = "₹ {0:N2}"
                },

                new()
                {
                    FieldName = nameof(InvoiceHeader.RecdAmount),
                    Header = "Received",
                    Width = 115,
                    ColumnType = ListColumnType.Currency,
                    ShowSummary = true,
                    SummaryFormat = "₹ {0:N2}"
                },

                new()
                {
                    FieldName = nameof(InvoiceHeader.InvBalance),
                    Header = "Balance",
                    Width = 115,
                    ColumnType = ListColumnType.Currency,
                    ShowSummary = true,
                    SummaryFormat = "₹ {0:N2}"
                },

                new()
                {
                    FieldName = nameof(InvoiceHeader.InvRefund),
                    Header = "Refund",
                    Width = 115,
                    ColumnType = ListColumnType.Currency,
                    ShowSummary = true,
                    SummaryFormat = "₹ {0:N2}"
                },

                new()
                {
                    FieldName = nameof(InvoiceHeader.PaymentDueDate),
                    Header = "Due Date",
                    Width = 110,
                    ColumnType = ListColumnType.Date
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