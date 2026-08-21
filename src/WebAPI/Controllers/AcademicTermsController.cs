using Business.Abstract;
using Business.DTOs.Reference;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/academic-terms")]
public sealed class AcademicTermsController(IAcademicTermService academicTermService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTerms([FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await academicTermService.GetTermsPagedAsync(pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> CreateTerm(CreateAcademicTermRequestDto request, CancellationToken cancellationToken)
    {
        var result = await academicTermService.CreateTermAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{id:int}/current")]
    public async Task<IActionResult> SetCurrent(int id, CancellationToken cancellationToken)
    {
        var result = await academicTermService.SetCurrentAsync(id, cancellationToken);
        return result.ToActionResult();
    }
}
