namespace StokSayim.Application.DTOs.Auth;

public record LoginRequestDto(string Email, string Sifre);

public record AuthResponseDto(
    string Token,
    string RefreshToken,
    DateTime TokenSonGecerlilik,
    string KullaniciId,
    string AdSoyad,
    string Email,
    IEnumerable<string> Roller
);

public record AktifGorevDto(
    string KullaniciId,
    string AdSoyad,
    int? EkipId,
    string? EkipAdi,
    EkipRoluDto? EkipRolu,
    int? BolgeId,
    string? BolgeAdi,
    int? SayimOturumuId,
    int? SayimTuruId,
    int? TurNo,
    string? TurTipi,
    int? SayimKaydiId,
    string? SayimKaydiDurum,
    bool GorevVar
);

public record EkipRoluDto(int Deger, string Ad);

public record KullaniciDto(
    string Id,
    string AdSoyad,
    string Email,
    bool AktifMi,
    IEnumerable<string> Roller,
    int? EkipId,
    string? EkipAdi
);

public record KullaniciOlusturDto(
    string AdSoyad,
    string Email,
    string Sifre,
    string Rol
);

public record KullaniciGuncelleDto(
    string AdSoyad,
    string Email,
    string? YeniSifre,
    string Rol
);
// Kullanıcının seçebileceği tek bir bölge/tur görevi
public record GorevSecenekDto(
    int EkipId,
    string EkipAdi,
    EkipRoluDto EkipRolu,
    int BolgeId,
    string BolgeAdi,
    string BolgeKodu,
    int SayimOturumuId,
    int SayimTuruId,
    int TurNo,
    string TurTipi,
    int? SayimKaydiId,
    string? SayimKaydiDurum,
    bool SayimTamamlandi
);

// Login sonrası dönen — seçim listesi
public record AktifGorevlerDto(
    string KullaniciId,
    string AdSoyad,
    List<GorevSecenekDto> Secenekler
);