using System.Collections.ObjectModel;

namespace InvEntry.Models.UI;

public enum ListFilterType
{
    None,
    Text,
    Selection
}

public sealed class ListFilterDefinition
{
    public string Label { get; set; } =
        string.Empty;

    public string Placeholder { get; set; } =
        string.Empty;

    public ListFilterType Type { get; set; } =
        ListFilterType.None;

    public ObservableCollection<string> Options { get; set; } =
        new();

}
