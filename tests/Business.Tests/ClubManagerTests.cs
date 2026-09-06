using System.Linq.Expressions;
using AutoMapper;
using Business.Concrete;
using Business.DTOs.Clubs;
using Business.Mappings;
using Core.DataAccess;
using Core.Utilities.Security;
using Core.Utilities.Time;
using DataAccess.Repositories;
using DataAccess.Seed;
using Entities;
using Entities.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>docs/PLAN-V2.md §9: A-20 gereği her iş kuralı için bir kabul + bir ret.</summary>
public class ClubManagerTests
{
    private readonly Mock<IEntityRepository<Club>> _clubRepository = new();
    private readonly Mock<IEntityRepository<AcademicStaff>> _academicStaffRepository = new();
    private readonly Mock<IEntityRepository<ClubCategoryAssignment>> _clubCategoryAssignmentRepository = new();
    private readonly Mock<IEntityRepository<ClubRoleDefinition>> _clubRoleDefinitionRepository = new();
    private readonly Mock<IEntityRepository<MembershipApplication>> _membershipApplicationRepository = new();
    private readonly Mock<IEntityRepository<Student>> _studentRepository = new();
    private readonly Mock<IEntityRepository<ClubMembership>> _clubMembershipRepository = new();
    private readonly Mock<IEntityRepository<AcademicTerm>> _academicTermRepository = new();
    private readonly Mock<IEntityRepository<ClubSocialLink>> _clubSocialLinkRepository = new();
    private readonly Mock<IClubCategoryAssignmentDal> _clubCategoryAssignmentDal = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();
    private readonly IMapper _mapper;
    private readonly ClubManager _sut;

    public ClubManagerTests()
    {
        var mapperConfiguration = new MapperConfiguration(cfg => cfg.AddProfile<ClubMappingProfile>(), NullLoggerFactory.Instance);
        _mapper = mapperConfiguration.CreateMapper();
        _currentUser.Setup(c => c.Permissions).Returns([]);
        _clubSocialLinkRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubSocialLink, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _clubCategoryAssignmentRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubCategoryAssignment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _clubCategoryAssignmentDal
            .Setup(d => d.GetNamesByClubAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, IReadOnlyList<string>>());
        _sut = new ClubManager(
            _clubRepository.Object,
            _academicStaffRepository.Object,
            _clubCategoryAssignmentRepository.Object,
            _clubRoleDefinitionRepository.Object,
            _membershipApplicationRepository.Object,
            _studentRepository.Object,
            _clubMembershipRepository.Object,
            _academicTermRepository.Object,
            _clubSocialLinkRepository.Object,
            _clubCategoryAssignmentDal.Object,
            _unitOfWork.Object,
            _currentUser.Object,
            _clock.Object,
            _mapper);
    }

    [Fact(DisplayName = "GetListPagedAsync: sayfa boyutu 100'ü aşarsa 100'e kırpılır (Y-11/A-16)")]
    public async Task GetListPagedAsync_PageSizeAbove100_ClampedTo100()
    {
        _clubRepository
            .Setup(r => r.GetListPagedAsync(
                0,
                100,
                It.IsAny<System.Linq.Expressions.Expression<Func<Club, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Club, string>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Club>([], 0, 0, 100));

        var result = await _sut.GetListPagedAsync(0, 500);

        Assert.True(result.IsSuccess);
        _clubRepository.Verify(
            r => r.GetListPagedAsync(
                0,
                100,
                It.IsAny<System.Linq.Expressions.Expression<Func<Club, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Club, string>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "GetByIdAsync: bulunamayan kulüp NotFound döner")]
    public async Task GetByIdAsync_NotFound_ReturnsNotFound()
    {
        _clubRepository
            .Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Club?)null);

        var result = await _sut.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(Core.Utilities.Results.ResultStatus.NotFound, result.Status);
    }

    [Fact(DisplayName = "CreateAsync: aynı isimde kulüp varsa Conflict döner")]
    public async Task CreateAsync_NameTaken_ReturnsConflict()
    {
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Club { Id = 1, Name = "Satranç Kulübü", AdvisorId = 1, IsActive = true, CreatedAtUtc = DateTime.UtcNow });

        var result = await _sut.CreateAsync(new CreateClubRequestDto { Name = "Satranç Kulübü", AdvisorId = 1 });

        Assert.False(result.IsSuccess);
        Assert.Equal(Core.Utilities.Results.ResultStatus.Conflict, result.Status);
        _clubRepository.Verify(r => r.AddAsync(It.IsAny<Club>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "CreateAsync: geçerli danışman ile yeni kulüp oluşturulur")]
    public async Task CreateAsync_ValidAdvisor_ReturnsSuccess()
    {
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Club?)null);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 7, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });

        var result = await _sut.CreateAsync(new CreateClubRequestDto { Name = "Yeni Kulüp", AdvisorId = 7 });

        Assert.True(result.IsSuccess);
        _clubRepository.Verify(r => r.AddAsync(It.IsAny<Club>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "CreateAsync: olmayan danışman ile kulüp oluşturulamaz")]
    public async Task CreateAsync_AdvisorNotFound_ReturnsNotFound()
    {
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Club?)null);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcademicStaff?)null);

        var result = await _sut.CreateAsync(new CreateClubRequestDto { Name = "Yeni Kulüp", AdvisorId = 999 });

        Assert.False(result.IsSuccess);
        Assert.Equal(Core.Utilities.Results.ResultStatus.NotFound, result.Status);
    }

    [Fact(DisplayName = "SetStatusAsync: bekleyen başvurusu olan kulüp pasife alınamaz")]
    public async Task SetStatusAsync_HasPendingApplications_ReturnsConflict()
    {
        var club = new Club { Id = 1, Name = "Satranç Kulübü", AdvisorId = 1, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);
        _membershipApplicationRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<MembershipApplication, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MembershipApplication { Id = 1, ClubId = 1, StudentId = 1, AcademicTermId = 1, Status = ApplicationStatus.Pending, AppliedAtUtc = DateTime.UtcNow });

        var result = await _sut.SetStatusAsync(1, new SetClubStatusRequestDto { IsActive = false });

        Assert.False(result.IsSuccess);
        Assert.Equal(Core.Utilities.Results.ResultStatus.Conflict, result.Status);
        Assert.True(club.IsActive);
    }

    [Fact(DisplayName = "SetStatusAsync: bekleyen başvuru yoksa pasife alınabilir")]
    public async Task SetStatusAsync_NoPendingApplications_ReturnsSuccess()
    {
        var club = new Club { Id = 1, Name = "Satranç Kulübü", AdvisorId = 1, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);
        _membershipApplicationRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<MembershipApplication, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MembershipApplication?)null);

        var result = await _sut.SetStatusAsync(1, new SetClubStatusRequestDto { IsActive = false });

        Assert.True(result.IsSuccess);
        Assert.False(club.IsActive);
    }

    [Fact(DisplayName = "GetByIdAsync: üye olmayan öğrenci için ilişki None döner (A-75)")]
    public async Task GetByIdAsync_ReportsNoneRelationship_ForNonMember()
    {
        var club = new Club { Id = 1, Name = "Kulüp", AdvisorId = 99, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);
        _currentUser.Setup(c => c.UserId).Returns(500);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcademicStaff?)null);
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicTerm { Id = 5, Name = "2026-Güz", StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow.AddMonths(4), IsCurrent = true });
        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = 10, ApplicationUserId = 500, StudentNumber = "S1", DepartmentId = 1, EnrollmentYear = 2026 });
        _clubMembershipRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClubMembership?)null);

        var result = await _sut.GetByIdAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(ClubRelationship.None, result.Data!.MyRelationship);
        Assert.Equal(ClubCapability.None, result.Data.MyCapabilities);
    }

    [Fact(DisplayName = "GetByIdAsync: başkan için ilişki President ve MembersManage kapasitesi döner (A-75)")]
    public async Task GetByIdAsync_ReportsPresidentCapabilities_ForPresident()
    {
        var club = new Club { Id = 1, Name = "Kulüp", AdvisorId = 99, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);
        _currentUser.Setup(c => c.UserId).Returns(600);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcademicStaff?)null);
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicTerm { Id = 5, Name = "2026-Güz", StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow.AddMonths(4), IsCurrent = true });
        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = 20, ApplicationUserId = 600, StudentNumber = "S2", DepartmentId = 1, EnrollmentYear = 2026 });
        _clubMembershipRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubMembership
            {
                ClubId = 1, StudentId = 20, AcademicTermId = 5, ClubRole = ClubRole.President,
                Capabilities = ClubCapabilityDefaults.ForRole(ClubRole.President), JoinedAtUtc = DateTime.UtcNow,
            });

        var result = await _sut.GetByIdAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(ClubRelationship.President, result.Data!.MyRelationship);
        Assert.True(result.Data.MyCapabilities.HasFlag(ClubCapability.MembersManage));
    }

    [Fact(DisplayName = "GetByIdAsync: kulübün danışmanı için ilişki Advisor döner (A-75)")]
    public async Task GetByIdAsync_ReportsAdvisor_ForAdvisorOfThatClub()
    {
        var club = new Club { Id = 1, Name = "Kulüp", AdvisorId = 77, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);
        _currentUser.Setup(c => c.UserId).Returns(700);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 77, ApplicationUserId = 700, Title = "Dr.", DepartmentId = 1 });

        var result = await _sut.GetByIdAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(ClubRelationship.Advisor, result.Data!.MyRelationship);
    }

    [Fact(DisplayName = "GetByIdAsync: clubs.manage.all taşıyan yönetici için ilişki Administrator döner (A-75)")]
    public async Task GetByIdAsync_ReportsAdministrator_ForClubsManageAll()
    {
        var club = new Club { Id = 1, Name = "Kulüp", AdvisorId = 99, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);
        _currentUser.Setup(c => c.Permissions).Returns([IdentitySeedData.Permissions.ClubsManageAll]);

        var result = await _sut.GetByIdAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(ClubRelationship.Administrator, result.Data!.MyRelationship);
    }

    [Fact(DisplayName = "K-49: UpdateAsync kulübün kategorilerini topluca değiştirir")]
    public async Task UpdateAsync_ReplacesCategories()
    {
        var club = new Club { Id = 1, Name = "Kulüp", AdvisorId = 1, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Club, bool>> filter, CancellationToken _) => new[] { club }.AsQueryable().Where(filter).FirstOrDefault());

        var result = await _sut.UpdateAsync(1, new UpdateClubRequestDto { Name = "Kulüp", ClubCategoryIds = [3, 7] });

        Assert.True(result.IsSuccess);
        _clubCategoryAssignmentDal.Verify(
            d => d.ReplaceAsync(1, It.Is<IReadOnlyCollection<int>>(ids => ids.Contains(3) && ids.Contains(7)), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "K-49/Y-85: GetByIdAsync kategori adlarını tek toplu sorgudan doldurur")]
    public async Task GetByIdAsync_FillsCategoryNames()
    {
        var club = new Club { Id = 1, Name = "Kulüp", AdvisorId = 1, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);
        _clubCategoryAssignmentDal
            .Setup(d => d.GetNamesByClubAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, IReadOnlyList<string>> { [1] = ["Bilim - Teknoloji", "Sosyal Sorumluluk"] });

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(["Bilim - Teknoloji", "Sosyal Sorumluluk"], result.Data!.ClubCategoryNames);
    }
}
