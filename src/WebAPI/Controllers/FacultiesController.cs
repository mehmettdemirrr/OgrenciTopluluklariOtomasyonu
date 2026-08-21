using Business.Abstract;
using Business.DTOs.Reference;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/faculties")]
public sealed class FacultiesController(IReferenceDataService referenceDataService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetFaculties([FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await referenceDataService.GetFacultiesPagedAsync(pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> CreateFaculty(CreateFacultyRequestDto request, CancellationToken cancellationToken)
    {
        var result = await referenceDataService.CreateFacultyAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}/departments")]
    public async Task<IActionResult> GetDepartments(
        int id, [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await referenceDataService.GetDepartmentsPagedAsync(id, pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:int}/departments")]
    public async Task<IActionResult> CreateDepartment(int id, CreateDepartmentRequestDto request, CancellationToken cancellationToken)
    {
        var result = await referenceDataService.CreateDepartmentAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }
}
