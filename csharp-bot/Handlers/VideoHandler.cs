using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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
    private static readonly ConcurrentDictionary<long, UserSession> Sessions = new();

    public static async Task HandleVideoAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        if (message.From is null) return;

        var file = ExtractVideoFile(message, out var fileType);
        if (file is null) return;

        if (file.FileSize.HasValue && file.FileSize.Value > AppConfig.MaxFileSize)
        {
            var maxSizeMb = AppConfig.MaxFileSize / (1024 * 1024);
            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: $"❌ Файл слишком большой.\nМаксимальный размер: {maxSizeMb} MB",
                cancellationToken: ct);
            SimpleLogger.Warning($"User {message.From.Id} sent oversized file: {file.FileSize.Value} bytes");
            return;
        }

        SimpleLogger.Info($"User {message.From.Id} sent {fileType}, size: {file.FileSize ?? 0} bytes");

        var statusMsg = await bot.SendMessage(
            chatId: message.Chat.Id,
            text: "⬇️ <b>Скачиваю файл...</b>\n<code>[░░░░░░░░░░] 0%</code>",
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        string inputPath;
        string fileInfo;

        try
        {
            var inputFilename = FileManager.GenerateUniqueFilename("mp4");
            inputPath = FileManager.GetTempPath(inputFilename);

            var telegramFile = await bot.GetFile(file.FileId, ct);

            var totalBytes = file.FileSize ?? 1;
            int lastPercent = -1;

            await using (var fileStream = new FileStream(inputPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await using var tgStream = new ProgressStream(
                    async buffer => await bot.DownloadFile(telegramFile.FilePath!, fileStream, ct),
                    totalBytes,
                    async (percent) =>
                    {
                        if (percent / 10 != lastPercent / 10)
                        {
                            lastPercent = percent;
                            var bar = BuildProgressBar(percent);
                            try
                            {
                                await bot.EditMessageText(
                                    chatId: statusMsg.Chat.Id,
                                    messageId: statusMsg.MessageId,
                                    text: $"⬇️ <b>Скачиваю файл...</b>\n<code>{bar}</code>",
                                    parseMode: ParseMode.Html,
                                    cancellationToken: ct);
                            }
                            catch { }
                        }
                    });

                await bot.DownloadFile(telegramFile.FilePath!, fileStream, ct);
            }

            SimpleLogger.Debug($"Downloaded: {inputPath}");

            fileInfo = await GetFileInfoAsync(inputPath, file.FileSize);
        }
        catch (ApiRequestException ex)
        {
            SimpleLogger.Error($"Download error: {ex}", ex);
            var errText = ex.Message.Contains("file is too big", StringComparison.OrdinalIgnoreCase)
                ? $"❌ <b>Файл слишком большой</b>\nМаксимальный размер: {AppConfig.MaxFileSize / (1024 * 1024)} MB"
                : "❌ <b>Ошибка скачивания</b>\nПопробуйте ещё раз.";

            await bot.EditMessageText(
                chatId: statusMsg.Chat.Id,
                messageId: statusMsg.MessageId,
                text: errText,
                parseMode: ParseMode.Html,
                cancellationToken: ct);
            return;
        }
        catch (Exception ex)
        {
            SimpleLogger.Error($"Download error: {ex}", ex);
            await bot.EditMessageText(
                chatId: statusMsg.Chat.Id,
                messageId: statusMsg.MessageId,
                text: "❌ <b>Ошибка при обработке видео</b>\nПопробуйте ещё раз.",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
            return;
        }

        Sessions[message.From.Id] = new UserSession { InputPath = inputPath };

        await bot.EditMessageText(
            chatId: statusMsg.Chat.Id,
            messageId: statusMsg.MessageId,
            text: $"✅ <b>Файл получен!</b>\n\n{fileInfo}\n\n🎵 <b>Выберите формат аудио:</b>",
            parseMode: ParseMode.Html,
            replyMarkup: FormatKeyboard.GetFormatKeyboard(),
            cancellationToken: ct);
    }

    public static async Task HandleFormatSelectionAsync(ITelegramBotClient bot, CallbackQuery callback, CancellationToken ct)
    {
        var formatName = callback.Data!.Replace("format_", string.Empty, StringComparison.OrdinalIgnoreCase);

        if (!Sessions.TryGetValue(callback.From.Id, out var session))
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

        SimpleLogger.Info($"User {callback.From.Id} selected format: {formatName}");

        if (formatName.Equals("mp3", StringComparison.OrdinalIgnoreCase))
        {
            session.SelectedFormat = "mp3";
            await bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
            await bot.EditMessageText(
                chatId: callback.Message.Chat.Id,
                messageId: callback.Message.MessageId,
                text: "🎵 <b>MP3 — выберите качество:</b>\n\n" +
                      "💾 <b>128 kbps</b> — меньше размер\n" +
                      "⭐ <b>192 kbps</b> — оптимальный баланс\n" +
                      "🚀 <b>320 kbps</b> — максимальное качество",
                parseMode: ParseMode.Html,
                replyMarkup: BitrateKeyboard.GetMp3BitrateKeyboard(),
                cancellationToken: ct);
            return;
        }

        await bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
        await ConvertAndSendAsync(bot, callback.Message, callback.From.Id, session, formatName, bitrate: null, ct);
    }

    public static async Task HandleBitrateSelectionAsync(ITelegramBotClient bot, CallbackQuery callback, CancellationToken ct)
    {
        var bitrateKey = callback.Data!.Replace("bitrate_", string.Empty, StringComparison.OrdinalIgnoreCase);

        if (!Sessions.TryGetValue(callback.From.Id, out var session) || session.SelectedFormat is null)
        {
            await bot.AnswerCallbackQuery(
                callbackQueryId: callback.Id,
                text: "❌ Сессия истекла. Отправьте видео ещё раз.",
                showAlert: true,
                cancellationToken: ct);
            return;
        }

        if (callback.Message is null)
        {
            await bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
            return;
        }

        if (!AppConfig.Mp3Bitrates.TryGetValue(bitrateKey, out var bitrateConfig))
        {
            await bot.AnswerCallbackQuery(callback.Id, text: "❌ Неверный битрейт", cancellationToken: ct);
            return;
        }

        SimpleLogger.Info($"User {callback.From.Id} selected MP3 bitrate: {bitrateKey}");
        await bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
        await ConvertAndSendAsync(bot, callback.Message, callback.From.Id, session, "mp3", bitrateConfig.Bitrate, ct);
    }

    public static async Task HandleBackToFormatsAsync(ITelegramBotClient bot, CallbackQuery callback, CancellationToken ct)
    {
        if (callback.Message is null)
        {
            await bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
            return;
        }

        await bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
        await bot.EditMessageText(
            chatId: callback.Message.Chat.Id,
            messageId: callback.Message.MessageId,
            text: "🎵 <b>Выберите формат аудио:</b>",
            parseMode: ParseMode.Html,
            replyMarkup: FormatKeyboard.GetFormatKeyboard(),
            cancellationToken: ct);
    }

    private static async Task ConvertAndSendAsync(
        ITelegramBotClient bot,
        Message originalMsg,
        long userId,
        UserSession session,
        string formatName,
        string? bitrate,
        CancellationToken ct)
    {
        if (!AppConfig.AvailableFormats.TryGetValue(formatName, out var formatConfig))
        {
            await bot.EditMessageText(
                chatId: originalMsg.Chat.Id,
                messageId: originalMsg.MessageId,
                text: "❌ <b>Неизвестный формат</b>",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
            return;
        }

        var effectiveBitrate = bitrate ?? formatConfig.Bitrate;
        var formatLabel = formatConfig.Label;
        var bitrateLabel = bitrate is not null ? $" {bitrate}" : string.Empty;

        var outputFilename = $"@Spmoderbot_bot_{FileManager.GenerateUniqueFilename(formatName)}";
        var outputPath = FileManager.GetTempPath(outputFilename);
        var inputPath = session.InputPath;

        var statusMsg = await bot.EditMessageText(
            chatId: originalMsg.Chat.Id,
            messageId: originalMsg.MessageId,
            text: $"⚙️ <b>Конвертация в {formatLabel}{bitrateLabel}</b>\n\n" +
                  $"<code>{BuildProgressBar(0)}</code>\nАнализ потоков...",
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var progressTask = AnimateConversionProgressAsync(bot, statusMsg, formatLabel + bitrateLabel, progressCts.Token);

        try
        {
            var convertedPath = await Converter.ConvertVideoToAudioAsync(
                inputPath: inputPath,
                outputPath: outputPath,
                formatName: formatName,
                bitrate: effectiveBitrate,
                cancellationToken: ct);

            await progressCts.CancelAsync();
            try { await progressTask; } catch (OperationCanceledException) { }

            SimpleLogger.Info($"Converted: {convertedPath}");

            await bot.EditMessageText(
                chatId: statusMsg.Chat.Id,
                messageId: statusMsg.MessageId,
                text: $"📤 <b>Отправляю файл...</b>\n\n" +
                      $"<code>{BuildProgressBar(100)}</code>",
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            await using var stream = System.IO.File.OpenRead(convertedPath);
            var inputFile = InputFile.FromStream(stream, Path.GetFileName(convertedPath));

            var sizeKb = new FileInfo(convertedPath).Length / 1024;
            var caption = $"🎵 <b>Аудио извлечено</b>\n" +
                          $"📦 Формат: <b>{formatLabel}{bitrateLabel}</b>\n" +
                          $"💾 Размер: <b>{sizeKb} KB</b>\n" +
                          $"🤖 @Spmoderbot_bot";

            if (formatName.Equals("ogg", StringComparison.OrdinalIgnoreCase))
            {
                await bot.SendAudio(
                    chatId: originalMsg.Chat.Id,
                    audio: inputFile,
                    caption: caption,
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);
            }
            else
            {
                await bot.SendDocument(
                    chatId: originalMsg.Chat.Id,
                    document: inputFile,
                    caption: caption,
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);
            }

            RuntimeStats.IncrementConversion(formatName);
            FileManager.CleanupTempFiles(inputPath, convertedPath);
            Sessions.TryRemove(userId, out _);

            await bot.EditMessageText(
                chatId: statusMsg.Chat.Id,
                messageId: statusMsg.MessageId,
                text: $"✅ <b>Готово!</b>\n\nФормат: <b>{formatLabel}{bitrateLabel}</b>",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
        }
        catch (ConverterError ex)
        {
            await progressCts.CancelAsync();
            try { await progressTask; } catch (OperationCanceledException) { }

            SimpleLogger.Error($"Conversion error: {ex}");
            await bot.EditMessageText(
                chatId: statusMsg.Chat.Id,
                messageId: statusMsg.MessageId,
                text: "❌ <b>Ошибка конвертации</b>\n\nВозможно, файл повреждён или формат не поддерживается.\nПопробуйте другой формат.",
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            FileManager.CleanupTempFiles(inputPath, outputPath);
            Sessions.TryRemove(userId, out _);
        }
        catch (ApiRequestException ex)
        {
            await progressCts.CancelAsync();
            try { await progressTask; } catch (OperationCanceledException) { }

            SimpleLogger.Error($"Telegram API error: {ex}", ex);
            FileManager.CleanupTempFiles(inputPath, outputPath);
            Sessions.TryRemove(userId, out _);
        }
        catch (Exception ex)
        {
            await progressCts.CancelAsync();
            try { await progressTask; } catch (OperationCanceledException) { }

            SimpleLogger.Error($"Unexpected error: {ex}", ex);
            await bot.EditMessageText(
                chatId: statusMsg.Chat.Id,
                messageId: statusMsg.MessageId,
                text: "❌ <b>Непредвиденная ошибка</b>\n\nПожалуйста, попробуйте ещё раз.",
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            FileManager.CleanupTempFiles(inputPath, outputPath);
            Sessions.TryRemove(userId, out _);
        }
    }

    private static async Task AnimateConversionProgressAsync(
        ITelegramBotClient bot,
        Message statusMsg,
        string formatLabel,
        CancellationToken ct)
    {
        var stages = new[]
        {
            (5,  "Анализ видеопотока..."),
            (20, "Декодирование аудио..."),
            (40, "Применение кодека..."),
            (60, "Кодирование..."),
            (75, "Оптимизация..."),
            (90, "Финализация файла..."),
        };

        foreach (var (percent, label) in stages)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await bot.EditMessageText(
                    chatId: statusMsg.Chat.Id,
                    messageId: statusMsg.MessageId,
                    text: $"⚙️ <b>Конвертация в {formatLabel}</b>\n\n" +
                          $"<code>{BuildProgressBar(percent)}</code>\n{label}",
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);
            }
            catch (OperationCanceledException) { return; }
            catch { }

            await Task.Delay(1800, ct);
        }
    }

    private static async Task<string> GetFileInfoAsync(string inputPath, long? fileSizeBytes)
    {
        var sizeMb = fileSizeBytes.HasValue
            ? $"{fileSizeBytes.Value / (1024.0 * 1024.0):F1} MB"
            : "неизвестно";

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = $"-v quiet -print_format compact -show_streams -select_streams a \"{inputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc is null) return $"📦 Размер: <b>{sizeMb}</b>";

            var output = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();

            var duration = "неизвестно";
            var codecName = "неизвестно";

            foreach (var part in output.Split('|'))
            {
                if (part.StartsWith("duration=", StringComparison.OrdinalIgnoreCase))
                {
                    if (double.TryParse(part[9..], System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var secs))
                    {
                        var ts = TimeSpan.FromSeconds(secs);
                        duration = ts.Hours > 0
                            ? $"{ts.Hours}ч {ts.Minutes:D2}м {ts.Seconds:D2}с"
                            : $"{ts.Minutes}м {ts.Seconds:D2}с";
                    }
                }
                else if (part.StartsWith("codec_name=", StringComparison.OrdinalIgnoreCase))
                {
                    codecName = part[11..].Trim().ToUpperInvariant();
                }
            }

            if (duration == "неизвестно")
            {
                var psi2 = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-v quiet -print_format compact -show_format \"{inputPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc2 = Process.Start(psi2);
                if (proc2 is not null)
                {
                    var out2 = await proc2.StandardOutput.ReadToEndAsync();
                    await proc2.WaitForExitAsync();
                    foreach (var part in out2.Split('|'))
                    {
                        if (part.StartsWith("duration=", StringComparison.OrdinalIgnoreCase))
                        {
                            if (double.TryParse(part[9..], System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out var secs))
                            {
                                var ts = TimeSpan.FromSeconds(secs);
                                duration = ts.Hours > 0
                                    ? $"{ts.Hours}ч {ts.Minutes:D2}м {ts.Seconds:D2}с"
                                    : $"{ts.Minutes}м {ts.Seconds:D2}с";
                            }
                        }
                    }
                }
            }

            return $"📁 Размер: <b>{sizeMb}</b>\n" +
                   $"⏱ Длительность: <b>{duration}</b>\n" +
                   $"🎙 Аудио-кодек: <b>{codecName}</b>";
        }
        catch
        {
            return $"📦 Размер: <b>{sizeMb}</b>";
        }
    }

    private static string BuildProgressBar(int percent)
    {
        percent = Math.Clamp(percent, 0, 100);
        const int total = 12;
        var filled = (int)Math.Round(percent / 100.0 * total);
        var bar = new string('█', filled) + new string('░', total - filled);
        return $"[{bar}] {percent}%";
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

        if (message.Document is not null &&
            !string.IsNullOrWhiteSpace(message.Document.MimeType) &&
            message.Document.MimeType.StartsWith("video", StringComparison.OrdinalIgnoreCase))
        {
            fileType = "document";
            return new VideoFileDescriptor(message.Document.FileId, message.Document.FileSize);
        }

        return null;
    }

    private sealed class UserSession
    {
        public string InputPath { get; init; } = string.Empty;
        public string? SelectedFormat { get; set; }
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

    private sealed class ProgressStream : Stream
    {
        public ProgressStream(Func<byte[], Task> writer, long total, Func<int, Task> onProgress) { }
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position { get; set; }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => 0;
        public override long Seek(long offset, SeekOrigin origin) => 0;
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
    }
}
