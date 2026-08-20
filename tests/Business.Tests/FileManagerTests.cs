using System.Linq.Expressions;
using Business.Concrete;
using Business.DTOs.Files;
using Core.DataAccess;
using Core.Utilities.Files;
using Core.Utilities.Results;
using Core.Utilities.Security;
using Core.Utilities.Time;
using Entities;
using Entities.Enums;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>docs/MIMARI.md · A-20: her iş kuralı için bir kabul + bir ret.</summary>
public class FileManagerTests
{
    private static readonly DateTime FixedNow = new(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);
    private static readonly byte[] ValidPngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];

    private readonly Mock<IEntityRepository<StoredFile>> _storedFileRepository = new();
    private readonly Mock<IEntityRepository<Club>> _clubRepository = new();
    private readonly Mock<IEntityRepository<Event>> _eventRepository = new();
    private readonly Mock<IEntityRepository<AcademicStaff>> _academicStaffRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IFileStorage> _fileStorage = new();
    private readonly FileManager _sut;

    public FileManagerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(FixedNow);
        var settings = Options.Create(new FileStorageSettings { MaxUploadBytes = 5 * 1024 * 1024 });

        _sut = new FileManager(
            _storedFileRepository.Object,
            _clubRepository.Object,
            _eventRepository.Object,
            _academicStaffRepository.Object,
            _unitOfWork.Object,
            _currentUser.Object,
            _clock.Object,
            _fileStorage.Object,
            settings);
    }

    private static Club CreateClub(int id = 1, int advisorId = 10) => new()
    {
        Id = id, Name = "Test Kulübü", AdvisorId = advisorId, IsActive = true, CreatedAtUtc = FixedNow,
    };

    private static AcademicStaff CreateAdvisor(int id = 10, int userId = 200) => new()
    {
        Id = id, ApplicationUserId = userId, Title = "Dr.", DepartmentId = 1,
    };

    [Fact(DisplayName = "UploadClubLogoAsync: geçerli PNG ve doğru danışman kabul edilir, ad sunucuda üretilir")]
    public async Task UploadClubLogoAsync_ValidPngByCorrectAdvisor_Succeeds()
    {
        _currentUser.Setup(c => c.UserId).Returns(200);
        var club = CreateClub();
        var advisor = CreateAdvisor();

        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(advisor);

        StoredFile? addedFile = null;
        _storedFileRepository
            .Setup(r => r.AddAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()))
            .Callback<StoredFile, CancellationToken>((f, _) =>
            {
                addedFile = f;
                f.Id = 42;
            })
            .Returns(Task.CompletedTask);

        var request = new UploadFileRequestDto { Content = new MemoryStream(ValidPngBytes), OriginalFileName = "istemcinin-verdigi-ad.exe", Length = ValidPngBytes.Length };

        var result = await _sut.UploadClubLogoAsync(club.Id, request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(addedFile);
        Assert.Equal(FileVisibility.Public, addedFile!.Visibility);
        Assert.Equal("image/png", addedFile.ContentType);
        Assert.EndsWith(".png", addedFile.GeneratedFileName);
        // Y-40: sunucu tarafında üretilen ad — istemcinin verdiği ad/uzantı hiçbir zaman kullanılmaz.
        Assert.NotEqual("istemcinin-verdigi-ad.exe", addedFile.GeneratedFileName);
        Assert.True(Guid.TryParse(Path.GetFileNameWithoutExtension(addedFile.GeneratedFileName), out _));
        _clubRepository.Verify(r => r.Update(It.Is<Club>(c => c.LogoFileId == 42)), Times.Once);
    }

    [Fact(DisplayName = "UploadClubLogoAsync: içeriği metin olan sahte .png reddedilir (Y-40)")]
    public async Task UploadClubLogoAsync_TextContentDisguisedAsPng_ReturnsValidationError()
    {
        _currentUser.Setup(c => c.UserId).Returns(200);
        var club = CreateClub();
        var advisor = CreateAdvisor();
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(advisor);

        var textBytes = "bu bir resim degil, sadece metin"u8.ToArray();
        var request = new UploadFileRequestDto { Content = new MemoryStream(textBytes), OriginalFileName = "logo.png", Length = textBytes.Length };

        var result = await _sut.UploadClubLogoAsync(club.Id, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ValidationError, result.Status);
        _storedFileRepository.Verify(r => r.AddAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UploadClubLogoAsync: boyut sınırını (5 MB) aşan dosya reddedilir")]
    public async Task UploadClubLogoAsync_TooLarge_ReturnsValidationError()
    {
        _currentUser.Setup(c => c.UserId).Returns(200);
        var club = CreateClub();
        var advisor = CreateAdvisor();
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(advisor);

        var request = new UploadFileRequestDto { Content = new MemoryStream(ValidPngBytes), OriginalFileName = "logo.png", Length = 6 * 1024 * 1024 };

        var result = await _sut.UploadClubLogoAsync(club.Id, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ValidationError, result.Status);
        _storedFileRepository.Verify(r => r.AddAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UploadClubLogoAsync: kulübün danışmanı olmayan kullanıcı reddedilir (Y-23)")]
    public async Task UploadClubLogoAsync_NotClubAdvisor_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(999);
        var club = CreateClub();
        var advisor = CreateAdvisor();
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(advisor);

        var request = new UploadFileRequestDto { Content = new MemoryStream(ValidPngBytes), OriginalFileName = "logo.png", Length = ValidPngBytes.Length };

        var result = await _sut.UploadClubLogoAsync(club.Id, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _storedFileRepository.Verify(r => r.AddAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "GetPublicFileAsync: sorgu predicate'i Protected bir kayda uygulandığında false döner (Y-52)")]
    public async Task GetPublicFileAsync_QueryPredicate_ExcludesProtectedFiles()
    {
        Expression<Func<StoredFile, bool>>? capturedFilter = null;
        _storedFileRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<StoredFile, bool>>>(), It.IsAny<CancellationToken>()))
            .Callback<Expression<Func<StoredFile, bool>>, CancellationToken>((filter, _) => capturedFilter = filter)
            .ReturnsAsync((StoredFile?)null);

        await _sut.GetPublicFileAsync(1);

        Assert.NotNull(capturedFilter);
        var protectedFile = new StoredFile
        {
            Id = 1,
            GeneratedFileName = "x.xlsx",
            OriginalFileName = "rapor.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileSizeBytes = 10,
            Visibility = FileVisibility.Protected,
            UploadedByUserId = 1,
            UploadedAtUtc = FixedNow,
        };

        Assert.False(capturedFilter!.Compile()(protectedFile));
    }

    [Fact(DisplayName = "GetPublicFileAsync: bulunamayan dosya NotFound döner")]
    public async Task GetPublicFileAsync_NotFound_ReturnsNotFound()
    {
        _storedFileRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<StoredFile, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoredFile?)null);

        var result = await _sut.GetPublicFileAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
