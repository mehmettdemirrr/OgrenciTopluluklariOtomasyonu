using AutoMapper;
using Business.Abstract;
using Business.Constants;
using Business.DTOs.Clubs;
using Core.DataAccess;
using Core.Utilities.Results;
using Core.Utilities.Security;
using Core.Utilities.Time;
using DataAccess.Repositories;
using DataAccess.Seed;
using Entities;
using Entities.Enums;

namespace Business.Concrete;

public sealed class ClubManager(
    IEntityRepository<Club> clubRepository,
    IEntityRepository<AcademicStaff> academicStaffRepository,
    IEntityRepository<ClubCategoryAssignment> clubCategoryAssignmentRepository,
    IEntityRepository<ClubRoleDefinition> clubRoleDefinitionRepository,
    IEntityRepository<MembershipApplication> membershipApplicationRepository,
    IEntityRepository<Student> studentRepository,
    IEntityRepository<ClubMembership> clubMembershipRepository,
    IEntityRepository<AcademicTerm> academicTermRepository,
    IEntityRepository<ClubSocialLink> clubSocialLinkRepository,
    IClubCategoryAssignmentDal clubCategoryAssignmentDal,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
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

        // Y-85: kategori filtresi SQL'de iki adımda: önce kategorideki kulüp id'leri, sonra sayfalama.
        IReadOnlyCollection<int>? categoryClubIds = categoryId is { } id
            ? await clubCategoryAssignmentDal.GetClubIdsByCategoryAsync(id, cancellationToken).ConfigureAwait(false)
            : null;

        // Y-11/A-16: sayfalama kırpma bir iş kuralıdır, controller'da değil burada yapılır.
        // A-50/Y-62: arama SQL'de (LIKE) — liste çekip bellekte ayıklamak yok.
        // isActive artık dışarıdan gelir: sabit `c.IsActive` filtresi pasif kulübü arayüzden
        // tamamen kaybediyor, dolayısıyla geri açılamıyordu (bkz. PLAN-V4 §21.1b).
        var paged = await clubRepository
            .GetListPagedAsync(
                pageIndex,
                clampedPageSize,
                c => (isActive == null || c.IsActive == isActive)
                    && (categoryClubIds == null || categoryClubIds.Contains(c.Id))
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

        var namesByClub = await clubCategoryAssignmentDal
            .GetNamesByClubAsync([id], cancellationToken)
            .ConfigureAwait(false);
        dto.ClubCategoryNames = namesByClub.GetValueOrDefault(id, []);

        dto.ClubCategoryIds = (await clubCategoryAssignmentRepository
                .GetListAsync(a => a.ClubId == id, cancellationToken)
                .ConfigureAwait(false))
            .Select(a => a.ClubCategoryId)
            .ToList();

        dto.SocialLinks = await GetSocialLinksAsync(id, cancellationToken).ConfigureAwait(false);

        (dto.MyRelationship, dto.MyCapabilities) = await ResolveViewerAsync(club, cancellationToken).ConfigureAwait(false);

        return DataResult<ClubDetailDto>.Success(dto);
    }

    /// <summary>
    /// docs/MIMARI.md · A-75: kapsam ÇÖZÜMÜdür, yetki KAPISI değildir — K-25. Arayüzün hangi
    /// sekmeyi/ucu çizeceğine karar vermesi için çağıranın BU kulüpteki ilişkisini döner;
    /// hiçbir dalı isteği reddetmez, yalnızca None/kapasitesiz döner.
    /// </summary>
    private async Task<(ClubRelationship Relationship, ClubCapability Capabilities)> ResolveViewerAsync(
        Club club, CancellationToken cancellationToken)
    {
        // Y-66: yönetici kontrolü her kapsam metodunun İLK satırıdır (A-55).
        if (currentUser.Permissions.Contains(IdentitySeedData.Permissions.ClubsManageAll))
        {
            return (ClubRelationship.Administrator, ClubCapabilityDefaults.ForRole(ClubRole.President) | ClubCapability.MembersManage);
        }

        if (currentUser.UserId is not { } userId)
        {
            return (ClubRelationship.None, ClubCapability.None);
        }

        var advisor = await academicStaffRepository.GetAsync(s => s.Id == club.AdvisorId, cancellationToken).ConfigureAwait(false);
        if (advisor is not null && advisor.ApplicationUserId == userId)
        {
            return (ClubRelationship.Advisor, ClubCapabilityDefaults.ForRole(ClubRole.President) | ClubCapability.MembersManage);
        }

        var student = await studentRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false);
        if (student is null)
        {
            return (ClubRelationship.None, ClubCapability.None);
        }

        // §22.3 / EnsureClubWriteAccessAsync ile aynı dönem: güncel dönemin üyeliği.
        var term = await academicTermRepository.GetAsync(t => t.IsCurrent, cancellationToken).ConfigureAwait(false);
        if (term is null)
        {
            return (ClubRelationship.None, ClubCapability.None);
        }

        var membership = await clubMembershipRepository
            .GetAsync(m => m.ClubId == club.Id && m.StudentId == student.Id && m.AcademicTermId == term.Id, cancellationToken)
            .ConfigureAwait(false);

        if (membership is null)
        {
            return (ClubRelationship.None, ClubCapability.None);
        }

        var relationship = membership.ClubRole switch
        {
            ClubRole.President => ClubRelationship.President,
            ClubRole.Officer => ClubRelationship.Officer,
            _ => ClubRelationship.Member,
        };

        return (relationship, membership.Capabilities);
    }

    /// <summary>K-44: `PublicContentManager.GetClubByIdAsync`'in de doldurduğu ikinci yapım noktası.</summary>
    private async Task<IReadOnlyList<ClubSocialLinkDto>> GetSocialLinksAsync(int clubId, CancellationToken cancellationToken) =>
        (await clubSocialLinkRepository.GetListAsync(l => l.ClubId == clubId, cancellationToken).ConfigureAwait(false))
            .OrderBy(l => l.DisplayOrder)
            .Select(l => new ClubSocialLinkDto { Platform = l.Platform, Url = l.Url, DisplayOrder = l.DisplayOrder })
            .ToList();

    /// <summary>Y-85: sayfa başına TEK toplu sorgu — satır başına sorgu N+1 üretirdi.</summary>
    private async Task FillCategoryNamesAsync(IReadOnlyList<ClubListItemDto> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        var namesByClub = await clubCategoryAssignmentDal
            .GetNamesByClubAsync(items.Select(i => i.Id).ToList(), cancellationToken)
            .ConfigureAwait(false);

        foreach (var item in items)
        {
            item.ClubCategoryNames = namesByClub.GetValueOrDefault(item.Id, []);
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
            IsActive = true,
            CreatedAtUtc = clock.UtcNow,
        };

        await clubRepository.AddAsync(club, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // A-80: club.Id yukarıdaki SaveChanges'ten geliyor (düz int FK, navigation property yok).
        await clubCategoryAssignmentDal.ReplaceAsync(club.Id, request.ClubCategoryIds, cancellationToken).ConfigureAwait(false);

        // O-20: yeni kulüp varsayılan unvan setiyle doğar — "Roller" sekmesi boş açılmasın.
        // club.Id yukarıdaki SaveChanges'ten geliyor (düz int FK, navigation property yok).
        foreach (var (roleName, role, capabilities, displayOrder) in DefaultClubRoles.All)
        {
            await clubRoleDefinitionRepository.AddAsync(
                new ClubRoleDefinition
                {
                    ClubId = club.Id, Name = roleName, ClubRole = role,
                    Capabilities = capabilities, DisplayOrder = displayOrder,
                },
                cancellationToken).ConfigureAwait(false);
        }

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
        clubRepository.Update(club);

        // A-80: kategoriler topluca değiştirilir (eskiler silinir, yeniler yazılır).
        await clubCategoryAssignmentDal.ReplaceAsync(clubId, request.ClubCategoryIds, cancellationToken).ConfigureAwait(false);

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

    public async Task<IResult> SetContactAsync(int clubId, SetClubSocialLinksRequestDto request, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return Result.NotFound(Messages.ClubNotFound);
        }

        var accessError = await EnsureClubWriteAccessAsync(club, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return Result.Forbidden(accessError);
        }

        club.ContactEmail = string.IsNullOrWhiteSpace(request.ContactEmail) ? null : request.ContactEmail.Trim();
        club.ContactPhone = string.IsNullOrWhiteSpace(request.ContactPhone) ? null : request.ContactPhone.Trim();
        clubRepository.Update(club);

        // Bağlantılar toplu değiştirilir: eski satırlar silinir, yenileri yazılır.
        // Kısmi güncelleme (id eşleme) bu ekran için gereksiz karmaşıklıktır — YAGNI.
        var existing = await clubSocialLinkRepository.GetListAsync(l => l.ClubId == clubId, cancellationToken).ConfigureAwait(false);
        foreach (var link in existing)
        {
            clubSocialLinkRepository.Delete(link);
        }

        var order = 0;
        foreach (var link in request.Links)
        {
            await clubSocialLinkRepository.AddAsync(
                new ClubSocialLink { ClubId = clubId, Platform = link.Platform, Url = link.Url.Trim(), DisplayOrder = order++ },
                cancellationToken).ConfigureAwait(false);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(Messages.ClubContactUpdated);
    }

    // Y-23/A-74: announcements.write izni yeterli değil — AnnouncementManager.EnsureClubWriteAccessAsync
    // ile aynı desen. İletişim/sosyal bağlantılar dışa dönük iletişimin bir parçası sayılır; A-68 kapalı
    // kümesinde bunun için ayrı bir kapasite yoktur, en yakın anlamsal karşılık (AnnouncementsManage)
    // yeniden kullanılır — yeni bir ClubCapability bayrağı açılmaz.
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

        return membership is not null && membership.Capabilities.HasFlag(ClubCapability.AnnouncementsManage)
            ? null
            : Messages.NotClubAdvisorOrOfficer;
    }

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
