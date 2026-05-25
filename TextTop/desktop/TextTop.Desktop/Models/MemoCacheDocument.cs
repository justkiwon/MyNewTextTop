namespace TextTop.Desktop.Models;

public sealed class MemoCacheDocument
{
    public DateTime LastSyncedAt { get; set; } = DateTime.MinValue;
    public List<MemoItem> Memos { get; set; } = [];
}
