namespace Business.Abstract;

/// <summary>docs/MIMARI.md · A-27/Y-19/Y-20: admin kullanıcısı parola hash'i UserManager gerektirdiği için HasData ile seed edilemez.</summary>
public interface IIdentitySeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
