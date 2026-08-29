using InvEntry.Models;

namespace InvEntry.Models.UI;

public static class GRNListDefinition
{
    public static ListViewDefinition Create()
    {
        return new ListViewDefinition
        {
            Title = "MATERIAL RECEIPT LIST",

            Description =
                "Browse and review material receipts from suppliers",

            SupportsDocumentPrint = false,

            Filter = new ListFilterDefinition
            {
                Type = ListFilterType.None
            },

            Columns =
            {
                new()
                {
                    FieldName = nameof(GrnDbView.GrnNbr),
                    Header = "GRN #",
                    Width = 110
                },

                new()
                {
                    FieldName = nameof(GrnDbView.GrnDate),
                    Header = "GRN Date",
                    Width = 110,
                    ColumnType = ListColumnType.Date
                },

                new()
                {
                    FieldName = nameof(GrnDbView.SupplierId),
                    Header = "Supplier",
                    Width = 120
                },

                new()
                {
                    FieldName = nameof(GrnDbView.DocumentType),
                    Header = "Doc Type",
                    Width = 110
                },

                new()
                {
                    FieldName = nameof(GrnDbView.ItemReceivedDate),
                    Header = "Item Recd On",
                    Width = 120,
                    ColumnType = ListColumnType.Date
                },

                new()
                {
                    FieldName = nameof(GrnDbView.LineNbr),
                    Header = "Line #",
                    Width = 70,
                    ColumnType = ListColumnType.Integer
                },

                new()
                {
                    FieldName = nameof(GrnDbView.ProductCategory),
                    Header = "Category",
                    Width = 130
                },

                new()
                {
                    FieldName = nameof(GrnDbView.SuppliedQty),
                    Header = "Qty",
                    Width = 75,
                    ColumnType = ListColumnType.Integer,
                    ShowSummary = true,
                    SummaryFormat = "{0:N0}"
                },

                new()
                {
                    FieldName = nameof(GrnDbView.GrossWeight),
                    Header = "Gross Wt",
                    Width = 105,
                    ColumnType = ListColumnType.Weight,
                    ShowSummary = true,
                    SummaryFormat = "{0:N3}"
                },

                new()
                {
                    FieldName = nameof(GrnDbView.StoneWeight),
                    Header = "Stone Wt",
                    Width = 105,
                    ColumnType = ListColumnType.Weight,
                    ShowSummary = true,
                    SummaryFormat = "{0:N3}"
                },

                new()
                {
                    FieldName = nameof(GrnDbView.NetWeight),
                    Header = "Net Wt",
                    Width = 105,
                    ColumnType = ListColumnType.Weight,
                    ShowSummary = true,
                    SummaryFormat = "{0:N3}"
                }
            }
        };
    }
}