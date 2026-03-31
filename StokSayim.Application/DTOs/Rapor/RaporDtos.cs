using StokSayim.Domain.Enums;

namespace StokSayim.Application.DTOs.Rapor;

public record SayimDurumRaporDto(
    int PlanId,
    string PlanAdi,
    SayimPlaniDurum PlanDurum,
    int ToplamBolge,
    int TamamlananBolge,
    int DevamEdenBolge,
    int BekleyenBolge,
    IEnumerable<BolgeDurumDto> BolgeDurumlari,
    IEnumerable<EkipSayimOzetDto> EkipSayimOzetleri
);

public record BolgeDurumDto(
    int BolgeId,
    string BolgeKodu,
    string BolgeAdi,
    string OturumDurum,
    int TamamlananTurSayisi,
    int ToplamTurSayisi,
    bool ErpKarsilastirmaYapildiMi
);

public record EkipSayimOzetDto(
    int EkipId,
    string EkipAdi,
    int TamamlananSayimSayisi,
    int ToplamSayimSayisi
);

public record KesinFarkRaporDto(
    int PlanId,
    string PlanAdi,
    DateTime RaporTarihi,
    int ToplamSatir,
    int FarksizSatir,
    int FarkliSatir,
    IEnumerable<BolgeDurumDto> BolgeDurumlari,
    IEnumerable<EkipSayimOzetDto> EkipSayimOzetleri,
    IEnumerable<FarkDetayDto> FarkDetaylari
);

public record FarkDetayDto(
    string MalzemeKodu,
    string MalzemeAdi,
    string Birim,
    decimal ErpMiktar,      // ERP'de yoksa 0
    decimal FiiliMiktar,    // Sayımda yoksa 0
    decimal Fark,
    decimal FarkYuzdesi,
    KararTipi? KararTipi,
    string? ManuelKararGerekce
);
