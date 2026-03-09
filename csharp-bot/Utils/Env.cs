using System;

namespace BotApp.Utils;

public static class Env
{
    public static string Get(string key, string defaultValue = "")
    {
        return Environment.GetEnvironmentVariable(key) ?? defaultValue;
    }

    public static long GetLong(string key, long defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(key);
        return long.TryParse(raw, out var value) ? value : defaultValue;
    }

    public static int GetInt(string key, int defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(key);
        return int.TryParse(raw, out var value) ? value : defaultValue;
    }
}
