using ClosedXML.Excel;
using StokSayim.Application.DTOs.SayimPlani;
using StokSayim.Application.Interfaces;
using StokSayim.Application.Interfaces.Services;
using StokSayim.Domain.Entities;
using StokSayim.Domain.Enums;

namespace StokSayim.Application.Services;

public class SayimPlaniService : ISayimPlaniService
{
    private readonly IUnitOfWork _uow;

    public SayimPlaniService(IUnitOfWork uow) => _uow = uow;

    public async Task<IEnumerable<SayimPlaniListDto>> GetAllAsync(CancellationToken ct = default)
    {
        var planlar = await _uow.SayimPlanlari.GetAllAsync(ct);
        return planlar.Select(p => new SayimPlaniListDto(
            Id: p.Id,
            PlanAdi: p.PlanAdi,
            Aciklama: p.Aciklama,
            BaslangicTarihi: p.BaslangicTarihi,
            BitisTarihi: p.BitisTarihi,
            Durum: p.Durum,
            DurumAdi: p.Durum.ToString(),
            BolgeSayisi: 0,
            TamamlananBolgeSayisi: 0,
            OlusturmaTarihi: p.OlusturmaTarihi
        ));
    }

    public async Task<SayimPlaniDetayDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var plan = await _uow.SayimPlanlari.GetWithDetailsAsync(id, ct);
        if (plan == null) return null;

        var erpSayisi = await _uow.ErpStoklar.CountAsync(x => x.SayimPlaniId == id, ct);
        var sonImport = await _uow.ErpStoklar.FirstOrDefaultAsync(
            x => x.SayimPlaniId == id, ct);

        return new SayimPlaniDetayDto(
            Id: plan.Id,
            PlanAdi: plan.PlanAdi,
            Aciklama: plan.Aciklama,
            BaslangicTarihi: plan.BaslangicTarihi,
            BitisTarihi: plan.BitisTarihi,
            Durum: plan.Durum,
            DurumAdi: plan.Durum.ToString(),
            DepoKodlari: plan.DepoKodlari.Select(d => d.DepoKodu),
            ErpStokSatirSayisi: erpSayisi,
            ErpImportTarihi: sonImport?.ImportTarihi,
            OlusturmaTarihi: plan.OlusturmaTarihi
        );
    }

    public async Task<SayimPlaniDetayDto> CreateAsync(SayimPlaniOlusturDto request, string kullaniciId, CancellationToken ct = default)
    {
        var plan = new SayimPlani
        {
            PlanAdi = request.PlanAdi,
            Aciklama = request.Aciklama,
            BaslangicTarihi = request.BaslangicTarihi,
            BitisTarihi = request.BitisTarihi,
            Durum = SayimPlaniDurum.Taslak,
            OlusturanKullaniciId = kullaniciId,
            DepoKodlari = request.DepoKodlari.Select(d => new SayimPlanDepoKodu { DepoKodu = d }).ToList()
        };

        await _uow.SayimPlanlari.AddAsync(plan, ct);
        await _uow.SaveChangesAsync(ct);
        return (await GetByIdAsync(plan.Id, ct))!;
    }

    public async Task UpdateAsync(int id, SayimPlaniGuncelleDto request, CancellationToken ct = default)
    {
        var plan = await _uow.SayimPlanlari.GetWithDetailsAsync(id, ct)
            ?? throw new KeyNotFoundException($"Plan bulunamadı: {id}");

        if (plan.Durum != SayimPlaniDurum.Taslak)
            throw new InvalidOperationException("Sadece Taslak durumdaki planlar düzenlenebilir.");

        plan.PlanAdi = request.PlanAdi;
        plan.Aciklama = request.Aciklama;
        plan.BaslangicTarihi = request.BaslangicTarihi;
        plan.BitisTarihi = request.BitisTarihi;

        plan.DepoKodlari.Clear();
        foreach (var kod in request.DepoKodlari)
            plan.DepoKodlari.Add(new SayimPlanDepoKodu { DepoKodu = kod, SayimPlaniId = id });

        _uow.SayimPlanlari.Update(plan);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task AktifEtAsync(int id, string kullaniciId, CancellationToken ct = default)
    {
        var plan = await _uow.SayimPlanlari.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Plan bulunamadı: {id}");

        if (plan.Durum != SayimPlaniDurum.Taslak)
            throw new InvalidOperationException("Sadece Taslak durumdaki plan aktif edilebilir.");

        plan.Durum = SayimPlaniDurum.Aktif;
        plan.AktifEdilemeTarihi = DateTime.UtcNow;
        plan.AktifEdenKullaniciId = kullaniciId;

        _uow.SayimPlanlari.Update(plan);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<ErpImportSonucDto> ImportErpStokAsync(int id, Stream dosya, string dosyaAdi, string kullaniciId, CancellationToken ct = default)
    {
        var plan = await _uow.SayimPlanlari.GetWithDetailsAsync(id, ct)
            ?? throw new KeyNotFoundException($"Plan bulunamadı: {id}");

        var hatalar = new List<string>();
        var yeniKayitlar = new List<ErpStok>();
        var islenenSatir = 0;
        var hataliSatir = 0;

        var izinliDepolar = plan.DepoKodlari.Select(d => d.DepoKodu).ToHashSet();

        var uzanti = Path.GetExtension(dosyaAdi).ToLower();

        if (uzanti == ".xlsx" || uzanti == ".xls")
        {
            using var wb = new XLWorkbook(dosya);
            var ws = wb.Worksheet(1);
            var satirlar = ws.RangeUsed()?.RowsUsed().Skip(1).ToList() ?? [];

            foreach (var satir in satirlar)
            {
                islenenSatir++;
                try
                {
                    var depoKodu = satir.Cell(3).GetString().Trim();
                    if (!izinliDepolar.Contains(depoKodu))
                    {
                        hataliSatir++;
                        hatalar.Add($"Satır {islenenSatir + 1}: '{depoKodu}' depo kodu plan kapsamında değil.");
                        continue;
                    }

                    yeniKayitlar.Add(new ErpStok
                    {
                        SayimPlaniId = id,
                        MalzemeKodu = satir.Cell(1).GetString().Trim(),
                        MalzemeAdi = satir.Cell(2).GetString().Trim(),
                        DepoKodu = depoKodu,
                        Miktar = satir.Cell(4).GetValue<decimal>(),
                        Birim = satir.Cell(5).GetString().Trim(),
                        LotNo = satir.Cell(6).GetString().Trim().NullIfEmpty(),
                        SeriNo = satir.Cell(7).GetString().Trim().NullIfEmpty(),
                        ImportTarihi = DateTime.UtcNow,
                        ImportDosyaAdi = dosyaAdi,
                        OlusturanKullaniciId = kullaniciId
                    });
                }
                catch (Exception ex)
                {
                    hataliSatir++;
                    hatalar.Add($"Satır {islenenSatir + 1}: {ex.Message}");
                }
            }
        }
        else
        {
            throw new InvalidOperationException("Sadece .xlsx formatı desteklenmektedir.");
        }

        // Önceki import'u sil, yenisini ekle
        await _uow.ErpStoklar.DeleteByPlanIdAsync(id, ct);
        await _uow.ErpStoklar.AddRangeAsync(yeniKayitlar, ct);
        await _uow.SaveChangesAsync(ct);

        return new ErpImportSonucDto(
            Basarili: hataliSatir == 0,
            IslenenSatir: islenenSatir,
            EklenenSatir: yeniKayitlar.Count,
            HataliSatir: hataliSatir,
            Hatalar: hatalar,
            DosyaAdi: dosyaAdi,
            ImportTarihi: DateTime.UtcNow
        );
    }
}

internal static class StringExtensions
{
    public static string? NullIfEmpty(this string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
