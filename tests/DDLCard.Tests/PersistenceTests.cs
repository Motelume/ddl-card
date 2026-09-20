using DDLCard.Core.Models;
using DDLCard.Infrastructure.Backup;
using DDLCard.Infrastructure.Storage;

namespace DDLCard.Tests;

public sealed class PersistenceTests
{
    [Fact]
    public async Task SqliteRepository_PersistsAndOrdersTasks()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var repository = new SqliteTaskRepository(new AppDataPaths(root));
            await repository.UpsertAsync(new DeadlineTask { Title = "later", DueAtUtc = DateTimeOffset.UtcNow.AddDays(2) });
            await repository.UpsertAsync(new DeadlineTask { Title = "sooner", DueAtUtc = DateTimeOffset.UtcNow.AddDays(1) });

            var stored = await repository.GetAllAsync();

            Assert.Equal(["sooner", "later"], stored.Select(x => x.Title));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Backup_RestoresTasksAndSettings()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var paths = new AppDataPaths(root);
            var repository = new SqliteTaskRepository(paths);
            var settingsRepository = new JsonSettingsRepository(paths);
            var service = new BackupService(repository, settingsRepository);
            var backupPath = Path.Combine(root, "backup.json");
            await repository.UpsertAsync(new DeadlineTask { Title = "paper", DueAtUtc = DateTimeOffset.UtcNow.AddDays(3) });
            await settingsRepository.SaveAsync(new AppSettings { ThemeId = "paper" });
            await service.ExportAsync(backupPath);

            await repository.ReplaceAllAsync([]);
            await settingsRepository.SaveAsync(new AppSettings { ThemeId = "midnight" });
            var preview = await service.PreviewAsync(backupPath);
            await service.RestoreAsync(preview);

            Assert.Single(await repository.GetAllAsync());
            Assert.Equal("paper", (await settingsRepository.LoadAsync()).ThemeId);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTemporaryRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "ddlcard-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
