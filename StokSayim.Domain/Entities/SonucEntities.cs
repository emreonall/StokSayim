using StokSayim.Domain.Enums;

namespace StokSayim.Domain.Entities;

public class TurSonucu : BaseEntity
{
    public int SayimTuruId { get; set; }
    public int ToplamMalzemeSayisi { get; set; }
    public int EslesilenSayisi { get; set; }
    public int FarkliSayisi { get; set; }
    public SayimTuruDurum GenelDurum { get; set; }
    public DateTime HesaplamaTarihi { get; set; } = DateTime.UtcNow;

    // Navigation
    public SayimTuru SayimTuru { get; set; } = null!;
    public ICollection<TurSonucuDetay> Detaylar { get; set; } = new List<TurSonucuDetay>();
}

public class TurSonucuDetay : BaseEntity
{
    public int TurSonucuId { get; set; }
    public string MalzemeKodu { get; set; } = string.Empty;
    public string? LotNo { get; set; }
    public string? SeriNo { get; set; }

    // Karşılaştırılan değerler
    public decimal? Deger1 { get; set; }  // Birinci Ekip / ERP
    public decimal? Deger2 { get; set; }  // İkinci Ekip / Fiili
    public decimal? Deger3 { get; set; }  // Kontrol Ekibi (varsa)
    public decimal? Fark { get; set; }
    public decimal? FarkYuzdesi { get; set; }

    public TurSonucuDetayDurum Durum { get; set; }
    public decimal? OnaylananDeger { get; set; }
    public KararTipi? KararTipi { get; set; }

    // Navigation
    public TurSonucu TurSonucu { get; set; } = null!;
    public ManuelKarar? ManuelKarar { get; set; }
}

public class ManuelKarar : BaseEntity
{
    public int TurSonucuDetayId { get; set; }
    public int SayimTuruId { get; set; }
    public string MalzemeKodu { get; set; } = string.Empty;
    public string? LotNo { get; set; }
    public decimal KararVerilenDeger { get; set; }
    public string Gerekce { get; set; } = string.Empty;
    public string KararVerenKullaniciId { get; set; } = string.Empty;
    public DateTime KararTarihi { get; set; } = DateTime.UtcNow;

    // Navigation
    public TurSonucuDetay TurSonucuDetay { get; set; } = null!;
    public SayimTuru SayimTuru { get; set; } = null!;
    public ApplicationUser KararVerenKullanici { get; set; } = null!;
}

public class GorevBildirimi : BaseEntity
{
    public int SayimOturumuId { get; set; }
    public int? SayimTuruId { get; set; }
    public GorevBildirimTipi BildirimTipi { get; set; }
    public GorevBildirimDurum Durum { get; set; } = GorevBildirimDurum.Beklemede;
    public string Mesaj { get; set; } = string.Empty;
    public DateTime? IslemTarihi { get; set; }
    public string? IsleyenKullaniciId { get; set; }

    // Navigation
    public SayimOturumu SayimOturumu { get; set; } = null!;
    public SayimTuru? SayimTuru { get; set; }
    public ApplicationUser? IsleyenKullanici { get; set; }
}
