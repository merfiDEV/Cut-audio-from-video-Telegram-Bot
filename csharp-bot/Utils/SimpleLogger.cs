using System;
using System.IO;

namespace BotApp.Utils;

public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}

public static class SimpleLogger
{
    private static readonly object Sync = new();
    private static string _logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "bot.log");
    private static LogLevel _minLevel = LogLevel.Info;

    public static void Initialize(string logFilePath, LogLevel minLevel)
    {
        _logFilePath = logFilePath;
        _minLevel = minLevel;
    }

    public static void Debug(string message) => Write(LogLevel.Debug, message, null);
    public static void Info(string message) => Write(LogLevel.Info, message, null);
    public static void Warning(string message) => Write(LogLevel.Warning, message, null);
    public static void Error(string message, Exception? ex = null) => Write(LogLevel.Error, message, ex);
    public static void Critical(string message, Exception? ex = null) => Write(LogLevel.Critical, message, ex);

    private static void Write(LogLevel level, string message, Exception? ex)
    {
        if (level < _minLevel)
        {
            return;
        }

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var payload = $"{timestamp} - {level} - {message}";

        if (ex is not null)
        {
            payload = $"{payload}{Environment.NewLine}{ex}";
        }

        lock (Sync)
        {
            Console.WriteLine(payload);
            File.AppendAllText(_logFilePath, payload + Environment.NewLine);
        }
    }
}
