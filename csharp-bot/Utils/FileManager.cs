using System;
using System.IO;
using BotApp.Config;

namespace BotApp.Utils;

public static class FileManager
{
    public static string GenerateUniqueFilename(string extension)
    {
        var uniqueId = Guid.NewGuid().ToString("N");
        return $"{uniqueId}.{extension}";
    }

    public static string GetTempPath(string filename)
    {
        return Path.Combine(AppConfig.TempDir, filename);
    }

    public static void EnsureTempDir()
    {
        Directory.CreateDirectory(AppConfig.TempDir);
        SimpleLogger.Info($"Временная директория проверена: {AppConfig.TempDir}");
    }

    public static bool RemoveFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                SimpleLogger.Debug($"Файл удалён: {filePath}");
                return true;
            }

            SimpleLogger.Warning($"Файл не найден для удаления: {filePath}");
            return false;
        }
        catch (IOException ex)
        {
            SimpleLogger.Error($"Ошибка при удалении файла {filePath}: {ex}", ex);
            return false;
        }
    }

    public static void CleanupTempFiles(params string[] filePaths)
    {
        foreach (var filePath in filePaths)
        {
            RemoveFile(filePath);
        }
    }
}
