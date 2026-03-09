namespace StokSayim.Domain.Entities;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    public DateTime? GuncellemeTarihi { get; set; }
    public string? OlusturanKullaniciId { get; set; }
    public string? GuncelleyenKullaniciId { get; set; }
}
