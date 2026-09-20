using DDLCard.Core.Models;

namespace DDLCard.Core.Contracts;

public sealed class PresetDocument
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public string ThemeId { get; set; } = "midnight";
    public string AccentColor { get; set; } = "#7C8CFF";
    public double CardOpacity { get; set; } = 0.94;
    public double CornerRadius { get; set; } = 22;
    public double FontScale { get; set; } = 1.0;
    public bool AlwaysOnTop { get; set; }
    public bool EdgeCollapseEnabled { get; set; }
    public List<int> DefaultReminderOffsetsMinutes { get; set; } = [10080, 4320, 1440, 60, 10];

    public static PresetDocument FromSettings(AppSettings settings) => new()
    {
        ThemeId = settings.ThemeId,
        AccentColor = settings.AccentColor,
        CardOpacity = settings.CardOpacity,
        CornerRadius = settings.CornerRadius,
        FontScale = settings.FontScale,
        AlwaysOnTop = settings.AlwaysOnTop,
        EdgeCollapseEnabled = settings.EdgeCollapseEnabled,
        DefaultReminderOffsetsMinutes = [.. settings.DefaultReminderOffsetsMinutes]
    };

    public void ApplyTo(AppSettings settings)
    {
        settings.ThemeId = ThemeId;
        settings.AccentColor = AccentColor;
        settings.CardOpacity = CardOpacity;
        settings.CornerRadius = CornerRadius;
        settings.FontScale = FontScale;
        settings.AlwaysOnTop = AlwaysOnTop;
        settings.EdgeCollapseEnabled = EdgeCollapseEnabled;
        settings.DefaultReminderOffsetsMinutes = [.. DefaultReminderOffsetsMinutes];
    }
}

