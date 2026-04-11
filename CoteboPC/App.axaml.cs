// App.axaml.cs
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CoteboPC.Views;

namespace CoteboPC;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (!ConfigService.ConfigExists())
            {
                // Первый запуск — окно настройки
                desktop.MainWindow = new FirstSetupWindow();
            }
            else
            {
                // Обычный запуск — можно создать главное окно или работать без него
                // Если бот работает в фоне, можно создать скрытое окно или null
                desktop.MainWindow = new MainWindow(); // или любое другое окно
                CoteboBotService.Start();
            }
        }
        
        base.OnFrameworkInitializationCompleted();
    }
}