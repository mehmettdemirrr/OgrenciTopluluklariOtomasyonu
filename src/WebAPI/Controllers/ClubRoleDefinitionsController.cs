using Business.Abstract;
using Business.DTOs.Clubs;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

/// <summary>docs/MIMARI.md · K-36/O-20: rol tanımları kulübe özeldir — rota kulübün altındadır.</summary>
[ApiController]
[Route("api/clubs/{clubId:int}/role-definitions")]
public sealed class ClubRoleDefinitionsController(IClubMemberService clubMemberService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRoleDefinitions(int clubId, CancellationToken cancellationToken)
    {
        var result = await clubMemberService.GetRoleDefinitionsAsync(clubId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoleDefinition(int clubId, CreateClubRoleDefinitionRequestDto request, CancellationToken cancellationToken)
    {
        var result = await clubMemberService.CreateRoleDefinitionAsync(clubId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{definitionId:int}")]
    public async Task<IActionResult> UpdateRoleDefinition(
        int clubId, int definitionId, UpdateClubRoleDefinitionRequestDto request, CancellationToken cancellationToken)
    {
        var result = await clubMemberService.UpdateRoleDefinitionAsync(clubId, definitionId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{definitionId:int}")]
    public async Task<IActionResult> DeleteRoleDefinition(int clubId, int definitionId, CancellationToken cancellationToken)
    {
        var result = await clubMemberService.DeleteRoleDefinitionAsync(clubId, definitionId, cancellationToken);
        return result.ToActionResult();
    }
}
