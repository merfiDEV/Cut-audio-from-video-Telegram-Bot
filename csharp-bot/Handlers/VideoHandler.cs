using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using BotApp.Config;
using BotApp.Keyboards;
using BotApp.Services;
using BotApp.Utils;

namespace BotApp.Handlers;

public static class VideoHandler
{
    private static readonly ConcurrentDictionary<long, UserFileInfo> UserFiles = new();

    public static async Task HandleVideoAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        if (message.From is null)
        {
            return;
        }

        var file = ExtractVideoFile(message, out var fileType);
        if (file is null)
        {
            return;
        }

        if (file.FileSize.HasValue && file.FileSize.Value > AppConfig.MaxFileSize)
        {
            var maxSizeMb = AppConfig.MaxFileSize / (1024 * 1024);
            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: $"❌ Файл слишком большой.\nМаксимальный размер: {maxSizeMb} MB",
                cancellationToken: ct);

            SimpleLogger.Warning(
                $"Пользователь {message.From.Id} отправил файл размером {file.FileSize.Value} байт (лимит: {AppConfig.MaxFileSize})");
            return;
        }

        SimpleLogger.Info(
            $"Пользователь {message.From.Id} отправил {fileType} размером {file.FileSize ?? 0} байт");

        string inputPath;

        try
        {
            var inputFilename = FileManager.GenerateUniqueFilename("mp4");
            inputPath = FileManager.GetTempPath(inputFilename);

            var telegramFile = await bot.GetFile(file.FileId, ct);

            await using var fileStream = new FileStream(inputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await bot.DownloadFile(telegramFile.FilePath!, fileStream, ct);

            SimpleLogger.Debug($"Файл скачан: {inputPath}");
        }
        catch (ApiRequestException ex)
        {
            SimpleLogger.Error($"Ошибка при скачивании файла: {ex}", ex);

            if (ex.Message.Contains("file is too big", StringComparison.OrdinalIgnoreCase))
            {
                var maxSizeMb = AppConfig.MaxFileSize / (1024 * 1024);
                await bot.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ <b>Ошибка</b>\n" +
                          "Файл слишком большой для обработки.\n" +
                          $"Максимальный размер: {maxSizeMb} MB",
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);
            }
            else
            {
                await bot.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ <b>Ошибка</b>\n" +
                          "Произошла ошибка при обработке видео.\n" +
                          "Пожалуйста, попробуйте ещё раз.",
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);
            }

            return;
        }
        catch (Exception ex)
        {
            SimpleLogger.Error($"Ошибка при скачивании файла: {ex}", ex);
            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ <b>Ошибка</b>\n" +
                      "Произошла ошибка при обработке видео.\n" +
                      "Пожалуйста, попробуйте ещё раз.",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
            return;
        }

        UserFiles[message.From.Id] = new UserFileInfo
        {
            InputPath = inputPath
        };

        await bot.SendMessage(
            chatId: message.Chat.Id,
            text: "🎵 Выберите формат аудио:",
            replyMarkup: FormatKeyboard.GetFormatKeyboard(),
            cancellationToken: ct);
    }

    public static async Task HandleFormatSelectionAsync(ITelegramBotClient bot, CallbackQuery callback, CancellationToken ct)
    {
        var formatName = callback.Data?.Replace("format_", string.Empty, StringComparison.OrdinalIgnoreCase) ?? string.Empty;

        SimpleLogger.Info($"Пользователь {callback.From.Id} выбрал формат: {formatName}");

        if (!UserFiles.TryGetValue(callback.From.Id, out var fileInfo))
        {
            await bot.AnswerCallbackQuery(
                callbackQueryId: callback.Id,
                text: "❌ Файл не найден. Отправьте видео ещё раз.",
                showAlert: true,
                cancellationToken: ct);
            return;
        }

        if (callback.Message is null)
        {
            await bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
            return;
        }

        var inputPath = fileInfo.InputPath;
        var outputFilename = $"@Spmoderbot_bot_{FileManager.GenerateUniqueFilename(formatName)}";
        var outputPath = FileManager.GetTempPath(outputFilename);

        var formatLabel = AppConfig.AvailableFormats.TryGetValue(formatName, out var cfg)
            ? cfg.Label
            : formatName.ToUpperInvariant();

        var statusMessage = await bot.EditMessageText(
            chatId: callback.Message.Chat.Id,
            messageId: callback.Message.MessageId,
            text: "⏳ <b>Обработка</b>\n" +
                  $"🔄 Извлечение аудио в {formatLabel}...",
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        try
        {
            var convertedPath = await Converter.ConvertVideoToAudioAsync(
                inputPath: inputPath,
                outputPath: outputPath,
                formatName: formatName,
                cancellationToken: ct);

            SimpleLogger.Info($"Файл сконвертирован: {convertedPath}");

            await bot.EditMessageText(
                chatId: statusMessage.Chat.Id,
                messageId: statusMessage.MessageId,
                text: "⏳ <b>Обработка</b>\n" +
                      "📤 Отправка файла...",
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            await using var stream = System.IO.File.OpenRead(convertedPath);
            var inputFile = InputFile.FromStream(stream, Path.GetFileName(convertedPath));

            var caption = "🎵 <b>Аудио из видео</b>\n" +
                          $"📦 Формат: {formatLabel}\n" +
                          "👤 От: @Spmoderbot_bot";

            if (formatName.Equals("ogg", StringComparison.OrdinalIgnoreCase))
            {
                await bot.SendAudio(
                    chatId: callback.Message.Chat.Id,
                    audio: inputFile,
                    caption: caption,
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);
            }
            else
            {
                await bot.SendDocument(
                    chatId: callback.Message.Chat.Id,
                    document: inputFile,
                    caption: caption,
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);
            }

            RuntimeStats.IncrementConversion(formatName);
            FileManager.CleanupTempFiles(inputPath, convertedPath);
            SimpleLogger.Debug("Временные файлы удалены");

            UserFiles.TryRemove(callback.From.Id, out _);

            await bot.EditMessageText(
                chatId: statusMessage.Chat.Id,
                messageId: statusMessage.MessageId,
                text: "✅ <b>Готово</b>\n" +
                      $"Аудио в формате {formatLabel} отправлено!",
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            await bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
        }
        catch (ConverterError ex)
        {
            SimpleLogger.Error($"Ошибка конвертации: {ex}");
            await bot.EditMessageText(
                chatId: statusMessage.Chat.Id,
                messageId: statusMessage.MessageId,
                text: "❌ <b>Ошибка</b>\n" +
                      "Не удалось конвертировать видео в аудио.\n" +
                      "Возможно, файл повреждён или имеет неподдерживаемый формат.",
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            FileManager.CleanupTempFiles(inputPath, outputPath);
            UserFiles.TryRemove(callback.From.Id, out _);
        }
        catch (ApiRequestException ex)
        {
            SimpleLogger.Error($"Telegram API ошибка: {ex}", ex);
            FileManager.CleanupTempFiles(inputPath, outputPath);
            UserFiles.TryRemove(callback.From.Id, out _);
        }
        catch (Exception ex)
        {
            SimpleLogger.Error($"Неожиданная ошибка: {ex}", ex);
            await bot.EditMessageText(
                chatId: statusMessage.Chat.Id,
                messageId: statusMessage.MessageId,
                text: "❌ <b>Ошибка</b>\n" +
                      "Произошла непредвиденная ошибка.",
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            FileManager.CleanupTempFiles(inputPath, outputPath);
            UserFiles.TryRemove(callback.From.Id, out _);
        }
    }

    private static VideoFileDescriptor? ExtractVideoFile(Message message, out string fileType)
    {
        fileType = "";

        if (message.Video is not null)
        {
            fileType = "video";
            return new VideoFileDescriptor(message.Video.FileId, message.Video.FileSize);
        }

        if (message.VideoNote is not null)
        {
            fileType = "video_note";
            return new VideoFileDescriptor(message.VideoNote.FileId, message.VideoNote.FileSize);
        }

        if (message.Document is not null)
        {
            if (!string.IsNullOrWhiteSpace(message.Document.MimeType) &&
                message.Document.MimeType.StartsWith("video", StringComparison.OrdinalIgnoreCase))
            {
                fileType = "document";
                return new VideoFileDescriptor(message.Document.FileId, message.Document.FileSize);
            }
        }

        return null;
    }

    private sealed class UserFileInfo
    {
        public string InputPath { get; init; } = string.Empty;
    }

    private sealed class VideoFileDescriptor
    {
        public VideoFileDescriptor(string fileId, long? fileSize)
        {
            FileId = fileId;
            FileSize = fileSize;
        }

        public string FileId { get; }
        public long? FileSize { get; }
    }
}
