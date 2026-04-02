using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using StokSayim.Web;
using StokSayim.Web.Services;
using Syncfusion.Blazor;

// Syncfusion lisans anahtarı — ücretsiz Community lisansı için https://www.syncfusion.com/sales/communitylicense
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JGaF1cWGhIfEx1RHxQdld5ZFRHallYTnNWUj0eQnxTdENjW31dcHFXQWReU0JzW0leYQ==");

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API base URL
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7000/")
});

// Syncfusion
builder.Services.AddSyncfusionBlazor();

// Auth
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthStateProvider>();

// HTTP services
builder.Services.AddScoped<IAuthHttpService, AuthHttpService>();
builder.Services.AddScoped<ISayimPlaniHttpService, SayimPlaniHttpService>();
builder.Services.AddScoped<IBolgeHttpService, BolgeHttpService>();
builder.Services.AddScoped<IEkipHttpService, EkipHttpService>();
builder.Services.AddScoped<ISayimOturumuHttpService, SayimOturumuHttpService>();
builder.Services.AddScoped<ISayimKaydiHttpService, SayimKaydiHttpService>();
builder.Services.AddScoped<IRaporHttpService, RaporHttpService>();
builder.Services.AddScoped<IKullaniciHttpService, KullaniciHttpService>();
builder.Services.AddScoped<MalzemeHttpService>();
builder.Services.AddScoped<IErpKontrolHttpService, ErpKontrolHttpService>();

await builder.Build().RunAsync();
