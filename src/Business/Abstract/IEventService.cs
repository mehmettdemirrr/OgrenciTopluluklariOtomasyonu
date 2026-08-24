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

    /// <summary>
    /// docs/PLAN-V2.md · Y-46/Y-06: transaction elle yönetilir (bkz. EventManager) — Hangfire
    /// enqueue'sinin commit'ten sonra çalışabilmesi için [TransactionAspect] kasıtlı olarak kullanılmaz.
    /// Y-23: events.approve yeterli değil — yalnızca kulübün danışmanı karar verebilir (MembershipApplicationManager.ReviewAsync precedent'i).
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.EventsApprove)]
    [ValidationAspect(typeof(DecideEventRequestValidator))]
    [CacheRemoveAspect("PublicContentManager.")]
    Task<IResult> DecideAsync(int eventId, DecideEventRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Çağıranın danışmanı olduğu kulüplerdeki PendingApproval etkinlikler (sorgu zaten scoped).</summary>
    [SecuredOperation(IdentitySeedData.Permissions.EventsApprove)]
    Task<IDataResult<PagedResult<EventListItemDto>>> GetApprovalQueueAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.EventsRead)]
    Task<IDataResult<PagedResult<EventListItemDto>>> GetPublishedAsync(int clubId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/PLAN-V2.md §10: kulüp yönetimi görünümü — Draft/PendingApproval/Rejected dahil TÜM durumlar.
    /// GetPublishedAsync'ten farklı olarak Y-23 kapsamlı: CreateAsync ile aynı kural (danışman veya
    /// güncel dönemde Officer/President), çünkü taslak/reddedilmiş etkinlikler kulüp dışına sızmamalı.
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.EventsWrite)]
    Task<IDataResult<PagedResult<EventListItemDto>>> GetForClubAsync(int clubId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.EventsRead)]
    Task<IDataResult<EventListItemDto>> GetByIdAsync(int eventId, CancellationToken cancellationToken = default);

    /// <summary>docs/PLAN-V2.md §10.2: tüm kulüplerde yayındaki, henüz başlamamış etkinlikler.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.EventsRead)]
    Task<IDataResult<PagedResult<EventListItemDto>>> GetUpcomingAsync(
        int pageIndex, int pageSize, string? search = null, CancellationToken cancellationToken = default);

    /// <summary>Y-23: CreateAsync ile aynı kapsam kuralı. Yalnızca Draft/Rejected iken düzenlenebilir.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.EventsWrite)]
    [ValidationAspect(typeof(UpdateEventRequestValidator))]
    [TransactionAspect]
    Task<IResult> UpdateAsync(int eventId, UpdateEventRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Y-16: soft delete. Yalnızca Draft iken silinebilir — yayınlanmış etkinlik silinmez, iptal edilir (A-49).</summary>
    [SecuredOperation(IdentitySeedData.Permissions.EventsWrite)]
    [TransactionAspect]
    Task<IResult> DeleteAsync(int eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/MIMARI.md · A-49/Y-61: yayınlanmış etkinliği iptal eder. Kayıtlar **silinmez** — katılımcılar
    /// kaydını iptal rozetiyle görmeye devam eder; yeni kayıt kabul edilmez ve vitrinde listelenmez.
    /// Y-46/Y-06: transaction elle yönetilir (bkz. EventManager) — commit sonrası katılımcılara
    /// bildirim işi kuyruğa eklenir, bu yüzden [TransactionAspect] kasıtlı olarak kullanılmaz.
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.EventsWrite)]
    [ValidationAspect(typeof(CancelEventRequestValidator))]
    [CacheRemoveAspect("PublicContentManager.")]
    Task<IResult> CancelAsync(int eventId, CancelEventRequestDto request, CancellationToken cancellationToken = default);
}
