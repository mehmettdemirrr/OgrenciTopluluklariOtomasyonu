using Business.Abstract;
using Business.DTOs.Memberships;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

/// <summary>docs/MIMARI.md · sessiz onay: kaynak odaklı REST, kebab-case, fiil yok.</summary>
[ApiController]
[Route("api")]
public sealed class MembershipApplicationsController(IMembershipApplicationService membershipApplicationService) : ControllerBase
{
    [HttpPost("clubs/{clubId:int}/membership-applications")]
    public async Task<IActionResult> Apply(int clubId, CancellationToken cancellationToken)
    {
        var result = await membershipApplicationService.ApplyAsync(new ApplyForMembershipRequestDto { ClubId = clubId }, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>docs/PLAN-V4.md §19.1 (A-48): çağıranın kendi başvuruları — `club-applications/mine` ile simetrik.</summary>
    [HttpGet("membership-applications/mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await membershipApplicationService.GetMineAsync(cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>docs/PLAN-V4.md §19.1: öğrenci kendi bekleyen başvurusunu geri çeker.</summary>
    [HttpDelete("membership-applications/{id:int}")]
    public async Task<IActionResult> Withdraw(int id, CancellationToken cancellationToken)
    {
        var result = await membershipApplicationService.WithdrawAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Çağıranın danışmanı olduğu kulüplerdeki bekleyen başvurular (Y-23: sorgu zaten scoped).</summary>
    [HttpGet("membership-applications")]
    public async Task<IActionResult> GetPending([FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await membershipApplicationService.GetPendingForAdvisorAsync(pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("membership-applications/{id:int}/decision")]
    public async Task<IActionResult> Review(int id, ReviewMembershipApplicationRequestDto request, CancellationToken cancellationToken)
    {
        var result = await membershipApplicationService.ReviewAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }
}
