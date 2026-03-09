namespace StokSayim.Domain.Entities;

public class Ekip : BaseEntity
{
    public string EkipKodu { get; set; } = string.Empty;
    public string EkipAdi { get; set; } = string.Empty;
    public bool AktifMi { get; set; } = true;

    // Navigation
    public ICollection<EkipKullanici> EkipKullanicilari { get; set; } = new List<EkipKullanici>();
    public ICollection<EkipGrubuEkip> EkipGrubuEkipler { get; set; } = new List<EkipGrubuEkip>();
    public ICollection<SayimTuruKatilimci> SayimTuruKatilimcilari { get; set; } = new List<SayimTuruKatilimci>();
}

public class EkipKullanici : BaseEntity
{
    public int EkipId { get; set; }
    public string KullaniciId { get; set; } = string.Empty;
    public DateTime BaslangicTarihi { get; set; } = DateTime.UtcNow;
    public DateTime? BitisTarihi { get; set; }
    public bool AktifMi { get; set; } = true;

    // Navigation
    public Ekip Ekip { get; set; } = null!;
    public ApplicationUser Kullanici { get; set; } = null!;
}
