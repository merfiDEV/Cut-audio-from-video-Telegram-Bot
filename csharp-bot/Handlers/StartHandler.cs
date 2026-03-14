using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using BotApp.Keyboards;
using BotApp.Utils;

namespace BotApp.Handlers;

public static class StartHandler
{
    private const string WelcomeText =
        "🎬 <b>Video to Audio Bot</b>\n" +
        "━━━━━━━━━━━━━━━━━━━━━\n\n" +
        "Отправь мне видео — я мгновенно извлеку аудио!\n\n" +
        "📋 <b>Поддерживаемые форматы:</b>\n" +
        "  🎵 <b>MP3</b> — 128 / 192 / 320 kbps\n" +
        "  🔊 <b>WAV</b> — без потерь, максимум качества\n" +
        "  🎙 <b>OGG</b> — компактный, идеален для Telegram\n" +
        "  🍎 <b>M4A</b> — для Apple устройств\n" +
        "  💎 <b>FLAC</b> — lossless, студийное качество\n\n" +
        "📦 <b>Что принимает бот:</b>\n" +
        "  📹 Обычное видео\n" +
        "  🔘 Видео-кружок\n" +
        "  📁 Видео-документ\n\n" +
        "⚡ <b>Лимит файла:</b> до 50 MB\n\n" +
        "━━━━━━━━━━━━━━━━━━━━━\n" +
        "<i>Просто отправь видео и выбери формат!</i>";

    public static async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        SimpleLogger.Info($"User {message.From?.Id} used /start");

        await bot.SendMessage(
            chatId: message.Chat.Id,
            text: WelcomeText,
            parseMode: ParseMode.Html,
            replyMarkup: GithubKeyboard.GetGithubKeyboard(),
            cancellationToken: ct);
    }
}
