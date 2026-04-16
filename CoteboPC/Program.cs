using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace CoteboPC;

internal class Program
{
    private static Mutex? _singleInstanceMutex;

    // P/Invoke для скрытия консольного окна
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0;

    [STAThread]
    public static void Main(string[] args)
    {
        const string mutexName = @"Global\CoteboPC_Avalonia_SingleInstance";
        bool createdNew;
        _singleInstanceMutex = new Mutex(true, mutexName, out createdNew);
        if (!createdNew)
        {
            Logger.Log("[App] Another CoteboPC instance is already running. Exiting duplicate instance.");
            MessageBox.Show("CoteboPC уже запущен и работает в фоне.", "CoteboPC", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Скрываем консольное окно при старте
        IntPtr consoleWindow = GetConsoleWindow();
        if (consoleWindow != IntPtr.Zero)
        {
            ShowWindow(consoleWindow, SW_HIDE);
        }

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
        }
        catch (Exception ex)
        {
            Logger.Log($"[App] Критическая ошибка при запуске: {ex}");
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}