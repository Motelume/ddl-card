using System.Text.Json;
using DDLCard.Core.Abstractions;
using DDLCard.Core.Contracts;
using DDLCard.Infrastructure.Serialization;

namespace DDLCard.Infrastructure.Backup;

public sealed class BackupService(ITaskRepository tasks, ISettingsRepository settings)
{
    private readonly JsonSerializerOptions _options = JsonDefaults.Create(indented: true);

    public async Task ExportAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var document = new BackupDocument
        {
            Tasks = [.. await tasks.GetAllAsync(cancellationToken)],
            Settings = await settings.LoadAsync(cancellationToken)
        };

        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, document, _options, cancellationToken);
    }

    public async Task<BackupDocument> PreviewAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        var document = await JsonSerializer.DeserializeAsync<BackupDocument>(stream, _options, cancellationToken)
            ?? throw new InvalidDataException("备份文件为空。");
        if (document.SchemaVersion != BackupDocument.CurrentSchemaVersion)
        {
            throw new InvalidDataException($"不支持备份版本 {document.SchemaVersion}。");
        }

        return document;
    }

    public async Task RestoreAsync(BackupDocument document, CancellationToken cancellationToken = default)
    {
        if (document.SchemaVersion != BackupDocument.CurrentSchemaVersion)
        {
            throw new InvalidDataException($"不支持备份版本 {document.SchemaVersion}。");
        }

        await tasks.ReplaceAllAsync(document.Tasks, cancellationToken);
        await settings.SaveAsync(document.Settings, cancellationToken);
    }
}

