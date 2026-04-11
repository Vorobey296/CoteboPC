using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using MsBox.Avalonia;           // Добавлено
using MsBox.Avalonia.Enums;     // Добавлено
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CoteboPC.Utils;

namespace CoteboPC.Views;

public partial class SecondSetupWindow : Window
{
    public SecondSetupWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        var tb = new TextBlock
        {
            FontSize = 15,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Colors.LightGray)
        };

        if (isWindows)
        {
            tb.Text = "Windows\n\nПриложение будет добавлено в автозагрузку через реестр.\nПосле нажатия кнопки оно будет запускаться автоматически после перезагрузки компьютера.";
            ActionButton.Content = "Добавить в автозагрузку Windows";
        }
        else
        {
            tb.Text = "Linux\n\nБудет создан пользовательский systemd-сервис.\nПриложение будет запускаться автоматически после входа в систему.";
            ActionButton.Content = "Настроить автозагрузку (systemd)";
        }

        ContentPanel.Children.Add(tb);
    }

    private async void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        try
        {
            if (isWindows)
                await PlatformHelpers.SetupWindowsAutostart();
            else
                await PlatformHelpers.SetupLinuxAutostart();

            var box = MessageBoxManager.GetMessageBoxStandard("Готово!", "Автозагрузка настроена.\nCoteboPC теперь работает в фоне.", ButtonEnum.Ok);
            await box.ShowAsync();

            CoteboBotService.Start();
            this.Close();
        }
        catch (Exception ex)
        {
            var errorBox = MessageBoxManager.GetMessageBoxStandard("Ошибка", $"Не удалось настроить автозагрузку:\n{ex.Message}", ButtonEnum.Ok);
            await errorBox.ShowAsync();
        }
    }
}