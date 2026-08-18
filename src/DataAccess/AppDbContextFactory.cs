using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DataAccess;

/// <summary>
/// EF Core araçlarının (migrations add/update) WebAPI'nin tam Autofac/Identity DI zincirini
/// ayağa kaldırmadan AppDbContext'i üretebilmesi için design-time fabrika. Yalnızca migration
/// üretiminde kullanılır; çalışma zamanında gerçek bağlantı DataAccessAutofacModule'den gelir.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=OgrenciTopluluklariOtomasyonu;Trusted_Connection=True;TrustServerCertificate=True;");

        return new AppDbContext(optionsBuilder.Options);
    }
}
