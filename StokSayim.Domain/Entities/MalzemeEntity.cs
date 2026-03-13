namespace StokSayim.Domain.Entities;

public class Malzeme : BaseEntity
{
    public string MalzemeKodu { get; set; } = string.Empty;
    public string MalzemeAdi { get; set; } = string.Empty;
    public string OlcuBirimi { get; set; } = string.Empty;
    public bool AktifMi { get; set; } = true;
    public DateTime SonGuncellemeTarihi { get; set; } = DateTime.UtcNow;
    public string GuncellemeKaynagi { get; set; } = string.Empty; // "Import" | "Manuel"
}
