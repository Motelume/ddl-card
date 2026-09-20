using System.Windows;
using System.Windows.Controls;
using DDLCard.Core.Contracts;
using DDLCard.Core.Services;
using DDLCard.Windows.Services;

namespace DDLCard.Windows;

public partial class SettingsWindow : Window
{
    private App AppInstance => (App)System.Windows.Application.Current;

    public SettingsWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = AppInstance.Services.Settings;
        ThemeBox.SelectedIndex = settings.ThemeId switch { "paper" => 1, "forest" => 2, _ => 0 };
        OpacitySlider.Value = settings.CardOpacity;
        CornerSlider.Value = settings.CornerRadius;
        FontSlider.Value = settings.FontScale;
        TopmostBox.IsChecked = settings.AlwaysOnTop;
        EdgeCollapseBox.IsChecked = settings.EdgeCollapseEnabled;
        StartupBox.IsChecked = settings.StartWithWindows;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ApplyControlsToSettings();
            await AppInstance.Services.SaveSettingsAsync();
            StartupService.SetEnabled(AppInstance.Services.Settings.StartWithWindows);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "无法保存设置", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyControlsToSettings()
    {
        var settings = AppInstance.Services.Settings;
        settings.ThemeId = ((ComboBoxItem)ThemeBox.SelectedItem).Tag?.ToString() ?? "midnight";
        settings.CardOpacity = OpacitySlider.Value;
        settings.CornerRadius = CornerSlider.Value;
        settings.FontScale = FontSlider.Value;
        settings.AlwaysOnTop = TopmostBox.IsChecked == true;
        settings.EdgeCollapseEnabled = EdgeCollapseBox.IsChecked == true;
        settings.StartWithWindows = StartupBox.IsChecked == true;
    }

    private void CopyPreset_Click(object sender, RoutedEventArgs e)
    {
        ApplyControlsToSettings();
        PresetCodeBox.Text = PresetCodec.Encode(PresetDocument.FromSettings(AppInstance.Services.Settings));
        Clipboard.SetText(PresetCodeBox.Text);
    }

    private void PastePreset_Click(object sender, RoutedEventArgs e)
    {
        if (Clipboard.ContainsText()) PresetCodeBox.Text = Clipboard.GetText().Trim();
    }

    private async void ApplyPreset_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var preset = PresetCodec.Decode(PresetCodeBox.Text);
            var summary = $"主题：{preset.ThemeId}\n透明度：{preset.CardOpacity:P0}\n圆角：{preset.CornerRadius:0}\n字体缩放：{preset.FontScale:0.00}\n置顶：{(preset.AlwaysOnTop ? "是" : "否")}\n侧边收起：{(preset.EdgeCollapseEnabled ? "是" : "否")}";
            if (MessageBox.Show(this, summary + "\n\n确认应用这份预设？", "预览预设", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            preset.ApplyTo(AppInstance.Services.Settings);
            await AppInstance.Services.SaveSettingsAsync();
            LoadSettings();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "预设码无效", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
