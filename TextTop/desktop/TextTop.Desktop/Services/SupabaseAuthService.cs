using System.Net.Http;
using System.Net.Http.Json;
using TextTop.Desktop.Models;

namespace TextTop.Desktop.Services;

public sealed class SupabaseAuthService(AppConfig config, TokenStore tokenStore)
{
    private static readonly HttpClient _http = new();

    public async Task<AuthTokenStoreModel> LoginAsync(string email, string password, bool rememberLogin = false, bool autoLogin = false)
    {
        if (string.IsNullOrWhiteSpace(config.SupabaseUrl) || string.IsNullOrWhiteSpace(config.SupabaseAnonKey))
        {
            throw new InvalidOperationException("Supabase URL and API key are not configured.");
        }

        var url = $"{config.SupabaseUrl.TrimEnd('/')}/auth/v1/token?grant_type=password";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new SupabaseLoginRequest { Email = email, Password = password })
        };
        request.Headers.Add("apikey", config.SupabaseAnonKey);

        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed: {(int)response.StatusCode} {body}");
        }

        var auth = await response.Content.ReadFromJsonAsync<SupabaseAuthResponse>()
            ?? throw new InvalidOperationException("Supabase login response was empty.");

        if (auth.User is null)
        {
            throw new InvalidOperationException("Supabase login response did not include user information.");
        }

        var token = new AuthTokenStoreModel
        {
            AccessToken = auth.AccessToken,
            RefreshToken = auth.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddSeconds(Math.Max(0, auth.ExpiresIn - 30)),
            UserId = auth.User.Id,
            Email = auth.User.Email,
            RememberLogin = rememberLogin,
            AutoLogin = autoLogin,
            SavedPassword = rememberLogin ? password : ""
        };

        await tokenStore.SaveAsync(token);
        return token;
    }

    public async Task<AuthTokenStoreModel> EnsureValidTokenAsync(AuthTokenStoreModel token)
    {
        if (token.ExpiresAt > DateTime.UtcNow.AddMinutes(2))
        {
            return token;
        }

        return await RefreshAsync(token);
    }

    public async Task<AuthTokenStoreModel> RefreshAsync(AuthTokenStoreModel token)
    {
        if (string.IsNullOrWhiteSpace(token.RefreshToken))
        {
            if (token.AutoLogin && token.RememberLogin && !string.IsNullOrWhiteSpace(token.SavedPassword))
            {
                return await LoginAsync(token.Email, token.SavedPassword, token.RememberLogin, token.AutoLogin);
            }

            throw new InvalidOperationException("Refresh token is missing. Please log in again.");
        }

        var url = $"{config.SupabaseUrl.TrimEnd('/')}/auth/v1/token?grant_type=refresh_token";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new SupabaseRefreshRequest { RefreshToken = token.RefreshToken })
        };
        request.Headers.Add("apikey", config.SupabaseAnonKey);

        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            if (token.AutoLogin && token.RememberLogin && !string.IsNullOrWhiteSpace(token.SavedPassword))
            {
                return await LoginAsync(token.Email, token.SavedPassword, token.RememberLogin, token.AutoLogin);
            }

            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Token refresh failed: {(int)response.StatusCode} {body}");
        }

        var auth = await response.Content.ReadFromJsonAsync<SupabaseAuthResponse>()
            ?? throw new InvalidOperationException("Supabase refresh response was empty.");

        token.AccessToken = auth.AccessToken;
        token.RefreshToken = auth.RefreshToken;
        token.ExpiresAt = DateTime.UtcNow.AddSeconds(Math.Max(0, auth.ExpiresIn - 30));
        if (auth.User is not null)
        {
            token.UserId = auth.User.Id;
            token.Email = auth.User.Email;
        }

        await tokenStore.SaveAsync(token);
        return token;
    }
}
