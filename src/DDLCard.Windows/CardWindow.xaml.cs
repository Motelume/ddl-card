using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DDLCard.Core.Services;
using DDLCard.Windows.Models;

namespace DDLCard.Windows;

public partial class CardWindow : Window
{
    private enum DockSide { None, Left, Right }

    private readonly DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromSeconds(20) };
    private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(700) };
    private readonly DispatcherTimer _collapseTimer = new() { Interval = TimeSpan.FromMilliseconds(650) };
    private DockSide _dockSide;
    private bool _collapsed;
    private bool _pinnedOpen;
    private double _expandedLeft;
    private App AppInstance => (App)System.Windows.Application.Current;

    public CardWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SizeChanged += (_, _) => { ScheduleGeometrySave(); _ = RefreshAsync(); };
        LocationChanged += (_, _) => { DetectDockSide(); ScheduleGeometrySave(); };
        MouseEnter += (_, _) => ExpandFromEdge();
        MouseLeave += (_, _) => { if (CanAutoCollapse()) _collapseTimer.Start(); };
        _refreshTimer.Tick += async (_, _) => await RefreshAsync();
        _saveTimer.Tick += async (_, _) => { _saveTimer.Stop(); await SaveGeometryAsync(); };
        _collapseTimer.Tick += (_, _) => { _collapseTimer.Stop(); if (CanAutoCollapse()) CollapseToEdge(); };
        AppInstance.Services.TasksChanged += async (_, _) => await Dispatcher.InvokeAsync(RefreshAsync);
        AppInstance.Services.SettingsChanged += async (_, _) => await Dispatcher.InvokeAsync(async () => { ApplySettings(); await RefreshAsync(); });
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplySettings();
        await RefreshAsync();
        _refreshTimer.Start();
    }

    private void ApplySettings()
    {
        var settings = AppInstance.Services.Settings;
        Width = Math.Max(MinWidth, settings.CardWidth);
        Height = Math.Max(MinHeight, settings.CardHeight);
        Left = EnsureVisible(settings.CardLeft, SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenWidth, Width);
        Top = EnsureVisible(settings.CardTop, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenHeight, Height);
        Topmost = settings.AlwaysOnTop;
        Opacity = settings.CardOpacity;
        CardRoot.CornerRadius = new CornerRadius(settings.CornerRadius);
        CardRoot.Background = new SolidColorBrush(ThemeBackground(settings.ThemeId));
        LayoutTransform = new ScaleTransform(settings.FontScale, settings.FontScale);
        PinButton.Content = Topmost ? "取消置顶" : "置顶";
    }

    private async Task RefreshAsync()
    {
        if (!IsLoaded) return;
        var ordered = TaskOrdering.ForCard(await AppInstance.Services.Tasks.GetAllAsync());
        var visibleCount = Math.Max(1, (int)Math.Floor((ActualHeight - 125) / 91));
        var rows = ordered.Take(visibleCount).Select(x => new TaskRowModel { Task = x }).ToList();
        TaskItems.ItemsSource = rows;
        EmptyState.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        SummaryText.Text = ordered.Count == 0 ? "今天可以轻松一点" : $"{ordered.Count} 个进行中 · 显示最近 {rows.Count} 个";
    }

    private void DragHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || e.OriginalSource is Button) return;
        _pinnedOpen = true;
        try { DragMove(); } catch (InvalidOperationException) { }
        _pinnedOpen = false;
        DetectDockSide();
    }

    private async void Complete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is Guid id) await AppInstance.Services.CompleteTaskAsync(id);
    }

    private void Postpone_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not Guid id || sender is not Button button) return;
        var menu = new System.Windows.Controls.ContextMenu();
        AddPostponeItem(menu, "延后 1 小时", id, TimeSpan.FromHours(1));
        AddPostponeItem(menu, "延到明天 09:00", id, TimeSpan.FromDays(1));
        AddPostponeItem(menu, "延到下周 09:00", id, TimeSpan.FromDays(7));
        menu.PlacementTarget = button;
        menu.IsOpen = true;
    }

    private void AddPostponeItem(System.Windows.Controls.ContextMenu menu, string title, Guid id, TimeSpan delay)
    {
        var item = new System.Windows.Controls.MenuItem { Header = title };
        item.Click += async (_, _) => await AppInstance.Services.PostponeTaskAsync(id, delay);
        menu.Items.Add(item);
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        Topmost = !Topmost;
        AppInstance.Services.Settings.AlwaysOnTop = Topmost;
        PinButton.Content = Topmost ? "取消置顶" : "置顶";
        _ = AppInstance.Services.SaveSettingsAsync();
    }

    private void ManageButton_Click(object sender, RoutedEventArgs e) => AppInstance.ShowManager();

    private void DetectDockSide()
    {
        if (_collapsed) return;
        var area = SystemParameters.WorkArea;
        _dockSide = Left <= area.Left + 20 ? DockSide.Left : Left + Width >= area.Right - 20 ? DockSide.Right : DockSide.None;
    }

    private bool CanAutoCollapse() => AppInstance.Services.Settings.EdgeCollapseEnabled && _dockSide != DockSide.None && !_collapsed && !_pinnedOpen && !IsMouseOver;

    private void CollapseToEdge()
    {
        _expandedLeft = Left;
        var target = _dockSide == DockSide.Left ? SystemParameters.WorkArea.Left - Width + 22 : SystemParameters.WorkArea.Right - 22;
        BeginAnimation(LeftProperty, new DoubleAnimation(target, TimeSpan.FromMilliseconds(180)) { EasingFunction = new QuadraticEase() });
        _collapsed = true;
    }

    private void ExpandFromEdge()
    {
        _collapseTimer.Stop();
        if (!_collapsed) return;
        BeginAnimation(LeftProperty, new DoubleAnimation(_expandedLeft, TimeSpan.FromMilliseconds(180)) { EasingFunction = new QuadraticEase() });
        _collapsed = false;
    }

    private void ScheduleGeometrySave()
    {
        if (!IsLoaded || _collapsed) return;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private async Task SaveGeometryAsync()
    {
        var settings = AppInstance.Services.Settings;
        settings.CardWidth = Width;
        settings.CardHeight = Height;
        settings.CardLeft = Left;
        settings.CardTop = Top;
        await AppInstance.Services.SaveSettingsAsync();
    }

    private static double EnsureVisible(double value, double start, double span, double size) =>
        double.IsFinite(value) && value >= start - size + 80 && value <= start + span - 80 ? value : start + 80;

    private static System.Windows.Media.Color ThemeBackground(string themeId) => themeId switch
    {
        "paper" => System.Windows.Media.Color.FromArgb(245, 48, 49, 57),
        "forest" => System.Windows.Media.Color.FromArgb(242, 20, 48, 43),
        _ => System.Windows.Media.Color.FromArgb(242, 21, 28, 49)
    };
}
