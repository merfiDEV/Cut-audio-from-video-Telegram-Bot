using System;
using System.Collections.Concurrent;
using System.Threading;

namespace BotApp.Utils;

public static class RuntimeStats
{
    private static DateTime? _startTime;
    private static int _errorCount;
    private static int _conversionCount;
    private static readonly ConcurrentDictionary<string, int> FormatCounts = new(
        new[]
        {
            new KeyValuePair<string, int>("mp3", 0),
            new KeyValuePair<string, int>("wav", 0),
            new KeyValuePair<string, int>("ogg", 0),
            new KeyValuePair<string, int>("m4a", 0),
            new KeyValuePair<string, int>("flac", 0),
        },
        StringComparer.OrdinalIgnoreCase);

    public static void MarkStart() => _startTime = DateTime.Now;

    public static DateTime GetStartTime() => _startTime ?? DateTime.Now;

    public static TimeSpan GetUptime(DateTime? now = null)
    {
        var current = now ?? DateTime.Now;
        return current - GetStartTime();
    }

    public static void IncrementError() => Interlocked.Increment(ref _errorCount);

    public static int GetErrorCount() => _errorCount;

    public static void IncrementConversion(string formatName)
    {
        Interlocked.Increment(ref _conversionCount);
        FormatCounts.AddOrUpdate(formatName, 1, (_, current) => current + 1);
    }

    public static int GetConversionCount() => _conversionCount;

    public static IReadOnlyDictionary<string, int> GetFormatCounts()
    {
        return new Dictionary<string, int>(FormatCounts, StringComparer.OrdinalIgnoreCase);
    }
}
