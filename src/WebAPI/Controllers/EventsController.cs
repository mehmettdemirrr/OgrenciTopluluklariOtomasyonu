using Business.Abstract;
using Business.DTOs.Events;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api")]
public sealed class EventsController(IEventService eventService) : ControllerBase
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
}
