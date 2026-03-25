using Microsoft.AspNetCore.Identity;
using StokSayim.Application.DTOs.Auth;
using StokSayim.Application.DTOs.Bolge;
using StokSayim.Application.DTOs.Rapor;
using StokSayim.Application.DTOs.SayimKaydi;
using StokSayim.Application.Interfaces;
using StokSayim.Application.Interfaces.Services;
using StokSayim.Domain.Entities;
using StokSayim.Domain.Enums;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace StokSayim.Application.Services;

// ──────────────────────────────────────────────────────────────────────────────
// BolgeService
// ──────────────────────────────────────────────────────────────────────────────
public class BolgeService : IBolgeService
{
    private readonly IUnitOfWork _uow;
    public BolgeService(IUnitOfWork uow) => _uow = uow;

    public async Task<IEnumerable<BolgeDto>> GetByPlanIdAsync(int planId, CancellationToken ct = default)
    {
        var bolgeler = await _uow.Bolgeler.GetByPlanIdAsync(planId, ct);
        return bolgeler.Select(b => new BolgeDto(
            Id: b.Id,
            SayimPlaniId: b.SayimPlaniId,
            BolgeKodu: b.BolgeKodu,
            BolgeAdi: b.BolgeAdi,
            Aciklama: b.Aciklama,
            EkipGrubuVarMi: b.EkipGrubu != null,
            SayimOturumuVarMi: b.SayimOturumu != null,
            OturumDurum: b.SayimOturumu?.Durum.ToString()
        ));
    }

    public async Task<BolgeDetayDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var bolge = await _uow.Bolgeler.GetWithOturumAsync(id, ct);
        if (bolge == null) return null;

        return new BolgeDetayDto(
            Id: bolge.Id,
            SayimPlaniId: bolge.SayimPlaniId,
            BolgeKodu: bolge.BolgeKodu,
            BolgeAdi: bolge.BolgeAdi,
            Aciklama: bolge.Aciklama,
            EkipGrubu: bolge.EkipGrubu == null ? null : new EkipGrubuDto(
                Id: bolge.EkipGrubu.Id,
                EkipGrubuAdi: bolge.EkipGrubu.EkipGrubuAdi,
                Ekipler: bolge.EkipGrubu.Ekipler.OrderBy(e => e.SiraNo).Select(e => new EkipGrubuEkipDto(
                    EkipId: e.EkipId,
                    EkipAdi: e.Ekip?.EkipAdi ?? string.Empty,
                    SiraNo: e.SiraNo,
                    EkipRolu: e.EkipRolu,
                    EkipRoluAdi: e.EkipRolu.ToString()
                ))
            ),
            SayimOturumu: bolge.SayimOturumu == null ? null : new SayimOturumuOzetDto(
                Id: bolge.SayimOturumu.Id,
                Durum: bolge.SayimOturumu.Durum,
                DurumAdi: bolge.SayimOturumu.Durum.ToString(),
                AktifTurNo: bolge.SayimOturumu.AktifTurNo,
                AktifTurTipi: bolge.SayimOturumu.SayimTurlari
                    .OrderByDescending(t => t.TurNo).FirstOrDefault()?.TurTipi.ToString(),
                BaslangicTarihi: bolge.SayimOturumu.BaslangicTarihi
            )
        );
    }

    public async Task<BolgeDto> CreateAsync(BolgeOlusturDto request, string kullaniciId, CancellationToken ct = default)
    {
        var mevcutMu = await _uow.Bolgeler.AnyAsync(
            b => b.SayimPlaniId == request.SayimPlaniId && b.BolgeKodu == request.BolgeKodu, ct);
        if (mevcutMu) throw new InvalidOperationException($"'{request.BolgeKodu}' kodlu bölge bu planda zaten mevcut.");

        var bolge = new Bolge
        {
            SayimPlaniId = request.SayimPlaniId,
            BolgeKodu = request.BolgeKodu,
            BolgeAdi = request.BolgeAdi,
            Aciklama = request.Aciklama,
            OlusturanKullaniciId = kullaniciId
        };

        await _uow.Bolgeler.AddAsync(bolge, ct);
        await _uow.SaveChangesAsync(ct);

        return new BolgeDto(bolge.Id, bolge.SayimPlaniId, bolge.BolgeKodu, bolge.BolgeAdi, bolge.Aciklama, false, false, null);
    }

    public async Task UpdateAsync(int id, BolgeOlusturDto request, CancellationToken ct = default)
    {
        var bolge = await _uow.Bolgeler.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Bölge bulunamadı: {id}");

        bolge.BolgeKodu = request.BolgeKodu;
        bolge.BolgeAdi = request.BolgeAdi;
        bolge.Aciklama = request.Aciklama;
        _uow.Bolgeler.Update(bolge);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var bolge = await _uow.Bolgeler.GetByIdAsync(id, ct)
         ?? throw new KeyNotFoundException($"Bölge bulunamadı: {id}");

        if (bolge.SayimOturumu != null)
            throw new InvalidOperationException("Sayım başlatılmış bölge silinemez.");

        //  Sadece taslak plandaki bölge silinebilir
        var plan = await _uow.SayimPlanlari.GetByIdAsync(bolge.SayimPlaniId, ct);
        if (plan?.Durum != SayimPlaniDurum.Taslak)
            throw new InvalidOperationException("Sadece taslak durumdaki planın bölgesi silinebilir.");

        _uow.Bolgeler.Delete(bolge);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task EkipGrubuAtaAsync(int bolgeId, EkipGrubuAtaDto request, string kullaniciId, CancellationToken ct = default)
    {
        var bolge = await _uow.Bolgeler.GetWithOturumAsync(bolgeId, ct)
            ?? throw new KeyNotFoundException($"Bölge bulunamadı: {bolgeId}");

        if (bolge.SayimOturumu != null)
            throw new InvalidOperationException("Sayım başlatılmış bölgede ekip grubu değiştirilemez.");

        if (bolge.EkipGrubu != null)
            _uow.EkipGruplari.Delete(bolge.EkipGrubu);

        var grup = new EkipGrubu
        {
            BolgeId = bolgeId,
            SayimPlaniId = bolge.SayimPlaniId,
            EkipGrubuAdi = request.EkipGrubuAdi,
            OlusturanKullaniciId = kullaniciId,
            Ekipler = request.Ekipler.Select(e => new EkipGrubuEkip
            {
                EkipId = e.EkipId,
                SiraNo = e.SiraNo,
                EkipRolu = e.EkipRolu
            }).ToList()
        };

        await _uow.EkipGruplari.AddAsync(grup, ct);
        await _uow.SaveChangesAsync(ct);
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// SayimKaydiService
// ──────────────────────────────────────────────────────────────────────────────
public class SayimKaydiService : ISayimKaydiService
{
    private readonly IUnitOfWork _uow;
    public SayimKaydiService(IUnitOfWork uow) => _uow = uow;

    public async Task<SayimKaydiDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var kaydi = await _uow.SayimKayitlari.GetWithDetaylarAsync(id, ct);
        if (kaydi == null) return null;
        var kodlar = kaydi.Detaylar.Select(d => d.MalzemeKodu).Distinct().ToList();
        var malzemeler = await _uow.Malzemeler.GetDictionaryByKodlarAsync(kodlar, ct);
        return MapToDto(kaydi, malzemeler);
    }

    public async Task<SayimKaydiDto> AcAsync(int turId, int ekipId, string kullaniciId, CancellationToken ct = default)
    {
        var tur = await _uow.SayimTurlari.GetWithKatilimcilarAsync(turId, ct)
            ?? throw new KeyNotFoundException($"Tur bulunamadı: {turId}");

        var katilimci = tur.Katilimcilar.FirstOrDefault(k => k.EkipId == ekipId)
            ?? throw new InvalidOperationException("Bu ekip bu tura katılımcı olarak atanmamış.");

        // Zaten kayıt varsa mevcut kaydı döndür (terminal yeniden bağlantı senaryosu)
        if (katilimci.SayimKaydiId.HasValue)
        {
            var mevcutKaydi = await _uow.SayimKayitlari.GetWithDetaylarAsync(katilimci.SayimKaydiId.Value, ct)
                ?? throw new KeyNotFoundException($"Mevcut kayıt bulunamadı: {katilimci.SayimKaydiId}");
            if (mevcutKaydi.Durum == SayimKaydiDurum.Tamamlandi)
                throw new InvalidOperationException("Bu ekibin sayımı zaten tamamlanmış.");
            var mevcutKodlar = mevcutKaydi.Detaylar.Select(d => d.MalzemeKodu).Distinct().ToList();
            var mevcutMalzemeler = await _uow.Malzemeler.GetDictionaryByKodlarAsync(mevcutKodlar, ct);
            return MapToDto(mevcutKaydi, mevcutMalzemeler);
        }

        var kaydi = new SayimKaydi
        {
            SayimTuruId = turId,
            EkipId = ekipId,
            EkipRolu = katilimci.EkipRolu,
            SayimYapanKullaniciId = kullaniciId,
            BaslangicTarihi = DateTime.UtcNow,
            Durum = SayimKaydiDurum.Devam,
            OlusturanKullaniciId = kullaniciId
        };

        await _uow.SayimKayitlari.AddAsync(kaydi, ct);
        // ✅ FIX: Tek SaveChanges — kaydi, katilimci ve tur güncellemesi birlikte kaydediliyor
        await _uow.SaveChangesAsync(ct);

        katilimci.SayimKaydiId = kaydi.Id;

        if (tur.Durum == SayimTuruDurum.Beklemede)
            tur.Durum = SayimTuruDurum.DevamEdiyor;

        await _uow.SaveChangesAsync(ct);

        return MapToDto(kaydi, new Dictionary<string, Malzeme>());
    }

    public async Task DetayEkleAsync(int kaydiId, SayimKaydiDetayEkleDto request, CancellationToken ct = default)
    {
        var kaydi = await _uow.SayimKayitlari.GetByIdAsync(kaydiId, ct)
            ?? throw new KeyNotFoundException($"Sayım kaydı bulunamadı: {kaydiId}");

        if (kaydi.Durum == SayimKaydiDurum.Tamamlandi)
            throw new InvalidOperationException("Tamamlanmış kayda satır eklenemez.");

        var detay = new SayimKaydiDetay
        {
            SayimKaydiId = kaydiId,
            MalzemeKodu = request.MalzemeKodu,
            LotNo = request.LotNo,
            SeriNo = request.SeriNo,
            SayilanMiktar = request.SayilanMiktar,
            Notlar = request.Notlar
        };

        // GetByIdAsync Detaylar navigation property'sini include etmediği için
        // kaydi.Detaylar.Add(detay) EF tarafından izlenmiyor — detay direkt AddAsync ile ekleniyor
        await _uow.SayimKayitlari.AddDetayAsync(detay, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<TopluDetayEkleSonucDto> TopluDetayEkleAsync(int kaydiId, IEnumerable<SayimKaydiDetayEkleDto> detaylar, CancellationToken ct = default)
    {
        var kaydi = await _uow.SayimKayitlari.GetByIdAsync(kaydiId, ct)
            ?? throw new KeyNotFoundException($"Sayım kaydı bulunamadı: {kaydiId}");

        if (kaydi.Durum == SayimKaydiDurum.Tamamlandi)
            throw new InvalidOperationException("Tamamlanmış kayda satır eklenemez.");

        var hatalar = new List<string>();
        var eklenecekler = new List<SayimKaydiDetay>();
        var satirNo = 0;

        foreach (var dto in detaylar)
        {
            satirNo++;
            if (string.IsNullOrWhiteSpace(dto.MalzemeKodu))
            {
                hatalar.Add($"Satır {satirNo}: Malzeme kodu boş olamaz.");
                continue;
            }
            if (dto.SayilanMiktar <= 0)
            {
                hatalar.Add($"Satır {satirNo}: Miktar sıfırdan büyük olmalıdır. (Malzeme: {dto.MalzemeKodu})");
                continue;
            }
            eklenecekler.Add(new SayimKaydiDetay
            {
                SayimKaydiId = kaydiId,
                MalzemeKodu = dto.MalzemeKodu.Trim().ToUpper(),
                LotNo = string.IsNullOrWhiteSpace(dto.LotNo) ? null : dto.LotNo.Trim(),
                SeriNo = string.IsNullOrWhiteSpace(dto.SeriNo) ? null : dto.SeriNo.Trim(),
                SayilanMiktar = dto.SayilanMiktar,
                Notlar = dto.Notlar
            });
        }

        if (eklenecekler.Any())
        {
            await _uow.SayimKayitlari.AddDetayRangeAsync(eklenecekler, ct);
            await _uow.SaveChangesAsync(ct);
        }

        return new TopluDetayEkleSonucDto(
            KaydiId: kaydiId,
            EklenenSatir: eklenecekler.Count,
            HataliSatir: hatalar.Count,
            Hatalar: hatalar
        );
    }

    public async Task DetayGuncelleAsync(int detayId, SayimKaydiDetayEkleDto request, CancellationToken ct = default)
    {
        var detay = await _uow.SayimKayitlari.Query()
            .SelectMany(k => k.Detaylar)
            .FirstOrDefaultAsync(d => d.Id == detayId, ct)
            ?? throw new KeyNotFoundException($"Detay bulunamadı: {detayId}");

        detay.MalzemeKodu = request.MalzemeKodu;
        detay.LotNo = request.LotNo;
        detay.SeriNo = request.SeriNo;
        detay.SayilanMiktar = request.SayilanMiktar;
        detay.Notlar = request.Notlar;
        detay.GuncellemeTarihi = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);
    }

    public async Task DetaySilAsync(int detayId, CancellationToken ct = default)
    {
        var detay = await _uow.SayimKayitlari.Query()
            .SelectMany(k => k.Detaylar)
            .FirstOrDefaultAsync(d => d.Id == detayId, ct)
            ?? throw new KeyNotFoundException($"Detay bulunamadı: {detayId}");

        // ✅ FIX: detay silinip sonra SaveChanges yapılıyor, önce silme yoktu

        _uow.SayimKayitlari.DeleteDetay(detay);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task TamamlaAsync(int kaydiId, string kullaniciId, CancellationToken ct = default)
    {
        var kaydi = await _uow.SayimKayitlari.GetByIdAsync(kaydiId, ct)
            ?? throw new KeyNotFoundException($"Sayım kaydı bulunamadı: {kaydiId}");

        kaydi.Durum = SayimKaydiDurum.Tamamlandi;
        kaydi.TamamlanmaTarihi = DateTime.UtcNow;
        _uow.SayimKayitlari.Update(kaydi);
        await _uow.SaveChangesAsync(ct);

        // Tüm katılımcılar tamamladıysa turu KarsilastirmaBekliyor'a çek
        // Bu sayede diğer bölgeleri beklemeden karşılaştırma yapılabilir
        var tur = await _uow.SayimTurlari.GetWithKatilimcilarAsync(kaydi.SayimTuruId, ct);
        if (tur == null) return;

        var tumKayitlar = await _uow.SayimKayitlari.GetByTurIdAsync(tur.Id, ct);
        var hepsiTamamlandi = tur.Katilimcilar.All(k =>
            k.SayimKaydiId.HasValue &&
            tumKayitlar.Any(r => r.Id == k.SayimKaydiId && r.Durum == SayimKaydiDurum.Tamamlandi));

        if (hepsiTamamlandi && tur.Durum == SayimTuruDurum.DevamEdiyor)
        {
            tur.Durum = SayimTuruDurum.KarsilastirmaBekliyor;
            await _uow.SaveChangesAsync(ct);
        }
    }

    private static SayimKaydiDto MapToDto(SayimKaydi k, Dictionary<string, Malzeme> malzemeler) => new(
        Id: k.Id,
        SayimTuruId: k.SayimTuruId,
        EkipId: k.EkipId,
        EkipAdi: k.Ekip?.EkipAdi ?? string.Empty,
        EkipRolu: k.EkipRolu,
        SayimYapanAdSoyad: k.SayimYapanKullanici?.AdSoyad ?? string.Empty,
        BaslangicTarihi: k.BaslangicTarihi,
        TamamlanmaTarihi: k.TamamlanmaTarihi,
        Durum: k.Durum,
        DurumAdi: k.Durum.ToString(),
        Notlar: k.Notlar,
        Detaylar: k.Detaylar.Select(d => new SayimKaydiDetayDto(
            Id: d.Id,
            MalzemeKodu: d.MalzemeKodu,
            MalzemeAdi: malzemeler.TryGetValue(d.MalzemeKodu, out var m) ? m.MalzemeAdi : d.MalzemeKodu,
            LotNo: d.LotNo,
            SeriNo: d.SeriNo,
            SayilanMiktar: d.SayilanMiktar,
            OlcuBirimi: malzemeler.TryGetValue(d.MalzemeKodu, out var mb) ? mb.OlcuBirimi : string.Empty,
            Notlar: d.Notlar
        ))
    );

    public async Task<IEnumerable<AcikSayimKaydiDto>> GetAcikKayitlarByPlanIdAsync(int planId, CancellationToken ct = default)
    {
        // Oturumları AktifTurNo ile birlikte çek
        var oturumlar = await _uow.SayimOturumlari.Query()
            .Include(o => o.Bolge)
            .Include(o => o.SayimTurlari)
                .ThenInclude(t => t.Katilimcilar)
                    .ThenInclude(k => k.Ekip)
                        .ThenInclude(e => e.EkipKullanicilari)
                            .ThenInclude(ek => ek.Kullanici)
            .Include(o => o.SayimTurlari)
                .ThenInclude(t => t.SayimKayitlari)
                    .ThenInclude(k => k.SayimYapanKullanici)
            .Include(o => o.SayimTurlari)
                .ThenInclude(t => t.SayimKayitlari)
                    .ThenInclude(k => k.Ekip)
            .Where(o => o.SayimPlaniId == planId)
            .ToListAsync(ct);

        var sonuc = new List<AcikSayimKaydiDto>();

        foreach (var oturum in oturumlar)
        {
            // Onaylanmış oturumları atla
            if (oturum.Durum == SayimOturumuDurum.Onaylandi) continue;

            // Aktif turdaki katılımcıları listele — kayıt olsun olmasın
            var aktifTur = oturum.SayimTurlari
                .FirstOrDefault(t => t.TurNo == oturum.AktifTurNo);
            if (aktifTur == null) continue;

            foreach (var katilimci in aktifTur.Katilimcilar)
            {
                var kullaniciAdlari = katilimci.Ekip?.EkipKullanicilari
                    .Select(ek => ek.Kullanici?.AdSoyad)
                    .Where(ad => !string.IsNullOrWhiteSpace(ad))
                    .ToList() ?? [];

                sonuc.Add(new AcikSayimKaydiDto(
                    KatilimciId: katilimci.Id,
                    EkipId: katilimci.EkipId,
                    SayimTuruId: aktifTur.Id,
                    EkipAdi: katilimci.Ekip?.EkipAdi ?? "",
                    EkipRoluAdi: katilimci.EkipRolu.ToString(),
                    BolgeAdi: oturum.Bolge?.BolgeAdi ?? "",
                    TurNo: aktifTur.TurNo,
                    KullaniciAdlari: kullaniciAdlari.Any() ? string.Join(", ", kullaniciAdlari) : ""
                ));
            }
        }

        return sonuc;
    }

    public async Task<OfflineImportSonucDto> OfflineImportAsync(int katilimciId, Stream dosya, string dosyaAdi, string kullaniciId, bool tamamla = false, CancellationToken ct = default)
    {
        // Katılımcıyı bul
        var katilimci = await _uow.SayimTurlari.Query()
            .Include(t => t.Katilimcilar)
            .SelectMany(t => t.Katilimcilar)
            .FirstOrDefaultAsync(k => k.Id == katilimciId, ct)
            ?? throw new KeyNotFoundException($"Katılımcı bulunamadı: {katilimciId}");

        var turId = katilimci.SayimTuruId;

        // Mevcut kaydı bul veya yeni oluştur
        var kaydi = await _uow.SayimKayitlari.Query()
            .FirstOrDefaultAsync(k => k.SayimTuruId == katilimci.SayimTuruId
                && k.EkipId == katilimci.EkipId, ct);

        if (kaydi == null)
        {
            kaydi = new SayimKaydi
            {
                SayimTuruId = katilimci.SayimTuruId,
                EkipId = katilimci.EkipId,
                EkipRolu = katilimci.EkipRolu,
                SayimYapanKullaniciId = kullaniciId,
                BaslangicTarihi = DateTime.UtcNow,
                Durum = SayimKaydiDurum.Devam,
                OlusturanKullaniciId = kullaniciId
            };
            await _uow.SayimKayitlari.AddAsync(kaydi, ct);
            await _uow.SaveChangesAsync(ct);

            // Katılımcıya kayıt bağla
            katilimci.SayimKaydiId = kaydi.Id;
            await _uow.SaveChangesAsync(ct);
        }

        var kaydiId = kaydi.Id;

        using var wb = new ClosedXML.Excel.XLWorkbook(dosya);
        var ws = wb.Worksheets.Any(s => s.Name == "OfflineKayit")
            ? wb.Worksheet("OfflineKayit")
            : wb.Worksheets.First();

        var satirlar = ws.RangeUsed()?.RowsUsed().Skip(3).ToList() ?? [];
        var hatalar = new List<string>();
        var eklenecekler = new List<SayimKaydiDetayEkleDto>();
        int hatali = 0;

        foreach (var satir in satirlar)
        {
            var kod = satir.Cell(1).GetString().Trim().ToUpper();
            var miktarStr = satir.Cell(2).GetString().Trim();
            var lotNo = satir.Cell(3).GetString().Trim();
            var seriNo = satir.Cell(4).GetString().Trim();

            if (string.IsNullOrWhiteSpace(kod)) { hatalar.Add($"Satır {satir.RowNumber()}: Malzeme kodu boş."); hatali++; continue; }
            if (!decimal.TryParse(miktarStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var miktar) || miktar < 0)
            { hatalar.Add($"Satır {satir.RowNumber()}: Geçersiz miktar '{miktarStr}'."); hatali++; continue; }

            eklenecekler.Add(new SayimKaydiDetayEkleDto(
                MalzemeKodu: kod,
                LotNo: string.IsNullOrWhiteSpace(lotNo) ? null : lotNo,
                SeriNo: string.IsNullOrWhiteSpace(seriNo) ? null : seriNo,
                SayilanMiktar: miktar,
                Notlar: null
            ));
        }

        if (eklenecekler.Any())
        {
            var sonuc = await TopluDetayEkleAsync(kaydiId, eklenecekler, ct);

            var karsilastirmaTetiklendi = false;
            if (tamamla)
            {
                var kaydiBitir = await _uow.SayimKayitlari.GetByIdAsync(kaydiId, ct);
                if (kaydiBitir != null)
                {
                    kaydiBitir.Durum = SayimKaydiDurum.Tamamlandi;
                    kaydiBitir.TamamlanmaTarihi = DateTime.UtcNow;
                    await _uow.SaveChangesAsync(ct);
                }

                // Turdaki katılımcı sayısı kadar tamamlanmış kayıt varsa karşılaştırma tetikle
                karsilastirmaTetiklendi = await KarsilastirmaHazirMiAsync(turId, ct);
            }

            return new OfflineImportSonucDto(
                Basarili: hatali == 0 && sonuc.HataliSatir == 0,
                KaydiId: kaydiId,
                TurId: turId,
                EklenenSatir: sonuc.EklenenSatir,
                HataliSatir: hatali + sonuc.HataliSatir,
                Hatalar: hatalar.Concat(sonuc.Hatalar).ToList(),
                KarsilastirmaTetiklendi: karsilastirmaTetiklendi
            );
        }

        var karsilastirmaTetiklendi2 = false;
        if (tamamla)
        {
            var kaydiBitir = await _uow.SayimKayitlari.GetByIdAsync(kaydiId, ct);
            if (kaydiBitir != null)
            {
                kaydiBitir.Durum = SayimKaydiDurum.Tamamlandi;
                kaydiBitir.TamamlanmaTarihi = DateTime.UtcNow;
                await _uow.SaveChangesAsync(ct);
            }

            karsilastirmaTetiklendi2 = await KarsilastirmaHazirMiAsync(turId, ct);
        }

        return new OfflineImportSonucDto(
            Basarili: hatali == 0,
            KaydiId: kaydiId,
            TurId: turId,
            EklenenSatir: 0,
            HataliSatir: hatali,
            Hatalar: hatalar,
            KarsilastirmaTetiklendi: karsilastirmaTetiklendi2
        );
    }

    private async Task<bool> KarsilastirmaHazirMiAsync(int turId, CancellationToken ct)
    {
        var tur = await _uow.SayimTurlari.Query()
            .Include(t => t.Katilimcilar)
            .Include(t => t.SayimKayitlari)
            .FirstOrDefaultAsync(t => t.Id == turId, ct);
        if (tur == null) return false;

        var katilimciSayisi = tur.Katilimcilar.Count;
        var tamamlananKayit = tur.SayimKayitlari.Count(k => k.Durum == SayimKaydiDurum.Tamamlandi);

        // EkipKarsilastirma: tüm katılımcıların kaydı tamamlanmalı
        // EkipKontrol: tek ekip sayar, 1 tamamlanmış kayıt yeterli
        return tur.TurTipi == SayimTuruTip.EkipKarsilastirma
            ? tamamlananKayit >= katilimciSayisi
            : tamamlananKayit >= 1;
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// KullaniciService
// ──────────────────────────────────────────────────────────────────────────────
public class KullaniciService : IKullaniciService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _uow;
    public KullaniciService(UserManager<ApplicationUser> userManager, IUnitOfWork uow)
    {
        _userManager = userManager;
        _uow = uow;
    }

    public async Task<IEnumerable<KullaniciDto>> GetAllAsync(CancellationToken ct = default)
    {
        var kullanicilar = _userManager.Users.ToList();
        var result = new List<KullaniciDto>();
        foreach (var u in kullanicilar)
        {
            var roller = await _userManager.GetRolesAsync(u);
            var ekip = await _uow.Ekipler.GetByKullaniciIdAsync(u.Id, ct);
            result.Add(new KullaniciDto(u.Id, u.AdSoyad, u.Email!, u.AktifMi, roller, ekip?.Id, ekip?.EkipAdi));
        }
        return result;
    }

    public async Task<KullaniciDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var u = await _userManager.FindByIdAsync(id);
        if (u == null) return null;
        var roller = await _userManager.GetRolesAsync(u);
        var ekip = await _uow.Ekipler.GetByKullaniciIdAsync(u.Id, ct);
        return new KullaniciDto(u.Id, u.AdSoyad, u.Email!, u.AktifMi, roller, ekip?.Id, ekip?.EkipAdi);
    }

    public async Task<KullaniciDto> CreateAsync(KullaniciOlusturDto request, CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            AdSoyad = request.AdSoyad,
            AktifMi = true,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Sifre);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, request.Rol);
        return new KullaniciDto(user.Id, user.AdSoyad, user.Email!, true, [request.Rol], null, null);
    }

    public async Task UpdateAsync(string id, KullaniciGuncelleDto request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id)
            ?? throw new KeyNotFoundException($"Kullanıcı bulunamadı: {id}");

        user.AdSoyad = request.AdSoyad;
        user.Email = request.Email;
        user.UserName = request.Email;

        await _userManager.UpdateAsync(user);

        if (!string.IsNullOrEmpty(request.YeniSifre))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, token, request.YeniSifre);
        }

        var mevcutRoller = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, mevcutRoller);
        await _userManager.AddToRoleAsync(user, request.Rol);
    }

    public async Task SetAktifAsync(string id, bool aktif, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id)
            ?? throw new KeyNotFoundException($"Kullanıcı bulunamadı: {id}");
        user.AktifMi = aktif;
        await _userManager.UpdateAsync(user);
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// RaporService
// ──────────────────────────────────────────────────────────────────────────────
public class RaporService : IRaporService
{
    private readonly IUnitOfWork _uow;
    public RaporService(IUnitOfWork uow) => _uow = uow;

    public async Task<SayimDurumRaporDto> GetSayimDurumRaporuAsync(int planId, CancellationToken ct = default)
    {
        var plan = await _uow.SayimPlanlari.GetWithDetailsAsync(planId, ct)
            ?? throw new KeyNotFoundException($"Plan bulunamadı: {planId}");

        var oturumlar = await _uow.SayimOturumlari.GetByPlanIdAsync(planId, ct);
        var oturumList = oturumlar.ToList();

        var bolgeDurumlari = plan.Bolgeler.Select(b =>
        {
            var oturum = oturumList.FirstOrDefault(o => o.BolgeId == b.Id);
            return new BolgeDurumDto(
                BolgeId: b.Id,
                BolgeKodu: b.BolgeKodu,
                BolgeAdi: b.BolgeAdi,
                OturumDurum: oturum?.Durum.ToString() ?? "Başlamadı",
                TamamlananTurSayisi: oturum?.SayimTurlari.Count(t => t.Durum == SayimTuruDurum.Onaylandi) ?? 0,
                ToplamTurSayisi: oturum?.SayimTurlari.Count ?? 0,
                ErpKarsilastirmaYapildiMi: oturum?.SayimTurlari.Any(t => t.TurTipi == SayimTuruTip.ErpKarsilastirma) ?? false
            );
        }).ToList();

        return new SayimDurumRaporDto(
            PlanId: planId,
            PlanAdi: plan.PlanAdi,
            PlanDurum: plan.Durum,
            ToplamBolge: plan.Bolgeler.Count,
            TamamlananBolge: oturumList.Count(o => o.Durum == SayimOturumuDurum.Onaylandi || o.Durum == SayimOturumuDurum.ManuelKarar),
            DevamEdenBolge: oturumList.Count(o => o.Durum == SayimOturumuDurum.DevamEdiyor),
            BekleyenBolge: plan.Bolgeler.Count - oturumList.Count,
            BolgeDurumlari: bolgeDurumlari,
            EkipSayimOzetleri: []
        );
    }

    public async Task<KesinFarkRaporDto> GetKesinFarkRaporuAsync(int planId, CancellationToken ct = default)
    {
        var plan = await _uow.SayimPlanlari.GetWithDetailsAsync(planId, ct)
            ?? throw new KeyNotFoundException($"Plan bulunamadı: {planId}");

        var oturumlar = (await _uow.SayimOturumlari.GetByPlanIdAsync(planId, ct)).ToList();
        var erpStoklar = (await _uow.ErpStoklar.GetByPlanIdAsync(planId, ct)).ToList();

        // Malzeme kodlarını önceden yükle
        var tumKodlar = oturumlar
            .SelectMany(o => o.SayimTurlari)
            .Where(t => t.TurSonucu != null)
            .SelectMany(t => t.TurSonucu!.Detaylar)
            .Select(d => d.MalzemeKodu).Distinct().ToList();
        var malzemeSozlugu = await _uow.Malzemeler.GetDictionaryByKodlarAsync(tumKodlar);

        var farkDetaylari = new List<FarkDetayDto>();

        foreach (var oturum in oturumlar)
        {
            var erpTuru = oturum.SayimTurlari
                .Where(t => t.TurTipi == SayimTuruTip.ErpKarsilastirma || t.TurTipi == SayimTuruTip.ErpKontrol)
                .OrderByDescending(t => t.TurNo)
                .FirstOrDefault(t => t.TurSonucu != null);

            if (erpTuru?.TurSonucu == null) continue;

            foreach (var detay in erpTuru.TurSonucu.Detaylar.Where(d => d.Durum == TurSonucuDetayDurum.FarkVar))
            {
                var erpDepoKodlari = erpStoklar
                    .Where(e => e.MalzemeKodu == detay.MalzemeKodu &&
                                (e.LotNo == detay.LotNo || (string.IsNullOrEmpty(e.LotNo) && string.IsNullOrEmpty(detay.LotNo))))
                    .Select(e => e.DepoKodu)
                    .Distinct()
                    .ToList();
                malzemeSozlugu.TryGetValue(detay.MalzemeKodu, out var malzeme);

                farkDetaylari.Add(new FarkDetayDto(
                    MalzemeKodu: detay.MalzemeKodu,
                    MalzemeAdi: malzeme?.MalzemeAdi ?? detay.MalzemeKodu,
                    LotNo: detay.LotNo,
                    SeriNo: detay.SeriNo,
                    Birim: malzeme?.OlcuBirimi ?? string.Empty,
                    DepoKodu: string.Join(", ", erpDepoKodlari),
                    ErpMiktar: detay.Deger1 ?? 0,
                    FiiliMiktar: detay.Deger2 ?? 0,
                    Fark: detay.Fark ?? 0,
                    FarkYuzdesi: detay.FarkYuzdesi ?? 0,
                    KararTipi: detay.KararTipi,
                    ManuelKararGerekce: detay.ManuelKarar?.Gerekce,
                    BolgeAdi: oturum.Bolge?.BolgeAdi ?? string.Empty
                ));
            }
        }

        var durumRaporu = await GetSayimDurumRaporuAsync(planId, ct);

        return new KesinFarkRaporDto(
            PlanId: planId,
            PlanAdi: plan.PlanAdi,
            RaporTarihi: DateTime.UtcNow,
            ToplamMalzeme: erpStoklar.Count,
            FarksizMalzeme: erpStoklar.Count - farkDetaylari.Count,
            FarkliMalzeme: farkDetaylari.Count,
            BolgeDurumlari: durumRaporu.BolgeDurumlari,
            EkipSayimOzetleri: durumRaporu.EkipSayimOzetleri,
            FarkDetaylari: farkDetaylari
        );
    }

    public async Task<byte[]> ExportKesinFarkRaporuExcelAsync(int planId, CancellationToken ct = default)
    {
        var rapor = await GetKesinFarkRaporuAsync(planId, ct);

        using var wb = new XLWorkbook();

        var wsOzet = wb.Worksheets.Add("Özet");
        wsOzet.Cell(1, 1).Value = "Plan";
        wsOzet.Cell(1, 2).Value = rapor.PlanAdi;
        wsOzet.Cell(2, 1).Value = "Rapor Tarihi";
        wsOzet.Cell(2, 2).Value = rapor.RaporTarihi.ToString("dd.MM.yyyy HH:mm");
        wsOzet.Cell(3, 1).Value = "Toplam Malzeme";
        wsOzet.Cell(3, 2).Value = rapor.ToplamMalzeme;
        wsOzet.Cell(4, 1).Value = "Farksız";
        wsOzet.Cell(4, 2).Value = rapor.FarksizMalzeme;
        wsOzet.Cell(5, 1).Value = "Farklı";
        wsOzet.Cell(5, 2).Value = rapor.FarkliMalzeme;

        var wsBolge = wb.Worksheets.Add("Bölge Durumları");
        wsBolge.Cell(1, 1).Value = "Bölge Kodu";
        wsBolge.Cell(1, 2).Value = "Bölge Adı";
        wsBolge.Cell(1, 3).Value = "Durum";
        wsBolge.Cell(1, 4).Value = "ERP Karşılaştırma";
        var satirNo = 2;
        foreach (var b in rapor.BolgeDurumlari)
        {
            wsBolge.Cell(satirNo, 1).Value = b.BolgeKodu;
            wsBolge.Cell(satirNo, 2).Value = b.BolgeAdi;
            wsBolge.Cell(satirNo, 3).Value = b.OturumDurum;
            wsBolge.Cell(satirNo, 4).Value = b.ErpKarsilastirmaYapildiMi ? "Evet" : "Hayır";
            satirNo++;
        }

        var wsFark = wb.Worksheets.Add("Fark Detayları");
        string[] basliklar = ["Malzeme Kodu", "Malzeme Adı", "Lot No", "Seri No", "Birim", "Depo Kodu", "Bölge", "ERP Miktar", "Fiili Miktar", "Fark", "Fark %", "Karar Tipi", "Manuel Karar Gerekçesi"];
        for (int i = 0; i < basliklar.Length; i++)
            wsFark.Cell(1, i + 1).Value = basliklar[i];

        satirNo = 2;
        foreach (var f in rapor.FarkDetaylari)
        {
            wsFark.Cell(satirNo, 1).Value = f.MalzemeKodu;
            wsFark.Cell(satirNo, 2).Value = f.MalzemeAdi;
            wsFark.Cell(satirNo, 3).Value = f.LotNo ?? "";
            wsFark.Cell(satirNo, 4).Value = f.SeriNo ?? "";
            wsFark.Cell(satirNo, 5).Value = f.Birim;
            wsFark.Cell(satirNo, 6).Value = f.DepoKodu;
            wsFark.Cell(satirNo, 7).Value = f.BolgeAdi;
            wsFark.Cell(satirNo, 8).Value = f.ErpMiktar;
            wsFark.Cell(satirNo, 9).Value = f.FiiliMiktar;
            wsFark.Cell(satirNo, 10).Value = f.Fark;
            wsFark.Cell(satirNo, 11).Value = Math.Round(f.FarkYuzdesi, 2);
            wsFark.Cell(satirNo, 12).Value = f.KararTipi?.ToString() ?? "Otomatik";
            wsFark.Cell(satirNo, 13).Value = f.ManuelKararGerekce ?? "";
            satirNo++;
        }

        wsFark.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}

internal static class StringExtensions
{
    public static string? NullIfEmpty(this string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}