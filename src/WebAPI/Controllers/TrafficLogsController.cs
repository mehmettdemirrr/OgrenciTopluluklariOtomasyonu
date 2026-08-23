using Business.Abstract;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

/// <summary>docs/PLAN-V3.md · K-28/A-44: erişim izi okuma ucu — yazma ucu yok, yalnızca RequestLoggingMiddleware yazar.</summary>
[ApiController]
[Route("api/traffic-logs")]
public sealed class TrafficLogsController(ITrafficLogService trafficLogService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int? userId = null,
        [FromQuery] string? ipAddress = null,
        [FromQuery] string? httpMethod = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await trafficLogService.GetPagedAsync(userId, ipAddress, httpMethod, correlationId, fromUtc, toUtc, pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }
}
