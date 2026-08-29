using System.Collections.Generic;

namespace InvEntry.Models.UI;

public sealed class ListViewDefinition
{
    public string Title { get; set; } =
        string.Empty;

    public string Description { get; set; } =
        string.Empty;

    public List<ListColumnDefinition> Columns { get; set; } =
        new();

    public ListFilterDefinition Filter { get; set; } =
        new();

    public List<RowFormatDefinition> RowFormats { get; set; } =
        new();

    public bool SupportsDocumentPrint { get; set; }

    public int DefaultFromDays { get; set; } = -1;

    public int DefaultToDays { get; set; } = 0;

}