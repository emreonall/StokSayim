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

        // --- SAYIM SONUÇLARINI topla: sadece malzeme kodu bazında ---
        // key: MalzemeKodu → (FiiliMiktar, BolgeAdi, KararTipi, Gerekce)
        var sayimSonuclari = new Dictionary<string, (decimal Miktar, string BolgeAdi, KararTipi? Karar, string? Gerekce)>();

        foreach (var oturum in oturumlar)
        {
            var erpTuru = oturum.SayimTurlari
                .Where(t => t.TurTipi == SayimTuruTip.ErpKarsilastirma || t.TurTipi == SayimTuruTip.ErpKontrol)
                .OrderByDescending(t => t.TurNo)
                .FirstOrDefault(t => t.TurSonucu != null);

            if (erpTuru?.TurSonucu == null) continue;

            foreach (var detay in erpTuru.TurSonucu.Detaylar)
            {
                var fiili = detay.Deger2 ?? 0;
                if (sayimSonuclari.TryGetValue(detay.MalzemeKodu, out var mevcut))
                    sayimSonuclari[detay.MalzemeKodu] = (mevcut.Miktar + fiili, mevcut.BolgeAdi, detay.KararTipi, detay.ManuelKarar?.Gerekce);
                else
                    sayimSonuclari[detay.MalzemeKodu] = (fiili, oturum.Bolge?.BolgeAdi ?? string.Empty, detay.KararTipi, detay.ManuelKarar?.Gerekce);
            }
        }

        // --- ERP KONTROL SAYIM sonuçlarını uygula (varsa fiili değerin üzerine yazar) ---
        var erpKontrolOturumu = await _uow.ErpKontrolOturumlari.GetByPlanIdAsync(planId, ct);
        if (erpKontrolOturumu != null)
        {
            // Tamamlanmış ekiplerin sayım değerlerini malzeme bazında topla
            var kontrolSonuclari = erpKontrolOturumu.Ekipler
                .Where(e => e.Durum == ErpKontrolEkipDurum.Tamamlandi)
                .SelectMany(e => e.Malzemeler)
                .Where(m => m.SayilanMiktar.HasValue)
                .GroupBy(m => m.MalzemeKodu)
                .ToDictionary(g => g.Key, g => g.Sum(m => m.SayilanMiktar!.Value));

            foreach (var kvp in kontrolSonuclari)
            {
                // ERP kontrol değeri geçerli sayım değeri olarak üst yazar
                sayimSonuclari[kvp.Key] = (kvp.Value, "ERP Kontrol", null, null);
            }
        }

        // --- ERP STOKLARI topla: sadece malzeme kodu bazında ---
        // key: MalzemeKodu → ToplamMiktar
        var erpOzet = erpStoklar
            .GroupBy(e => e.MalzemeKodu)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(e => e.Miktar)
            );

        // --- Malzeme bilgileri ---
        var tumKodlar = erpOzet.Keys
            .Union(sayimSonuclari.Keys)
            .Distinct().ToList();
        var malzemeSozlugu = await _uow.Malzemeler.GetDictionaryByKodlarAsync(tumKodlar);

        var satirlar = new List<FarkDetayDto>();

        // --- 1. ERP'deki tüm malzemeleri işle ---
        foreach (var kvp in erpOzet)
        {
            var malzemeKodu = kvp.Key;
            var erpMiktar = kvp.Value;

            sayimSonuclari.TryGetValue(malzemeKodu, out var sayim);
            var fiiliMiktar = sayim.Miktar;
            var fark = fiiliMiktar - erpMiktar;
            var farkYuzdesi = erpMiktar != 0 ? Math.Abs(fark / erpMiktar * 100) : (fiiliMiktar != 0 ? 100m : 0m);

            malzemeSozlugu.TryGetValue(malzemeKodu, out var malzeme);

            satirlar.Add(new FarkDetayDto(
                MalzemeKodu: malzemeKodu,
                MalzemeAdi: malzeme?.MalzemeAdi ?? malzemeKodu,
                Birim: malzeme?.OlcuBirimi ?? string.Empty,
                ErpMiktar: erpMiktar,
                FiiliMiktar: fiiliMiktar,
                Fark: fark,
                FarkYuzdesi: Math.Round(farkYuzdesi, 2),
                KararAdi: sayim.Karar == Domain.Enums.KararTipi.Manuel ? "Manuel" : fark == 0 ? "Eşleşti" : "Fark Var",
                KararTipi: sayim.Karar,
                ManuelKararGerekce: sayim.Gerekce
            ));
        }

        // --- 2. Sadece sayımda olan malzemeleri ekle (ERP'de yok) ---
        foreach (var kvp in sayimSonuclari)
        {
            var malzemeKodu = kvp.Key;
            if (erpOzet.ContainsKey(malzemeKodu)) continue;

            var fiiliMiktar = kvp.Value.Miktar;
            malzemeSozlugu.TryGetValue(malzemeKodu, out var malzeme);

            satirlar.Add(new FarkDetayDto(
                MalzemeKodu: malzemeKodu,
                MalzemeAdi: malzeme?.MalzemeAdi ?? malzemeKodu,
                Birim: malzeme?.OlcuBirimi ?? string.Empty,
                ErpMiktar: 0,
                FiiliMiktar: fiiliMiktar,
                Fark: fiiliMiktar,
                FarkYuzdesi: 100,
                KararAdi: kvp.Value.Karar == Domain.Enums.KararTipi.Manuel ? "Manuel" : "Fark Var",
                KararTipi: kvp.Value.Karar,
                ManuelKararGerekce: kvp.Value.Gerekce
            ));
        }

        var siraliSatirlar = satirlar.OrderBy(s => s.MalzemeKodu).ToList();
        var farksiz = siraliSatirlar.Count(s => s.Fark == 0);
        var farkli = siraliSatirlar.Count(s => s.Fark != 0);

        var durumRaporu = await GetSayimDurumRaporuAsync(planId, ct);

        return new KesinFarkRaporDto(
            PlanId: planId,
            PlanAdi: plan.PlanAdi,
            RaporTarihi: DateTime.UtcNow,
            ToplamSatir: siraliSatirlar.Count,
            FarksizSatir: farksiz,
            FarkliSatir: farkli,
            BolgeDurumlari: durumRaporu.BolgeDurumlari,
            EkipSayimOzetleri: durumRaporu.EkipSayimOzetleri,
            FarkDetaylari: siraliSatirlar
        );
    }

}

internal static class StringExtensions
{
    public static string? NullIfEmpty(this string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}