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
                InlineKeyboardButton.WithCallbackData(
                    AppConfig.AvailableFormats["mp3"].Label,
                    "format_mp3"),
                InlineKeyboardButton.WithCallbackData(
                    AppConfig.AvailableFormats["wav"].Label,
                    "format_wav")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    AppConfig.AvailableFormats["ogg"].Label,
                    "format_ogg"),
                InlineKeyboardButton.WithCallbackData(
                    AppConfig.AvailableFormats["m4a"].Label,
                    "format_m4a")
            }
        });
    }
}
