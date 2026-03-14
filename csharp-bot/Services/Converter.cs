using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BotApp.Config;
using BotApp.Utils;

namespace BotApp.Services;

public sealed class ConverterError : Exception
{
    public ConverterError(string message) : base(message) { }
}

public static class Converter
{
    public static async Task<string> ConvertVideoToAudioAsync(
        string inputPath,
        string outputPath,
        string formatName,
        string? bitrate = null,
        CancellationToken cancellationToken = default)
    {
        if (!AppConfig.AvailableFormats.TryGetValue(formatName, out var formatConfig))
        {
            throw new ConverterError($"Неподдерживаемый формат: {formatName}");
        }

        var effectiveBitrate = bitrate ?? formatConfig.Bitrate;

        var args = new List<string>
        {
            "-i", inputPath,
            "-vn",
            "-acodec", formatConfig.Codec
        };

        if (!string.IsNullOrWhiteSpace(effectiveBitrate))
        {
            args.AddRange(new[] { "-b:a", effectiveBitrate! });
        }

        args.AddRange(new[] { "-y", outputPath });

        SimpleLogger.Info($"Convert: {inputPath} -> {outputPath} [{formatName}, {effectiveBitrate ?? "lossless"}]");
        SimpleLogger.Debug($"FFmpeg args: {string.Join(" ", args)}");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var arg in args)
                psi.ArgumentList.Add(arg);

            using var process = Process.Start(psi);
            if (process is null)
                throw new ConverterError("Не удалось запустить FFmpeg");

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(AppConfig.FfmpegTimeout));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                try { process.Kill(true); } catch { }
                SimpleLogger.Error($"FFmpeg timeout: {inputPath}");
                throw new ConverterError($"Таймаут конвертации ({AppConfig.FfmpegTimeout} сек)");
            }

            if (process.ExitCode != 0)
            {
                var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
                SimpleLogger.Error($"FFmpeg exit {process.ExitCode}: {stderr}");
                throw new ConverterError($"FFmpeg вернул код {process.ExitCode}");
            }

            if (!File.Exists(outputPath))
                throw new ConverterError("Выходной файл не создан");

            SimpleLogger.Info($"Conversion OK: {outputPath}");
            return outputPath;
        }
        catch (Win32Exception)
        {
            SimpleLogger.Error("FFmpeg not found in PATH");
            throw new ConverterError("FFmpeg не найден. Установите FFmpeg и добавьте его в PATH.");
        }
        catch (ConverterError) { throw; }
        catch (Exception ex)
        {
            SimpleLogger.Error($"Unexpected conversion error: {ex}", ex);
            throw new ConverterError($"Ошибка конвертации: {ex.Message}");
        }
    }
}
