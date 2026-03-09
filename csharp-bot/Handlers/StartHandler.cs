using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using BotApp.Keyboards;
using BotApp.Utils;

namespace BotApp.Handlers;

public static class StartHandler
{
    public static async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        SimpleLogger.Info($"Пользователь {message.From?.Id} использовал команду /start");

        await bot.SendMessage(
            chatId: message.Chat.Id,
            text: "👋 Привет! Я open source бот для извлечения аудио из видео.\n\n" +
                  "📹 Отправь мне видео или видео-сообщение (кружок),\n" +
                  "и я предложу выбрать формат аудио для конвертации.\n\n" +
                  "🎧 Поддерживаемые форматы: MP3, WAV, OGG, M4A",
            replyMarkup: GithubKeyboard.GetGithubKeyboard(),
            cancellationToken: ct);
    }
}
