using Business.Abstract;
using Business.DTOs.Admin;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api")]
public sealed class RolesController(IRoleAdminService roleAdminService) : ControllerBase
{
    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissionCatalog(CancellationToken cancellationToken)
    {
        var result = await roleAdminService.GetPermissionCatalogAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles([FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await roleAdminService.GetRolesPagedAsync(pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole(CreateRoleRequestDto request, CancellationToken cancellationToken)
    {
        var result = await roleAdminService.CreateRoleAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("roles/{id:int}")]
    public async Task<IActionResult> DeleteRole(int id, CancellationToken cancellationToken)
    {
        var result = await roleAdminService.DeleteRoleAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("roles/{id:int}/permissions")]
    public async Task<IActionResult> SetRolePermissions(int id, SetRolePermissionsRequestDto request, CancellationToken cancellationToken)
    {
        var result = await roleAdminService.SetRolePermissionsAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }
}
