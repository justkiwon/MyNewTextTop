using System.Windows.Input;
using TextTop.Desktop.Models;
using TextTop.Desktop.Services;

namespace TextTop.Desktop.ViewModels;

public sealed class MemoWindowViewModel : NotifyBase
{
    private readonly AuthTokenStoreModel? _token;
    private readonly SupabaseMemoStore? _memoStore;
    private readonly LocalMemoCacheStore _cacheStore;
    private string _statusText;

    public event EventHandler? RequestNewMemo;

    public MemoWindowViewModel(MemoItem memo, AuthTokenStoreModel? token, SupabaseMemoStore? memoStore, LocalMemoCacheStore cacheStore)
    {
        Memo = memo;
        _token = token;
        _memoStore = memoStore;
        _cacheStore = cacheStore;
        _statusText = memo.SyncState.ToString();
        SaveCommand = new RelayCommand(async _ => { await SaveAsync(); });
        NewCommand = new RelayCommand(_ =>
        {
            RequestNewMemo?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        });
    }

    public MemoItem Memo { get; }
    public ICommand SaveCommand { get; }
    public ICommand NewCommand { get; }

    public string Title
    {
        get => Memo.Title;
        set
        {
            Memo.Title = value;
            OnPropertyChanged();
        }
    }

    public string Content
    {
        get => Memo.Content;
        set
        {
            Memo.Content = value;
            OnPropertyChanged();
        }
    }

    public bool IsTopMost
    {
        get => Memo.IsTopMost;
        set
        {
            Memo.IsTopMost = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public async Task<SaveResult> SaveAsync()
    {
        Memo.UpdatedAt = DateTime.UtcNow;
        Memo.IsOpen = true; // Keep windows open on next launch. Change to false here if closed windows should stay closed.

        if (_memoStore is null || _token is null || !NetworkService.LooksOnline())
        {
            return await SaveOfflineAsync("No network or Supabase config. Saved to local cache only.");
        }

        var result = Memo.IsLocalOnly || Memo.SyncState == SyncState.PendingInsert
            ? await _memoStore.InsertAsync(Memo, _token)
            : await _memoStore.UpdateAsync(Memo, _token);

        if (result.Success && result.ServerMemo is not null)
        {
            CopyFrom(result.ServerMemo);
            Memo.SyncState = SyncState.Synced;
            await _cacheStore.MarkSyncedAsync(Memo);
            StatusText = $"Saved to Supabase. Version {Memo.Version}";
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Content));
            OnPropertyChanged(nameof(IsTopMost));
            return result;
        }

        if (result.IsConflict)
        {
            Memo.SyncState = SyncState.Conflict;
            await _cacheStore.MarkConflictAsync(Memo);
            StatusText = "Conflict. Local memo preserved; not overwritten.";
            return result;
        }

        return await SaveOfflineAsync(result.ErrorMessage ?? "Supabase save failed. Saved to local cache only.");
    }

    public void CaptureWindowBounds(double left, double top, double width, double height)
    {
        Memo.LeftPos = left;
        Memo.TopPos = top;
        Memo.Width = width;
        Memo.Height = height;
    }

    private async Task<SaveResult> SaveOfflineAsync(string message)
    {
        Memo.SyncState = Memo.IsLocalOnly ? SyncState.PendingInsert : SyncState.PendingUpdate;
        await _cacheStore.UpsertMemoAsync(Memo);
        StatusText = $"Local saved only. {message}";
        return SaveResult.Offline(message);
    }

    private void CopyFrom(MemoItem serverMemo)
    {
        Memo.Id = serverMemo.Id;
        Memo.OwnerId = serverMemo.OwnerId;
        Memo.Title = serverMemo.Title;
        Memo.Content = serverMemo.Content;
        Memo.IsTopMost = serverMemo.IsTopMost;
        Memo.LeftPos = serverMemo.LeftPos;
        Memo.TopPos = serverMemo.TopPos;
        Memo.Width = serverMemo.Width;
        Memo.Height = serverMemo.Height;
        Memo.IsOpen = serverMemo.IsOpen;
        Memo.Version = serverMemo.Version;
        Memo.BaseVersion = serverMemo.Version;
        Memo.IsDeleted = serverMemo.IsDeleted;
        Memo.CreatedAt = serverMemo.CreatedAt;
        Memo.UpdatedAt = serverMemo.UpdatedAt;
        Memo.IsLocalOnly = false;
    }
}
