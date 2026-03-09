namespace StokSayim.Application.DTOs.Ekip;

public record EkipDto(
    int Id,
    string EkipKodu,
    string EkipAdi,
    bool AktifMi,
    IEnumerable<EkipKullaniciDto> Kullanicilar
);

public record EkipKullaniciDto(
    string KullaniciId,
    string AdSoyad,
    string Email,
    bool AktifMi
);

public record EkipOlusturDto(
    string EkipKodu,
    string EkipAdi
);
