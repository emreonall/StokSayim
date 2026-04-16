namespace StokSayim.Web.Services;

public enum ToastTip { Basari, Hata, Bilgi, Uyari }

public record ToastMesaj(string Mesaj, ToastTip Tip);

public class ToastService
{
    public event Action<ToastMesaj>? OnMesaj;

    public void Basari(string mesaj) => OnMesaj?.Invoke(new(mesaj, ToastTip.Basari));
    public void Hata(string mesaj) => OnMesaj?.Invoke(new(mesaj, ToastTip.Hata));
    public void Bilgi(string mesaj) => OnMesaj?.Invoke(new(mesaj, ToastTip.Bilgi));
    public void Uyari(string mesaj) => OnMesaj?.Invoke(new(mesaj, ToastTip.Uyari));
}
