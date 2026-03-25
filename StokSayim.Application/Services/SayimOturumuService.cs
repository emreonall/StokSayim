using StokSayim.Application.DTOs.Bolge;
using StokSayim.Application.DTOs.SayimOturumu;
using StokSayim.Application.Interfaces;
using StokSayim.Application.Interfaces.Services;
using StokSayim.Domain.Entities;
using StokSayim.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace StokSayim.Application.Services;

public class SayimOturumuService : ISayimOturumuService
{
    private readonly IUnitOfWork _uow;

    public SayimOturumuService(IUnitOfWork uow) => _uow = uow;

    public async Task<SayimOturumuDetayDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var oturum = await _uow.SayimOturumlari.GetWithTurlerAsync(id, ct);
        return oturum == null ? null : MapToDto(oturum);
    }

    public async Task<SayimOturumuDetayDto?> GetByBolgeIdAsync(int bolgeId, CancellationToken ct = default)
    {
        var oturum = await _uow.SayimOturumlari.GetByBolgeIdAsync(bolgeId, ct);
        return oturum == null ? null : MapToDto(oturum);
    }

    public async Task BaslatAsync(int bolgeId, string kullaniciId, CancellationToken ct = default)
    {
        var mevcutOturum = await _uow.SayimOturumlari.GetByBolgeIdAsync(bolgeId, ct);
        if (mevcutOturum != null)
            throw new InvalidOperationException("Bu bölge için zaten sayım oturumu açık.");

        var bolge = await _uow.Bolgeler.GetWithOturumAsync(bolgeId, ct)
            ?? throw new KeyNotFoundException($"Bölge bulunamadı: {bolgeId}");

        if (bolge.EkipGrubu == null || !bolge.EkipGrubu.Ekipler.Any())
            throw new InvalidOperationException("Bölgeye ekip grubu atanmadan sayım başlatılamaz.");

        await _uow.BeginTransactionAsync(ct);
        try
        {
            var oturum = new SayimOturumu
            {
                BolgeId = bolgeId,
                SayimPlaniId = bolge.SayimPlaniId,
                Durum = SayimOturumuDurum.DevamEdiyor,
                AktifTurNo = 1,
                BaslangicTarihi = DateTime.UtcNow,
                SorumluKullaniciId = kullaniciId,
                OlusturanKullaniciId = kullaniciId
            };

            await _uow.SayimOturumlari.AddAsync(oturum, ct);
            await _uow.SaveChangesAsync(ct);

            // İlk turu aç
            var tur = await YeniTurAcAsync(oturum.Id, 1, SayimTuruTip.EkipKarsilastirma, kullaniciId, ct);

            // EkipGrubu'ndaki Birinci ve Ikinci ekipleri katılımcı olarak ekle
            var birinciveIkinciler = bolge.EkipGrubu.Ekipler
                .Where(e => e.EkipRolu == EkipRolu.Birinci || e.EkipRolu == EkipRolu.Ikinci)
                .ToList();

            foreach (var grubuEkip in birinciveIkinciler)
            {
                var katilimci = new SayimTuruKatilimci
                {
                    SayimTuruId = tur.Id,
                    EkipId = grubuEkip.EkipId,
                    EkipRolu = grubuEkip.EkipRolu,
                    OlusturanKullaniciId = kullaniciId
                };
                //  katilimci tur'a ekleniyor, tur tekrar Add edilmiyordu
                tur.Katilimcilar.Add(katilimci);
            }

            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<IEnumerable<GorevBildirimDto>> GetBekleyenBildirimlerAsync(CancellationToken ct = default)
    {
        var bildirimler = await _uow.GorevBildirimleri.GetBekleyenlerAsync(ct);
        return bildirimler.Select(b => new GorevBildirimDto(
            Id: b.Id,
            SayimOturumuId: b.SayimOturumuId,
            BolgeId: b.SayimOturumu?.BolgeId ?? 0,
            BolgeAdi: b.SayimOturumu?.Bolge?.BolgeAdi ?? string.Empty,
            SayimTuruId: b.SayimTuruId,
            TurNo: b.SayimTuru?.TurNo,
            BildirimTipi: b.BildirimTipi,
            BildirimTipiAdi: b.BildirimTipi.ToString(),
            Mesaj: b.Mesaj,
            OlusturmaTarihi: b.OlusturmaTarihi
        ));
    }

    public async Task KontrolTuruAcAsync(int oturumuId, KontrolTuruAcDto request, string kullaniciId, CancellationToken ct = default)
    {
        var oturum = await _uow.SayimOturumlari.GetWithTurlerAsync(oturumuId, ct)
            ?? throw new KeyNotFoundException($"Oturum bulunamadı: {oturumuId}");

        var sonTur = oturum.SayimTurlari.OrderByDescending(t => t.TurNo).First();
        if (sonTur.Durum != SayimTuruDurum.FarkVar)
            throw new InvalidOperationException("Kontrol turu sadece fark olan turdan sonra açılabilir.");

        await _uow.BeginTransactionAsync(ct);
        try
        {
            oturum.AktifTurNo = sonTur.TurNo + 1;

            var turTipi = sonTur.TurTipi == SayimTuruTip.ErpKarsilastirma || sonTur.TurTipi == SayimTuruTip.ErpKontrol
                ? SayimTuruTip.ErpKontrol
                : SayimTuruTip.EkipKontrol;

            var yeniTur = await YeniTurAcAsync(oturumuId, oturum.AktifTurNo, turTipi, kullaniciId, ct);
            yeniTur.Notlar = request.Notlar;

            foreach (var ekipDto in request.Ekipler)
            {
                var katilimci = new SayimTuruKatilimci
                {
                    SayimTuruId = yeniTur.Id,
                    EkipId = ekipDto.EkipId,
                    EkipRolu = ekipDto.EkipRolu,
                    OlusturanKullaniciId = kullaniciId
                };
                // ✅ FIX: katilimci yeniTur'a ekleniyor, önce hiç eklenmiyordu
                yeniTur.Katilimcilar.Add(katilimci);
            }

            // Bildirimi işle
            var bekleyenBildirim = await _uow.GorevBildirimleri
                .FirstOrDefaultAsync(b => b.SayimOturumuId == oturumuId && b.Durum == GorevBildirimDurum.Beklemede, ct);

            if (bekleyenBildirim != null)
            {
                bekleyenBildirim.Durum = GorevBildirimDurum.Islendi;
                bekleyenBildirim.IslemTarihi = DateTime.UtcNow;
                bekleyenBildirim.IsleyenKullaniciId = kullaniciId;
            }

            _uow.SayimOturumlari.Update(oturum);
            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<TurSonucuDto?> GetTurSonucuAsync(int turId, CancellationToken ct = default)
    {
        var sonuc = await _uow.TurSonuclari.GetByTurIdAsync(turId, ct);
        if (sonuc == null) return null;

        var kodlar = sonuc.Detaylar.Select(d => d.MalzemeKodu).Distinct().ToList();
        var malzemeler = await _uow.Malzemeler.GetDictionaryByKodlarAsync(kodlar, ct);

        return new TurSonucuDto(
            Id: sonuc.Id,
            SayimTuruId: sonuc.SayimTuruId,
            TurNo: sonuc.SayimTuru?.TurNo ?? 0,
            TurTipi: sonuc.SayimTuru?.TurTipi.ToString() ?? string.Empty,
            ToplamMalzemeSayisi: sonuc.ToplamMalzemeSayisi,
            EslesilenSayisi: sonuc.EslesilenSayisi,
            FarkliSayisi: sonuc.FarkliSayisi,
            GenelDurum: sonuc.GenelDurum.ToString(),
            HesaplamaTarihi: sonuc.HesaplamaTarihi,
            Detaylar: sonuc.Detaylar.Select(d => new TurSonucuDetayDto(
                Id: d.Id,
                MalzemeKodu: d.MalzemeKodu,
                MalzemeAdi: malzemeler.TryGetValue(d.MalzemeKodu, out var m) ? m.MalzemeAdi : d.MalzemeKodu,
                LotNo: d.LotNo,
                SeriNo: d.SeriNo,
                OlcuBirimi: malzemeler.TryGetValue(d.MalzemeKodu, out var mb) ? mb.OlcuBirimi : string.Empty,
                Deger1: d.Deger1,
                Deger2: d.Deger2,
                Deger3: d.Deger3,
                Fark: d.Fark,
                FarkYuzdesi: d.FarkYuzdesi,
                Durum: d.Durum.ToString(),
                OnaylananDeger: d.OnaylananDeger,
                KararTipi: d.KararTipi?.ToString(),
                ManuelGerekce: d.ManuelKarar?.Gerekce
            )).ToList()
        );
    }

    public async Task ManuelKararVerAsync(int turSonucuDetayId, ManuelKararDto request, string kullaniciId, CancellationToken ct = default)
    {
        // TurSonucu + tüm detayları + oturum birlikte yükle
        var turSonucu = await _uow.TurSonuclari.Query()
            .Include(t => t.Detaylar)
                .ThenInclude(d => d.ManuelKarar)
            .Include(t => t.SayimTuru)
                .ThenInclude(t => t.SayimOturumu)
            .FirstOrDefaultAsync(t => t.Detaylar.Any(d => d.Id == turSonucuDetayId), ct)
            ?? throw new KeyNotFoundException($"TurSonucu bulunamadı: {turSonucuDetayId}");

        var detay = turSonucu.Detaylar.First(d => d.Id == turSonucuDetayId);

        // Karar kaydet
        var karar = new ManuelKarar
        {
            TurSonucuDetayId = turSonucuDetayId,
            SayimTuruId = turSonucu.SayimTuruId,
            MalzemeKodu = detay.MalzemeKodu,
            LotNo = detay.LotNo,
            KararVerilenDeger = request.KararVerilenDeger,
            Gerekce = request.Gerekce,
            KararVerenKullaniciId = kullaniciId,
            KararTarihi = DateTime.UtcNow,
            OlusturanKullaniciId = kullaniciId
        };
        detay.ManuelKarar = karar;
        detay.OnaylananDeger = request.KararVerilenDeger;
        detay.KararTipi = KararTipi.Manuel;
        detay.Durum = TurSonucuDetayDurum.Eslesti;

        // TurSonucu sayaçlarını güncelle
        turSonucu.EslesilenSayisi = turSonucu.Detaylar.Count(d =>
            d.Durum == TurSonucuDetayDurum.Eslesti || d.Id == turSonucuDetayId);
        turSonucu.FarkliSayisi = turSonucu.Detaylar.Count(d =>
            d.Durum == TurSonucuDetayDurum.FarkVar && d.Id != turSonucuDetayId);

        // Tüm detaylar çözüldüyse turu ve oturumu kapat
        bool tumDetaylarCozuldu = turSonucu.Detaylar
            .All(d => d.Durum == TurSonucuDetayDurum.Eslesti || d.Id == turSonucuDetayId);

        if (tumDetaylarCozuldu)
        {
            turSonucu.GenelDurum = SayimTuruDurum.Onaylandi;
            turSonucu.SayimTuru.Durum = SayimTuruDurum.Onaylandi;

            var oturum = turSonucu.SayimTuru.SayimOturumu;
            oturum.Durum = SayimOturumuDurum.Onaylandi;

            // Bu oturuma ait tüm bekleyen bildirimleri kapat
            var bekleyenBildirimler = await _uow.GorevBildirimleri
                .Query()
                .Where(b => b.SayimOturumuId == oturum.Id
                    && b.Durum == GorevBildirimDurum.Beklemede)
                .ToListAsync(ct);
            foreach (var b in bekleyenBildirimler)
            {
                b.Durum = GorevBildirimDurum.Islendi;
                b.IslemTarihi = DateTime.UtcNow;
                b.IsleyenKullaniciId = kullaniciId;
            }
        }

        await _uow.SaveChangesAsync(ct);
    }

    public async Task ErpKarsilastirmaBaslatAsync(int planId, string kullaniciId, CancellationToken ct = default)
    {
        var oturumlar = await _uow.SayimOturumlari.GetByPlanIdAsync(planId, ct);
        var tamamlanmamisOturumlar = oturumlar.Where(o =>
            o.Durum != SayimOturumuDurum.Onaylandi &&
            o.Durum != SayimOturumuDurum.ManuelKarar).ToList();

        if (tamamlanmamisOturumlar.Any())
            throw new InvalidOperationException($"{tamamlanmamisOturumlar.Count} bölgede sayım henüz tamamlanmamış.");

        var erpStoklar = await _uow.ErpStoklar.GetByPlanIdAsync(planId, ct);

        var plan = await _uow.SayimPlanlari.GetByIdAsync(planId, ct)
            ?? throw new KeyNotFoundException($"Plan bulunamadı: {planId}");

        await _uow.BeginTransactionAsync(ct);
        try
        {
            foreach (var oturum in oturumlar)
            {
                var sonTur = oturum.SayimTurlari.OrderByDescending(t => t.TurNo).First();
                var sonSonuc = sonTur.TurSonucu;
                if (sonSonuc == null) continue;

                var erpTur = await YeniTurAcAsync(oturum.Id, oturum.AktifTurNo + 1, SayimTuruTip.ErpKarsilastirma, kullaniciId, ct);
                oturum.AktifTurNo++;

                // ERP karşılaştırma sonucunu hesapla
                await HesaplaErpKarsilastirmaAsync(erpTur, sonSonuc, erpStoklar, oturum, kullaniciId, ct);
            }

            plan.Durum = SayimPlaniDurum.ErpKarsilastirmaAktif;
            _uow.SayimPlanlari.Update(plan);

            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }
    }

    // --- Yardımcı metodlar ---

    private async Task<SayimTuru> YeniTurAcAsync(int oturumuId, int turNo, SayimTuruTip tip, string kullaniciId, CancellationToken ct)
    {
        var tur = new SayimTuru
        {
            SayimOturumuId = oturumuId,
            TurNo = turNo,
            TurTipi = tip,
            Durum = SayimTuruDurum.Beklemede,
            AcilamaTarihi = DateTime.UtcNow,
            OlusturanKullaniciId = kullaniciId
        };

        await _uow.SayimTurlari.AddAsync(tur, ct);
        await _uow.SaveChangesAsync(ct);
        return tur;
    }

    public async Task HesaplaKarsilastirmaAsync(int turId, CancellationToken ct = default)
    {
        var tur = await _uow.SayimTurlari.GetWithKatilimcilarAsync(turId, ct)
            ?? throw new KeyNotFoundException($"Tur bulunamadı: {turId}");

        var tamamlanmamisKatilimci = tur.Katilimcilar.Any(k => k.SayimKaydiId == null);
        if (tamamlanmamisKatilimci)
            throw new InvalidOperationException("Tüm ekipler sayımını tamamlamadan karşılaştırma yapılamaz.");

        var kayitlar = await _uow.SayimKayitlari.GetByTurIdAsync(turId, ct);
        var ekipKayitlari = kayitlar.ToDictionary(k => k.EkipRolu, k => k.Detaylar);
        var kontrolTuru = tur.TurTipi == SayimTuruTip.EkipKontrol;

        // --- KONTROL TURU: ilk EkipKarsilastirma TurSonucu'nun Deger3'ünü güncelle ---
        if (kontrolTuru)
        {
            var karsilastirmaOturumu = await _uow.SayimOturumlari.GetWithTurlerAsync(tur.SayimOturumuId, ct)
                ?? throw new KeyNotFoundException("Oturum bulunamadı.");

            var ilkTur = karsilastirmaOturumu.SayimTurlari
                .Where(t => t.TurTipi == SayimTuruTip.EkipKarsilastirma)
                .OrderBy(t => t.TurNo)
                .First();

            var mevcutSonuc = await _uow.TurSonuclari.GetByTurIdAsync(ilkTur.Id, ct)
                ?? throw new InvalidOperationException("İlk tur sonucu bulunamadı.");

            ekipKayitlari.TryGetValue(EkipRolu.Kontrol, out var kontrolDetaylar);

            decimal? Miktar3(string malzemeKodu, string? lotNo) =>
                kontrolDetaylar == null ? null :
                kontrolDetaylar.Where(d => d.MalzemeKodu == malzemeKodu && d.LotNo == lotNo)
                               .Sum(d => d.SayilanMiktar);

            var farkVar3 = false;
            foreach (var detay in mevcutSonuc.Detaylar)
            {
                // Manuel karar verilmişse dokunma
                if (detay.KararTipi == KararTipi.Manuel) continue;

                var deger3 = Miktar3(detay.MalzemeKodu, detay.LotNo);
                detay.Deger3 = deger3;

                var eslesti = deger3.HasValue && (deger3 == detay.Deger1 || deger3 == detay.Deger2);
                detay.Durum = eslesti ? TurSonucuDetayDurum.Eslesti : TurSonucuDetayDurum.FarkVar;
                detay.Fark = eslesti ? 0 : (deger3.HasValue && detay.Deger1.HasValue ? deger3.Value - detay.Deger1.Value : null);
                detay.FarkYuzdesi = eslesti ? 0 : (detay.Fark.HasValue && detay.Deger1.HasValue && detay.Deger1.Value != 0
                    ? Math.Abs(detay.Fark.Value / detay.Deger1.Value * 100) : null);
                detay.OnaylananDeger = eslesti ? deger3 : null;
                detay.KararTipi = eslesti ? KararTipi.Otomatik : null;

                if (!eslesti) farkVar3 = true;
            }

            mevcutSonuc.EslesilenSayisi = mevcutSonuc.Detaylar.Count(d => d.Durum == TurSonucuDetayDurum.Eslesti);
            mevcutSonuc.FarkliSayisi = mevcutSonuc.Detaylar.Count(d => d.Durum == TurSonucuDetayDurum.FarkVar);
            mevcutSonuc.GenelDurum = farkVar3 ? SayimTuruDurum.FarkVar : SayimTuruDurum.Onaylandi;
            mevcutSonuc.HesaplamaTarihi = DateTime.UtcNow;

            // Bu kontrol turunu da kapat, ayrı TurSonucu yaratma
            tur.Durum = farkVar3 ? SayimTuruDurum.FarkVar : SayimTuruDurum.Onaylandi;
            tur.KapanmaTarihi = farkVar3 ? null : DateTime.UtcNow;

            // Tüm açık KontrolSayimiGerekli bildirimlerini kapat
            var acikBildirimler = await _uow.GorevBildirimleri.GetBekleyenlerByOturumAsync(
                tur.SayimOturumuId, GorevBildirimTipi.KontrolSayimiGerekli, ct);
            foreach (var b in acikBildirimler)
            {
                b.Durum = GorevBildirimDurum.Islendi;
                b.IslemTarihi = DateTime.UtcNow;
            }

            var kontrolOturum = await _uow.SayimOturumlari.GetByIdAsync(tur.SayimOturumuId, ct)!;
            if (!farkVar3)
                kontrolOturum!.Durum = SayimOturumuDurum.Onaylandi;
            else
            {
                var bildirim = new GorevBildirimi
                {
                    SayimOturumuId = tur.SayimOturumuId,
                    SayimTuruId = turId,
                    BildirimTipi = GorevBildirimTipi.KontrolSayimiGerekli,
                    Durum = GorevBildirimDurum.Beklemede,
                    Mesaj = $"Tur {tur.TurNo}: {mevcutSonuc.FarkliSayisi} malzemede fark var. Kontrol sayımı gerekli."
                };
                await _uow.GorevBildirimleri.AddAsync(bildirim, ct);
            }

            await _uow.SaveChangesAsync(ct);
            return;
        }

        // --- EKİP KARŞILAŞTIRMA TURU ---
        var tumDetaylar = kayitlar.SelectMany(k => k.Detaylar).ToList();
        var sonucDetaylar = new List<TurSonucuDetay>();
        var farkVar = false;

        var tumMalzemeler = tumDetaylar
            .Select(d => new { d.MalzemeKodu, d.LotNo, d.SeriNo })
            .Distinct()
            .ToList();

        foreach (var malzeme in tumMalzemeler)
        {
            decimal? Miktar(ICollection<SayimKaydiDetay>? detaylar) =>
                detaylar == null ? null :
                detaylar.Where(d => d.MalzemeKodu == malzeme.MalzemeKodu && d.LotNo == malzeme.LotNo)
                        .Sum(d => d.SayilanMiktar);

            var deger1 = ekipKayitlari.TryGetValue(EkipRolu.Birinci, out var k1) ? Miktar(k1) : null;
            var deger2 = ekipKayitlari.TryGetValue(EkipRolu.Ikinci, out var k2) ? Miktar(k2) : null;

            var fark = deger1.HasValue && deger2.HasValue ? deger1.Value - deger2.Value : (decimal?)null;
            var farkYuzdesi = fark.HasValue && deger1.HasValue && deger1.Value != 0
                ? Math.Abs(fark.Value / deger1.Value * 100) : (decimal?)null;

            var eslesti = deger1.HasValue && deger2.HasValue && deger1 == deger2;
            var durum = eslesti ? TurSonucuDetayDurum.Eslesti : TurSonucuDetayDurum.FarkVar;
            if (!eslesti) farkVar = true;

            sonucDetaylar.Add(new TurSonucuDetay
            {
                MalzemeKodu = malzeme.MalzemeKodu,
                LotNo = malzeme.LotNo,
                SeriNo = malzeme.SeriNo,
                Deger1 = deger1,
                Deger2 = deger2,
                Deger3 = null,
                Fark = fark,
                FarkYuzdesi = farkYuzdesi,
                Durum = durum,
                OnaylananDeger = eslesti ? deger1 : null,
                KararTipi = eslesti ? KararTipi.Otomatik : null
            });
        }

        var turSonucu = new TurSonucu
        {
            SayimTuruId = turId,
            ToplamMalzemeSayisi = sonucDetaylar.Count,
            EslesilenSayisi = sonucDetaylar.Count(d => d.Durum == TurSonucuDetayDurum.Eslesti),
            FarkliSayisi = sonucDetaylar.Count(d => d.Durum == TurSonucuDetayDurum.FarkVar),
            GenelDurum = farkVar ? SayimTuruDurum.FarkVar : SayimTuruDurum.Onaylandi,
            Detaylar = sonucDetaylar
        };
        tur.TurSonucu = turSonucu;

        tur.Durum = farkVar ? SayimTuruDurum.FarkVar : SayimTuruDurum.Onaylandi;
        tur.KapanmaTarihi = farkVar ? null : DateTime.UtcNow;

        var oturum = await _uow.SayimOturumlari.GetByIdAsync(tur.SayimOturumuId, ct)!;
        if (!farkVar)
            oturum!.Durum = SayimOturumuDurum.Onaylandi;
        else
        {
            var bildirim = new GorevBildirimi
            {
                SayimOturumuId = tur.SayimOturumuId,
                SayimTuruId = turId,
                BildirimTipi = GorevBildirimTipi.KontrolSayimiGerekli,
                Durum = GorevBildirimDurum.Beklemede,
                Mesaj = $"Tur {tur.TurNo}: {turSonucu.FarkliSayisi} malzemede fark var. Kontrol sayımı gerekli."
            };
            await _uow.GorevBildirimleri.AddAsync(bildirim, ct);
        }

        await _uow.SaveChangesAsync(ct);
    }

    private async Task HesaplaErpKarsilastirmaAsync(SayimTuru tur, TurSonucu fiiliSonuc,
        IEnumerable<ErpStok> erpStoklar, SayimOturumu oturum, string kullaniciId, CancellationToken ct)
    {
        var onaylananlar = fiiliSonuc.Detaylar.Where(d => d.OnaylananDeger.HasValue).ToList();
        var sonucDetaylar = new List<TurSonucuDetay>();
        var farkVar = false;

        foreach (var fiiliDetay in onaylananlar)
        {
            // ERP'de aynı malzeme kodu + lot no kombinasyonu birden fazla depoda olabilir.
            // Bölge veya depo koduna bakmaksızın sadece malzeme kodu + lot no bazında toplam alıyoruz.
            var erpMiktar = erpStoklar
                .Where(e =>
                    e.MalzemeKodu == fiiliDetay.MalzemeKodu &&
                    (e.LotNo == fiiliDetay.LotNo || (string.IsNullOrEmpty(e.LotNo) && string.IsNullOrEmpty(fiiliDetay.LotNo))))
                .Sum(e => e.Miktar);
            var fiiliMiktar = fiiliDetay.OnaylananDeger!.Value;
            var fark = fiiliMiktar - erpMiktar;

            var durum = fark == 0 ? TurSonucuDetayDurum.Eslesti : TurSonucuDetayDurum.FarkVar;
            if (durum == TurSonucuDetayDurum.FarkVar) farkVar = true;

            sonucDetaylar.Add(new TurSonucuDetay
            {
                MalzemeKodu = fiiliDetay.MalzemeKodu,
                LotNo = fiiliDetay.LotNo,
                SeriNo = fiiliDetay.SeriNo,
                Deger1 = erpMiktar,
                Deger2 = fiiliMiktar,
                Fark = fark,
                FarkYuzdesi = erpMiktar != 0 ? Math.Abs(fark / erpMiktar * 100) : null,
                Durum = durum,
                OnaylananDeger = durum == TurSonucuDetayDurum.Eslesti ? fiiliMiktar : null,
                KararTipi = durum == TurSonucuDetayDurum.Eslesti ? KararTipi.Otomatik : null
            });
        }

        // ✅ FIX: turSonucu oluşturulup tur'a atanıyor, önce hiç atanmıyordu
        var turSonucu = new TurSonucu
        {
            SayimTuruId = tur.Id,
            ToplamMalzemeSayisi = sonucDetaylar.Count,
            EslesilenSayisi = sonucDetaylar.Count(d => d.Durum == TurSonucuDetayDurum.Eslesti),
            FarkliSayisi = sonucDetaylar.Count(d => d.Durum == TurSonucuDetayDurum.FarkVar),
            GenelDurum = farkVar ? SayimTuruDurum.FarkVar : SayimTuruDurum.Onaylandi,
            Detaylar = sonucDetaylar
        };
        tur.TurSonucu = turSonucu;

        tur.Durum = farkVar ? SayimTuruDurum.FarkVar : SayimTuruDurum.Onaylandi;

        if (farkVar)
        {
            var bildirim = new GorevBildirimi
            {
                SayimOturumuId = oturum.Id,
                SayimTuruId = tur.Id,
                BildirimTipi = GorevBildirimTipi.ErpKontrolGerekli,
                Durum = GorevBildirimDurum.Beklemede,
                Mesaj = $"ERP karşılaştırma: {sonucDetaylar.Count(d => d.Durum == TurSonucuDetayDurum.FarkVar)} malzemede fark tespit edildi.",
                OlusturanKullaniciId = kullaniciId
            };
            await _uow.GorevBildirimleri.AddAsync(bildirim, ct);
        }
    }

    private static SayimOturumuDetayDto MapToDto(SayimOturumu oturum) => new(
        Id: oturum.Id,
        BolgeId: oturum.BolgeId,
        BolgeAdi: oturum.Bolge?.BolgeAdi ?? string.Empty,
        SayimPlaniId: oturum.SayimPlaniId,
        Durum: oturum.Durum,
        DurumAdi: oturum.Durum.ToString(),
        AktifTurNo: oturum.AktifTurNo,
        BaslangicTarihi: oturum.BaslangicTarihi,
        KapanisTarihi: oturum.KapanisTarihi,
        Turler: oturum.SayimTurlari.OrderBy(t => t.TurNo).Select(t => new SayimTuruOzetDto(
            Id: t.Id,
            TurNo: t.TurNo,
            TurTipi: t.TurTipi,
            TurTipiAdi: t.TurTipi.ToString(),
            Durum: t.Durum,
            DurumAdi: t.Durum.ToString(),
            AcilamaTarihi: t.AcilamaTarihi,
            KapanmaTarihi: t.KapanmaTarihi,
            Katilimcilar: t.Katilimcilar.Select(k => new KatilimciOzetDto(
                EkipId: k.EkipId,
                EkipAdi: k.Ekip?.EkipAdi ?? string.Empty,
                EkipRolu: k.EkipRolu,
                EkipRoluAdi: k.EkipRolu.ToString(),
                SayimTamamlandi: k.SayimKaydiId.HasValue,
                SayimKaydiId: k.SayimKaydiId
            )),
            Sonuc: t.TurSonucu == null ? null : new TurSonucuOzetDto(
                Id: t.TurSonucu.Id,
                ToplamMalzeme: t.TurSonucu.ToplamMalzemeSayisi,
                Eslesen: t.TurSonucu.EslesilenSayisi,
                Farkli: t.TurSonucu.FarkliSayisi,
                GenelDurum: t.TurSonucu.GenelDurum.ToString()
            )
        ))
    );
}