using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace StokSayim.Web.Services;

/// <summary>
/// API 401 (Unauthorized) döndüğünde — token süresi dolmuş ya da geçersiz —
/// oturumu kapatıp kullanıcıyı giriş sayfasına yönlendirir.
/// Aksi halde sayfa açık kaldığı sürece (token süresi dolsa bile) istekler sessizce başarısız olur.
/// </summary>
public class UnauthorizedHandler : DelegatingHandler
{
    private readonly IServiceProvider _sp;

    public UnauthorizedHandler(IServiceProvider sp) => _sp = sp;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        // Giriş isteğinin kendi 401'i (hatalı şifre) oturum kapatma nedeni değildir
        var girisIstegi = request.RequestUri?.AbsolutePath.EndsWith("/login", StringComparison.OrdinalIgnoreCase) == true;

        if (response.StatusCode == HttpStatusCode.Unauthorized && !girisIstegi)
        {
            var nav = _sp.GetRequiredService<NavigationManager>();
            if (!nav.Uri.Contains("/login", StringComparison.OrdinalIgnoreCase))
            {
                if (_sp.GetRequiredService<AuthenticationStateProvider>() is JwtAuthStateProvider auth)
                    await auth.LogoutAsync();
                nav.NavigateTo("/login");
            }
        }

        return response;
    }
}
