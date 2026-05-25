using TextTop.Desktop.Models;

namespace TextTop.Desktop.Services;

public sealed class MemoSyncService(LocalMemoCacheStore cacheStore, SupabaseMemoStore memoStore)
{
    public async Task<List<MemoItem>> LoadForStartupAsync(AuthTokenStoreModel token)
    {
        try
        {
            var serverMemos = await memoStore.LoadMemosAsync(token);
            foreach (var pending in await cacheStore.GetPendingMemosAsync())
            {
                var synced = await TrySyncOneAsync(pending, token);
                if (synced is not null)
                {
                    serverMemos.RemoveAll(m => m.Id == synced.Id);
                    serverMemos.Insert(0, synced);
                }
            }

            await cacheStore.SaveCacheAsync(new MemoCacheDocument
            {
                LastSyncedAt = DateTime.UtcNow,
                Memos = serverMemos
            });
            return serverMemos;
        }
        catch
        {
            var cache = await cacheStore.LoadCacheAsync();
            return cache.Memos.Where(m => !m.IsDeleted).ToList();
        }
    }

    public async Task SyncPendingAsync(AuthTokenStoreModel token)
    {
        foreach (var pending in await cacheStore.GetPendingMemosAsync())
        {
            await TrySyncOneAsync(pending, token);
        }
    }

    private async Task<MemoItem?> TrySyncOneAsync(MemoItem pending, AuthTokenStoreModel token)
    {
        var result = pending.SyncState == SyncState.PendingInsert || pending.IsLocalOnly
            ? await memoStore.InsertAsync(pending, token)
            : await memoStore.UpdateAsync(pending, token);

        if (result.Success && result.ServerMemo is not null)
        {
            await cacheStore.MarkSyncedAsync(result.ServerMemo);
            return result.ServerMemo;
        }

        if (result.IsConflict)
        {
            await cacheStore.MarkConflictAsync(pending);
        }

        return null;
    }
}
