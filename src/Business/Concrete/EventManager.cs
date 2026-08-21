using Business.Abstract;
using Business.Constants;
using Business.DTOs.Events;
using Core.DataAccess;
using Core.Utilities.Results;
using Core.Utilities.Security;
using Core.Utilities.Time;
using Entities;
using Entities.Enums;

namespace Business.Concrete;

public sealed class EventManager(
    IEntityRepository<Event> eventRepository,
    IEntityRepository<Club> clubRepository,
    IEntityRepository<AcademicStaff> academicStaffRepository,
    IEntityRepository<Student> studentRepository,
    IEntityRepository<ClubMembership> clubMembershipRepository,
    IEntityRepository<AcademicTerm> academicTermRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IEventService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<IDataResult<int>> CreateAsync(int clubId, CreateEventRequestDto request, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return DataResult<int>.NotFound(Messages.ClubNotFound);
        }

        var accessError = await EnsureClubWriteAccessAsync(club, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return DataResult<int>.Forbidden(accessError);
        }

        var @event = new Event
        {
            ClubId = clubId,
            Title = request.Title,
            Description = request.Description,
            Location = request.Location,
            StartDateUtc = request.StartDateUtc,
            EndDateUtc = request.EndDateUtc,
            Capacity = request.Capacity,
            Status = EventStatus.Draft,
            CreatedAtUtc = clock.UtcNow,
        };

        await eventRepository.AddAsync(@event, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return DataResult<int>.Success(@event.Id);
    }

    public async Task<IResult> SubmitForApprovalAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var @event = await eventRepository.GetAsync(e => e.Id == eventId, cancellationToken).ConfigureAwait(false);
        if (@event is null)
        {
            return Result.NotFound(Messages.EventNotFound);
        }

        var club = await clubRepository.GetAsync(c => c.Id == @event.ClubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return Result.NotFound(Messages.ClubNotFound);
        }

        var accessError = await EnsureClubWriteAccessAsync(club, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return Result.Forbidden(accessError);
        }

        if (@event.Status != EventStatus.Draft)
        {
            return Result.Conflict(Messages.EventNotInDraft);
        }

        @event.Status = EventStatus.PendingApproval;
        eventRepository.Update(@event);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.EventSubmitted);
    }

    public async Task<IResult> DecideAsync(int eventId, DecideEventRequestDto request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Forbidden(Messages.NotClubAdvisor);
        }

        var @event = await eventRepository.GetAsync(e => e.Id == eventId, cancellationToken).ConfigureAwait(false);
        if (@event is null)
        {
            return Result.NotFound(Messages.EventNotFound);
        }

        if (@event.Status != EventStatus.PendingApproval)
        {
            return Result.Conflict(Messages.EventNotPendingApproval);
        }

        var club = await clubRepository.GetAsync(c => c.Id == @event.ClubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return Result.NotFound(Messages.ClubNotFound);
        }

        // Y-23: events.approve izni yeterli değil — yalnızca kulübün danışmanı karar verebilir
        // (MembershipApplicationManager.ReviewAsync ile aynı precedent: Admin'in blanket izni bile bunu bypass etmez).
        var advisor = await academicStaffRepository.GetAsync(s => s.Id == club.AdvisorId, cancellationToken).ConfigureAwait(false);
        if (advisor is null || advisor.ApplicationUserId != userId)
        {
            return Result.Forbidden(Messages.NotClubAdvisor);
        }

        @event.Status = request.Status;
        eventRepository.Update(@event);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(request.Status == EventStatus.Published ? Messages.EventPublished : Messages.EventRejected);
    }

    public async Task<IDataResult<PagedResult<EventListItemDto>>> GetApprovalQueueAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var clampedPageSize = ClampPageSize(pageSize);

        var advisor = currentUser.UserId is { } userId
            ? await academicStaffRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false)
            : null;

        if (advisor is null)
        {
            return DataResult<PagedResult<EventListItemDto>>.Success(new PagedResult<EventListItemDto>([], 0, pageIndex, clampedPageSize));
        }

        var clubIds = (await clubRepository.GetListAsync(c => c.AdvisorId == advisor.Id, cancellationToken).ConfigureAwait(false))
            .Select(c => c.Id)
            .ToList();

        var paged = await eventRepository
            .GetListPagedAsync(pageIndex, clampedPageSize, e => clubIds.Contains(e.ClubId) && e.Status == EventStatus.PendingApproval, cancellationToken)
            .ConfigureAwait(false);

        var items = await MapWithClubNamesAsync(paged, clubIds, cancellationToken).ConfigureAwait(false);
        return DataResult<PagedResult<EventListItemDto>>.Success(new PagedResult<EventListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    public async Task<IDataResult<PagedResult<EventListItemDto>>> GetPublishedAsync(
        int clubId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var paged = await eventRepository
            .GetListPagedAsync(pageIndex, ClampPageSize(pageSize), e => e.ClubId == clubId && e.Status == EventStatus.Published, cancellationToken)
            .ConfigureAwait(false);

        var items = await MapWithClubNamesAsync(paged, [clubId], cancellationToken).ConfigureAwait(false);
        return DataResult<PagedResult<EventListItemDto>>.Success(new PagedResult<EventListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    private async Task<List<EventListItemDto>> MapWithClubNamesAsync(PagedResult<Event> paged, List<int> clubIds, CancellationToken cancellationToken)
    {
        var clubNames = (await clubRepository.GetListAsync(c => clubIds.Contains(c.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(c => c.Id, c => c.Name);

        return paged.Items.Select(e => new EventListItemDto
        {
            Id = e.Id,
            ClubId = e.ClubId,
            ClubName = clubNames.GetValueOrDefault(e.ClubId, string.Empty),
            Title = e.Title,
            Description = e.Description,
            Location = e.Location,
            StartDateUtc = e.StartDateUtc,
            EndDateUtc = e.EndDateUtc,
            Capacity = e.Capacity,
            Status = e.Status,
        }).ToList();
    }

    // Y-23: izin claim'i (events.write) yeterli değil — yalnızca kulübün danışmanı VEYA güncel
    // dönemde bu kulüpte Officer/President üyeliği olan kişi etkinlik oluşturabilir/gönderebilir.
    private async Task<string?> EnsureClubWriteAccessAsync(Club club, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Messages.NotClubAdvisorOrOfficer;
        }

        var advisor = await academicStaffRepository.GetAsync(s => s.Id == club.AdvisorId, cancellationToken).ConfigureAwait(false);
        if (advisor is not null && advisor.ApplicationUserId == userId)
        {
            return null;
        }

        var term = await academicTermRepository.GetAsync(t => t.IsCurrent, cancellationToken).ConfigureAwait(false);
        if (term is null)
        {
            return Messages.NotClubAdvisorOrOfficer;
        }

        var student = await studentRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false);
        if (student is null)
        {
            return Messages.NotClubAdvisorOrOfficer;
        }

        var membership = await clubMembershipRepository
            .GetAsync(m => m.ClubId == club.Id && m.StudentId == student.Id && m.AcademicTermId == term.Id, cancellationToken)
            .ConfigureAwait(false);

        return membership is not null && membership.ClubRole is ClubRole.Officer or ClubRole.President
            ? null
            : Messages.NotClubAdvisorOrOfficer;
    }

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
