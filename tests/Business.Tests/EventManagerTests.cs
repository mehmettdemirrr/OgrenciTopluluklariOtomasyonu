using System.Linq.Expressions;
using Business.Concrete;
using Business.DTOs.Events;
using Core.DataAccess;
using Core.Utilities.Security;
using Core.Utilities.Time;
using Entities;
using Entities.Enums;
using Hangfire;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>docs/MIMARI.md · A-20/Y-23: her erişim kuralı için bir kabul + bir ret.</summary>
public class EventManagerTests
{
    private static readonly DateTime FixedNow = new(2026, 8, 21, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IEntityRepository<Event>> _eventRepository = new();
    private readonly Mock<IEntityRepository<Club>> _clubRepository = new();
    private readonly Mock<IEntityRepository<AcademicStaff>> _academicStaffRepository = new();
    private readonly Mock<IEntityRepository<Student>> _studentRepository = new();
    private readonly Mock<IEntityRepository<EventParticipation>> _participationRepository = new();
    private readonly Mock<IEntityRepository<ClubMembership>> _clubMembershipRepository = new();
    private readonly Mock<IEntityRepository<AcademicTerm>> _academicTermRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IBackgroundJobClient> _backgroundJobClient = new();
    private readonly EventManager _sut;

    private readonly Club _club = new() { Id = 1, Name = "Satranç Kulübü", AdvisorId = 10, IsActive = true, CreatedAtUtc = FixedNow };
    private readonly AcademicTerm _term = new() { Id = 1, Name = "2026-Güz", StartDateUtc = FixedNow.AddMonths(-1), EndDateUtc = FixedNow.AddMonths(3), IsCurrent = true };

    public EventManagerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(FixedNow);

        // Y-66: kapsam metotları artık ilk satırda currentUser.Permissions okuyor — varsayılan boş.
        // Yönetici yolunu sınayan test bunu kendi içinde ezer.
        _currentUser.Setup(c => c.Permissions).Returns([]);
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(_club);
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(_term);

        var transaction = new Mock<ITransaction>();
        transaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transaction.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transaction.Object);

        _sut = new EventManager(
            _eventRepository.Object,
            _clubRepository.Object,
            _academicStaffRepository.Object,
            _studentRepository.Object,
            _participationRepository.Object,
            _clubMembershipRepository.Object,
            _academicTermRepository.Object,
            _unitOfWork.Object,
            _currentUser.Object,
            _clock.Object,
            _backgroundJobClient.Object);
    }

    [Fact(DisplayName = "Create: kulübün danışmanı etkinlik oluşturabilir")]
    public async Task CreateAsync_ClubAdvisor_ReturnsSuccess()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });

        var result = await _sut.CreateAsync(1, ValidCreateRequest());

        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "Create: güncel dönemde Officer üyesi etkinlik oluşturabilir")]
    public async Task CreateAsync_CurrentTermOfficer_ReturnsSuccess()
    {
        _currentUser.Setup(c => c.UserId).Returns(200);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcademicStaff?)null);
        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = 5, ApplicationUserId = 200, StudentNumber = "S1", DepartmentId = 1, EnrollmentYear = 2026 });
        _clubMembershipRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubMembership { Id = 1, ClubId = 1, StudentId = 5, AcademicTermId = 1, ClubRole = ClubRole.Officer, JoinedAtUtc = FixedNow });

        var result = await _sut.CreateAsync(1, ValidCreateRequest());

        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "Create: başka kulübün yetkilisi etkinlik oluşturamaz")]
    public async Task CreateAsync_OfficerOfAnotherClub_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(200);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcademicStaff?)null);
        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = 5, ApplicationUserId = 200, StudentNumber = "S1", DepartmentId = 1, EnrollmentYear = 2026 });
        // Bu kulüpte (ClubId=1) üyeliği yok — başka bir kulüpte Officer olabilir ama sorgu zaten ClubId=1'e scoped.
        _clubMembershipRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClubMembership?)null);

        var result = await _sut.CreateAsync(1, ValidCreateRequest());

        Assert.False(result.IsSuccess);
        _eventRepository.Verify(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Create: düz Member etkinlik oluşturamaz")]
    public async Task CreateAsync_PlainMember_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(200);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcademicStaff?)null);
        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = 5, ApplicationUserId = 200, StudentNumber = "S1", DepartmentId = 1, EnrollmentYear = 2026 });
        _clubMembershipRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubMembership { Id = 1, ClubId = 1, StudentId = 5, AcademicTermId = 1, ClubRole = ClubRole.Member, JoinedAtUtc = FixedNow });

        var result = await _sut.CreateAsync(1, ValidCreateRequest());

        Assert.False(result.IsSuccess);
    }

    [Fact(DisplayName = "Decide: events.approve taşıyan ama danışman olmayan kişi karar veremez")]
    public async Task DecideAsync_ApproverNotAdvisor_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(999);
        var pendingEvent = new Event
        {
            Id = 1, ClubId = 1, Title = "Etkinlik", StartDateUtc = FixedNow, EndDateUtc = FixedNow.AddHours(2), Status = EventStatus.PendingApproval, CreatedAtUtc = FixedNow,
        };
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(pendingEvent);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });

        var result = await _sut.DecideAsync(1, new DecideEventRequestDto { Status = EventStatus.Published });

        Assert.False(result.IsSuccess);
        _eventRepository.Verify(r => r.Update(It.IsAny<Event>()), Times.Never);
    }

    [Fact(DisplayName = "Decide: danışman PendingApproval etkinliği onaylayabilir")]
    public async Task DecideAsync_Advisor_ReturnsSuccess()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        var pendingEvent = new Event
        {
            Id = 1, ClubId = 1, Title = "Etkinlik", StartDateUtc = FixedNow, EndDateUtc = FixedNow.AddHours(2), Status = EventStatus.PendingApproval, CreatedAtUtc = FixedNow,
        };
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(pendingEvent);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });

        var result = await _sut.DecideAsync(1, new DecideEventRequestDto { Status = EventStatus.Published });

        Assert.True(result.IsSuccess);
        Assert.Equal(EventStatus.Published, pendingEvent.Status);
    }

    [Fact(DisplayName = "Decide: Draft durumundaki etkinlik onaylanamaz (Conflict)")]
    public async Task DecideAsync_EventNotPending_ReturnsConflict()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        var draftEvent = new Event
        {
            Id = 1, ClubId = 1, Title = "Etkinlik", StartDateUtc = FixedNow, EndDateUtc = FixedNow.AddHours(2), Status = EventStatus.Draft, CreatedAtUtc = FixedNow,
        };
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(draftEvent);

        var result = await _sut.DecideAsync(1, new DecideEventRequestDto { Status = EventStatus.Published });

        Assert.False(result.IsSuccess);
    }

    [Fact(DisplayName = "SubmitForApproval: yalnızca Draft durumundaki etkinlik gönderilebilir")]
    public async Task SubmitForApprovalAsync_AlreadyPending_ReturnsConflict()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        var pendingEvent = new Event
        {
            Id = 1, ClubId = 1, Title = "Etkinlik", StartDateUtc = FixedNow, EndDateUtc = FixedNow.AddHours(2), Status = EventStatus.PendingApproval, CreatedAtUtc = FixedNow,
        };
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(pendingEvent);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });

        var result = await _sut.SubmitForApprovalAsync(1);

        Assert.False(result.IsSuccess);
    }

    [Fact(DisplayName = "Decide: onay/ret sonrası bildirim işi kuyruğa eklenir")]
    public async Task DecideAsync_Advisor_EnqueuesNotificationJob()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        var pendingEvent = new Event
        {
            Id = 1, ClubId = 1, Title = "Etkinlik", StartDateUtc = FixedNow, EndDateUtc = FixedNow.AddHours(2), Status = EventStatus.PendingApproval, CreatedAtUtc = FixedNow,
        };
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(pendingEvent);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });

        var result = await _sut.DecideAsync(1, new DecideEventRequestDto { Status = EventStatus.Published });

        Assert.True(result.IsSuccess);
        _backgroundJobClient.Verify(c => c.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<Hangfire.States.IState>()), Times.Once);
    }

    [Fact(DisplayName = "Update: Draft durumundaki etkinlik danışman tarafından düzenlenebilir")]
    public async Task UpdateAsync_DraftEvent_ClubAdvisor_ReturnsSuccess()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        var draftEvent = new Event
        {
            Id = 1, ClubId = 1, Title = "Eski Başlık", StartDateUtc = FixedNow.AddDays(5), EndDateUtc = FixedNow.AddDays(5).AddHours(2), Status = EventStatus.Draft, CreatedAtUtc = FixedNow,
        };
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(draftEvent);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });

        var result = await _sut.UpdateAsync(1, new UpdateEventRequestDto
        {
            Title = "Yeni Başlık", StartDateUtc = FixedNow.AddDays(6), EndDateUtc = FixedNow.AddDays(6).AddHours(2),
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Yeni Başlık", draftEvent.Title);
    }

    [Fact(DisplayName = "Update: PendingApproval durumundaki etkinlik düzenlenemez (Conflict)")]
    public async Task UpdateAsync_PendingApprovalEvent_ReturnsConflict()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        var pendingEvent = new Event
        {
            Id = 1, ClubId = 1, Title = "Etkinlik", StartDateUtc = FixedNow.AddDays(5), EndDateUtc = FixedNow.AddDays(5).AddHours(2), Status = EventStatus.PendingApproval, CreatedAtUtc = FixedNow,
        };
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(pendingEvent);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });

        var result = await _sut.UpdateAsync(1, new UpdateEventRequestDto
        {
            Title = "Yeni Başlık", StartDateUtc = FixedNow.AddDays(6), EndDateUtc = FixedNow.AddDays(6).AddHours(2),
        });

        Assert.False(result.IsSuccess);
    }

    [Fact(DisplayName = "Delete: Draft durumundaki etkinlik silinebilir (soft delete)")]
    public async Task DeleteAsync_DraftEvent_ReturnsSuccess()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        var draftEvent = new Event
        {
            Id = 1, ClubId = 1, Title = "Etkinlik", StartDateUtc = FixedNow.AddDays(5), EndDateUtc = FixedNow.AddDays(5).AddHours(2), Status = EventStatus.Draft, CreatedAtUtc = FixedNow,
        };
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(draftEvent);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });

        var result = await _sut.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        Assert.True(draftEvent.IsDeleted);
    }

    [Fact(DisplayName = "Delete: PendingApproval durumundaki etkinlik silinemez (Conflict)")]
    public async Task DeleteAsync_PendingApprovalEvent_ReturnsConflict()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        var pendingEvent = new Event
        {
            Id = 1, ClubId = 1, Title = "Etkinlik", StartDateUtc = FixedNow.AddDays(5), EndDateUtc = FixedNow.AddDays(5).AddHours(2), Status = EventStatus.PendingApproval, CreatedAtUtc = FixedNow,
        };
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(pendingEvent);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });

        var result = await _sut.DeleteAsync(1);

        Assert.False(result.IsSuccess);
    }

    [Fact(DisplayName = "GetForClub: kulübün danışmanı taslak dahil tüm etkinlikleri görebilir")]
    public async Task GetForClubAsync_ClubAdvisor_ReturnsAllStatuses()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });
        _eventRepository
            .Setup(r => r.GetListPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Event>([new Event { Id = 1, ClubId = 1, Title = "Taslak", StartDateUtc = FixedNow, EndDateUtc = FixedNow.AddHours(1), Status = EventStatus.Draft, CreatedAtUtc = FixedNow }], 1, 0, 20));
        _clubRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([_club]);

        var result = await _sut.GetForClubAsync(1, 0, 20);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data.Items);
    }

    [Fact(DisplayName = "GetForClub: ilgisiz kullanıcı kulübün etkinlik listesini göremez")]
    public async Task GetForClubAsync_UnrelatedUser_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(777);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((AcademicStaff?)null);
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((AcademicTerm?)null);

        var result = await _sut.GetForClubAsync(1, 0, 20);

        Assert.False(result.IsSuccess);
    }

    private static CreateEventRequestDto ValidCreateRequest() => new()
    {
        Title = "Yılsonu Etkinliği", StartDateUtc = FixedNow.AddDays(10), EndDateUtc = FixedNow.AddDays(10).AddHours(3),
    };
}
