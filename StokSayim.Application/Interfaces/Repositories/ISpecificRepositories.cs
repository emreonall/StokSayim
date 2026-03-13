using StokSayim.Domain.Entities;
using StokSayim.Domain.Enums;

namespace StokSayim.Application.Interfaces.Repositories;

public interface ISayimPlaniRepository : IRepository<SayimPlani>
{
    Task<SayimPlani?> GetWithDetailsAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<SayimPlani>> GetAktifPlanlarAsync(CancellationToken ct = default);
}

public interface IBolgeRepository : IRepository<Bolge>
{
    Task<IEnumerable<Bolge>> GetByPlanIdAsync(int planId, CancellationToken ct = default);
    Task<Bolge?> GetWithOturumAsync(int id, CancellationToken ct = default);
}

public interface IEkipRepository : IRepository<Ekip>
{
    Task<IEnumerable<Ekip>> GetAktifEkiplerAsync(CancellationToken ct = default);
    Task<Ekip?> GetWithKullaniciarAsync(int id, CancellationToken ct = default);
    Task<Ekip?> GetByKullaniciIdAsync(string kullaniciId, CancellationToken ct = default);
    Task AddKullaniciAsync(EkipKullanici kayit, CancellationToken ct = default);
}

public interface IEkipGrubuRepository : IRepository<EkipGrubu>
{
    Task<EkipGrubu?> GetWithEkiplerAsync(int id, CancellationToken ct = default);
    Task<EkipGrubu?> GetByBolgeIdAsync(int bolgeId, CancellationToken ct = default);
}

public interface ISayimOturumuRepository : IRepository<SayimOturumu>
{
    Task<SayimOturumu?> GetWithTurlerAsync(int id, CancellationToken ct = default);
    Task<SayimOturumu?> GetByBolgeIdAsync(int bolgeId, CancellationToken ct = default);
    Task<IEnumerable<SayimOturumu>> GetByPlanIdAsync(int planId, CancellationToken ct = default);
}

public interface ISayimTuruRepository : IRepository<SayimTuru>
{
    Task<SayimTuru?> GetWithKatilimcilarAsync(int id, CancellationToken ct = default);
    Task<SayimTuru?> GetAktifTurByOturumuAsync(int oturumuId, CancellationToken ct = default);
}

public interface ISayimKaydiRepository : IRepository<SayimKaydi>
{
    Task<SayimKaydi?> GetWithDetaylarAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<SayimKaydi>> GetByTurIdAsync(int turId, CancellationToken ct = default);
    Task<SayimKaydi?> GetAktifKaydiByKullaniciAsync(string kullaniciId, CancellationToken ct = default);
    Task AddDetayAsync(SayimKaydiDetay detay, CancellationToken ct = default);
    Task AddDetayRangeAsync(IEnumerable<SayimKaydiDetay> detaylar, CancellationToken ct = default);
    void DeleteDetay(SayimKaydiDetay detay);
}

public interface IErpStokRepository : IRepository<ErpStok>
{
    Task<IEnumerable<ErpStok>> GetByPlanIdAsync(int planId, CancellationToken ct = default);
    Task<IEnumerable<ErpStok>> GetByPlanAndDepoAsync(int planId, IEnumerable<string> depKodlari, CancellationToken ct = default);
    Task DeleteByPlanIdAsync(int planId, CancellationToken ct = default);
}

public interface ITurSonucuRepository : IRepository<TurSonucu>
{
    Task<TurSonucu?> GetWithDetaylarAsync(int id, CancellationToken ct = default);
    Task<TurSonucu?> GetByTurIdAsync(int turId, CancellationToken ct = default);
}

public interface IGorevBildirimiRepository : IRepository<GorevBildirimi>
{
    Task<IEnumerable<GorevBildirimi>> GetBekleyenlerAsync(CancellationToken ct = default);
    Task<IEnumerable<GorevBildirimi>> GetBekleyenlerByOturumAsync(int oturumuId, GorevBildirimTipi tip, CancellationToken ct = default);
    Task<IEnumerable<GorevBildirimi>> GetByOturumuIdAsync(int oturumuId, CancellationToken ct = default);
}
public interface IMalzemeRepository
{
    Task<Malzeme?> GetByKodAsync(string malzemeKodu, CancellationToken ct = default);
    Task<Dictionary<string, Malzeme>> GetDictionaryByKodlarAsync(IEnumerable<string> kodlar, CancellationToken ct = default);
    Task<IEnumerable<Malzeme>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Malzeme malzeme, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Malzeme> malzemeler, CancellationToken ct = default);
}