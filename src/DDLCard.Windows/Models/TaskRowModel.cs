using System.Windows.Media;
using DDLCard.Core.Models;
using DDLCard.Core.Services;

namespace DDLCard.Windows.Models;

public sealed class TaskRowModel
{
    public required DeadlineTask Task { get; init; }
    public Guid Id => Task.Id;
    public string Title => Task.Title;
    public string Category => string.IsNullOrWhiteSpace(Task.Category) ? "未分类" : Task.Category;
    public string Countdown => CountdownFormatter.Format(Task.DueAtUtc, DateTimeOffset.UtcNow);
    public string DueText => Task.DueAtUtc.ToLocalTime().ToString("M月d日 HH:mm");
    public string PriorityText => Task.Priority switch
    {
        TaskPriority.Low => "低",
        TaskPriority.Normal => "普通",
        TaskPriority.High => "高",
        TaskPriority.Urgent => "紧急",
        _ => "普通"
    };
    public int Progress => Task.ProgressPercent;
    public bool CanComplete => Task.State == TaskState.Active;
    public System.Windows.Media.Brush PriorityBrush => Task.Priority switch
    {
        TaskPriority.Low => new SolidColorBrush(System.Windows.Media.Color.FromRgb(99, 190, 160)),
        TaskPriority.Normal => new SolidColorBrush(System.Windows.Media.Color.FromRgb(108, 140, 255)),
        TaskPriority.High => new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 178, 87)),
        TaskPriority.Urgent => new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 107, 122)),
        _ => System.Windows.Media.Brushes.SlateBlue
    };
}
