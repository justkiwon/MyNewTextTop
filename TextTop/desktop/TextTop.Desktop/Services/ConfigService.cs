using System;
using System.IO;
using System.Text.Json;
using TextTop.Desktop.Models;

namespace TextTop.Desktop.Services;

public sealed class ConfigService(AppPathService paths)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    public async Task<AppConfig> LoadAsync()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            paths.ConfigPath,
            Path.Combine(baseDir, "appsettings.json"),
            Path.Combine(baseDir, "appsettings.example.json")
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                await using var stream = File.OpenRead(path);
                return await JsonSerializer.DeserializeAsync<AppConfig>(stream, JsonOptions) ?? new AppConfig();
            }
            catch
            {
                // A broken config should not crash the app; LoginWindow shows a readable setup message.
                return new AppConfig();
            }
        }

        return new AppConfig();
    }
}
