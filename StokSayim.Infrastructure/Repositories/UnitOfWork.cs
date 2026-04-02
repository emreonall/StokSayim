using Microsoft.EntityFrameworkCore.Storage;
using StokSayim.Application.Interfaces;
using StokSayim.Application.Interfaces.Repositories;
using StokSayim.Infrastructure.Data;

namespace StokSayim.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public ISayimPlaniRepository SayimPlanlari { get; }
    public IBolgeRepository Bolgeler { get; }
    public IEkipRepository Ekipler { get; }
    public IEkipGrubuRepository EkipGruplari { get; }
    public ISayimOturumuRepository SayimOturumlari { get; }
    public ISayimTuruRepository SayimTurlari { get; }
    public ISayimKaydiRepository SayimKayitlari { get; }
    public IErpStokRepository ErpStoklar { get; }
    public ITurSonucuRepository TurSonuclari { get; }
    public IGorevBildirimiRepository GorevBildirimleri { get; }
    public IMalzemeRepository Malzemeler { get; }
    public IErpKontrolOturumuRepository ErpKontrolOturumlari { get; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        SayimPlanlari = new SayimPlaniRepository(context);
        Bolgeler = new BolgeRepository(context);
        Ekipler = new EkipRepository(context);
        EkipGruplari = new EkipGrubuRepository(context);
        SayimOturumlari = new SayimOturumuRepository(context);
        SayimTurlari = new SayimTuruRepository(context);
        SayimKayitlari = new SayimKaydiRepository(context);
        ErpStoklar = new ErpStokRepository(context);
        TurSonuclari = new TurSonucuRepository(context);
        GorevBildirimleri = new GorevBildirimiRepository(context);
        Malzemeler = new MalzemeRepository(context);
        ErpKontrolOturumlari = new ErpKontrolOturumuRepository(context);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(CancellationToken.None);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _transaction = await _context.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}