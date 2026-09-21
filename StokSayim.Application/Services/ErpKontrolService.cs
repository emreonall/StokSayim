using ClosedXML.Excel;
using StokSayim.Application.DTOs.ErpKontrol;
using StokSayim.Application.Interfaces;
using StokSayim.Application.Interfaces.Services;
using StokSayim.Domain.Entities;
using StokSayim.Domain.Enums;

namespace StokSayim.Application.Services;

public class ErpKontrolService : IErpKontrolService
{
    private readonly IUnitOfWork _uow;

    public ErpKontrolService(IUnitOfWork uow) => _uow = uow;

    // ─── Atama listesi ────────────────────────────────────────────────────────
    // ERP karşılaştırma sonucu fark olan + sadece ERP'de olan malzemeleri döner

    public async Task<IEnumerable<ErpKontrolAtamaDto>> GetAtamaListesiAsync(int planId, CancellationToken ct = default)
    {
        var plan = await _uow.SayimPlanlari.GetWithDetailsAsync(planId, ct)
            ?? throw new KeyNotFoundException($"Plan bulunamadı: {planId}");
        // ERP miktarı: sadece plan depo kodlarındaki stoklar
        var erpStoklar = (await _uow.ErpStoklar.GetByPlanIdAsync(planId, ct)).SadecePlanDepolari(plan.DepoKodlari);
        var oturumlar = (await _uow.SayimOturumlari.GetByPlanIdAsync(planId, ct)).ToList();

        // Fiili sayım sonuçlarını topla (malzeme kodu bazında)
        var sayimSonuclari = new Dictionary<string, decimal>();
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
                if (sayimSonuclari.ContainsKey(detay.MalzemeKodu))
                    sayimSonuclari[detay.MalzemeKodu] += fiili;
                else
                    sayimSonuclari[detay.MalzemeKodu] = fiili;
            }
        }

        // ERP özeti (malzeme kodu bazında toplam)
        var erpOzet = erpStoklar
            .GroupBy(e => e.MalzemeKodu)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Miktar));

        var tumKodlar = erpOzet.Keys.Union(sayimSonuclari.Keys).Distinct().ToList();
        var malzemeSozlugu = await _uow.Malzemeler.GetDictionaryByKodlarAsync(tumKodlar);

        // Mevcut atama varsa getir
        var mevcutOturum = await _uow.ErpKontrolOturumlari.GetByPlanIdAsync(planId, ct);
        var mevcutAtamalar = new Dictionary<string, int>();
        if (mevcutOturum != null)
        {
            foreach (var ekip in mevcutOturum.Ekipler)
                foreach (var mal in ekip.Malzemeler)
                    mevcutAtamalar[mal.MalzemeKodu] = ekip.EkipId;
        }

        var liste = new List<ErpKontrolAtamaDto>();

        foreach (var kvp in erpOzet)
        {
            var malzemeKodu = kvp.Key;
            var erpMiktar = kvp.Value;
            sayimSonuclari.TryGetValue(malzemeKodu, out var fiiliMiktar);
            var fark = fiiliMiktar - erpMiktar;

            // Sadece fark olan veya hiç sayılmamış malzemeleri dahil et
            if (fark == 0) continue;

            malzemeSozlugu.TryGetValue(malzemeKodu, out var malzeme);
            mevcutAtamalar.TryGetValue(malzemeKodu, out var atananEkipId);

            liste.Add(new ErpKontrolAtamaDto(
                MalzemeKodu: malzemeKodu,
                MalzemeAdi: malzeme?.MalzemeAdi ?? malzemeKodu,
                Birim: malzeme?.OlcuBirimi ?? string.Empty,
                ErpMiktar: erpMiktar,
                FiiliMiktar: fiiliMiktar,
                Fark: fark,
                AtananEkipId: atananEkipId == 0 ? null : atananEkipId
            ));
        }

        // Sadece sayımda olan ama ERP'de olmayan malzemeleri de ekle
        foreach (var kvp in sayimSonuclari)
        {
            if (erpOzet.ContainsKey(kvp.Key)) continue;
            malzemeSozlugu.TryGetValue(kvp.Key, out var malzeme);
            mevcutAtamalar.TryGetValue(kvp.Key, out var atananEkipId);

            liste.Add(new ErpKontrolAtamaDto(
                MalzemeKodu: kvp.Key,
                MalzemeAdi: malzeme?.MalzemeAdi ?? kvp.Key,
                Birim: malzeme?.OlcuBirimi ?? string.Empty,
                ErpMiktar: 0,
                FiiliMiktar: kvp.Value,
                Fark: kvp.Value,
                AtananEkipId: atananEkipId == 0 ? null : atananEkipId
            ));
        }

        return liste.OrderBy(x => x.MalzemeKodu).ToList();
    }

    // ─── Başlat ───────────────────────────────────────────────────────────────

    public async Task<ErpKontrolOturumuDto> BaslatAsync(int planId, ErpKontrolBaslatDto request, string kullaniciId, CancellationToken ct = default)
    {
        // ERP kontrol sayımı tek turdur ve zorunludur: fark olan TÜM malzemelere kontrol ekibi atanmalıdır.
        var farkMalzemeleri = (await GetAtamaListesiAsync(planId, ct)).Select(a => a.MalzemeKodu).ToList();
        var atananlar = request.EkipAtamalari.SelectMany(e => e.MalzemeKodlari).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var atanmayan = farkMalzemeleri.Count(k => !atananlar.Contains(k));
        if (atanmayan > 0)
            throw new InvalidOperationException(
                $"{atanmayan} fark malzemesine kontrol ekibi atanmamış. ERP kontrol sayımı tüm fark malzemeleri için yapılmalıdır.");

        // Önceki oturum varsa sil (yeniden atama senaryosu)
        var mevcutOturum = await _uow.ErpKontrolOturumlari.GetByPlanIdAsync(planId, ct);
        if (mevcutOturum != null)
        {
            if (mevcutOturum.Durum == ErpKontrolOturumuDurum.Tamamlandi)
                throw new InvalidOperationException("Tamamlanmış ERP kontrol oturumu yeniden başlatılamaz.");
            _uow.ErpKontrolOturumlari.Delete(mevcutOturum);
            await _uow.SaveChangesAsync(ct);
        }

        var malzemeSozlugu = await _uow.Malzemeler.GetDictionaryByKodlarAsync(
            request.EkipAtamalari.SelectMany(e => e.MalzemeKodlari).Distinct());

        var erpStoklar = (await _uow.ErpStoklar.GetByPlanIdAsync(planId, ct))
            .GroupBy(e => e.MalzemeKodu)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Miktar));

        await _uow.BeginTransactionAsync(ct);
        try
        {
            var oturum = new ErpKontrolOturumu
            {
                SayimPlaniId = planId,
                Durum = ErpKontrolOturumuDurum.DevamEdiyor,
                OlusturanKullaniciId = kullaniciId
            };
            await _uow.ErpKontrolOturumlari.AddAsync(oturum, ct);
            await _uow.SaveChangesAsync(ct);

            foreach (var ekipAtama in request.EkipAtamalari)
            {
                if (!ekipAtama.MalzemeKodlari.Any()) continue;

                var ekip = new ErpKontrolEkip
                {
                    ErpKontrolOturumuId = oturum.Id,
                    EkipId = ekipAtama.EkipId,
                    Durum = ErpKontrolEkipDurum.Beklemede,
                    Malzemeler = ekipAtama.MalzemeKodlari.Select(kod =>
                    {
                        malzemeSozlugu.TryGetValue(kod, out var m);
                        return new ErpKontrolMalzeme
                        {
                            MalzemeKodu = kod,
                            MalzemeAdi = m?.MalzemeAdi ?? kod,
                            Birim = m?.OlcuBirimi ?? string.Empty,
                            // ErpMiktar kaydedilmiyor — kör sayım
                            SayilanMiktar = null,
                            Tamamlandi = false
                        };
                    }).ToList()
                };
                await _uow.SaveChangesAsync(ct);
                oturum.Ekipler.Add(ekip);
            }

            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);

            return await GetOturumuDtoAsync(oturum.Id, ct);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }
    }

    // ─── Oturum özeti ─────────────────────────────────────────────────────────

    public async Task<ErpKontrolOturumuDto?> GetOturumuAsync(int planId, CancellationToken ct = default)
    {
        var oturum = await _uow.ErpKontrolOturumlari.GetByPlanIdAsync(planId, ct);
        if (oturum == null) return null;
        return MapOturumuDto(oturum);
    }

    // ─── Ekip detayı (kör sayım) ──────────────────────────────────────────────

    public async Task<ErpKontrolEkipDetayDto?> GetEkipDetayAsync(int planId, int ekipId, CancellationToken ct = default)
    {
        var ekip = await _uow.ErpKontrolOturumlari.GetEkipByPlanAndEkipAsync(planId, ekipId, ct);
        return ekip == null ? null : MapEkipDetayDto(ekip);
    }

    public async Task<ErpKontrolEkipDetayDto?> GetEkipDetayByKullaniciAsync(int planId, string kullaniciId, CancellationToken ct = default)
    {
        var ekip = await _uow.ErpKontrolOturumlari.GetEkipByPlanAndKullaniciAsync(planId, kullaniciId, ct);
        return ekip == null ? null : MapEkipDetayDto(ekip);
    }

    private static ErpKontrolEkipDetayDto MapEkipDetayDto(ErpKontrolEkip ekip) =>
        new(
            ErpKontrolEkipId: ekip.Id,
            EkipId: ekip.EkipId,
            EkipAdi: ekip.Ekip?.EkipAdi ?? string.Empty,
            Durum: ekip.Durum,
            Malzemeler: ekip.Malzemeler.Select(m => new ErpKontrolMalzemeDto(
                Id: m.Id,
                MalzemeKodu: m.MalzemeKodu,
                MalzemeAdi: m.MalzemeAdi,
                Birim: m.Birim,
                SayilanMiktar: m.SayilanMiktar,
                Tamamlandi: m.Tamamlandi
            )).OrderBy(m => m.MalzemeKodu)
        );

    // ─── Malzeme sayım güncelle ───────────────────────────────────────────────

    public async Task MalzemeSayimGuncelleAsync(int malzemeId, ErpKontrolMalzemeSayimDto request, CancellationToken ct = default)
    {
        var malzeme = await _uow.ErpKontrolOturumlari.GetMalzemeAsync(malzemeId, ct)
            ?? throw new KeyNotFoundException($"Malzeme bulunamadı: {malzemeId}");

        if (malzeme.Tamamlandi)
            throw new InvalidOperationException("Tamamlanmış malzeme güncellenemez.");

        malzeme.SayilanMiktar = request.SayilanMiktar;
        await _uow.SaveChangesAsync(ct);
    }

    // ─── Ekip tamamla ─────────────────────────────────────────────────────────

    public async Task EkipTamamlaAsync(int erpKontrolEkipId, string kullaniciId, CancellationToken ct = default)
    {
        var ekip = await _uow.ErpKontrolOturumlari.GetEkipWithMalzemelerAsync(erpKontrolEkipId, ct)
            ?? throw new KeyNotFoundException($"ERP kontrol ekip bulunamadı: {erpKontrolEkipId}");

        if (ekip.Durum == ErpKontrolEkipDurum.Tamamlandi)
            throw new InvalidOperationException("Bu ekip zaten tamamlanmış.");

        var eksikMalzemeler = ekip.Malzemeler.Where(m => !m.SayilanMiktar.HasValue).ToList();
        if (eksikMalzemeler.Any())
            throw new InvalidOperationException($"{eksikMalzemeler.Count} malzemenin miktarı girilmemiş.");

        foreach (var m in ekip.Malzemeler)
            m.Tamamlandi = true;

        ekip.Durum = ErpKontrolEkipDurum.Tamamlandi;
        ekip.TamamlanmaTarihi = DateTime.UtcNow;

        // Tüm ekipler tamamlandıysa oturumu "Tamamlandi" yap — ama planı kapatma
        var oturum = ekip.ErpKontrolOturumu;
        if (oturum.Ekipler.All(e => e.Id == erpKontrolEkipId || e.Durum == ErpKontrolEkipDurum.Tamamlandi))
        {
            oturum.Durum = ErpKontrolOturumuDurum.Tamamlandi;
            oturum.TamamlanmaTarihi = DateTime.UtcNow;
            // Plan kapanışı manuel — SayimSorumlusu sonuçları inceleyip kapatır
        }

        // Kontrol sayımını ERP karşılaştırma turu sonucuna (Deger3) işle:
        //  - kontrol sayımı ERP ile eşleşirse satır otomatik çözülür,
        //  - eşleşmezse satır "Fark Var" kalır ve manuel karar beklenir.
        var erpTuru = await GetErpTuruAsync(oturum.SayimPlaniId, ct);
        if (erpTuru?.TurSonucu != null)
        {
            var detaylar = erpTuru.TurSonucu.Detaylar.ToLookup(d => d.MalzemeKodu.Trim(), StringComparer.OrdinalIgnoreCase);
            foreach (var m in ekip.Malzemeler)
            {
                foreach (var d in detaylar[m.MalzemeKodu.Trim()])
                {
                    d.Deger3 = m.SayilanMiktar;
                    if (d.KararTipi == KararTipi.Manuel || !m.SayilanMiktar.HasValue) continue;

                    var kontrol = m.SayilanMiktar.Value;
                    var erp = d.Deger1 ?? 0;
                    d.Fark = kontrol - erp;
                    d.FarkYuzdesi = erp != 0 ? Math.Abs((kontrol - erp) / erp * 100) : null;

                    if (kontrol == erp)
                    {
                        d.Durum = TurSonucuDetayDurum.Eslesti;
                        d.OnaylananDeger = kontrol;
                        d.KararTipi = KararTipi.Otomatik;
                    }
                }
            }
            ErpTuruDurumunuGuncelle(erpTuru);
        }

        await _uow.SaveChangesAsync(ct);
    }

    // ─── ERP kontrol sonrası manuel karar ─────────────────────────────────────

    public async Task ManuelKararVerAsync(int planId, ErpKontrolManuelKararDto request, string kullaniciId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Gerekce))
            throw new InvalidOperationException("Gerekçe zorunludur.");

        var plan = await _uow.SayimPlanlari.GetByIdAsync(planId, ct)
            ?? throw new KeyNotFoundException($"Plan bulunamadı: {planId}");
        if (plan.Durum == SayimPlaniDurum.Kapali)
            throw new InvalidOperationException("Kapalı planda karar verilemez.");

        var oturum = await _uow.ErpKontrolOturumlari.GetByPlanIdAsync(planId, ct)
            ?? throw new InvalidOperationException("ERP kontrol sayımı başlatılmamış. Karar vermeden önce ERP kontrol sayımı yapılmalıdır.");

        var kod = request.MalzemeKodu.Trim();
        var kontrolTamamlandi = oturum.Ekipler
            .Where(e => e.Durum == ErpKontrolEkipDurum.Tamamlandi)
            .SelectMany(e => e.Malzemeler)
            .Any(m => string.Equals(m.MalzemeKodu.Trim(), kod, StringComparison.OrdinalIgnoreCase));
        if (!kontrolTamamlandi)
            throw new InvalidOperationException($"'{kod}' için ERP kontrol sayımı henüz tamamlanmamış.");

        var erpTuru = await GetErpTuruAsync(planId, ct)
            ?? throw new InvalidOperationException("ERP karşılaştırma sonucu bulunamadı.");

        var detay = erpTuru.TurSonucu!.Detaylar
            .FirstOrDefault(d => string.Equals(d.MalzemeKodu.Trim(), kod, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"ERP karşılaştırma sonucunda '{kod}' bulunamadı.");

        if (detay.Durum == TurSonucuDetayDurum.Eslesti && detay.KararTipi != KararTipi.Manuel)
            throw new InvalidOperationException($"'{kod}' için fark yok, karar gerekmiyor.");

        if (detay.ManuelKarar != null)
        {
            // Mevcut kararı güncelle
            detay.ManuelKarar.KararVerilenDeger = request.KararVerilenDeger;
            detay.ManuelKarar.Gerekce = request.Gerekce.Trim();
            detay.ManuelKarar.KararVerenKullaniciId = kullaniciId;
            detay.ManuelKarar.KararTarihi = DateTime.UtcNow;
        }
        else
        {
            detay.ManuelKarar = new ManuelKarar
            {
                SayimTuruId = erpTuru.Id,
                MalzemeKodu = detay.MalzemeKodu,
                LotNo = null,
                KararVerilenDeger = request.KararVerilenDeger,
                Gerekce = request.Gerekce.Trim(),
                KararVerenKullaniciId = kullaniciId,
                KararTarihi = DateTime.UtcNow,
                OlusturanKullaniciId = kullaniciId
            };
        }

        detay.OnaylananDeger = request.KararVerilenDeger;
        detay.KararTipi = KararTipi.Manuel;
        detay.Durum = TurSonucuDetayDurum.Eslesti;

        ErpTuruDurumunuGuncelle(erpTuru);
        await _uow.SaveChangesAsync(ct);
    }

    // ─── ERP karşılaştırma turu (plan geneli) yardımcıları ────────────────────

    private async Task<SayimTuru?> GetErpTuruAsync(int planId, CancellationToken ct)
    {
        var oturumlar = await _uow.SayimOturumlari.GetByPlanIdAsync(planId, ct);
        return oturumlar
            .SelectMany(o => o.SayimTurlari)
            .Where(t => t.TurTipi == SayimTuruTip.ErpKarsilastirma && t.TurSonucu != null)
            .OrderByDescending(t => t.Id)
            .FirstOrDefault();
    }

    private static void ErpTuruDurumunuGuncelle(SayimTuru tur)
    {
        var sonuc = tur.TurSonucu!;
        sonuc.EslesilenSayisi = sonuc.Detaylar.Count(d => d.Durum == TurSonucuDetayDurum.Eslesti);
        sonuc.FarkliSayisi = sonuc.Detaylar.Count(d => d.Durum == TurSonucuDetayDurum.FarkVar);
        var cozuldu = sonuc.FarkliSayisi == 0;
        sonuc.GenelDurum = cozuldu ? SayimTuruDurum.Onaylandi : SayimTuruDurum.FarkVar;
        tur.Durum = sonuc.GenelDurum;
        tur.KapanmaTarihi = cozuldu ? DateTime.UtcNow : null;
    }

    // ─── Planı manuel kapat (SayimSorumlusu) ──────────────────────────────────

    public async Task PlaniKapatAsync(int planId, string kullaniciId, CancellationToken ct = default)
    {
        var oturum = await _uow.ErpKontrolOturumlari.GetByPlanIdAsync(planId, ct);

        // ERP karşılaştırmasında fark varsa ERP kontrol sayımı zorunludur ve farklar karara bağlanmış olmalıdır.
        var erpTuru = await GetErpTuruAsync(planId, ct);
        var cozulmemis = erpTuru?.TurSonucu?.Detaylar.Where(d => d.Durum == TurSonucuDetayDurum.FarkVar).ToList()
                         ?? new List<TurSonucuDetay>();
        if (cozulmemis.Count > 0)
        {
            if (oturum == null)
                throw new InvalidOperationException(
                    $"ERP karşılaştırmasında {cozulmemis.Count} malzemede fark var. Plan kapatılmadan önce ERP kontrol sayımı yapılmalıdır.");

            var kontrolsuz = cozulmemis.Count(d => !d.Deger3.HasValue);
            if (kontrolsuz > 0)
                throw new InvalidOperationException($"{kontrolsuz} fark malzemesinde ERP kontrol sayımı tamamlanmamış.");

            throw new InvalidOperationException(
                $"ERP kontrol sayımı sonrası {cozulmemis.Count} malzemede fark sürüyor. Bu malzemeler için manuel karar verilmelidir.");
        }

        if (oturum != null)
        {
            var tamamlanmamisEkipler = oturum.Ekipler
                .Where(e => e.Durum != ErpKontrolEkipDurum.Tamamlandi).ToList();

            if (tamamlanmamisEkipler.Any())
                throw new InvalidOperationException(
                    $"{tamamlanmamisEkipler.Count} ekip henüz sayımını tamamlamamış.");
        }

        var plan = await _uow.SayimPlanlari.GetByIdAsync(planId, ct)
            ?? throw new KeyNotFoundException($"Plan bulunamadı: {planId}");

        if (plan.Durum == SayimPlaniDurum.Kapali)
            throw new InvalidOperationException("Plan zaten kapalı.");

        plan.Durum = SayimPlaniDurum.Kapali;
        _uow.SayimPlanlari.Update(plan);
        await _uow.SaveChangesAsync(ct);

        await _uow.SaveChangesAsync(ct);
    }

    // ─── Final sonuçlar ───────────────────────────────────────────────────────

    public async Task<IEnumerable<ErpKontrolSonucDto>> GetSonuclarAsync(int planId, CancellationToken ct = default)
    {
        var oturum = await _uow.ErpKontrolOturumlari.GetByPlanIdAsync(planId, ct);
        if (oturum == null) return [];

        var planDepolari = (await _uow.SayimPlanlari.GetWithDetailsAsync(planId, ct))?.DepoKodlari
            ?? new List<SayimPlanDepoKodu>();
        var erpStoklar = (await _uow.ErpStoklar.GetByPlanIdAsync(planId, ct))
            .SadecePlanDepolari(planDepolari)
            .GroupBy(e => e.MalzemeKodu)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Miktar));

        var tumMalzemeler = oturum.Ekipler
            .SelectMany(e => e.Malzemeler)
            .GroupBy(m => m.MalzemeKodu)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.SayilanMiktar ?? 0));

        var kodlar = tumMalzemeler.Keys.Union(erpStoklar.Keys.Where(k => tumMalzemeler.ContainsKey(k))).Distinct().ToList();
        var malzemeSozlugu = await _uow.Malzemeler.GetDictionaryByKodlarAsync(kodlar);

        var sonuclar = new List<ErpKontrolSonucDto>();
        foreach (var kvp in tumMalzemeler)
        {
            erpStoklar.TryGetValue(kvp.Key, out var erpMiktar);
            malzemeSozlugu.TryGetValue(kvp.Key, out var malzeme);
            var kontrolMiktar = kvp.Value;
            var fark = kontrolMiktar - erpMiktar;
            var farkYuzdesi = erpMiktar != 0 ? Math.Abs(fark / erpMiktar * 100) : (kontrolMiktar != 0 ? 100m : 0m);

            sonuclar.Add(new ErpKontrolSonucDto(
                MalzemeKodu: kvp.Key,
                MalzemeAdi: malzeme?.MalzemeAdi ?? kvp.Key,
                Birim: malzeme?.OlcuBirimi ?? string.Empty,
                ErpMiktar: erpMiktar,
                KontrolSayimMiktar: kontrolMiktar,
                Fark: fark,
                FarkYuzdesi: Math.Round(farkYuzdesi, 2)
            ));
        }

        return sonuclar.OrderBy(s => s.MalzemeKodu).ToList();
    }

    // ─── Excel / Terminal import ───────────────────────────────────────────────

    public async Task<ErpKontrolImportSonucDto> ImportSayimAsync(int erpKontrolEkipId, Stream dosya, string dosyaAdi, CancellationToken ct = default)
    {
        var ekip = await _uow.ErpKontrolOturumlari.GetEkipWithMalzemelerAsync(erpKontrolEkipId, ct)
            ?? throw new KeyNotFoundException($"ERP kontrol ekip bulunamadı: {erpKontrolEkipId}");

        if (ekip.Durum == ErpKontrolEkipDurum.Tamamlandi)
            throw new InvalidOperationException("Tamamlanmış ekibe import yapılamaz.");

        var malzemeIndex = ekip.Malzemeler.ToDictionary(m => m.MalzemeKodu, m => m);
        var guncellenen = 0;
        var hatalar = new List<string>();
        var hataliSatir = 0;

        var uzanti = Path.GetExtension(dosyaAdi).ToLower();
        if (uzanti != ".xlsx" && uzanti != ".xls")
            throw new InvalidOperationException("Sadece .xlsx formatı desteklenmektedir.");

        using var wb = new XLWorkbook(dosya);
        var ws = wb.Worksheet(1);
        var satirlar = ws.RangeUsed()?.RowsUsed().Skip(1).ToList() ?? [];

        foreach (var satir in satirlar)
        {
            var satirNo = satir.RowNumber();
            var malzemeKodu = satir.Cell(1).GetString().Trim();
            var miktarStr = satir.Cell(2).GetString().Trim();

            if (string.IsNullOrEmpty(malzemeKodu))
            {
                hataliSatir++;
                hatalar.Add($"Satır {satirNo}: Malzeme kodu boş.");
                continue;
            }

            if (!malzemeIndex.TryGetValue(malzemeKodu, out var malzeme))
            {
                hataliSatir++;
                hatalar.Add($"Satır {satirNo}: '{malzemeKodu}' bu ekibe atanmamış.");
                continue;
            }

            decimal miktar;
            if (!decimal.TryParse(miktarStr.Replace(",", "."),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out miktar))
            {
                try { miktar = satir.Cell(2).GetValue<decimal>(); }
                catch
                {
                    hataliSatir++;
                    hatalar.Add($"Satır {satirNo}: Miktar sayısal değil ('{miktarStr}').");
                    continue;
                }
            }

            malzeme.SayilanMiktar = miktar;
            guncellenen++;
        }

        await _uow.SaveChangesAsync(ct);

        return new ErpKontrolImportSonucDto(
            Basarili: hataliSatir == 0,
            Guncellenen: guncellenen,
            HataliSatir: hataliSatir,
            Hatalar: hatalar
        );
    }

    // ─── Terminal import (malzeme kodu + miktar listesi) ──────────────────────

    public async Task TerminalSayimGuncelleAsync(int planId, int ekipId, IEnumerable<ErpKontrolMalzemeSayimDto> kayitlar, CancellationToken ct = default)
    {
        var ekip = await _uow.ErpKontrolOturumlari.GetEkipByPlanAndEkipAsync(planId, ekipId, ct)
            ?? throw new KeyNotFoundException("ERP kontrol ekip bulunamadı.");

        if (ekip.Durum == ErpKontrolEkipDurum.Tamamlandi)
            throw new InvalidOperationException("Tamamlanmış ekibe güncelleme yapılamaz.");

        var malzemeIndex = ekip.Malzemeler.ToDictionary(m => m.Id, m => m);

        foreach (var kayit in kayitlar)
        {
            if (!malzemeIndex.TryGetValue(kayit.MalzemeId, out var malzeme)) continue;
            malzeme.SayilanMiktar = kayit.SayilanMiktar;
        }

        await _uow.SaveChangesAsync(ct);
    }

    // ─── Yardımcı metodlar ────────────────────────────────────────────────────

    private async Task<ErpKontrolOturumuDto> GetOturumuDtoAsync(int oturumuId, CancellationToken ct)
    {
        var oturum = await _uow.ErpKontrolOturumlari.GetWithEkiplerAsync(oturumuId, ct)
            ?? throw new KeyNotFoundException($"ERP kontrol oturumu bulunamadı: {oturumuId}");
        return MapOturumuDto(oturum);
    }

    private static ErpKontrolOturumuDto MapOturumuDto(ErpKontrolOturumu oturum) =>
        new(
            Id: oturum.Id,
            SayimPlaniId: oturum.SayimPlaniId,
            Durum: oturum.Durum,
            OlusturmaTarihi: oturum.OlusturmaTarihi,
            TamamlanmaTarihi: oturum.TamamlanmaTarihi,
            Ekipler: oturum.Ekipler.Select(e => new ErpKontrolEkipOzetDto(
                Id: e.Id,
                EkipId: e.EkipId,
                EkipAdi: e.Ekip?.EkipAdi ?? string.Empty,
                Durum: e.Durum,
                ToplamMalzeme: e.Malzemeler.Count,
                TamamlananMalzeme: e.Malzemeler.Count(m => m.Tamamlandi)
            ))
        );
}
