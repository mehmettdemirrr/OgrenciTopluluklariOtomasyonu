using Business.Abstract;
using Business.DTOs.Clubs;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/clubs")]
public sealed class ClubsController(IClubService clubService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await clubService.GetListPagedAsync(pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await clubService.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateClubRequestDto request, CancellationToken cancellationToken)
    {
        var result = await clubService.CreateAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateClubRequestDto request, CancellationToken cancellationToken)
    {
        var result = await clubService.UpdateAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, SetClubStatusRequestDto request, CancellationToken cancellationToken)
    {
        var result = await clubService.SetStatusAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }
}
