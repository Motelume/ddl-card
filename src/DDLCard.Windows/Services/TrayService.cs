using Forms = System.Windows.Forms;

namespace DDLCard.Windows.Services;

public sealed class TrayService : IDisposable
{
    private readonly App _app;
    private readonly Forms.NotifyIcon _icon;

    public TrayService(App app)
    {
        _app = app;
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("打开任务管理器", null, (_, _) => _app.Dispatcher.Invoke(_app.ShowManager));
        menu.Items.Add("快速添加", null, (_, _) => _app.Dispatcher.Invoke(_app.ShowQuickAdd));
        menu.Items.Add("显示/隐藏桌面卡片", null, (_, _) => _app.Dispatcher.Invoke(ToggleCard));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("退出 DDLCard", null, (_, _) => _app.Dispatcher.Invoke(_app.ExitApplication));
        _icon = new Forms.NotifyIcon
        {
            Text = "DDLCard",
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = menu
        };
        _icon.DoubleClick += (_, _) => _app.Dispatcher.Invoke(_app.ShowManager);
    }

    public void ShowInfo(string title, string message) => _icon.ShowBalloonTip(4000, title, message, Forms.ToolTipIcon.Info);
    public void ShowReminder(string title, string message) => _icon.ShowBalloonTip(8000, title, message, Forms.ToolTipIcon.Warning);

    private void ToggleCard()
    {
        if (_app.DesktopCardWindow.IsVisible) _app.DesktopCardWindow.Hide();
        else _app.DesktopCardWindow.Show();
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}

