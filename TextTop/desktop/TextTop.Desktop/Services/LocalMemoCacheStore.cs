using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TextTop.Desktop.Models;

namespace TextTop.Desktop.Services;

public sealed class LocalMemoCacheStore(AppPathService paths)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<MemoCacheDocument> LoadCacheAsync()
    {
        if (!File.Exists(paths.CachePath))
        {
            return new MemoCacheDocument();
        }

        try
        {
            await using var stream = File.OpenRead(paths.CachePath);
            return await JsonSerializer.DeserializeAsync<MemoCacheDocument>(stream, JsonOptions) ?? new MemoCacheDocument();
        }
        catch
        {
            return new MemoCacheDocument();
        }
    }

    public async Task SaveCacheAsync(MemoCacheDocument document)
    {
        Directory.CreateDirectory(paths.AppDataDirectory);
        await using var stream = File.Create(paths.CachePath);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions);
    }

    public async Task UpsertMemoAsync(MemoItem memo)
    {
        var cache = await LoadCacheAsync();
        var index = cache.Memos.FindIndex(m => m.Id == memo.Id);
        if (index >= 0)
        {
            cache.Memos[index] = memo.Clone();
        }
        else
        {
            cache.Memos.Add(memo.Clone());
        }

        await SaveCacheAsync(cache);
    }

    public async Task<List<MemoItem>> GetPendingMemosAsync()
    {
        var cache = await LoadCacheAsync();
        return cache.Memos
            .Where(m => m.SyncState is SyncState.PendingInsert or SyncState.PendingUpdate)
            .ToList();
    }

    public async Task MarkSyncedAsync(MemoItem serverMemo)
    {
        serverMemo.SyncState = SyncState.Synced;
        serverMemo.BaseVersion = serverMemo.Version;
        serverMemo.IsLocalOnly = false;
        await UpsertMemoAsync(serverMemo);
    }

    public async Task MarkConflictAsync(MemoItem localMemo)
    {
        localMemo.SyncState = SyncState.Conflict;
        await UpsertMemoAsync(localMemo);
    }
}
