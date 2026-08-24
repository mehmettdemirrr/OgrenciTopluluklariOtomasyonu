using Business.DTOs.Events;
using Core.Aspects.Autofac;
using Core.DataAccess;
using Core.Utilities.Results;
using DataAccess.Seed;

namespace Business.Abstract;

/// <summary>docs/PLAN-V2.md §10: EventParticipation'ın ilk gerçek kullanımı, ClubMemberManager ile aynı ayrım gerekçesi.</summary>
public interface IEventParticipationService
{
    /// <summary>
    /// docs/PLAN-V2.md §10.1: kaydolmak izin gerektirmez — MembershipApplicationManager.ApplyAsync
    /// precedent'i (herhangi bir kimliği doğrulanmış öğrenci). A-38: kontenjan RowVersion ile korunur.
    /// </summary>
    [TransactionAspect]
    Task<IResult> RegisterAsync(int eventId, CancellationToken cancellationToken = default);

    [TransactionAspect]
    Task<IResult> CancelAsync(int eventId, CancellationToken cancellationToken = default);

    /// <summary>Y-23: events.write yeterli değil — danışman / o kulübün Officer-President'i / reports.read.all.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.EventsWrite)]
    Task<IDataResult<PagedResult<EventParticipantListItemDto>>> GetParticipantsAsync(
        int eventId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.EventsRead)]
    Task<IDataResult<PagedResult<EventListItemDto>>> GetMineAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/PLAN-V4.md §21.5: "kayıtlı mıyım" sorusunun tek amaçlı cevabı. Önceden arayüz bunu
    /// tüm `/events/mine` listesini çekip içinde arayarak yanıtlıyordu (Y-62).
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.EventsRead)]
    Task<IDataResult<bool>> IsRegisteredAsync(int eventId, CancellationToken cancellationToken = default);
}
