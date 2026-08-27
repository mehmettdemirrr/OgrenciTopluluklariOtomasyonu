using System.Globalization;
using Business.Abstract;
using Business.Constants;
using Business.DTOs.Reference;
using Core.DataAccess;
using Core.Utilities.Results;
using Core.Utilities.Time;
using Entities;
using Entities.Enums;

namespace Business.Concrete;

public sealed class AcademicTermManager(
    IEntityRepository<AcademicTerm> academicTermRepository,
    IEntityRepository<ClubMembership> clubMembershipRepository,
    IEntityRepository<Club> clubRepository,
    IUnitOfWork unitOfWork,
    IClock clock) : IAcademicTermService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<IDataResult<PagedResult<AcademicTermListItemDto>>> GetTermsPagedAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var paged = await academicTermRepository
            .GetListPagedAsync(pageIndex, ClampPageSize(pageSize), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var items = paged.Items
            .OrderByDescending(t => t.StartDateUtc)
            .Select(t => new AcademicTermListItemDto
            {
                Id = t.Id,
                Name = t.Name,
                StartDateUtc = t.StartDateUtc,
                EndDateUtc = t.EndDateUtc,
                IsCurrent = t.IsCurrent,
                ClubApplicationStartUtc = t.ClubApplicationStartUtc,
                ClubApplicationEndUtc = t.ClubApplicationEndUtc,
                ClubApplicationOverride = t.ClubApplicationOverride,
            })
            .ToList();

        return DataResult<PagedResult<AcademicTermListItemDto>>.Success(
            new PagedResult<AcademicTermListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    public async Task<IDataResult<int>> CreateTermAsync(CreateAcademicTermRequestDto request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();

        var existing = await academicTermRepository.GetAsync(t => t.Name == name, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return DataResult<int>.Conflict(Messages.AcademicTermNameTaken);
        }

        var term = new AcademicTerm { Name = name, StartDateUtc = request.StartDateUtc, EndDateUtc = request.EndDateUtc, IsCurrent = false };
        await academicTermRepository.AddAsync(term, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return DataResult<int>.Success(term.Id);
    }

    public async Task<IResult> UpdateTermAsync(int termId, UpdateAcademicTermRequestDto request, CancellationToken cancellationToken = default)
    {
        var term = await academicTermRepository.GetAsync(t => t.Id == termId, cancellationToken).ConfigureAwait(false);
        if (term is null)
        {
            return Result.NotFound(Messages.AcademicTermNotFound);
        }

        var name = request.Name.Trim();
        var duplicate = await academicTermRepository.GetAsync(t => t.Id != termId && t.Name == name, cancellationToken).ConfigureAwait(false);
        if (duplicate is not null)
        {
            return Result.Conflict(Messages.AcademicTermNameTaken);
        }

        term.Name = name;
        term.StartDateUtc = request.StartDateUtc;
        term.EndDateUtc = request.EndDateUtc;
        academicTermRepository.Update(term);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.AcademicTermUpdated);
    }

    public async Task<IResult> SetCurrentAsync(int termId, CancellationToken cancellationToken = default)
    {
        var target = await academicTermRepository.GetAsync(t => t.Id == termId, cancellationToken).ConfigureAwait(false);
        if (target is null)
        {
            return Result.NotFound(Messages.AcademicTermNotFound);
        }

        if (target.IsCurrent)
        {
            return Result.Success(Messages.AcademicTermAlreadyCurrent);
        }

        // Sıra kasıtlı: önce eski güncel dönem false yapılıp kaydedilir, ancak ondan sonra hedef
        // true yapılıp kaydedilir — aksi hâlde tek SaveChanges'in ürettiği iki UPDATE'in sırası
        // garanti edilmez ve IX_AcademicTerms_IsCurrent (filtreli unique index) ihlal edilebilir.
        var currentTerm = await academicTermRepository.GetAsync(t => t.IsCurrent, cancellationToken).ConfigureAwait(false);
        if (currentTerm is not null)
        {
            currentTerm.IsCurrent = false;
            academicTermRepository.Update(currentTerm);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        target.IsCurrent = true;
        academicTermRepository.Update(target);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // A-51/K-30: dönem devri. Kaynak dönem, IsCurrent bayrağı düşürülmeden ÖNCE yakalanan
        // `currentTerm`dir — bayrağa göre sorgulansaydı bu noktada hiçbir dönem güncel görünmezdi.
        var carriedOver = currentTerm is null
            ? 0
            : await CarryOverMembershipsAsync(currentTerm.Id, target.Id, cancellationToken).ConfigureAwait(false);

        return Result.Success(
            carriedOver == 0
                ? Messages.AcademicTermSetCurrent
                : string.Format(CultureInfo.InvariantCulture, Messages.AcademicTermSetCurrentWithRollover, carriedOver));
    }

    public async Task<IResult> SetClubApplicationWindowAsync(
        int termId, SetClubApplicationWindowRequestDto request, CancellationToken cancellationToken = default)
    {
        var term = await academicTermRepository.GetAsync(t => t.Id == termId, cancellationToken).ConfigureAwait(false);
        if (term is null)
        {
            return Result.NotFound(Messages.AcademicTermNotFound);
        }

        // A-66: üç alan birlikte yazılır — kısmi güncelleme yok, yönetici tam durumu bildirir.
        term.ClubApplicationStartUtc = request.StartUtc;
        term.ClubApplicationEndUtc = request.EndUtc;
        term.ClubApplicationOverride = request.Override;

        academicTermRepository.Update(term);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.ClubApplicationWindowUpdated);
    }

    /// <summary>
    /// docs/MIMARI.md · A-51: önceki dönemin üyeliklerini rolleriyle yeni döneme taşır.
    /// <b>Idempotent</b>: hedef dönemde zaten var olan (kulüp, öğrenci) çifti atlanır, dolayısıyla
    /// ikinci çalıştırma sıfır satır üretir. Soft-delete edilmiş üyelikler Y-16 query filter'ı
    /// tarafından zaten elenir; pasif kulüplerin üyelikleri burada elenir.
    /// </summary>
    private async Task<int> CarryOverMembershipsAsync(int sourceTermId, int targetTermId, CancellationToken cancellationToken)
    {
        var source = await clubMembershipRepository
            .GetListAsync(m => m.AcademicTermId == sourceTermId, cancellationToken)
            .ConfigureAwait(false);

        if (source.Count == 0)
        {
            return 0;
        }

        var clubIds = source.Select(m => m.ClubId).Distinct().ToList();
        var activeClubIds = (await clubRepository
                .GetListAsync(c => clubIds.Contains(c.Id) && c.IsActive, cancellationToken)
                .ConfigureAwait(false))
            .Select(c => c.Id)
            .ToHashSet();

        var existing = await clubMembershipRepository
            .GetListAsync(m => m.AcademicTermId == targetTermId, cancellationToken)
            .ConfigureAwait(false);

        // Y-18: (ClubId, StudentId, AcademicTermId) benzersizdir — çakışacak satır hiç eklenmez.
        var takenSlots = existing.Select(m => (m.ClubId, m.StudentId)).ToHashSet();

        // A-39: bir kulüpte bir dönemde tek President (filtreli unique index).
        var clubsWithPresident = existing.Where(m => m.ClubRole == ClubRole.President).Select(m => m.ClubId).ToHashSet();

        var now = clock.UtcNow;
        var carriedOver = 0;

        foreach (var membership in source)
        {
            if (!activeClubIds.Contains(membership.ClubId))
            {
                continue;
            }

            if (!takenSlots.Add((membership.ClubId, membership.StudentId)))
            {
                continue;
            }

            var role = membership.ClubRole;
            if (role == ClubRole.President && !clubsWithPresident.Add(membership.ClubId))
            {
                // Hedef dönemde bu kulübe elle bir başkan atanmışsa onun sözü geçer; devredilen
                // başkan Officer'a düşer. Y-23 açısından kayıp yok — Officer da kulüp yönetebilir.
                role = ClubRole.Officer;
            }

            await clubMembershipRepository.AddAsync(
                new ClubMembership
                {
                    ClubId = membership.ClubId,
                    StudentId = membership.StudentId,
                    AcademicTermId = targetTermId,
                    ClubRole = role,
                    // A-68: kapasite `role`'den TÜRETİLİR, kaynak üyelikten kopyalanmaz. Kopyalasaydık
                    // Officer'a düşürülen başkan MembersManage yetkisini taşımaya devam ederdi.
                    // Unvanı (ClubRoleDefinitionId) devretmiyoruz: tanım kulübe özel ve dönemsiz,
                    // ama devir kararı makam üzerinden veriliyor — ikisini karıştırmak yanlış olurdu.
                    Capabilities = ClubCapabilityDefaults.ForRole(role),
                    JoinedAtUtc = now,
                },
                cancellationToken).ConfigureAwait(false);

            carriedOver++;
        }

        if (carriedOver > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return carriedOver;
    }

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
