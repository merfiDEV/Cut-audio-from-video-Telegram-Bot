using Telegram.Bot.Types.ReplyMarkups;
using BotApp.Config;

namespace BotApp.Keyboards;

public static class FormatKeyboard
{
    public static InlineKeyboardMarkup GetFormatKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🎵 MP3",  "format_mp3"),
                InlineKeyboardButton.WithCallbackData("🔊 WAV",  "format_wav"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🎙 OGG",  "format_ogg"),
                InlineKeyboardButton.WithCallbackData("🍎 M4A",  "format_m4a"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("💎 FLAC (без потерь)", "format_flac"),
            }
        });
    }
}
