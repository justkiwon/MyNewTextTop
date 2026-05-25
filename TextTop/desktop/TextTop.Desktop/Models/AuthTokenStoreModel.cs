namespace TextTop.Desktop.Models;

public sealed class AuthTokenStoreModel
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = "";
    public bool RememberLogin { get; set; }
    public bool AutoLogin { get; set; }
    public string SavedPassword { get; set; } = "";
}
