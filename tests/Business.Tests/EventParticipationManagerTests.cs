using System.Linq.Expressions;
using Business.Concrete;
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

/// <summary>docs/PLAN-V2.md · A-20/A-38: her erişim/kontenjan kuralı için bir kabul + bir ret.</summary>
public class EventParticipationManagerTests
{
    private static readonly DateTime FixedNow = new(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IEntityRepository<Event>> _eventRepository = new();
    private readonly Mock<IEntityRepository<EventParticipation>> _participationRepository = new();
    private readonly Mock<IEntityRepository<Club>> _clubRepository = new();
    private readonly Mock<IEntityRepository<Student>> _studentRepository = new();
    private readonly Mock<IEntityRepository<AcademicStaff>> _academicStaffRepository = new();
    private readonly Mock<IEntityRepository<ClubMembership>> _clubMembershipRepository = new();
    private readonly Mock<IEntityRepository<AcademicTerm>> _academicTermRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();
    private readonly EventParticipationManager _sut;

    private readonly Club _club = new() { Id = 1, Name = "Satranç Kulübü", AdvisorId = 10, IsActive = true, CreatedAtUtc = FixedNow };
    private readonly Student _student = new() { Id = 5, ApplicationUserId = 100, StudentNumber = "S1", DepartmentId = 1, EnrollmentYear = 2026 };

    public EventParticipationManagerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(FixedNow);
        _currentUser.Setup(c => c.UserId).Returns(100);
        _currentUser.Setup(c => c.Permissions).Returns([]);
        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(_student);
        _studentRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(_club);

        _sut = new EventParticipationManager(
            _eventRepository.Object,
            _participationRepository.Object,
            _clubRepository.Object,
            _studentRepository.Object,
            _academicStaffRepository.Object,
            _clubMembershipRepository.Object,
            _academicTermRepository.Object,
            _unitOfWork.Object,
            _currentUser.Object,
            _clock.Object);
    }

    private static Event PublishedEvent(int? capacity = null) => new()
    {
        Id = 1, ClubId = 1, Title = "Etkinlik", StartDateUtc = FixedNow.AddDays(1), EndDateUtc = FixedNow.AddDays(1).AddHours(2),
        Status = EventStatus.Published, Capacity = capacity, CreatedAtUtc = FixedNow,
    };

    [Fact(DisplayName = "Register: yayında ve kontenjanı olan etkinliğe kayıt olunabilir")]
    public async Task RegisterAsync_OpenEvent_ReturnsSuccess()
    {
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(PublishedEvent(capacity: 10));
        _participationRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<EventParticipation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((EventParticipation?)null);
        _participationRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<EventParticipation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await _sut.RegisterAsync(1);

        Assert.True(result.IsSuccess);
        _participationRepository.Verify(r => r.AddAsync(It.IsAny<EventParticipation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Register: kontenjan doluysa kayıt reddedilir")]
    public async Task RegisterAsync_CapacityFull_ReturnsConflict()
    {
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(PublishedEvent(capacity: 1));
        _participationRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<EventParticipation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((EventParticipation?)null);
        _participationRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<EventParticipation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new EventParticipation { Id = 1, EventId = 1, StudentId = 99, RegisteredAtUtc = FixedNow }]);

        var result = await _sut.RegisterAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(Core.Utilities.Results.ResultStatus.Conflict, result.Status);
        _participationRepository.Verify(r => r.AddAsync(It.IsAny<EventParticipation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Register: zaten kayıtlı öğrenci ikinci kez kaydolamaz")]
    public async Task RegisterAsync_AlreadyRegistered_ReturnsConflict()
    {
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(PublishedEvent());
        _participationRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<EventParticipation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventParticipation { Id = 1, EventId = 1, StudentId = 5, RegisteredAtUtc = FixedNow });

        var result = await _sut.RegisterAsync(1);

        Assert.False(result.IsSuccess);
        _participationRepository.Verify(r => r.AddAsync(It.IsAny<EventParticipation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Register: taslak durumundaki etkinliğe kayıt olunamaz")]
    public async Task RegisterAsync_DraftEvent_ReturnsConflict()
    {
        var draftEvent = PublishedEvent();
        draftEvent.Status = EventStatus.Draft;
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(draftEvent);

        var result = await _sut.RegisterAsync(1);

        Assert.False(result.IsSuccess);
    }

    [Fact(DisplayName = "Cancel: kayıtlı öğrenci kaydını iptal edebilir")]
    public async Task CancelAsync_Registered_ReturnsSuccess()
    {
        var participation = new EventParticipation { Id = 1, EventId = 1, StudentId = 5, RegisteredAtUtc = FixedNow };
        _participationRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<EventParticipation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(participation);

        var result = await _sut.CancelAsync(1);

        Assert.True(result.IsSuccess);
        Assert.True(participation.IsDeleted);
    }

    [Fact(DisplayName = "Cancel: kaydı olmayan öğrenci iptal edemez")]
    public async Task CancelAsync_NotRegistered_ReturnsNotFound()
    {
        _participationRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<EventParticipation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((EventParticipation?)null);

        var result = await _sut.CancelAsync(1);

        Assert.False(result.IsSuccess);
    }

    [Fact(DisplayName = "GetParticipants: kulübün danışmanı katılımcı listesini görebilir")]
    public async Task GetParticipantsAsync_ClubAdvisor_ReturnsSuccess()
    {
        _currentUser.Setup(c => c.UserId).Returns(999);
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(PublishedEvent());
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 999, Title = "Dr.", DepartmentId = 1 });
        _participationRepository
            .Setup(r => r.GetListPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<EventParticipation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<EventParticipation>([], 0, 0, 20));

        var result = await _sut.GetParticipantsAsync(1, 0, 20);

        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "GetParticipants: ilgisiz kullanıcı katılımcı listesini göremez")]
    public async Task GetParticipantsAsync_UnrelatedUser_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(777);
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(PublishedEvent());
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((AcademicStaff?)null);
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((AcademicTerm?)null);

        var result = await _sut.GetParticipantsAsync(1, 0, 20);

        Assert.False(result.IsSuccess);
    }

    private static Event MembersOnlyEvent() => new()
    {
        Id = 1, ClubId = 1, Title = "Üyelere Özel Atölye",
        StartDateUtc = FixedNow.AddDays(1), EndDateUtc = FixedNow.AddDays(1).AddHours(2),
        Status = EventStatus.Published, Audience = EventAudience.ClubMembers, CreatedAtUtc = FixedNow,
    };

    private static AcademicTerm CurrentTerm() => new()
    {
        Id = 3, Name = "2026-2027 Güz",
        StartDateUtc = FixedNow.AddMonths(-1), EndDateUtc = FixedNow.AddMonths(4), IsCurrent = true,
    };

    [Fact(DisplayName = "Y-72: üye olmayan öğrenci üyelere özel etkinliğe kaydolamaz")]
    public async Task RegisterAsync_MembersOnlyEvent_NonMember_ReturnsForbidden()
    {
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(MembersOnlyEvent());
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(CurrentTerm());
        _clubMembershipRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((ClubMembership?)null);

        var result = await _sut.RegisterAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _participationRepository.Verify(r => r.AddAsync(It.IsAny<EventParticipation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Y-72: güncel dönem üyesi üyelere özel etkinliğe kaydolabilir")]
    public async Task RegisterAsync_MembersOnlyEvent_CurrentTermMember_ReturnsSuccess()
    {
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(MembersOnlyEvent());
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(CurrentTerm());
        _clubMembershipRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubMembership { Id = 9, ClubId = 1, StudentId = 5, AcademicTermId = 3, ClubRole = ClubRole.Member, JoinedAtUtc = FixedNow });
        _participationRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<EventParticipation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((EventParticipation?)null);
        _participationRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<EventParticipation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await _sut.RegisterAsync(1);

        Assert.True(result.IsSuccess);
        _participationRepository.Verify(r => r.AddAsync(It.IsAny<EventParticipation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Y-72: güncel dönem yoksa üyelere özel etkinliğe kaydolunamaz (fail-closed)")]
    public async Task RegisterAsync_MembersOnlyEvent_NoCurrentTerm_ReturnsForbidden()
    {
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(MembersOnlyEvent());
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((AcademicTerm?)null);

        var result = await _sut.RegisterAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _participationRepository.Verify(r => r.AddAsync(It.IsAny<EventParticipation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Y-72: geçen dönemin üyesi üyelere özel etkinliğe kaydolamaz (dönem filtresi gerçekten uygulanır)")]
    public async Task RegisterAsync_MembersOnlyEvent_PreviousTermMemberOnly_ReturnsForbidden()
    {
        var currentTerm = CurrentTerm();
        // Geçen dönemin üyeliği: AcademicTermId = 2, güncel dönem 3.
        var previousTermMembership = new ClubMembership
        {
            Id = 8, ClubId = 1, StudentId = 5, AcademicTermId = 2, ClubRole = ClubRole.Member, JoinedAtUtc = FixedNow.AddMonths(-8),
        };

        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(MembersOnlyEvent());
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(currentTerm);

        // Predicate GERÇEKTEN çalıştırılır (PublicContentManagerTests'in SetupPagedFilter deseni) —
        // Moq salt-geçiş olsaydı bu test dönem filtresinin varlığını kanıtlamazdı.
        _clubMembershipRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ClubMembership, bool>> filter, CancellationToken _) =>
                new[] { previousTermMembership }.AsQueryable().Where(filter).FirstOrDefault());

        var result = await _sut.RegisterAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _participationRepository.Verify(r => r.AddAsync(It.IsAny<EventParticipation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "K-38: herkese açık etkinlikte üyelik hiç sorgulanmaz (mevcut davranış bozulmadı)")]
    public async Task RegisterAsync_PublicEvent_DoesNotQueryMembership()
    {
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(PublishedEvent(capacity: 10));
        _participationRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<EventParticipation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((EventParticipation?)null);
        _participationRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<EventParticipation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await _sut.RegisterAsync(1);

        Assert.True(result.IsSuccess);
        _clubMembershipRepository.Verify(
            r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
