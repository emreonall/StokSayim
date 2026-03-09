using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using StokSayim.Domain.Entities;

namespace StokSayim.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Ekip> Ekipler => Set<Ekip>();
    public DbSet<EkipKullanici> EkipKullanicilari => Set<EkipKullanici>();
    public DbSet<EkipGrubu> EkipGruplari => Set<EkipGrubu>();
    public DbSet<EkipGrubuEkip> EkipGrubuEkipler => Set<EkipGrubuEkip>();
    public DbSet<SayimPlani> SayimPlanlari => Set<SayimPlani>();
    public DbSet<SayimPlanDepoKodu> SayimPlanDepoKodlari => Set<SayimPlanDepoKodu>();
    public DbSet<ErpStok> ErpStoklar => Set<ErpStok>();
    public DbSet<Bolge> Bolgeler => Set<Bolge>();
    public DbSet<SayimOturumu> SayimOturumlari => Set<SayimOturumu>();
    public DbSet<SayimTuru> SayimTurlari => Set<SayimTuru>();
    public DbSet<SayimTuruKatilimci> SayimTuruKatilimcilari => Set<SayimTuruKatilimci>();
    public DbSet<SayimKaydi> SayimKayitlari => Set<SayimKaydi>();
    public DbSet<SayimKaydiDetay> SayimKaydiDetaylari => Set<SayimKaydiDetay>();
    public DbSet<TurSonucu> TurSonuclari => Set<TurSonucu>();
    public DbSet<TurSonucuDetay> TurSonucuDetaylari => Set<TurSonucuDetay>();
    public DbSet<ManuelKarar> ManuelKararlar => Set<ManuelKarar>();
    public DbSet<GorevBildirimi> GorevBildirimleri => Set<GorevBildirimi>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

// Design-time factory — migration için gerekli
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=172.17.3.50\\SQLEXPRESS;Database=StokSayimDb;User Id=emre.onal;Password=K0p@s.22;MultipleActiveResultSets=true;TrustServerCertificate=True",
         b => b.MigrationsAssembly("StokSayim.Infrastructure"));
        DbContextOptions<AppDbContext> options = optionsBuilder.Options;
        return new AppDbContext(options);
    }
}