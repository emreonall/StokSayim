using StokSayim.Domain.Enums;

namespace StokSayim.Application.DTOs.ErpKontrol;

public record ErpKontrolAtamaDto(
    string MalzemeKodu,
    string MalzemeAdi,
    string Birim,
    decimal ErpMiktar,
    decimal FiiliMiktar,
    decimal Fark,
    int? AtananEkipId
);

public record ErpKontrolBaslatDto(
    IEnumerable<ErpKontrolEkipAtamaDto> EkipAtamalari
);

public record ErpKontrolEkipAtamaDto(
    int EkipId,
    IEnumerable<string> MalzemeKodlari
);

public record ErpKontrolEkipDetayDto(
    int ErpKontrolEkipId,
    int EkipId,
    string EkipAdi,
    ErpKontrolEkipDurum Durum,
    IEnumerable<ErpKontrolMalzemeDto> Malzemeler
);

public record ErpKontrolMalzemeDto(
    int Id,
    string MalzemeKodu,
    string MalzemeAdi,
    string Birim,
    decimal? SayilanMiktar,
    bool Tamamlandi
);

public record ErpKontrolMalzemeSayimDto(
    int MalzemeId,
    decimal SayilanMiktar
);

public record ErpKontrolOturumuDto(
    int Id,
    int SayimPlaniId,
    ErpKontrolOturumuDurum Durum,
    DateTime OlusturmaTarihi,
    DateTime? TamamlanmaTarihi,
    IEnumerable<ErpKontrolEkipOzetDto> Ekipler
);

public record ErpKontrolEkipOzetDto(
    int Id,
    int EkipId,
    string EkipAdi,
    ErpKontrolEkipDurum Durum,
    int ToplamMalzeme,
    int TamamlananMalzeme
);

public record ErpKontrolSonucDto(
    string MalzemeKodu,
    string MalzemeAdi,
    string Birim,
    decimal ErpMiktar,
    decimal KontrolSayimMiktar,
    decimal Fark,
    decimal FarkYuzdesi
);

public record ErpKontrolImportSonucDto(
    bool Basarili,
    int Guncellenen,
    int HataliSatir,
    IEnumerable<string> Hatalar
);
