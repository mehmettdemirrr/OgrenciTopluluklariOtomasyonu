using Business.Abstract;
using Business.DTOs.Events;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api")]
public sealed class EventsController(IEventService eventService, IEventParticipationService participationService) : ControllerBase
{
    [HttpPost("clubs/{clubId:int}/events")]
    public async Task<IActionResult> Create(int clubId, CreateEventRequestDto request, CancellationToken cancellationToken)
    {
        var result = await eventService.CreateAsync(clubId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("events/{id:int}/submission")]
    public async Task<IActionResult> Submit(int id, CancellationToken cancellationToken)
    {
        var result = await eventService.SubmitForApprovalAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("events/{id:int}/decision")]
    public async Task<IActionResult> Decide(int id, DecideEventRequestDto request, CancellationToken cancellationToken)
    {
        var result = await eventService.DecideAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("events/approval-queue")]
    public async Task<IActionResult> GetApprovalQueue([FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await eventService.GetApprovalQueueAsync(pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetPublished(
        [FromQuery] int clubId, [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await eventService.GetPublishedAsync(clubId, pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("clubs/{clubId:int}/events")]
    public async Task<IActionResult> GetForClub(int clubId, [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await eventService.GetForClubAsync(clubId, pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("events/upcoming")]
    public async Task<IActionResult> GetUpcoming([FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await eventService.GetUpcomingAsync(pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("events/mine")]
    public async Task<IActionResult> GetMine([FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await participationService.GetMineAsync(pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("events/{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await eventService.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("events/{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateEventRequestDto request, CancellationToken cancellationToken)
    {
        var result = await eventService.UpdateAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>docs/MIMARI.md · A-49/Y-61: yayınlanmış etkinlik silinmez, iptal edilir.</summary>
    [HttpPut("events/{id:int}/cancellation")]
    public async Task<IActionResult> Cancel(int id, CancelEventRequestDto request, CancellationToken cancellationToken)
    {
        var result = await eventService.CancelAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("events/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await eventService.DeleteAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("events/{id:int}/participation")]
    public async Task<IActionResult> Register(int id, CancellationToken cancellationToken)
    {
        var result = await participationService.RegisterAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("events/{id:int}/participation")]
    public async Task<IActionResult> CancelRegistration(int id, CancellationToken cancellationToken)
    {
        var result = await participationService.CancelAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("events/{id:int}/participants")]
    public async Task<IActionResult> GetParticipants(int id, [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await participationService.GetParticipantsAsync(id, pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }
}
