using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models.UI
{

    public enum ListColumnType
    {
        Text,
        Integer,
        Decimal,
        Currency,
        Date,
        DateTime,
        Weight
    }

    public sealed class ListColumnDefinition
    {
        public string FieldName { get; set; } =
            string.Empty;

        public string Header { get; set; } =
            string.Empty;

        public double Width { get; set; } =
            100;

        public ListColumnType ColumnType { get; set; } =
            ListColumnType.Text;

        public bool ReadOnly { get; set; } =
            true;

        public bool Visible { get; set; } =
            true;

        public string? Format { get; set; }

        public bool ShowSummary { get; set; }

        public string? SummaryFormat { get; set; }

        // Privacy
        public bool MaskValue { get; set; }

        public int VisibleLastCharacters { get; set; } = 4;

    }
}
