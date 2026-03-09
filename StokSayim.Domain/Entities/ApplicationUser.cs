using Microsoft.AspNetCore.Identity;

namespace StokSayim.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string AdSoyad { get; set; } = string.Empty;
    public bool AktifMi { get; set; } = true;
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<EkipKullanici> EkipKullanicilari { get; set; } = new List<EkipKullanici>();
}
