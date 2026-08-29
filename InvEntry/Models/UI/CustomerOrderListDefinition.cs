using InvEntry.Models;

namespace InvEntry.Models.UI;

public static class CustomerOrderListDefinition
{
    public static ListViewDefinition Create()
    {
        return new ListViewDefinition
        {
            Title = "CUSTOMER ORDERS",

            Description =
                "Browse and track customer orders and order items",

            SupportsDocumentPrint = false,

            Filter = new ListFilterDefinition
            {
                Type = ListFilterType.None
            },

            Columns =
            {
                new()
                {
                    FieldName =
                        nameof(CustomerOrderDBView.OrderNbr),

                    Header = "Order #",
                    Width = 110
                },

                new()
                {
                    FieldName =
                        nameof(CustomerOrderDBView.OrderDate),

                    Header = "Order Date",
                    Width = 110,

                    ColumnType =
                        ListColumnType.Date
                },

                new()
                {
                    FieldName =
                        nameof(CustomerOrderDBView.OrderRefNbr),

                    Header = "Order Ref #",
                    Width = 110
                },

                new()
                {
                    FieldName =
                        nameof(CustomerOrderDBView.CustomerName),

                    Header = "Customer Name",
                    Width = 190
                },

                new()
                {
                    FieldName =
                        nameof(CustomerOrderDBView.CustMobileNbr),

                    Header = "Contact #",
                    Width = 110,

                    MaskValue = true,
                    VisibleLastCharacters = 4
                },

                new()
                {
                    FieldName =
                        nameof(CustomerOrderDBView.OrderType),

                    Header = "Order Type",
                    Width = 105
                },

                new()
                {
                    FieldName =
                        nameof(CustomerOrderDBView.OrderStatus),

                    Header = "Status",
                    Width = 110
                },

                new()
                {
                    FieldName =
                        nameof(CustomerOrderDBView.OrderDueDate),

                    Header = "Due Date",
                    Width = 110,

                    ColumnType =
                        ListColumnType.Date
                },

                new()
                {
                    FieldName =
                        nameof(CustomerOrderDBView.OrderLineNbr),

                    Header = "Line",
                    Width = 60,

                    ColumnType =
                        ListColumnType.Integer
                },

                new()
                {
                    FieldName =
                        nameof(CustomerOrderDBView.ProdCategory),

                    Header = "Item",
                    Width = 120
                },

                new()
                {
                    FieldName =
                        nameof(CustomerOrderDBView.ProdQty),

                    Header = "Ord Qty",
                    Width = 80,

                    ColumnType =
                        ListColumnType.Integer,

                    ShowSummary = true,
                    SummaryFormat = "{0:N0}"
                },

                new()
                {
                    FieldName =
                        nameof(CustomerOrderDBView.ProdGrossWeight),

                    Header = "Gross Wt",
                    Width = 100,

                    ColumnType =
                        ListColumnType.Weight,

                    ShowSummary = true,
                    SummaryFormat = "{0:N3}"
                },

                new()
                {
                    FieldName =
                        nameof(CustomerOrderDBView.ProdStoneWeight),

                    Header = "Stone Wt",
                    Width = 100,

                    ColumnType =
                        ListColumnType.Weight,

                    ShowSummary = true,
                    SummaryFormat = "{0:N3}"
                },

                new()
                {
                    FieldName =
                        nameof(CustomerOrderDBView.ProdNetWeight),

                    Header = "Net Wt",
                    Width = 100,

                    ColumnType =
                        ListColumnType.Weight,

                    ShowSummary = true,
                    SummaryFormat = "{0:N3}"
                }
            }
        };
    }
}