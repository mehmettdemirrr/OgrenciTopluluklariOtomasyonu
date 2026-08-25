using Business.Abstract;
using Business.Constants;
using Business.DTOs.Files;
using Core.DataAccess;
using Core.Utilities.Files;
using Core.Utilities.Results;
using Core.Utilities.Security;
using Core.Utilities.Time;
using DataAccess.Seed;
using Entities;
using Entities.Enums;
using Microsoft.Extensions.Options;

namespace Business.Concrete;

public sealed class FileManager(
    IEntityRepository<StoredFile> storedFileRepository,
    IEntityRepository<Club> clubRepository,
    IEntityRepository<Event> eventRepository,
    IEntityRepository<AcademicStaff> academicStaffRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    IFileStorage fileStorage,
    IOptions<FileStorageSettings> settings) : IFileService
{
    public async Task<IDataResult<UploadedFileDto>> UploadClubLogoAsync(int clubId, UploadFileRequestDto request, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return DataResult<UploadedFileDto>.NotFound(Messages.ClubNotFound);
        }

        var ownershipError = await EnsureClubAdvisorAsync(club, cancellationToken).ConfigureAwait(false);
        if (ownershipError is not null)
        {
            return DataResult<UploadedFileDto>.Forbidden(ownershipError);
        }

        var stored = await StoreFileAsync(request, FileVisibility.Public, cancellationToken).ConfigureAwait(false);
        if (!stored.IsSuccess)
        {
            return stored;
        }

        club.LogoFileId = stored.Data.FileId;
        clubRepository.Update(club);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return DataResult<UploadedFileDto>.Success(stored.Data, Messages.LogoUploaded);
    }

    public async Task<IDataResult<UploadedFileDto>> UploadEventPosterAsync(int eventId, UploadFileRequestDto request, CancellationToken cancellationToken = default)
    {
        var @event = await eventRepository.GetAsync(e => e.Id == eventId, cancellationToken).ConfigureAwait(false);
        if (@event is null)
        {
            return DataResult<UploadedFileDto>.NotFound(Messages.EventNotFound);
        }

        var club = await clubRepository.GetAsync(c => c.Id == @event.ClubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return DataResult<UploadedFileDto>.NotFound(Messages.ClubNotFound);
        }

        var ownershipError = await EnsureClubAdvisorAsync(club, cancellationToken).ConfigureAwait(false);
        if (ownershipError is not null)
        {
            return DataResult<UploadedFileDto>.Forbidden(ownershipError);
        }

        var stored = await StoreFileAsync(request, FileVisibility.Public, cancellationToken).ConfigureAwait(false);
        if (!stored.IsSuccess)
        {
            return stored;
        }

        @event.PosterFileId = stored.Data.FileId;
        eventRepository.Update(@event);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return DataResult<UploadedFileDto>.Success(stored.Data, Messages.PosterUploaded);
    }

    public async Task<IDataResult<FileContentDto>> GetPublicFileAsync(int fileId, CancellationToken cancellationToken = default)
    {
        // Y-52: Protected bir kayıt bu sorguya asla girmez — ikinci savunma katmanı (rota ayrımı
        // WebAPI'de, controller anonim ucu FilesController'da ayrı tutar).
        var file = await storedFileRepository
            .GetAsync(f => f.Id == fileId && f.Visibility == FileVisibility.Public, cancellationToken)
            .ConfigureAwait(false);

        if (file is null)
        {
            return DataResult<FileContentDto>.NotFound(Messages.FileNotFound);
        }

        var stream = await fileStorage.OpenReadAsync(file.GeneratedFileName, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return DataResult<FileContentDto>.NotFound(Messages.FileNotFound);
        }

        return DataResult<FileContentDto>.Success(new FileContentDto
        {
            Content = stream,
            ContentType = file.ContentType,
            DownloadFileName = file.OriginalFileName,
        });
    }

    // Y-23: izin claim'i (files.upload) yeterli değil — yalnızca kulübün danışmanı yükleyebilir.
    private async Task<string?> EnsureClubAdvisorAsync(Club club, CancellationToken cancellationToken)
    {
        // Y-66: yönetici kontrolü her kapsam metodunun İLK satırıdır (A-55).
        if (currentUser.Permissions.Contains(IdentitySeedData.Permissions.ClubsManageAll))
        {
            return null;
        }

        if (currentUser.UserId is not { } userId)
        {
            return Messages.NotClubAdvisor;
        }

        var advisor = await academicStaffRepository.GetAsync(s => s.Id == club.AdvisorId, cancellationToken).ConfigureAwait(false);
        return advisor is null || advisor.ApplicationUserId != userId ? Messages.NotClubAdvisor : null;
    }

    private async Task<IDataResult<UploadedFileDto>> StoreFileAsync(UploadFileRequestDto request, FileVisibility visibility, CancellationToken cancellationToken)
    {
        if (request.Length > settings.Value.MaxUploadBytes)
        {
            return DataResult<UploadedFileDto>.ValidationError(Messages.FileTooLarge);
        }

        using var buffer = new MemoryStream();
        await request.Content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;

        var header = new byte[12];
        var headerLength = await buffer.ReadAsync(header.AsMemory(0, 12), cancellationToken).ConfigureAwait(false);
        var detectedType = FileSignatureInspector.Detect(header.AsSpan(0, headerLength));
        var contentType = detectedType.ToContentType();
        var extension = detectedType.ToExtension();

        if (contentType is null || extension is null)
        {
            return DataResult<UploadedFileDto>.ValidationError(Messages.UnsupportedFileType);
        }

        buffer.Position = 0;

        // Y-40: ad sunucu tarafında üretilir — istemcinin verdiği dosya adı/uzantı asla kullanılmaz.
        var generatedFileName = $"{Guid.NewGuid():N}{extension}";
        await fileStorage.SaveAsync(generatedFileName, buffer, cancellationToken).ConfigureAwait(false);

        var storedFile = new StoredFile
        {
            GeneratedFileName = generatedFileName,
            OriginalFileName = request.OriginalFileName,
            ContentType = contentType,
            FileSizeBytes = buffer.Length,
            Visibility = visibility,
            UploadedByUserId = currentUser.UserId ?? 0,
            UploadedAtUtc = clock.UtcNow,
        };

        await storedFileRepository.AddAsync(storedFile, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return DataResult<UploadedFileDto>.Success(new UploadedFileDto
        {
            FileId = storedFile.Id,
            ContentType = storedFile.ContentType,
            FileSizeBytes = storedFile.FileSizeBytes,
        });
    }
}
