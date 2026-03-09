using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StokSayim.Application.DTOs.Auth;
using StokSayim.Application.Interfaces.Services;
using StokSayim.Domain.Entities;

namespace StokSayim.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;
    private readonly IEkipService _ekipService;

    public AuthService(UserManager<ApplicationUser> userManager, IConfiguration config, IEkipService ekipService)
    {
        _userManager = userManager;
        _config = config;
        _ekipService = ekipService;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Geçersiz e-posta veya şifre.");

        if (!user.AktifMi)
            throw new UnauthorizedAccessException("Hesabınız aktif değil.");

        if (!await _userManager.CheckPasswordAsync(user, request.Sifre))
            throw new UnauthorizedAccessException("Geçersiz e-posta veya şifre.");

        var roles = await _userManager.GetRolesAsync(user);
        var token = GenerateJwtToken(user, roles);

        return new AuthResponseDto(
            Token: token,
            RefreshToken: string.Empty, // Gerekirse implement edilebilir
            TokenSonGecerlilik: DateTime.UtcNow.AddHours(8),
            KullaniciId: user.Id,
            AdSoyad: user.AdSoyad,
            Email: user.Email!,
            Roller: roles
        );
    }

    public Task<AuthResponseDto> RefreshTokenAsync(string token, CancellationToken ct = default)
        => throw new NotImplementedException();

    public async Task<AktifGorevDto?> GetAktifGorevAsync(string kullaniciId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(kullaniciId);
        if (user == null) return null;

        var ekip = await _ekipService.GetEkipByKullaniciIdAsync(kullaniciId, ct);

        return new AktifGorevDto(
            KullaniciId: kullaniciId,
            AdSoyad: user.AdSoyad,
            EkipId: ekip?.Id,
            EkipAdi: ekip?.EkipAdi,
            EkipRolu: null, // SayimTuruKatilimci'dan doldurulur
            BolgeId: null,
            BolgeAdi: null,
            SayimOturumuId: null,
            SayimTuruId: null,
            TurNo: null,
            TurTipi: null,
            SayimKaydiId: null,
            SayimKaydiDurum: null,
            GorevVar: ekip != null
        );
    }

    private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("adSoyad", user.AdSoyad),
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
