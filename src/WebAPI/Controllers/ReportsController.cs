using Business.Abstract;
using Business.DTOs.Reports;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

/// <summary>docs/MIMARI.md · sessiz onay: kaynak odaklı REST, kebab-case, fiil yok.</summary>
[ApiController]
[Route("api/reports")]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await reportService.GetTermSummaryAsync(cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Çağıranın kendi taleplerinin listesi (Business zaten RequestedByUserId ile sınırlar).</summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await reportService.GetListPagedAsync(pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateReportRequestDto request, CancellationToken cancellationToken)
    {
        var result = await reportService.RequestAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>docs/MIMARI.md · Y-51: sahiplik + durum + kapsam burada YENİDEN kontrol edilir.</summary>
    [HttpGet("{id:int}/file")]
    public async Task<IActionResult> DownloadFile(int id, CancellationToken cancellationToken)
    {
        var result = await reportService.DownloadAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ToActionResult();
        }

        return File(result.Data.Content, result.Data.ContentType, result.Data.DownloadFileName);
    }
}
