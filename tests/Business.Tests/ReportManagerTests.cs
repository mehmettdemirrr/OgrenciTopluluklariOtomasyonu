using System.Linq.Expressions;
using Business.Abstract;
using Business.Concrete;
using Business.DTOs.Reports;
using Core.DataAccess;
using Core.Utilities.Files;
using Core.Utilities.Results;
using Core.Utilities.Security;
using Core.Utilities.Time;
using DataAccess.Repositories;
using Entities;
using Entities.Enums;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>docs/MIMARI.md · Y-51: talep, üretim, indirme — üçü ayrı kod yolu; burada talep ve indirme kanıtlanır.</summary>
public class ReportManagerTests
{
    private static readonly DateTime FixedNow = new(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IReportDal> _reportDal = new();
    private readonly Mock<IEntityRepository<ReportRequest>> _reportRequestRepository = new();
    private readonly Mock<IEntityRepository<StoredFile>> _storedFileRepository = new();
    private readonly Mock<IEntityRepository<Club>> _clubRepository = new();
    private readonly Mock<IEntityRepository<Event>> _eventRepository = new();
    private readonly Mock<IEntityRepository<AcademicTerm>> _academicTermRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IReportScopeResolver> _scopeResolver = new();
    private readonly Mock<IFileStorage> _fileStorage = new();
    private readonly Mock<IBackgroundJobClient> _backgroundJobClient = new();
    private readonly ReportManager _sut;

    public ReportManagerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(FixedNow);

        _sut = new ReportManager(
            _reportDal.Object,
            _reportRequestRepository.Object,
            _storedFileRepository.Object,
            _clubRepository.Object,
            _eventRepository.Object,
            _academicTermRepository.Object,
            _unitOfWork.Object,
            _currentUser.Object,
            _clock.Object,
            _scopeResolver.Object,
            _fileStorage.Object,
            _backgroundJobClient.Object);
    }

    private static ReportRequest CreateReportRequest(
        int id = 10, int requestedByUserId = 1, ReportStatus status = ReportStatus.Ready, int? outputFileId = 50) => new()
    {
        Id = id,
        RequestedByUserId = requestedByUserId,
        ReportType = "ClubMembers",
        ParametersJson = "{\"ClubId\":5,\"EventId\":null,\"AcademicTermId\":3}",
        Status = status,
        OutputFileId = outputFileId,
        RequestedAtUtc = FixedNow,
    };

    [Fact(DisplayName = "RequestAsync: kapsam içi kulüp için talep kuyruğa alınır")]
    public async Task RequestAsync_WithinScope_QueuesRequest()
    {
        _currentUser.Setup(c => c.UserId).Returns(1);
        var club = new Club { Id = 5, Name = "Test", AdvisorId = 10, IsActive = true, CreatedAtUtc = FixedNow };
        var term = new AcademicTerm { Id = 3, Name = "2026 Güz", StartDateUtc = FixedNow.AddMonths(-1), EndDateUtc = FixedNow.AddMonths(3), IsCurrent = true };
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(term);
        _scopeResolver.Setup(s => s.ResolveAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new ReportScope(false, [5]));

        ReportRequest? added = null;
        _reportRequestRepository
            .Setup(r => r.AddAsync(It.IsAny<ReportRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ReportRequest, CancellationToken>((r, _) =>
            {
                added = r;
                r.Id = 100;
            })
            .Returns(Task.CompletedTask);

        var result = await _sut.RequestAsync(new CreateReportRequestDto { ReportType = ReportType.ClubMembers, ClubId = 5 });

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.Equal(ReportStatus.Queued, added!.Status);
        _backgroundJobClient.Verify(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
    }

    [Fact(DisplayName = "RequestAsync: kapsam dışı kulüp için talep reddedilir, kuyruğa hiç girmez")]
    public async Task RequestAsync_OutsideScope_ReturnsForbiddenAndNeverQueues()
    {
        _currentUser.Setup(c => c.UserId).Returns(1);
        var club = new Club { Id = 5, Name = "Test", AdvisorId = 10, IsActive = true, CreatedAtUtc = FixedNow };
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);
        _scopeResolver.Setup(s => s.ResolveAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ReportScope.None);

        var result = await _sut.RequestAsync(new CreateReportRequestDto { ReportType = ReportType.ClubMembers, ClubId = 5 });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _reportRequestRepository.Verify(r => r.AddAsync(It.IsAny<ReportRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _backgroundJobClient.Verify(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
    }

    [Fact(DisplayName = "DownloadAsync: sahip ve kapsam içiyse dosya döner")]
    public async Task DownloadAsync_OwnerWithinScope_ReturnsFileContent()
    {
        _currentUser.Setup(c => c.UserId).Returns(1);
        var reportRequest = CreateReportRequest();
        _reportRequestRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ReportRequest, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(reportRequest);
        _scopeResolver.Setup(s => s.ResolveAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new ReportScope(false, [5]));

        var storedFile = new StoredFile
        {
            Id = 50,
            GeneratedFileName = "abc.xlsx",
            OriginalFileName = "rapor.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileSizeBytes = 100,
            Visibility = FileVisibility.Protected,
            UploadedByUserId = 1,
            UploadedAtUtc = FixedNow,
        };
        _storedFileRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<StoredFile, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(storedFile);
        _fileStorage.Setup(s => s.OpenReadAsync("abc.xlsx", It.IsAny<CancellationToken>())).ReturnsAsync(new MemoryStream());

        var result = await _sut.DownloadAsync(10);

        Assert.True(result.IsSuccess);
        Assert.Equal("rapor.xlsx", result.Data.DownloadFileName);
    }

    [Fact(DisplayName = "DownloadAsync: yetki (kapsam) sonradan kaybedilmişse indirme reddedilir — Faz 6'nın çekirdek kanıtı (Y-51)")]
    public async Task DownloadAsync_ScopeLostAfterReady_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(1);
        var reportRequest = CreateReportRequest();
        _reportRequestRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ReportRequest, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(reportRequest);
        // Aynı hâlâ geçerli access token, ama danışman rolü artık alınmış — DB'deki kapsam boş.
        _scopeResolver.Setup(s => s.ResolveAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ReportScope.None);

        var result = await _sut.DownloadAsync(10);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _fileStorage.Verify(s => s.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "DownloadAsync: başkasının raporu indirilemez")]
    public async Task DownloadAsync_NotOwner_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(2);
        var reportRequest = CreateReportRequest(requestedByUserId: 1);
        _reportRequestRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ReportRequest, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(reportRequest);

        var result = await _sut.DownloadAsync(10);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _scopeResolver.Verify(s => s.ResolveAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "DownloadAsync: Ready ama çıktı dosyası yoksa (gecelik temizlik) NotFound döner")]
    public async Task DownloadAsync_ReadyWithoutOutputFile_ReturnsNotFound()
    {
        _currentUser.Setup(c => c.UserId).Returns(1);
        var reportRequest = CreateReportRequest(outputFileId: null);
        _reportRequestRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ReportRequest, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(reportRequest);

        var result = await _sut.DownloadAsync(10);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact(DisplayName = "DownloadAsync: hâlâ Queued olan rapor indirilemez")]
    public async Task DownloadAsync_StillQueued_ReturnsConflict()
    {
        _currentUser.Setup(c => c.UserId).Returns(1);
        var reportRequest = CreateReportRequest(status: ReportStatus.Queued, outputFileId: null);
        _reportRequestRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ReportRequest, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(reportRequest);

        var result = await _sut.DownloadAsync(10);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
    }
}
