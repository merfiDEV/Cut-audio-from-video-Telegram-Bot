using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using BotApp.Config;
using BotApp.Utils;

namespace BotApp.Handlers;

public static class StatsHandler
{
    public static async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        if (message.From is null || message.From.Id != AppConfig.AdminUserId)
        {
            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: "⛔️ Доступ запрещен.\n[ admin only ]",
                cancellationToken: ct);
            return;
        }

        var startTime = RuntimeStats.GetStartTime().ToString("yyyy-MM-dd HH:mm:ss");
        var formatCounts = RuntimeStats.GetFormatCounts();

        var text =
            "📊 Статистика бота\n\n" +
            $"⏱ Время работы: {FormatUptime()}\n" +
            $"✅ Всего конвертаций: {RuntimeStats.GetConversionCount()}\n\n" +
            "📦 По форматам:\n" +
            $"   MP3: {formatCounts.GetValueOrDefault("mp3", 0)}\n" +
            $"   WAV: {formatCounts.GetValueOrDefault("wav", 0)}\n" +
            $"   OGG: {formatCounts.GetValueOrDefault("ogg", 0)}\n" +
            $"   M4A: {formatCounts.GetValueOrDefault("m4a", 0)}\n\n" +
            $"🗓 Дата запуска: {startTime}\n" +
            "---\n" +
            $"⚠️ Ошибок: {RuntimeStats.GetErrorCount()}\n" +
            "---\n" +
            "[ admin only ]";

        await bot.SendMessage(
            chatId: message.Chat.Id,
            text: text,
            cancellationToken: ct);
    }

    private static string FormatUptime()
    {
        var uptime = RuntimeStats.GetUptime();
        var totalSeconds = (int)uptime.TotalSeconds;
        var hours = totalSeconds / 3600;
        var remainder = totalSeconds % 3600;
        var minutes = remainder / 60;
        var seconds = remainder % 60;

        return hours > 0
            ? $"{hours}ч {minutes}м {seconds}с"
            : $"{minutes}м {seconds}с";
    }
}
