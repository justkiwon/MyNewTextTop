using System.Text.Json.Serialization;

namespace TextTop.Desktop.Models;

public sealed class SupabaseLoginRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("password")]
    public string Password { get; set; } = "";
}

public sealed class SupabaseRefreshRequest
{
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = "";
}

public sealed class SupabaseAuthResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = "";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("user")]
    public SupabaseUser? User { get; set; }
}

public sealed class SupabaseUser
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = "";
}
