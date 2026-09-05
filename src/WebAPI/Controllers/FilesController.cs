using Business.Abstract;
using Business.DTOs.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

/// <summary>
/// docs/MIMARI.md · Y-52: rota ayrımı — anonim tek uç (Public görünürlük), korumalı indirme
/// (rapor çıktıları) ReportsController'da ayrı bir yolda yaşar. Y-49: yükleme klasörü
/// UseStaticFiles ile yayınlanmaz, tek erişim noktası burasıdır.
/// </summary>
[ApiController]
[Route("api")]
public sealed class FilesController(IFileService fileService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("files/{id:int}")]
    public async Task<IActionResult> GetPublicFile(int id, CancellationToken cancellationToken)
    {
        var result = await fileService.GetPublicFileAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ToActionResult();
        }

        // A-36: tarayıcı önbelleği çalışsın diye — açık görseller (logo/afiş) sık değişmez.
        Response.Headers.CacheControl = "public, max-age=86400";
        return File(result.Data.Content, result.Data.ContentType);
    }

    [HttpPost("clubs/{clubId:int}/logo")]
    [RequestSizeLimit(5_242_880)]
    [RequestFormLimits(MultipartBodyLengthLimit = 5_242_880)]
    public async Task<IActionResult> UploadClubLogo(int clubId, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await fileService.UploadClubLogoAsync(
            clubId,
            new UploadFileRequestDto { Content = stream, OriginalFileName = file.FileName, Length = file.Length },
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("events/{eventId:int}/poster")]
    [RequestSizeLimit(5_242_880)]
    [RequestFormLimits(MultipartBodyLengthLimit = 5_242_880)]
    public async Task<IActionResult> UploadEventPoster(int eventId, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await fileService.UploadEventPosterAsync(
            eventId,
            new UploadFileRequestDto { Content = stream, OriginalFileName = file.FileName, Length = file.Length },
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("announcements/{announcementId:int}/image")]
    [RequestSizeLimit(5_242_880)]
    [RequestFormLimits(MultipartBodyLengthLimit = 5_242_880)]
    public async Task<IActionResult> UploadAnnouncementImage(int announcementId, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await fileService.UploadAnnouncementImageAsync(
            announcementId,
            new UploadFileRequestDto { Content = stream, OriginalFileName = file.FileName, Length = file.Length },
            cancellationToken);

        return result.ToActionResult();
    }
}
