namespace TextTop.Desktop.Models;

public sealed class MemoItem
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Title { get; set; } = "Memo";
    public string Content { get; set; } = "";
    public bool IsTopMost { get; set; } = true;
    public double LeftPos { get; set; } = 100;
    public double TopPos { get; set; } = 100;
    public double Width { get; set; } = 260;
    public double Height { get; set; } = 380;
    public bool IsOpen { get; set; } = true;
    public int Version { get; set; } = 1;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // SyncState is only for the desktop cache. It is not stored in Supabase.
    public SyncState SyncState { get; set; } = SyncState.Synced;

    // BaseVersion is the server version that this window originally loaded.
    // Updates use WHERE version = BaseVersion to prevent silent overwrites.
    public int BaseVersion { get; set; } = 1;

    // New offline memos have a local Guid until their first successful insert.
    public bool IsLocalOnly { get; set; }

    public static MemoItem CreateNew(Guid ownerId) => new()
    {
        Id = Guid.NewGuid(),
        OwnerId = ownerId,
        Title = "Untitled",
        Content = "",
        IsTopMost = true,
        LeftPos = 100,
        TopPos = 100,
        Width = 260,
        Height = 380,
        IsOpen = true,
        Version = 1,
        BaseVersion = 1,
        IsLocalOnly = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        SyncState = SyncState.PendingInsert
    };

    public MemoItem Clone() => (MemoItem)MemberwiseClone();
}
