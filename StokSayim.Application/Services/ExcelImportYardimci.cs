using ClosedXML.Excel;

namespace StokSayim.Application.Services;

internal static class ExcelImportYardimci
{
    /// <summary>
    /// Şablon sayfasındaki VERİ satırlarını döner.
    /// Başlık satırı ("MalzemeKodu ..." ile başlayan satır) bulunur, ondan önceki bilgi satırları ve
    /// başlıktan sonra gelebilecek açıklama satırı ("... (zorunlu)" / "(opsiyonel)") atlanır.
    /// Böylece şablondaki açıklama satırı silinmiş olsa bile ilk veri satırı kaybolmaz.
    /// </summary>
    public static List<IXLRangeRow> VeriSatirlari(IXLWorksheet ws)
    {
        var tum = ws.RangeUsed()?.RowsUsed().ToList() ?? new List<IXLRangeRow>();

        var baslikIndex = tum.FindIndex(r =>
            r.Cell(1).GetString().Trim().StartsWith("MalzemeKodu", StringComparison.OrdinalIgnoreCase));

        // Başlık bulunamazsa eski davranış: bilgi başlığı + kolon adları + açıklama = ilk 3 satır atlanır
        var baslangic = baslikIndex >= 0 ? baslikIndex + 1 : Math.Min(3, tum.Count);

        return tum.Skip(baslangic).Where(r => !AciklamaSatiriMi(r)).ToList();
    }

    private static bool AciklamaSatiriMi(IXLRangeRow satir)
    {
        var ilkHucre = satir.Cell(1).GetString();
        return ilkHucre.Contains("(zorunlu", StringComparison.OrdinalIgnoreCase)
            || ilkHucre.Contains("(opsiyonel", StringComparison.OrdinalIgnoreCase);
    }
}
