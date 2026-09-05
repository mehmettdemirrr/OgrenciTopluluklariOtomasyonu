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

    /// <summary>
    /// docs/MIMARI.md · K-37/A-63/A-64: kuruluş evrakı — yalnızca PDF, daima Protected.
    /// Controller ucu YOKTUR; yalnızca ClubApplicationManager çağırır (başvuru akışının parçası),
    /// bu yüzden [SecuredOperation] da yok — yetki SubmitAsync'te zaten kurulmuş durumda.
    /// </summary>
    Task<IDataResult<UploadedFileDto>> StoreApplicationDocumentAsync(
        UploadFileRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/MIMARI.md · K-40/A-64: başvuru logosu — yalnızca JPEG/PNG/WebP, Public.
    /// Controller ucu YOKTUR; yalnızca ClubApplicationManager çağırır (başvuru akışının parçası),
    /// bu yüzden [SecuredOperation] da yok — yetki SubmitAsync'te zaten kurulmuş durumda.
    /// Kulüp henüz yoktur; bu yüzden UploadClubLogoAsync'in danışman kontrolü BURADA YAPILAMAZ (A-69).
    /// </summary>
    Task<IDataResult<UploadedFileDto>> StoreApplicationLogoAsync(
        UploadFileRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/MIMARI.md · K-42/A-64: duyuru kapak görseli — yalnızca JPEG/PNG/WebP, Public.
    /// Yetki: duyuruyu yönetebilen yönetir (kulüp duyurusunda A-68 AnnouncementsManage,
    /// sistem duyurusunda announcements.global) — kapı IAnnouncementService.EnsureCanManageAsync'tedir.
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.FilesUpload)]
    [TransactionAspect]
    Task<IDataResult<UploadedFileDto>> UploadAnnouncementImageAsync(
        int announcementId, UploadFileRequestDto request, CancellationToken cancellationToken = default);
}
