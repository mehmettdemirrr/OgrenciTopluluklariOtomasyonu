using System.Linq.Expressions;
using Business.Abstract;
using Business.Concrete;
using Business.DTOs.Reports;
using Core.DataAccess;
using Core.Utilities.Files;
using Core.Utilities.Results;
using Core.Utilities.Time;
using DataAccess.Repositories;
using Entities;
using Entities.Dtos.Reports;
using Entities.Enums;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>docs/MIMARI.md · Y-51 (üretim yarısı) / Y-47 (idempotentlik).</summary>
public class ReportGenerationManagerTests
{
    private static readonly DateTime FixedNow = new(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IEntityRepository<ReportRequest>> _reportRequestRepository = new();
    private readonly Mock<IEntityRepository<StoredFile>> _storedFileRepository = new();
    private readonly Mock<IEntityRepository<Club>> _clubRepository = new();
    private readonly Mock<IEntityRepository<Event>> _eventRepository = new();
    private readonly Mock<IEntityRepository<AcademicTerm>> _academicTermRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IReportDal> _reportDal = new();
    private readonly Mock<IReportScopeResolver> _scopeResolver = new();
    private readonly Mock<IExcelReportBuilder> _excelReportBuilder = new();
    private readonly Mock<IFileStorage> _fileStorage = new();
    private readonly ReportGenerationManager _sut;

    public ReportGenerationManagerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(FixedNow);

        _sut = new ReportGenerationManager(
            _reportRequestRepository.Object,
            _storedFileRepository.Object,
            _clubRepository.Object,
            _eventRepository.Object,
            _academicTermRepository.Object,
            _unitOfWork.Object,
            _clock.Object,
            _reportDal.Object,
            _scopeResolver.Object,
            _excelReportBuilder.Object,
            _fileStorage.Object);
    }

    private static ReportRequest CreateQueuedRequest() => new()
    {
        Id = 1,
        RequestedByUserId = 1,
        ReportType = "ClubMembers",
        ParametersJson = "{\"ClubId\":5,\"EventId\":null,\"AcademicTermId\":3}",
        Status = ReportStatus.Queued,
        RequestedAtUtc = FixedNow,
    };

    [Fact(DisplayName = "GenerateAsync: Queued dışındaki bir durumda tekrar çağrılırsa hiçbir şey yazmaz (Y-47)")]
    public async Task GenerateAsync_AlreadyReady_DoesNothing()
    {
        var reportRequest = new ReportRequest
        {
            Id = 1, RequestedByUserId = 1, ReportType = "ClubMembers", Status = ReportStatus.Ready, OutputFileId = 50, RequestedAtUtc = FixedNow,
        };
        _reportRequestRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ReportRequest, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(reportRequest);

        var result = await _sut.GenerateAsync(1);

        Assert.True(result.IsSuccess);
        _reportRequestRepository.Verify(r => r.Update(It.IsAny<ReportRequest>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "GenerateAsync: talep sahibi kapsam dışına çıkmışsa üretim yapılmadan Failed işaretlenir (Y-51)")]
    public async Task GenerateAsync_RequesterOutsideScope_MarksFailedWithoutGenerating()
    {
        var reportRequest = CreateQueuedRequest();
        _reportRequestRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ReportRequest, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(reportRequest);
        _scopeResolver.Setup(s => s.ResolveAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ReportScope.None);

        var result = await _sut.GenerateAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReportStatus.Failed, reportRequest.Status);
        _reportDal.Verify(d => d.GetClubMemberRowsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _excelReportBuilder.Verify(b => b.BuildClubMemberWorkbook(It.IsAny<IReadOnlyList<ClubMemberExportRowDto>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _fileStorage.Verify(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "GenerateAsync: kapsam içinde başarılı üretim StoredFile (Protected) oluşturur ve Ready işaretler")]
    public async Task GenerateAsync_WithinScope_ProducesFileAndMarksReady()
    {
        var reportRequest = CreateQueuedRequest();
        _reportRequestRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ReportRequest, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(reportRequest);
        _scopeResolver.Setup(s => s.ResolveAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new ReportScope(false, [5]));

        var club = new Club { Id = 5, Name = "Test Kulübü", AdvisorId = 10, IsActive = true, CreatedAtUtc = FixedNow };
        var term = new AcademicTerm { Id = 3, Name = "2026 Güz", StartDateUtc = FixedNow.AddMonths(-1), EndDateUtc = FixedNow.AddMonths(3), IsCurrent = true };
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(term);
        _reportDal.Setup(d => d.GetClubMemberRowsAsync(5, 3, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _excelReportBuilder
            .Setup(b => b.BuildClubMemberWorkbook(It.IsAny<IReadOnlyList<ClubMemberExportRowDto>>(), club.Name, term.Name))
            .Returns([1, 2, 3]);

        StoredFile? addedFile = null;
        _storedFileRepository
            .Setup(r => r.AddAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()))
            .Callback<StoredFile, CancellationToken>((f, _) =>
            {
                addedFile = f;
                f.Id = 77;
            })
            .Returns(Task.CompletedTask);

        var result = await _sut.GenerateAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReportStatus.Ready, reportRequest.Status);
        Assert.Equal(77, reportRequest.OutputFileId);
        Assert.NotNull(addedFile);
        Assert.Equal(FileVisibility.Protected, addedFile!.Visibility);
        _fileStorage.Verify(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "GenerateAsync: TermSummary — kapsam içi kullanıcı için ClubId'ler kapsamdan alınıp dönem özeti üretilir")]
    public async Task GenerateAsync_TermSummaryWithinScope_ProducesFileFromScopedClubIds()
    {
        var reportRequest = new ReportRequest
        {
            Id = 1, RequestedByUserId = 1, ReportType = "TermSummary", Status = ReportStatus.Queued, RequestedAtUtc = FixedNow,
        };
        _reportRequestRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ReportRequest, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(reportRequest);
        _scopeResolver.Setup(s => s.ResolveAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new ReportScope(false, [5, 9]));
        _reportDal
            .Setup(d => d.GetTermSummaryAsync(
                It.Is<IReadOnlyCollection<int>?>(c => c != null && c.OrderBy(x => x).SequenceEqual(new[] { 5, 9 })), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _excelReportBuilder.Setup(b => b.BuildTermSummaryWorkbook(It.IsAny<IReadOnlyList<TermSummaryRowDto>>())).Returns([9, 9, 9]);

        StoredFile? addedFile = null;
        _storedFileRepository
            .Setup(r => r.AddAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()))
            .Callback<StoredFile, CancellationToken>((f, _) =>
            {
                addedFile = f;
                f.Id = 88;
            })
            .Returns(Task.CompletedTask);

        var result = await _sut.GenerateAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReportStatus.Ready, reportRequest.Status);
        Assert.NotNull(addedFile);
        _clubRepository.Verify(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "GenerateAsync: TermSummary — kapsamı olmayan kullanıcı için üretim yapılmadan Failed işaretlenir")]
    public async Task GenerateAsync_TermSummaryNoScope_MarksFailedWithoutGenerating()
    {
        var reportRequest = new ReportRequest
        {
            Id = 1, RequestedByUserId = 1, ReportType = "TermSummary", Status = ReportStatus.Queued, RequestedAtUtc = FixedNow,
        };
        _reportRequestRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ReportRequest, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(reportRequest);
        _scopeResolver.Setup(s => s.ResolveAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ReportScope.None);

        var result = await _sut.GenerateAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReportStatus.Failed, reportRequest.Status);
        _reportDal.Verify(d => d.GetTermSummaryAsync(It.IsAny<IReadOnlyCollection<int>?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
