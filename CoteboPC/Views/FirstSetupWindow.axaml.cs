using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia;           // Добавлено
using MsBox.Avalonia.Enums;     // Добавлено
using System.Diagnostics;
using System.Threading.Tasks;

namespace CoteboPC.Views;

public partial class FirstSetupWindow : Window
{
    public FirstSetupWindow()
    {
        InitializeComponent();
    }

    private async void SaveAndNext_Click(object sender, RoutedEventArgs e)
    {
        var token = TokenBox.Text?.Trim();
        var userIdText = UserIdBox.Text?.Trim();

        if (string.IsNullOrWhiteSpace(token) || !long.TryParse(userIdText, out long userId))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Пожалуйста, введите корректный Bot Token и User ID", ButtonEnum.Ok);
            await box.ShowAsync();
            return;
        }

        await ConfigService.SaveConfig(token, userId);

        var successBox = MessageBoxManager.GetMessageBoxStandard("Успешно", "Настройки сохранены!", ButtonEnum.Ok);
        await successBox.ShowAsync();

        var secondWindow = new SecondSetupWindow();
        secondWindow.Show();
        this.Close();
    }

    private void BotTokenHelp_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://t.me/botfather") { UseShellExecute = true });
    }

    private void UserIdHelp_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://t.me/userinfobot") { UseShellExecute = true });
    }
}