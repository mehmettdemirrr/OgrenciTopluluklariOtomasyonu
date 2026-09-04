using System.Linq.Expressions;
using Business.Abstract;
using Business.Concrete;
using Business.Constants;
using Business.DTOs.ClubApplications;
using Business.DTOs.Files;
using Core.DataAccess;
using Core.Utilities.Files;
using Core.Utilities.Results;
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
    private readonly Mock<IEntityRepository<ClubCategory>> _clubCategoryRepository = new();
    private readonly Mock<IEntityRepository<AcademicTerm>> _academicTermRepository = new();

    // Faz 34 (K-36): onayla doğan kulüp varsayılan unvan setini alır.
    private readonly Mock<IEntityRepository<ClubRoleDefinition>> _clubRoleDefinitionRepository = new();

    // Faz 33 (K-37): kuruluş evrakları.
    private readonly Mock<IEntityRepository<ClubDocumentType>> _clubDocumentTypeRepository = new();
    private readonly Mock<IEntityRepository<ClubApplicationDocument>> _clubApplicationDocumentRepository = new();
    private readonly Mock<IEntityRepository<StoredFile>> _storedFileRepository = new();
    private readonly Mock<IFileService> _fileService = new();
    private readonly Mock<IFileStorage> _fileStorage = new();

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

        // DecideAsync onay dalı [TransactionAspect] yerine elle transaction yönetir (Y-46/Y-06).
        var transaction = new Mock<ITransaction>();
        transaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transaction.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transaction.Object);

        _sut = new ClubApplicationManager(
            _clubApplicationRepository.Object,
            _clubRepository.Object,
            _clubMembershipRepository.Object,
            _studentRepository.Object,
            _academicStaffRepository.Object,
            _clubCategoryRepository.Object,
            _academicTermRepository.Object,
            _clubRoleDefinitionRepository.Object,
            _clubDocumentTypeRepository.Object,
            _clubApplicationDocumentRepository.Object,
            _storedFileRepository.Object,
            _academicStaffDal.Object,
            _fileService.Object,
            _fileStorage.Object,
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

    /// <summary>Başvuruyu göndermek için gereken tüm muhafızları geçiren minimal, geçerli istek.</summary>
    private static SubmitClubApplicationRequestDto BuildValidSubmitRequest() => new()
    {
        ProposedName = "Robotik Topluluğu",
        Justification = "Kampüste robotik alanında etkinlik ve eğitim düzenlemek.",
        ProposedAdvisorId = 7,
    };

    private void SetupSubmitPrerequisites()
    {
        SetupCurrentTerm(ClubApplicationWindowOverride.ForceOpen);

        _studentRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = 5, ApplicationUserId = 100, StudentNumber = "2210191045" });

        _academicStaffRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 7, ApplicationUserId = 200, Title = "Dr. Öğr. Üyesi", DepartmentId = 1 });

        // Y-71: boş katalog = zorunlu evrak yok — bu testler logoyu sınar, evrakı değil.
        _clubDocumentTypeRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubDocumentType, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    [Fact(DisplayName = "K-40/A-69: SubmitAsync logoyu saklar ve LogoFileId'yi başvuruya yazar")]
    public async Task SubmitAsync_StoresLogo_WhenProvided()
    {
        SetupSubmitPrerequisites();

        ClubApplication? captured = null;
        _clubApplicationRepository
            .Setup(r => r.AddAsync(It.IsAny<ClubApplication>(), It.IsAny<CancellationToken>()))
            .Callback<ClubApplication, CancellationToken>((application, _) => captured = application)
            .Returns(Task.CompletedTask);

        _fileService
            .Setup(f => f.StoreApplicationLogoAsync(It.IsAny<UploadFileRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DataResult<UploadedFileDto>.Success(new UploadedFileDto { FileId = 42, ContentType = "image/png", FileSizeBytes = 10 }));

        var request = BuildValidSubmitRequest();
        request.Logo = new UploadFileRequestDto { Content = new MemoryStream([1, 2, 3]), OriginalFileName = "logo.png", Length = 3 };

        var result = await _sut.SubmitAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Equal(42, captured!.LogoFileId);
        _fileService.Verify(f => f.StoreApplicationLogoAsync(It.IsAny<UploadFileRequestDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "K-40: logo verilmezse SubmitAsync başarılı olur, LogoFileId null kalır")]
    public async Task SubmitAsync_AcceptsMissingLogo()
    {
        SetupSubmitPrerequisites();

        ClubApplication? captured = null;
        _clubApplicationRepository
            .Setup(r => r.AddAsync(It.IsAny<ClubApplication>(), It.IsAny<CancellationToken>()))
            .Callback<ClubApplication, CancellationToken>((application, _) => captured = application)
            .Returns(Task.CompletedTask);

        var result = await _sut.SubmitAsync(BuildValidSubmitRequest());

        Assert.True(result.IsSuccess);
        Assert.Null(captured!.LogoFileId);
        _fileService.Verify(f => f.StoreApplicationLogoAsync(It.IsAny<UploadFileRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "A-64: desteklenmeyen logo dosyası SubmitAsync'i reddeder")]
    public async Task SubmitAsync_Rejects_WhenLogoTypeUnsupported()
    {
        // Yarım kaydın gerçekten geri alınması [TransactionAspect]'in işidir (yorum satırındaki
        // gerekçe budur) ve yalnızca DI proxy'si üzerinden çağrıldığında devreye girer — bu birim
        // testi somut sınıfı doğrudan çağırdığı için aspect'i tetiklemez (dosyadaki diğer tüm
        // testlerle aynı kısıt, ör. evrak tipi hatasında da AddAsync zaten çağrılmış olur).
        // Burada sınanan, Manager'ın hatayı doğru yakalayıp ValidationError döndürdüğüdür.
        SetupSubmitPrerequisites();

        _fileService
            .Setup(f => f.StoreApplicationLogoAsync(It.IsAny<UploadFileRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DataResult<UploadedFileDto>.ValidationError(Messages.UnsupportedFileType));

        var request = BuildValidSubmitRequest();
        request.Logo = new UploadFileRequestDto { Content = new MemoryStream([1, 2, 3]), OriginalFileName = "logo.exe", Length = 3 };

        var result = await _sut.SubmitAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(Messages.UnsupportedFileType, result.Message);
    }

    /// <summary>Onay dalının geçmesi için gereken muhafızları geçiren, logolu, bekleyen bir başvuru.</summary>
    private ClubApplication SeedPendingApplicationWithLogo(int logoFileId)
    {
        var application = new ClubApplication
        {
            Id = 9,
            StudentId = 5,
            AcademicTermId = 1,
            ProposedName = "Robotik Topluluğu",
            Justification = "Gerekçe",
            ProposedAdvisorId = 7,
            Status = ApplicationStatus.Pending,
            AppliedAtUtc = FixedNow.AddDays(-1),
            LogoFileId = logoFileId,
        };

        _clubApplicationRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubApplication, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _academicStaffRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 7, ApplicationUserId = 200, Title = "Dr. Öğr. Üyesi", DepartmentId = 1 });

        return application;
    }

    [Fact(DisplayName = "A-69: DecideAsync onay dalı başvurunun logosunu aynı dosyayı işaret ederek kulübe kopyalar")]
    public async Task DecideAsync_CopiesLogoToClub_WhenApproved()
    {
        SeedPendingApplicationWithLogo(logoFileId: 42);

        Club? capturedClub = null;
        _clubRepository
            .Setup(r => r.AddAsync(It.IsAny<Club>(), It.IsAny<CancellationToken>()))
            .Callback<Club, CancellationToken>((club, _) => capturedClub = club)
            .Returns(Task.CompletedTask);

        var result = await _sut.DecideAsync(9, new DecideClubApplicationRequestDto { Status = ApplicationStatus.Approved });

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedClub);
        Assert.Equal(42, capturedClub!.LogoFileId);
    }

    [Fact(DisplayName = "Y-76: DecideAsync ret dalında hiçbir kulüp oluşmaz, logo hiçbir yere kopyalanmaz")]
    public async Task DecideAsync_DoesNotCreateClub_WhenRejected()
    {
        SeedPendingApplicationWithLogo(logoFileId: 42);

        var result = await _sut.DecideAsync(9, new DecideClubApplicationRequestDto { Status = ApplicationStatus.Rejected, ReviewNote = "Eksik evrak" });

        Assert.True(result.IsSuccess);
        _clubRepository.Verify(r => r.AddAsync(It.IsAny<Club>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
