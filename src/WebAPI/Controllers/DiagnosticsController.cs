using Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/diagnostics")]
public sealed class DiagnosticsController(IDiagnosticsService diagnosticsService) : ControllerBase
{
    [HttpGet("ping")]
    [AllowAnonymous]
    public async Task<IActionResult> Ping()
    {
        var result = await diagnosticsService.PingAsync();
        return result.ToActionResult();
    }

    /// <summary>docs/MIMARI.md · Faz 3 "bitti sayılır": izinsiz uçta 403.</summary>
    [HttpGet("secure-ping")]
    public async Task<IActionResult> SecurePing()
    {
        var result = await diagnosticsService.SecurePingAsync();
        return result.ToActionResult();
    }
}
