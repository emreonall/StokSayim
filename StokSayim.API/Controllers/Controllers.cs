using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StokSayim.Application.DTOs.Auth;
using StokSayim.Application.DTOs.Malzeme;
using StokSayim.Application.DTOs.SayimKaydi;
using StokSayim.Application.DTOs.ErpKontrol;
using StokSayim.Application.Interfaces.Services;

namespace StokSayim.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        try
        {
            var result = await _authService.LoginAsync(request, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { mesaj = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("aktif-gorev")]
    public async Task<IActionResult> GetAktifGorev(CancellationToken ct)
    {
        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(kullaniciId)) return Unauthorized();
        var gorev = await _authService.GetAktifGorevAsync(kullaniciId, ct);
        return Ok(gorev);
    }

    [HttpGet("aktif-gorevler")]
    public async Task<IActionResult> GetAktifGorevler(CancellationToken ct)
    {
        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(kullaniciId)) return Unauthorized();
        var gorevler = await _authService.GetAktifGorevlerAsync(kullaniciId, ct);
        return Ok(gorevler);
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KullaniciController : ControllerBase
{
    private readonly IKullaniciService _kullaniciService;
    public KullaniciController(IKullaniciService kullaniciService) => _kullaniciService = kullaniciService;

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _kullaniciService.GetAllAsync(ct));

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _kullaniciService.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] KullaniciOlusturDto request, CancellationToken ct)
    {
        var result = await _kullaniciService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(string id, [FromBody] KullaniciGuncelleDto request, CancellationToken ct)
    {
        await _kullaniciService.UpdateAsync(id, request, ct);
        return NoContent();
    }

    [HttpPatch("{id}/aktif")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetAktif(string id, [FromQuery] bool aktif, CancellationToken ct)
    {
        await _kullaniciService.SetAktifAsync(id, aktif, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EkipController : ControllerBase
{
    private readonly IEkipService _ekipService;
    public EkipController(IEkipService ekipService) => _ekipService = ekipService;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _ekipService.GetAllAsync(ct));

    [HttpGet("benim")]
    public async Task<IActionResult> GetBenim(CancellationToken ct)
    {
        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        var ekip = await _ekipService.GetEkipByKullaniciIdAsync(kullaniciId, ct);
        return ekip == null ? NotFound() : Ok(new { ekip.Id, ekip.EkipAdi });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _ekipService.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] Application.DTOs.Ekip.EkipOlusturDto request, CancellationToken ct)
    {
        var result = await _ekipService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] Application.DTOs.Ekip.EkipOlusturDto request, CancellationToken ct)
    {
        await _ekipService.UpdateAsync(id, request, ct);
        return NoContent();
    }

    [HttpPost("{id}/kullanicilar/{kullaniciId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> KullaniciEkle(int id, string kullaniciId, CancellationToken ct)
    {
        await _ekipService.KullaniciEkleAsync(id, kullaniciId, ct);
        return NoContent();
    }

    [HttpDelete("{id}/kullanicilar/{kullaniciId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> KullaniciCikar(int id, string kullaniciId, CancellationToken ct)
    {
        await _ekipService.KullaniciCikarAsync(id, kullaniciId, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SayimPlaniController : ControllerBase
{
    private readonly ISayimPlaniService _service;
    public SayimPlaniController(ISayimPlaniService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _service.GetAllAsync(ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> Create([FromBody] Application.DTOs.SayimPlani.SayimPlaniOlusturDto request, CancellationToken ct)
    {
        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        var result = await _service.CreateAsync(request, kullaniciId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> Update(int id, [FromBody] Application.DTOs.SayimPlani.SayimPlaniGuncelleDto request, CancellationToken ct)
    {
        await _service.UpdateAsync(id, request, ct);
        return NoContent();
    }

    [HttpPost("{id}/aktif-et")]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> AktifEt(int id, CancellationToken ct)
    {
        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _service.AktifEtAsync(id, kullaniciId, ct);
        return NoContent();
    }

    [HttpPost("{id}/sayimi-tamamla")]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> SayimiTamamla(int id, CancellationToken ct)
    {
        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _service.SayimiTamamlaAsync(id, kullaniciId, ct);
        return NoContent();
    }

    [HttpPost("{id}/erp-import")]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> ErpImport(int id, IFormFile dosya, CancellationToken ct)
    {
        if (dosya == null || dosya.Length == 0)
            return BadRequest(new { mesaj = "Dosya seçilmedi." });

        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        using var stream = dosya.OpenReadStream();
        var result = await _service.ImportErpStokAsync(id, stream, dosya.FileName, kullaniciId, ct);
        return Ok(result);
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BolgeController : ControllerBase
{
    private readonly IBolgeService _service;
    public BolgeController(IBolgeService service) => _service = service;

    [HttpGet("plan/{planId}")]
    public async Task<IActionResult> GetByPlan(int planId, CancellationToken ct) =>
        Ok(await _service.GetByPlanIdAsync(planId, ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> Create([FromBody] Application.DTOs.Bolge.BolgeOlusturDto request, CancellationToken ct)
    {
        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        var result = await _service.CreateAsync(request, kullaniciId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> Update(int id, [FromBody] Application.DTOs.Bolge.BolgeOlusturDto request, CancellationToken ct)
    {
        await _service.UpdateAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id}/ekip-grubu")]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> EkipGrubuAta(int id, [FromBody] Application.DTOs.Bolge.EkipGrubuAtaDto request, CancellationToken ct)
    {
        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _service.EkipGrubuAtaAsync(id, request, kullaniciId, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SayimOturumuController : ControllerBase
{
    private readonly ISayimOturumuService _service;
    public SayimOturumuController(ISayimOturumuService service) => _service = service;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("bolge/{bolgeId}")]
    public async Task<IActionResult> GetByBolge(int bolgeId, CancellationToken ct)
    {
        var result = await _service.GetByBolgeIdAsync(bolgeId, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("bolge/{bolgeId}/baslat")]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> Baslat(int bolgeId, CancellationToken ct)
    {
        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _service.BaslatAsync(bolgeId, kullaniciId, ct);
        return NoContent();
    }

    [HttpGet("bekleyen-bildirimler")]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> GetBekleyenBildirimler(CancellationToken ct) =>
        Ok(await _service.GetBekleyenBildirimlerAsync(ct));

    [HttpPost("{id}/kontrol-turu-ac")]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> KontrolTuruAc(int id, [FromBody] Application.DTOs.SayimOturumu.KontrolTuruAcDto request, CancellationToken ct)
    {
        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _service.KontrolTuruAcAsync(id, request, kullaniciId, ct);
        return NoContent();
    }

    [HttpGet("tur-sonucu/{turId}")]
    [Authorize(Roles = "Admin,SayimSorumlusu,SayimEkibi")]
    public async Task<IActionResult> GetTurSonucu(int turId, CancellationToken ct)
    {
        var sonuc = await _service.GetTurSonucuAsync(turId, ct);
        if (sonuc == null) return NotFound();
        return Ok(sonuc);
    }

    [HttpPost("tur-sonucu/{turId}/hesapla")]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> HesaplaKarsilastirma(int turId, CancellationToken ct)
    {
        await _service.HesaplaKarsilastirmaAsync(turId, ct);
        return NoContent();
    }

    [HttpPost("tur-sonucu-detay/{detayId}/manuel-karar")]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> ManuelKarar(int detayId, [FromBody] Application.DTOs.SayimOturumu.ManuelKararDto request, CancellationToken ct)
    {
        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _service.ManuelKararVerAsync(detayId, request, kullaniciId, ct);
        return NoContent();
    }

    [HttpPost("plan/{planId}/erp-karsilastirma-baslat")]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> ErpKarsilastirmaBaslat(int planId, CancellationToken ct)
    {
        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _service.ErpKarsilastirmaBaslatAsync(planId, kullaniciId, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SayimKaydiController : ControllerBase
{
    private readonly ISayimKaydiService _service;
    private readonly ISayimOturumuService _oturumuService;
    public SayimKaydiController(ISayimKaydiService service, ISayimOturumuService oturumuService)
    {
        _service = service;
        _oturumuService = oturumuService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("tur/{turId}/ac")]
    [Authorize(Roles = "SayimEkibi,Admin,SayimSorumlusu")]
    public async Task<IActionResult> Ac(int turId, [FromQuery] int ekipId, CancellationToken ct)
    {
        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        var result = await _service.AcAsync(turId, ekipId, kullaniciId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id}/detay")]
    [Authorize(Roles = "SayimEkibi,Admin,SayimSorumlusu")]
    public async Task<IActionResult> DetayEkle(int id, [FromBody] SayimKaydiDetayEkleDto request, CancellationToken ct)
    {
        await _service.DetayEkleAsync(id, request, ct);
        return NoContent();
    }

    [HttpPut("detay/{detayId}")]
    [Authorize(Roles = "SayimEkibi,Admin,SayimSorumlusu")]
    public async Task<IActionResult> DetayGuncelle(int detayId, [FromBody] SayimKaydiDetayEkleDto request, CancellationToken ct)
    {
        await _service.DetayGuncelleAsync(detayId, request, ct);
        return NoContent();
    }

    [HttpDelete("detay/{detayId}")]
    [Authorize(Roles = "SayimEkibi,Admin,SayimSorumlusu")]
    public async Task<IActionResult> DetaySil(int detayId, CancellationToken ct)
    {
        await _service.DetaySilAsync(detayId, ct);
        return NoContent();
    }

    [HttpPost("{id}/detaylar-toplu")]
    [Authorize(Roles = "SayimEkibi,Admin,SayimSorumlusu")]
    public async Task<IActionResult> TopluDetayEkle(int id, [FromBody] IEnumerable<SayimKaydiDetayEkleDto> detaylar, CancellationToken ct)
    {
        if (detaylar == null || !detaylar.Any())
            return BadRequest(new { mesaj = "Detay listesi boş olamaz." });

        var result = await _service.TopluDetayEkleAsync(id, detaylar, ct);
        return Ok(result);
    }

    [HttpPost("{id}/tamamla")]
    [Authorize(Roles = "SayimEkibi,Admin,SayimSorumlusu")]
    public async Task<IActionResult> Tamamla(int id, CancellationToken ct)
    {
        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _service.TamamlaAsync(id, kullaniciId, ct);
        return NoContent();
    }

    [HttpGet("plan/{planId}/acik-kayitlar")]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> GetAcikKayitlar(int planId, CancellationToken ct)
    {
        var result = await _service.GetAcikKayitlarByPlanIdAsync(planId, ct);
        return Ok(result);
    }

    [HttpPost("katilimci/{katilimciId}/offline-import")]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> OfflineImport(int katilimciId, IFormFile dosya, [FromQuery] bool tamamla, CancellationToken ct)
    {
        if (dosya == null || dosya.Length == 0) return BadRequest("Dosya seçilmedi.");
        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        using var stream = dosya.OpenReadStream();
        var result = await _service.OfflineImportAsync(katilimciId, stream, dosya.FileName, kullaniciId, tamamla, ct);

        if (result.KarsilastirmaTetiklendi)
            await _oturumuService.HesaplaKarsilastirmaAsync(result.TurId, ct);

        return Ok(result);
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,SayimSorumlusu")]
public class RaporController : ControllerBase
{
    private readonly IRaporService _service;
    public RaporController(IRaporService service) => _service = service;

    [HttpGet("plan/{planId}/durum")]
    public async Task<IActionResult> GetDurumRaporu(int planId, CancellationToken ct) =>
        Ok(await _service.GetSayimDurumRaporuAsync(planId, ct));

    [HttpGet("plan/{planId}/kesin-fark")]
    public async Task<IActionResult> GetKesinFarkRaporu(int planId, CancellationToken ct) =>
        Ok(await _service.GetKesinFarkRaporuAsync(planId, ct));

}
[ApiController]
[Route("api/malzeme")]
[Authorize]
public class MalzemeController : ControllerBase
{
    private readonly IMalzemeService _service;
    public MalzemeController(IMalzemeService service) => _service = service;

    [HttpGet("{kod}")]
    public async Task<IActionResult> GetByKod(string kod, CancellationToken ct)
    {
        var m = await _service.GetByKodAsync(kod, ct);
        return m == null ? NotFound() : Ok(m);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _service.GetAllAsync(ct));

    [HttpPost("import")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Import(IFormFile dosya, CancellationToken ct)
    {
        if (dosya == null || dosya.Length == 0)
            return BadRequest("Dosya boş.");
        using var stream = dosya.OpenReadStream();
        var sonuc = await _service.ImportAsync(stream, dosya.FileName, ct);
        return Ok(sonuc);
    }
}
[ApiController]
[Route("api/erp-kontrol")]
[Authorize(Roles = "Admin,SayimSorumlusu,SayimEkibi")]
public class ErpKontrolController : ControllerBase
{
    private readonly IErpKontrolService _service;
    public ErpKontrolController(IErpKontrolService service) => _service = service;

    // Atama listesi — fark olan malzemeler
    [HttpGet("plan/{planId}/atama-listesi")]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> GetAtamaListesi(int planId, CancellationToken ct) =>
        Ok(await _service.GetAtamaListesiAsync(planId, ct));

    // ERP kontrol sayımını başlat (ekip atamaları ile)
    [HttpPost("plan/{planId}/baslat")]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> Baslat(int planId, [FromBody] ErpKontrolBaslatDto request, CancellationToken ct)
    {
        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        var sonuc = await _service.BaslatAsync(planId, request, kullaniciId, ct);
        return Ok(sonuc);
    }

    // Oturum özeti
    [HttpGet("plan/{planId}")]
    public async Task<IActionResult> GetOturum(int planId, CancellationToken ct)
    {
        var sonuc = await _service.GetOturumuAsync(planId, ct);
        return sonuc == null ? NotFound() : Ok(sonuc);
    }

    // Ekip kendi malzeme listesini alır (kör sayım)
    [HttpGet("plan/{planId}/ekip/{ekipId}")]
    public async Task<IActionResult> GetEkipDetay(int planId, int ekipId, CancellationToken ct)
    {
        var sonuc = await _service.GetEkipDetayAsync(planId, ekipId, ct);
        return sonuc == null ? NotFound() : Ok(sonuc);
    }

    // Giriş yapan kullanıcının bu plandaki ERP kontrol görevini getir
    [HttpGet("plan/{planId}/benim")]
    public async Task<IActionResult> GetBenimEkipDetay(int planId, CancellationToken ct)
    {
        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        var sonuc = await _service.GetEkipDetayByKullaniciAsync(planId, kullaniciId, ct);
        return sonuc == null ? NotFound() : Ok(sonuc);
    }

    // Tek malzeme miktarı güncelle
    [HttpPut("malzeme/{malzemeId}")]
    public async Task<IActionResult> MalzemeSayimGuncelle(int malzemeId, [FromBody] ErpKontrolMalzemeSayimDto request, CancellationToken ct)
    {
        await _service.MalzemeSayimGuncelleAsync(malzemeId, request, ct);
        return NoContent();
    }

    // Ekip tamamla
    [HttpPost("ekip/{erpKontrolEkipId}/tamamla")]
    public async Task<IActionResult> EkipTamamla(int erpKontrolEkipId, CancellationToken ct)
    {
        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _service.EkipTamamlaAsync(erpKontrolEkipId, kullaniciId, ct);
        return NoContent();
    }

    // Final sonuçlar
    [HttpGet("plan/{planId}/sonuclar")]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> GetSonuclar(int planId, CancellationToken ct) =>
        Ok(await _service.GetSonuclarAsync(planId, ct));

    // Planı manuel kapat
    [HttpPost("plan/{planId}/kapat")]
    [Authorize(Roles = "Admin,SayimSorumlusu")]
    public async Task<IActionResult> PlaniKapat(int planId, CancellationToken ct)
    {
        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _service.PlaniKapatAsync(planId, kullaniciId, ct);
        return NoContent();
    }

    // Excel import
    [HttpPost("ekip/{erpKontrolEkipId}/import")]
    public async Task<IActionResult> Import(int erpKontrolEkipId, IFormFile dosya, CancellationToken ct)
    {
        if (dosya == null || dosya.Length == 0)
            return BadRequest("Dosya boş.");
        using var stream = dosya.OpenReadStream();
        var sonuc = await _service.ImportSayimAsync(erpKontrolEkipId, stream, dosya.FileName, ct);
        return Ok(sonuc);
    }

    // Terminal import (toplu miktar güncelleme)
    [HttpPut("plan/{planId}/ekip/{ekipId}/terminal")]
    public async Task<IActionResult> TerminalGuncelle(int planId, int ekipId, [FromBody] IEnumerable<ErpKontrolMalzemeSayimDto> kayitlar, CancellationToken ct)
    {
        await _service.TerminalSayimGuncelleAsync(planId, ekipId, kayitlar, ct);
        return NoContent();
    }
}
