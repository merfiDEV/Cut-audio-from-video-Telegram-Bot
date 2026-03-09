using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace BotApp.Handlers;

public sealed class UpdateDispatcher
{
    public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        switch (update.Type)
        {
            case UpdateType.Message:
                if (update.Message is null)
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(update.Message.Text))
                {
                    var text = update.Message.Text.Trim();
                    if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
                    {
                        await StartHandler.HandleAsync(bot, update.Message, ct);
                        return;
                    }

                    if (text.StartsWith("/stats", StringComparison.OrdinalIgnoreCase))
                    {
                        await StatsHandler.HandleAsync(bot, update.Message, ct);
                        return;
                    }
                }

                if (update.Message.Video is not null ||
                    update.Message.VideoNote is not null ||
                    update.Message.Document is not null)
                {
                    await VideoHandler.HandleVideoAsync(bot, update.Message, ct);
                }

                break;

            case UpdateType.CallbackQuery:
                if (update.CallbackQuery is not null &&
                    !string.IsNullOrWhiteSpace(update.CallbackQuery.Data) &&
                    update.CallbackQuery.Data.StartsWith("format_", StringComparison.OrdinalIgnoreCase))
                {
                    await VideoHandler.HandleFormatSelectionAsync(bot, update.CallbackQuery, ct);
                }

                break;
        }
    }
}
