namespace StokSayim.Web.Services;

/// <summary>
/// Menüdeki "Bekleyen İşler" rozetinin tek kaynağı.
/// Sayfalar bir işlem yaptıktan sonra <see cref="YenileAsync"/> çağırarak rozeti anında güncelleyebilir.
/// </summary>
public class BildirimSayaciService
{
    private readonly ISayimOturumuHttpService _oturumuService;

    public BildirimSayaciService(ISayimOturumuHttpService oturumuService)
        => _oturumuService = oturumuService;

    public int Sayi { get; private set; }

    public event Action? OnDegisti;

    public async Task YenileAsync()
    {
        try
        {
            var bildirimler = await _oturumuService.GetBekleyenBildirimlerAsync();
            var yeniSayi = bildirimler.Count();
            if (yeniSayi != Sayi)
            {
                Sayi = yeniSayi;
                OnDegisti?.Invoke();
            }
        }
        catch
        {
            // Geçici bağlantı/yetki hatası: mevcut değer korunur, bir sonraki yenilemede tekrar denenir.
        }
    }
}
