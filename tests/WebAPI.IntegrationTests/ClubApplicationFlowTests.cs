using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Business.BackgroundJobs;
using DataAccess;
using Entities;
using Entities.Enums;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/PLAN-V3.md §17 "Çıkış koşulu": öğrenci başvurur → admin kuyruğunda görür → onaylar →
/// kulüp oluşur, öğrenci o kulübün President'i olur → yeni kulüp anında listede görünür (cache) →
/// aynı dönemde ikinci bekleyen başvuru DB seviyesinde reddedilir.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class ClubApplicationFlowTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string StudentEmail = "ca-student@test.local";
    private const string StudentPassword = "Student!Test123456";
    private const string OtherStudentEmail = "ca-other-student@test.local";
    private const string OtherStudentPassword = "OtherStudent!Test123456";
    private const string AdvisorEmail = "ca-advisor@test.local";
    private const string AdvisorPassword = "Advisor!Test123456";
    private const string AdminEmail = "ca-admin@test.local";
    private const string AdminPassword = "Admin!Test123456";
    private const string MemberEmail = "ca-member@test.local";
    private const string MemberPassword = "Member!Test123456";

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private int _proposedAdvisorId;

    public ClubApplicationFlowTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureUserAsync(userManager, StudentEmail, StudentPassword, "Member");
        await EnsureUserAsync(userManager, OtherStudentEmail, OtherStudentPassword, "Member");
        await EnsureUserAsync(userManager, MemberEmail, MemberPassword, "Member");
        await EnsureUserAsync(userManager, AdminEmail, AdminPassword, "Admin");
        var advisorUser = await EnsureUserAsync(userManager, AdvisorEmail, AdvisorPassword, "Advisor");

        var department = await db.Departments.FirstAsync();

        var advisor = await db.AcademicStaff.FirstOrDefaultAsync(a => a.ApplicationUserId == advisorUser.Id);
        if (advisor is null)
        {
            advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr.", DepartmentId = department.Id };
            db.AcademicStaff.Add(advisor);
            await db.SaveChangesAsync();
        }

        _proposedAdvisorId = advisor.Id;

        foreach (var (email, number) in new[] { (StudentEmail, "CA-S1"), (OtherStudentEmail, "CA-S2"), (MemberEmail, "CA-S3") })
        {
            var user = await userManager.FindByEmailAsync(email);
            var student = await db.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == user!.Id);
            if (student is null)
            {
                db.Students.Add(new Student
                {
                    ApplicationUserId = user!.Id,
                    StudentNumber = $"{number}{Guid.NewGuid():N}"[..12],
                    DepartmentId = department.Id,
                    EnrollmentYear = 2026,
                });
                await db.SaveChangesAsync();
            }
        }

        // Faz 31: pencere varsayılanı fail-closed (A-66). Bu sınıftaki AKIŞ testleri pencereyi
        // sınamıyor, akışı sınıyor — bu yüzden her kurulumda pencere açık başlar. Pencereyi
        // sınayan testler kendi durumlarını SetWindowAsync ile kurar (Y-34: her test kendi verisini kurar).
        var currentTerm = await db.AcademicTerms.SingleAsync(t => t.IsCurrent);
        currentTerm.ClubApplicationOverride = ClubApplicationWindowOverride.ForceOpen;
        currentTerm.ClubApplicationStartUtc = null;
        currentTerm.ClubApplicationEndUtc = null;
        await db.SaveChangesAsync();

        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Onay akışı: başvuru → çift bekleyen başvuru reddedilir → yetkisiz karar 403 → onay → kulüp+President+audit+bildirim+cache")]
    public async Task Submit_Then_Approve_CreatesClubAndPresidentMembership()
    {
        var proposedName = $"Robotik Kulübü {Guid.NewGuid():N}"[..30];
        var studentToken = await LoginAsync(StudentEmail, StudentPassword);

        // 1. Kulüp önce vitrinde/listede yok.
        var beforeResponse = await SendWithBearerAsync(HttpMethod.Get, "/api/clubs?pageIndex=0&pageSize=200", studentToken);
        Assert.DoesNotContain(proposedName, await beforeResponse.Content.ReadAsStringAsync());

        // 2. Başvuru.
        var submitResponse = await SubmitWithAllDocumentsAsync(studentToken, proposedName);
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        // 3. Aynı dönemde ikinci bekleyen başvuru — filtreli unique index'in iş kuralı yansıması (A-45).
        var duplicateResponse = await SubmitWithAllDocumentsAsync(studentToken, $"Başka Ad {Guid.NewGuid():N}"[..20]);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        // 4. "Başvurularım" ekranında görünüyor mu?
        var mineResponse = await SendWithBearerAsync(HttpMethod.Get, "/api/club-applications/mine", studentToken);
        Assert.Equal(HttpStatusCode.OK, mineResponse.StatusCode);
        Assert.Contains(proposedName, await mineResponse.Content.ReadAsStringAsync());

        int applicationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            applicationId = (await db.ClubApplications.SingleAsync(a => a.ProposedName == proposedName)).Id;
        }

        // 5. clubs.write taşımayan kimliği doğrulanmış kullanıcı (sıradan öğrenci) karar veremez.
        var memberToken = await LoginAsync(MemberEmail, MemberPassword);
        var unauthorizedDecision = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/club-applications/{applicationId}/decision", memberToken, new { Status = "Approved" });
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedDecision.StatusCode);

        // 6. Admin kuyrukta görür.
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        var pendingResponse = await SendWithBearerAsync(HttpMethod.Get, "/api/club-applications", adminToken);
        Assert.Equal(HttpStatusCode.OK, pendingResponse.StatusCode);
        Assert.Contains(proposedName, await pendingResponse.Content.ReadAsStringAsync());

        // 7. Onayla — ClubApplicationManager.DecideAsync'in elle yönettiği transaction (Y-46/Y-06).
        var decideResponse = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/club-applications/{applicationId}/decision", adminToken, new { Status = "Approved", ReviewNote = "Uygun bulundu." });
        Assert.Equal(HttpStatusCode.OK, decideResponse.StatusCode);

        int createdClubId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 8. Club oluştu, başvuru CreatedClubId'yi taşıyor.
            var application = await db.ClubApplications.SingleAsync(a => a.Id == applicationId);
            Assert.Equal(ApplicationStatus.Approved, application.Status);
            Assert.NotNull(application.CreatedClubId);
            createdClubId = application.CreatedClubId!.Value;

            var club = await db.Clubs.SingleAsync(c => c.Id == createdClubId);
            Assert.Equal(proposedName, club.Name);
            Assert.True(club.IsActive);

            // 9. Başvuran öğrenci o kulübün President'i oldu (O-3).
            var membership = await db.ClubMemberships.SingleAsync(m => m.ClubId == createdClubId);
            Assert.Equal(ClubRole.President, membership.ClubRole);

            // Audit kanıtı (K-12).
            Assert.NotEmpty(await db.AuditLogs
                .Where(a => a.EntityType == nameof(ClubApplication) && a.EntityId == applicationId.ToString())
                .ToListAsync());
        }

        // 10. Cache geçersizleştirme kanıtı: yeni kulüp anında listede (Y-45'in ClubManager ayağı).
        var afterResponse = await SendWithBearerAsync(HttpMethod.Get, "/api/clubs?pageIndex=0&pageSize=200", studentToken);
        Assert.Contains(proposedName, await afterResponse.Content.ReadAsStringAsync());

        // 11. Kuyruk kanıtı: bildirim işi Hangfire'da Succeeded durumuna ulaşana kadar bekle.
        await WaitForNotificationJobToSucceedAsync(TimeSpan.FromSeconds(10));
        Assert.Contains(_factory.EmailSender.SentEmails, e => e.ToAddress == StudentEmail);

        // Not: yeni başkan hâlâ "Member" Identity rolünde — ClubRole=President tek başına
        // events.write JWT claim'ini taşımaz (bkz. EventApprovalFlowTests: officer'a ayrıca
        // "ClubOfficer" Identity rolü atanıyor). Onay akışı bir Identity rol ataması yapmaz;
        // bu bilinçli bir sınır, ayrı bir admin adımı gerektirir (Faz 17 kapsamı dışında).
    }

    [Fact(DisplayName = "Ret akışı: başvuru → reddet → kulüp oluşmaz, ret bildirimi gönderilir")]
    public async Task Submit_Then_Reject_CreatesNoClub()
    {
        var proposedName = $"Satranç Kulübü {Guid.NewGuid():N}"[..30];
        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);

        var submitResponse = await SubmitWithAllDocumentsAsync(studentToken, proposedName);
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        int applicationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            applicationId = (await db.ClubApplications.SingleAsync(a => a.ProposedName == proposedName)).Id;
        }

        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        var decideResponse = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/club-applications/{applicationId}/decision", adminToken,
            new { Status = "Rejected", ReviewNote = "Yeterli üye ilgisi yok." });
        Assert.Equal(HttpStatusCode.OK, decideResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var application = await db.ClubApplications.SingleAsync(a => a.Id == applicationId);
            Assert.Equal(ApplicationStatus.Rejected, application.Status);
            Assert.Null(application.CreatedClubId);
            Assert.False(await db.Clubs.AnyAsync(c => c.Name == proposedName));
        }

        // Aynı başvuru için ikinci bir karar reddedilir (idempotentlik — MembershipApplicationManager precedent'i).
        var secondDecision = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/club-applications/{applicationId}/decision", adminToken, new { Status = "Approved" });
        Assert.Equal(HttpStatusCode.Conflict, secondDecision.StatusCode);

        await WaitForNotificationJobToSucceedAsync(TimeSpan.FromSeconds(10));
        Assert.Contains(_factory.EmailSender.SentEmails, e => e.ToAddress == OtherStudentEmail);
    }

    private async Task WaitForNotificationJobToSucceedAsync(TimeSpan timeout)
    {
        using var scope = _factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<JobStorage>();
        var monitoringApi = storage.GetMonitoringApi();

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var succeeded = monitoringApi.SucceededJobs(0, 100);
            if (succeeded.Any(j => j.Value?.Job?.Type == typeof(ClubApplicationDecisionNotificationJob)))
            {
                return;
            }

            await Task.Delay(250);
        }

        Assert.Fail("Hangfire bildirim işi zaman aşımı içinde Succeeded durumuna ulaşmadı.");
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return body!.AccessToken;
    }

    private async Task<HttpResponseMessage> SendWithBearerAsync(HttpMethod method, string requestUri, string accessToken, object? body = null)
    {
        using var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
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

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public DateTime AccessTokenExpiresAtUtc { get; init; }

        public string CsrfToken { get; init; } = string.Empty;
    }

    /// <summary>docs/MIMARI.md · A-66: güncel dönemin pencere alanlarını testin istediği hâle getirir.</summary>
    private async Task SetWindowAsync(ClubApplicationWindowOverride windowOverride, DateTime? startUtc, DateTime? endUtc)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var term = await db.AcademicTerms.SingleAsync(t => t.IsCurrent);

        term.ClubApplicationOverride = windowOverride;
        term.ClubApplicationStartUtc = startUtc;
        term.ClubApplicationEndUtc = endUtc;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Y-34: pencere testleri arka arkaya başvuru gönderiyor, ama bir öğrenci dönem başına yalnızca
    /// BİR bekleyen başvuru yapabilir (DuplicatePendingClubApplication). Temizlik olmadan ikinci
    /// "açık" senaryosu 409 alır ve test pencereyi değil, çift başvuru kuralını ölçer.
    /// </summary>
    private async Task ClearPendingApplicationsAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(email);
        var student = await db.Students.SingleAsync(s => s.ApplicationUserId == user!.Id);

        var pending = await db.ClubApplications
            .Where(a => a.StudentId == student.Id && a.Status == ApplicationStatus.Pending)
            .ToListAsync();

        db.ClubApplications.RemoveRange(pending);
        await db.SaveChangesAsync();
    }

    private async Task<HttpResponseMessage> SubmitApplicationAsync(string accessToken, string email)
    {
        await ClearPendingApplicationsAsync(email);

        return await SubmitWithAllDocumentsAsync(accessToken, $"Pencere Kulübü {Guid.NewGuid():N}"[..30]);
    }

    private static byte[] FakePdfBytes() =>
        [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37, 0x0A, 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A];

    private static byte[] FakePngBytes() =>
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    /// <summary>Katalogdaki zorunlu evrak tiplerinin kimlikleri (A-62).</summary>
    private async Task<List<int>> GetRequiredDocumentTypeIdsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.ClubDocumentTypes
            .Where(t => t.IsActive && t.IsRequired)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => t.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Faz 33: uç artık multipart. Evrak bütünlüğünü (Y-71) sınamayan testler tam evrak
    /// göndermek zorunda — aksi hâlde ölçtükleri kural yerine "eksik evrak" kuralına takılırlar.
    /// </summary>
    private async Task<HttpResponseMessage> SubmitWithAllDocumentsAsync(
        string accessToken, string proposedName, int? proposedCategoryId = null)
    {
        var requiredIds = await GetRequiredDocumentTypeIdsAsync();
        var documents = requiredIds
            .Select((id, index) => (TypeId: id, Bytes: FakePdfBytes(), FileName: $"evrak{index}.pdf"))
            .ToList();

        return await SubmitMultipartAsync(accessToken, proposedName, documents, proposedCategoryId);
    }

    private async Task<HttpResponseMessage> SubmitMultipartAsync(
        string accessToken,
        string proposedName,
        IReadOnlyList<(int TypeId, byte[] Bytes, string FileName)> documents,
        int? proposedCategoryId = null)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(proposedName), "ProposedName" },
            { new StringContent("Evrak testi"), "Description" },
            { new StringContent("Evrak testi gerekçesi"), "Justification" },
            { new StringContent(_proposedAdvisorId.ToString(CultureInfo.InvariantCulture)), "ProposedAdvisorId" },
        };

        if (proposedCategoryId is { } categoryId)
        {
            content.Add(new StringContent(categoryId.ToString(CultureInfo.InvariantCulture)), "ProposedCategoryId");
        }

        for (var i = 0; i < documents.Count; i++)
        {
            content.Add(new StringContent(documents[i].TypeId.ToString(CultureInfo.InvariantCulture)), $"Documents[{i}].DocumentTypeId");

            var fileContent = new ByteArrayContent(documents[i].Bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, $"Documents[{i}].File", documents[i].FileName);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/club-applications") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await _client.SendAsync(request);
    }

    [Fact(DisplayName = "Y-71: eksik zorunlu evrakla başvuru 400 alır ve HİÇBİR kayıt yazılmaz")]
    public async Task Submit_MissingRequiredDocuments_ReturnsValidationErrorAndWritesNothing()
    {
        var requiredIds = await GetRequiredDocumentTypeIdsAsync();
        Assert.NotEmpty(requiredIds);

        var proposedName = $"Eksik Evrak {Guid.NewGuid():N}"[..30];
        await ClearPendingApplicationsAsync(OtherStudentEmail);
        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);

        // Yalnızca ilk zorunlu evrak yüklenir — geri kalanı eksik.
        var response = await SubmitMultipartAsync(studentToken, proposedName,
            [(requiredIds[0], FakePdfBytes(), "evrak1.pdf")]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.ClubApplications.AnyAsync(a => a.ProposedName == proposedName));
    }

    [Fact(DisplayName = "O-22: tam evrakla başvuru kabul edilir, evraklar Protected görünürlükle saklanır")]
    public async Task Submit_AllRequiredDocuments_IsAcceptedAndStoredProtected()
    {
        var requiredIds = await GetRequiredDocumentTypeIdsAsync();
        var proposedName = $"Tam Evrak {Guid.NewGuid():N}"[..30];
        await ClearPendingApplicationsAsync(OtherStudentEmail);
        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);

        var response = await SubmitWithAllDocumentsAsync(studentToken, proposedName);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var application = await db.ClubApplications.SingleAsync(a => a.ProposedName == proposedName);
        var stored = await db.ClubApplicationDocuments.Where(d => d.ClubApplicationId == application.Id).ToListAsync();
        Assert.Equal(requiredIds.Count, stored.Count);

        var fileIds = stored.Select(d => d.StoredFileId).ToList();
        var files = await db.StoredFiles.Where(f => fileIds.Contains(f.Id)).ToListAsync();

        // Y-70: adli sicil/kurucu üye dilekçesi kişisel veridir — Public kaydedilemez.
        Assert.All(files, f => Assert.Equal(FileVisibility.Protected, f.Visibility));
        Assert.All(files, f => Assert.Equal("application/pdf", f.ContentType));
        // Y-40: ad sunucuda üretilir — istemcinin verdiği "evrak0.pdf" saklanmaz.
        Assert.All(files, f => Assert.DoesNotContain("evrak", f.GeneratedFileName, StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "A-64: evrak yerine PNG yüklenirse 400 alınır ve kayıt yazılmaz")]
    public async Task Submit_NonPdfDocument_ReturnsValidationError()
    {
        var requiredIds = await GetRequiredDocumentTypeIdsAsync();
        var proposedName = $"Yanlis Tip {Guid.NewGuid():N}"[..30];
        await ClearPendingApplicationsAsync(OtherStudentEmail);
        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);

        var documents = requiredIds
            .Select((id, index) => (TypeId: id, Bytes: index == 0 ? FakePngBytes() : FakePdfBytes(), FileName: $"evrak{index}.pdf"))
            .ToList();

        var response = await SubmitMultipartAsync(studentToken, proposedName, documents);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.ClubApplications.AnyAsync(a => a.ProposedName == proposedName));
    }

    [Fact(DisplayName = "A-63/Y-70: başvuran ve yönetici evrağı indirir; başka öğrenci 403 alır; anonim uçtan erişilemez")]
    public async Task DocumentDownload_EnforcesOwnershipAndVisibility()
    {
        var proposedName = $"Indirme {Guid.NewGuid():N}"[..30];
        await ClearPendingApplicationsAsync(OtherStudentEmail);
        var ownerToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);
        Assert.Equal(HttpStatusCode.OK, (await SubmitWithAllDocumentsAsync(ownerToken, proposedName)).StatusCode);

        int applicationId;
        int documentId;
        int storedFileId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var application = await db.ClubApplications.SingleAsync(a => a.ProposedName == proposedName);
            applicationId = application.Id;
            var document = await db.ClubApplicationDocuments.FirstAsync(d => d.ClubApplicationId == applicationId);
            documentId = document.Id;
            storedFileId = document.StoredFileId;
        }

        var url = $"/api/club-applications/{applicationId}/documents/{documentId}";

        // 1. Başvuran indirebilir.
        var ownerResponse = await SendWithBearerAsync(HttpMethod.Get, url, ownerToken);
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.Equal("application/pdf", ownerResponse.Content.Headers.ContentType?.MediaType);

        // 2. Yönetici indirebilir.
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        Assert.Equal(HttpStatusCode.OK, (await SendWithBearerAsync(HttpMethod.Get, url, adminToken)).StatusCode);

        // 3. Başka bir öğrenci indiremez — Y-70.
        var strangerToken = await LoginAsync(MemberEmail, MemberPassword);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendWithBearerAsync(HttpMethod.Get, url, strangerToken)).StatusCode);

        // 4. Anonim dosya ucu Protected kaydı GÖREMEZ — Y-52/Y-70 ikinci savunma katmanı.
        var anonymousResponse = await _client.GetAsync($"/api/files/{storedFileId}");
        Assert.Equal(HttpStatusCode.NotFound, anonymousResponse.StatusCode);
    }

    [Fact(DisplayName = "K-37: inceleme listesi her başvurunun evraklarını kod ve adla taşır")]
    public async Task GetPending_IncludesDocuments()
    {
        var proposedName = $"Liste Evrak {Guid.NewGuid():N}"[..30];
        await ClearPendingApplicationsAsync(OtherStudentEmail);
        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);
        Assert.Equal(HttpStatusCode.OK, (await SubmitWithAllDocumentsAsync(studentToken, proposedName)).StatusCode);

        // A-50/Y-11: sunucu pageSize'ı 100'e kırpar; kendi başvurumuzu bulmak için son sayfaya
        // değil, bu başvurunun kimliğine bakıyoruz.
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        var response = await SendWithBearerAsync(HttpMethod.Get, "/api/club-applications?pageIndex=0&pageSize=100", adminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(proposedName, body, StringComparison.Ordinal);
        Assert.Contains("FR-0230", body, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "O-20: onayla doğan kulüp beş varsayılan rol tanımıyla gelir")]
    public async Task Approve_CreatesDefaultRoleDefinitions()
    {
        var proposedName = $"Varsayilan Rol {Guid.NewGuid():N}"[..30];
        await ClearPendingApplicationsAsync(OtherStudentEmail);
        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);
        Assert.Equal(HttpStatusCode.OK, (await SubmitWithAllDocumentsAsync(studentToken, proposedName)).StatusCode);

        int applicationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            applicationId = (await db.ClubApplications.SingleAsync(a => a.ProposedName == proposedName)).Id;
        }

        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        var decision = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/club-applications/{applicationId}/decision", adminToken,
            new { Status = "Approved", ReviewNote = (string?)null });
        Assert.Equal(HttpStatusCode.OK, decision.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var application = await db.ClubApplications.SingleAsync(a => a.Id == applicationId);
            Assert.NotNull(application.CreatedClubId);

            var definitions = await db.ClubRoleDefinitions
                .Where(d => d.ClubId == application.CreatedClubId!.Value)
                .OrderBy(d => d.DisplayOrder)
                .ToListAsync();

            Assert.Equal(5, definitions.Count);
            Assert.Equal("Başkan", definitions[0].Name);
            Assert.Equal(ClubRole.President, definitions[0].ClubRole);
        }
    }

    [Fact(DisplayName = "A-62: aynı başvuruya aynı evrak tipi iki kez yüklenemez (bileşik unique index)")]
    public async Task ClubApplicationDocument_DuplicateTypePerApplication_IsRejected()
    {
        var proposedName = $"Evrak Unique {Guid.NewGuid():N}"[..30];
        await ClearPendingApplicationsAsync(OtherStudentEmail);
        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);
        Assert.Equal(HttpStatusCode.OK, (await SubmitWithAllDocumentsAsync(studentToken, proposedName)).StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var application = await db.ClubApplications.SingleAsync(a => a.ProposedName == proposedName);
        var existing = await db.ClubApplicationDocuments.FirstAsync(d => d.ClubApplicationId == application.Id);

        // Aynı (başvuru, tip) çifti için ikinci satır — index bunu DB seviyesinde reddetmeli.
        db.ClubApplicationDocuments.Add(new ClubApplicationDocument
        {
            ClubApplicationId = existing.ClubApplicationId,
            ClubDocumentTypeId = existing.ClubDocumentTypeId,
            StoredFileId = existing.StoredFileId,
        });

        await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
    }

    [Theory(DisplayName = "Y-73: pencere kapalıyken başvuru 409, açıkken kabul — dört senaryo")]
    [InlineData(ClubApplicationWindowOverride.ForceOpen, -10, -5, true)]
    [InlineData(ClubApplicationWindowOverride.ForceClosed, -2, 2, false)]
    [InlineData(ClubApplicationWindowOverride.FollowSchedule, -2, 2, true)]
    [InlineData(ClubApplicationWindowOverride.FollowSchedule, 5, 10, false)]
    public async Task Submit_RespectsApplicationWindow(
        ClubApplicationWindowOverride windowOverride, int startOffsetDays, int endOffsetDays, bool expectedOpen)
    {
        var now = DateTime.UtcNow;
        await SetWindowAsync(windowOverride, now.AddDays(startOffsetDays), now.AddDays(endOffsetDays));

        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);
        var response = await SubmitApplicationAsync(studentToken, OtherStudentEmail);

        if (expectedOpen)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        else
        {
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
    }

    [Fact(DisplayName = "A-66: FollowSchedule + takvim tanımsız → başvuru 409 (fail-closed)")]
    public async Task Submit_FollowScheduleWithoutDates_ReturnsConflict()
    {
        await SetWindowAsync(ClubApplicationWindowOverride.FollowSchedule, null, null);

        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);
        var response = await SubmitApplicationAsync(studentToken, OtherStudentEmail);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact(DisplayName = "Y-73: okuma ucu ile muhafız aynı cevabı verir — beş senaryoda da")]
    public async Task Window_Endpoint_AgreesWithSubmitGuard()
    {
        var now = DateTime.UtcNow;
        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);

        var scenarios = new (ClubApplicationWindowOverride Override, DateTime? Start, DateTime? End)[]
        {
            (ClubApplicationWindowOverride.ForceOpen, now.AddDays(-10), now.AddDays(-5)),
            (ClubApplicationWindowOverride.ForceClosed, now.AddDays(-2), now.AddDays(2)),
            (ClubApplicationWindowOverride.FollowSchedule, now.AddDays(-2), now.AddDays(2)),
            (ClubApplicationWindowOverride.FollowSchedule, now.AddDays(5), now.AddDays(10)),
            (ClubApplicationWindowOverride.FollowSchedule, null, null),
        };

        foreach (var scenario in scenarios)
        {
            await SetWindowAsync(scenario.Override, scenario.Start, scenario.End);

            var windowResponse = await SendWithBearerAsync(HttpMethod.Get, "/api/club-applications/window", studentToken);
            Assert.Equal(HttpStatusCode.OK, windowResponse.StatusCode);
            var windowBody = await windowResponse.Content.ReadFromJsonAsync<WindowProbe>();

            var submitResponse = await SubmitApplicationAsync(studentToken, OtherStudentEmail);
            var submitAccepted = submitResponse.StatusCode == HttpStatusCode.OK;

            Assert.True(
                windowBody!.IsOpen == submitAccepted,
                $"Okuma ucu IsOpen={windowBody.IsOpen} derken gönderim {submitResponse.StatusCode} döndü ({scenario.Override}).");
        }
    }

    [Fact(DisplayName = "K-39: pencere kapalıyken yönetici bekleyen başvuruyu yine de karara bağlayabilir")]
    public async Task Decide_WorksWhileWindowIsClosed()
    {
        var now = DateTime.UtcNow;

        // Önce pencereyi aç ve bir başvuru üret.
        await SetWindowAsync(ClubApplicationWindowOverride.ForceOpen, null, null);
        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);
        var submitResponse = await SubmitApplicationAsync(studentToken, OtherStudentEmail);
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        int applicationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            applicationId = (await db.ClubApplications
                .Where(a => a.Status == ApplicationStatus.Pending)
                .OrderByDescending(a => a.Id)
                .FirstAsync()).Id;
        }

        // Sonra pencereyi kapat.
        await SetWindowAsync(ClubApplicationWindowOverride.ForceClosed, now.AddDays(-2), now.AddDays(2));

        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        var decisionResponse = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/club-applications/{applicationId}/decision", adminToken,
            new { Status = "Rejected", ReviewNote = "Pencere kapalıyken karar" });

        Assert.Equal(HttpStatusCode.OK, decisionResponse.StatusCode);
    }

    private sealed record WindowProbe(bool IsOpen, DateTime? StartUtc, DateTime? EndUtc, string Override, string TermName);

    [Fact(DisplayName = "K-35: başvurudaki kategori onayda doğan kulübe taşınır")]
    public async Task Approve_CarriesProposedCategoryToCreatedClub()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var proposedName = $"Kategorili Kulüp {suffix}";
        int categoryId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = new ClubCategory { Name = $"Başvuru Kategorisi {suffix}" };
            db.ClubCategories.Add(category);
            await db.SaveChangesAsync();
            categoryId = category.Id;
        }

        await ClearPendingApplicationsAsync(OtherStudentEmail);
        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);

        var submitResponse = await SubmitWithAllDocumentsAsync(studentToken, proposedName, categoryId);
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        int applicationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var application = await db.ClubApplications.SingleAsync(a => a.ProposedName == proposedName);
            Assert.Equal(categoryId, application.ProposedCategoryId);
            applicationId = application.Id;
        }

        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        var decisionResponse = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/club-applications/{applicationId}/decision", adminToken,
            new { Status = "Approved", ReviewNote = (string?)null });
        Assert.Equal(HttpStatusCode.OK, decisionResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var createdClub = await db.Clubs.SingleAsync(c => c.Name == proposedName);
            Assert.Equal(categoryId, createdClub.ClubCategoryId);
        }
    }

    [Fact(DisplayName = "K-35: var olmayan kategori ile başvuru 404 alır ve kayıt yazılmaz")]
    public async Task Submit_UnknownCategory_ReturnsNotFound()
    {
        var proposedName = $"Hayalet Kategori {Guid.NewGuid():N}"[..30];

        await ClearPendingApplicationsAsync(OtherStudentEmail);
        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);

        var response = await SubmitWithAllDocumentsAsync(studentToken, proposedName, 999_999);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await verifyDb.ClubApplications.AnyAsync(a => a.ProposedName == proposedName));
    }
}
