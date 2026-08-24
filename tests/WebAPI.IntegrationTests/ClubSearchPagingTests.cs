using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DataAccess;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/PLAN-V4.md §21 "Çıkış koşulu" (A-50/Y-62/Y-64) — Faz 21'in kabul testi.
/// 120 kulüp seed edilir; 101+'inci kayıt <b>aranarak</b> bulunuyor, `pageSize=200` istense bile
/// 100 dönüyor ama `TotalCount` gerçeği söylüyor, sayfalar birbirinin satırını tekrarlamıyor,
/// `isActive` filtresi pasif kulübü gerçekten getiriyor.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class ClubSearchPagingTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string AdminEmail = "csp-admin@test.local";
    private const string AdminPassword = "Admin!Test123456";

    /// <summary>Aynı fabrikayı paylaşan diğer testlerin kulüpleri sayıma karışmasın diye benzersiz önek.</summary>
    private static readonly string Prefix = $"CSP{Guid.NewGuid():N}"[..11];

    private const int SeededClubCount = 120;

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private string _adminToken = string.Empty;

    public ClubSearchPagingTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminUser = await EnsureUserAsync(userManager, AdminEmail, AdminPassword, "Admin");

        var department = await db.Departments.FirstAsync();
        var advisor = await db.AcademicStaff.FirstOrDefaultAsync(a => a.ApplicationUserId == adminUser.Id);
        if (advisor is null)
        {
            advisor = new AcademicStaff { ApplicationUserId = adminUser.Id, Title = "Prof. Dr.", DepartmentId = department.Id };
            db.AcademicStaff.Add(advisor);
            await db.SaveChangesAsync();
        }

        if (!await db.Clubs.AnyAsync(c => c.Name.StartsWith(Prefix)))
        {
            // Ad sıralaması sayfalamayı belirler; 001..120 sıfır dolgulu ki sıra sayısal olsun.
            for (var i = 1; i <= SeededClubCount; i++)
            {
                db.Clubs.Add(new Club
                {
                    Name = $"{Prefix} Kulubu {i:D3}",
                    AdvisorId = advisor.Id,
                    // Son kulüp bilerek pasif: isActive filtresinin gerçekten çalıştığını göstermek için.
                    IsActive = i != SeededClubCount,
                    CreatedAtUtc = DateTime.UtcNow,
                });
            }

            await db.SaveChangesAsync();
        }

        _client = _factory.CreateClient();
        _adminToken = await LoginAsync(AdminEmail, AdminPassword);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "A-50/Y-62: 120 kulüpte 101+'inci kayıt aranarak bulunur — tek sayfaya sığmasa bile")]
    public async Task Search_FindsRecordBeyondFirstPage()
    {
        // 120. kulüp ada göre sıralamada en sonda ve ilk 100'e asla giremez.
        var unreachable = $"{Prefix} Kulubu {SeededClubCount:D3}";

        // Arama olmadan: kayıt ilk sayfada yok — üst sınır 100 olduğu için hiçbir sayfa boyutuyla erişilemez.
        var firstPage = await GetClubsAsync($"pageIndex=0&pageSize=100&search={Uri.EscapeDataString(Prefix)}");
        Assert.DoesNotContain(firstPage.Items, c => c.Name == unreachable);

        // Aranınca: tek kayıt olarak geliyor. Y-62'nin kanıtı — istemci hiçbir şey ayıklamadı.
        var searched = await GetClubsAsync($"pageIndex=0&pageSize=20&search={Uri.EscapeDataString($"Kulubu {SeededClubCount:D3}")}&isActive=false");
        Assert.Equal(unreachable, Assert.Single(searched.Items).Name);
        Assert.Equal(1, searched.TotalCount);
    }

    [Fact(DisplayName = "Y-11/A-16: pageSize=200 istense bile 100 döner ama TotalCount gerçeği söyler")]
    public async Task PageSizeAbove100_IsClamped_ButTotalCountIsHonest()
    {
        var paged = await GetClubsAsync($"pageIndex=0&pageSize=200&search={Uri.EscapeDataString(Prefix)}");

        Assert.Equal(100, paged.PageSize);
        Assert.Equal(100, paged.Items.Count);

        // Sessiz kırpmanın panzehiri: sunucu "120 tane var, sana 100 verdim" diyor.
        Assert.Equal(SeededClubCount, paged.TotalCount);
    }

    [Fact(DisplayName = "Y-64: 1. ve 2. sayfa hiçbir kaydı paylaşmaz — sıralama deterministik")]
    public async Task ConsecutivePages_ShareNoRecords()
    {
        var page0 = await GetClubsAsync($"pageIndex=0&pageSize=50&search={Uri.EscapeDataString(Prefix)}");
        var page1 = await GetClubsAsync($"pageIndex=1&pageSize=50&search={Uri.EscapeDataString(Prefix)}");
        var page2 = await GetClubsAsync($"pageIndex=2&pageSize=50&search={Uri.EscapeDataString(Prefix)}");

        var ids = page0.Items.Concat(page1.Items).Concat(page2.Items).Select(c => c.Id).ToList();

        Assert.Equal(SeededClubCount, ids.Count);
        Assert.Equal(SeededClubCount, ids.Distinct().Count());

        // Sıra gerçekten ada göre: birleşik liste sıralı olmalı.
        var names = page0.Items.Concat(page1.Items).Concat(page2.Items).Select(c => c.Name).ToList();
        Assert.Equal(names.OrderBy(n => n, StringComparer.Ordinal), names);
    }

    [Fact(DisplayName = "PLAN-V4 §21.1b: isActive=false pasif kulübü gerçekten getirir (önceden hiç dönmüyordu)")]
    public async Task IsActiveFilter_ReturnsInactiveClubs()
    {
        var inactive = await GetClubsAsync($"pageIndex=0&pageSize=100&search={Uri.EscapeDataString(Prefix)}&isActive=false");
        var active = await GetClubsAsync($"pageIndex=0&pageSize=100&search={Uri.EscapeDataString(Prefix)}&isActive=true");

        Assert.Equal(1, inactive.TotalCount);
        Assert.Equal($"{Prefix} Kulubu {SeededClubCount:D3}", Assert.Single(inactive.Items).Name);
        Assert.Equal(SeededClubCount - 1, active.TotalCount);
    }

    [Fact(DisplayName = "A-50: anonim vitrin de arama alır ve pasif kulübü aramayla dahi göstermez (Y-58)")]
    public async Task PublicSearch_NeverLeaksInactiveClub()
    {
        var inactiveName = $"{Prefix} Kulubu {SeededClubCount:D3}";

        var response = await _client.GetAsync($"/api/public/clubs?pageIndex=0&pageSize=100&search={Uri.EscapeDataString(inactiveName)}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var paged = (await response.Content.ReadFromJsonAsync<PagedClubs>())!;
        Assert.Empty(paged.Items);
        Assert.Equal(0, paged.TotalCount);
    }

    private async Task<PagedClubs> GetClubsAsync(string queryString)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/clubs?{queryString}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<PagedClubs>())!;
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!.AccessToken;
    }

    private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string password, string roleName)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return existing;
        }

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var createResult = await userManager.CreateAsync(user, password);
        Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, roleName);
        return user;
    }

    private sealed class PagedClubs
    {
        public List<ClubRow> Items { get; init; } = [];

        public int TotalCount { get; init; }

        public int PageIndex { get; init; }

        public int PageSize { get; init; }
    }

    private sealed class ClubRow
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public bool IsActive { get; init; }
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;
    }
}
