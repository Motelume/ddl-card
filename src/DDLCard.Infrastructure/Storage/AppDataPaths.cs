namespace DDLCard.Infrastructure.Storage;

public sealed class AppDataPaths
{
    public AppDataPaths(string? rootDirectory = null)
    {
        RootDirectory = rootDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DDLCard");
    }

    public string RootDirectory { get; }
    public string DatabasePath => Path.Combine(RootDirectory, "ddlcard.db");
    public string SettingsPath => Path.Combine(RootDirectory, "settings.json");

    public void EnsureCreated() => Directory.CreateDirectory(RootDirectory);
}

