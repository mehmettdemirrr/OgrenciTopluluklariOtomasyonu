using System.Linq.Expressions;
using Business.Concrete;
using Business.DTOs.Clubs;
using Core.DataAccess;
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
        var membership = new ClubMembership { Id = 1, ClubId = 1, StudentId = 5, AcademicTermId = 1, ClubRole = ClubRole.President, JoinedAtUtc = FixedNow };
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

    [Fact(DisplayName = "GetMembersPagedAsync: reports.read.all taşıyan yönetici herhangi bir kulübün üyelerini görebilir")]
    public async Task GetMembersPagedAsync_ReportsReadAllHolder_ReturnsSuccess()
    {
        _currentUser.Setup(c => c.Permissions).Returns([IdentitySeedData.Permissions.ReportsReadAll]);
        _clubMembershipRepository
            .Setup(r => r.GetListPagedAsync(0, 20, It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ClubMembership>([], 0, 0, 20));

        var result = await _sut.GetMembersPagedAsync(1, 0, 20);

        Assert.True(result.IsSuccess);
        // Blanket bypass — danışman/öğrenci sorgularına hiç gidilmemeli.
        _academicStaffRepository.Verify(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
