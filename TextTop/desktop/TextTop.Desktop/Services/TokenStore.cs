using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TextTop.Desktop.Models;

namespace TextTop.Desktop.Services;

public sealed class TokenStore(AppPathService paths)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<AuthTokenStoreModel?> LoadAsync()
    {
        if (!File.Exists(paths.TokenPath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(paths.TokenPath);
            var protectedFile = JsonSerializer.Deserialize<ProtectedTokenFile>(json, JsonOptions);
            if (!string.IsNullOrWhiteSpace(protectedFile?.ProtectedPayload))
            {
                var protectedBytes = Convert.FromBase64String(protectedFile.ProtectedPayload);
                var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
                return JsonSerializer.Deserialize<AuthTokenStoreModel>(Encoding.UTF8.GetString(bytes), JsonOptions);
            }

            // Backward compatibility: older builds wrote AuthTokenStoreModel directly.
            return JsonSerializer.Deserialize<AuthTokenStoreModel>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveAsync(AuthTokenStoreModel token)
    {
        Directory.CreateDirectory(paths.AppDataDirectory);
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(token, JsonOptions));
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        var file = new ProtectedTokenFile
        {
            ProtectedPayload = Convert.ToBase64String(protectedBytes)
        };
        await File.WriteAllTextAsync(paths.TokenPath, JsonSerializer.Serialize(file, JsonOptions));
    }

    private sealed class ProtectedTokenFile
    {
        public string ProtectedPayload { get; set; } = "";
    }
}
