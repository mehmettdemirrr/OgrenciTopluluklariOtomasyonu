using Business.Abstract;
using Business.DTOs.Auth;
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

    /// <summary>docs/PLAN-V4.md §19.1 (A-48): öğrencinin kendi bölüm/kayıt yılı düzeltmesi.</summary>
    [HttpPut]
    public async Task<IActionResult> Update(UpdateMeRequestDto request, CancellationToken cancellationToken)
    {
        var result = await accountService.UpdateMeAsync(request, cancellationToken);
        return result.ToActionResult();
    }
}
