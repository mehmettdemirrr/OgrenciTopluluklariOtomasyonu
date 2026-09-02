using Business.Abstract;
using Business.BackgroundJobs;
using Business.Constants;
using Business.DTOs.Events;
using Core.DataAccess;
using Core.Utilities.Results;
using Core.Utilities.Security;
using Core.Utilities.Time;
using DataAccess.Seed;
using Entities;
using Entities.Enums;
using Hangfire;

namespace Business.Concrete;

public sealed class EventManager(
    IEntityRepository<Event> eventRepository,
    IEntityRepository<Club> clubRepository,
    IEntityRepository<AcademicStaff> academicStaffRepository,
    IEntityRepository<Student> studentRepository,
    IEntityRepository<EventParticipation> participationRepository,
    IEntityRepository<ClubMembership> clubMembershipRepository,
    IEntityRepository<AcademicTerm> academicTermRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    IBackgroundJobClient backgroundJobClient) : IEventService
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
            Audience = request.Audience,
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

        // Y-74/A-67: yönetici kontrolü İLK sırada. A-67 bu metodun eski "yalnızca danışman"
        // kuralını yönetici için açıkça gevşetir — danışmanı ulaşılamayan bir kulübün onay kuyruğu
        // aksi hâlde kimse tarafından açılamıyordu. Danışmanın kendi yetkisi aynen duruyor.
        if (!currentUser.Permissions.Contains(IdentitySeedData.Permissions.ClubsManageAll))
        {
            // Y-23: events.approve izni tek başına yeterli değil — kulübün danışmanı olmak gerekir.
            var advisor = await academicStaffRepository.GetAsync(s => s.Id == club.AdvisorId, cancellationToken).ConfigureAwait(false);
            if (advisor is null || advisor.ApplicationUserId != userId)
            {
                return Result.Forbidden(Messages.NotClubAdvisor);
            }
        }

        // Y-46/Y-06: [TransactionAspect] kasıtlı olarak kullanılmaz — commit'ten SONRA Hangfire'a
        // kuyruğa ekleme yapılabilmesi için transaction burada elle yönetilir (ReviewAsync precedent'i).
        var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            try
            {
                @event.Status = request.Status;
                eventRepository.Update(@event);
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        // Y-46: kuyruğa ekleme yalnızca commit'ten sonra — worker henüz var olmayan durumu okumasın.
        backgroundJobClient.Enqueue<EventDecisionNotificationJob>(job => job.SendAsync(@event.Id));

        return Result.Success(request.Status == EventStatus.Published ? Messages.EventPublished : Messages.EventRejected);
    }

    public async Task<IResult> CancelAsync(int eventId, CancelEventRequestDto request, CancellationToken cancellationToken = default)
    {
        var @event = await eventRepository.GetAsync(e => e.Id == eventId, cancellationToken).ConfigureAwait(false);
        if (@event is null)
        {
            return Result.NotFound(Messages.EventNotFound);
        }

        // A-49: yalnızca yayındaki etkinlik iptal edilir. Draft/Rejected zaten kimseye görünmedi
        // (silinebilirler); PendingApproval için karar mekanizması ayrı (reddet).
        if (@event.Status != EventStatus.Published)
        {
            return Result.Conflict(Messages.OnlyPublishedEventsCanBeCancelled);
        }

        var club = await clubRepository.GetAsync(c => c.Id == @event.ClubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return Result.NotFound(Messages.ClubNotFound);
        }

        // Y-23: events.write izni yeterli değil — danışman ya da kulübün Officer/President'i.
        var accessError = await EnsureClubWriteAccessAsync(club, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return Result.Forbidden(accessError);
        }

        // Y-46/Y-06: [TransactionAspect] kasıtlı olarak kullanılmaz — commit'ten SONRA katılımcı
        // bildirimi kuyruğa eklenebilsin diye transaction elle yönetilir (DecideAsync precedent'i).
        var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            try
            {
                // Y-61: katılımcı kayıtlarına DOKUNULMAZ — öğrenci kaydını iptal rozetiyle görmeye
                // devam etmeli. Kaydın kaybolması "ben kaydolmamış mıydım?" sorusunu doğurur.
                @event.Status = EventStatus.Cancelled;
                @event.CancellationReason = request.CancellationReason.Trim();
                eventRepository.Update(@event);

                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        // Y-46: kuyruğa ekleme yalnızca commit'ten sonra — worker henüz yazılmamış durumu okumasın.
        backgroundJobClient.Enqueue<EventCancellationNotificationJob>(job => job.SendAsync(@event.Id));

        return Result.Success(Messages.EventCancelled);
    }

    public async Task<IDataResult<PagedResult<EventListItemDto>>> GetApprovalQueueAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var clampedPageSize = ClampPageSize(pageSize);

        // Y-74/A-67: yönetici kontrolü kapsam daraltmasının İLK satırıdır. v6.0'a kadar burada
        // yönetici yolu yoktu: admin'in AcademicStaff kaydı olmadığı için kuyruk SESSİZCE boş
        // dönüyordu ve sekme hiç dolmuyordu. Y-66 aynı şeyi söylüyordu ama ScopeGuardTests
        // metotları ada göre tarıyordu; bu metot adı Ensure* olmadığı için kör noktada kaldı.
        var isAdmin = currentUser.Permissions.Contains(IdentitySeedData.Permissions.ClubsManageAll);

        List<int> advisedClubIds = [];
        if (!isAdmin)
        {
            var advisor = currentUser.UserId is { } userId
                ? await academicStaffRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false)
                : null;

            if (advisor is null)
            {
                return DataResult<PagedResult<EventListItemDto>>.Success(new PagedResult<EventListItemDto>([], 0, pageIndex, clampedPageSize));
            }

            advisedClubIds = (await clubRepository.GetListAsync(c => c.AdvisorId == advisor.Id, cancellationToken).ConfigureAwait(false))
                .Select(c => c.Id)
                .ToList();
        }

        var paged = await eventRepository
            .GetListPagedAsync(
                pageIndex,
                clampedPageSize,
                e => (isAdmin || advisedClubIds.Contains(e.ClubId)) && e.Status == EventStatus.PendingApproval,
                cancellationToken)
            .ConfigureAwait(false);

        // Kulüp adları sayfanın KENDİ kulüplerinden çözülür: yöneticide advisedClubIds boştur,
        // eski kod o listeyi kullandığı için ad sütunu boş kalırdı.
        var pageClubIds = paged.Items.Select(e => e.ClubId).Distinct().ToList();
        var items = await MapWithClubNamesAsync(paged, pageClubIds, cancellationToken).ConfigureAwait(false);
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

    public async Task<IDataResult<PagedResult<EventListItemDto>>> GetForClubAsync(int clubId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return DataResult<PagedResult<EventListItemDto>>.NotFound(Messages.ClubNotFound);
        }

        var accessError = await EnsureClubWriteAccessAsync(club, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return DataResult<PagedResult<EventListItemDto>>.Forbidden(accessError);
        }

        var paged = await eventRepository
            .GetListPagedAsync(pageIndex, ClampPageSize(pageSize), e => e.ClubId == clubId, cancellationToken)
            .ConfigureAwait(false);

        var items = await MapWithClubNamesAsync(paged, [clubId], cancellationToken).ConfigureAwait(false);
        return DataResult<PagedResult<EventListItemDto>>.Success(new PagedResult<EventListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    public async Task<IDataResult<EventListItemDto>> GetByIdAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var @event = await eventRepository.GetAsync(e => e.Id == eventId, cancellationToken).ConfigureAwait(false);
        if (@event is null)
        {
            return DataResult<EventListItemDto>.NotFound(Messages.EventNotFound);
        }

        var club = await clubRepository.GetAsync(c => c.Id == @event.ClubId, cancellationToken).ConfigureAwait(false);
        return DataResult<EventListItemDto>.Success(MapToDto(@event, club?.Name ?? string.Empty));
    }

    public async Task<IDataResult<PagedResult<EventListItemDto>>> GetUpcomingAsync(
        int pageIndex, int pageSize, string? search = null, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var clampedPageSize = ClampPageSize(pageSize);
        var term = SearchTerm.Normalize(search);

        // A-50: başlık araması SQL'de; sıra tarihe göre artan — en yakın etkinlik başta (Y-64).
        var paged = await eventRepository
            .GetListPagedAsync(
                pageIndex,
                clampedPageSize,
                e => e.Status == EventStatus.Published && e.StartDateUtc >= now
                    && (term.Length == 0 || e.Title.Contains(term)),
                e => e.StartDateUtc,
                descending: false,
                cancellationToken)
            .ConfigureAwait(false);

        var clubIds = paged.Items.Select(e => e.ClubId).Distinct().ToList();
        var items = await MapWithClubNamesAsync(paged, clubIds, cancellationToken).ConfigureAwait(false);

        // Y-62: "kayıtlı mıyım" bilgisi listeyle birlikte gelir — arayüz bunun için ikinci bir
        // tam liste (`/events/mine?pageSize=200`) çekmek zorunda kalmasın.
        var registeredIds = await GetRegisteredEventIdsAsync(paged.Items.Select(e => e.Id).ToList(), cancellationToken).ConfigureAwait(false);
        foreach (var item in items)
        {
            item.IsRegistered = registeredIds.Contains(item.Id);
        }

        return DataResult<PagedResult<EventListItemDto>>.Success(new PagedResult<EventListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    private async Task<HashSet<int>> GetRegisteredEventIdsAsync(List<int> eventIds, CancellationToken cancellationToken)
    {
        // Y-22: öğrenci kimliği token'dan çözülür, istemciden gelmez.
        if (eventIds.Count == 0 || currentUser.UserId is not { } userId)
        {
            return [];
        }

        var student = await studentRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false);
        if (student is null)
        {
            return [];
        }

        var participations = await participationRepository
            .GetListAsync(p => p.StudentId == student.Id && eventIds.Contains(p.EventId), cancellationToken)
            .ConfigureAwait(false);

        return participations.Select(p => p.EventId).ToHashSet();
    }

    public async Task<IResult> UpdateAsync(int eventId, UpdateEventRequestDto request, CancellationToken cancellationToken = default)
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

        if (@event.Status is not (EventStatus.Draft or EventStatus.Rejected))
        {
            return Result.Conflict(Messages.EventCannotBeUpdated);
        }

        @event.Title = request.Title;
        @event.Description = request.Description;
        @event.Location = request.Location;
        @event.StartDateUtc = request.StartDateUtc;
        @event.EndDateUtc = request.EndDateUtc;
        @event.Capacity = request.Capacity;
        @event.Audience = request.Audience;
        eventRepository.Update(@event);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.EventUpdated);
    }

    public async Task<IResult> DeleteAsync(int eventId, CancellationToken cancellationToken = default)
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
            return Result.Conflict(Messages.EventCannotBeDeleted);
        }

        // Y-16: soft delete + audit interceptor'ın Delete olarak tanıması (Y-44).
        @event.IsDeleted = true;
        @event.DeletedAtUtc = clock.UtcNow;
        eventRepository.Update(@event);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.EventDeleted);
    }

    private static EventListItemDto MapToDto(Event e, string clubName) => new()
    {
        Id = e.Id,
        ClubId = e.ClubId,
        ClubName = clubName,
        Title = e.Title,
        Description = e.Description,
        Location = e.Location,
        StartDateUtc = e.StartDateUtc,
        EndDateUtc = e.EndDateUtc,
        Capacity = e.Capacity,
        Status = e.Status,
        Audience = e.Audience,
        CancellationReason = e.CancellationReason,
        PosterFileId = e.PosterFileId,
    };

    private async Task<List<EventListItemDto>> MapWithClubNamesAsync(PagedResult<Event> paged, List<int> clubIds, CancellationToken cancellationToken)
    {
        var clubNames = (await clubRepository.GetListAsync(c => clubIds.Contains(c.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(c => c.Id, c => c.Name);

        return paged.Items.Select(e => MapToDto(e, clubNames.GetValueOrDefault(e.ClubId, string.Empty))).ToList();
    }

    // Y-23: izin claim'i (events.write) yeterli değil — yalnızca kulübün danışmanı VEYA güncel
    // dönemde bu kulüpte Officer/President üyeliği olan kişi etkinlik oluşturabilir/gönderebilir.
    private async Task<string?> EnsureClubWriteAccessAsync(Club club, CancellationToken cancellationToken)
    {
        // Y-66: yönetici kontrolü her kapsam metodunun İLK satırıdır (A-55).
        if (currentUser.Permissions.Contains(IdentitySeedData.Permissions.ClubsManageAll))
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

        // A-68: karar artık makamdan değil KAPASİTEDEN. Y-75: uçtaki [SecuredOperation(events.write)]
        // birinci kapı olarak yerinde — bu ikinci kapı, yalnızca daraltır.
        return membership is not null && membership.Capabilities.HasFlag(ClubCapability.EventsManage)
            ? null
            : Messages.NotClubAdvisorOrOfficer;
    }

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
