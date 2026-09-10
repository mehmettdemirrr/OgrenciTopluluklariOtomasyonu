using System.IO.Compression;
using System.Linq.Expressions;
using Business.Abstract;
using Business.Concrete;
using Business.Constants;
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
    private static readonly byte[] PdfBytes = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34];

    private readonly Mock<IEntityRepository<StoredFile>> _storedFileRepository = new();
    private readonly Mock<IEntityRepository<Club>> _clubRepository = new();
    private readonly Mock<IEntityRepository<Event>> _eventRepository = new();
    private readonly Mock<IEntityRepository<AcademicStaff>> _academicStaffRepository = new();
    private readonly Mock<IEntityRepository<Announcement>> _announcementRepository = new();
    private readonly Mock<IAnnouncementService> _announcementService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IFileStorage> _fileStorage = new();
    private readonly FileManager _sut;

    public FileManagerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(FixedNow);

        // Y-66: kapsam metotları artık ilk satırda currentUser.Permissions okuyor — varsayılan boş.
        _currentUser.Setup(c => c.Permissions).Returns([]);
        var settings = Options.Create(new FileStorageSettings { MaxUploadBytes = 5 * 1024 * 1024 });

        _sut = new FileManager(
            _storedFileRepository.Object,
            _clubRepository.Object,
            _eventRepository.Object,
            _academicStaffRepository.Object,
            _announcementRepository.Object,
            _announcementService.Object,
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

    [Fact(DisplayName = "UploadAnnouncementImageAsync: duyuruyu yönetemeyen kullanıcı reddedilir")]
    public async Task UploadAnnouncementImageAsync_Rejects_WhenCallerCannotManageAnnouncements()
    {
        _announcementService
            .Setup(s => s.EnsureCanManageAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Forbidden("yasak"));

        var request = new UploadFileRequestDto { Content = new MemoryStream(ValidPngBytes), OriginalFileName = "kapak.png", Length = ValidPngBytes.Length };

        var result = await _sut.UploadAnnouncementImageAsync(1, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _storedFileRepository.Verify(r => r.AddAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UploadAnnouncementImageAsync: yönetebilen kullanıcı görseli yükleyebilir")]
    public async Task UploadAnnouncementImageAsync_StoresImage_WhenCallerCanManageAnnouncements()
    {
        _announcementService
            .Setup(s => s.EnsureCanManageAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        var announcement = new Announcement { Id = 1, ClubId = 1, Title = "Duyuru", Content = "İçerik", Visibility = AnnouncementVisibility.Public, PublishedAtUtc = FixedNow };
        _announcementRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Announcement, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(announcement);
        _storedFileRepository
            .Setup(r => r.AddAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()))
            .Callback<StoredFile, CancellationToken>((f, _) => f.Id = 77)
            .Returns(Task.CompletedTask);

        var request = new UploadFileRequestDto { Content = new MemoryStream(ValidPngBytes), OriginalFileName = "kapak.png", Length = ValidPngBytes.Length };

        var result = await _sut.UploadAnnouncementImageAsync(1, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(77, announcement.ImageFileId);
        _announcementRepository.Verify(r => r.Update(It.Is<Announcement>(a => a.ImageFileId == 77)), Times.Once);
    }

    [Fact(DisplayName = "UploadAnnouncementImageAsync: PNG uzantılı ama PDF içerikli dosya reddedilir (Y-40)")]
    public async Task UploadAnnouncementImageAsync_Rejects_PdfDisguisedAsPng()
    {
        _announcementService
            .Setup(s => s.EnsureCanManageAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        var announcement = new Announcement { Id = 1, ClubId = 1, Title = "Duyuru", Content = "İçerik", Visibility = AnnouncementVisibility.Public, PublishedAtUtc = FixedNow };
        _announcementRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Announcement, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(announcement);

        var request = new UploadFileRequestDto { Content = new MemoryStream(PdfBytes), OriginalFileName = "kapak.png", Length = PdfBytes.Length };

        var result = await _sut.UploadAnnouncementImageAsync(1, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ValidationError, result.Status);
        _storedFileRepository.Verify(r => r.AddAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "A-64: şablon yolu Docx kabul eder ve Public saklar")]
    public async Task StoreDocumentTemplateAsync_AcceptsDocx_AsPublic()
    {
        var docx = BuildOfficePackage("word/document.xml", "wordprocessingml");
        StoredFile? addedFile = null;
        _storedFileRepository
            .Setup(r => r.AddAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()))
            .Callback<StoredFile, CancellationToken>((f, _) =>
            {
                addedFile = f;
                f.Id = 11;
            })
            .Returns(Task.CompletedTask);

        var result = await _sut.StoreDocumentTemplateAsync(
            new UploadFileRequestDto { Content = new MemoryStream(docx), OriginalFileName = "FR-0230.docx", Length = docx.Length });

        Assert.True(result.IsSuccess);
        Assert.NotNull(addedFile);
        Assert.Equal(FileVisibility.Public, addedFile!.Visibility);
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", addedFile.ContentType);
        Assert.EndsWith(".docx", addedFile.GeneratedFileName);
    }

    [Fact(DisplayName = "A-64: şablon yolu PDF kabul eder")]
    public async Task StoreDocumentTemplateAsync_AcceptsPdf()
    {
        _storedFileRepository
            .Setup(r => r.AddAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()))
            .Callback<StoredFile, CancellationToken>((f, _) => f.Id = 12)
            .Returns(Task.CompletedTask);

        var result = await _sut.StoreDocumentTemplateAsync(
            new UploadFileRequestDto { Content = new MemoryStream(PdfBytes), OriginalFileName = "sablon.pdf", Length = PdfBytes.Length });

        Assert.True(result.IsSuccess);
        Assert.Equal(12, result.Data.FileId);
    }

    [Fact(DisplayName = "Y-40: şablon yoluna xlsx/zip yüklenemez")]
    public async Task StoreDocumentTemplateAsync_RejectsXlsx()
    {
        var xlsx = BuildOfficePackage("xl/workbook.xml", "spreadsheetml");

        var result = await _sut.StoreDocumentTemplateAsync(
            new UploadFileRequestDto { Content = new MemoryStream(xlsx), OriginalFileName = "sablon.xlsx", Length = xlsx.Length });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ValidationError, result.Status);
        Assert.Equal(Messages.UnsupportedDocumentTemplateType, result.Message);
        _storedFileRepository.Verify(r => r.AddAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "A-64: şablon yoluna PNG yüklenemez")]
    public async Task StoreDocumentTemplateAsync_RejectsPng()
    {
        var result = await _sut.StoreDocumentTemplateAsync(
            new UploadFileRequestDto { Content = new MemoryStream(ValidPngBytes), OriginalFileName = "sablon.png", Length = ValidPngBytes.Length });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ValidationError, result.Status);
        _storedFileRepository.Verify(r => r.AddAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "A-64 REGRESYON: kulüp logosu ucuna Docx yüklenemez")]
    public async Task UploadClubLogoAsync_RejectsDocx()
    {
        _currentUser.Setup(c => c.UserId).Returns(200);
        var club = CreateClub();
        var advisor = CreateAdvisor();
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(advisor);

        var docx = BuildOfficePackage("word/document.xml", "wordprocessingml");
        var result = await _sut.UploadClubLogoAsync(
            club.Id,
            new UploadFileRequestDto { Content = new MemoryStream(docx), OriginalFileName = "logo.docx", Length = docx.Length });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ValidationError, result.Status);
        _storedFileRepository.Verify(r => r.AddAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "A-64 REGRESYON: başvuru evrakı yoluna Docx yüklenemez")]
    public async Task StoreApplicationDocumentAsync_RejectsDocx()
    {
        var docx = BuildOfficePackage("word/document.xml", "wordprocessingml");

        var result = await _sut.StoreApplicationDocumentAsync(
            new UploadFileRequestDto { Content = new MemoryStream(docx), OriginalFileName = "evrak.docx", Length = docx.Length });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ValidationError, result.Status);
        Assert.Equal(Messages.UnsupportedDocumentFileType, result.Message);
        _storedFileRepository.Verify(r => r.AddAsync(It.IsAny<StoredFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Gerçek bir OOXML paketi üretir: girdi ADLARI sıkıştırılmadan, girdi İÇERİKLERİ deflate ile
    /// yazılır — Word'ün ürettiği dosyanın davranışı (A-83).
    /// </summary>
    private static byte[] BuildOfficePackage(string entryPath, string contentTypeMarker)
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            var contentTypes = archive.CreateEntry("[Content_Types].xml", CompressionLevel.Optimal);
            using (var writer = new StreamWriter(contentTypes.Open()))
            {
                writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types>");
                for (var i = 0; i < 40; i++)
                {
                    writer.Write($"<Default Extension=\"rels{i}\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
                }

                writer.Write($"<Override PartName=\"/{entryPath}\" ContentType=\"application/vnd.openxmlformats-officedocument.{contentTypeMarker}.document.main+xml\"/></Types>");
            }

            var document = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
            using var documentWriter = new StreamWriter(document.Open());
            documentWriter.Write("<w:document><w:body/></w:document>");
        }

        return buffer.ToArray();
    }
}
