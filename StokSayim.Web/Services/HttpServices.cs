using System.Net.Http.Json;
using StokSayim.Application.DTOs.Auth;
using StokSayim.Application.DTOs.Bolge;
using StokSayim.Application.DTOs.Ekip;
using StokSayim.Application.DTOs.Rapor;
using StokSayim.Application.DTOs.SayimKaydi;
using StokSayim.Application.DTOs.SayimOturumu;
using StokSayim.Application.DTOs.SayimPlani;

namespace StokSayim.Web.Services;

// ─── Interfaces ───────────────────────────────────────────────────────────────

public interface IAuthHttpService
{
    Task<AuthResponseDto?> LoginAsync(LoginRequestDto request);
    Task<AktifGorevDto?> GetAktifGorevAsync();
}

public interface ISayimPlaniHttpService
{
    Task<IEnumerable<SayimPlaniListDto>> GetAllAsync();
    Task<SayimPlaniDetayDto?> GetByIdAsync(int id);
    Task<SayimPlaniDetayDto?> CreateAsync(SayimPlaniOlusturDto request);
    Task UpdateAsync(int id, SayimPlaniGuncelleDto request);
    Task AktifEtAsync(int id);
    Task<ErpImportSonucDto?> ImportErpAsync(int id, MultipartFormDataContent form);
}

public interface IBolgeHttpService
{
    Task<IEnumerable<BolgeDto>> GetByPlanAsync(int planId);
    Task<BolgeDetayDto?> GetByIdAsync(int id);
    Task<BolgeDto?> CreateAsync(BolgeOlusturDto request);
    Task UpdateAsync(int id, BolgeOlusturDto request);
    Task DeleteAsync(int id);
    Task EkipGrubuAtaAsync(int id, EkipGrubuAtaDto request);
}

public interface IEkipHttpService
{
    Task<IEnumerable<EkipDto>> GetAllAsync();
    Task<EkipDto?> GetByIdAsync(int id);
    Task<EkipDto?> CreateAsync(EkipOlusturDto request);
    Task UpdateAsync(int id, EkipOlusturDto request);
    Task KullaniciEkleAsync(int ekipId, string kullaniciId);
    Task KullaniciCikarAsync(int ekipId, string kullaniciId);
}

public interface ISayimOturumuHttpService
{
    Task<SayimOturumuDetayDto?> GetByBolgeAsync(int bolgeId);
    Task BaslatAsync(int bolgeId);
    Task<IEnumerable<GorevBildirimDto>> GetBekleyenBildirimlerAsync();
    Task KontrolTuruAcAsync(int oturumuId, KontrolTuruAcDto request);
    Task ManuelKararVerAsync(int detayId, ManuelKararDto request);
    Task ErpKarsilastirmaBaslatAsync(int planId);
    Task HesaplaKarsilastirmaAsync(int turId);
}

public interface ISayimKaydiHttpService
{
    Task<SayimKaydiDto?> GetByIdAsync(int id);
    Task<SayimKaydiDto?> AcAsync(int turId, int ekipId);
    Task DetayEkleAsync(int kaydiId, SayimKaydiDetayEkleDto request);
    Task DetayGuncelleAsync(int detayId, SayimKaydiDetayEkleDto request);
    Task DetaySilAsync(int detayId);
    Task TamamlaAsync(int kaydiId);
}

public interface IRaporHttpService
{
    Task<SayimDurumRaporDto?> GetDurumRaporuAsync(int planId);
    Task<KesinFarkRaporDto?> GetKesinFarkRaporuAsync(int planId);
    Task<byte[]?> ExportExcelAsync(int planId);
}

public interface IKullaniciHttpService
{
    Task<IEnumerable<KullaniciDto>> GetAllAsync();
    Task<KullaniciDto?> CreateAsync(KullaniciOlusturDto request);
    Task UpdateAsync(string id, KullaniciGuncelleDto request);
    Task SetAktifAsync(string id, bool aktif);
}

// ─── Implementations ──────────────────────────────────────────────────────────

public class AuthHttpService : IAuthHttpService
{
    private readonly HttpClient _http;
    private readonly JwtAuthStateProvider _authProvider;
    public AuthHttpService(HttpClient http, Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider authProvider)
    {
        _http = http;
        _authProvider = (JwtAuthStateProvider)authProvider;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", request);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        if (result != null) await _authProvider.LoginAsync(result.Token);
        return result;
    }

    public async Task<AktifGorevDto?> GetAktifGorevAsync()
        => await _http.GetFromJsonAsync<AktifGorevDto>("api/auth/aktif-gorev");
}

public class SayimPlaniHttpService : ISayimPlaniHttpService
{
    private readonly HttpClient _http;
    public SayimPlaniHttpService(HttpClient http) => _http = http;

    public async Task<IEnumerable<SayimPlaniListDto>> GetAllAsync()
        => await _http.GetFromJsonAsync<IEnumerable<SayimPlaniListDto>>("api/sayimplani") ?? [];

    public async Task<SayimPlaniDetayDto?> GetByIdAsync(int id)
        => await _http.GetFromJsonAsync<SayimPlaniDetayDto>($"api/sayimplani/{id}");

    public async Task<SayimPlaniDetayDto?> CreateAsync(SayimPlaniOlusturDto request)
    {
        var r = await _http.PostAsJsonAsync("api/sayimplani", request);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<SayimPlaniDetayDto>() : null;
    }

    public async Task UpdateAsync(int id, SayimPlaniGuncelleDto request)
        => await _http.PutAsJsonAsync($"api/sayimplani/{id}", request);

    public async Task AktifEtAsync(int id)
        => await _http.PostAsync($"api/sayimplani/{id}/aktif-et", null);

    public async Task<ErpImportSonucDto?> ImportErpAsync(int id, MultipartFormDataContent form)
    {
        var r = await _http.PostAsync($"api/sayimplani/{id}/erp-import", form);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<ErpImportSonucDto>() : null;
    }
}

public class BolgeHttpService : IBolgeHttpService
{
    private readonly HttpClient _http;
    public BolgeHttpService(HttpClient http) => _http = http;

    public async Task<IEnumerable<BolgeDto>> GetByPlanAsync(int planId)
        => await _http.GetFromJsonAsync<IEnumerable<BolgeDto>>($"api/bolge/plan/{planId}") ?? [];

    public async Task<BolgeDetayDto?> GetByIdAsync(int id)
        => await _http.GetFromJsonAsync<BolgeDetayDto>($"api/bolge/{id}");

    public async Task<BolgeDto?> CreateAsync(BolgeOlusturDto request)
    {
        var r = await _http.PostAsJsonAsync("api/bolge", request);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<BolgeDto>() : null;
    }

    public async Task UpdateAsync(int id, BolgeOlusturDto request)
        => await _http.PutAsJsonAsync($"api/bolge/{id}", request);

    public async Task DeleteAsync(int id)
        => await _http.DeleteAsync($"api/bolge/{id}");

    public async Task EkipGrubuAtaAsync(int id, EkipGrubuAtaDto request)
        => await _http.PostAsJsonAsync($"api/bolge/{id}/ekip-grubu", request);
}

public class EkipHttpService : IEkipHttpService
{
    private readonly HttpClient _http;
    public EkipHttpService(HttpClient http) => _http = http;

    public async Task<IEnumerable<EkipDto>> GetAllAsync()
        => await _http.GetFromJsonAsync<IEnumerable<EkipDto>>("api/ekip") ?? [];

    public async Task<EkipDto?> GetByIdAsync(int id)
        => await _http.GetFromJsonAsync<EkipDto>($"api/ekip/{id}");

    public async Task<EkipDto?> CreateAsync(EkipOlusturDto request)
    {
        var r = await _http.PostAsJsonAsync("api/ekip", request);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<EkipDto>() : null;
    }

    public async Task UpdateAsync(int id, EkipOlusturDto request)
        => await _http.PutAsJsonAsync($"api/ekip/{id}", request);

    public async Task KullaniciEkleAsync(int ekipId, string kullaniciId)
        => await _http.PostAsync($"api/ekip/{ekipId}/kullanicilar/{kullaniciId}", null);

    public async Task KullaniciCikarAsync(int ekipId, string kullaniciId)
        => await _http.DeleteAsync($"api/ekip/{ekipId}/kullanicilar/{kullaniciId}");
}

public class SayimOturumuHttpService : ISayimOturumuHttpService
{
    private readonly HttpClient _http;
    public SayimOturumuHttpService(HttpClient http) => _http = http;

    public async Task<SayimOturumuDetayDto?> GetByBolgeAsync(int bolgeId)
        => await _http.GetFromJsonAsync<SayimOturumuDetayDto>($"api/sayimoturumu/bolge/{bolgeId}");

    public async Task BaslatAsync(int bolgeId)
        => await _http.PostAsync($"api/sayimoturumu/bolge/{bolgeId}/baslat", null);

    public async Task<IEnumerable<GorevBildirimDto>> GetBekleyenBildirimlerAsync()
        => await _http.GetFromJsonAsync<IEnumerable<GorevBildirimDto>>("api/sayimoturumu/bekleyen-bildirimler") ?? [];

    public async Task KontrolTuruAcAsync(int oturumuId, KontrolTuruAcDto request)
        => await _http.PostAsJsonAsync($"api/sayimoturumu/{oturumuId}/kontrol-turu-ac", request);

    public async Task ManuelKararVerAsync(int detayId, ManuelKararDto request)
        => await _http.PostAsJsonAsync($"api/sayimoturumu/tur-sonucu-detay/{detayId}/manuel-karar", request);

    public async Task ErpKarsilastirmaBaslatAsync(int planId)
        => await _http.PostAsync($"api/sayimoturumu/plan/{planId}/erp-karsilastirma-baslat", null);

    public async Task HesaplaKarsilastirmaAsync(int turId)
        => await _http.PostAsync($"api/sayimoturumu/tur-sonucu/{turId}/hesapla", null);
}

public class SayimKaydiHttpService : ISayimKaydiHttpService
{
    private readonly HttpClient _http;
    public SayimKaydiHttpService(HttpClient http) => _http = http;

    public async Task<SayimKaydiDto?> GetByIdAsync(int id)
        => await _http.GetFromJsonAsync<SayimKaydiDto>($"api/sayimkaydi/{id}");

    public async Task<SayimKaydiDto?> AcAsync(int turId, int ekipId)
    {
        var r = await _http.PostAsync($"api/sayimkaydi/tur/{turId}/ac?ekipId={ekipId}", null);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<SayimKaydiDto>() : null;
    }

    public async Task DetayEkleAsync(int kaydiId, SayimKaydiDetayEkleDto request)
        => await _http.PostAsJsonAsync($"api/sayimkaydi/{kaydiId}/detay", request);

    public async Task DetayGuncelleAsync(int detayId, SayimKaydiDetayEkleDto request)
        => await _http.PutAsJsonAsync($"api/sayimkaydi/detay/{detayId}", request);

    public async Task DetaySilAsync(int detayId)
        => await _http.DeleteAsync($"api/sayimkaydi/detay/{detayId}");

    public async Task TamamlaAsync(int kaydiId)
        => await _http.PostAsync($"api/sayimkaydi/{kaydiId}/tamamla", null);
}

public class RaporHttpService : IRaporHttpService
{
    private readonly HttpClient _http;
    public RaporHttpService(HttpClient http) => _http = http;

    public async Task<SayimDurumRaporDto?> GetDurumRaporuAsync(int planId)
        => await _http.GetFromJsonAsync<SayimDurumRaporDto>($"api/rapor/plan/{planId}/durum");

    public async Task<KesinFarkRaporDto?> GetKesinFarkRaporuAsync(int planId)
        => await _http.GetFromJsonAsync<KesinFarkRaporDto>($"api/rapor/plan/{planId}/kesin-fark");

    public async Task<byte[]?> ExportExcelAsync(int planId)
    {
        var r = await _http.GetAsync($"api/rapor/plan/{planId}/kesin-fark/excel");
        return r.IsSuccessStatusCode ? await r.Content.ReadAsByteArrayAsync() : null;
    }
}

public class KullaniciHttpService : IKullaniciHttpService
{
    private readonly HttpClient _http;
    public KullaniciHttpService(HttpClient http) => _http = http;

    public async Task<IEnumerable<KullaniciDto>> GetAllAsync()
        => await _http.GetFromJsonAsync<IEnumerable<KullaniciDto>>("api/kullanici") ?? [];

    public async Task<KullaniciDto?> CreateAsync(KullaniciOlusturDto request)
    {
        var r = await _http.PostAsJsonAsync("api/kullanici", request);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<KullaniciDto>() : null;
    }

    public async Task UpdateAsync(string id, KullaniciGuncelleDto request)
        => await _http.PutAsJsonAsync($"api/kullanici/{id}", request);

    public async Task SetAktifAsync(string id, bool aktif)
        => await _http.PatchAsync($"api/kullanici/{id}/aktif?aktif={aktif}", null);
}
