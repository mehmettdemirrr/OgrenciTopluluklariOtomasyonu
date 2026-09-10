using Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

/// <summary>
/// docs/PLAN-V2.md · Faz 14 (A-42): projedeki TEK çok-action'lı [AllowAnonymous] dosyası —
/// anonim yüzeyin tamamı burada toplanır, `git diff` ile denetlenebilir. Yeni bir anonim uç
/// eklemek bu dosyayı değiştirmeyi gerektirir (kaza sonucu genişleme olmaz).
/// </summary>
[ApiController]
[Route("api/public")]
[AllowAnonymous]
public sealed class PublicContentController(IPublicContentService publicContentService) : ControllerBase
{
    [HttpGet("clubs")]
    public async Task<IActionResult> GetClubs(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] string? letter = null,
        CancellationToken cancellationToken = default)
    {
        var result = await publicContentService.GetClubsAsync(pageIndex, pageSize, search, categoryId, letter, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("clubs/{id:int}")]
    public async Task<IActionResult> GetClubById(int id, CancellationToken cancellationToken)
    {
        var result = await publicContentService.GetClubByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>docs/MIMARI.md · A-82: sayaç ucu; gövde almaz, oturum başına bir kez çağrılır.</summary>
    [HttpPost("clubs/{id:int}/view")]
    public async Task<IActionResult> RegisterClubView(int id, CancellationToken cancellationToken)
    {
        var result = await publicContentService.RegisterClubViewAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents(
        [FromQuery] int? clubId = null,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await publicContentService.GetEventsAsync(clubId, pageIndex, pageSize, search, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("calendar-events")]
    public async Task<IActionResult> GetCalendarEvents(
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var result = await publicContentService.GetCalendarEventsAsync(fromUtc, toUtc, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("events/{id:int}")]
    public async Task<IActionResult> GetEventById(int id, CancellationToken cancellationToken)
    {
        var result = await publicContentService.GetEventByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>docs/MIMARI.md · A-77: sayaç ucu; gövde almaz, oturum başına bir kez çağrılır.</summary>
    [HttpPost("events/{id:int}/view")]
    public async Task<IActionResult> RegisterEventView(int id, CancellationToken cancellationToken)
    {
        var result = await publicContentService.RegisterEventViewAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("announcements")]
    public async Task<IActionResult> GetAnnouncements(
        [FromQuery] int? clubId = null,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await publicContentService.GetAnnouncementsAsync(clubId, pageIndex, pageSize, search, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("announcements/{id:int}")]
    public async Task<IActionResult> GetAnnouncementById(int id, CancellationToken cancellationToken)
    {
        var result = await publicContentService.GetAnnouncementByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken = default)
    {
        var result = await publicContentService.GetStatsAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("club-categories")]
    public async Task<IActionResult> GetClubCategories(CancellationToken cancellationToken = default)
    {
        var result = await publicContentService.GetClubCategoriesAsync(cancellationToken);
        return result.ToActionResult();
    }
}
