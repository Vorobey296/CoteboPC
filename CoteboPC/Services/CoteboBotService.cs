using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using CoteboPC.Utils;
using CoteboPC;                   


namespace CoteboPC;

public static class CoteboBotService
{
    private static TelegramBotClient? _bot;
    private static AppConfig? _config;
    private static CancellationTokenSource? _cts;

    public static async void Start()
    {
        _config = await ConfigService.LoadConfig();
        if (_config?.Token == null) return;

        _bot = new TelegramBotClient(_config.Token);
        _cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        _bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, _cts.Token);

        await _bot.SendMessage(_config.ChatId, "✅ CoteboPC успешно запущен и работает в фоне.");
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message || message.Text is not { } text || _config == null)
            return;

        if (message.Chat.Id != _config.ChatId) return;

        string response = "Команда получена";

        try
        {
            string lower = text.ToLower().Trim();

            if (lower == "/ping")
                response = "✅ CoteboPC работает";
            else if (lower.StartsWith("/screenshot"))
                response = await Commands.TakeScreenshot(botClient, message.Chat.Id);
            else if (lower == "/sysinfo")
                response = await Commands.GetSystemInfo();
            else if (lower == "/shutdown")
                response = await Commands.Shutdown();
            else if (lower == "/reboot")
                response = await Commands.Reboot();
            else if (lower.StartsWith("/exec "))
                response = await Commands.ExecuteConsole(text.Substring(6));
            else if (lower == "/minimize")
                response = Commands.MinimizeAll();
            else
                response = "Неизвестная команда. Доступны: /ping, /screenshot, /sysinfo, /shutdown, /reboot, /exec, /minimize и др.";
        }
        catch (Exception ex)
        {
            response = $"❌ Ошибка: {ex.Message}";
        }

        await botClient.SendMessage(message.Chat.Id, response);
    }

    private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Bot error: {exception}");
        return Task.CompletedTask;
    }
}