using Business.Abstract;
using Business.DTOs.Clubs;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/clubs/{clubId:int}/members")]
public sealed class ClubMembersController(IClubMemberService clubMemberService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMembers(
        int clubId, [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await clubMemberService.GetMembersPagedAsync(clubId, pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{membershipId:int}/role")]
    public async Task<IActionResult> SetRole(int clubId, int membershipId, SetClubRoleRequestDto request, CancellationToken cancellationToken)
    {
        var result = await clubMemberService.SetRoleAsync(clubId, membershipId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{membershipId:int}")]
    public async Task<IActionResult> RemoveMember(int clubId, int membershipId, CancellationToken cancellationToken)
    {
        var result = await clubMemberService.RemoveMemberAsync(clubId, membershipId, cancellationToken);
        return result.ToActionResult();
    }
}
