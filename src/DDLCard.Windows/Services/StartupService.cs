using Microsoft.Win32;

namespace DDLCard.Windows.Services;

public static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DDLCard";

    public static void SetEnabled(bool enabled)
    {
        if (AppContext.BaseDirectory.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("无法打开 Windows 启动项设置。");
        if (!enabled)
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var processPath = Environment.ProcessPath ?? throw new InvalidOperationException("无法确定程序路径。");
        key.SetValue(ValueName, $"\"{processPath}\"");
    }
}
