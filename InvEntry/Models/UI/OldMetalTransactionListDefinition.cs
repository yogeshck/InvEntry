using InvEntry.Models;

namespace InvEntry.Models.UI;

public static class OldMetalTransactionListDefinition
{
    public static ListViewDefinition Create()
    {
        return new ListViewDefinition
        {
            Title = "OLD METAL TRANSACTIONS",

            Description =
                "Browse and review old gold and old metal transactions",

            SupportsDocumentPrint = true,

            Filter = new ListFilterDefinition
            {
                Label = "Search",
                Placeholder = "Enter reference / mobile number",
                Type = ListFilterType.Text
            },

            Columns =
            {
                new()
                {
                    FieldName =
                        nameof(OldMetalTransaction.TransType),

                    Header = "Trans Type",
                    Width = 110
                },

                new()
                {
                    FieldName =
                        nameof(OldMetalTransaction.TransNbr),

                    Header = "Trans #",
                    Width = 110
                },

                new()
                {
                    FieldName =
                        nameof(OldMetalTransaction.TransDate),

                    Header = "Trans Date",
                    Width = 110,

                    ColumnType =
                        ListColumnType.Date
                },

                new()
                {
                    FieldName =
                        nameof(OldMetalTransaction.DocRefType),

                    Header = "Ref Type",
                    Width = 105
                },

                new()
                {
                    FieldName =
                        nameof(OldMetalTransaction.DocRefNbr),

                    Header = "Ref #",
                    Width = 110
                },

                new()
                {
                    FieldName =
                        nameof(OldMetalTransaction.DocRefDate),

                    Header = "Ref Date",
                    Width = 110,

                    ColumnType =
                        ListColumnType.Date
                },

                new()
                {
                    FieldName =
                        nameof(OldMetalTransaction.ProductId),

                    Header = "Product",
                    Width = 130
                },

                new()
                {
                    FieldName =
                        nameof(OldMetalTransaction.GrossWeight),

                    Header = "Gross Wt",
                    Width = 105,

                    ColumnType =
                        ListColumnType.Weight,

                    ShowSummary = true,
                    SummaryFormat = "{0:N3}"
                },

                new()
                {
                    FieldName =
                        nameof(OldMetalTransaction.StoneWeight),

                    Header = "Stone Wt",
                    Width = 105,

                    ColumnType =
                        ListColumnType.Weight,

                    ShowSummary = true,
                    SummaryFormat = "{0:N3}"
                },

                new()
                {
                    FieldName =
                        nameof(OldMetalTransaction.NetWeight),

                    Header = "Net Wt",
                    Width = 105,

                    ColumnType =
                        ListColumnType.Weight,

                    ShowSummary = true,
                    SummaryFormat = "{0:N3}"
                },

                new()
                {
                    FieldName =
                        nameof(OldMetalTransaction.FinalPurchasePrice),

                    Header = "Purchase Price",
                    Width = 130,

                    ColumnType =
                        ListColumnType.Currency,

                    ShowSummary = true,
                    SummaryFormat = "₹ {0:N2}"
                }
            }
        };
    }
}