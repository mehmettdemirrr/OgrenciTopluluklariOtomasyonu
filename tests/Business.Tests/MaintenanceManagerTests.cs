using System.Linq.Expressions;
using Business.Concrete;
using Core.DataAccess;
using Core.Utilities.Files;
using Core.Utilities.Time;
using DataAccess.Repositories;
using Entities;
using Entities.Enums;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>docs/MIMARI.md · §7 sessiz onay: gecelik tek iş; K-28/A-44 ile trafik logu temizliği eklendi.</summary>
public class MaintenanceManagerTests
{
    private static readonly DateTime FixedNow = new(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IEntityRepository<RefreshToken>> _refreshTokenRepository = new();
    private readonly Mock<IEntityRepository<StoredFile>> _storedFileRepository = new();
    private readonly Mock<IEntityRepository<ReportRequest>> _reportRequestRepository = new();

    // Faz 33 (K-37/A-63): kuruluş evrakı saklama süresi.
    private readonly Mock<IEntityRepository<ClubApplication>> _clubApplicationRepository = new();
    private readonly Mock<IEntityRepository<ClubApplicationDocument>> _clubApplicationDocumentRepository = new();

    private readonly Mock<ITrafficLogDal> _trafficLogDal = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IFileStorage> _fileStorage = new();
    private readonly MaintenanceManager _sut;

    public MaintenanceManagerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(FixedNow);

        _sut = new MaintenanceManager(
            _refreshTokenRepository.Object,
            _storedFileRepository.Object,
            _reportRequestRepository.Object,
            _clubApplicationRepository.Object,
            _clubApplicationDocumentRepository.Object,
            _trafficLogDal.Object,
            _unitOfWork.Object,
            _clock.Object,
            _fileStorage.Object);

        _storedFileRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<StoredFile, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _refreshTokenRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _reportRequestRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ReportRequest, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _clubApplicationRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubApplication, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _clubApplicationDocumentRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubApplicationDocument, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        // Moq varsayılanı Task<IReadOnlyList<string>> için null döner — sahipsiz dosya taraması NRE alırdı.
        _fileStorage.Setup(s => s.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    [Fact(DisplayName = "RunNightlyMaintenanceAsync: süresi geçmiş refresh token silinir")]
    public async Task RunNightlyMaintenanceAsync_ExpiredToken_IsDeleted()
    {
        var expiredToken = new RefreshToken
        {
            Id = 1, ApplicationUserId = 1, TokenHash = "x", CreatedAtUtc = FixedNow.AddDays(-10), ExpiresAtUtc = FixedNow.AddDays(-1),
        };
        _refreshTokenRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([expiredToken]);

        var result = await _sut.RunNightlyMaintenanceAsync();

        Assert.True(result.IsSuccess);
        _refreshTokenRepository.Verify(r => r.Delete(expiredToken), Times.Once);
    }

    [Fact(DisplayName = "RunNightlyMaintenanceAsync: süresi dolmamış token sorgu filtresinde elenir")]
    public async Task RunNightlyMaintenanceAsync_ValidToken_QueryPredicateExcludesIt()
    {
        Expression<Func<RefreshToken, bool>>? capturedFilter = null;
        _refreshTokenRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<RefreshToken, bool>>, CancellationToken>((f, _) => capturedFilter = f)
            .ReturnsAsync([]);

        await _sut.RunNightlyMaintenanceAsync();

        Assert.NotNull(capturedFilter);
        var validToken = new RefreshToken { Id = 2, ApplicationUserId = 1, TokenHash = "y", CreatedAtUtc = FixedNow, ExpiresAtUtc = FixedNow.AddDays(5) };
        Assert.False(capturedFilter!.Compile()(validToken));
        _refreshTokenRepository.Verify(r => r.Delete(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact(DisplayName = "RunNightlyMaintenanceAsync: 7 günden eski rapor dosyası silinir, ilişkili ReportRequest.OutputFileId null'lanır")]
    public async Task RunNightlyMaintenanceAsync_StaleReportFile_DeletedAndUnlinked()
    {
        var staleFile = new StoredFile
        {
            Id = 50, GeneratedFileName = "old.xlsx", OriginalFileName = "rapor.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileSizeBytes = 10, Visibility = FileVisibility.Protected, UploadedByUserId = 1, UploadedAtUtc = FixedNow.AddDays(-8),
        };
        _storedFileRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<StoredFile, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([staleFile]);

        var relatedReportRequest = new ReportRequest
        {
            Id = 1, RequestedByUserId = 1, ReportType = "ClubMembers", Status = ReportStatus.Ready, OutputFileId = 50, RequestedAtUtc = FixedNow.AddDays(-8),
        };
        _reportRequestRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ReportRequest, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([relatedReportRequest]);
        _fileStorage.Setup(s => s.DeleteAsync("old.xlsx", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _sut.RunNightlyMaintenanceAsync();

        Assert.True(result.IsSuccess);
        Assert.Null(relatedReportRequest.OutputFileId);
        _storedFileRepository.Verify(r => r.Delete(staleFile), Times.Once);
        _fileStorage.Verify(s => s.DeleteAsync("old.xlsx", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "RunNightlyMaintenanceAsync: taze (7 günden yeni) rapor dosyası sorgu filtresinde elenir")]
    public async Task RunNightlyMaintenanceAsync_FreshReportFile_QueryPredicateExcludesIt()
    {
        Expression<Func<StoredFile, bool>>? capturedFilter = null;
        _storedFileRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<StoredFile, bool>>>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<StoredFile, bool>>, CancellationToken>((f, _) => capturedFilter = f)
            .ReturnsAsync([]);

        await _sut.RunNightlyMaintenanceAsync();

        Assert.NotNull(capturedFilter);
        var freshFile = new StoredFile
        {
            Id = 60, GeneratedFileName = "fresh.xlsx", OriginalFileName = "rapor.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileSizeBytes = 10, Visibility = FileVisibility.Protected, UploadedByUserId = 1, UploadedAtUtc = FixedNow.AddDays(-1),
        };
        Assert.False(capturedFilter!.Compile()(freshFile));
    }

    [Fact(DisplayName = "RunNightlyMaintenanceAsync: fiziksel dosya zaten yoksa akış bozulmaz (idempotent)")]
    public async Task RunNightlyMaintenanceAsync_PhysicalFileAlreadyGone_DoesNotBreakFlow()
    {
        var staleFile = new StoredFile
        {
            Id = 50, GeneratedFileName = "gone.xlsx", OriginalFileName = "rapor.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileSizeBytes = 10, Visibility = FileVisibility.Protected, UploadedByUserId = 1, UploadedAtUtc = FixedNow.AddDays(-8),
        };
        _storedFileRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<StoredFile, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([staleFile]);
        _fileStorage.Setup(s => s.DeleteAsync("gone.xlsx", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _sut.RunNightlyMaintenanceAsync();

        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "RunNightlyMaintenanceAsync: A-44 — 30 günden eski trafik logu satırları temizlenir")]
    public async Task RunNightlyMaintenanceAsync_DeletesTrafficLogsOlderThan30Days()
    {
        var result = await _sut.RunNightlyMaintenanceAsync();

        Assert.True(result.IsSuccess);
        _trafficLogDal.Verify(d => d.DeleteOlderThanAsync(FixedNow.AddDays(-30), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "A-63: 90 günü geçmiş REDDEDİLMİŞ başvurunun evrakları silinir")]
    public async Task RunNightlyMaintenanceAsync_OldRejectedApplication_DeletesDocuments()
    {
        var rejected = new ClubApplication
        {
            Id = 1, StudentId = 1, AcademicTermId = 1, ProposedName = "Eski Ret", Justification = "x",
            ProposedAdvisorId = 1, Status = ApplicationStatus.Rejected,
            AppliedAtUtc = FixedNow.AddDays(-200), ReviewedAtUtc = FixedNow.AddDays(-91),
        };
        var link = new ClubApplicationDocument { Id = 10, ClubApplicationId = 1, ClubDocumentTypeId = 1, StoredFileId = 100 };
        var file = new StoredFile
        {
            Id = 100, GeneratedFileName = "abc.pdf", OriginalFileName = "evrak.pdf", ContentType = "application/pdf",
            FileSizeBytes = 10, Visibility = FileVisibility.Protected, UploadedByUserId = 1, UploadedAtUtc = FixedNow.AddDays(-200),
        };

        SetupApplicationCleanupScenario([rejected], [link], [file]);

        var result = await _sut.RunNightlyMaintenanceAsync();

        Assert.True(result.IsSuccess);
        _clubApplicationDocumentRepository.Verify(r => r.Delete(It.Is<ClubApplicationDocument>(d => d.Id == 10)), Times.Once);
        _storedFileRepository.Verify(r => r.Delete(It.Is<StoredFile>(f => f.Id == 100)), Times.Once);
        _fileStorage.Verify(s => s.DeleteAsync("abc.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "A-63/K-37: ONAYLANMIŞ başvurunun evrakları 90 gün sonra da durur — 7 günlük rapor temizliği onlara dokunmaz")]
    public async Task RunNightlyMaintenanceAsync_OldApprovedApplication_KeepsDocuments()
    {
        var approved = new ClubApplication
        {
            Id = 2, StudentId = 1, AcademicTermId = 1, ProposedName = "Eski Onay", Justification = "x",
            ProposedAdvisorId = 1, Status = ApplicationStatus.Approved, CreatedClubId = 5,
            AppliedAtUtc = FixedNow.AddDays(-200), ReviewedAtUtc = FixedNow.AddDays(-150),
        };
        var link = new ClubApplicationDocument { Id = 20, ClubApplicationId = 2, ClubDocumentTypeId = 1, StoredFileId = 200 };
        var file = new StoredFile
        {
            Id = 200, GeneratedFileName = "onay.pdf", OriginalFileName = "evrak.pdf", ContentType = "application/pdf",
            FileSizeBytes = 10, Visibility = FileVisibility.Protected, UploadedByUserId = 1, UploadedAtUtc = FixedNow.AddDays(-200),
        };

        SetupApplicationCleanupScenario([approved], [link], [file]);

        var result = await _sut.RunNightlyMaintenanceAsync();

        Assert.True(result.IsSuccess);
        _clubApplicationDocumentRepository.Verify(r => r.Delete(It.IsAny<ClubApplicationDocument>()), Times.Never);
        // Dosya Protected ve 200 günlük: ayıklama olmasa 7 günlük rapor temizliği bunu silerdi.
        _storedFileRepository.Verify(r => r.Delete(It.IsAny<StoredFile>()), Times.Never);
        _fileStorage.Verify(s => s.DeleteAsync("onay.pdf", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "A-63: 90 günü DOLMAMIŞ reddedilmiş başvurunun evrakları durur")]
    public async Task RunNightlyMaintenanceAsync_RecentRejectedApplication_KeepsDocuments()
    {
        var rejected = new ClubApplication
        {
            Id = 3, StudentId = 1, AcademicTermId = 1, ProposedName = "Yeni Ret", Justification = "x",
            ProposedAdvisorId = 1, Status = ApplicationStatus.Rejected,
            AppliedAtUtc = FixedNow.AddDays(-40), ReviewedAtUtc = FixedNow.AddDays(-30),
        };
        var link = new ClubApplicationDocument { Id = 30, ClubApplicationId = 3, ClubDocumentTypeId = 1, StoredFileId = 300 };
        var file = new StoredFile
        {
            Id = 300, GeneratedFileName = "yeni.pdf", OriginalFileName = "evrak.pdf", ContentType = "application/pdf",
            FileSizeBytes = 10, Visibility = FileVisibility.Protected, UploadedByUserId = 1, UploadedAtUtc = FixedNow.AddDays(-40),
        };

        SetupApplicationCleanupScenario([rejected], [link], [file]);

        var result = await _sut.RunNightlyMaintenanceAsync();

        Assert.True(result.IsSuccess);
        _clubApplicationDocumentRepository.Verify(r => r.Delete(It.IsAny<ClubApplicationDocument>()), Times.Never);
        _storedFileRepository.Verify(r => r.Delete(It.IsAny<StoredFile>()), Times.Never);
    }

    [Fact(DisplayName = "K-37 Tuzak 3: StoredFile kaydı olmayan disk dosyası (sahipsiz) silinir")]
    public async Task RunNightlyMaintenanceAsync_OrphanDiskFile_IsDeleted()
    {
        var known = new StoredFile
        {
            Id = 400, GeneratedFileName = "kayitli.pdf", OriginalFileName = "evrak.pdf", ContentType = "application/pdf",
            FileSizeBytes = 10, Visibility = FileVisibility.Protected, UploadedByUserId = 1, UploadedAtUtc = FixedNow,
        };
        _storedFileRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<StoredFile, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<StoredFile, bool>> f, CancellationToken _) =>
                new[] { known }.AsQueryable().Where(f).ToList());

        _fileStorage
            .Setup(s => s.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["kayitli.pdf", "sahipsiz.pdf"]);

        var result = await _sut.RunNightlyMaintenanceAsync();

        Assert.True(result.IsSuccess);
        _fileStorage.Verify(s => s.DeleteAsync("sahipsiz.pdf", It.IsAny<CancellationToken>()), Times.Once);
        _fileStorage.Verify(s => s.DeleteAsync("kayitli.pdf", It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Predicate'ler GERÇEKTEN çalıştırılır — Moq salt-geçiş olsaydı testler kuralı kanıtlamazdı.</summary>
    private void SetupApplicationCleanupScenario(
        ClubApplication[] applications, ClubApplicationDocument[] links, StoredFile[] files)
    {
        _clubApplicationRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubApplication, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ClubApplication, bool>> f, CancellationToken _) =>
                applications.AsQueryable().Where(f).ToList());

        _clubApplicationDocumentRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubApplicationDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ClubApplicationDocument, bool>> f, CancellationToken _) =>
                links.AsQueryable().Where(f).ToList());

        _storedFileRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<StoredFile, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<StoredFile, bool>> f, CancellationToken _) =>
                files.AsQueryable().Where(f).ToList());
    }
}
