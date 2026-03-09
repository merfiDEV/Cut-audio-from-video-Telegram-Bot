using System;
using System.Collections.Generic;
using System.IO;
using BotApp.Utils;

namespace BotApp.Config;

public static class AppConfig
{
    public static string BotToken => Env.Get("BOT_TOKEN");
    public static long AdminUserId => Env.GetLong("ADMIN_USER_ID", 0);

    public static string BaseDir => Directory.GetCurrentDirectory();
    public static string TempDir => Path.Combine(BaseDir, "temp");

    public static long MaxFileSize => Env.GetLong("MAX_FILE_SIZE", 50L * 1024 * 1024);
    public static int FfmpegTimeout => Env.GetInt("FFMPEG_TIMEOUT", 300);

    public static IReadOnlyDictionary<string, FormatConfig> AvailableFormats { get; } =
        new Dictionary<string, FormatConfig>(StringComparer.OrdinalIgnoreCase)
        {
            ["mp3"] = new("MP3", "libmp3lame", "192k"),
            ["wav"] = new("WAV", "pcm_s16le", null),
            ["ogg"] = new("OGG", "libvorbis", "160k"),
            ["m4a"] = new("M4A", "aac", "192k"),
        };
}

public sealed record FormatConfig(string Label, string Codec, string? Bitrate);
