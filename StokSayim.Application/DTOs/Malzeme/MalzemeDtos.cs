namespace StokSayim.Application.DTOs.Malzeme;

public record MalzemeDto(
    int Id,
    string MalzemeKodu,
    string MalzemeAdi,
    string OlcuBirimi,
    bool AktifMi,
    DateTime SonGuncellemeTarihi,
    string GuncellemeKaynagi
);

public record MalzemeOzetDto(
    string MalzemeKodu,
    string MalzemeAdi,
    string OlcuBirimi
);

public record MalzemeImportDto(
    int ToplamSatir,
    int EklenenSatir,
    int GuncelleneSatir,
    int HataliSatir,
    List<string> Hatalar
);
