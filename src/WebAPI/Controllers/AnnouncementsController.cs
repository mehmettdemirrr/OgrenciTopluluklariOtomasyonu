using Business.Abstract;
using Business.DTOs.Announcements;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api")]
public sealed class AnnouncementsController(IAnnouncementService announcementService) : ControllerBase
{
    [HttpGet("clubs/{clubId:int}/announcements")]
    public async Task<IActionResult> GetForClub(int clubId, [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await announcementService.GetForClubAsync(clubId, pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("announcements")]
    public async Task<IActionResult> GetFeed(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await announcementService.GetFeedAsync(pageIndex, pageSize, search, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("clubs/{clubId:int}/announcements")]
    public async Task<IActionResult> Create(int clubId, CreateAnnouncementRequestDto request, CancellationToken cancellationToken)
    {
        var result = await announcementService.CreateAsync(clubId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("announcements/{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await announcementService.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("announcements/{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateAnnouncementRequestDto request, CancellationToken cancellationToken)
    {
        var result = await announcementService.UpdateAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("announcements/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await announcementService.DeleteAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("announcements")]
    public async Task<IActionResult> CreateGlobal(CreateAnnouncementRequestDto request, CancellationToken cancellationToken)
    {
        var result = await announcementService.CreateGlobalAsync(request, cancellationToken);
        return result.ToActionResult();
    }
}
