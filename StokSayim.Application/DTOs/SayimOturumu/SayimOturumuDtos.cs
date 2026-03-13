using StokSayim.Domain.Enums;

namespace StokSayim.Application.DTOs.SayimOturumu;

public record SayimOturumuDetayDto(
    int Id,
    int BolgeId,
    string BolgeAdi,
    int SayimPlaniId,
    SayimOturumuDurum Durum,
    string DurumAdi,
    int AktifTurNo,
    DateTime? BaslangicTarihi,
    DateTime? KapanisTarihi,
    IEnumerable<SayimTuruOzetDto> Turler
);

public record SayimTuruOzetDto(
    int Id,
    int TurNo,
    SayimTuruTip TurTipi,
    string TurTipiAdi,
    SayimTuruDurum Durum,
    string DurumAdi,
    DateTime? AcilamaTarihi,
    DateTime? KapanmaTarihi,
    IEnumerable<KatilimciOzetDto> Katilimcilar,
    TurSonucuOzetDto? Sonuc
);

public record KatilimciOzetDto(
    int EkipId,
    string EkipAdi,
    EkipRolu EkipRolu,
    string EkipRoluAdi,
    bool SayimTamamlandi,
    int? SayimKaydiId
);

public record TurSonucuOzetDto(
    int Id,
    int ToplamMalzeme,
    int Eslesen,
    int Farkli,
    string GenelDurum
);

public record GorevBildirimDto(
    int Id,
    int SayimOturumuId,
    int BolgeId,
    string BolgeAdi,
    int? SayimTuruId,
    int? TurNo,
    GorevBildirimTipi BildirimTipi,
    string BildirimTipiAdi,
    string Mesaj,
    DateTime OlusturmaTarihi
);

public record KontrolTuruAcDto(
    IEnumerable<KontrolEkipDto> Ekipler,
    string? Notlar
);

public record KontrolEkipDto(
    int EkipId,
    EkipRolu EkipRolu
);

public record ManuelKararDto(
    decimal KararVerilenDeger,
    string Gerekce
);
public record TurSonucuDetayDto(
    int Id,
    string MalzemeKodu,
    string MalzemeAdi,    // Malzeme tablosundan join
    string? LotNo,
    string? SeriNo,
    string OlcuBirimi,    // Malzeme tablosundan join
    decimal? Deger1,
    decimal? Deger2,
    decimal? Deger3,
    decimal? Fark,
    decimal? FarkYuzdesi,
    string Durum,
    decimal? OnaylananDeger,
    string? KararTipi,
    string? ManuelGerekce
);

public record TurSonucuDto(
    int Id,
    int SayimTuruId,
    int TurNo,
    string TurTipi,
    int ToplamMalzemeSayisi,
    int EslesilenSayisi,
    int FarkliSayisi,
    string GenelDurum,
    DateTime HesaplamaTarihi,
    List<TurSonucuDetayDto> Detaylar
);