using Telegram.Bot.Types.ReplyMarkups;

namespace BotApp.Keyboards;

public static class GithubKeyboard
{
    public const string GithubRepoUrl = "https://github.com/merfiDEV/Cut-audio-from-video-Telegram-Bot";

    public static InlineKeyboardMarkup GetGithubKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithUrl("🌐 GitHub", GithubRepoUrl)
            }
        });
    }
}
