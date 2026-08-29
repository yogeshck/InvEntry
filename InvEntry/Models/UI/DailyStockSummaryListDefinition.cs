using InvEntry.Models;

namespace InvEntry.Models.UI;

public static class DailyStockSummaryListDefinition
{
    public static ListViewDefinition Create()
    {
        return new ListViewDefinition
        {
            Title = "DAILY STOCK SUMMARY",

            Description =
                "Review daily stock movement by metal and product category",

            SupportsDocumentPrint = false,

            // Existing screen defaults to yesterday only
            DefaultFromDays = -1,
            DefaultToDays = -1,

            Filter = new ListFilterDefinition
            {
                Type = ListFilterType.None
            },

            Columns =
{
    new()
    {
        FieldName = nameof(DailyStockSummary.TransactionDate),
        Header = "Date",
        Width = 90,
        ColumnType = ListColumnType.Date
    },

    new()
    {
        FieldName = nameof(DailyStockSummary.Metal),
        Header = "Metal",
        Width = 65
    },

    new()
    {
        FieldName = nameof(DailyStockSummary.ProductCategory),
        Header = "Category",
        Width = 95
    },

    // OPENING
    new()
    {
        FieldName = nameof(DailyStockSummary.OpeningStockGrossWeight),
        Header = "Open Gross",
        Width = 85,
        ColumnType = ListColumnType.Weight,
        ShowZeroAsDash = true,
        ShowSummary = true,
        SummaryFormat = "{0:N3}"
    },

    new()
    {
        FieldName = nameof(DailyStockSummary.OpeningStockStoneWeight),
        Header = "Open Stone",
        Width = 82,
        ColumnType = ListColumnType.Weight,
        ShowZeroAsDash = true,
        ShowSummary = true,
        SummaryFormat = "{0:N3}"
    },

    new()
    {
        FieldName = nameof(DailyStockSummary.OpeningStockNetWeight),
        Header = "Open Net",
        Width = 82,
        ColumnType = ListColumnType.Weight,
        ShowZeroAsDash = true,
        ShowSummary = true,
        SummaryFormat = "{0:N3}"
    },

    new()
    {
        FieldName = nameof(DailyStockSummary.OpeningStockQty),
        Header = "Open Qty",
        Width = 68,
        ColumnType = ListColumnType.Integer,
        ShowZeroAsDash = true,
        ShowSummary = true,
        SummaryFormat = "{0:N0}"
    },

    // STOCK IN - compact
    new()
    {
        FieldName = nameof(DailyStockSummary.StockInGrossWeight),
        Header = "In Gross",
        Width = 72,
        ColumnType = ListColumnType.Weight,
        ShowZeroAsDash = true,
        ShowSummary = true,
        SummaryFormat = "{0:N3}"
    },

    new()
    {
        FieldName = nameof(DailyStockSummary.StockInStoneWeight),
        Header = "In Stone",
        Width = 70,
        ColumnType = ListColumnType.Weight,
        ShowZeroAsDash = true,
        ShowSummary = true,
        SummaryFormat = "{0:N3}"
    },

    new()
    {
        FieldName = nameof(DailyStockSummary.StockInNetWeight),
        Header = "In Net",
        Width = 68,
        ColumnType = ListColumnType.Weight,
        ShowZeroAsDash = true,
        ShowSummary = true,
        SummaryFormat = "{0:N3}"
    },

    new()
    {
        FieldName = nameof(DailyStockSummary.StockInQty),
        Header = "In Qty",
        Width = 58,
        ColumnType = ListColumnType.Integer,
        ShowZeroAsDash = true,
        ShowSummary = true,
        SummaryFormat = "{0:N0}"
    },

    // STOCK OUT - compact
    new()
    {
        FieldName = nameof(DailyStockSummary.StockOutGrossWeight),
        Header = "Out Gross",
        Width = 76,
        ColumnType = ListColumnType.Weight,
        ShowZeroAsDash = true,
        ShowSummary = true,
        SummaryFormat = "{0:N3}"
    },

    new()
    {
        FieldName = nameof(DailyStockSummary.StockOutStoneWeight),
        Header = "Out Stone",
        Width = 74,
        ColumnType = ListColumnType.Weight,
        ShowZeroAsDash = true,
        ShowSummary = true,
        SummaryFormat = "{0:N3}"
    },

    new()
    {
        FieldName = nameof(DailyStockSummary.StockOutNetWeight),
        Header = "Out Net",
        Width = 70,
        ColumnType = ListColumnType.Weight,
        ShowZeroAsDash = true,
        ShowSummary = true,
        SummaryFormat = "{0:N3}"
    },

    new()
    {
        FieldName = nameof(DailyStockSummary.StockOutQty),
        Header = "Out Qty",
        Width = 60,
        ColumnType = ListColumnType.Integer,
        ShowZeroAsDash = true,
        ShowSummary = true,
        SummaryFormat = "{0:N0}"
    },

    // CLOSING
    new()
    {
        FieldName = nameof(DailyStockSummary.ClosingStockGrossWeight),
        Header = "Close Gross",
        Width = 88,
        ColumnType = ListColumnType.Weight,
        ShowZeroAsDash = true,
        ShowSummary = true,
        SummaryFormat = "{0:N3}"
    },

    new()
    {
        FieldName = nameof(DailyStockSummary.ClosingStockStoneWeight),
        Header = "Close Stone",
        Width = 86,
        ColumnType = ListColumnType.Weight,
        ShowZeroAsDash = true,
        ShowSummary = true,
        SummaryFormat = "{0:N3}"
    },

    new()
    {
        FieldName = nameof(DailyStockSummary.ClosingStockNetWeight),
        Header = "Close Net",
        Width = 84,
        ColumnType = ListColumnType.Weight,
        ShowZeroAsDash = true,
        ShowSummary = true,
        SummaryFormat = "{0:N3}"
    },

    new()
    {
        FieldName = nameof(DailyStockSummary.ClosingStockQty),
        Header = "Close Qty",
        Width = 70,
        ColumnType = ListColumnType.Integer,
        ShowZeroAsDash = true,
        ShowSummary = true,
        SummaryFormat = "{0:N0}"
    }
}

        };
    }
}