using Business.Abstract;
using Business.DTOs.Reference;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

/// <summary>docs/MIMARI.md · K-37/A-62: evrak tipi kataloğu — ClubCategoriesController ile aynı biçim.</summary>
[ApiController]
[Route("api/club-document-types")]
public sealed class ClubDocumentTypesController(IReferenceDataService referenceDataService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetClubDocumentTypes(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var result = await referenceDataService.GetClubDocumentTypesPagedAsync(pageIndex, pageSize, activeOnly, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> CreateClubDocumentType(CreateClubDocumentTypeRequestDto request, CancellationToken cancellationToken)
    {
        var result = await referenceDataService.CreateClubDocumentTypeAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateClubDocumentType(int id, UpdateClubDocumentTypeRequestDto request, CancellationToken cancellationToken)
    {
        var result = await referenceDataService.UpdateClubDocumentTypeAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteClubDocumentType(int id, CancellationToken cancellationToken)
    {
        var result = await referenceDataService.DeleteClubDocumentTypeAsync(id, cancellationToken);
        return result.ToActionResult();
    }
}
