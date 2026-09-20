using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using DDLCard.Core.Models;
using DDLCard.Core.Services;
using DDLCard.Windows.Models;
using Microsoft.Win32;

namespace DDLCard.Windows;

public partial class MainWindow : Window
{
    private const int HotkeyId = 0xDD1;
    private const uint ModControl = 0x0002;
    private const uint ModAlt = 0x0001;
    private const uint VirtualKeyD = 0x44;
    private List<DeadlineTask> _allTasks = [];
    private App AppInstance => (App)System.Windows.Application.Current;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await RefreshAsync();
        Closing += OnClosing;
        SourceInitialized += OnSourceInitialized;
        AppInstance.Services.TasksChanged += async (_, _) => await Dispatcher.InvokeAsync(RefreshAsync);
    }

    private async Task RefreshAsync()
    {
        _allTasks = [.. await AppInstance.Services.Tasks.GetAllAsync()];
        ApplyFilter();
        StatusText.Text = $"本地保存 · 共 {_allTasks.Count} 项";
    }

    private void ApplyFilter()
    {
        var query = SearchBox?.Text.Trim() ?? string.Empty;
        var stateTag = (StateFilter?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Active";
        var filtered = _allTasks.Where(task =>
            (stateTag == "All" || task.State.ToString() == stateTag) &&
            (string.IsNullOrEmpty(query) || task.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             task.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             task.Description.Contains(query, StringComparison.OrdinalIgnoreCase)));
        var ordered = stateTag == "Active" ? TaskOrdering.ForCard(filtered) : filtered.OrderByDescending(x => x.UpdatedAtUtc).ToList();
        var rows = ordered.Select(x => new TaskRowModel { Task = x }).ToList();
        TaskList.ItemsSource = rows;
        EmptyState.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void FilterChanged(object sender, EventArgs e) { if (IsLoaded) ApplyFilter(); }
    private void Add_Click(object sender, RoutedEventArgs e) => new TaskEditWindow { Owner = this }.ShowDialog();
    private void QuickAdd_Click(object sender, RoutedEventArgs e) => AppInstance.ShowQuickAdd();

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not Guid id) return;
        var task = await AppInstance.Services.Tasks.GetAsync(id);
        if (task is not null) new TaskEditWindow(task) { Owner = this }.ShowDialog();
    }

    private async void Complete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is Guid id) await AppInstance.Services.CompleteTaskAsync(id);
    }

    private async void More_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not Guid id || sender is not Button button) return;
        var task = await AppInstance.Services.Tasks.GetAsync(id);
        if (task is null) return;
        var menu = new ContextMenu();
        if (task.State == TaskState.Active)
        {
            AddMenuItem(menu, "延后 1 小时", async () => await AppInstance.Services.PostponeTaskAsync(id, TimeSpan.FromHours(1)));
            AddMenuItem(menu, "延到明天 09:00", async () => await AppInstance.Services.PostponeTaskAsync(id, TimeSpan.FromDays(1)));
            AddMenuItem(menu, "延到下周 09:00", async () => await AppInstance.Services.PostponeTaskAsync(id, TimeSpan.FromDays(7)));
            AddMenuItem(menu, "归档", async () => await AppInstance.Services.ArchiveTaskAsync(id));
        }
        else
        {
            AddMenuItem(menu, "恢复为进行中", async () => await AppInstance.Services.ReopenTaskAsync(id));
        }
        AddMenuItem(menu, "永久删除", async () =>
        {
            if (MessageBox.Show(this, $"确定永久删除“{task.Title}”吗？", "删除任务", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                await AppInstance.Services.DeleteTaskAsync(id);
        });
        menu.PlacementTarget = button;
        menu.IsOpen = true;
    }

    private static void AddMenuItem(ContextMenu menu, string title, Func<Task> action)
    {
        var item = new MenuItem { Header = title };
        item.Click += async (_, _) => await action();
        menu.Items.Add(item);
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "DDLCard JSON 备份 (*.json)|*.json", FileName = $"ddlcard-backup-{DateTime.Now:yyyyMMdd-HHmm}.json" };
        if (dialog.ShowDialog(this) != true) return;
        await AppInstance.Services.Backup.ExportAsync(dialog.FileName);
        StatusText.Text = "备份已导出";
    }

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "DDLCard JSON 备份 (*.json)|*.json" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var preview = await AppInstance.Services.Backup.PreviewAsync(dialog.FileName);
            if (MessageBox.Show(this, $"备份包含 {preview.Tasks.Count} 个任务。导入会替换当前本地数据，是否继续？", "预览备份", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            await AppInstance.Services.Backup.RestoreAsync(preview);
            await AppInstance.Services.ReloadAfterRestoreAsync();
            StatusText.Text = "备份已恢复";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "无法导入", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e) => new SettingsWindow { Owner = this }.ShowDialog();

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (AppInstance.IsShuttingDown) return;
        e.Cancel = true;
        Hide();
        AppInstance.Tray.ShowInfo("DDLCard 仍在后台运行", "可从系统托盘重新打开任务管理器。");
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        source.AddHook(WndProc);
        RegisterHotKey(source.Handle, HotkeyId, ModControl | ModAlt, VirtualKeyD);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 0x0312 && wParam.ToInt32() == HotkeyId)
        {
            AppInstance.ShowQuickAdd();
            handled = true;
        }
        return IntPtr.Zero;
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
}
