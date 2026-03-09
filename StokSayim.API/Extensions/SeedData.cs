using Microsoft.AspNetCore.Identity;
using StokSayim.Domain.Entities;

namespace StokSayim.API.Extensions;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roller = ["Admin", "SayimSorumlusu", "SayimEkibi"];

        foreach (var rol in roller)
        {
            if (!await roleManager.RoleExistsAsync(rol))
                await roleManager.CreateAsync(new IdentityRole(rol));
        }

        // Admin kullanıcı
        if (await userManager.FindByEmailAsync("admin@stoksayim.com") == null)
        {
            var admin = new ApplicationUser
            {
                UserName = "admin@stoksayim.com",
                Email = "admin@stoksayim.com",
                AdSoyad = "Sistem Yöneticisi",
                AktifMi = true,
                EmailConfirmed = true
            };

            var sonuc = await userManager.CreateAsync(admin, "Admin123!");
            if (sonuc.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
