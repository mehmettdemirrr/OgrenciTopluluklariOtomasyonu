using Business.Abstract;
using Business.Constants;
using Business.DTOs.Events;
using Core.DataAccess;
using Core.Utilities.Results;
using Core.Utilities.Security;
using Core.Utilities.Time;
using DataAccess.Seed;
using Entities;
using Entities.Enums;

namespace Business.Concrete;

/// <summary>docs/PLAN-V2.md §10: EventParticipation'ın ilk gerçek kullanımı.</summary>
public sealed class EventParticipationManager(
    IEntityRepository<Event> eventRepository,
    IEntityRepository<EventParticipation> participationRepository,
    IEntityRepository<Club> clubRepository,
    IEntityRepository<Student> studentRepository,
    IEntityRepository<AcademicStaff> academicStaffRepository,
    IEntityRepository<ClubMembership> clubMembershipRepository,
    IEntityRepository<AcademicTerm> academicTermRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IEventParticipationService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<IResult> RegisterAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var student = await GetCurrentStudentAsync(cancellationToken).ConfigureAwait(false);
        if (student is null)
        {
            return Result.Forbidden(Messages.NotAStudent);
        }

        var @event = await eventRepository.GetAsync(e => e.Id == eventId, cancellationToken).ConfigureAwait(false);
        if (@event is null)
        {
            return Result.NotFound(Messages.EventNotFound);
        }

        if (@event.Status != EventStatus.Published || @event.StartDateUtc <= clock.UtcNow)
        {
            return Result.Conflict(Messages.EventNotOpenForRegistration);
        }

        var existing = await participationRepository
            .GetAsync(p => p.EventId == eventId && p.StudentId == student.Id, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return Result.Conflict(Messages.AlreadyRegisteredForEvent);
        }

        if (@event.Capacity is { } capacity)
        {
            var activeCount = (await participationRepository.GetListAsync(p => p.EventId == eventId, cancellationToken).ConfigureAwait(false)).Count;
            if (activeCount >= capacity)
            {
                return Result.Conflict(Messages.EventCapacityFull);
            }
        }

        var participation = new EventParticipation { EventId = eventId, StudentId = student.Id, RegisteredAtUtc = clock.UtcNow };
        await participationRepository.AddAsync(participation, cancellationToken).ConfigureAwait(false);

        // A-38/Y-53: kontenjan yalnızca yukarıdaki sayımla korunmaz — Event'i de Modified işaretleyip
        // RowVersion'ı tüketiyoruz ki eşzamanlı iki kayıttan biri DB'de kesin olarak kaybetsin.
        eventRepository.Update(@event);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Conflict(Messages.EventCapacityFull);
        }

        return Result.Success(Messages.EventRegistered);
    }

    public async Task<IResult> CancelAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var student = await GetCurrentStudentAsync(cancellationToken).ConfigureAwait(false);
        if (student is null)
        {
            return Result.Forbidden(Messages.NotAStudent);
        }

        var participation = await participationRepository
            .GetAsync(p => p.EventId == eventId && p.StudentId == student.Id, cancellationToken)
            .ConfigureAwait(false);
        if (participation is null)
        {
            return Result.NotFound(Messages.NotRegisteredForEvent);
        }

        // Y-16: soft delete + audit interceptor'ın Delete olarak tanıması (Y-44).
        participation.IsDeleted = true;
        participation.DeletedAtUtc = clock.UtcNow;
        participationRepository.Update(participation);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.EventRegistrationCancelled);
    }

    public async Task<IDataResult<PagedResult<EventParticipantListItemDto>>> GetParticipantsAsync(
        int eventId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var @event = await eventRepository.GetAsync(e => e.Id == eventId, cancellationToken).ConfigureAwait(false);
        if (@event is null)
        {
            return DataResult<PagedResult<EventParticipantListItemDto>>.NotFound(Messages.EventNotFound);
        }

        var club = await clubRepository.GetAsync(c => c.Id == @event.ClubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return DataResult<PagedResult<EventParticipantListItemDto>>.NotFound(Messages.ClubNotFound);
        }

        var accessError = await EnsureViewAccessAsync(club, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return DataResult<PagedResult<EventParticipantListItemDto>>.Forbidden(accessError);
        }

        var clampedPageSize = ClampPageSize(pageSize);
        var paged = await participationRepository
            .GetListPagedAsync(pageIndex, clampedPageSize, p => p.EventId == eventId, cancellationToken)
            .ConfigureAwait(false);

        var studentIds = paged.Items.Select(p => p.StudentId).Distinct().ToList();
        var studentNumbers = (await studentRepository.GetListAsync(s => studentIds.Contains(s.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(s => s.Id, s => s.StudentNumber);

        var items = paged.Items.Select(p => new EventParticipantListItemDto
        {
            StudentId = p.StudentId,
            StudentNumber = studentNumbers.GetValueOrDefault(p.StudentId, string.Empty),
            RegisteredAtUtc = p.RegisteredAtUtc,
        }).ToList();

        var result = new PagedResult<EventParticipantListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize);
        return DataResult<PagedResult<EventParticipantListItemDto>>.Success(result);
    }

    public async Task<IDataResult<PagedResult<EventListItemDto>>> GetMineAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var clampedPageSize = ClampPageSize(pageSize);

        var student = await GetCurrentStudentAsync(cancellationToken).ConfigureAwait(false);
        if (student is null)
        {
            return DataResult<PagedResult<EventListItemDto>>.Success(new PagedResult<EventListItemDto>([], 0, pageIndex, clampedPageSize));
        }

        var eventIds = (await participationRepository.GetListAsync(p => p.StudentId == student.Id, cancellationToken).ConfigureAwait(false))
            .Select(p => p.EventId)
            .ToList();

        var paged = await eventRepository
            .GetListPagedAsync(pageIndex, clampedPageSize, e => eventIds.Contains(e.Id), cancellationToken)
            .ConfigureAwait(false);

        var clubIds = paged.Items.Select(e => e.ClubId).Distinct().ToList();
        var clubNames = (await clubRepository.GetListAsync(c => clubIds.Contains(c.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(c => c.Id, c => c.Name);

        var items = paged.Items.Select(e => new EventListItemDto
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
            CancellationReason = e.CancellationReason,
        }).ToList();

        var result = new PagedResult<EventListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize);
        return DataResult<PagedResult<EventListItemDto>>.Success(result);
    }

    private async Task<Student?> GetCurrentStudentAsync(CancellationToken cancellationToken) =>
        currentUser.UserId is { } userId
            ? await studentRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false)
            : null;

    // Y-23: events.write izni yeterli değil — EventManager.EnsureClubWriteAccessAsync ile aynı desen,
    // ek olarak ClubMemberManager.EnsureMemberViewAccessAsync'teki reports.read.all blanket bypass'ı taşır.
    private async Task<string?> EnsureViewAccessAsync(Club club, CancellationToken cancellationToken)
    {
        if (currentUser.Permissions.Contains(IdentitySeedData.Permissions.ReportsReadAll))
        {
            return null;
        }

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
