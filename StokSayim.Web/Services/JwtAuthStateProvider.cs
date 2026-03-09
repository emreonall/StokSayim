using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace StokSayim.Web.Services;

public class JwtAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _http;
    private readonly AuthenticationState _anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public JwtAuthStateProvider(ILocalStorageService localStorage, HttpClient http)
    {
        _localStorage = localStorage;
        _http = http;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync("authToken");

        if (string.IsNullOrWhiteSpace(token))
            return _anonymous;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                await _localStorage.RemoveItemAsync("authToken");
                return _anonymous;
            }

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }
        catch
        {
            return _anonymous;
        }
    }

    public async Task LoginAsync(string token)
    {
        await _localStorage.SetItemAsStringAsync("authToken", token);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var state = await GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(state));
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync("authToken");
        _http.DefaultRequestHeaders.Authorization = null;
        NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
    }
}
