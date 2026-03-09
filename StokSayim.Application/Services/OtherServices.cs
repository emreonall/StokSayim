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
        return kaydi == null ? null : MapToDto(kaydi);
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
            return MapToDto(mevcutKaydi);
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

        return MapToDto(kaydi);
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
            MalzemeAdi = request.MalzemeAdi,
            LotNo = request.LotNo,
            SeriNo = request.SeriNo,
            SayilanMiktar = request.SayilanMiktar,
            Birim = request.Birim,
            Notlar = request.Notlar
        };

        // ✅ FIX: detay kaydi'ya ekleniyor, önce kaydi tekrar Add ediliyordu
        kaydi.Detaylar.Add(detay);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task DetayGuncelleAsync(int detayId, SayimKaydiDetayEkleDto request, CancellationToken ct = default)
    {
        var detay = await _uow.SayimKayitlari.Query()
            .SelectMany(k => k.Detaylar)
            .FirstOrDefaultAsync(d => d.Id == detayId, ct)
            ?? throw new KeyNotFoundException($"Detay bulunamadı: {detayId}");

        detay.MalzemeKodu = request.MalzemeKodu;
        detay.MalzemeAdi = request.MalzemeAdi;
        detay.LotNo = request.LotNo;
        detay.SeriNo = request.SeriNo;
        detay.SayilanMiktar = request.SayilanMiktar;
        detay.Birim = request.Birim;
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
    }

    private static SayimKaydiDto MapToDto(SayimKaydi k) => new(
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
            MalzemeAdi: d.MalzemeAdi,
            LotNo: d.LotNo,
            SeriNo: d.SeriNo,
            SayilanMiktar: d.SayilanMiktar,
            Birim: d.Birim,
            Notlar: d.Notlar
        ))
    );
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
                var erpKayit = erpStoklar.FirstOrDefault(e => e.MalzemeKodu == detay.MalzemeKodu && e.LotNo == detay.LotNo);

                farkDetaylari.Add(new FarkDetayDto(
                    MalzemeKodu: detay.MalzemeKodu,
                    MalzemeAdi: detay.MalzemeAdi,
                    LotNo: detay.LotNo,
                    SeriNo: detay.SeriNo,
                    Birim: detay.Birim,
                    DepoKodu: erpKayit?.DepoKodu ?? string.Empty,
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