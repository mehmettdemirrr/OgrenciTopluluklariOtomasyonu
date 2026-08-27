using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DataAccess;
using Entities;
using Entities.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/MIMARI.md · Faz 6: Y-40 (içerik imzası), Y-49 (wwwroot dışı depolama, tek servis ucu),
/// Y-52 (anonim uç bir Protected kaydı asla döndürmez), A-36 (açık görsel anonim uçtan).
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class FileEndpointTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private static readonly byte[] ValidPngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public FileEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Logo yükle → anonim uçtan Authorization header'sız indirilebilir (A-36)")]
    public async Task UploadThenAnonymousDownload_Succeeds()
    {
        var scenario = await SeedAdvisorWithClubAsync("logo");
        var token = await LoginAndGetAccessTokenAsync(scenario.Email, scenario.Password);

        var uploadResponse = await UploadClubLogoAsync(scenario.ClubId, token, ValidPngBytes);
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);

        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<UploadedFileResponseDto>();
        Assert.NotNull(uploaded);

        // Authorization header kasıtlı olarak yok — bu uç anonim (Y-52/A-36).
        var downloadResponse = await _client.GetAsync($"/api/files/{uploaded!.FileId}");

        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal("image/png", downloadResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(ValidPngBytes, await downloadResponse.Content.ReadAsByteArrayAsync());
    }

    [Fact(DisplayName = "İçeriği metin olan sahte .png reddedilir (Y-40)")]
    public async Task UploadTextDisguisedAsPng_Returns400()
    {
        var scenario = await SeedAdvisorWithClubAsync("fake");
        var token = await LoginAndGetAccessTokenAsync(scenario.Email, scenario.Password);

        var textBytes = "bu bir resim degil, sadece metin"u8.ToArray();
        var uploadResponse = await UploadClubLogoAsync(scenario.ClubId, token, textBytes);

        Assert.Equal(HttpStatusCode.BadRequest, uploadResponse.StatusCode);
    }

    [Fact(DisplayName = "Rapor çıktısının (Protected) id'siyle anonim uç 404 döner (Y-52)")]
    public async Task AnonymousEndpoint_NeverReturnsProtectedFile()
    {
        int protectedFileId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var protectedFile = new StoredFile
            {
                GeneratedFileName = $"{Guid.NewGuid():N}.xlsx",
                OriginalFileName = "rapor.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileSizeBytes = 10,
                Visibility = FileVisibility.Protected,
                UploadedByUserId = 1,
                UploadedAtUtc = DateTime.UtcNow,
            };
            db.StoredFiles.Add(protectedFile);
            await db.SaveChangesAsync();
            protectedFileId = protectedFile.Id;
        }

        var response = await _client.GetAsync($"/api/files/{protectedFileId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "Yüklenen dosya wwwroot dışında bir kökte, sunucu tarafında üretilen adla saklanır (Y-49)")]
    public async Task UploadedFile_IsStoredOutsideWwwroot()
    {
        var scenario = await SeedAdvisorWithClubAsync("path");
        var token = await LoginAndGetAccessTokenAsync(scenario.Email, scenario.Password);

        var uploadResponse = await UploadClubLogoAsync(scenario.ClubId, token, ValidPngBytes);
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<UploadedFileResponseDto>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedFile = await db.StoredFiles.SingleAsync(f => f.Id == uploaded!.FileId);

        var expectedPath = Path.Combine(_factory.FileStorageRootPath, storedFile.GeneratedFileName);
        Assert.True(File.Exists(expectedPath));
        Assert.DoesNotContain("wwwroot", expectedPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "A-64 REGRESYON: kulüp logosu ucuna PDF yüklenemez (400) — PDF eklenince tip kümesi gevşemedi")]
    public async Task UploadClubLogo_PdfContent_ReturnsBadRequest()
    {
        var scenario = await SeedAdvisorWithClubAsync("pdf");
        var token = await LoginAndGetAccessTokenAsync(scenario.Email, scenario.Password);

        // Geçerli bir PDF imzası: Core artık bunu TANIYOR. Uç yine de reddetmeli — logo yolunun
        // izin verdiği küme yalnızca JPEG/PNG/WebP (A-64).
        byte[] pdfBytes = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37, 0x0A, 0x25, 0xE2, 0xE3, 0xCF, 0xD3];

        var response = await UploadClubLogoAsync(scenario.ClubId, token, pdfBytes, "application/pdf", "sahte-logo.pdf");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpResponseMessage> UploadClubLogoAsync(
        int clubId,
        string accessToken,
        byte[] fileBytes,
        string contentType = "image/png",
        string fileName = "logo.png")
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/clubs/{clubId}/logo") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await _client.SendAsync(request);
    }

    private sealed record AdvisorScenario(int ClubId, string Email, string Password);

    private async Task<AdvisorScenario> SeedAdvisorWithClubAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var faculty = new Faculty { Name = $"Fen-{suffix}" };
        db.Faculties.Add(faculty);
        await db.SaveChangesAsync();

        var department = new Department { Name = $"Bilgisayar-{suffix}", FacultyId = faculty.Id };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        const string password = "Advisor!Test123456";
        var email = $"file-advisor-{suffix}@test.local";
        var advisorUser = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var createResult = await userManager.CreateAsync(advisorUser, password);
        Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(advisorUser, "Advisor");

        var advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr.", DepartmentId = department.Id };
        db.AcademicStaff.Add(advisor);
        await db.SaveChangesAsync();

        var club = new Club { Name = $"Dosya Kulübü-{suffix}", AdvisorId = advisor.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();

        return new AdvisorScenario(club.Id, email, password);
    }

    private async Task<string> LoginAndGetAccessTokenAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return body!.AccessToken;
    }

    private sealed class UploadedFileResponseDto
    {
        public int FileId { get; init; }

        public string ContentType { get; init; } = string.Empty;

        public long FileSizeBytes { get; init; }
    }

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public DateTime AccessTokenExpiresAtUtc { get; init; }

        public string CsrfToken { get; init; } = string.Empty;
    }
}
