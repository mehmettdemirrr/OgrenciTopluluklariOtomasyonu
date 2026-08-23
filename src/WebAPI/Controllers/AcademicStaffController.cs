using Business.Abstract;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/academic-staff")]
public sealed class AcademicStaffController(IReferenceDataService referenceDataService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await referenceDataService.GetAcademicStaffPagedAsync(pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>docs/PLAN-V3.md §17.4: topluluk kurma başvurusundaki danışman seçici — herhangi bir kimliği doğrulanmış kullanıcı.</summary>
    [HttpGet("selectable")]
    public async Task<IActionResult> GetSelectableList([FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await referenceDataService.GetSelectableAcademicStaffAsync(pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }
}
