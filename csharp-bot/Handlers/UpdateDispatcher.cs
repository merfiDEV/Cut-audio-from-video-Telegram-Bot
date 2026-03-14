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
                if (update.Message is null) return;

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

                    if (text.StartsWith("/help", StringComparison.OrdinalIgnoreCase))
                    {
                        await StartHandler.HandleAsync(bot, update.Message, ct);
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
                var cb = update.CallbackQuery;
                if (cb is null || string.IsNullOrWhiteSpace(cb.Data)) break;

                if (cb.Data.StartsWith("format_", StringComparison.OrdinalIgnoreCase))
                {
                    await VideoHandler.HandleFormatSelectionAsync(bot, cb, ct);
                    break;
                }

                if (cb.Data.StartsWith("bitrate_", StringComparison.OrdinalIgnoreCase))
                {
                    await VideoHandler.HandleBitrateSelectionAsync(bot, cb, ct);
                    break;
                }

                if (cb.Data.Equals("back_to_formats", StringComparison.OrdinalIgnoreCase))
                {
                    await VideoHandler.HandleBackToFormatsAsync(bot, cb, ct);
                    break;
                }

                break;
        }
    }
}
