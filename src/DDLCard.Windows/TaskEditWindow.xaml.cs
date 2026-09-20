using System.Windows;
using DDLCard.Core.Models;

namespace DDLCard.Windows;

public partial class TaskEditWindow : Window
{
    private readonly DeadlineTask _task;
    private App AppInstance => (App)System.Windows.Application.Current;

    public TaskEditWindow(DeadlineTask? task = null)
    {
        InitializeComponent();
        _task = task ?? new DeadlineTask();
        LoadTask();
    }

    private void LoadTask()
    {
        var localDue = _task.DueAtUtc.ToLocalTime();
        Heading.Text = _task.Title.Length == 0 ? "新建截止事项" : "编辑截止事项";
        TitleBox.Text = _task.Title;
        DescriptionBox.Text = _task.Description;
        CategoryBox.Text = _task.Category;
        DueDatePicker.SelectedDate = localDue.Date;
        DueTimeBox.Text = localDue.ToString("HH:mm");
        PriorityBox.SelectedIndex = (int)_task.Priority;
        ProgressSlider.Value = _task.ManualProgressPercent;
        SubtasksBox.Text = string.Join(Environment.NewLine, _task.Subtasks.OrderBy(x => x.Position).Select(x => (x.IsCompleted ? "[x] " : "") + x.Title));
        RemindersBox.Text = string.Join(", ", _task.ReminderOffsetsMinutes.Select(FormatOffset));
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DueDatePicker.SelectedDate is not DateTime date || !TimeSpan.TryParse(DueTimeBox.Text.Trim(), out var time))
                throw new FormatException("请输入有效的截止日期和时间，例如 23:59。");
            _task.Title = TitleBox.Text;
            _task.Description = DescriptionBox.Text;
            _task.Category = CategoryBox.Text;
            _task.Priority = (TaskPriority)Math.Max(0, PriorityBox.SelectedIndex);
            _task.ManualProgressPercent = (int)ProgressSlider.Value;
            var local = DateTime.SpecifyKind(date.Date + time, DateTimeKind.Unspecified);
            _task.DueAtUtc = new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local)).ToUniversalTime();
            _task.TimeZoneId = TimeZoneInfo.Local.Id;
            _task.ReminderOffsetsMinutes = ParseOffsets(RemindersBox.Text);
            _task.Subtasks = ParseSubtasks(SubtasksBox.Text, _task.Subtasks);
            await AppInstance.Services.SaveTaskAsync(_task);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "无法保存", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static List<DeadlineSubtask> ParseSubtasks(string value, List<DeadlineSubtask> existing)
    {
        var result = new List<DeadlineSubtask>();
        var lines = value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < lines.Length; i++)
        {
            var completed = lines[i].StartsWith("[x] ", StringComparison.OrdinalIgnoreCase);
            var title = completed ? lines[i][4..].Trim() : lines[i];
            var old = existing.FirstOrDefault(x => x.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
            result.Add(new DeadlineSubtask { Id = old?.Id ?? Guid.NewGuid(), Title = title, IsCompleted = completed || old?.IsCompleted == true, Position = i });
        }
        return result;
    }

    private static List<int> ParseOffsets(string value)
    {
        var result = new List<int>();
        foreach (var token in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (token.Length < 2 || !double.TryParse(token[..^1], out var number) || number < 0) throw new FormatException($"无法识别提醒点“{token}”。");
            var minutes = char.ToLowerInvariant(token[^1]) switch
            {
                'd' => number * 1440,
                'h' => number * 60,
                'm' => number,
                _ => throw new FormatException($"提醒点“{token}”必须以 d、h 或 m 结尾。")
            };
            result.Add((int)Math.Round(minutes));
        }
        return result.Distinct().OrderByDescending(x => x).ToList();
    }

    private static string FormatOffset(int minutes) => minutes % 1440 == 0 ? $"{minutes / 1440}d" : minutes % 60 == 0 ? $"{minutes / 60}h" : $"{minutes}m";
    private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { if (ProgressText is not null) ProgressText.Text = $"{(int)e.NewValue}%"; }
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
