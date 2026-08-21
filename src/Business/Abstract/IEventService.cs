using Business.DTOs.Events;
using Business.ValidationRules;
using Core.Aspects.Autofac;
using Core.DataAccess;
using Core.Utilities.Results;
using DataAccess.Seed;

namespace Business.Abstract;

/// <summary>docs/MIMARI.md · A-25: Draft → PendingApproval → Published|Rejected. Minimal oluşturma + onay kuyruğu (tam CRUD değil).</summary>
public interface IEventService
{
    /// <summary>Y-23: events.write yeterli değil — yalnızca kulübün danışmanı VEYA güncel dönemde Officer/President üyesi oluşturabilir.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.EventsWrite)]
    [ValidationAspect(typeof(CreateEventRequestValidator))]
    [TransactionAspect]
    Task<IDataResult<int>> CreateAsync(int clubId, CreateEventRequestDto request, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.EventsWrite)]
    [TransactionAspect]
    Task<IResult> SubmitForApprovalAsync(int eventId, CancellationToken cancellationToken = default);

    /// <summary>Y-23: events.approve yeterli değil — yalnızca kulübün danışmanı karar verebilir (MembershipApplicationManager.ReviewAsync precedent'i).</summary>
    [SecuredOperation(IdentitySeedData.Permissions.EventsApprove)]
    [ValidationAspect(typeof(DecideEventRequestValidator))]
    [TransactionAspect]
    Task<IResult> DecideAsync(int eventId, DecideEventRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Çağıranın danışmanı olduğu kulüplerdeki PendingApproval etkinlikler (sorgu zaten scoped).</summary>
    [SecuredOperation(IdentitySeedData.Permissions.EventsApprove)]
    Task<IDataResult<PagedResult<EventListItemDto>>> GetApprovalQueueAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.EventsRead)]
    Task<IDataResult<PagedResult<EventListItemDto>>> GetPublishedAsync(int clubId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}
