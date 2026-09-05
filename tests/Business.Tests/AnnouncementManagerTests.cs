using System.Linq.Expressions;
using Business.Concrete;
using Business.DTOs.Announcements;
using Core.DataAccess;
using Core.Utilities.Security;
using Core.Utilities.Time;
using DataAccess.Seed;
using Entities;
using Entities.Enums;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>docs/PLAN-V2.md · A-20/A-43: her kapsam kuralı için bir kabul + bir ret.</summary>
public class AnnouncementManagerTests
{
    private static readonly DateTime FixedNow = new(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IEntityRepository<Announcement>> _announcementRepository = new();
    private readonly Mock<IEntityRepository<Club>> _clubRepository = new();
    private readonly Mock<IEntityRepository<Student>> _studentRepository = new();
    private readonly Mock<IEntityRepository<AcademicStaff>> _academicStaffRepository = new();
    private readonly Mock<IEntityRepository<ClubMembership>> _clubMembershipRepository = new();
    private readonly Mock<IEntityRepository<AcademicTerm>> _academicTermRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();
    private readonly AnnouncementManager _sut;

    private readonly Club _club = new() { Id = 1, Name = "Satranç Kulübü", AdvisorId = 10, IsActive = true, CreatedAtUtc = FixedNow };

    public AnnouncementManagerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(FixedNow);
        _currentUser.Setup(c => c.Permissions).Returns([]);
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(_club);

        _sut = new AnnouncementManager(
            _announcementRepository.Object,
            _clubRepository.Object,
            _studentRepository.Object,
            _academicStaffRepository.Object,
            _clubMembershipRepository.Object,
            _academicTermRepository.Object,
            _unitOfWork.Object,
            _currentUser.Object,
            _clock.Object);
    }

    private static CreateAnnouncementRequestDto ValidRequest() => new()
    {
        Title = "Duyuru", Content = "İçerik", Visibility = AnnouncementVisibility.Members,
    };

    [Fact(DisplayName = "Create: kulübün danışmanı duyuru oluşturabilir")]
    public async Task CreateAsync_ClubAdvisor_ReturnsSuccess()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });

        var result = await _sut.CreateAsync(1, ValidRequest());

        Assert.True(result.IsSuccess);
        _announcementRepository.Verify(r => r.AddAsync(It.IsAny<Announcement>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Create: kulüple ilgisi olmayan kullanıcı duyuru oluşturamaz")]
    public async Task CreateAsync_UnrelatedUser_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(777);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((AcademicStaff?)null);
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((AcademicTerm?)null);

        var result = await _sut.CreateAsync(1, ValidRequest());

        Assert.False(result.IsSuccess);
        _announcementRepository.Verify(r => r.AddAsync(It.IsAny<Announcement>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Update: announcements.global taşıyan Admin sistem duyurusunu düzenleyebilir")]
    public async Task UpdateAsync_SystemAnnouncement_AdminWithGlobalClaim_ReturnsSuccess()
    {
        var announcement = new Announcement { Id = 1, ClubId = null, Title = "Eski", Content = "Eski içerik", Visibility = AnnouncementVisibility.Public, PublishedAtUtc = FixedNow };
        _announcementRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Announcement, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(announcement);
        _currentUser.Setup(c => c.Permissions).Returns([IdentitySeedData.Permissions.AnnouncementsGlobal]);

        var result = await _sut.UpdateAsync(1, new UpdateAnnouncementRequestDto { Title = "Yeni", Content = "Yeni içerik", Visibility = AnnouncementVisibility.Public });

        Assert.True(result.IsSuccess);
        Assert.Equal("Yeni", announcement.Title);
    }

    [Fact(DisplayName = "Update: announcements.global taşımayan kullanıcı sistem duyurusunu düzenleyemez")]
    public async Task UpdateAsync_SystemAnnouncement_WithoutGlobalClaim_ReturnsForbidden()
    {
        var announcement = new Announcement { Id = 1, ClubId = null, Title = "Eski", Content = "Eski içerik", Visibility = AnnouncementVisibility.Public, PublishedAtUtc = FixedNow };
        _announcementRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Announcement, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(announcement);

        var result = await _sut.UpdateAsync(1, new UpdateAnnouncementRequestDto { Title = "Yeni", Content = "Yeni içerik", Visibility = AnnouncementVisibility.Public });

        Assert.False(result.IsSuccess);
    }

    [Fact(DisplayName = "CreateGlobal: sistem duyurusu ClubId=null olarak oluşturulur")]
    public async Task CreateGlobalAsync_CreatesAnnouncementWithoutClub()
    {
        var result = await _sut.CreateGlobalAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        _announcementRepository.Verify(r => r.AddAsync(It.Is<Announcement>(a => a.ClubId == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Delete: kulübün güncel dönemdeki başkanı duyuruyu kaldırabilir")]
    public async Task DeleteAsync_ClubPresident_ReturnsSuccess()
    {
        var announcement = new Announcement { Id = 1, ClubId = 1, Title = "Duyuru", Content = "İçerik", Visibility = AnnouncementVisibility.Members, PublishedAtUtc = FixedNow };
        _announcementRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Announcement, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(announcement);
        _currentUser.Setup(c => c.UserId).Returns(200);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((AcademicStaff?)null);
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicTerm { Id = 1, Name = "2026-Güz", StartDateUtc = FixedNow.AddMonths(-1), EndDateUtc = FixedNow.AddMonths(3), IsCurrent = true });
        _studentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = 5, ApplicationUserId = 200, StudentNumber = "S1", DepartmentId = 1, EnrollmentYear = 2026 });
        _clubMembershipRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubMembership
            {
                Id = 1, ClubId = 1, StudentId = 5, AcademicTermId = 1,
                ClubRole = ClubRole.President,
                Capabilities = ClubCapabilityDefaults.ForRole(ClubRole.President),
                JoinedAtUtc = FixedNow,
            });

        var result = await _sut.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        Assert.True(announcement.IsDeleted);
    }

    private const string ValidContentJson = """
    {"type":"doc","content":[
      {"type":"paragraph","attrs":{"textAlign":"left"},"content":[
        {"type":"text","text":"Kayıtlar ","marks":[{"type":"bold"}]},
        {"type":"text","text":"15 Ekim","marks":[{"type":"textColor","attrs":{"token":"accent"}}]}
      ]}
    ]}
    """;

    [Fact(DisplayName = "Create: K-42/A-71 — ContentJson kaydedilir, Content düz metin aynası olarak türetilir")]
    public async Task CreateAsync_StoresContentJson_AndDerivesPlainText()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });
        Announcement? captured = null;
        _announcementRepository.Setup(r => r.AddAsync(It.IsAny<Announcement>(), It.IsAny<CancellationToken>()))
            .Callback<Announcement, CancellationToken>((a, _) => captured = a)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(1, new CreateAnnouncementRequestDto
        {
            Title = "Kayıtlar açıldı", ContentJson = ValidContentJson, Visibility = AnnouncementVisibility.Public,
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(ValidContentJson, captured!.ContentJson);
        Assert.Equal("Kayıtlar 15 Ekim", captured.Content);
    }

    [Fact(DisplayName = "Create: Y-78 — izin listesinde olmayan düğüm içeren ContentJson reddedilir, kayıt oluşmaz")]
    public async Task CreateAsync_Rejects_WhenContentJsonHasDisallowedNode()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });

        var result = await _sut.CreateAsync(1, new CreateAnnouncementRequestDto
        {
            Title = "Kötü", ContentJson = """{"type":"doc","content":[{"type":"iframe"}]}""", Visibility = AnnouncementVisibility.Public,
        });

        Assert.False(result.IsSuccess);
        _announcementRepository.Verify(r => r.AddAsync(It.IsAny<Announcement>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Create: ContentJson boşken düz metin duyuru hâlâ yazılabilir (geriye dönük)")]
    public async Task CreateAsync_AcceptsPlainContent_WhenContentJsonIsNull()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });
        Announcement? captured = null;
        _announcementRepository.Setup(r => r.AddAsync(It.IsAny<Announcement>(), It.IsAny<CancellationToken>()))
            .Callback<Announcement, CancellationToken>((a, _) => captured = a)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(1, new CreateAnnouncementRequestDto
        {
            Title = "Düz", Content = "Sadece metin", Visibility = AnnouncementVisibility.Public,
        });

        Assert.True(result.IsSuccess);
        Assert.Null(captured!.ContentJson);
        Assert.Equal("Sadece metin", captured.Content);
    }

    [Fact(DisplayName = "CreateGlobal: sistem duyurusu da ContentJson taşıyabilir")]
    public async Task CreateGlobalAsync_StoresContentJson()
    {
        Announcement? captured = null;
        _announcementRepository.Setup(r => r.AddAsync(It.IsAny<Announcement>(), It.IsAny<CancellationToken>()))
            .Callback<Announcement, CancellationToken>((a, _) => captured = a)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateGlobalAsync(new CreateAnnouncementRequestDto
        {
            Title = "Sistem", ContentJson = ValidContentJson, Visibility = AnnouncementVisibility.Public,
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Kayıtlar 15 Ekim", captured!.Content);
    }

    [Fact(DisplayName = "Update: ContentJson güncellenince Content aynası yeniden türetilir")]
    public async Task UpdateAsync_StoresContentJson_AndDerivesPlainText()
    {
        var announcement = new Announcement { Id = 1, ClubId = null, Title = "Eski", Content = "Eski içerik", Visibility = AnnouncementVisibility.Public, PublishedAtUtc = FixedNow };
        _announcementRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Announcement, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(announcement);
        _currentUser.Setup(c => c.Permissions).Returns([IdentitySeedData.Permissions.AnnouncementsGlobal]);

        var result = await _sut.UpdateAsync(1, new UpdateAnnouncementRequestDto
        {
            Title = "Yeni", ContentJson = ValidContentJson, Visibility = AnnouncementVisibility.Public,
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Kayıtlar 15 Ekim", announcement.Content);
        Assert.Equal(ValidContentJson, announcement.ContentJson);
    }
}
