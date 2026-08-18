using Business.Abstract;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/diagnostics")]
public sealed class DiagnosticsController(IDiagnosticsService diagnosticsService) : ControllerBase
{
    [HttpGet("ping")]
    public async Task<IActionResult> Ping()
    {
        var result = await diagnosticsService.PingAsync();
        return result.ToActionResult();
    }
}
