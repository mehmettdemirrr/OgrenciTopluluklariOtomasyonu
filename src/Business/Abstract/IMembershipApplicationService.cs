using Business.DTOs.Memberships;
using Business.ValidationRules;
using Core.Aspects.Autofac;
using Core.DataAccess;
using Core.Utilities.Results;
using DataAccess.Seed;

namespace Business.Abstract;

public interface IMembershipApplicationService
{
    /// <summary>
    /// docs/MIMARI.md: öğrencinin kendi başvurusu — [SecuredOperation] kasıtlı olarak yok,
    /// herhangi bir kimliği doğrulanmış kullanıcı kendi adına başvurabilir (Y-21 fallback policy
    /// zaten kimlik doğrulaması şart koşuyor). [TransactionAspect]'in dürüst, kontrivasyonsuz
    /// gösterimi burada — bu metodun commit-sonrası bir yan etkisi yok (bkz. ReviewAsync).
    /// </summary>
    [ValidationAspect(typeof(ApplyForMembershipRequestValidator))]
    [TransactionAspect]
    Task<IResult> ApplyAsync(ApplyForMembershipRequestDto request, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.MembershipsRead)]
    Task<IDataResult<PagedResult<MembershipApplicationListItemDto>>> GetPendingForAdvisorAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/MIMARI.md · Y-46/Y-06: transaction elle yönetilir (bkz. MembershipApplicationManager) —
    /// Hangfire enqueue'sinin commit'ten sonra çalışabilmesi için [TransactionAspect] kasıtlı
    /// olarak kullanılmaz.
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.MembershipsWrite)]
    [ValidationAspect(typeof(ReviewMembershipApplicationRequestValidator))]
    Task<IResult> ReviewAsync(int applicationId, ReviewMembershipApplicationRequestDto request, CancellationToken cancellationToken = default);
}
