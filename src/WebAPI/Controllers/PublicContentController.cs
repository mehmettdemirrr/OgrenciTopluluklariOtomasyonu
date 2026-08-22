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
    public async Task<IActionResult> GetClubs([FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await publicContentService.GetClubsAsync(pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("clubs/{id:int}")]
    public async Task<IActionResult> GetClubById(int id, CancellationToken cancellationToken)
    {
        var result = await publicContentService.GetClubByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents(
        [FromQuery] int? clubId = null, [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await publicContentService.GetEventsAsync(clubId, pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("announcements")]
    public async Task<IActionResult> GetAnnouncements(
        [FromQuery] int? clubId = null, [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await publicContentService.GetAnnouncementsAsync(clubId, pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }
}
