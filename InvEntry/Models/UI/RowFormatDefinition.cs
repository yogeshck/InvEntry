namespace InvEntry.Models.UI;

public sealed class RowFormatDefinition
{
    public string Expression { get; set; } =
        string.Empty;

    public string PredefinedFormatName { get; set; } =
        string.Empty;

    public bool ApplyToRow { get; set; } =
        true;
}