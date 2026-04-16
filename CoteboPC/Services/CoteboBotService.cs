using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System;
using System.Threading;
using System.Threading.Tasks;
using CoteboPC.Utils;

namespace CoteboPC;

public static class CoteboBotService
{
    private static TelegramBotClient? _bot;
    private static AppConfig? _config;
    private static CancellationTokenSource? _cts;

    public static async Task StartAsync(AppConfig config)
    {
        var tokenPreview = string.IsNullOrEmpty(config.Token)
            ? "<empty>"
            : config.Token[..Math.Min(10, config.Token.Length)];

        Logger.Log($"[Bot] StartAsync entered. Token: {tokenPreview}... ChatId: {config.ChatId}");
        Logger.Log($"[Bot] _bot is currently: {(_bot == null ? "NULL (новый запуск)" : "инициализирован (пропускаем)")}" );
        
        // Если бот уже запущен, не запускаем заново
        if (_bot != null)
        {
            Logger.Log("[Bot] Bot already running, skipping initialization.");
            return;
        }

        _config = config;

        if (string.IsNullOrEmpty(config.Token))
        {
            Logger.Log("[Bot] Error: token is null or empty.");
            throw new ArgumentException("Telegram bot token is required.", nameof(config.Token));
        }

        try
        {
            _bot = new TelegramBotClient(config.Token);
            Logger.Log("[Bot] TelegramBotClient created.");
            
            var me = await _bot.GetMe();
            Logger.Log($"[Bot] Authorized as @{me.Username}");
            
            _cts = new CancellationTokenSource();
            Logger.Log("[Bot] Starting receiving...");
            _bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, new ReceiverOptions(), _cts.Token);
            Logger.Log("[Bot] StartReceiving called.");
            
            await _bot.SendMessage(config.ChatId, "✅ CoteboPC успешно запущен и работает в фоне.");
            Logger.Log("[Bot] Startup message sent.");
        }
        catch (Exception ex)
        {
            _bot = null; // Очищаем при ошибке
            Logger.Log($"[Bot] Exception in StartAsync: {ex}");
            throw;
        }
    }

    public static void Stop()
    {
        try
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"[Bot] Exception in Stop: {ex}");
        }
        finally
        {
            _bot = null;
            _config = null;
        }
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        Logger.Log($"[Bot] Update received: {update.Type}");

        if (update.Message is not { } message || message.Text is not { } text || message.Chat is null || _config == null)
        {
            Logger.Log("[Bot] Update ignored (no message/text/chat or config null).");
            return;
        }

        var chatId = message.Chat.Id;
        Logger.Log($"[Bot] Message from {chatId}: {text}");
        if (chatId != _config.ChatId)
        {
            Logger.Log($"[Bot] Unauthorized chat id: {message.Chat.Id}");
            return;
        }

        string response = "Команда получена";

        try
        {
            string lower = text.ToLower().Trim();

            if (lower == "/start" || lower == "/help")
            {
                response = "Выберите команду ниже";
                await botClient.SendMessage(chatId, response, replyMarkup: Commands.GetCommandsKeyboard());
                Logger.Log($"[Bot] Help sent with keyboard");
                return;
            }
            else if (lower == "/ping" || lower == "🔍 ping")
                response = "✅ CoteboPC работает";
            else if (lower.StartsWith("/screenshot") || lower == "📸 screenshot")
                response = await Commands.TakeScreenshot(botClient, message.Chat.Id);
            else if (lower == "/sysinfo" || lower == "🖥 sysinfo")
                response = await Commands.GetSystemInfo();
            else if (lower == "/ip" || lower == "🌐 ip-address")
                response = await Commands.GetIpAddresses();
            else if (lower == "/processes" || lower == "🧠 processes")
                response = await Commands.GetProcessesList();
            else if (lower.StartsWith("/open "))
                response = await Commands.OpenPath(text.Substring(6).Trim());
            else if (lower == "/shutdown" || lower == "⚡ shutdown")
                response = await Commands.Shutdown();
            else if (lower == "/reboot" || lower == "🔄 reboot")
                response = await Commands.Reboot();
            else if (lower.StartsWith("/exec "))
                response = await Commands.ExecuteConsole(text.Substring(6));
            else if (lower == "/minimize" || lower == "📋 minimize")
                response = Commands.MinimizeAll();
            else
                response = Commands.Default(text);
        }
        catch (Exception ex)
        {
            response = $"❌ Ошибка: {ex.Message}";
            Logger.Log($"[Bot] Command error: {ex}");
        }

        await botClient.SendMessage(chatId, response);
        Logger.Log($"[Bot] Response sent: {response}");
    }

    private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Logger.Log($"[Bot] Polling error: {exception}");
        return Task.CompletedTask;
    }
}