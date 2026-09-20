using System.Windows.Threading;
using DDLCard.Core.Abstractions;
using DDLCard.Core.Models;
using DDLCard.Core.Services;

namespace DDLCard.Windows.Services;

public sealed class ReminderScheduler : IDisposable
{
    private readonly ITaskRepository _tasks;
    private readonly IReminderDeliveryStore _deliveries;
    private readonly TrayService _tray;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(30) };
    private bool _checking;

    public ReminderScheduler(ITaskRepository tasks, IReminderDeliveryStore deliveries, TrayService tray)
    {
        _tasks = tasks;
        _deliveries = deliveries;
        _tray = tray;
        _timer.Tick += async (_, _) => await CheckAsync();
    }

    public void Start()
    {
        _timer.Start();
        _ = CheckAsync();
    }

    private async Task CheckAsync()
    {
        if (_checking) return;
        _checking = true;
        try
        {
            var now = DateTimeOffset.UtcNow;
            var active = (await _tasks.GetAllAsync()).Where(x => x.State == TaskState.Active);
            foreach (var task in active)
            {
                foreach (var offset in task.ReminderOffsetsMinutes)
                {
                    var triggerAt = task.DueAtUtc.AddMinutes(-offset);
                    if (now < triggerAt || now - triggerAt > TimeSpan.FromMinutes(10)) continue;
                    if (await _deliveries.WasDeliveredAsync(task.Id, task.DueAtUtc, offset)) continue;
                    _tray.ShowReminder(task.Title, $"{CountdownFormatter.Format(task.DueAtUtc, now)} · 截止 {task.DueAtUtc.ToLocalTime():M月d日 HH:mm}");
                    await _deliveries.MarkDeliveredAsync(task.Id, task.DueAtUtc, offset, now);
                }
            }
        }
        finally
        {
            _checking = false;
        }
    }

    public void Dispose() => _timer.Stop();
}
