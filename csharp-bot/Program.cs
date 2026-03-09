using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using BotApp.Config;
using BotApp.Handlers;
using BotApp.Utils;

EnvLoader.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

SimpleLogger.Initialize(Path.Combine(Directory.GetCurrentDirectory(), "bot.log"), LogLevel.Info);
SimpleLogger.Info("Бот запускается...");

if (string.IsNullOrWhiteSpace(AppConfig.BotToken))
{
    SimpleLogger.Error("BOT_TOKEN не найден в переменных окружения!");
    SimpleLogger.Error("Создайте .env файл с переменной BOT_TOKEN");
    Environment.Exit(1);
}

RuntimeStats.MarkStart();
FileManager.EnsureTempDir();
SimpleLogger.Info("Временная директория готова");

var bot = new TelegramBotClient(AppConfig.BotToken);
var dispatcher = new UpdateDispatcher();

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = Array.Empty<UpdateType>()
};

bot.StartReceiving(
    updateHandler: async (client, update, token) =>
    {
        try
        {
            await dispatcher.HandleUpdateAsync(client, update, token);
        }
        catch (Exception ex)
        {
            RuntimeStats.IncrementError();
            SimpleLogger.Error($"Глобальная ошибка: {ex}", ex);

            if (update.Message is not null)
            {
                try
                {
                    await client.SendMessage(
                        chatId: update.Message.Chat.Id,
                        text: "❌ Произошла ошибка при обработке запроса.\nПожалуйста, попробуйте ещё раз позже.",
                        cancellationToken: token);
                }
                catch (Exception sendEx)
                {
                    SimpleLogger.Error($"Не удалось отправить сообщение об ошибке: {sendEx}", sendEx);
                }
            }
        }
    },
    errorHandler: async (client, exception, token) =>
    {
        RuntimeStats.IncrementError();
        SimpleLogger.Error($"Критическая ошибка: {exception}", exception);

        if (exception is ApiRequestException apiException)
        {
            SimpleLogger.Error($"Telegram API ошибка: {apiException.ErrorCode} - {apiException.Message}");
        }

        await Task.CompletedTask;
    },
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token);

try
{
    SimpleLogger.Info("Запуск бота в режиме polling...");
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (TaskCanceledException)
{
    SimpleLogger.Info("Получен сигнал остановки");
}
catch (Exception ex)
{
    RuntimeStats.IncrementError();
    SimpleLogger.Error($"Фатальная ошибка: {ex}", ex);
    Environment.Exit(1);
}
finally
{
    SimpleLogger.Info("Бот останавливается...");
}
