using Business.DTOs.Files;
using Core.Aspects.Autofac;
using Core.Utilities.Results;
using DataAccess.Seed;

namespace Business.Abstract;

public interface IFileService
{
    [SecuredOperation(IdentitySeedData.Permissions.FilesUpload)]
    [TransactionAspect]
    [CacheRemoveAspect("ClubManager.")]
    Task<IDataResult<UploadedFileDto>> UploadClubLogoAsync(int clubId, UploadFileRequestDto request, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.FilesUpload)]
    [TransactionAspect]
    Task<IDataResult<UploadedFileDto>> UploadEventPosterAsync(int eventId, UploadFileRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>docs/MIMARI.md · Y-52: anonim — aspect kasıtlı olarak yok, sorgu Visibility==Public'e sabit.</summary>
    Task<IDataResult<FileContentDto>> GetPublicFileAsync(int fileId, CancellationToken cancellationToken = default);
}
