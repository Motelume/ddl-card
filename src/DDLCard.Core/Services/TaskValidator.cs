using DDLCard.Core.Models;

namespace DDLCard.Core.Services;

public static class TaskValidator
{
    public static void Validate(DeadlineTask task)
    {
        task.Title = task.Title.Trim();
        task.Description = task.Description.Trim();
        task.Category = task.Category.Trim();

        if (string.IsNullOrWhiteSpace(task.Title))
        {
            throw new ArgumentException("任务标题不能为空。", nameof(task));
        }

        if (task.Title.Length > 160)
        {
            throw new ArgumentException("任务标题不能超过 160 个字符。", nameof(task));
        }

        if (task.Description.Length > 8000)
        {
            throw new ArgumentException("任务备注不能超过 8000 个字符。", nameof(task));
        }

        task.ManualProgressPercent = Math.Clamp(task.ManualProgressPercent, 0, 100);
        task.ReminderOffsetsMinutes = task.ReminderOffsetsMinutes
            .Where(x => x >= 0)
            .Distinct()
            .OrderByDescending(x => x)
            .ToList();

        foreach (var subtask in task.Subtasks)
        {
            subtask.Title = subtask.Title.Trim();
            if (string.IsNullOrWhiteSpace(subtask.Title))
            {
                throw new ArgumentException("子任务标题不能为空。", nameof(task));
            }
        }
    }
}

