using ClosedXML.Excel;
using StokSayim.Application.DTOs.Malzeme;
using StokSayim.Application.Interfaces;
using StokSayim.Application.Interfaces.Services;
using StokSayim.Domain.Entities;

namespace StokSayim.Application.Services;

public class MalzemeService : IMalzemeService
{
    private readonly IUnitOfWork _uow;

    public MalzemeService(IUnitOfWork uow) => _uow = uow;

    public async Task<MalzemeOzetDto?> GetByKodAsync(string malzemeKodu, CancellationToken ct = default)
    {
        var m = await _uow.Malzemeler.GetByKodAsync(malzemeKodu, ct);
        return m == null ? null : new MalzemeOzetDto(m.MalzemeKodu, m.MalzemeAdi, m.OlcuBirimi);
    }

    public async Task<IEnumerable<MalzemeDto>> GetAllAsync(CancellationToken ct = default)
    {
        var liste = await _uow.Malzemeler.GetAllAsync(ct);
        return liste.Select(m => new MalzemeDto(m.Id, m.MalzemeKodu, m.MalzemeAdi, m.OlcuBirimi, m.AktifMi, m.SonGuncellemeTarihi, m.GuncellemeKaynagi));
    }

    public async Task<MalzemeImportDto> ImportAsync(Stream dosya, string dosyaAdi, CancellationToken ct = default)
    {
        var hatalar = new List<string>();
        int eklenen = 0, guncellenen = 0, hatali = 0, toplam = 0;

        using var wb = new XLWorkbook(dosya);
        // "Malzeme_Import" sheet adıyla bul, yoksa ilk sheet
        var ws = wb.Worksheets.Any(s => s.Name == "Malzeme_Import")
            ? wb.Worksheet("Malzeme_Import")
            : wb.Worksheets.First();
        // Satır 1: bilgi başlığı, Satır 2: kolon adları, Satır 3: açıklamalar → Skip(3)
        var satirlar = ws.RangeUsed()?.RowsUsed().Skip(3).ToList() ?? [];

        foreach (var satir in satirlar)
        {
            toplam++;
            var kod = satir.Cell(1).GetString().Trim();
            var ad = satir.Cell(2).GetString().Trim();
            var birim = satir.Cell(3).GetString().Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(kod)) { hatalar.Add($"Satır {satir.RowNumber}: Malzeme kodu boş."); hatali++; continue; }
            if (string.IsNullOrWhiteSpace(ad)) { hatalar.Add($"Satır {satir.RowNumber}: Malzeme adı boş."); hatali++; continue; }
            if (string.IsNullOrWhiteSpace(birim)) { hatalar.Add($"Satır {satir.RowNumber}: Ölçü birimi boş."); hatali++; continue; }

            var mevcut = await _uow.Malzemeler.GetByKodAsync(kod, ct);
            if (mevcut == null)
            {
                await _uow.Malzemeler.AddAsync(new Malzeme
                {
                    MalzemeKodu = kod,
                    MalzemeAdi = ad,
                    OlcuBirimi = birim,
                    AktifMi = true,
                    SonGuncellemeTarihi = DateTime.UtcNow,
                    GuncellemeKaynagi = "Import"
                }, ct);
                eklenen++;
            }
            else
            {
                mevcut.MalzemeAdi = ad;
                mevcut.OlcuBirimi = birim;
                mevcut.AktifMi = true;
                mevcut.SonGuncellemeTarihi = DateTime.UtcNow;
                mevcut.GuncellemeKaynagi = "Import";
                guncellenen++;
            }
        }

        await _uow.SaveChangesAsync(ct);
        return new MalzemeImportDto(toplam, eklenen, guncellenen, hatali, hatalar);
    }
}