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
                await _uow.SayimTurleri.AddAsync(tur, ct); // SayimTuruKatilimci için extend edilmeli
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

        var sonTur = oturum.SayimTurleri.OrderByDescending(t => t.TurNo).First();
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

    public async Task ManuelKararVerAsync(int turSonucuDetayId, ManuelKararDto request, string kullaniciId, CancellationToken ct = default)
    {
        var detay = await _uow.TurSonuclari.Query()
            .SelectMany(t => t.Detaylar)
            .FirstOrDefaultAsync(d => d.Id == turSonucuDetayId, ct)
            ?? throw new KeyNotFoundException($"TurSonucuDetay bulunamadı: {turSonucuDetayId}");

        var karar = new ManuelKarar
        {
            TurSonucuDetayId = turSonucuDetayId,
            SayimTuruId = detay.TurSonucu.SayimTuruId,
            MalzemeKodu = detay.MalzemeKodu,
            LotNo = detay.LotNo,
            KararVerilenDeger = request.KararVerilenDeger,
            Gerekce = request.Gerekce,
            KararVerenKullaniciId = kullaniciId,
            KararTarihi = DateTime.UtcNow,
            OlusturanKullaniciId = kullaniciId
        };

        detay.OnaylananDeger = request.KararVerilenDeger;
        detay.KararTipi = KararTipi.Manuel;

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

        await _uow.BeginTransactionAsync(ct);
        try
        {
            foreach (var oturum in oturumlar)
            {
                var sonTur = oturum.SayimTurleri.OrderByDescending(t => t.TurNo).First();
                var sonSonuc = sonTur.TurSonucu;
                if (sonSonuc == null) continue;

                var erpTur = await YeniTurAcAsync(oturum.Id, oturum.AktifTurNo + 1, SayimTuruTip.ErpKarsilastirma, kullaniciId, ct);
                oturum.AktifTurNo++;

                // ERP karşılaştırma sonucunu hesapla
                await HesaplaErpKarsilastirmaAsync(erpTur, sonSonuc, erpStoklar, oturum, kullaniciId, ct);
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

        await _uow.SayimTurleri.AddAsync(tur, ct);
        await _uow.SaveChangesAsync(ct);
        return tur;
    }

    public async Task HesaplaKarsilastirmaAsync(int turId, CancellationToken ct = default)
    {
        var tur = await _uow.SayimTurleri.GetWithKatilimcilarAsync(turId, ct)
            ?? throw new KeyNotFoundException($"Tur bulunamadı: {turId}");

        // Tüm katılımcılar tamamladı mı?
        var tamamlanmamisKatilimci = tur.Katilimcilar.Any(k => k.SayimKaydiId == null);
        if (tamamlanmamisKatilimci)
            throw new InvalidOperationException("Tüm ekipler sayımını tamamlamadan karşılaştırma yapılamaz.");

        var kayitlar = await _uow.SayimKayitlari.GetByTurIdAsync(turId, ct);

        // Malzeme bazında grupla ve karşılaştır
        var tumDetaylar = kayitlar.SelectMany(k => k.Detaylar).ToList();
        var gruplar = tumDetaylar
            .GroupBy(d => new { d.MalzemeKodu, LotNo = d.LotNo ?? "", SeriNo = d.SeriNo ?? "" })
            .ToList();

        var sonucDetaylar = new List<TurSonucuDetay>();
        var farkVar = false;

        // Ekip bazında kayıtları al
        var ekipKayitlari = kayitlar.ToDictionary(k => k.EkipRolu, k => k.Detaylar);

        var tumMalzemeler = tumDetaylar
            .Select(d => new { d.MalzemeKodu, d.MalzemeAdi, d.LotNo, d.SeriNo, d.Birim })
            .Distinct()
            .ToList();

        foreach (var malzeme in tumMalzemeler)
        {
            var deger1 = ekipKayitlari.TryGetValue(EkipRolu.Birinci, out var k1)
                ? k1.Where(d => d.MalzemeKodu == malzeme.MalzemeKodu && d.LotNo == malzeme.LotNo)
                     .Sum(d => d.SayilanMiktar)
                : (decimal?)null;

            var deger2 = ekipKayitlari.TryGetValue(EkipRolu.Ikinci, out var k2)
                ? k2.Where(d => d.MalzemeKodu == malzeme.MalzemeKodu && d.LotNo == malzeme.LotNo)
                     .Sum(d => d.SayilanMiktar)
                : ekipKayitlari.TryGetValue(EkipRolu.Kontrol, out var kk)
                    ? kk.Where(d => d.MalzemeKodu == malzeme.MalzemeKodu && d.LotNo == malzeme.LotNo)
                         .Sum(d => d.SayilanMiktar)
                    : (decimal?)null;

            var fark = deger1.HasValue && deger2.HasValue ? deger1.Value - deger2.Value :(decimal?) null;
            var farkYuzdesi = deger1.HasValue && deger2.HasValue && deger1.Value != 0
                ? Math.Abs(fark!.Value / deger1.Value * 100)
                : (decimal?)null;

            var durum = fark == 0 ? TurSonucuDetayDurum.Eslesti : TurSonucuDetayDurum.FarkVar;
            if (durum == TurSonucuDetayDurum.FarkVar) farkVar = true;

            sonucDetaylar.Add(new TurSonucuDetay
            {
                MalzemeKodu = malzeme.MalzemeKodu,
                MalzemeAdi = malzeme.MalzemeAdi ?? string.Empty,
                LotNo = malzeme.LotNo,
                SeriNo = malzeme.SeriNo,
                Birim = malzeme.Birim ?? string.Empty,
                Deger1 = deger1,
                Deger2 = deger2,
                Fark = fark,
                FarkYuzdesi = farkYuzdesi,
                Durum = durum,
                OnaylananDeger = durum == TurSonucuDetayDurum.Eslesti ? deger1 : null,
                KararTipi = durum == TurSonucuDetayDurum.Eslesti ? KararTipi.Otomatik : null
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

        tur.Durum = farkVar ? SayimTuruDurum.FarkVar : SayimTuruDurum.Onaylandi;
        tur.KapanmaTarihi = farkVar ? null : DateTime.UtcNow;

        // Oturum durumunu güncelle
        var oturum = await _uow.SayimOturumlari.GetByIdAsync(tur.SayimOturumuId, ct)!;
        if (!farkVar)
            oturum!.Durum = SayimOturumuDurum.Onaylandi;
        else
        {
            // Bildirim oluştur
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
            var erpKayit = erpStoklar.FirstOrDefault(e =>
                e.MalzemeKodu == fiiliDetay.MalzemeKodu &&
                (e.LotNo == fiiliDetay.LotNo || (string.IsNullOrEmpty(e.LotNo) && string.IsNullOrEmpty(fiiliDetay.LotNo))));

            var erpMiktar = erpKayit?.Miktar ?? 0;
            var fiiliMiktar = fiiliDetay.OnaylananDeger!.Value;
            var fark = fiiliMiktar - erpMiktar;

            var durum = fark == 0 ? TurSonucuDetayDurum.Eslesti : TurSonucuDetayDurum.FarkVar;
            if (durum == TurSonucuDetayDurum.FarkVar) farkVar = true;

            sonucDetaylar.Add(new TurSonucuDetay
            {
                MalzemeKodu = fiiliDetay.MalzemeKodu,
                MalzemeAdi = fiiliDetay.MalzemeAdi,
                LotNo = fiiliDetay.LotNo,
                SeriNo = fiiliDetay.SeriNo,
                Birim = fiiliDetay.Birim,
                Deger1 = erpMiktar,      // Deger1 = ERP
                Deger2 = fiiliMiktar,    // Deger2 = Fiili
                Fark = fark,
                FarkYuzdesi = erpMiktar != 0 ? Math.Abs(fark / erpMiktar * 100) : null,
                Durum = durum,
                OnaylananDeger = durum == TurSonucuDetayDurum.Eslesti ? fiiliMiktar : null,
                KararTipi = durum == TurSonucuDetayDurum.Eslesti ? KararTipi.Otomatik : null
            });
        }

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
        Turler: oturum.SayimTurleri.OrderBy(t => t.TurNo).Select(t => new SayimTuruOzetDto(
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


