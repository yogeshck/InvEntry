using InvEntry.Models;

namespace InvEntry.Models.UI;

public static class EstimateListDefinition
{
    public static ListViewDefinition Create()
    {
        return new ListViewDefinition
        {
            Title = "ESTIMATE LIST",

            Description =
                "Browse, search and manage estimate transactions",

            SupportsDocumentPrint = true,

            Filter = new ListFilterDefinition
            {
                Type = ListFilterType.None
            },

            Columns =
            {
                new()
                {
                    FieldName = nameof(EstimateHeader.EstNbr),
                    Header = "Estimate #",
                    Width = 110
                },

                new()
                {
                    FieldName = nameof(EstimateHeader.EstDate),
                    Header = "Date",
                    Width = 110,
                    ColumnType = ListColumnType.Date
                },

                new()
                {
                    FieldName = nameof(EstimateHeader.GrossRcbAmount),
                    Header = "Total Amount",
                    Width = 130,
                    ColumnType = ListColumnType.Currency,
                    ShowSummary = true,
                    SummaryFormat = "₹ {0:N2}"
                },

                new()
                {
                    FieldName = nameof(EstimateHeader.DiscountAmount),
                    Header = "Discount",
                    Width = 105,
                    ColumnType = ListColumnType.Currency,
                    ShowSummary = true,
                    SummaryFormat = "₹ {0:N2}"
                },

                new()
                {
                    FieldName = nameof(EstimateHeader.AmountPayable),
                    Header = "Receivable",
                    Width = 120,
                    ColumnType = ListColumnType.Currency,
                    ShowSummary = true,
                    SummaryFormat = "₹ {0:N2}"
                },

                new()
                {
                    FieldName = nameof(EstimateHeader.RecdAmount),
                    Header = "Received",
                    Width = 120,
                    ColumnType = ListColumnType.Currency,
                    ShowSummary = true,
                    SummaryFormat = "₹ {0:N2}"
                },

                new()
                {
                    FieldName = nameof(EstimateHeader.EstBalance),
                    Header = "Balance",
                    Width = 120,
                    ColumnType = ListColumnType.Currency,
                    ShowSummary = true,
                    SummaryFormat = "₹ {0:N2}"
                },

                new()
                {
                    FieldName = nameof(EstimateHeader.PaymentDueDate),
                    Header = "Follow Up Date",
                    Width = 110,
                    ColumnType = ListColumnType.Date
                }
            },

            RowFormats =
            {
                new()
                {
                    Expression =
                        $"[{nameof(EstimateHeader.EstBalance)}] > 0",

                    PredefinedFormatName =
                        "RedText",

                    ApplyToRow = true
                }
            }
        };
    }
}