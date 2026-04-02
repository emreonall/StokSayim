namespace StokSayim.Domain.Enums;

public enum SayimPlaniDurum
{
    Taslak = 0,
    Aktif = 1,
    SayimTamamlandi = 2,
    ErpKarsilastirmaAktif = 3,
    Kapali = 4,
    Iptal = 5
}

public enum SayimOturumuDurum
{
    Beklemede = 0,
    DevamEdiyor = 1,
    Onaylandi = 2,
    ManuelKarar = 3
}

public enum SayimTuruTip
{
    EkipKarsilastirma = 0,
    EkipKontrol = 1,
    ErpKarsilastirma = 2,
    ErpKontrol = 3
}

public enum SayimTuruDurum
{
    Beklemede = 0,
    DevamEdiyor = 1,
    KarsilastirmaBekliyor = 2,
    Onaylandi = 3,
    FarkVar = 4,
    ManuelKarar = 5
}

public enum SayimKaydiDurum
{
    Taslak = 0,
    Devam = 1,
    Tamamlandi = 2
}

public enum TurSonucuDetayDurum
{
    Eslesti = 0,
    FarkVar = 1,
    SadeceEkip1de = 2,
    SadeceEkip2de = 3
}

public enum KararTipi
{
    Otomatik = 0,
    Manuel = 1
}

public enum EkipRolu
{
    Birinci = 1,
    Ikinci = 2,
    Kontrol = 3
}

public enum GorevBildirimTipi
{
    KontrolSayimiGerekli = 0,
    ManuelKararGerekli = 1,
    ErpKontrolGerekli = 2,
    ErpManuelKararGerekli = 3
}

public enum GorevBildirimDurum
{
    Beklemede = 0,
    Islendi = 1
}

public enum ErpKontrolOturumuDurum
{
    Beklemede = 0,
    DevamEdiyor = 1,
    Tamamlandi = 2
}

public enum ErpKontrolEkipDurum
{
    Beklemede = 0,
    DevamEdiyor = 1,
    Tamamlandi = 2
}
