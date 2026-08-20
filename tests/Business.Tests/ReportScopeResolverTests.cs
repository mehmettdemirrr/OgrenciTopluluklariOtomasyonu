using System.Linq.Expressions;
using Business.Abstract;
using Business.Concrete;
using Core.DataAccess;
using DataAccess.Seed;
using Entities;
using Entities.Enums;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>docs/MIMARI.md · Y-51: kapsam token'dan değil DB'den çözülür.</summary>
public class ReportScopeResolverTests
{
    private static readonly DateTime FixedNow = new(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IIdentityGateway> _identityGateway = new();
    private readonly Mock<IEntityRepository<AcademicStaff>> _academicStaffRepository = new();
    private readonly Mock<IEntityRepository<Club>> _clubRepository = new();
    private readonly Mock<IEntityRepository<Student>> _studentRepository = new();
    private readonly Mock<IEntityRepository<ClubMembership>> _clubMembershipRepository = new();
    private readonly Mock<IEntityRepository<AcademicTerm>> _academicTermRepository = new();
    private readonly ReportScopeResolver _sut;

    public ReportScopeResolverTests()
    {
        _sut = new ReportScopeResolver(
            _identityGateway.Object,
            _academicStaffRepository.Object,
            _clubRepository.Object,
            _studentRepository.Object,
            _clubMembershipRepository.Object,
            _academicTermRepository.Object);

        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((AcademicStaff?)null);
        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Student?)null);
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((AcademicTerm?)null);
    }

    [Fact(DisplayName = "ResolveAsync: reports.read izni yoksa None döner")]
    public async Task ResolveAsync_NoReportsPermission_ReturnsNone()
    {
        var user = new ApplicationUser { Id = 1 };
        _identityGateway.Setup(g => g.FindByIdAsync(1)).ReturnsAsync(user);
        _identityGateway.Setup(g => g.GetPermissionsAsync(user)).ReturnsAsync((IReadOnlyCollection<string>)["clubs.read"]);

        var scope = await _sut.ResolveAsync(1);

        Assert.False(scope.AllClubs);
        Assert.Empty(scope.ClubIds);
        Assert.False(scope.Covers(1));
    }

    [Fact(DisplayName = "ResolveAsync: reports.read.all izni tüm kulüpleri kapsar")]
    public async Task ResolveAsync_ReportsReadAll_CoversAllClubs()
    {
        var user = new ApplicationUser { Id = 1 };
        _identityGateway.Setup(g => g.FindByIdAsync(1)).ReturnsAsync(user);
        _identityGateway
            .Setup(g => g.GetPermissionsAsync(user))
            .ReturnsAsync((IReadOnlyCollection<string>)[IdentitySeedData.Permissions.ReportsRead, IdentitySeedData.Permissions.ReportsReadAll]);

        var scope = await _sut.ResolveAsync(1);

        Assert.True(scope.AllClubs);
        Assert.True(scope.Covers(999));
    }

    [Fact(DisplayName = "ResolveAsync: danışman yalnızca kendi danışmanı olduğu kulüpleri kapsar")]
    public async Task ResolveAsync_Advisor_CoversOnlyAdvisedClubs()
    {
        var user = new ApplicationUser { Id = 1 };
        _identityGateway.Setup(g => g.FindByIdAsync(1)).ReturnsAsync(user);
        _identityGateway
            .Setup(g => g.GetPermissionsAsync(user))
            .ReturnsAsync((IReadOnlyCollection<string>)[IdentitySeedData.Permissions.ReportsRead]);

        var advisor = new AcademicStaff { Id = 10, ApplicationUserId = 1, Title = "Dr.", DepartmentId = 1 };
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(advisor);
        _clubRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Club { Id = 5, Name = "Danışılan Kulüp", AdvisorId = 10, IsActive = true, CreatedAtUtc = FixedNow }]);

        var scope = await _sut.ResolveAsync(1);

        Assert.False(scope.AllClubs);
        Assert.True(scope.Covers(5));
        Assert.False(scope.Covers(6));
    }

    [Fact(DisplayName = "ResolveAsync: Officer üyeliği güncel dönemde kendi kulübünü kapsar")]
    public async Task ResolveAsync_OfficerMembership_CoversOwnClub()
    {
        var user = new ApplicationUser { Id = 2 };
        _identityGateway.Setup(g => g.FindByIdAsync(2)).ReturnsAsync(user);
        _identityGateway
            .Setup(g => g.GetPermissionsAsync(user))
            .ReturnsAsync((IReadOnlyCollection<string>)[IdentitySeedData.Permissions.ReportsRead]);

        var student = new Student { Id = 7, ApplicationUserId = 2, StudentNumber = "20260002", DepartmentId = 1, EnrollmentYear = 2026 };
        var term = new AcademicTerm { Id = 3, Name = "2026 Güz", StartDateUtc = FixedNow.AddMonths(-1), EndDateUtc = FixedNow.AddMonths(3), IsCurrent = true };
        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(term);
        _clubMembershipRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ClubMembership { Id = 1, ClubId = 8, StudentId = 7, AcademicTermId = 3, ClubRole = ClubRole.Officer, JoinedAtUtc = FixedNow }]);

        var scope = await _sut.ResolveAsync(2);

        Assert.False(scope.AllClubs);
        Assert.True(scope.Covers(8));
        Assert.False(scope.Covers(9));
    }
}
