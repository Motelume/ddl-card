using DDLCard.Core.Abstractions;
using DDLCard.Core.Models;
using DDLCard.Infrastructure.Backup;
using DDLCard.Infrastructure.Storage;

namespace DDLCard.Windows.Services;

public sealed class AppServices
{
    public AppServices()
    {
        Paths = new AppDataPaths();
        Tasks = new SqliteTaskRepository(Paths);
        ReminderDeliveries = new SqliteReminderDeliveryStore(Paths);
        SettingsRepository = new JsonSettingsRepository(Paths);
        Backup = new BackupService(Tasks, SettingsRepository);
    }

    public AppDataPaths Paths { get; }
    public ITaskRepository Tasks { get; }
    public IReminderDeliveryStore ReminderDeliveries { get; }
    public ISettingsRepository SettingsRepository { get; }
    public BackupService Backup { get; }
    public AppSettings Settings { get; private set; } = new();
    public event EventHandler? TasksChanged;
    public event EventHandler? SettingsChanged;

    public async Task InitializeAsync() => Settings = await SettingsRepository.LoadAsync();

    public async Task SaveTaskAsync(DeadlineTask task)
    {
        await Tasks.UpsertAsync(task);
        TasksChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task CompleteTaskAsync(Guid id)
    {
        var task = await Tasks.GetAsync(id);
        if (task is null) return;
        task.State = TaskState.Completed;
        task.CompletedAtUtc = DateTimeOffset.UtcNow;
        task.ManualProgressPercent = 100;
        foreach (var subtask in task.Subtasks) subtask.IsCompleted = true;
        await SaveTaskAsync(task);
    }

    public async Task PostponeTaskAsync(Guid id, TimeSpan delay)
    {
        var task = await Tasks.GetAsync(id);
        if (task is null) return;
        task.DueAtUtc = delay == TimeSpan.FromDays(1)
            ? NextLocalMorning(1)
            : delay == TimeSpan.FromDays(7)
                ? NextLocalMorning(7)
                : task.DueAtUtc.Add(delay);
        await SaveTaskAsync(task);
    }

    public async Task ArchiveTaskAsync(Guid id)
    {
        var task = await Tasks.GetAsync(id);
        if (task is null) return;
        task.State = TaskState.Archived;
        await SaveTaskAsync(task);
    }

    public async Task ReopenTaskAsync(Guid id)
    {
        var task = await Tasks.GetAsync(id);
        if (task is null) return;
        task.State = TaskState.Active;
        task.CompletedAtUtc = null;
        await SaveTaskAsync(task);
    }

    public async Task DeleteTaskAsync(Guid id)
    {
        await Tasks.DeleteAsync(id);
        TasksChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveSettingsAsync()
    {
        await SettingsRepository.SaveAsync(Settings);
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task ReloadAfterRestoreAsync()
    {
        Settings = await SettingsRepository.LoadAsync();
        TasksChanged?.Invoke(this, EventArgs.Empty);
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static DateTimeOffset NextLocalMorning(int days)
    {
        var local = DateTime.Today.AddDays(days).AddHours(9);
        return new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local)).ToUniversalTime();
    }
}
