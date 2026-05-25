namespace TextTop.Desktop.Models;

public sealed class SaveResult
{
    public bool Success { get; set; }
    public bool IsConflict { get; set; }
    public bool IsOffline { get; set; }
    public string? ErrorMessage { get; set; }
    public MemoItem? ServerMemo { get; set; }

    public static SaveResult Ok(MemoItem memo) => new() { Success = true, ServerMemo = memo };
    public static SaveResult Conflict(string? message = null) => new() { IsConflict = true, ErrorMessage = message ?? "Version conflict." };
    public static SaveResult Offline(string? message = null) => new() { IsOffline = true, ErrorMessage = message ?? "Offline or Supabase request failed." };
    public static SaveResult Failed(string message) => new() { ErrorMessage = message };
}
