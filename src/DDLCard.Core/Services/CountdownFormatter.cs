namespace DDLCard.Core.Services;

public static class CountdownFormatter
{
    public static string Format(DateTimeOffset dueAtUtc, DateTimeOffset nowUtc)
    {
        var overdue = dueAtUtc < nowUtc;
        var duration = (overdue ? nowUtc - dueAtUtc : dueAtUtc - nowUtc).Duration();
        var totalMinutes = Math.Max(0, (int)Math.Floor(duration.TotalMinutes));
        var days = totalMinutes / 1440;
        var hours = totalMinutes % 1440 / 60;
        var minutes = totalMinutes % 60;
        var value = days > 0
            ? $"{days}天 {hours}小时 {minutes}分钟"
            : $"{hours}小时 {minutes}分钟";

        return overdue ? $"已逾期 {value}" : $"还剩 {value}";
    }
}

