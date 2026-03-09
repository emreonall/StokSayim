using StokSayim.Domain.Enums;

namespace StokSayim.Application.DTOs.SayimPlani;

public record SayimPlaniListDto(
    int Id,
    string PlanAdi,
    string? Aciklama,
    DateTime BaslangicTarihi,
    DateTime BitisTarihi,
    SayimPlaniDurum Durum,
    string DurumAdi,
    int BolgeSayisi,
    int TamamlananBolgeSayisi,
    DateTime OlusturmaTarihi
);

public record SayimPlaniDetayDto(
    int Id,
    string PlanAdi,
    string? Aciklama,
    DateTime BaslangicTarihi,
    DateTime BitisTarihi,
    SayimPlaniDurum Durum,
    string DurumAdi,
    IEnumerable<string> DepoKodlari,
    int ErpStokSatirSayisi,
    DateTime? ErpImportTarihi,
    DateTime OlusturmaTarihi
);

public record SayimPlaniOlusturDto(
    string PlanAdi,
    string? Aciklama,
    DateTime BaslangicTarihi,
    DateTime BitisTarihi,
    IEnumerable<string> DepoKodlari
);

public record SayimPlaniGuncelleDto(
    string PlanAdi,
    string? Aciklama,
    DateTime BaslangicTarihi,
    DateTime BitisTarihi,
    IEnumerable<string> DepoKodlari
);

public record ErpImportSonucDto(
    bool Basarili,
    int IslenenSatir,
    int EklenenSatir,
    int HataliSatir,
    IEnumerable<string> Hatalar,
    string DosyaAdi,
    DateTime ImportTarihi
);
