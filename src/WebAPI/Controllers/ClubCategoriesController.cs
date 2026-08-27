using Business.Abstract;
using Business.DTOs.Reference;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

/// <summary>docs/MIMARI.md · K-35/A-60: FacultiesController ile aynı biçim.</summary>
[ApiController]
[Route("api/club-categories")]
public sealed class ClubCategoriesController(IReferenceDataService referenceDataService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetClubCategories(
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await referenceDataService.GetClubCategoriesPagedAsync(pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> CreateClubCategory(CreateClubCategoryRequestDto request, CancellationToken cancellationToken)
    {
        var result = await referenceDataService.CreateClubCategoryAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateClubCategory(int id, UpdateClubCategoryRequestDto request, CancellationToken cancellationToken)
    {
        var result = await referenceDataService.UpdateClubCategoryAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteClubCategory(int id, CancellationToken cancellationToken)
    {
        var result = await referenceDataService.DeleteClubCategoryAsync(id, cancellationToken);
        return result.ToActionResult();
    }
}
