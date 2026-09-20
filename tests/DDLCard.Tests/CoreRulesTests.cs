using DDLCard.Core.Contracts;
using DDLCard.Core.Models;
using DDLCard.Core.Services;

namespace DDLCard.Tests;

public sealed class CoreRulesTests
{
    [Fact]
    public void Countdown_UsesDaysHoursMinutesWithoutSeconds()
    {
        var now = new DateTimeOffset(2026, 9, 21, 8, 0, 45, TimeSpan.Zero);
        var due = now.AddDays(2).AddHours(3).AddMinutes(7).AddSeconds(50);

        Assert.Equal("还剩 2天 3小时 7分钟", CountdownFormatter.Format(due, now));
    }

    [Fact]
    public void Countdown_HidesDaysBelowOneDayAndLabelsOverdue()
    {
        var now = new DateTimeOffset(2026, 9, 21, 8, 0, 0, TimeSpan.Zero);

        Assert.Equal("已逾期 2小时 15分钟", CountdownFormatter.Format(now.AddHours(-2).AddMinutes(-15), now));
    }

    [Fact]
    public void CardOrdering_IsDeadlineFirstThenPriority()
    {
        var now = DateTimeOffset.UtcNow;
        var laterUrgent = NewTask("later", now.AddDays(2), TaskPriority.Urgent);
        var soonerLow = NewTask("sooner", now.AddDays(1), TaskPriority.Low);
        var sameTimeHigh = NewTask("same-high", now.AddDays(1), TaskPriority.High);

        var ordered = TaskOrdering.ForCard([laterUrgent, soonerLow, sameTimeHigh]);

        Assert.Equal(["same-high", "sooner", "later"], ordered.Select(x => x.Title));
    }

    [Fact]
    public void Progress_IsCalculatedFromSubtasks()
    {
        var task = NewTask("paper", DateTimeOffset.UtcNow.AddDays(1), TaskPriority.Normal);
        task.ManualProgressPercent = 99;
        task.Subtasks =
        [
            new() { Title = "A", IsCompleted = true },
            new() { Title = "B", IsCompleted = false },
            new() { Title = "C", IsCompleted = true }
        ];

        Assert.Equal(67, task.ProgressPercent);
    }

    [Fact]
    public void PresetCode_RoundTripsWithoutTaskData()
    {
        var settings = new AppSettings { ThemeId = "paper", CardOpacity = 0.8, AlwaysOnTop = true };
        var code = PresetCodec.Encode(PresetDocument.FromSettings(settings));

        var result = PresetCodec.Decode(code);

        Assert.Equal("paper", result.ThemeId);
        Assert.Equal(0.8, result.CardOpacity);
        Assert.True(result.AlwaysOnTop);
        Assert.DoesNotContain("task", code, StringComparison.OrdinalIgnoreCase);
    }

    private static DeadlineTask NewTask(string title, DateTimeOffset due, TaskPriority priority) => new()
    {
        Title = title,
        DueAtUtc = due,
        Priority = priority
    };
}
