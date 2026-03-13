using StokSayim.Domain.Enums;

namespace StokSayim.Domain.Entities;

public class SayimOturumu : BaseEntity
{
    public int BolgeId { get; set; }
    public int SayimPlaniId { get; set; }
    public SayimOturumuDurum Durum { get; set; } = SayimOturumuDurum.Beklemede;
    public int AktifTurNo { get; set; } = 0;
    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? KapanisTarihi { get; set; }
    public string? SorumluKullaniciId { get; set; }

    // Navigation
    public Bolge Bolge { get; set; } = null!;
    public SayimPlani SayimPlani { get; set; } = null!;
    public ICollection<SayimTuru> SayimTurlari { get; set; } = new List<SayimTuru>();
    public ICollection<GorevBildirimi> GorevBildirimleri { get; set; } = new List<GorevBildirimi>();
}

public class SayimTuru : BaseEntity
{
    public int SayimOturumuId { get; set; }
    public int TurNo { get; set; }
    public SayimTuruTip TurTipi { get; set; }
    public SayimTuruDurum Durum { get; set; } = SayimTuruDurum.Beklemede;
    public DateTime? AcilamaTarihi { get; set; }
    public DateTime? KapanmaTarihi { get; set; }
    public string? Notlar { get; set; }

    // Navigation
    public SayimOturumu SayimOturumu { get; set; } = null!;
    public ICollection<SayimTuruKatilimci> Katilimcilar { get; set; } = new List<SayimTuruKatilimci>();
    public ICollection<SayimKaydi> SayimKayitlari { get; set; } = new List<SayimKaydi>();
    public TurSonucu? TurSonucu { get; set; }
}

public class SayimTuruKatilimci : BaseEntity
{
    public int SayimTuruId { get; set; }
    public int EkipId { get; set; }
    public EkipRolu EkipRolu { get; set; }
    public int? SayimKaydiId { get; set; }

    // Navigation
    public SayimTuru SayimTuru { get; set; } = null!;
    public Ekip Ekip { get; set; } = null!;
    public SayimKaydi? SayimKaydi { get; set; }
}

public class SayimKaydi : BaseEntity
{
    public int SayimTuruId { get; set; }
    public int EkipId { get; set; }
    public EkipRolu EkipRolu { get; set; }
    public string SayimYapanKullaniciId { get; set; } = string.Empty;
    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? TamamlanmaTarihi { get; set; }
    public SayimKaydiDurum Durum { get; set; } = SayimKaydiDurum.Taslak;
    public string? Notlar { get; set; }

    // Navigation
    public SayimTuru SayimTuru { get; set; } = null!;
    public Ekip Ekip { get; set; } = null!;
    public ApplicationUser SayimYapanKullanici { get; set; } = null!;
    public ICollection<SayimKaydiDetay> Detaylar { get; set; } = new List<SayimKaydiDetay>();
}

public class SayimKaydiDetay : BaseEntity
{
    public int SayimKaydiId { get; set; }
    public string MalzemeKodu { get; set; } = string.Empty;
    public string? LotNo { get; set; }
    public string? SeriNo { get; set; }
    public decimal SayilanMiktar { get; set; }
    public string? Notlar { get; set; }

    // Navigation
    public SayimKaydi SayimKaydi { get; set; } = null!;
}
