using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Business.BackgroundJobs;
using DataAccess;
using Entities;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>docs/PLAN-V2.md · Faz 13: ReportType.TermSummary — ClubMembers/EventParticipants ile aynı kuyruk/indirme akışı.</summary>
[Collection("WebAPI Integration Tests")]
public sealed class TermSummaryReportTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string AdminEmail = "term-summary-admin@test.local";
    private const string AdminPassword = "Admin!Test123456";

    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public TermSummaryReportTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(AdminEmail) is null)
        {
            var admin = new ApplicationUser { UserName = AdminEmail, Email = AdminEmail, EmailConfirmed = true };
            var createResult = await userManager.CreateAsync(admin, AdminPassword);
            Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        _client = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "TermSummary: ClubId/EventId olmadan talep edilir, kuyruktan geçer ve xlsx olarak indirilir")]
    public async Task RequestTermSummary_QueuesGeneratesAndDownloads()
    {
        var adminToken = await LoginAndGetAccessTokenAsync();

        var requestResponse = await SendWithBearerAsync(HttpMethod.Post, "/api/reports", adminToken, new { ReportType = "TermSummary" });
        Assert.Equal(HttpStatusCode.OK, requestResponse.StatusCode);

        int reportRequestId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = await userManager.FindByEmailAsync(AdminEmail);
            var reportRequest = await db.ReportRequests
                .Where(r => r.RequestedByUserId == admin!.Id && r.ReportType == "TermSummary")
                .OrderByDescending(r => r.Id)
                .FirstAsync();
            reportRequestId = reportRequest.Id;
        }

        await WaitForReportGenerationSucceededAsync(reportRequestId, TimeSpan.FromSeconds(15));

        var downloadResponse = await SendWithBearerAsync(HttpMethod.Get, $"/api/reports/{reportRequestId}/file", adminToken);
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            downloadResponse.Content.Headers.ContentType?.MediaType);

        var bytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 2 && bytes[0] == (byte)'P' && bytes[1] == (byte)'K', "xlsx içeriği ZIP (PK) imzasıyla başlamalı.");
    }

    private async Task WaitForReportGenerationSucceededAsync(int reportRequestId, TimeSpan timeout)
    {
        using var scope = _factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<JobStorage>();
        var monitoringApi = storage.GetMonitoringApi();

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var succeeded = monitoringApi.SucceededJobs(0, 100);
            if (succeeded.Any(j =>
                    j.Value?.Job?.Type == typeof(ReportGenerationJob) &&
                    j.Value.Job.Args.Count > 0 &&
                    Equals(j.Value.Job.Args[0], reportRequestId)))
            {
                return;
            }

            await Task.Delay(250);
        }

        Assert.Fail("Hangfire rapor üretim işi zaman aşımı içinde Succeeded durumuna ulaşmadı.");
    }

    private async Task<string> LoginAndGetAccessTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = AdminEmail, Password = AdminPassword });
        response.EnsureSuccessStatusCode();

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

    private sealed class AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public DateTime AccessTokenExpiresAtUtc { get; init; }

        public string CsrfToken { get; init; } = string.Empty;
    }
}
