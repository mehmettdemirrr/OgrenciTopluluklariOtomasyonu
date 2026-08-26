using Business.DTOs.Reference;
using Business.ValidationRules;
using Core.Aspects.Autofac;
using Core.DataAccess;
using Core.Utilities.Results;
using DataAccess.Seed;

namespace Business.Abstract;

/// <summary>docs/MIMARI.md · A-13: dönem yönetimi — IX_AcademicTerms_IsCurrent tek güncel dönem şartını korur.</summary>
public interface IAcademicTermService
{
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    Task<IDataResult<PagedResult<AcademicTermListItemDto>>> GetTermsPagedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [ValidationAspect(typeof(CreateAcademicTermRequestValidator))]
    [TransactionAspect]
    Task<IDataResult<int>> CreateTermAsync(CreateAcademicTermRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>docs/PLAN-V2.md · Faz 13: isim/tarih düzenleme — IsCurrent burada değiştirilmez, bkz. SetCurrentAsync.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [ValidationAspect(typeof(UpdateAcademicTermRequestValidator))]
    [TransactionAspect]
    Task<IResult> UpdateTermAsync(int termId, UpdateAcademicTermRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/MIMARI.md: eski güncel dönem önce false yapılıp kaydedilir, SONRA hedef true yapılıp
    /// kaydedilir (iki ayrı SaveChanges, tek [TransactionAspect]) — IX_AcademicTerms_IsCurrent
    /// filtreli unique index'i tek SaveChanges'in garanti etmediği bir sırayla ihlal etmesin diye.
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [TransactionAspect]
    Task<IResult> SetCurrentAsync(int termId, CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/MIMARI.md · K-39/A-66: dönemin topluluk kurma başvuru penceresi. Yeni izin yok —
    /// reference.manage zaten dönem yönetiminin izni. Üç alan birlikte yazılır.
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [ValidationAspect(typeof(SetClubApplicationWindowRequestValidator))]
    [TransactionAspect]
    Task<IResult> SetClubApplicationWindowAsync(
        int termId, SetClubApplicationWindowRequestDto request, CancellationToken cancellationToken = default);
}
