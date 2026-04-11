using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CoteboPC.Utils;

public static class PlatformHelpers
{
    public static async Task SetupWindowsAutostart()
    {
        var exe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        key?.SetValue("CoteboPC", exe);
        await Task.CompletedTask;
    }

    public static async Task SetupLinuxAutostart()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var servicePath = Path.Combine(home, ".config", "systemd", "user", "cotebopc.service");
        var exe = Process.GetCurrentProcess().MainModule?.FileName ?? "";

        var content = $@"[Unit]
Description=CoteboPC Telegram Remote Control
After=network.target

[Service]
Type=simple
ExecStart={exe}
Restart=always
RestartSec=10

[Install]
WantedBy=default.target";

        Directory.CreateDirectory(Path.GetDirectoryName(servicePath)!);
        await File.WriteAllTextAsync(servicePath, content);

        await RunCommand("systemctl --user daemon-reload");
        await RunCommand("systemctl --user enable cotebopc.service");
        await RunCommand("systemctl --user start cotebopc.service");
    }

    private static async Task RunCommand(string cmd)
    {
        var psi = new ProcessStartInfo("bash", $"-c \"{cmd}\"") { RedirectStandardOutput = true, UseShellExecute = false };
        using var p = Process.Start(psi);
        await p!.WaitForExitAsync();
    }
}