using System.Windows;
using System.Windows.Input;
using DDLCard.Core.Models;

namespace DDLCard.Windows;

public partial class QuickAddWindow : Window
{
    public QuickAddWindow()
    {
        InitializeComponent();
        DueDatePicker.SelectedDate = DateTime.Today.AddDays(1);
        DueTimeBox.Text = "23:59";
        Loaded += (_, _) => TitleBox.Focus();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DueDatePicker.SelectedDate is not DateTime date || !TimeSpan.TryParse(DueTimeBox.Text.Trim(), out var time)) throw new FormatException("请输入有效的截止日期和时间。");
            var local = DateTime.SpecifyKind(date.Date + time, DateTimeKind.Unspecified);
            var task = new DeadlineTask
            {
                Title = TitleBox.Text,
                DueAtUtc = new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local)).ToUniversalTime(),
                TimeZoneId = TimeZoneInfo.Local.Id,
                Priority = (TaskPriority)Math.Max(0, PriorityBox.SelectedIndex)
            };
            await ((App)System.Windows.Application.Current).Services.SaveTaskAsync(task);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "无法添加", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void TitleBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key == Key.Enter) Save_Click(sender, e); }
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
