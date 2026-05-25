using System;
using System.IO;

namespace TextTop.Desktop.Services;

public sealed class AppPathService
{
    public string AppDataDirectory { get; }
    public string ConfigPath => Path.Combine(AppDataDirectory, "config.json");
    public string CachePath => Path.Combine(AppDataDirectory, "MemosCache.json");
    public string TokenPath => Path.Combine(AppDataDirectory, "AuthToken.json");

    public AppPathService()
    {
        AppDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TextTop");
        Directory.CreateDirectory(AppDataDirectory);
    }
}
