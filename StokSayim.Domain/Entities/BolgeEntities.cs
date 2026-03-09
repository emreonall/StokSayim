using StokSayim.Domain.Enums;

namespace StokSayim.Domain.Entities;

public class Bolge : BaseEntity
{
    public int SayimPlaniId { get; set; }
    public string BolgeKodu { get; set; } = string.Empty;
    public string BolgeAdi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }

    // Navigation
    public SayimPlani SayimPlani { get; set; } = null!;
    public EkipGrubu? EkipGrubu { get; set; }
    public SayimOturumu? SayimOturumu { get; set; }
}

public class EkipGrubu : BaseEntity
{
    public int BolgeId { get; set; }
    public int SayimPlaniId { get; set; }
    public string EkipGrubuAdi { get; set; } = string.Empty;

    // Navigation
    public Bolge Bolge { get; set; } = null!;
    public SayimPlani SayimPlani { get; set; } = null!;
    public ICollection<EkipGrubuEkip> Ekipler { get; set; } = new List<EkipGrubuEkip>();
}

public class EkipGrubuEkip : BaseEntity
{
    public int EkipGrubuId { get; set; }
    public int EkipId { get; set; }
    public int SiraNo { get; set; }
    public EkipRolu EkipRolu { get; set; }

    // Navigation
    public EkipGrubu EkipGrubu { get; set; } = null!;
    public Ekip Ekip { get; set; } = null!;
}
