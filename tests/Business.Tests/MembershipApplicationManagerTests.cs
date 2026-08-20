using System.Linq.Expressions;
using Business.Concrete;
using Business.DTOs.Memberships;
using Core.DataAccess;
using Core.Utilities.Results;
using Core.Utilities.Security;
using Core.Utilities.Time;
using Entities;
using Entities.Enums;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>docs/MIMARI.md · A-20: her iş kuralı için bir kabul + bir ret.</summary>
public class MembershipApplicationManagerTests
{
    private static readonly DateTime FixedNow = new(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IEntityRepository<MembershipApplication>> _applicationRepository = new();
    private readonly Mock<IEntityRepository<ClubMembership>> _membershipRepository = new();
    private readonly Mock<IEntityRepository<Club>> _clubRepository = new();
    private readonly Mock<IEntityRepository<Student>> _studentRepository = new();
    private readonly Mock<IEntityRepository<AcademicStaff>> _academicStaffRepository = new();
    private readonly Mock<IEntityRepository<AcademicTerm>> _academicTermRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IBackgroundJobClient> _backgroundJobClient = new();
    private readonly MembershipApplicationManager _sut;

    public MembershipApplicationManagerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(FixedNow);

        var transaction = new Mock<ITransaction>();
        transaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transaction.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transaction.Object);

        _sut = new MembershipApplicationManager(
            _applicationRepository.Object,
            _membershipRepository.Object,
            _clubRepository.Object,
            _studentRepository.Object,
            _academicStaffRepository.Object,
            _academicTermRepository.Object,
            _unitOfWork.Object,
            _currentUser.Object,
            _clock.Object,
            _backgroundJobClient.Object);
    }

    private static Student CreateStudent(int id = 1, int userId = 100) => new()
    {
        Id = id, ApplicationUserId = userId, StudentNumber = "20260001", DepartmentId = 1, EnrollmentYear = 2026,
    };

    private static AcademicTerm CreateTerm(int id = 1) => new()
    {
        Id = id, Name = "2026 Güz", StartDateUtc = FixedNow.AddMonths(-1), EndDateUtc = FixedNow.AddMonths(3), IsCurrent = true,
    };

    private static Club CreateClub(int id = 1, int advisorId = 1, bool isActive = true) => new()
    {
        Id = id, Name = "Test Kulübü", AdvisorId = advisorId, IsActive = isActive, CreatedAtUtc = FixedNow,
    };

    [Fact(DisplayName = "Apply: geçerli koşullarda başvuru oluşturulur")]
    public async Task ApplyAsync_ValidConditions_CreatesApplication()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        var student = CreateStudent();
        var term = CreateTerm();
        var club = CreateClub();

        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(term);
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);
        _membershipRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((ClubMembership?)null);
        _applicationRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<MembershipApplication, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((MembershipApplication?)null);

        var result = await _sut.ApplyAsync(new ApplyForMembershipRequestDto { ClubId = club.Id });

        Assert.True(result.IsSuccess);
        _applicationRepository.Verify(r => r.AddAsync(It.IsAny<MembershipApplication>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Apply: aynı üçlüde bekleyen başvuru varsa reddedilir")]
    public async Task ApplyAsync_DuplicatePending_ReturnsConflict()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        var student = CreateStudent();
        var term = CreateTerm();
        var club = CreateClub();

        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(term);
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);
        _membershipRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((ClubMembership?)null);
        _applicationRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<MembershipApplication, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MembershipApplication
            {
                ClubId = club.Id, StudentId = student.Id, AcademicTermId = term.Id,
                Status = ApplicationStatus.Pending, AppliedAtUtc = FixedNow,
            });

        var result = await _sut.ApplyAsync(new ApplyForMembershipRequestDto { ClubId = club.Id });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        _applicationRepository.Verify(r => r.AddAsync(It.IsAny<MembershipApplication>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Review: doğru danışman onaylarsa üyelik oluşturulur ve bildirim kuyruğa eklenir")]
    public async Task ReviewAsync_CorrectAdvisorApproves_CreatesMembershipAndEnqueuesJob()
    {
        _currentUser.Setup(c => c.UserId).Returns(200);
        var application = new MembershipApplication
        {
            Id = 1, ClubId = 1, StudentId = 1, AcademicTermId = 1, Status = ApplicationStatus.Pending, AppliedAtUtc = FixedNow,
        };
        var club = CreateClub(id: 1, advisorId: 10);
        var advisor = new AcademicStaff { Id = 10, ApplicationUserId = 200, Title = "Dr.", DepartmentId = 1 };

        _applicationRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<MembershipApplication, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(application);
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(advisor);

        var result = await _sut.ReviewAsync(1, new ReviewMembershipApplicationRequestDto { Status = ApplicationStatus.Approved });

        Assert.True(result.IsSuccess);
        _membershipRepository.Verify(r => r.AddAsync(It.IsAny<ClubMembership>(), It.IsAny<CancellationToken>()), Times.Once);
        _backgroundJobClient.Verify(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
    }

    [Fact(DisplayName = "Review: danışman olmayan kullanıcının onay denemesi Forbidden döner, bildirim kuyruğa eklenmez")]
    public async Task ReviewAsync_WrongUser_ReturnsForbiddenAndDoesNotEnqueue()
    {
        _currentUser.Setup(c => c.UserId).Returns(999);
        var application = new MembershipApplication
        {
            Id = 1, ClubId = 1, StudentId = 1, AcademicTermId = 1, Status = ApplicationStatus.Pending, AppliedAtUtc = FixedNow,
        };
        var club = CreateClub(id: 1, advisorId: 10);
        var advisor = new AcademicStaff { Id = 10, ApplicationUserId = 200, Title = "Dr.", DepartmentId = 1 };

        _applicationRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<MembershipApplication, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(application);
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(advisor);

        var result = await _sut.ReviewAsync(1, new ReviewMembershipApplicationRequestDto { Status = ApplicationStatus.Approved });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _membershipRepository.Verify(r => r.AddAsync(It.IsAny<ClubMembership>(), It.IsAny<CancellationToken>()), Times.Never);
        _backgroundJobClient.Verify(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
    }

    [Fact(DisplayName = "Review: zaten karar verilmiş başvuru tekrar incelenemez")]
    public async Task ReviewAsync_AlreadyReviewed_ReturnsConflict()
    {
        _currentUser.Setup(c => c.UserId).Returns(200);
        var application = new MembershipApplication
        {
            Id = 1, ClubId = 1, StudentId = 1, AcademicTermId = 1, Status = ApplicationStatus.Approved, AppliedAtUtc = FixedNow,
        };

        _applicationRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<MembershipApplication, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(application);

        var result = await _sut.ReviewAsync(1, new ReviewMembershipApplicationRequestDto { Status = ApplicationStatus.Approved });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
    }
}
