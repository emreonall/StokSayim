# Stok Sayım Yönetim Sistemi

## Proje Yapısı

```
StokSayim.sln
├── StokSayim.Domain          → Entity'ler, Enum'lar
├── StokSayim.Application     → Interface'ler, Service'ler, DTO'lar
├── StokSayim.Infrastructure  → DbContext, Repository'ler, Identity/JWT
├── StokSayim.API             → .NET Core 10 WebAPI
└── StokSayim.Web             → Blazor WebAssembly + Syncfusion 27.2.3
```

## Kurulum

### 1. Ön Gereksinimler
- .NET 9 SDK (10 yayınlanınca `net9.0` → `net10.0` olarak değiştirin)
- SQL Server Express
- Visual Studio 2022

### 2. Bağlantı Ayarları
`StokSayim.API/appsettings.json` dosyasında:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=StokSayimDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "SecretKey": "BURAYA_EN_AZ_32_KARAKTER_GIZLI_ANAHTAR_YAZIN"
  }
}
```

### 3. Syncfusion Lisans Anahtarı
`StokSayim.Web/Program.cs` dosyasında:
```csharp
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("LISANS_ANAHTARINIZ");
```
Ücretsiz Community lisansı: https://www.syncfusion.com/sales/communitylicense

### 4. Migration ve Veritabanı
Package Manager Console'da (Infrastructure projesini seçin):
```
Add-Migration InitialCreate
Update-Database
```
veya terminal:
```bash
dotnet ef migrations add InitialCreate --project StokSayim.Infrastructure --startup-project StokSayim.API
dotnet ef database update --project StokSayim.Infrastructure --startup-project StokSayim.API
```

### 5. İlk Kullanıcı
Uygulama ilk çalıştığında otomatik oluşur:
- Email: admin@stoksayim.com
- Şifre: Admin123!

### 6. Çalıştırma
Visual Studio'da Multiple Startup Projects ayarlayın:
- StokSayim.API → Start
- StokSayim.Web → Start

---

## ERP Import Excel Format

Kolon sırası:
| A | B | C | D | E | F | G |
|---|---|---|---|---|---|---|
| Malzeme Kodu | Malzeme Adı | Depo Kodu | Miktar | Birim | Lot No | Seri No |

---

## Kullanıcı Rolleri

| Rol | Yetkiler |
|-----|---------|
| Admin | Tüm işlemler + kullanıcı yönetimi |
| SayimSorumlusu | Plan, bölge, ekip grubu, kontrol turu, rapor |
| SayimEkibi | Sadece sayım kaydı giriş/tamamlama |

---

## Önemli Notlar

- `StokSayim.Application` katmanında EF Core referansı var (SayimOturumuService içinde).
  Bu, Clean Architecture'ı hafif çiğnese de servis katmanını bölmemek için tercih edilmiştir.
  İstirseniz `IQueryable` yerine özel repository metodları ekleyerek tamamen ayırabilirsiniz.

- Syncfusion paket versiyonu `.csproj` dosyasında `27.2.3` olarak girilmiştir.
  Siz `32.2.3` lisansınız varsa tüm `.csproj` dosyalarında `27.2.3` → `32.2.3` olarak değiştirin.

- Blazor sayfaları iskelet olarak bırakılmıştır. Her servis hazır, sayfalar servisleri kullanarak doldurulacak.
