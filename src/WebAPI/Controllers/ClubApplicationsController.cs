using Business.Abstract;
using Business.DTOs.ClubApplications;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

/// <summary>docs/PLAN-V3.md §17: MembershipApplicationsController'ın kulüp kurma karşılığı.</summary>
[ApiController]
[Route("api")]
public sealed class ClubApplicationsController(IClubApplicationService clubApplicationService) : ControllerBase
{
    [HttpPost("club-applications")]
    public async Task<IActionResult> Submit(SubmitClubApplicationRequestDto request, CancellationToken cancellationToken)
    {
        var result = await clubApplicationService.SubmitAsync(request, cancellationToken);
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
