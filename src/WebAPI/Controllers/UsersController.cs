using Business.Abstract;
using Business.DTOs.Admin;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(IRoleAdminService roleAdminService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var result = await roleAdminService.GetUsersPagedAsync(pageIndex, pageSize, search, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{id:int}/roles")]
    public async Task<IActionResult> SetUserRoles(int id, SetUserRolesRequestDto request, CancellationToken cancellationToken)
    {
        var result = await roleAdminService.SetUserRolesAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }
}
