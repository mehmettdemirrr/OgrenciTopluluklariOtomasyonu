using Business.DTOs.ClubApplications;
using Business.DTOs.Files;
using Business.ValidationRules;
using Core.Aspects.Autofac;
using Core.DataAccess;
using Core.Utilities.Results;
using DataAccess.Seed;

namespace Business.Abstract;

public interface IClubApplicationService
{
    /// <summary>
    /// docs/PLAN-V3.md §17: öğrencinin kendi başvurusu — MembershipApplicationManager.ApplyAsync'in
    /// [SecuredOperation]'sız precedent'i, herhangi bir kimliği doğrulanmış kullanıcı başvurabilir.
    /// </summary>
    [ValidationAspect(typeof(SubmitClubApplicationRequestValidator))]
    [TransactionAspect]
    Task<IResult> SubmitAsync(SubmitClubApplicationRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/MIMARI.md · K-39/Y-73: pencerenin güncel durumu. `[SecuredOperation]` yok — herhangi bir
    /// kimliği doğrulanmış kullanıcı görebilir (SubmitAsync ile aynı erişim sınıfı). Anonim yüzeye
    /// eklenmez (A-42). Muhafız ile bu uç AYNI EvaluateWindow metodunu çağırır.
    /// </summary>
    Task<IDataResult<ClubApplicationWindowDto>> GetWindowAsync(CancellationToken cancellationToken = default);

    /// <summary>Çağıranın kendi başvurularının durumu — küçük ve kişiye özel, sayfalanmaz (`/clubs/mine` precedent'i).</summary>
    Task<IDataResult<IReadOnlyList<ClubApplicationListItemDto>>> GetMineAsync(CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.ClubsWrite)]
    Task<IDataResult<PagedResult<ClubApplicationListItemDto>>> GetPendingAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/MIMARI.md · Y-46/Y-06: transaction elle yönetilir (bkz. ClubApplicationManager) —
    /// onayda Club + ClubMembership oluşturulup commit sonrası Hangfire'a bildirim eklenir.
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.ClubsWrite)]
    [ValidationAspect(typeof(DecideClubApplicationRequestValidator))]
    [CacheRemoveAspect("ClubManager.", "PublicContentManager.")]
    Task<IResult> DecideAsync(int applicationId, DecideClubApplicationRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/MIMARI.md · A-63/Y-70/Y-51: korumalı evrak indirme. Yetki İNDİRME ANINDA yeniden
    /// kontrol edilir — başvuran öğrenci veya clubs.write/clubs.manage.all taşıyan inceleyici.
    /// [SecuredOperation] YOK: öğrencinin kendi evrağını indirmesi izin gerektirmez, sahiplik yeter.
    /// </summary>
    Task<IDataResult<FileContentDto>> GetDocumentAsync(
        int applicationId, int documentId, CancellationToken cancellationToken = default);
}
