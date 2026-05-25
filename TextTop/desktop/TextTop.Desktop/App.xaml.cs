using System.Windows;
using System.Windows.Threading;
using TextTop.Desktop.Models;
using TextTop.Desktop.Services;
using TextTop.Desktop.ViewModels;
using TextTop.Desktop.Views;

namespace TextTop.Desktop;

public partial class App : Application
{
    private AppPathService _paths = null!;
    private ConfigService _configService = null!;
    private TokenStore _tokenStore = null!;
    private LocalMemoCacheStore _cacheStore = null!;
    private SupabaseAuthService? _authService;
    private SupabaseMemoStore? _memoStore;
    private MemoSyncService? _syncService;
    private AppConfig _config = new();
    private AuthTokenStoreModel? _token;
    private readonly List<MemoWindow> _memoWindows = [];
    private MemoListWindow? _memoListWindow;
    private DispatcherTimer? _syncTimer;
    private bool _syncRunning;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _paths = new AppPathService();
        _configService = new ConfigService(_paths);
        _cacheStore = new LocalMemoCacheStore(_paths);
        _tokenStore = new TokenStore(_paths);
        _config = await _configService.LoadAsync();

        if (_config.IsConfigured)
        {
            _authService = new SupabaseAuthService(_config, _tokenStore);
            _memoStore = new SupabaseMemoStore(_config, _authService);
            _syncService = new MemoSyncService(_cacheStore, _memoStore);
            _token = await _tokenStore.LoadAsync();
        }

        if (_token is not null && _authService is not null)
        {
            try
            {
                _token = await _authService.EnsureValidTokenAsync(_token);
            }
            catch
            {
                if (_token.AutoLogin && _token.RememberLogin && !string.IsNullOrWhiteSpace(_token.SavedPassword))
                {
                    try
                    {
                        _token = await _authService.LoginAsync(_token.Email, _token.SavedPassword, true, true);
                    }
                    catch
                    {
                        _token = null;
                    }
                }
                else
                {
                    _token = null;
                }
            }
        }

        if (_token is null)
        {
            await ShowLoginAsync();
        }
        else
        {
            StartBackgroundSync();
            await OpenInitialMemosAsync();
        }
    }

    private async Task ShowLoginAsync()
    {
        var vm = new LoginViewModel(_config, _authService, _token);
        var login = new LoginWindow(vm);
        var result = login.ShowDialog();

        if (result == true && vm.Token is not null)
        {
            _token = vm.Token;
            StartBackgroundSync();
            await OpenInitialMemosAsync();
            return;
        }

        Shutdown();
    }

    private async Task OpenInitialMemosAsync()
    {
        var userId = _token?.UserId ?? Guid.Empty;
        List<MemoItem> memos;

        if (_syncService is not null && _token is not null)
        {
            memos = await _syncService.LoadForStartupAsync(_token);
        }
        else
        {
            var cache = await _cacheStore.LoadCacheAsync();
            memos = cache.Memos.Where(m => !m.IsDeleted).ToList();
        }

        var openMemos = memos.Where(m => m.IsOpen && !m.IsDeleted).ToList();
        if (openMemos.Count == 0)
        {
            openMemos.Add(MemoItem.CreateNew(userId));
        }

        OpenMemoListWindow(memos);

        foreach (var memo in openMemos)
        {
            OpenMemoWindow(memo);
        }
    }

    private void StartBackgroundSync()
    {
        if (_syncTimer is not null)
        {
            return;
        }

        _syncTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(15)
        };
        _syncTimer.Tick += async (_, _) => await TrySyncPendingAsync();
        _syncTimer.Start();

        _ = TrySyncPendingAsync();
    }

    private async Task TrySyncPendingAsync()
    {
        if (_syncRunning || _syncService is null || _token is null || !NetworkService.LooksOnline())
        {
            return;
        }

        try
        {
            _syncRunning = true;
            await _syncService.SyncPendingAsync(_token);
        }
        catch
        {
            // Background sync must never crash the memo windows. The local cache
            // remains the source of truth until a later retry succeeds.
        }
        finally
        {
            _syncRunning = false;
        }
    }

    public void OpenNewMemoWindow()
    {
        OpenMemoWindow(MemoItem.CreateNew(_token?.UserId ?? Guid.Empty));
    }

    private void OpenMemoListWindow(List<MemoItem> memos)
    {
        var vm = new MemoListViewModel(memos, _token, _memoStore, _cacheStore);
        _memoListWindow = new MemoListWindow(vm);

        vm.RequestOpenMemo += (_, memo) => OpenMemoWindow(memo);
        vm.RequestNewMemo += (_, _) => OpenNewMemoWindow();
        _memoListWindow.Closed += (_, _) =>
        {
            _memoListWindow = null;
            ShutdownIfNothingIsOpen();
        };

        _memoListWindow.Show();
    }

    public void OpenMemoWindow(MemoItem memo)
    {
        var existing = _memoWindows.FirstOrDefault(window =>
            window.DataContext is MemoWindowViewModel vm && vm.Memo.Id == memo.Id);
        if (existing is not null)
        {
            existing.Activate();
            return;
        }

        var vm = new MemoWindowViewModel(memo, _token, _memoStore, _cacheStore);
        var window = new MemoWindow(vm)
        {
            Left = memo.LeftPos,
            Top = memo.TopPos,
            Width = Math.Max(220, memo.Width),
            Height = Math.Max(280, memo.Height),
            Topmost = memo.IsTopMost
        };

        vm.RequestNewMemo += (_, _) => OpenNewMemoWindow();
        window.Closed += (_, _) =>
        {
            _memoWindows.Remove(window);
            ShutdownIfNothingIsOpen();
        };

        _memoWindows.Add(window);
        window.Show();
    }

    private void ShutdownIfNothingIsOpen()
    {
        if (_memoWindows.Count == 0 && _memoListWindow is null)
        {
            Shutdown();
        }
    }
}
