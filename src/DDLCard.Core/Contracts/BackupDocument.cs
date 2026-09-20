using DDLCard.Core.Models;

namespace DDLCard.Core.Contracts;

public sealed class BackupDocument
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public DateTimeOffset ExportedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string SourcePlatform { get; set; } = "windows";
    public List<DeadlineTask> Tasks { get; set; } = [];
    public AppSettings Settings { get; set; } = new();
}

