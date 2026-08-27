using System.Linq.Expressions;
using AutoMapper;
using Business.Concrete;
using Business.DTOs.Clubs;
using Business.Mappings;
using Core.DataAccess;
using Core.Utilities.Time;
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
    private readonly Mock<IEntityRepository<ClubCategory>> _clubCategoryRepository = new();
    private readonly Mock<IEntityRepository<ClubRoleDefinition>> _clubRoleDefinitionRepository = new();
    private readonly Mock<IEntityRepository<MembershipApplication>> _membershipApplicationRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();
    private readonly IMapper _mapper;
    private readonly ClubManager _sut;

    public ClubManagerTests()
    {
        var mapperConfiguration = new MapperConfiguration(cfg => cfg.AddProfile<ClubMappingProfile>(), NullLoggerFactory.Instance);
        _mapper = mapperConfiguration.CreateMapper();
        _sut = new ClubManager(
            _clubRepository.Object,
            _academicStaffRepository.Object,
            _clubCategoryRepository.Object,
            _clubRoleDefinitionRepository.Object,
            _membershipApplicationRepository.Object,
            _unitOfWork.Object,
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
}
