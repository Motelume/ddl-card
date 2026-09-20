using System.Windows;
using DDLCard.Windows.Services;

namespace DDLCard.Windows;

public partial class App : System.Windows.Application
{
    public AppServices Services { get; private set; } = null!;
    public MainWindow MainManagerWindow { get; private set; } = null!;
    public CardWindow DesktopCardWindow { get; private set; } = null!;
    public TrayService Tray { get; private set; } = null!;
    public ReminderScheduler Reminders { get; private set; } = null!;
    public bool IsShuttingDown { get; private set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Services = new AppServices();
        await Services.InitializeAsync();
        MainManagerWindow = new MainWindow();
        DesktopCardWindow = new CardWindow();
        Tray = new TrayService(this);
        Reminders = new ReminderScheduler(Services.Tasks, Services.ReminderDeliveries, Tray);
        DesktopCardWindow.Show();
        MainManagerWindow.Show();
        Reminders.Start();
    }

    public void ShowManager()
    {
        MainManagerWindow.Show();
        MainManagerWindow.WindowState = WindowState.Normal;
        MainManagerWindow.Activate();
    }

    public void ShowQuickAdd()
    {
        var window = new QuickAddWindow { Owner = MainManagerWindow.IsVisible ? MainManagerWindow : null };
        window.ShowDialog();
    }

    public void ExitApplication()
    {
        IsShuttingDown = true;
        Reminders.Dispose();
        Tray.Dispose();
        DesktopCardWindow.Close();
        MainManagerWindow.Close();
        Shutdown();
    }
}
