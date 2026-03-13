using StokSayim.Domain.Enums;

namespace StokSayim.Application.DTOs.SayimKaydi;

public record SayimKaydiDto(
    int Id,
    int SayimTuruId,
    int EkipId,
    string EkipAdi,
    EkipRolu EkipRolu,
    string SayimYapanAdSoyad,
    DateTime? BaslangicTarihi,
    DateTime? TamamlanmaTarihi,
    SayimKaydiDurum Durum,
    string DurumAdi,
    string? Notlar,
    IEnumerable<SayimKaydiDetayDto> Detaylar
);

public record SayimKaydiDetayDto(
    int Id,
    string MalzemeKodu,
    string MalzemeAdi,   // Malzeme tablosundan join
    string? LotNo,
    string? SeriNo,
    decimal SayilanMiktar,
    string OlcuBirimi,   // Malzeme tablosundan join
    string? Notlar
);

public record SayimKaydiDetayEkleDto(
    string MalzemeKodu,
    string? LotNo,
    string? SeriNo,
    decimal SayilanMiktar,
    string? Notlar
);
public record AcikSayimKaydiDto(
    int KatilimciId,   // SayimTuruKatilimci.Id — import için anahtar
    int EkipId,
    int SayimTuruId,
    string EkipAdi,
    string EkipRoluAdi,
    string BolgeAdi,
    int TurNo,
    string KullaniciAdlari  // Ekipteki kişiler
);

public record OfflineImportSonucDto(
    bool Basarili,
    int KaydiId,
    int TurId,
    int EklenenSatir,
    int HataliSatir,
    List<string> Hatalar,
    bool KarsilastirmaTetiklendi
);

public record TopluDetayEkleSonucDto(
    int KaydiId,
    int EklenenSatir,
    int HataliSatir,
    List<string> Hatalar
);