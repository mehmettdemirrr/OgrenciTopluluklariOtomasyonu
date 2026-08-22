using System.Linq.Expressions;
using Business.Abstract;
using Business.Concrete;
using Business.DTOs.Reports;
using Core.DataAccess;
using Core.Utilities.Security;
using Core.Utilities.Time;
using DataAccess.Repositories;
using Entities;
using Entities.Dtos.Dashboard;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>
/// docs/PLAN-V2.md · Faz 12 (K-25): "Üç farklı rolle giriş → her biri yalnızca kendi kapsamındaki
/// sayıları görüyor" çıkış koşulunun birim seviyesindeki kanıtı — kapsam sızıntısı olmadığını
/// GetManagementStatsAsync'in HANGİ parametrelerle çağrıldığını doğrulayarak gösterir.
/// </summary>
public class DashboardManagerTests
{
    private static readonly DateTime FixedNow = new(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IDashboardDal> _dashboardDal = new();
    private readonly Mock<IReportDal> _reportDal = new();
    private readonly Mock<IReportScopeResolver> _scopeResolver = new();
    private readonly Mock<IEntityRepository<Student>> _studentRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();
    private readonly DashboardManager _sut;

    public DashboardManagerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(FixedNow);
        _sut = new DashboardManager(
            _dashboardDal.Object, _reportDal.Object, _scopeResolver.Object, _studentRepository.Object, _currentUser.Object, _clock.Object);
    }

    [Fact(DisplayName = "GetSummaryAsync: kapsamı olmayan düz üye yalnızca kişisel sayıları görür, yönetim bölümü null")]
    public async Task GetSummaryAsync_PlainMemberNoScope_ReturnsPersonalOnlyManagementNull()
    {
        _currentUser.Setup(c => c.UserId).Returns(5);
        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = 1, ApplicationUserId = 5, StudentNumber = "S1", DepartmentId = 1, EnrollmentYear = 2026 });
        _dashboardDal.Setup(d => d.GetPersonalStatsAsync(1, FixedNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersonalDashboardStatsDto { MyClubCount = 2, MyPendingApplicationCount = 1, MyUpcomingEventCount = 3 });
        _scopeResolver.Setup(s => s.ResolveAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(ReportScope.None);

        var result = await _sut.GetSummaryAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Personal.MyClubCount);
        Assert.Null(result.Data.Management);
        Assert.Empty(result.Data.TermTrend);
        _dashboardDal.Verify(
            d => d.GetManagementStatsAsync(It.IsAny<bool>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "GetSummaryAsync: reports.read.all taşıyan admin AllClubs=true kapsamıyla yönetim özeti görür")]
    public async Task GetSummaryAsync_AdminAllClubsScope_ReturnsManagementWithAllClubsTrue()
    {
        _currentUser.Setup(c => c.UserId).Returns(99);
        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);
        _scopeResolver.Setup(s => s.ResolveAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(new ReportScope(true, []));
        _dashboardDal.Setup(d => d.GetManagementStatsAsync(true, It.IsAny<IReadOnlyCollection<int>>(), FixedNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ManagementDashboardStatsDto { AllClubs = true, ScopeClubCount = 10 });

        var result = await _sut.GetSummaryAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Data!.Personal.MyClubCount);
        Assert.NotNull(result.Data.Management);
        Assert.True(result.Data.Management!.AllClubs);
        Assert.Equal(10, result.Data.Management.ScopeClubCount);
    }

    [Fact(DisplayName = "GetSummaryAsync: officer/advisor kapsamı yalnızca KENDİ kulüpleriyle GetManagementStatsAsync'e geçirilir (kapsam sızıntısı yok)")]
    public async Task GetSummaryAsync_OfficerScope_PassesOnlyOwnClubIdsToManagementStats()
    {
        _currentUser.Setup(c => c.UserId).Returns(7);
        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);
        _scopeResolver.Setup(s => s.ResolveAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(new ReportScope(false, [3]));
        _dashboardDal.Setup(d => d.GetManagementStatsAsync(false, It.Is<IReadOnlyCollection<int>>(c => c.Single() == 3), FixedNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ManagementDashboardStatsDto { AllClubs = false, ScopeClubCount = 1 });

        var result = await _sut.GetSummaryAsync();

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.Management!.AllClubs);
        _reportDal.Verify(d => d.GetTermSummaryAsync(It.Is<IReadOnlyCollection<int>?>(c => c != null && c.Single() == 3), It.IsAny<CancellationToken>()), Times.Once);
    }
}
