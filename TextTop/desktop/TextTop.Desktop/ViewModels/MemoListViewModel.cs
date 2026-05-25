using System.Collections.ObjectModel;
using System.Windows.Input;
using TextTop.Desktop.Models;
using TextTop.Desktop.Services;

namespace TextTop.Desktop.ViewModels;

public sealed class MemoListViewModel : NotifyBase
{
    private readonly AuthTokenStoreModel? _token;
    private readonly SupabaseMemoStore? _memoStore;
    private readonly LocalMemoCacheStore _cacheStore;
    private MemoItem? _selectedMemo;
    private string _statusText = "";

    public event EventHandler<MemoItem>? RequestOpenMemo;
    public event EventHandler? RequestNewMemo;

    public MemoListViewModel(
        IEnumerable<MemoItem> initialMemos,
        AuthTokenStoreModel? token,
        SupabaseMemoStore? memoStore,
        LocalMemoCacheStore cacheStore)
    {
        _token = token;
        _memoStore = memoStore;
        _cacheStore = cacheStore;

        Memos = new ObservableCollection<MemoItem>(
            initialMemos.Where(m => !m.IsDeleted).OrderByDescending(m => m.UpdatedAt));

        SelectedMemo = Memos.FirstOrDefault();
        StatusText = Memos.Count == 0
            ? "No saved memos loaded."
            : $"{Memos.Count} memos loaded.";

        RefreshCommand = new RelayCommand(async _ => await RefreshAsync());
        OpenCommand = new RelayCommand(_ =>
        {
            if (SelectedMemo is not null)
            {
                // The list displays title, but opening uses the stable memo id.
                // This prevents mistakes when several memos have the same title.
                RequestOpenMemo?.Invoke(this, SelectedMemo.Clone());
            }

            return Task.CompletedTask;
        }, _ => SelectedMemo is not null);
        NewCommand = new RelayCommand(_ =>
        {
            RequestNewMemo?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        });
    }

    public ObservableCollection<MemoItem> Memos { get; }

    public MemoItem? SelectedMemo
    {
        get => _selectedMemo;
        set
        {
            if (SetProperty(ref _selectedMemo, value) && OpenCommand is RelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand NewCommand { get; }

    public async Task RefreshAsync()
    {
        try
        {
            List<MemoItem> loaded;
            if (_memoStore is not null && _token is not null && NetworkService.LooksOnline())
            {
                var cache = await _cacheStore.LoadCacheAsync();
                var pendingLocal = cache.Memos
                    .Where(m => !m.IsDeleted && m.SyncState is SyncState.PendingInsert or SyncState.PendingUpdate or SyncState.Conflict)
                    .ToList();

                loaded = await _memoStore.LoadMemosAsync(_token);
                foreach (var localMemo in pendingLocal)
                {
                    if (loaded.All(serverMemo => serverMemo.Id != localMemo.Id))
                    {
                        loaded.Add(localMemo);
                    }
                }

                await _cacheStore.SaveCacheAsync(new MemoCacheDocument
                {
                    LastSyncedAt = DateTime.UtcNow,
                    Memos = loaded
                });
                StatusText = $"{loaded.Count} memos loaded from Supabase. Pending local items are kept.";
            }
            else
            {
                var cache = await _cacheStore.LoadCacheAsync();
                loaded = cache.Memos.Where(m => !m.IsDeleted).ToList();
                StatusText = $"{loaded.Count} memos loaded from local cache.";
            }

            Memos.Clear();
            foreach (var memo in loaded.Where(m => !m.IsDeleted).OrderByDescending(m => m.UpdatedAt))
            {
                Memos.Add(memo);
            }

            SelectedMemo = Memos.FirstOrDefault();
        }
        catch (Exception ex)
        {
            StatusText = $"Load failed: {ex.Message}";
        }
    }
}
