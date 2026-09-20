namespace DDLCard.Core.Models;

public sealed class AppSettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public string ThemeId { get; set; } = "midnight";
    public string AccentColor { get; set; } = "#7C8CFF";
    public double CardOpacity { get; set; } = 0.94;
    public double CornerRadius { get; set; } = 22;
    public double FontScale { get; set; } = 1.0;
    public double CardWidth { get; set; } = 420;
    public double CardHeight { get; set; } = 560;
    public double CardLeft { get; set; } = 80;
    public double CardTop { get; set; } = 80;
    public bool AlwaysOnTop { get; set; }
    public bool EdgeCollapseEnabled { get; set; }
    public bool StartWithWindows { get; set; } = true;
    public List<int> DefaultReminderOffsetsMinutes { get; set; } = [10080, 4320, 1440, 60, 10];
}

