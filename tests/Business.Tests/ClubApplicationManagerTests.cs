using System.Linq.Expressions;
using Business.Concrete;
using Core.DataAccess;
using Core.Utilities.Security;
using Core.Utilities.Time;
using DataAccess.Repositories;
using Entities;
using Entities.Enums;
using Hangfire;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>
/// docs/PLAN-V6.md §Faz 31 · Y-73: pencere kararı TEK metotta hesaplanır. Bu sınıf o metodun
/// beş satırlık karar tablosunu (A-66) doğrular — muhafız ve okuma ucu aynı cevabı vermek zorunda.
/// </summary>
public class ClubApplicationManagerTests
{
    private static readonly DateTime FixedNow = new(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IEntityRepository<ClubApplication>> _clubApplicationRepository = new();
    private readonly Mock<IEntityRepository<Club>> _clubRepository = new();
    private readonly Mock<IEntityRepository<ClubMembership>> _clubMembershipRepository = new();
    private readonly Mock<IEntityRepository<Student>> _studentRepository = new();
    private readonly Mock<IEntityRepository<AcademicStaff>> _academicStaffRepository = new();
    private readonly Mock<IEntityRepository<AcademicTerm>> _academicTermRepository = new();
    private readonly Mock<IAcademicStaffDal> _academicStaffDal = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IBackgroundJobClient> _backgroundJobClient = new();
    private readonly ClubApplicationManager _sut;

    public ClubApplicationManagerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(FixedNow);
        _currentUser.Setup(c => c.UserId).Returns(100);
        _currentUser.Setup(c => c.Permissions).Returns([]);

        _sut = new ClubApplicationManager(
            _clubApplicationRepository.Object,
            _clubRepository.Object,
            _clubMembershipRepository.Object,
            _studentRepository.Object,
            _academicStaffRepository.Object,
            _academicTermRepository.Object,
            _academicStaffDal.Object,
            _unitOfWork.Object,
            _currentUser.Object,
            _clock.Object,
            _backgroundJobClient.Object);
    }

    /// <summary>Güncel dönem; pencere alanları teste göre doldurulur.</summary>
    private void SetupCurrentTerm(
        ClubApplicationWindowOverride windowOverride,
        DateTime? startUtc = null,
        DateTime? endUtc = null)
    {
        var term = new AcademicTerm
        {
            Id = 1,
            Name = "2026-2027 Güz",
            StartDateUtc = FixedNow.AddMonths(-2),
            EndDateUtc = FixedNow.AddMonths(3),
            IsCurrent = true,
            ClubApplicationStartUtc = startUtc,
            ClubApplicationEndUtc = endUtc,
            ClubApplicationOverride = windowOverride,
        };

        _academicTermRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(term);
    }

    [Fact(DisplayName = "A-66 satır 3: FollowSchedule + now aralıkta → pencere AÇIK")]
    public async Task GetWindowAsync_FollowSchedule_NowInRange_IsOpen()
    {
        SetupCurrentTerm(ClubApplicationWindowOverride.FollowSchedule, FixedNow.AddDays(-2), FixedNow.AddDays(2));

        var result = await _sut.GetWindowAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsOpen);
        Assert.Equal("2026-2027 Güz", result.Data.TermName);
    }

    [Fact(DisplayName = "A-66 satır 4: FollowSchedule + now aralıktan ÖNCE → pencere KAPALI")]
    public async Task GetWindowAsync_FollowSchedule_NowBeforeRange_IsClosed()
    {
        SetupCurrentTerm(ClubApplicationWindowOverride.FollowSchedule, FixedNow.AddDays(5), FixedNow.AddDays(10));

        var result = await _sut.GetWindowAsync();

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsOpen);
    }

    [Fact(DisplayName = "A-66 satır 4: FollowSchedule + now aralıktan SONRA → pencere KAPALI")]
    public async Task GetWindowAsync_FollowSchedule_NowAfterRange_IsClosed()
    {
        SetupCurrentTerm(ClubApplicationWindowOverride.FollowSchedule, FixedNow.AddDays(-10), FixedNow.AddDays(-5));

        var result = await _sut.GetWindowAsync();

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsOpen);
    }

    [Fact(DisplayName = "A-66: aralık KAPSAYICI — tam başlangıç anında pencere AÇIK")]
    public async Task GetWindowAsync_FollowSchedule_ExactlyAtStart_IsOpen()
    {
        SetupCurrentTerm(ClubApplicationWindowOverride.FollowSchedule, FixedNow, FixedNow.AddDays(5));

        var result = await _sut.GetWindowAsync();

        Assert.True(result.Data!.IsOpen);
    }

    [Fact(DisplayName = "A-66: aralık KAPSAYICI — tam bitiş anında pencere AÇIK")]
    public async Task GetWindowAsync_FollowSchedule_ExactlyAtEnd_IsOpen()
    {
        SetupCurrentTerm(ClubApplicationWindowOverride.FollowSchedule, FixedNow.AddDays(-5), FixedNow);

        var result = await _sut.GetWindowAsync();

        Assert.True(result.Data!.IsOpen);
    }

    [Fact(DisplayName = "A-66 satır 5: FollowSchedule + tarih tanımsız → pencere KAPALI (fail-closed)")]
    public async Task GetWindowAsync_FollowSchedule_NoDates_IsClosed()
    {
        SetupCurrentTerm(ClubApplicationWindowOverride.FollowSchedule);

        var result = await _sut.GetWindowAsync();

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsOpen);
    }

    [Fact(DisplayName = "A-66 satır 5: FollowSchedule + yalnızca başlangıç tanımlı → pencere KAPALI")]
    public async Task GetWindowAsync_FollowSchedule_OnlyStartDefined_IsClosed()
    {
        SetupCurrentTerm(ClubApplicationWindowOverride.FollowSchedule, FixedNow.AddDays(-2), null);

        var result = await _sut.GetWindowAsync();

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsOpen);
    }

    [Fact(DisplayName = "A-66 satır 1: ForceOpen + tarih aralık dışında bile pencere AÇIK")]
    public async Task GetWindowAsync_ForceOpen_OutOfRange_IsOpen()
    {
        SetupCurrentTerm(ClubApplicationWindowOverride.ForceOpen, FixedNow.AddDays(-10), FixedNow.AddDays(-5));

        var result = await _sut.GetWindowAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsOpen);
    }

    [Fact(DisplayName = "A-66 satır 1: ForceOpen + hiç tarih yokken pencere AÇIK")]
    public async Task GetWindowAsync_ForceOpen_NoDates_IsOpen()
    {
        SetupCurrentTerm(ClubApplicationWindowOverride.ForceOpen);

        var result = await _sut.GetWindowAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsOpen);
    }

    [Fact(DisplayName = "A-66 satır 2: ForceClosed + now aralıkta olsa bile pencere KAPALI")]
    public async Task GetWindowAsync_ForceClosed_NowInRange_IsClosed()
    {
        SetupCurrentTerm(ClubApplicationWindowOverride.ForceClosed, FixedNow.AddDays(-2), FixedNow.AddDays(2));

        var result = await _sut.GetWindowAsync();

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsOpen);
    }

    [Fact(DisplayName = "K-39: güncel dönem yoksa pencere KAPALI döner, çökmez")]
    public async Task GetWindowAsync_NoCurrentTerm_IsClosed()
    {
        _academicTermRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcademicTerm?)null);

        var result = await _sut.GetWindowAsync();

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsOpen);
        Assert.Equal(string.Empty, result.Data.TermName);
    }

    [Fact(DisplayName = "K-39: pencere DTO'su tarihleri ve override'ı olduğu gibi taşır (arayüz metni buradan kurar)")]
    public async Task GetWindowAsync_CarriesDatesAndOverride()
    {
        var start = FixedNow.AddDays(-2);
        var end = FixedNow.AddDays(2);
        SetupCurrentTerm(ClubApplicationWindowOverride.FollowSchedule, start, end);

        var result = await _sut.GetWindowAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(start, result.Data!.StartUtc);
        Assert.Equal(end, result.Data.EndUtc);
        Assert.Equal(ClubApplicationWindowOverride.FollowSchedule, result.Data.Override);
    }
}
