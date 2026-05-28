using Blazored.LocalStorage;

namespace WalletApp.Services;

public class AuthService
{
    private readonly ILocalStorageService _localStorage;
    private readonly GoogleSheetsService _sheets;
    private readonly IConfiguration _config;

    public bool IsLoggedIn { get; private set; }
    public string? UserEmail { get; private set; }
    public string? UserName { get; private set; }
    public string? UserPhoto { get; private set; }

    public event Action? AuthStateChanged;

    public AuthService(ILocalStorageService localStorage,
                       GoogleSheetsService sheets,
                       IConfiguration config)
    {
        _localStorage = localStorage;
        _sheets = sheets;
        _config = config;
    }

    public async Task InitAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("access_token");
        if (!string.IsNullOrEmpty(token))
        {
            _sheets.SetAccessToken(token);
            IsLoggedIn = true;
            UserEmail = await _localStorage.GetItemAsync<string>("user_email");
            UserName = await _localStorage.GetItemAsync<string>("user_name");
            UserPhoto = await _localStorage.GetItemAsync<string>("user_photo");
            AuthStateChanged?.Invoke();
        }
    }

    public string GetGoogleLoginUrl()
    {
        var clientId = _config["Google:ClientId"];
        var redirectUri = Uri.EscapeDataString(GetRedirectUri());
        var scope = Uri.EscapeDataString("https://www.googleapis.com/auth/spreadsheets email profile");
        return $"https://accounts.google.com/o/oauth2/v2/auth" +
               $"?client_id={clientId}&redirect_uri={redirectUri}" +
               $"&response_type=token&scope={scope}&include_granted_scopes=true";
    }

    private string GetRedirectUri()
    {
        return _config["Auth:RedirectUri"];
    }

    public async Task HandleCallbackAsync(string accessToken, string? email, string? name, string? photo)
    {
        await _localStorage.SetItemAsync("access_token", accessToken);
        await _localStorage.SetItemAsync("user_email", email ?? "");
        await _localStorage.SetItemAsync("user_name", name ?? "");
        await _localStorage.SetItemAsync("user_photo", photo ?? "");

        _sheets.SetAccessToken(accessToken);
        IsLoggedIn = true;
        UserEmail = email;
        UserName = name;
        UserPhoto = photo;
        AuthStateChanged?.Invoke();
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync("access_token");
        await _localStorage.RemoveItemAsync("user_email");
        await _localStorage.RemoveItemAsync("user_name");
        await _localStorage.RemoveItemAsync("user_photo");
        IsLoggedIn = false;
        UserEmail = null;
        UserName = null;
        UserPhoto = null;
        AuthStateChanged?.Invoke();
    }
}