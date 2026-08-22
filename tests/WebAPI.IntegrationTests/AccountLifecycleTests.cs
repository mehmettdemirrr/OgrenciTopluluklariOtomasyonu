using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using DataAccess;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/PLAN-V2.md · Faz 11 (K-03/A-40) çıkış koşulu: kayıt ol → giriş reddedilir → e-postadaki
/// linkle doğrula → giriş başarılı. Y-54: token Hangfire iş parametresine değil, e-posta bağlantısına
/// gömülür — bu test parametreyi değil, GERÇEKTEN gönderilen e-postanın içeriğini okur.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class AccountLifecycleTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private static readonly Regex ConfirmationLinkRegex = new(@"userId=(\d+)&token=([^\s&]+)", RegexOptions.Compiled);

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private int _departmentId;

    public AccountLifecycleTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        // IAsyncLifetime.InitializeAsync bu sınıftaki HER test metodundan önce çalışır ama IClassFixture
        // aynı veritabanını paylaşır — Faculty.Name unique index'i olduğu için isim her seferinde benzersiz olmalı.
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var faculty = new Faculty { Name = $"Fen Fakültesi-acct-{suffix}" };
        db.Faculties.Add(faculty);
        await db.SaveChangesAsync();

        var department = new Department { Name = $"Bilgisayar Mühendisliği-acct-{suffix}", FacultyId = faculty.Id };
        db.Departments.Add(department);
        await db.SaveChangesAsync();
        _departmentId = department.Id;

        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Kayıt → giriş reddedilir → e-postadaki linkle doğrula → giriş başarılı")]
    public async Task FullFlow_RegisterConfirmLogin_WorksEndToEnd()
    {
        const string email = "lifecycle-student@test.local";
        const string password = "Str0ng!Pass1";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email, Password = password, StudentNumber = "20260123", DepartmentId = _departmentId, EnrollmentYear = 2026,
        });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginBeforeConfirm = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.Unauthorized, loginBeforeConfirm.StatusCode);

        var sentEmail = await WaitForEmailAsync(email, "doğrula", TimeSpan.FromSeconds(10));
        var match = ConfirmationLinkRegex.Match(sentEmail.Body);
        Assert.True(match.Success, "E-posta gövdesinde doğrulama bağlantısı bulunamadı: " + sentEmail.Body);

        var userId = int.Parse(match.Groups[1].Value);
        var token = Uri.UnescapeDataString(match.Groups[2].Value);

        var confirmResponse = await _client.PostAsJsonAsync("/api/auth/confirm-email", new { UserId = userId, Token = token });
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var loginAfterConfirm = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.OK, loginAfterConfirm.StatusCode);
    }

    [Fact(DisplayName = "Aynı e-posta ile ikinci kayıt reddedilir")]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        const string email = "dup-student@test.local";
        var request = new { Email = email, Password = "Str0ng!Pass1", StudentNumber = "20260124", DepartmentId = _departmentId, EnrollmentYear = 2026 };

        var first = await _client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact(DisplayName = "Y-55: ForgotPassword — kayıtlı olan ve olmayan e-posta AYNI cevabı döner")]
    public async Task ForgotPassword_KnownAndUnknownEmail_ReturnSameResponse()
    {
        const string knownEmail = "forgot-known@test.local";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = knownEmail, Password = "Str0ng!Pass1", StudentNumber = "20260125", DepartmentId = _departmentId, EnrollmentYear = 2026,
        });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var knownResponse = await _client.PostAsJsonAsync("/api/auth/forgot-password", new { Email = knownEmail });
        var unknownResponse = await _client.PostAsJsonAsync("/api/auth/forgot-password", new { Email = "yok-boyle-biri@test.local" });

        Assert.Equal(HttpStatusCode.OK, knownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, unknownResponse.StatusCode);
        Assert.Equal(await knownResponse.Content.ReadAsStringAsync(), await unknownResponse.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "Parola sıfırlama: e-postadaki linkle yeni parola belirlenir, eski parola artık çalışmaz")]
    public async Task ResetPassword_WithLinkFromEmail_ChangesPasswordAndOldOneStopsWorking()
    {
        const string email = "reset-student@test.local";
        const string oldPassword = "Str0ng!Pass1";
        const string newPassword = "EvenStr0ng3r!";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email, Password = oldPassword, StudentNumber = "20260126", DepartmentId = _departmentId, EnrollmentYear = 2026,
        });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        // Doğrulama olmadan giriş reddedileceği için önce e-postayı doğrula (bu testin odağı değil, ön koşul).
        var confirmationEmail = await WaitForEmailAsync(email, "doğrula", TimeSpan.FromSeconds(10));
        var confirmMatch = ConfirmationLinkRegex.Match(confirmationEmail.Body);
        await _client.PostAsJsonAsync(
            "/api/auth/confirm-email", new { UserId = int.Parse(confirmMatch.Groups[1].Value), Token = Uri.UnescapeDataString(confirmMatch.Groups[2].Value) });

        var forgotResponse = await _client.PostAsJsonAsync("/api/auth/forgot-password", new { Email = email });
        Assert.Equal(HttpStatusCode.OK, forgotResponse.StatusCode);

        var resetEmail = await WaitForEmailAsync(email, "sıfırlama", TimeSpan.FromSeconds(10));
        var resetMatch = ConfirmationLinkRegex.Match(resetEmail.Body);
        Assert.True(resetMatch.Success, "E-posta gövdesinde sıfırlama bağlantısı bulunamadı: " + resetEmail.Body);

        var resetResponse = await _client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            UserId = int.Parse(resetMatch.Groups[1].Value), Token = Uri.UnescapeDataString(resetMatch.Groups[2].Value), NewPassword = newPassword,
        });
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        var loginWithOldPassword = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = oldPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, loginWithOldPassword.StatusCode);

        var loginWithNewPassword = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = newPassword });
        Assert.Equal(HttpStatusCode.OK, loginWithNewPassword.StatusCode);
    }

    /// <summary>
    /// Hangfire işinin türüne göre değil, GERÇEKTEN gönderilen e-postaya göre bekler — aynı fabrika/
    /// veritabanı bu sınıftaki TÜM testler arasında paylaşıldığı için (IClassFixture), "X türünde bir
    /// iş başarılı oldu mu" sorusu önceki bir testin işiyle yanlışlıkla eşleşebilirdi. E-posta adresi
    /// her testte benzersiz olduğundan, konu metnine göre süzmek (doğrulama vs sıfırlama) doğru işin
    /// bitişini bekleyen güvenilir bir sinyaldir — ConcurrentBag sıralama garantisi vermez.
    /// </summary>
    private async Task<(string ToAddress, string Subject, string Body)> WaitForEmailAsync(string toAddress, string subjectContains, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var match = _factory.EmailSender.SentEmails.FirstOrDefault(e => e.ToAddress == toAddress && e.Subject.Contains(subjectContains));
            if (match != default)
            {
                return match;
            }

            await Task.Delay(250);
        }

        Assert.Fail($"'{toAddress}' adresine '{subjectContains}' konulu e-posta zaman aşımı içinde gönderilmedi.");
        return default;
    }
}
