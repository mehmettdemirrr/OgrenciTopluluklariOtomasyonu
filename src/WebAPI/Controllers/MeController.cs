using Business.Abstract;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/me")]
public sealed class MeController(IAccountService accountService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await accountService.GetMeAsync(cancellationToken);
        return result.ToActionResult();
    }
}
