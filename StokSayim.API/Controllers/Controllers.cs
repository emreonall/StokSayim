using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StokSayim.Application.DTOs.Auth;
using StokSayim.Application.DTOs.SayimKaydi;
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
    public SayimKaydiController(ISayimKaydiService service) => _service = service;

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

    [HttpPost("{id}/tamamla")]
    [Authorize(Roles = "SayimEkibi,Admin,SayimSorumlusu")]
    public async Task<IActionResult> Tamamla(int id, CancellationToken ct)
    {
        var kullaniciId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _service.TamamlaAsync(id, kullaniciId, ct);
        return NoContent();
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

    [HttpGet("plan/{planId}/kesin-fark/excel")]
    public async Task<IActionResult> ExportKesinFarkExcel(int planId, CancellationToken ct)
    {
        var bytes = await _service.ExportKesinFarkRaporuExcelAsync(planId, ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"KesinFarkRaporu_{planId}_{DateTime.Now:yyyyMMdd}.xlsx");
    }
}
