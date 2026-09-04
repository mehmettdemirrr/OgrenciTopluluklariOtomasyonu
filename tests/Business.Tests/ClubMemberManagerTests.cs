using System.Linq.Expressions;
using Business.Concrete;
using Business.DTOs.Clubs;
using Core.DataAccess;
using Core.Utilities.Results;
using Core.Utilities.Security;
using Core.Utilities.Time;
using DataAccess.Seed;
using Entities;
using Entities.Enums;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>
/// docs/PLAN-V2.md §9: A-39 (başkan tekilliği) ve Y-23 (kaynak kapsamı) iş kurallarının birim testleri.
/// A-20 gereği her kural için bir kabul + bir ret.
/// </summary>
public class ClubMemberManagerTests
{
    private static readonly DateTime FixedNow = new(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IEntityRepository<Club>> _clubRepository = new();
    private readonly Mock<IEntityRepository<ClubMembership>> _clubMembershipRepository = new();

    // Faz 34 (K-36): unvan kataloğu.
    private readonly Mock<IEntityRepository<ClubRoleDefinition>> _clubRoleDefinitionRepository = new();

    private readonly Mock<IEntityRepository<Student>> _studentRepository = new();
    private readonly Mock<IEntityRepository<AcademicStaff>> _academicStaffRepository = new();
    private readonly Mock<IEntityRepository<AcademicTerm>> _academicTermRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();
    private readonly ClubMemberManager _sut;

    private readonly Club _club = new() { Id = 1, Name = "Satranç Kulübü", AdvisorId = 10, IsActive = true, CreatedAtUtc = FixedNow };
    private readonly AcademicTerm _term = new() { Id = 1, Name = "2026-Güz", StartDateUtc = FixedNow.AddMonths(-1), EndDateUtc = FixedNow.AddMonths(3), IsCurrent = true };

    public ClubMemberManagerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(FixedNow);
        _currentUser.Setup(c => c.Permissions).Returns(Array.Empty<string>());
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(_club);
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(_term);
        _studentRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        _sut = new ClubMemberManager(
            _clubRepository.Object,
            _clubMembershipRepository.Object,
            _clubRoleDefinitionRepository.Object,
            _studentRepository.Object,
            _academicStaffRepository.Object,
            _academicTermRepository.Object,
            _unitOfWork.Object,
            _currentUser.Object,
            _clock.Object);
    }

    [Fact(DisplayName = "GetMembersPagedAsync: kulübün danışmanı üye listesini görebilir")]
    public async Task GetMembersPagedAsync_Advisor_ReturnsSuccess()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });
        _clubMembershipRepository
            .Setup(r => r.GetListPagedAsync(0, 20, It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ClubMembership>([], 0, 0, 20));

        var result = await _sut.GetMembersPagedAsync(1, 0, 20);

        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "GetMembersPagedAsync: danışman/yetkili olmayan düz üye listeyi göremez")]
    public async Task GetMembersPagedAsync_PlainMember_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(200);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcademicStaff?)null);
        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = 5, ApplicationUserId = 200, StudentNumber = "S1", DepartmentId = 1, EnrollmentYear = 2026 });
        _clubMembershipRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubMembership { Id = 1, ClubId = 1, StudentId = 5, AcademicTermId = 1, ClubRole = ClubRole.Member, JoinedAtUtc = FixedNow });

        var result = await _sut.GetMembersPagedAsync(1, 0, 20);

        Assert.False(result.IsSuccess);
        Assert.Equal(Core.Utilities.Results.ResultStatus.Forbidden, result.Status);
    }

    [Fact(DisplayName = "SetRoleAsync: danışman bir öğrenciyi President yapabilir — Officer/President'ı canlandıran kural")]
    public async Task SetRoleAsync_AdvisorPromotesToPresident_ReturnsSuccess()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        var membership = new ClubMembership { Id = 1, ClubId = 1, StudentId = 5, AcademicTermId = 1, ClubRole = ClubRole.Member, JoinedAtUtc = FixedNow };
        _clubMembershipRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ClubMembership, bool>> filter, CancellationToken _) => filter.Compile()(membership) ? membership : null);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });

        var result = await _sut.SetRoleAsync(1, 1, new SetClubRoleRequestDto { ClubRole = ClubRole.President });

        Assert.True(result.IsSuccess);
        Assert.Equal(ClubRole.President, membership.ClubRole);
    }

    [Fact(DisplayName = "SetRoleAsync: kulüpte zaten bir President varsa ikinci atama Conflict döner (A-39)")]
    public async Task SetRoleAsync_ClubAlreadyHasPresident_ReturnsConflict()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        var target = new ClubMembership { Id = 1, ClubId = 1, StudentId = 5, AcademicTermId = 1, ClubRole = ClubRole.Member, JoinedAtUtc = FixedNow };
        var existingPresident = new ClubMembership { Id = 2, ClubId = 1, StudentId = 6, AcademicTermId = 1, ClubRole = ClubRole.President, JoinedAtUtc = FixedNow };

        _clubMembershipRepository
            .SetupSequence(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(target)
            .ReturnsAsync(existingPresident);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });

        var result = await _sut.SetRoleAsync(1, 1, new SetClubRoleRequestDto { ClubRole = ClubRole.President });

        Assert.False(result.IsSuccess);
        Assert.Equal(Core.Utilities.Results.ResultStatus.Conflict, result.Status);
        Assert.Equal(ClubRole.Member, target.ClubRole);
    }

    [Fact(DisplayName = "SetRoleAsync: President kendi rolünü kendi kendine düşüremez (self-lockout guardı)")]
    public async Task SetRoleAsync_PresidentDemotesSelf_ReturnsConflict()
    {
        // Başkanın kendisi işlemi yapıyor: currentUser == membership.Student.ApplicationUserId.
        _currentUser.Setup(c => c.UserId).Returns(500);
        var membership = new ClubMembership
        {
            Id = 1, ClubId = 1, StudentId = 5, AcademicTermId = 1,
            ClubRole = ClubRole.President,
            // A-68: erişim MembersManage'ten geliyor; başkan makamı tek başına yetmiyor artık.
            Capabilities = ClubCapabilityDefaults.ForRole(ClubRole.President),
            JoinedAtUtc = FixedNow,
        };
        _clubMembershipRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ClubMembership, bool>> filter, CancellationToken _) => filter.Compile()(membership) ? membership : null);
        // Danışman değil, bu yüzden "kulübün President'i" yolundan erişim kazanmalı.
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcademicStaff?)null);
        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = 5, ApplicationUserId = 500, StudentNumber = "S1", DepartmentId = 1, EnrollmentYear = 2026 });

        var result = await _sut.SetRoleAsync(1, 1, new SetClubRoleRequestDto { ClubRole = ClubRole.Officer });

        Assert.False(result.IsSuccess);
        Assert.Equal(Core.Utilities.Results.ResultStatus.Conflict, result.Status);
        Assert.Equal(ClubRole.President, membership.ClubRole);
    }

    [Fact(DisplayName = "RemoveMemberAsync: danışman üyeyi topluluktan çıkarabilir (soft delete)")]
    public async Task RemoveMemberAsync_Advisor_SoftDeletes()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        var membership = new ClubMembership { Id = 1, ClubId = 1, StudentId = 5, AcademicTermId = 1, ClubRole = ClubRole.Member, JoinedAtUtc = FixedNow };
        _clubMembershipRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ClubMembership, bool>> filter, CancellationToken _) => filter.Compile()(membership) ? membership : null);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });

        var result = await _sut.RemoveMemberAsync(1, 1);

        Assert.True(result.IsSuccess);
        Assert.True(membership.IsDeleted);
        _clubMembershipRepository.Verify(r => r.Delete(It.IsAny<ClubMembership>()), Times.Never);
    }

    [Fact(DisplayName = "RemoveMemberAsync: President kendini topluluktan çıkaramaz")]
    public async Task RemoveMemberAsync_PresidentRemovesSelf_ReturnsConflict()
    {
        _currentUser.Setup(c => c.UserId).Returns(500);
        var membership = new ClubMembership { Id = 1, ClubId = 1, StudentId = 5, AcademicTermId = 1, ClubRole = ClubRole.President, JoinedAtUtc = FixedNow };
        _clubMembershipRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ClubMembership, bool>> filter, CancellationToken _) => filter.Compile()(membership) ? membership : null);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcademicStaff?)null);
        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = 5, ApplicationUserId = 500, StudentNumber = "S1", DepartmentId = 1, EnrollmentYear = 2026 });

        var result = await _sut.RemoveMemberAsync(1, 1);

        Assert.False(result.IsSuccess);
        Assert.False(membership.IsDeleted);
    }

    // A-55: v4.0'da bu bypass `reports.read.all` ile yapılıyordu — rapor izni fiilen yönetim izni
    // gibi davranıyordu. Faz 25'te ayrı ve açık bir izne taşındı (Y-66).
    [Fact(DisplayName = "GetMembersPagedAsync: clubs.manage.all taşıyan yönetici herhangi bir kulübün üyelerini görebilir")]
    public async Task GetMembersPagedAsync_ClubsManageAllHolder_ReturnsSuccess()
    {
        _currentUser.Setup(c => c.Permissions).Returns([IdentitySeedData.Permissions.ClubsManageAll]);
        _clubMembershipRepository
            .Setup(r => r.GetListPagedAsync(0, 20, It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ClubMembership>([], 0, 0, 20));

        var result = await _sut.GetMembersPagedAsync(1, 0, 20);

        Assert.True(result.IsSuccess);
        // Blanket bypass — danışman/öğrenci sorgularına hiç gidilmemeli.
        _academicStaffRepository.Verify(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "GetMineAsync: öğrenci profili olan kullanıcı kendi üyeliklerini kulüp adıyla görür")]
    public async Task GetMineAsync_StudentWithMemberships_ReturnsOwnClubs()
    {
        _currentUser.Setup(c => c.UserId).Returns(500);
        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = 5, ApplicationUserId = 500, StudentNumber = "S1", DepartmentId = 1, EnrollmentYear = 2026 });
        var membership = new ClubMembership { Id = 1, ClubId = 1, StudentId = 5, AcademicTermId = 1, ClubRole = ClubRole.Officer, JoinedAtUtc = FixedNow };
        _clubMembershipRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([membership]);
        _clubRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_club]);

        var result = await _sut.GetMineAsync();

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Data!);
        Assert.Equal(_club.Name, item.ClubName);
        Assert.Equal(ClubRole.Officer, item.ClubRole);
        // K-41/A-70: üyelik satırı Member ilişkisiyle işaretlenir — danışman satırından ayırt edilsin diye.
        Assert.Equal(ClubRelationship.Member, item.Relationship);
    }

    [Fact(DisplayName = "GetMineAsync: ne öğrenci ne danışman profili olan kullanıcı için hata değil boş liste döner")]
    public async Task GetMineAsync_NoStudentOrAdvisorProfile_ReturnsEmptySuccess()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcademicStaff?)null);

        var result = await _sut.GetMineAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
        _clubMembershipRepository.Verify(r => r.GetListAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "K-41/A-70: danışmanı olduğu kulüp Advisor ilişkisiyle Kulüplerim listesinde görünür")]
    public async Task GetMineAsync_ReturnsAdvisorRow_ForAcademicStaff()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });
        _clubRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_club]);

        var result = await _sut.GetMineAsync();

        var item = Assert.Single(result.Data!);
        Assert.Equal(_club.Name, item.ClubName);
        Assert.Equal(ClubRelationship.Advisor, item.Relationship);
        Assert.Null(item.ClubRoleName);
    }

    [Fact(DisplayName = "A-70: danışmanlık dönemsel değildir — güncel dönem tanımsızken bile danışman kulübünü görür")]
    public async Task GetMineAsync_AdvisorRow_IgnoresMissingCurrentTerm()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });
        _clubRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_club]);
        // Güncel dönem yok — üyelik dalı bu yüzden boş dönerdi, danışman dalı buna bağlı değildir.
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcademicTerm?)null);

        var result = await _sut.GetMineAsync();

        Assert.Single(result.Data!);
    }

    [Fact(DisplayName = "A-61/Y-22: unvan atanınca ClubRole TANIMDAN gelir, istemcinin gönderdiği değer yok sayılır")]
    public async Task SetRoleAsync_WithDefinition_TakesRoleFromDefinitionNotRequest()
    {
        var membership = new ClubMembership
        {
            Id = 5, ClubId = 1, StudentId = 9, AcademicTermId = 1, ClubRole = ClubRole.Member, JoinedAtUtc = FixedNow,
        };
        _clubMembershipRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
        _clubRoleDefinitionRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubRoleDefinition, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubRoleDefinition { Id = 3, ClubId = 1, Name = "Sayman", ClubRole = ClubRole.Officer, DisplayOrder = 3 });

        GrantAdminScope();

        // İstemci "Member" diyor ama tanım "Officer" — tanım kazanmalı.
        var result = await _sut.SetRoleAsync(1, 5, new SetClubRoleRequestDto
        {
            ClubRole = ClubRole.Member,
            ClubRoleDefinitionId = 3,
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(ClubRole.Officer, membership.ClubRole);
        Assert.Equal(3, membership.ClubRoleDefinitionId);
    }

    [Fact(DisplayName = "A-61: unvansız atama bugünkü davranışı sürdürür — ClubRole istekten gelir, unvan null olur")]
    public async Task SetRoleAsync_WithoutDefinition_KeepsLegacyBehaviour()
    {
        var membership = new ClubMembership
        {
            Id = 5, ClubId = 1, StudentId = 9, AcademicTermId = 1, ClubRole = ClubRole.Member,
            ClubRoleDefinitionId = 7, JoinedAtUtc = FixedNow,
        };
        _clubMembershipRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);

        GrantAdminScope();

        var result = await _sut.SetRoleAsync(1, 5, new SetClubRoleRequestDto { ClubRole = ClubRole.Officer });

        Assert.True(result.IsSuccess);
        Assert.Equal(ClubRole.Officer, membership.ClubRole);
        Assert.Null(membership.ClubRoleDefinitionId);
    }

    [Fact(DisplayName = "O-20: başka kulübün unvanı atanamaz — 404")]
    public async Task SetRoleAsync_DefinitionFromAnotherClub_ReturnsNotFound()
    {
        _clubMembershipRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubMembership { Id = 5, ClubId = 1, StudentId = 9, AcademicTermId = 1, ClubRole = ClubRole.Member, JoinedAtUtc = FixedNow });

        // Predicate GERÇEKTEN çalıştırılır: tanım ClubId = 2, istek ClubId = 1 → eşleşme yok.
        _clubRoleDefinitionRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubRoleDefinition, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ClubRoleDefinition, bool>> filter, CancellationToken _) =>
                new[] { new ClubRoleDefinition { Id = 3, ClubId = 2, Name = "Sayman", ClubRole = ClubRole.Officer, DisplayOrder = 3 } }
                    .AsQueryable().Where(filter).FirstOrDefault());

        GrantAdminScope();

        var result = await _sut.SetRoleAsync(1, 5, new SetClubRoleRequestDto { ClubRoleDefinitionId = 3 });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact(DisplayName = "O-27: tanımın seviyesi değişince o unvanı taşıyan TÜM üyeliklerin ClubRole'ü de değişir")]
    public async Task UpdateRoleDefinitionAsync_LevelChange_CascadesToMemberships()
    {
        var definition = new ClubRoleDefinition { Id = 3, ClubId = 1, Name = "Sayman", ClubRole = ClubRole.Member, DisplayOrder = 3 };
        var holders = new List<ClubMembership>
        {
            new() { Id = 11, ClubId = 1, StudentId = 21, AcademicTermId = 1, ClubRole = ClubRole.Member, ClubRoleDefinitionId = 3, JoinedAtUtc = FixedNow },
            new() { Id = 12, ClubId = 1, StudentId = 22, AcademicTermId = 1, ClubRole = ClubRole.Member, ClubRoleDefinitionId = 3, JoinedAtUtc = FixedNow },
        };

        // Ad çakışması sorgusu da aynı kurulumu görüyor; yalnızca Id eşleşmesi olan çağrı tanımı bulmalı.
        _clubRoleDefinitionRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubRoleDefinition, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ClubRoleDefinition, bool>> filter, CancellationToken _) =>
                new[] { definition }.AsQueryable().Where(filter).FirstOrDefault());
        _clubMembershipRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(holders);

        GrantAdminScope();

        var result = await _sut.UpdateRoleDefinitionAsync(1, 3, new UpdateClubRoleDefinitionRequestDto
        {
            Name = "Sayman", ClubRole = ClubRole.Officer, DisplayOrder = 3,
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(ClubRole.Officer, definition.ClubRole);
        Assert.All(holders, m => Assert.Equal(ClubRole.Officer, m.ClubRole));
    }

    [Fact(DisplayName = "O-27/A-39: iki üyenin taşıdığı unvan Başkan seviyesine yükseltilemez — 409")]
    public async Task UpdateRoleDefinitionAsync_WouldCreateSecondPresident_ReturnsConflict()
    {
        var definition = new ClubRoleDefinition { Id = 3, ClubId = 1, Name = "Sayman", ClubRole = ClubRole.Officer, DisplayOrder = 3 };
        var holders = new List<ClubMembership>
        {
            new() { Id = 11, ClubId = 1, StudentId = 21, AcademicTermId = 1, ClubRole = ClubRole.Officer, ClubRoleDefinitionId = 3, JoinedAtUtc = FixedNow },
            new() { Id = 12, ClubId = 1, StudentId = 22, AcademicTermId = 1, ClubRole = ClubRole.Officer, ClubRoleDefinitionId = 3, JoinedAtUtc = FixedNow },
        };

        _clubRoleDefinitionRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubRoleDefinition, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ClubRoleDefinition, bool>> filter, CancellationToken _) =>
                new[] { definition }.AsQueryable().Where(filter).FirstOrDefault());
        _clubMembershipRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(holders);

        GrantAdminScope();

        var result = await _sut.UpdateRoleDefinitionAsync(1, 3, new UpdateClubRoleDefinitionRequestDto
        {
            Name = "Sayman", ClubRole = ClubRole.President, DisplayOrder = 3,
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal(ClubRole.Officer, definition.ClubRole);
    }

    [Fact(DisplayName = "A-61/A-68: unvan atanınca kapasite TANIMDAN kopyalanır")]
    public async Task SetRoleAsync_WithDefinition_CopiesCapabilitiesFromDefinition()
    {
        var membership = new ClubMembership
        {
            Id = 5, ClubId = 1, StudentId = 9, AcademicTermId = 1,
            ClubRole = ClubRole.Member, Capabilities = ClubCapability.None, JoinedAtUtc = FixedNow,
        };
        _clubMembershipRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
        _clubRoleDefinitionRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubRoleDefinition, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubRoleDefinition
            {
                Id = 3, ClubId = 1, Name = "Sayman", ClubRole = ClubRole.Member,
                Capabilities = ClubCapability.EventsManage | ClubCapability.ReportsView, DisplayOrder = 3,
            });

        GrantAdminScope();

        var result = await _sut.SetRoleAsync(1, 5, new SetClubRoleRequestDto { ClubRoleDefinitionId = 3 });

        Assert.True(result.IsSuccess);
        Assert.Equal(ClubCapability.EventsManage | ClubCapability.ReportsView, membership.Capabilities);
    }

    [Fact(DisplayName = "A-68: unvansız atamada kapasite makamdan türetilir — eski davranış")]
    public async Task SetRoleAsync_WithoutDefinition_DerivesCapabilitiesFromRole()
    {
        var membership = new ClubMembership
        {
            Id = 5, ClubId = 1, StudentId = 9, AcademicTermId = 1,
            ClubRole = ClubRole.Member, Capabilities = ClubCapability.None, JoinedAtUtc = FixedNow,
        };
        _clubMembershipRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);

        GrantAdminScope();

        var result = await _sut.SetRoleAsync(1, 5, new SetClubRoleRequestDto { ClubRole = ClubRole.Officer });

        Assert.True(result.IsSuccess);
        Assert.Equal(ClubCapabilityDefaults.ForRole(ClubRole.Officer), membership.Capabilities);
        Assert.Null(membership.ClubRoleDefinitionId);
    }

    [Fact(DisplayName = "O-27: tanımın KAPASİTESİ değişince taşıyan tüm üyeliklere yayılır")]
    public async Task UpdateRoleDefinitionAsync_CapabilityChange_CascadesToMemberships()
    {
        var definition = new ClubRoleDefinition
        {
            Id = 3, ClubId = 1, Name = "Sayman", ClubRole = ClubRole.Member,
            Capabilities = ClubCapability.MembersView, DisplayOrder = 3,
        };
        var holders = new List<ClubMembership>
        {
            new() { Id = 11, ClubId = 1, StudentId = 21, AcademicTermId = 1, ClubRole = ClubRole.Member, Capabilities = ClubCapability.MembersView, ClubRoleDefinitionId = 3, JoinedAtUtc = FixedNow },
            new() { Id = 12, ClubId = 1, StudentId = 22, AcademicTermId = 1, ClubRole = ClubRole.Member, Capabilities = ClubCapability.MembersView, ClubRoleDefinitionId = 3, JoinedAtUtc = FixedNow },
        };

        _clubRoleDefinitionRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubRoleDefinition, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ClubRoleDefinition, bool>> filter, CancellationToken _) =>
                new[] { definition }.AsQueryable().Where(filter).FirstOrDefault());
        _clubMembershipRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(holders);

        GrantAdminScope();

        // Makam değişmiyor, YALNIZCA kapasite — eski kod bu durumda hiç yayılım yapmazdı.
        var result = await _sut.UpdateRoleDefinitionAsync(1, 3, new UpdateClubRoleDefinitionRequestDto
        {
            Name = "Sayman",
            ClubRole = ClubRole.Member,
            Capabilities = ClubCapability.MembersView | ClubCapability.EventsManage,
            DisplayOrder = 3,
        });

        Assert.True(result.IsSuccess);
        Assert.All(holders, m => Assert.True(m.Capabilities.HasFlag(ClubCapability.EventsManage)));
    }

    [Fact(DisplayName = "A-68: kapasitesi MembersView olan üye listeyi görür — makamı hâlâ Member olsa bile")]
    public async Task GetMembersPagedAsync_MemberWithMembersViewCapability_ReturnsSuccess()
    {
        _currentUser.Setup(c => c.UserId).Returns(200);
        _academicStaffRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 999, Title = "Dr.", DepartmentId = 1 });
        _studentRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = 9, ApplicationUserId = 200, StudentNumber = "S1", DepartmentId = 1, EnrollmentYear = 2026 });

        // Makam Member, ama kapasite açık — matrisin merdivenden ayrıldığı yer burası.
        _clubMembershipRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubMembership
            {
                Id = 5, ClubId = 1, StudentId = 9, AcademicTermId = 1,
                ClubRole = ClubRole.Member,
                Capabilities = ClubCapability.MembersView,
                JoinedAtUtc = FixedNow,
            });
        _clubMembershipRepository
            .Setup(r => r.GetListPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ClubMembership>([], 0, 0, 20));

        var result = await _sut.GetMembersPagedAsync(1, 0, 20);

        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "A-68: kapasitesi kısılmış BAŞKAN üye listesini göremez — makam yetki vermez")]
    public async Task GetMembersPagedAsync_PresidentWithoutMembersView_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(200);
        _academicStaffRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 999, Title = "Dr.", DepartmentId = 1 });
        _studentRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = 9, ApplicationUserId = 200, StudentNumber = "S1", DepartmentId = 1, EnrollmentYear = 2026 });
        _clubMembershipRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubMembership
            {
                Id = 5, ClubId = 1, StudentId = 9, AcademicTermId = 1,
                ClubRole = ClubRole.President,
                Capabilities = ClubCapability.EventsManage,
                JoinedAtUtc = FixedNow,
            });

        var result = await _sut.GetMembersPagedAsync(1, 0, 20);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    /// <summary>Y-66: yönetici yolu — kapsam metotlarının ilk satırı. Bu testler kapsamı değil atamayı sınıyor.</summary>
    private void GrantAdminScope() =>
        _currentUser.Setup(c => c.Permissions).Returns([IdentitySeedData.Permissions.ClubsManageAll]);
}
