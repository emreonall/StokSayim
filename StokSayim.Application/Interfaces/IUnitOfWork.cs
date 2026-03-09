using StokSayim.Application.Interfaces.Repositories;

namespace StokSayim.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    ISayimPlaniRepository SayimPlanlari { get; }
    IBolgeRepository Bolgeler { get; }
    IEkipRepository Ekipler { get; }
    IEkipGrubuRepository EkipGruplari { get; }
    ISayimOturumuRepository SayimOturumlari { get; }
    ISayimTuruRepository SayimTurleri { get; }
    ISayimKaydiRepository SayimKayitlari { get; }
    IErpStokRepository ErpStoklar { get; }
    ITurSonucuRepository TurSonuclari { get; }
    IGorevBildirimiRepository GorevBildirimleri { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
