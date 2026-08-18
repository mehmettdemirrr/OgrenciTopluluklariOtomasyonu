using Business.Abstract;
using DataAccess.Seed;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Business.Concrete;

/// <summary>
/// docs/MIMARI.md · A-27/Y-19/Y-20: admin kullanıcısını UserManager üzerinden create-if-missing
/// olarak seed eder (parola hash'i HasData ile üretilemez). Seed bilgisi user-secrets/ortam
/// değişkeninden gelir, appsettings*.json'a asla yazılmaz.
/// </summary>
public sealed class IdentitySeeder(UserManager<ApplicationUser> userManager, IConfiguration configuration) : IIdentitySeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var existing = await userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (existing is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
        };

        var createResult = await userManager.CreateAsync(admin, password).ConfigureAwait(false);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Admin seed kullanıcısı oluşturulamadı: " + string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(admin, IdentitySeedData.AdminRoleName).ConfigureAwait(false);
    }
}
