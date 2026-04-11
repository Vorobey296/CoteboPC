using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using GregsStack.InputSimulatorStandard;

namespace CoteboPC.Utils;

public static class Commands
{
    /// <summary>
    /// 3. Отправка скриншота текущего экрана
    /// </summary>
    public static async Task<string> TakeScreenshot(ITelegramBotClient bot, long chatId)
    {
        try
        {
            Bitmap bmp;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Захват всего экрана на Windows
                var screenBounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                bmp = new Bitmap(screenBounds.Width, screenBounds.Height);
                using var graphics = Graphics.FromImage(bmp);
                graphics.CopyFromScreen(0, 0, 0, 0, bmp.Size);
            }
            else
            {
                // Linux — требует установленного scrot (sudo apt install scrot)
                var tempFile = Path.Combine(Path.GetTempPath(), $"cotebo_screenshot_{DateTime.Now.Ticks}.png");
                
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "scrot",
                    Arguments = tempFile,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                });

                if (process != null)
                    await process.WaitForExitAsync();

                if (File.Exists(tempFile))
                {
                    bmp = new Bitmap(tempFile);
                    File.Delete(tempFile);
                }
                else
                {
                    return "❌ Не удалось сделать скриншот. Установи scrot: sudo apt install scrot";
                }
            }

            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            ms.Position = 0;

            await bot.SendPhoto(chatId, new InputFileStream(ms, "screenshot.png"));
            return "✅ Скриншот текущего экрана отправлен";
        }
        catch (Exception ex)
        {
            return $"❌ Ошибка при создании скриншота: {ex.Message}";
        }
    }

    /// <summary>
    /// 5. Вывод информации о системе
    /// </summary>
    public static async Task<string> GetSystemInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🖥 Операционная система: {RuntimeInformation.OSDescription}");
        sb.AppendLine($"💾 Используемая память (GC): {GC.GetTotalMemory(false) / (1024 * 1024)} МБ");
        sb.AppendLine($"⏱ Uptime: {Environment.TickCount64 / 3600000} часов");
        sb.AppendLine($"🌐 .NET версия: {RuntimeInformation.FrameworkDescription}");
        return sb.ToString();
    }

    /// <summary>
    /// 11. Выключение ПК
    /// </summary>
    public static async Task<string> Shutdown()
    {
        try
        {
            RunPowerCommand("shutdown");
            return "✅ Команда выключения отправлена";
        }
        catch { return "❌ Не удалось выполнить выключение"; }
    }

    /// <summary>
    /// 11. Перезагрузка ПК
    /// </summary>
    public static async Task<string> Reboot()
    {
        try
        {
            RunPowerCommand("reboot");
            return "✅ Команда перезагрузки отправлена";
        }
        catch { return "❌ Не удалось выполнить перезагрузку"; }
    }

    private static void RunPowerCommand(string action)
    {
        var psi = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new ProcessStartInfo("shutdown", action == "reboot" ? "/r /t 0" : "/s /t 0")
            : new ProcessStartInfo("bash", $"-c \"{action}\"");
        
        psi.UseShellExecute = false;
        Process.Start(psi);
    }

    /// <summary>
    /// 12. Выполнение произвольной консольной команды
    /// Пример: /exec ipconfig
    /// </summary>
    public static async Task<string> ExecuteConsole(string cmd)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash",
                Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"/c {cmd}" : $"-c \"{cmd}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            string output = await process!.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();

            return string.IsNullOrWhiteSpace(error) 
                ? output.Trim() + "\n✅ Команда выполнена" 
                : $"Вывод:\n{output}\nОшибка:\n{error}";
        }
        catch (Exception ex)
        {
            return $"❌ Ошибка выполнения команды: {ex.Message}";
        }
    }

    /// <summary>
    /// 15. Скрытие всех окон (минимизация)
    /// </summary>
    public static string MinimizeAll()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("powershell", "(New-Object -ComObject Shell.Application).MinimizeAll()");
            }
            else
            {
                Process.Start("xdotool", "key super+d");
            }
            return "✅ Все окна свёрнуты";
        }
        catch
        {
            return "❌ Не удалось свернуть окна";
        }
    }

    /// <summary>
    /// 6. Пример перемещения мыши (можно расширить)
    /// Пример команды: /mouse 800 600
    /// </summary>
    public static string MouseMove(int x, int y)
    {
        try
        {
            var simulator = new InputSimulator();
            simulator.Mouse.MoveMouseTo(x * 65535 / 1920, y * 65535 / 1080); // под FullHD
            return $"✅ Мышь перемещена в точку ({x}, {y})";
        }
        catch
        {
            return "❌ Не удалось переместить мышь (только Windows)";
        }
    }

    /// <summary>
    /// Заглушка для остальных команд
    /// </summary>
    public static string Default(string command)
    {
        return $"✅ Команда '{command}' получена.\n\n" +
               "Реализованные команды:\n" +
               "/ping — проверка\n" +
               "/screenshot — скриншот\n" +
               "/sysinfo — информация о системе\n" +
               "/shutdown — выключить ПК\n" +
               "/reboot — перезагрузить ПК\n" +
               "/exec [команда] — выполнить команду\n" +
               "/minimize — свернуть все окна";
    }
}