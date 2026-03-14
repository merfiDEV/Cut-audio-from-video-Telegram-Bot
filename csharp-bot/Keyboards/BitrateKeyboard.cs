using Telegram.Bot.Types.ReplyMarkups;
using BotApp.Config;

namespace BotApp.Keyboards;

public static class BitrateKeyboard
{
    public static InlineKeyboardMarkup GetMp3BitrateKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("💾 128 kbps — эконом",  "bitrate_128"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⭐ 192 kbps — стандарт", "bitrate_192"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🚀 320 kbps — максимум", "bitrate_320"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("◀️ Назад к форматам", "back_to_formats"),
            }
        });
    }
}
