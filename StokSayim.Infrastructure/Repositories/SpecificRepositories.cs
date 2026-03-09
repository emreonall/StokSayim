using Microsoft.EntityFrameworkCore;
using StokSayim.Application.Interfaces.Repositories;
using StokSayim.Domain.Entities;
using StokSayim.Domain.Enums;
using StokSayim.Infrastructure.Data;

namespace StokSayim.Infrastructure.Repositories;

public class SayimPlaniRepository : Repository<SayimPlani>, ISayimPlaniRepository
{
    public SayimPlaniRepository(AppDbContext context) : base(context) { }

    public async Task<SayimPlani?> GetWithDetailsAsync(int id, CancellationToken ct = default)
        => await _context.SayimPlanlari
            .Include(x => x.DepoKodlari)
            .Include(x => x.Bolgeler)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IEnumerable<SayimPlani>> GetAktifPlanlarAsync(CancellationToken ct = default)
        => await _context.SayimPlanlari
            .Where(x => x.Durum == SayimPlaniDurum.Aktif || x.Durum == SayimPlaniDurum.ErpKarsilastirmaAktif)
            .ToListAsync(ct);
}

public class BolgeRepository : Repository<Bolge>, IBolgeRepository
{
    public BolgeRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Bolge>> GetByPlanIdAsync(int planId, CancellationToken ct = default)
        => await _context.Bolgeler
            .Include(x => x.EkipGrubu).ThenInclude(g => g!.Ekipler).ThenInclude(e => e.Ekip)
            .Include(x => x.SayimOturumu)
            .Where(x => x.SayimPlaniId == planId)
            .ToListAsync(ct);

    public async Task<Bolge?> GetWithOturumAsync(int id, CancellationToken ct = default)
        => await _context.Bolgeler
            .Include(x => x.EkipGrubu).ThenInclude(g => g!.Ekipler).ThenInclude(e => e.Ekip)
            .Include(x => x.SayimOturumu).ThenInclude(o => o!.SayimTurleri).ThenInclude(t => t.Katilimcilar)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
}

public class EkipRepository : Repository<Ekip>, IEkipRepository
{
    public EkipRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Ekip>> GetAktifEkiplerAsync(CancellationToken ct = default)
        => await _context.Ekipler
            .Include(x => x.EkipKullanicilari.Where(k => k.AktifMi)).ThenInclude(k => k.Kullanici)
            .Where(x => x.AktifMi)
            .ToListAsync(ct);

    public async Task<Ekip?> GetWithKullaniciarAsync(int id, CancellationToken ct = default)
        => await _context.Ekipler
            .Include(x => x.EkipKullanicilari.Where(k => k.AktifMi)).ThenInclude(k => k.Kullanici)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<Ekip?> GetByKullaniciIdAsync(string kullaniciId, CancellationToken ct = default)
        => await _context.Ekipler
            .Include(x => x.EkipKullanicilari)
            .FirstOrDefaultAsync(x => x.EkipKullanicilari.Any(k => k.KullaniciId == kullaniciId && k.AktifMi), ct);
}

public class EkipGrubuRepository : Repository<EkipGrubu>, IEkipGrubuRepository
{
    public EkipGrubuRepository(AppDbContext context) : base(context) { }

    public async Task<EkipGrubu?> GetWithEkiplerAsync(int id, CancellationToken ct = default)
        => await _context.EkipGruplari
            .Include(x => x.Ekipler).ThenInclude(e => e.Ekip)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<EkipGrubu?> GetByBolgeIdAsync(int bolgeId, CancellationToken ct = default)
        => await _context.EkipGruplari
            .Include(x => x.Ekipler).ThenInclude(e => e.Ekip)
            .FirstOrDefaultAsync(x => x.BolgeId == bolgeId, ct);
}

public class SayimOturumuRepository : Repository<SayimOturumu>, ISayimOturumuRepository
{
    public SayimOturumuRepository(AppDbContext context) : base(context) { }

    public async Task<SayimOturumu?> GetWithTurlerAsync(int id, CancellationToken ct = default)
        => await _context.SayimOturumlari
            .Include(x => x.SayimTurleri)
                .ThenInclude(t => t.Katilimcilar).ThenInclude(k => k.Ekip)
            .Include(x => x.SayimTurleri)
                .ThenInclude(t => t.TurSonucu).ThenInclude(s => s!.Detaylar)
            .Include(x => x.Bolge)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<SayimOturumu?> GetByBolgeIdAsync(int bolgeId, CancellationToken ct = default)
        => await _context.SayimOturumlari
            .Include(x => x.SayimTurleri).ThenInclude(t => t.Katilimcilar)
            .FirstOrDefaultAsync(x => x.BolgeId == bolgeId, ct);

    public async Task<IEnumerable<SayimOturumu>> GetByPlanIdAsync(int planId, CancellationToken ct = default)
        => await _context.SayimOturumlari
            .Include(x => x.Bolge)
            .Where(x => x.SayimPlaniId == planId)
            .ToListAsync(ct);
}

public class SayimTuruRepository : Repository<SayimTuru>, ISayimTuruRepository
{
    public SayimTuruRepository(AppDbContext context) : base(context) { }

    public async Task<SayimTuru?> GetWithKatilimcilarAsync(int id, CancellationToken ct = default)
        => await _context.SayimTurleri
            .Include(x => x.Katilimcilar).ThenInclude(k => k.Ekip)
            .Include(x => x.Katilimcilar).ThenInclude(k => k.SayimKaydi).ThenInclude(s => s!.Detaylar)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<SayimTuru?> GetAktifTurByOturumuAsync(int oturumuId, CancellationToken ct = default)
        => await _context.SayimTurleri
            .Include(x => x.Katilimcilar)
            .Where(x => x.SayimOturumuId == oturumuId &&
                       (x.Durum == SayimTuruDurum.Beklemede || x.Durum == SayimTuruDurum.DevamEdiyor || x.Durum == SayimTuruDurum.KarsilastirmaBekliyor))
            .OrderByDescending(x => x.TurNo)
            .FirstOrDefaultAsync(ct);
}

public class SayimKaydiRepository : Repository<SayimKaydi>, ISayimKaydiRepository
{
    public SayimKaydiRepository(AppDbContext context) : base(context) { }

    public async Task<SayimKaydi?> GetWithDetaylarAsync(int id, CancellationToken ct = default)
        => await _context.SayimKayitlari
            .Include(x => x.Detaylar)
            .Include(x => x.Ekip)
            .Include(x => x.SayimYapanKullanici)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IEnumerable<SayimKaydi>> GetByTurIdAsync(int turId, CancellationToken ct = default)
        => await _context.SayimKayitlari
            .Include(x => x.Detaylar)
            .Include(x => x.Ekip)
            .Where(x => x.SayimTuruId == turId)
            .ToListAsync(ct);

    public async Task<SayimKaydi?> GetAktifKaydiByKullaniciAsync(string kullaniciId, CancellationToken ct = default)
        => await _context.SayimKayitlari
            .Include(x => x.Detaylar)
            .Include(x => x.SayimTuru).ThenInclude(t => t.SayimOturumu).ThenInclude(o => o.Bolge)
            .Where(x => x.SayimYapanKullaniciId == kullaniciId &&
                       (x.Durum == SayimKaydiDurum.Taslak || x.Durum == SayimKaydiDurum.Devam))
            .FirstOrDefaultAsync(ct);
}

public class ErpStokRepository : Repository<ErpStok>, IErpStokRepository
{
    public ErpStokRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<ErpStok>> GetByPlanIdAsync(int planId, CancellationToken ct = default)
        => await _context.ErpStoklar.Where(x => x.SayimPlaniId == planId).ToListAsync(ct);

    public async Task<IEnumerable<ErpStok>> GetByPlanAndDepoAsync(int planId, IEnumerable<string> depoKodlari, CancellationToken ct = default)
        => await _context.ErpStoklar
            .Where(x => x.SayimPlaniId == planId && depoKodlari.Contains(x.DepoKodu))
            .ToListAsync(ct);

    public async Task DeleteByPlanIdAsync(int planId, CancellationToken ct = default)
    {
        var records = await _context.ErpStoklar.Where(x => x.SayimPlaniId == planId).ToListAsync(ct);
        _context.ErpStoklar.RemoveRange(records);
    }
}

public class TurSonucuRepository : Repository<TurSonucu>, ITurSonucuRepository
{
    public TurSonucuRepository(AppDbContext context) : base(context) { }

    public async Task<TurSonucu?> GetWithDetaylarAsync(int id, CancellationToken ct = default)
        => await _context.TurSonuclari
            .Include(x => x.Detaylar).ThenInclude(d => d.ManuelKarar)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<TurSonucu?> GetByTurIdAsync(int turId, CancellationToken ct = default)
        => await _context.TurSonuclari
            .Include(x => x.Detaylar).ThenInclude(d => d.ManuelKarar)
            .FirstOrDefaultAsync(x => x.SayimTuruId == turId, ct);
}

public class GorevBildirimiRepository : Repository<GorevBildirimi>, IGorevBildirimiRepository
{
    public GorevBildirimiRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<GorevBildirimi>> GetBekleyenlerAsync(CancellationToken ct = default)
        => await _context.GorevBildirimleri
            .Include(x => x.SayimOturumu).ThenInclude(o => o.Bolge)
            .Where(x => x.Durum == GorevBildirimDurum.Beklemede)
            .OrderBy(x => x.OlusturmaTarihi)
            .ToListAsync(ct);

    public async Task<IEnumerable<GorevBildirimi>> GetByOturumuIdAsync(int oturumuId, CancellationToken ct = default)
        => await _context.GorevBildirimleri
            .Where(x => x.SayimOturumuId == oturumuId)
            .ToListAsync(ct);
}
