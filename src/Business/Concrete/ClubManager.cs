using AutoMapper;
using Business.Abstract;
using Business.Constants;
using Business.DTOs.Clubs;
using Core.DataAccess;
using Core.Utilities.Results;
using Core.Utilities.Time;
using Entities;
using Entities.Enums;

namespace Business.Concrete;

public sealed class ClubManager(
    IEntityRepository<Club> clubRepository,
    IEntityRepository<AcademicStaff> academicStaffRepository,
    IEntityRepository<ClubCategory> clubCategoryRepository,
    IEntityRepository<MembershipApplication> membershipApplicationRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    IMapper mapper) : IClubService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<IDataResult<PagedResult<ClubListItemDto>>> GetListPagedAsync(
        int pageIndex, int pageSize, string? search = null, bool? isActive = null, int? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var clampedPageSize = ClampPageSize(pageSize);
        var term = SearchTerm.Normalize(search);

        // Y-11/A-16: sayfalama kırpma bir iş kuralıdır, controller'da değil burada yapılır.
        // A-50/Y-62: arama SQL'de (LIKE) — liste çekip bellekte ayıklamak yok.
        // isActive artık dışarıdan gelir: sabit `c.IsActive` filtresi pasif kulübü arayüzden
        // tamamen kaybediyor, dolayısıyla geri açılamıyordu (bkz. PLAN-V4 §21.1b).
        var paged = await clubRepository
            .GetListPagedAsync(
                pageIndex,
                clampedPageSize,
                c => (isActive == null || c.IsActive == isActive)
                    && (categoryId == null || c.ClubCategoryId == categoryId)
                    && (term.Length == 0 || c.Name.Contains(term)),
                c => c.Name,
                descending: false,
                cancellationToken)
            .ConfigureAwait(false);

        var items = mapper.Map<IReadOnlyList<ClubListItemDto>>(paged.Items);
        await FillCategoryNamesAsync(items, cancellationToken).ConfigureAwait(false);

        var result = new PagedResult<ClubListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize);

        return DataResult<PagedResult<ClubListItemDto>>.Success(result);
    }

    public async Task<IDataResult<ClubDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return DataResult<ClubDetailDto>.NotFound(Messages.ClubNotFound);
        }

        var dto = mapper.Map<ClubDetailDto>(club);
        if (club.ClubCategoryId is { } detailCategoryId)
        {
            var category = await clubCategoryRepository
                .GetAsync(c => c.Id == detailCategoryId, cancellationToken)
                .ConfigureAwait(false);
            dto.ClubCategoryName = category?.Name;
        }

        return DataResult<ClubDetailDto>.Success(dto);
    }

    /// <summary>
    /// Y-10/Y-32: kategori adı AutoMapper ile taşınamaz (join gerekir). Tek toplu sorguyla
    /// doldurulur — satır başına sorgu N+1 üretirdi (EventManager.MapWithClubNamesAsync deseni).
    /// </summary>
    private async Task FillCategoryNamesAsync(IReadOnlyList<ClubListItemDto> items, CancellationToken cancellationToken)
    {
        var categoryIds = items
            .Where(i => i.ClubCategoryId is not null)
            .Select(i => i.ClubCategoryId!.Value)
            .Distinct()
            .ToList();

        if (categoryIds.Count == 0)
        {
            return;
        }

        var namesById = (await clubCategoryRepository.GetListAsync(c => categoryIds.Contains(c.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(c => c.Id, c => c.Name);

        foreach (var item in items)
        {
            if (item.ClubCategoryId is { } id)
            {
                item.ClubCategoryName = namesById.GetValueOrDefault(id);
            }
        }
    }

    public async Task<IDataResult<int>> CreateAsync(CreateClubRequestDto request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();

        var existing = await clubRepository.GetAsync(c => c.Name == name, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return DataResult<int>.Conflict(Messages.ClubNameTaken);
        }

        var advisor = await academicStaffRepository.GetAsync(s => s.Id == request.AdvisorId, cancellationToken).ConfigureAwait(false);
        if (advisor is null)
        {
            return DataResult<int>.NotFound(Messages.AdvisorNotFound);
        }

        var club = new Club
        {
            Name = name,
            Description = request.Description?.Trim(),
            AdvisorId = advisor.Id,
            ClubCategoryId = request.ClubCategoryId,
            IsActive = true,
            CreatedAtUtc = clock.UtcNow,
        };

        await clubRepository.AddAsync(club, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return DataResult<int>.Success(club.Id, Messages.ClubCreated);
    }

    public async Task<IResult> UpdateAsync(int clubId, UpdateClubRequestDto request, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return Result.NotFound(Messages.ClubNotFound);
        }

        var name = request.Name.Trim();
        var nameTaken = await clubRepository.GetAsync(c => c.Id != clubId && c.Name == name, cancellationToken).ConfigureAwait(false);
        if (nameTaken is not null)
        {
            return Result.Conflict(Messages.ClubNameTaken);
        }

        // K-33: danışman değişimi. null gelirse mevcut danışman korunur (kısmi güncelleme).
        var advisorChanged = false;
        if (request.AdvisorId is { } advisorId && advisorId != club.AdvisorId)
        {
            var advisor = await academicStaffRepository.GetAsync(s => s.Id == advisorId, cancellationToken).ConfigureAwait(false);
            if (advisor is null)
            {
                return Result.NotFound(Messages.AdvisorNotFound);
            }

            club.AdvisorId = advisor.Id;
            advisorChanged = true;
        }

        club.Name = name;
        club.Description = request.Description?.Trim();
        // A-60: AdvisorId'den FARKLI semantik — orada null "değiştirme" demek (K-33 kısmi
        // güncelleme), burada null "kategorisiz yap" demektir. Kategori zorunlu olmadığı için temizlenebilmeli.
        club.ClubCategoryId = request.ClubCategoryId;
        clubRepository.Update(club);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(advisorChanged ? Messages.ClubAdvisorChanged : Messages.ClubUpdated);
    }

    public async Task<IResult> SetStatusAsync(int clubId, SetClubStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return Result.NotFound(Messages.ClubNotFound);
        }

        if (!request.IsActive && club.IsActive)
        {
            var hasPending = await membershipApplicationRepository
                .GetAsync(a => a.ClubId == clubId && a.Status == ApplicationStatus.Pending, cancellationToken)
                .ConfigureAwait(false);
            if (hasPending is not null)
            {
                return Result.Conflict(Messages.ClubHasPendingApplications);
            }
        }

        club.IsActive = request.IsActive;
        clubRepository.Update(club);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.ClubStatusUpdated);
    }

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
