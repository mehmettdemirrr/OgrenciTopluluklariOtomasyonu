using Business.Abstract;
using Business.DTOs.ClubApplications;
using Business.DTOs.Files;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;
using WebAPI.Models;

namespace WebAPI.Controllers;

/// <summary>docs/PLAN-V3.md §17: MembershipApplicationsController'ın kulüp kurma karşılığı.</summary>
[ApiController]
[Route("api")]
public sealed class ClubApplicationsController(IClubApplicationService clubApplicationService) : ControllerBase
{
    /// <summary>
    /// docs/MIMARI.md · O-22: form + evraklar tek istekte, atomik. Y-05/Y-09: IFormFile bu katmanda kalır.
    /// Y-01: aşağıdaki döngü bağlama/dönüştürmedir — tek `if` null dosyayı atlıyor, iş kuralı değil.
    /// Karar veren tek satır SubmitAsync'tedir.
    /// </summary>
    [HttpPost("club-applications")]
    [RequestSizeLimit(62_914_560)]
    [RequestFormLimits(MultipartBodyLengthLimit = 62_914_560)]
    public async Task<IActionResult> Submit([FromForm] SubmitClubApplicationForm form, CancellationToken cancellationToken)
    {
        var documents = new List<ClubApplicationDocumentUploadDto>(form.Documents.Count);
        var streams = new List<Stream>(form.Documents.Count);

        try
        {
            foreach (var part in form.Documents)
            {
                if (part.File is null)
                {
                    continue;
                }

                var stream = part.File.OpenReadStream();
                streams.Add(stream);
                documents.Add(new ClubApplicationDocumentUploadDto
                {
                    DocumentTypeId = part.DocumentTypeId,
                    File = new UploadFileRequestDto
                    {
                        Content = stream,
                        OriginalFileName = part.File.FileName,
                        Length = part.File.Length,
                    },
                });
            }

            UploadFileRequestDto? logo = null;
            if (form.Logo is not null)
            {
                var logoStream = form.Logo.OpenReadStream();
                streams.Add(logoStream);
                logo = new UploadFileRequestDto
                {
                    Content = logoStream,
                    OriginalFileName = form.Logo.FileName,
                    Length = form.Logo.Length,
                };
            }

            var request = new SubmitClubApplicationRequestDto
            {
                ProposedName = form.ProposedName,
                Description = form.Description,
                Justification = form.Justification,
                ProposedAdvisorId = form.ProposedAdvisorId,
                ProposedCategoryId = form.ProposedCategoryId,
                Logo = logo,
                Documents = documents,
            };

            var result = await clubApplicationService.SubmitAsync(request, cancellationToken);
            return result.ToActionResult();
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// docs/MIMARI.md · A-63/Y-70/Y-51: korumalı evrak indirme. FilesController'daki anonim ucun
    /// aksine Cache-Control YOK — kişisel veri tarayıcı önbelleğinde bırakılmaz.
    /// </summary>
    [HttpGet("club-applications/{applicationId:int}/documents/{documentId:int}")]
    public async Task<IActionResult> GetDocument(int applicationId, int documentId, CancellationToken cancellationToken)
    {
        var result = await clubApplicationService.GetDocumentAsync(applicationId, documentId, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ToActionResult();
        }

        return File(result.Data.Content, result.Data.ContentType, result.Data.DownloadFileName);
    }

    /// <summary>docs/MIMARI.md · K-39/A-42: giriş yapmış kullanıcıya açık; anonim vitrine eklenmez.</summary>
    [HttpGet("club-applications/window")]
    public async Task<IActionResult> GetWindow(CancellationToken cancellationToken)
    {
        var result = await clubApplicationService.GetWindowAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("club-applications/mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await clubApplicationService.GetMineAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("club-applications")]
    public async Task<IActionResult> GetPending([FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await clubApplicationService.GetPendingAsync(pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("club-applications/{id:int}/decision")]
    public async Task<IActionResult> Decide(int id, DecideClubApplicationRequestDto request, CancellationToken cancellationToken)
    {
        var result = await clubApplicationService.DecideAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }
}
