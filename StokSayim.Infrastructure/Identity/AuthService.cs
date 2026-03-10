using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StokSayim.Application.DTOs.Auth;
using StokSayim.Application.Interfaces;
using StokSayim.Application.Interfaces.Services;
using StokSayim.Domain.Entities;
using StokSayim.Domain.Enums;

namespace StokSayim.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;
    private readonly IEkipService _ekipService;
    private readonly IUnitOfWork _uow;

    public AuthService(UserManager<ApplicationUser> userManager, IConfiguration config, IEkipService ekipService, IUnitOfWork uow)
    {
        _userManager = userManager;
        _config = config;
        _ekipService = ekipService;
        _uow = uow;
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
            RefreshToken: string.Empty,
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

        // Kullanıcının ekibini bul
        var ekip = await _uow.Ekipler.GetByKullaniciIdAsync(kullaniciId, ct);
        if (ekip == null)
            return Bos(kullaniciId, user.AdSoyad);

        // Önce aktif açık sayım kaydı var mı? (Taslak veya Devam durumunda)
        var aktifKaydi = await _uow.SayimKayitlari.GetAktifKaydiByKullaniciAsync(kullaniciId, ct);
        if (aktifKaydi != null)
        {
            var tur = aktifKaydi.SayimTuru;
            var oturum = tur.SayimOturumu;
            var bolge = oturum.Bolge;
            var katilimci = tur.Katilimcilar.FirstOrDefault(k => k.EkipId == ekip.Id);

            return new AktifGorevDto(
                KullaniciId: kullaniciId,
                AdSoyad: user.AdSoyad,
                EkipId: ekip.Id,
                EkipAdi: ekip.EkipAdi,
                EkipRolu: katilimci != null
                    ? new EkipRoluDto((int)katilimci.EkipRolu, RolAdi(katilimci.EkipRolu))
                    : null,
                BolgeId: bolge.Id,
                BolgeAdi: bolge.BolgeAdi,
                SayimOturumuId: oturum.Id,
                SayimTuruId: tur.Id,
                TurNo: tur.TurNo,
                TurTipi: tur.TurTipi.ToString(),
                SayimKaydiId: aktifKaydi.Id,
                SayimKaydiDurum: aktifKaydi.Durum.ToString(),
                GorevVar: true
            );
        }

        // Aktif kayıt yoksa — ekibin bağlı olduğu bölgeyi EkipGrubuEkip üzerinden bul
        var ekipGrubuEkipEntry = await _db.Set<EkipGrubuEkip>()
            .Include(x => x.EkipGrubu)
            .FirstOrDefaultAsync(x => x.EkipId == ekip.Id, ct);

        if (ekipGrubuEkipEntry == null)
            return new AktifGorevDto(kullaniciId, user.AdSoyad, ekip.Id, ekip.EkipAdi,
                null, null, null, null, null, null, null, null, null, false);

        var bolgeEntity = await _uow.Bolgeler.GetWithOturumAsync(ekipGrubuEkipEntry.EkipGrubu.BolgeId, ct);
        if (bolgeEntity?.SayimOturumu == null)
            return new AktifGorevDto(kullaniciId, user.AdSoyad, ekip.Id, ekip.EkipAdi, null,
                bolgeEntity?.Id, bolgeEntity?.BolgeAdi, null, null, null, null, null, null, false);

        var aktifTur = await _uow.SayimTurlari.GetAktifTurByOturumuAsync(bolgeEntity.SayimOturumu.Id, ct);
        if (aktifTur == null)
            return new AktifGorevDto(kullaniciId, user.AdSoyad, ekip.Id, ekip.EkipAdi, null,
                bolgeEntity.Id, bolgeEntity.BolgeAdi, bolgeEntity.SayimOturumu.Id,
                null, null, null, null, null, false);

        var katilimciEntry = aktifTur.Katilimcilar.FirstOrDefault(k => k.EkipId == ekip.Id);

        return new AktifGorevDto(
            KullaniciId: kullaniciId,
            AdSoyad: user.AdSoyad,
            EkipId: ekip.Id,
            EkipAdi: ekip.EkipAdi,
            EkipRolu: katilimciEntry != null
                ? new EkipRoluDto((int)katilimciEntry.EkipRolu, RolAdi(katilimciEntry.EkipRolu))
                : null,
            BolgeId: bolgeEntity.Id,
            BolgeAdi: bolgeEntity.BolgeAdi,
            SayimOturumuId: bolgeEntity.SayimOturumu.Id,
            SayimTuruId: aktifTur.Id,
            TurNo: aktifTur.TurNo,
            TurTipi: aktifTur.TurTipi.ToString(),
            SayimKaydiId: katilimciEntry?.SayimKaydiId,
            SayimKaydiDurum: null,
            GorevVar: katilimciEntry != null
        );
    }

    private static AktifGorevDto Bos(string kullaniciId, string adSoyad) =>
        new(kullaniciId, adSoyad, null, null, null, null, null, null, null, null, null, null, null, false);

    private static string RolAdi(EkipRolu rol) => rol switch
    {
        EkipRolu.Birinci => "Birinci Ekip",
        EkipRolu.Ikinci => "İkinci Ekip",
        EkipRolu.Kontrol => "Kontrol Ekibi",
        _ => rol.ToString()
    };

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

    public async Task<AktifGorevlerDto> GetAktifGorevlerAsync(string kullaniciId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(kullaniciId);
        if (user == null)
            return new AktifGorevlerDto(kullaniciId, string.Empty, []);

        // Kullanıcının tüm aktif ekiplerini bul
        var ekipIdleri = await _db.Set<EkipKullanici>()
            .Where(ek => ek.KullaniciId == kullaniciId && ek.AktifMi)
            .Select(ek => ek.EkipId)
            .ToListAsync(ct);

        if (!ekipIdleri.Any())
            return new AktifGorevlerDto(kullaniciId, user.AdSoyad, []);

        // Her ekibin bağlı olduğu EkipGrubu → Bölge → SayimOturumu → aktif tur zinciri
        var ekipGrubuEkipler = await _db.Set<EkipGrubuEkip>()
            .Include(x => x.Ekip)
            .Include(x => x.EkipGrubu)
                .ThenInclude(g => g.Bolge)
                    .ThenInclude(b => b.SayimOturumu)
                        .ThenInclude(o => o!.SayimTurlari)
                            .ThenInclude(t => t.Katilimcilar)
            .Where(x => ekipIdleri.Contains(x.EkipId))
            .ToListAsync(ct);

        var secenekler = new List<GorevSecenekDto>();

        foreach (var ege in ekipGrubuEkipler)
        {
            var bolge = ege.EkipGrubu.Bolge;
            var oturum = bolge?.SayimOturumu;
            if (oturum == null) continue;

            // Aktif tur: Beklemede, DevamEdiyor veya KarsilastirmaBekliyor
            var aktifTur = oturum.SayimTurlari
                .Where(t => t.Durum == SayimTuruDurum.Beklemede ||
                            t.Durum == SayimTuruDurum.DevamEdiyor ||
                            t.Durum == SayimTuruDurum.KarsilastirmaBekliyor)
                .OrderByDescending(t => t.TurNo)
                .FirstOrDefault();

            if (aktifTur == null) continue;

            // Bu ekip bu turda katılımcı mı?
            var katilimci = aktifTur.Katilimcilar.FirstOrDefault(k => k.EkipId == ege.EkipId);
            if (katilimci == null) continue;

            // Sayım kaydının durumu
            string? kaydiDurum = null;
            bool tamamlandi = false;
            if (katilimci.SayimKaydiId.HasValue)
            {
                var kaydi = await _db.Set<SayimKaydi>()
                    .Where(k => k.Id == katilimci.SayimKaydiId.Value)
                    .Select(k => new { k.Durum })
                    .FirstOrDefaultAsync(ct);
                kaydiDurum = kaydi?.Durum.ToString();
                tamamlandi = kaydi?.Durum == SayimKaydiDurum.Tamamlandi;
            }

            secenekler.Add(new GorevSecenekDto(
                EkipId: ege.EkipId,
                EkipAdi: ege.Ekip.EkipAdi,
                EkipRolu: new EkipRoluDto((int)ege.EkipRolu, RolAdi(ege.EkipRolu)),
                BolgeId: bolge!.Id,
                BolgeAdi: bolge.BolgeAdi,
                BolgeKodu: bolge.BolgeKodu,
                SayimOturumuId: oturum.Id,
                SayimTuruId: aktifTur.Id,
                TurNo: aktifTur.TurNo,
                TurTipi: aktifTur.TurTipi.ToString(),
                SayimKaydiId: katilimci.SayimKaydiId,
                SayimKaydiDurum: kaydiDurum,
                SayimTamamlandi: tamamlandi
            ));
        }

        return new AktifGorevlerDto(kullaniciId, user.AdSoyad, secenekler);
    }
}