using System.Windows.Input;
using TextTop.Desktop.Models;
using TextTop.Desktop.Services;

namespace TextTop.Desktop.ViewModels;

public sealed class LoginViewModel : NotifyBase
{
    private readonly AppConfig _config;
    private readonly SupabaseAuthService? _authService;
    private string _email = "";
    private string _statusMessage = "";
    private bool _rememberLogin = true;
    private bool _autoLogin = true;

    public event EventHandler? LoginSucceeded;
    public AuthTokenStoreModel? Token { get; private set; }

    public LoginViewModel(AppConfig config, SupabaseAuthService? authService, AuthTokenStoreModel? savedToken = null)
    {
        _config = config;
        _authService = authService;
        _email = savedToken?.Email ?? "";
        _rememberLogin = savedToken?.RememberLogin ?? true;
        _autoLogin = savedToken?.AutoLogin ?? true;
        StatusMessage = config.IsConfigured
            ? "Sign in with the Supabase email user for this project."
            : "Set Supabase URL/key in %AppData%\\TextTop\\config.json or appsettings.json.";
        LoginCommand = new RelayCommand(async p => await LoginAsync(p as string ?? ""));
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool RememberLogin
    {
        get => _rememberLogin;
        set => SetProperty(ref _rememberLogin, value);
    }

    public bool AutoLogin
    {
        get => _autoLogin;
        set => SetProperty(ref _autoLogin, value);
    }

    public ICommand LoginCommand { get; }

    private async Task LoginAsync(string password)
    {
        if (!_config.IsConfigured || _authService is null)
        {
            StatusMessage = "Supabase URL and anon/publishable key are required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(password))
        {
            StatusMessage = "Enter email and password.";
            return;
        }

        try
        {
            StatusMessage = "Signing in...";
            Token = await _authService.LoginAsync(Email.Trim(), password, RememberLogin, AutoLogin);
            StatusMessage = "Login succeeded.";
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }
}
