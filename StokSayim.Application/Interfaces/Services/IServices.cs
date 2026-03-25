using StokSayim.Application.DTOs.Auth;
using StokSayim.Application.DTOs.Malzeme;
using StokSayim.Application.DTOs.SayimPlani;
using StokSayim.Application.DTOs.Bolge;
using StokSayim.Application.DTOs.Ekip;
using StokSayim.Application.DTOs.SayimOturumu;
using StokSayim.Application.DTOs.SayimKaydi;
using StokSayim.Application.DTOs.Rapor;
using StokSayim.Domain.Entities;

namespace StokSayim.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
    Task<AuthResponseDto> RefreshTokenAsync(string token, CancellationToken ct = default);
    Task<AktifGorevDto?> GetAktifGorevAsync(string kullaniciId, CancellationToken ct = default);
    Task<AktifGorevlerDto> GetAktifGorevlerAsync(string kullaniciId, CancellationToken ct = default);
}

public interface IKullaniciService
{
    Task<IEnumerable<KullaniciDto>> GetAllAsync(CancellationToken ct = default);
    Task<KullaniciDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<KullaniciDto> CreateAsync(KullaniciOlusturDto request, CancellationToken ct = default);
    Task UpdateAsync(string id, KullaniciGuncelleDto request, CancellationToken ct = default);
    Task SetAktifAsync(string id, bool aktif, CancellationToken ct = default);
}

public interface IEkipService
{
    Task<IEnumerable<EkipDto>> GetAllAsync(CancellationToken ct = default);
    Task<Ekip?> GetEkipByKullaniciIdAsync(string kullaniciId, CancellationToken ct = default);
    Task<EkipDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<EkipDto> CreateAsync(EkipOlusturDto request, CancellationToken ct = default);
    Task UpdateAsync(int id, EkipOlusturDto request, CancellationToken ct = default);
    Task KullaniciEkleAsync(int ekipId, string kullaniciId, CancellationToken ct = default);
    Task KullaniciCikarAsync(int ekipId, string kullaniciId, CancellationToken ct = default);
}

public interface ISayimPlaniService
{
    Task<IEnumerable<SayimPlaniListDto>> GetAllAsync(CancellationToken ct = default);
    Task<SayimPlaniDetayDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<SayimPlaniDetayDto> CreateAsync(SayimPlaniOlusturDto request, string kullaniciId, CancellationToken ct = default);
    Task UpdateAsync(int id, SayimPlaniGuncelleDto request, CancellationToken ct = default);
    Task AktifEtAsync(int id, string kullaniciId, CancellationToken ct = default);
    Task SayimiTamamlaAsync(int id, string kullaniciId, CancellationToken ct = default);
    Task<ErpImportSonucDto> ImportErpStokAsync(int id, Stream dosya, string dosyaAdi, string kullaniciId, CancellationToken ct = default);
}

public interface IBolgeService
{
    Task<IEnumerable<BolgeDto>> GetByPlanIdAsync(int planId, CancellationToken ct = default);
    Task<BolgeDetayDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<BolgeDto> CreateAsync(BolgeOlusturDto request, string kullaniciId, CancellationToken ct = default);
    Task UpdateAsync(int id, BolgeOlusturDto request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task EkipGrubuAtaAsync(int bolgeId, EkipGrubuAtaDto request, string kullaniciId, CancellationToken ct = default);
}

public interface ISayimOturumuService
{
    Task<SayimOturumuDetayDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<SayimOturumuDetayDto?> GetByBolgeIdAsync(int bolgeId, CancellationToken ct = default);
    Task BaslatAsync(int bolgeId, string kullaniciId, CancellationToken ct = default);
    Task<IEnumerable<GorevBildirimDto>> GetBekleyenBildirimlerAsync(CancellationToken ct = default);
    Task KontrolTuruAcAsync(int oturumuId, KontrolTuruAcDto request, string kullaniciId, CancellationToken ct = default);
    Task<TurSonucuDto?> GetTurSonucuAsync(int turId, CancellationToken ct = default);
    Task ManuelKararVerAsync(int turSonucuDetayId, ManuelKararDto request, string kullaniciId, CancellationToken ct = default);
    Task ErpKarsilastirmaBaslatAsync(int planId, string kullaniciId, CancellationToken ct = default);
    Task HesaplaKarsilastirmaAsync(int turId, CancellationToken ct = default);
}

public interface ISayimKaydiService
{
    Task<SayimKaydiDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<AcikSayimKaydiDto>> GetAcikKayitlarByPlanIdAsync(int planId, CancellationToken ct = default);
    Task<OfflineImportSonucDto> OfflineImportAsync(int katilimciId, Stream dosya, string dosyaAdi, string kullaniciId, bool tamamla = false, CancellationToken ct = default);
    Task<SayimKaydiDto> AcAsync(int turId, int ekipId, string kullaniciId, CancellationToken ct = default);
    Task DetayEkleAsync(int kaydiId, SayimKaydiDetayEkleDto request, CancellationToken ct = default);
    Task<TopluDetayEkleSonucDto> TopluDetayEkleAsync(int kaydiId, IEnumerable<SayimKaydiDetayEkleDto> detaylar, CancellationToken ct = default);
    Task DetayGuncelleAsync(int detayId, SayimKaydiDetayEkleDto request, CancellationToken ct = default);
    Task DetaySilAsync(int detayId, CancellationToken ct = default);
    Task TamamlaAsync(int kaydiId, string kullaniciId, CancellationToken ct = default);
}

public interface IRaporService
{
    Task<SayimDurumRaporDto> GetSayimDurumRaporuAsync(int planId, CancellationToken ct = default);
    Task<KesinFarkRaporDto> GetKesinFarkRaporuAsync(int planId, CancellationToken ct = default);
    Task<byte[]> ExportKesinFarkRaporuExcelAsync(int planId, CancellationToken ct = default);
}
public interface IMalzemeService
{
    Task<MalzemeOzetDto?> GetByKodAsync(string malzemeKodu, CancellationToken ct = default);
    Task<IEnumerable<MalzemeDto>> GetAllAsync(CancellationToken ct = default);
    Task<MalzemeImportDto> ImportAsync(Stream dosya, string dosyaAdi, CancellationToken ct = default);
}
