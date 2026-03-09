using StokSayim.Domain.Enums;

namespace StokSayim.Application.DTOs.Bolge;

public record BolgeDto(
    int Id,
    int SayimPlaniId,
    string BolgeKodu,
    string BolgeAdi,
    string? Aciklama,
    bool EkipGrubuVarMi,
    bool SayimOturumuVarMi,
    string? OturumDurum
);

public record BolgeDetayDto(
    int Id,
    int SayimPlaniId,
    string BolgeKodu,
    string BolgeAdi,
    string? Aciklama,
    EkipGrubuDto? EkipGrubu,
    SayimOturumuOzetDto? SayimOturumu
);

public record BolgeOlusturDto(
    int SayimPlaniId,
    string BolgeKodu,
    string BolgeAdi,
    string? Aciklama
);

public record EkipGrubuDto(
    int Id,
    string EkipGrubuAdi,
    IEnumerable<EkipGrubuEkipDto> Ekipler
);

public record EkipGrubuEkipDto(
    int EkipId,
    string EkipAdi,
    int SiraNo,
    EkipRolu EkipRolu,
    string EkipRoluAdi
);

public record EkipGrubuAtaDto(
    string EkipGrubuAdi,
    IEnumerable<EkipGrubuEkipAtaDto> Ekipler
);

public record EkipGrubuEkipAtaDto(
    int EkipId,
    int SiraNo,
    EkipRolu EkipRolu
);

public record SayimOturumuOzetDto(
    int Id,
    SayimOturumuDurum Durum,
    string DurumAdi,
    int AktifTurNo,
    string? AktifTurTipi,
    DateTime? BaslangicTarihi
);
