using InvEntry.Models;

namespace InvEntry.Models.UI;

public static class VoucherListDefinition
{
    public static ListViewDefinition Create()
    {
        return new ListViewDefinition
        {
            Title = "VOUCHER LIST",

            Description =
                "Browse and review cash and petty cash transactions",

            SupportsDocumentPrint = true,

            Filter = new ListFilterDefinition
            {
                Label = "Statement Type",
                Placeholder = "Select statement type",
                Type = ListFilterType.Selection,

                Options =
                {
                    "Cash",
                    "Petty Cash"
                }
            },

            Columns =
            {
                new()
                {
                    FieldName = nameof(VoucherDbView.VoucherNbr),
                    Header = "Voucher #",
                    Width = 105
                },

                new()
                {
                    FieldName = nameof(VoucherDbView.VoucherDate),
                    Header = "Voucher Date",
                    Width = 110,
                    ColumnType = ListColumnType.Date
                },

                new()
                {
                    FieldName = nameof(VoucherDbView.Mode),
                    Header = "Voucher Mode",
                    Width = 110
                },

                new()
                {
                    FieldName = nameof(VoucherDbView.TransType),
                    Header = "Type",
                    Width = 90
                },

                new()
                {
                    FieldName = nameof(VoucherDbView.TransAmount),
                    Header = "Trans Amt",
                    Width = 115,
                    ColumnType = ListColumnType.Currency,
                    ShowSummary = true,
                    SummaryFormat = "₹ {0:N2}"
                },

                new()
                {
                    FieldName = nameof(VoucherDbView.FromLedgerName),
                    Header = "From A/c",
                    Width = 150
                },

                new()
                {
                    FieldName = nameof(VoucherDbView.ToLedgerName),
                    Header = "To A/c",
                    Width = 150
                },

                new()
                {
                    FieldName = nameof(VoucherDbView.TransDesc),
                    Header = "Description",
                    Width = 200
                },

                new()
                {
                    FieldName = nameof(VoucherDbView.ObAmount),
                    Header = "Opening Cash",
                    Width = 120,
                    ColumnType = ListColumnType.Currency
                },

                new()
                {
                    FieldName = nameof(VoucherDbView.RecdAmount),
                    Header = "Receipt Amount",
                    Width = 125,
                    ColumnType = ListColumnType.Currency,
                    ShowSummary = true,
                    SummaryFormat = "₹ {0:N2}"
                },

                new()
                {
                    FieldName = nameof(VoucherDbView.PaidAmount),
                    Header = "Payment",
                    Width = 115,
                    ColumnType = ListColumnType.Currency,
                    ShowSummary = true,
                    SummaryFormat = "₹ {0:N2}"
                },

                new()
                {
                    FieldName = nameof(VoucherDbView.CbAmount),
                    Header = "Closing Cash",
                    Width = 120,
                    ColumnType = ListColumnType.Currency
                },

                new()
                {
                    FieldName = nameof(VoucherDbView.RefDocNbr),
                    Header = "Ref Doc #",
                    Width = 110
                },

                new()
                {
                    FieldName = nameof(VoucherDbView.RefDocDate),
                    Header = "Ref Doc Date",
                    Width = 110,
                    ColumnType = ListColumnType.Date
                }
            }
        };
    }
}