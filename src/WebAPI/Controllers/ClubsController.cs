using Business.Abstract;
using Business.DTOs.Clubs;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/clubs")]
public sealed class ClubsController(IClubService clubService, IClubMemberService clubMemberService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await clubService.GetListPagedAsync(pageIndex, pageSize, search, isActive, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await clubMemberService.GetMineAsync(cancellationToken);
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
