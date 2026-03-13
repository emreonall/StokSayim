using StokSayim.Domain.Enums;

namespace StokSayim.Domain.Entities;

public class SayimPlani : BaseEntity
{
    public string PlanAdi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public SayimPlaniDurum Durum { get; set; } = SayimPlaniDurum.Taslak;
    public DateTime? AktifEdilemeTarihi { get; set; }
    public DateTime? KapanisTarihi { get; set; }
    public string? AktifEdenKullaniciId { get; set; }

    // Navigation
    public ICollection<SayimPlanDepoKodu> DepoKodlari { get; set; } = new List<SayimPlanDepoKodu>();
    public ICollection<ErpStok> ErpStoklar { get; set; } = new List<ErpStok>();
    public ICollection<Bolge> Bolgeler { get; set; } = new List<Bolge>();
}

public class SayimPlanDepoKodu : BaseEntity
{
    public int SayimPlaniId { get; set; }
    public string DepoKodu { get; set; } = string.Empty;
    public string? DepoAdi { get; set; }

    // Navigation
    public SayimPlani SayimPlani { get; set; } = null!;
}

public class ErpStok : BaseEntity
{
    public int SayimPlaniId { get; set; }
    public string MalzemeKodu { get; set; } = string.Empty;
    public string DepoKodu { get; set; } = string.Empty;
    public decimal Miktar { get; set; }
    public string? LotNo { get; set; }
    public string? SeriNo { get; set; }
    public DateTime ImportTarihi { get; set; } = DateTime.UtcNow;
    public string? ImportDosyaAdi { get; set; }

    // Navigation
    public SayimPlani SayimPlani { get; set; } = null!;
}
