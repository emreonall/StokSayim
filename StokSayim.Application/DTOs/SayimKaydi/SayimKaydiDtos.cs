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
    string MalzemeAdi,
    string? LotNo,
    string? SeriNo,
    decimal SayilanMiktar,
    string Birim,
    string? Notlar
);

public record SayimKaydiDetayEkleDto(
    string MalzemeKodu,
    string MalzemeAdi,
    string? LotNo,
    string? SeriNo,
    decimal SayilanMiktar,
    string Birim,
    string? Notlar
);
