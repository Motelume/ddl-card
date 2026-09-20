namespace DDLCard.Core.Models;

public sealed class DeadlineTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#6C8CFF";
    public DateTimeOffset DueAtUtc { get; set; } = DateTimeOffset.UtcNow.AddDays(1);
    public string TimeZoneId { get; set; } = TimeZoneInfo.Local.Id;
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;
    public TaskState State { get; set; } = TaskState.Active;
    public int ManualProgressPercent { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public List<DeadlineSubtask> Subtasks { get; set; } = [];
    public List<int> ReminderOffsetsMinutes { get; set; } = [10080, 4320, 1440, 60, 10];

    public int ProgressPercent => Subtasks.Count == 0
        ? Math.Clamp(ManualProgressPercent, 0, 100)
        : (int)Math.Round(Subtasks.Count(x => x.IsCompleted) * 100d / Subtasks.Count);

    public bool IsOverdue(DateTimeOffset nowUtc) =>
        State == TaskState.Active && DueAtUtc < nowUtc;
}

