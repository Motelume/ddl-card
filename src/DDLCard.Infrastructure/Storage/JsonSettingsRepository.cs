using System.Text.Json;
using DDLCard.Core.Abstractions;
using DDLCard.Core.Models;
using DDLCard.Infrastructure.Serialization;

namespace DDLCard.Infrastructure.Storage;

public sealed class JsonSettingsRepository : ISettingsRepository
{
    private readonly AppDataPaths _paths;
    private readonly JsonSerializerOptions _options = JsonDefaults.Create(indented: true);

    public JsonSettingsRepository(AppDataPaths paths)
    {
        _paths = paths;
        _paths.EnsureCreated();
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_paths.SettingsPath))
        {
            return new AppSettings();
        }

        await using var stream = File.OpenRead(_paths.SettingsPath);
        var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, _options, cancellationToken);
        return settings?.SchemaVersion == AppSettings.CurrentSchemaVersion
            ? settings
            : new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        _paths.EnsureCreated();
        var temporaryPath = _paths.SettingsPath + ".tmp";
        await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(stream, settings, _options, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        File.Move(temporaryPath, _paths.SettingsPath, overwrite: true);
    }
}

