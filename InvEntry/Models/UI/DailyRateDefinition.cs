namespace InvEntry.Models.UI;

public sealed class DailyRateDefinition
{
    public int GKey { get; set; }

    public string Metal { get; set; } = string.Empty;

    public string? Purity { get; set; }

    public string? Carat { get; set; }

    public int DisplayOrder { get; set; }

    public bool TrackDailyRate { get; set; }

    public bool ShowInHeader { get; set; }
}