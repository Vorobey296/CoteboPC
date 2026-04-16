// App.axaml.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Controls.Primitives;
using CoteboPC.Views;
using Avalonia.Controls;
using WinForms = System.Windows.Forms;
using System.Drawing;

namespace CoteboPC;

public partial class App : Application
{
    private WinForms.NotifyIcon? _notifyIcon;
    private WinForms.ApplicationContext? _trayContext;
    private SynchronizationContext? _traySyncContext;
    private Thread? _trayThread;
    private ManualResetEventSlim _trayReady = new(false);

    public App()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Logger.Log("[App] OnFrameworkInitializationCompleted started.");
                
                // Главное окно всегда скрытое
                desktop.MainWindow = CreateHiddenWindow();
                Logger.Log("[App] Hidden main window created.");

                // Проверяем конфиг и показываем нужное окно
                var configPath = ConfigService.GetConfigPath();
                Logger.Log($"[App] Config path: {configPath}");
                Logger.Log($"[App] Config exists: {System.IO.File.Exists(configPath)}");
                Logger.Log("[App] Loading config...");
                var config = ConfigService.LoadConfig().GetAwaiter().GetResult();
                Logger.Log($"[App] Config loaded result: {(config == null ? "null" : "present")}");

                if (config == null || string.IsNullOrEmpty(config.Token))
                {
                    Logger.Log("[App] No config found, showing FirstSetupWindow.");
                    // Первый запуск — показываем окно настройки
                    var setupWindow = new FirstSetupWindow();
                    setupWindow.Show();
                }
                else
                {
                    Logger.Log("[App] Config found, starting bot...");
                    SetupTrayIcon();
                    var botTask = CoteboBotService.StartAsync(config);
                    botTask.ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            Logger.Log($"[App] Bot startup failed: {t.Exception?.Flatten().InnerException}");
                            ShowTrayMessage("CoteboPC", "Не удалось запустить бота. См. лог.");
                        }
                    }, TaskScheduler.Default);
                }

                desktop.Exit += OnExit;
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"[App] Exception in OnFrameworkInitializationCompleted: {ex}");
            ShowTrayMessage("CoteboPC", "Ошибка запуска. См. лог.");
        }
        finally
        {
            base.OnFrameworkInitializationCompleted();
        }
    }

    public void EnsureTrayIcon()
    {
        if (_notifyIcon != null)
            return;

        SetupTrayIcon();
    }

    private void SetupTrayIcon()
    {
        _trayThread = new Thread(() =>
        {
            _trayContext = new WinForms.ApplicationContext();
            WinForms.Application.EnableVisualStyles();
            WinForms.Application.SetCompatibleTextRenderingDefault(false);
            SynchronizationContext.SetSynchronizationContext(new WinForms.WindowsFormsSynchronizationContext());
            _traySyncContext = SynchronizationContext.Current;

            _notifyIcon = new WinForms.NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "CoteboPC",
                Visible = true
            };

            var menu = new WinForms.ContextMenuStrip();
            menu.Items.Add("Выход", null, (_, _) => ExitApplication());
            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick += (_, _) => ShowTrayMessage();

            _trayReady.Set();
            WinForms.Application.Run(_trayContext);

            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        })
        {
            IsBackground = true
        };

        _trayThread.SetApartmentState(ApartmentState.STA);
        _trayThread.Start();
        _trayReady.Wait(2000);
    }

    private void ShowTrayMessage(string title = "CoteboPC", string text = "Приложение работает в фоне.")
    {
        if (_traySyncContext != null && _notifyIcon != null)
        {
            _traySyncContext.Post(_ => _notifyIcon.ShowBalloonTip(3000, title, text, WinForms.ToolTipIcon.Info), null);
        }
    }

    private void ExitApplication()
    {
        CoteboBotService.Stop();
        _trayContext?.ExitThread();
        _notifyIcon = null;
        _trayContext = null;
        (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _notifyIcon?.Visible = false;
        _notifyIcon?.Dispose();
        _notifyIcon = null;
        CoteboBotService.Stop();
    }

private Window CreateHiddenWindow()
{
    var hiddenWindow = new Window
    {
        Width = 0,
        Height = 0,
        WindowState = WindowState.Minimized,
        ShowInTaskbar = false,
        ShowActivated = false,
        IsVisible = false,
        CanResize = false,
        Topmost = false,
        Opacity = 0,
        Content = null,
        WindowDecorations = WindowDecorations.None,
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent }
    };

    return hiddenWindow;
}
}