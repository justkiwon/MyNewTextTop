namespace TextTop.Desktop.Models;

public sealed class AppConfig
{
    public string SupabaseUrl { get; set; } = "";
    public string SupabaseAnonKey { get; set; } = "";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SupabaseUrl)
        && !SupabaseUrl.Contains("YOUR_PROJECT_REF", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(SupabaseAnonKey)
        && !SupabaseAnonKey.Contains("YOUR_SUPABASE", StringComparison.OrdinalIgnoreCase);
}
