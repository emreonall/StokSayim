using Microsoft.AspNetCore.Identity;
using StokSayim.Application.DTOs.Ekip;
using StokSayim.Application.Interfaces;
using StokSayim.Application.Interfaces.Services;
using StokSayim.Domain.Entities;

namespace StokSayim.Application.Services;

public class EkipService : IEkipService
{
    private readonly IUnitOfWork _uow;
    private readonly UserManager<ApplicationUser> _userManager;

    public EkipService(IUnitOfWork uow, UserManager<ApplicationUser> userManager)
    {
        _uow = uow;
        _userManager = userManager;
    }

    public async Task<IEnumerable<EkipDto>> GetAllAsync(CancellationToken ct = default)
    {
        var ekipler = await _uow.Ekipler.GetAktifEkiplerAsync(ct);
        return ekipler.Select(MapToDto);
    }

    public async Task<EkipDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var ekip = await _uow.Ekipler.GetWithKullaniciarAsync(id, ct);
        return ekip == null ? null : MapToDto(ekip);
    }

    public async Task<Ekip?> GetEkipByKullaniciIdAsync(string kullaniciId, CancellationToken ct = default)
        => await _uow.Ekipler.GetByKullaniciIdAsync(kullaniciId, ct);

    public async Task<EkipDto> CreateAsync(EkipOlusturDto request, CancellationToken ct = default)
    {
        var mevcutMu = await _uow.Ekipler.AnyAsync(x => x.EkipKodu == request.EkipKodu, ct);
        if (mevcutMu) throw new InvalidOperationException($"'{request.EkipKodu}' kodlu ekip zaten mevcut.");

        var ekip = new Ekip
        {
            EkipKodu = request.EkipKodu,
            EkipAdi = request.EkipAdi
        };

        await _uow.Ekipler.AddAsync(ekip, ct);
        await _uow.SaveChangesAsync(ct);
        return MapToDto(ekip);
    }

    public async Task UpdateAsync(int id, EkipOlusturDto request, CancellationToken ct = default)
    {
        var ekip = await _uow.Ekipler.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Ekip bulunamadı: {id}");

        ekip.EkipKodu = request.EkipKodu;
        ekip.EkipAdi = request.EkipAdi;
        _uow.Ekipler.Update(ekip);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task KullaniciEkleAsync(int ekipId, string kullaniciId, CancellationToken ct = default)
    {
        var ekip = await _uow.Ekipler.GetByIdAsync(ekipId, ct)
          ?? throw new KeyNotFoundException($"Ekip bulunamadı: {ekipId}");

        var mevcutEkip = await _uow.Ekipler.GetByKullaniciIdAsync(kullaniciId, ct);
        if (mevcutEkip != null && mevcutEkip.Id != ekipId)
            throw new InvalidOperationException("Kullanıcı zaten başka bir ekipte aktif.");

        var kayit = new EkipKullanici
        {
            EkipId = ekipId,
            KullaniciId = kullaniciId,
            BaslangicTarihi = DateTime.UtcNow,
            AktifMi = true
        };

        // ✅ ekip değil, kayit ekleniyor
        ekip.EkipKullanicilari.Add(kayit);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task KullaniciCikarAsync(int ekipId, string kullaniciId, CancellationToken ct = default)
    {
        var ekip = await _uow.Ekipler.GetWithKullaniciarAsync(ekipId, ct)
            ?? throw new KeyNotFoundException($"Ekip bulunamadı: {ekipId}");

        var kayit = ekip.EkipKullanicilari.FirstOrDefault(k => k.KullaniciId == kullaniciId && k.AktifMi)
            ?? throw new KeyNotFoundException("Kullanıcı bu ekipte bulunamadı.");

        kayit.AktifMi = false;
        kayit.BitisTarihi = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);
    }

    private static EkipDto MapToDto(Ekip ekip) => new(
        Id: ekip.Id,
        EkipKodu: ekip.EkipKodu,
        EkipAdi: ekip.EkipAdi,
        AktifMi: ekip.AktifMi,
        Kullanicilar: ekip.EkipKullanicilari
            .Where(k => k.AktifMi)
            .Select(k => new EkipKullaniciDto(
                KullaniciId: k.KullaniciId,
                AdSoyad: k.Kullanici?.AdSoyad ?? string.Empty,
                Email: k.Kullanici?.Email ?? string.Empty,
                AktifMi: k.AktifMi
            ))
    );
}
