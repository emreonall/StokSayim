using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StokSayim.Domain.Entities;

namespace StokSayim.Infrastructure.Data.Configurations;

public class SayimPlaniConfiguration : IEntityTypeConfiguration<SayimPlani>
{
    public void Configure(EntityTypeBuilder<SayimPlani> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.PlanAdi).HasMaxLength(200).IsRequired();
        b.Property(x => x.Aciklama).HasMaxLength(500);
        b.HasMany(x => x.DepoKodlari).WithOne(x => x.SayimPlani).HasForeignKey(x => x.SayimPlaniId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.ErpStoklar).WithOne(x => x.SayimPlani).HasForeignKey(x => x.SayimPlaniId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Bolgeler).WithOne(x => x.SayimPlani).HasForeignKey(x => x.SayimPlaniId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ErpStokConfiguration : IEntityTypeConfiguration<ErpStok>
{
    public void Configure(EntityTypeBuilder<ErpStok> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.MalzemeKodu).HasMaxLength(50).IsRequired();
        b.Property(x => x.MalzemeAdi).HasMaxLength(200).IsRequired();
        b.Property(x => x.DepoKodu).HasMaxLength(20).IsRequired();
        b.Property(x => x.Miktar).HasPrecision(18, 4);
        b.Property(x => x.Birim).HasMaxLength(10);
        b.Property(x => x.LotNo).HasMaxLength(50);
        b.Property(x => x.SeriNo).HasMaxLength(50);
        b.HasIndex(x => new { x.SayimPlaniId, x.MalzemeKodu, x.DepoKodu, x.LotNo });
    }
}

public class BolgeConfiguration : IEntityTypeConfiguration<Bolge>
{
    public void Configure(EntityTypeBuilder<Bolge> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.BolgeKodu).HasMaxLength(20).IsRequired();
        b.Property(x => x.BolgeAdi).HasMaxLength(100).IsRequired();
        b.HasOne(x => x.EkipGrubu).WithOne(x => x.Bolge).HasForeignKey<EkipGrubu>(x => x.BolgeId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.SayimOturumu).WithOne(x => x.Bolge).HasForeignKey<SayimOturumu>(x => x.BolgeId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SayimPlaniId, x.BolgeKodu }).IsUnique();
    }
}

public class EkipConfiguration : IEntityTypeConfiguration<Ekip>
{
    public void Configure(EntityTypeBuilder<Ekip> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.EkipKodu).HasMaxLength(20).IsRequired();
        b.Property(x => x.EkipAdi).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.EkipKodu).IsUnique();
        b.HasMany(x => x.EkipKullanicilari).WithOne(x => x.Ekip).HasForeignKey(x => x.EkipId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class EkipGrubuConfiguration : IEntityTypeConfiguration<EkipGrubu>
{
    public void Configure(EntityTypeBuilder<EkipGrubu> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.EkipGrubuAdi).HasMaxLength(100).IsRequired();
        b.HasMany(x => x.Ekipler).WithOne(x => x.EkipGrubu).HasForeignKey(x => x.EkipGrubuId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SayimOturumuConfiguration : IEntityTypeConfiguration<SayimOturumu>
{
    public void Configure(EntityTypeBuilder<SayimOturumu> b)
    {
        b.HasKey(x => x.Id);
        b.HasMany(x => x.SayimTurleri).WithOne(x => x.SayimOturumu).HasForeignKey(x => x.SayimOturumuId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.GorevBildirimleri).WithOne(x => x.SayimOturumu).HasForeignKey(x => x.SayimOturumuId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SayimTuruConfiguration : IEntityTypeConfiguration<SayimTuru>
{
    public void Configure(EntityTypeBuilder<SayimTuru> b)
    {
        b.HasKey(x => x.Id);
        b.HasMany(x => x.Katilimcilar).WithOne(x => x.SayimTuru).HasForeignKey(x => x.SayimTuruId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.SayimKayitlari).WithOne(x => x.SayimTuru).HasForeignKey(x => x.SayimTuruId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TurSonucu).WithOne(x => x.SayimTuru).HasForeignKey<TurSonucu>(x => x.SayimTuruId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SayimKaydiConfiguration : IEntityTypeConfiguration<SayimKaydi>
{
    public void Configure(EntityTypeBuilder<SayimKaydi> b)
    {
        b.HasKey(x => x.Id);
        b.HasMany(x => x.Detaylar).WithOne(x => x.SayimKaydi).HasForeignKey(x => x.SayimKaydiId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SayimKaydiDetayConfiguration : IEntityTypeConfiguration<SayimKaydiDetay>
{
    public void Configure(EntityTypeBuilder<SayimKaydiDetay> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.MalzemeKodu).HasMaxLength(50).IsRequired();
        b.Property(x => x.MalzemeAdi).HasMaxLength(200).IsRequired();
        b.Property(x => x.SayilanMiktar).HasPrecision(18, 4);
        b.Property(x => x.LotNo).HasMaxLength(50);
        b.Property(x => x.SeriNo).HasMaxLength(50);
        b.Property(x => x.Birim).HasMaxLength(10);
    }
}

public class ManuelKararConfiguration : IEntityTypeConfiguration<ManuelKarar>
{
    public void Configure(EntityTypeBuilder<ManuelKarar> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.KararVerilenDeger).HasPrecision(18, 2);
        // Cascade döngüsünü önlemek için NoAction
        b.HasOne(x => x.TurSonucuDetay).WithOne(x => x.ManuelKarar)
            .HasForeignKey<ManuelKarar>(x => x.TurSonucuDetayId)
            .OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.SayimTuru).WithMany()
            .HasForeignKey(x => x.SayimTuruId)
            .OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.KararVerenKullanici).WithMany()
            .HasForeignKey(x => x.KararVerenKullaniciId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class TurSonucuDetayConfiguration : IEntityTypeConfiguration<TurSonucuDetay>
{
    public void Configure(EntityTypeBuilder<TurSonucuDetay> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Deger1).HasPrecision(18, 4);
        b.Property(x => x.Deger2).HasPrecision(18, 4);
        b.Property(x => x.Fark).HasPrecision(18, 4);
        b.Property(x => x.FarkYuzdesi).HasPrecision(18, 4);
        b.Property(x => x.OnaylananDeger).HasPrecision(18, 4);
        b.HasOne(x => x.ManuelKarar).WithOne(x => x.TurSonucuDetay).HasForeignKey<ManuelKarar>(x => x.TurSonucuDetayId).OnDelete(DeleteBehavior.NoAction);
    }
}