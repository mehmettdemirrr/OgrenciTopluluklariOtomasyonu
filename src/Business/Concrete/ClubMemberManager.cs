using Business.Abstract;
using Business.Constants;
using Business.DTOs.Clubs;
using Core.DataAccess;
using Core.Utilities.Results;
using Core.Utilities.Security;
using Core.Utilities.Time;
using DataAccess.Seed;
using Entities;
using Entities.Enums;

namespace Business.Concrete;

public sealed class ClubMemberManager(
    IEntityRepository<Club> clubRepository,
    IEntityRepository<ClubMembership> clubMembershipRepository,
    IEntityRepository<ClubRoleDefinition> clubRoleDefinitionRepository,
    IEntityRepository<Student> studentRepository,
    IEntityRepository<AcademicStaff> academicStaffRepository,
    IEntityRepository<AcademicTerm> academicTermRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IClubMemberService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<IDataResult<IReadOnlyCollection<MyClubMembershipDto>>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return DataResult<IReadOnlyCollection<MyClubMembershipDto>>.Success([]);
        }

        var student = await studentRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false);
        if (student is null)
        {
            return DataResult<IReadOnlyCollection<MyClubMembershipDto>>.Success([]);
        }

        // docs/PLAN-V4.md §22.3: buraya kadar TÜM dönemlerin üyelikleri dönüyordu, oysa
        // EventManager.EnsureClubWriteAccessAsync yalnızca GÜNCEL dönemin üyeliğine bakar —
        // arayüz "başkansın" derken backend "değilsin" diyordu. Artık ikisi aynı dönemi konuşuyor.
        var term = await academicTermRepository.GetAsync(t => t.IsCurrent, cancellationToken).ConfigureAwait(false);
        if (term is null)
        {
            return DataResult<IReadOnlyCollection<MyClubMembershipDto>>.Success([]);
        }

        var memberships = await clubMembershipRepository
            .GetListAsync(m => m.StudentId == student.Id && m.AcademicTermId == term.Id, cancellationToken)
            .ConfigureAwait(false);

        var clubIds = memberships.Select(m => m.ClubId).Distinct().ToList();
        var clubsById = (await clubRepository.GetListAsync(c => clubIds.Contains(c.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(c => c.Id, c => c);

        var definitionNames = await GetRoleDefinitionNamesAsync(
            memberships.Select(m => m.ClubRoleDefinitionId), cancellationToken).ConfigureAwait(false);

        IReadOnlyCollection<MyClubMembershipDto> items = memberships
            .OrderByDescending(m => m.JoinedAtUtc)
            .Select(m =>
            {
                clubsById.TryGetValue(m.ClubId, out var club);
                return new MyClubMembershipDto
                {
                    ClubId = m.ClubId,
                    ClubName = club?.Name ?? string.Empty,
                    ClubIsActive = club?.IsActive ?? false,
                    ClubRole = m.ClubRole,
                    ClubRoleName = m.ClubRoleDefinitionId is { } did ? definitionNames.GetValueOrDefault(did) : null,
                    JoinedAtUtc = m.JoinedAtUtc,
                    AcademicTermName = term.Name,
                };
            })
            .ToList();

        return DataResult<IReadOnlyCollection<MyClubMembershipDto>>.Success(items);
    }

    public async Task<IDataResult<PagedResult<ClubMemberListItemDto>>> GetMembersPagedAsync(
        int clubId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return DataResult<PagedResult<ClubMemberListItemDto>>.NotFound(Messages.ClubNotFound);
        }

        var accessError = await EnsureMemberViewAccessAsync(club, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return DataResult<PagedResult<ClubMemberListItemDto>>.Forbidden(accessError);
        }

        var clampedPageSize = ClampPageSize(pageSize);
        var paged = await clubMembershipRepository
            .GetListPagedAsync(pageIndex, clampedPageSize, m => m.ClubId == clubId, cancellationToken)
            .ConfigureAwait(false);

        var studentIds = paged.Items.Select(m => m.StudentId).Distinct().ToList();
        var studentNumbers = (await studentRepository.GetListAsync(s => studentIds.Contains(s.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(s => s.Id, s => s.StudentNumber);

        var definitionNames = await GetRoleDefinitionNamesAsync(
            paged.Items.Select(m => m.ClubRoleDefinitionId), cancellationToken).ConfigureAwait(false);

        var items = paged.Items.Select(m => new ClubMemberListItemDto
        {
            MembershipId = m.Id,
            StudentId = m.StudentId,
            StudentNumber = studentNumbers.GetValueOrDefault(m.StudentId, string.Empty),
            ClubRole = m.ClubRole,
            ClubRoleDefinitionId = m.ClubRoleDefinitionId,
            ClubRoleName = m.ClubRoleDefinitionId is { } did ? definitionNames.GetValueOrDefault(did) : null,
            JoinedAtUtc = m.JoinedAtUtc,
        }).ToList();

        var result = new PagedResult<ClubMemberListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize);
        return DataResult<PagedResult<ClubMemberListItemDto>>.Success(result);
    }

    public async Task<IResult> SetRoleAsync(int clubId, int membershipId, SetClubRoleRequestDto request, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return Result.NotFound(Messages.ClubNotFound);
        }

        var membership = await clubMembershipRepository
            .GetAsync(m => m.Id == membershipId && m.ClubId == clubId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return Result.NotFound(Messages.ClubMembershipNotFound);
        }

        var accessError = await EnsureRoleManagementAccessAsync(club, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return Result.Forbidden(accessError);
        }

        // A-61/Y-22: unvan verilmişse yetki seviyesi TANIMDAN okunur — istemcinin gönderdiği
        // ClubRole yok sayılır. İki alan asla ayrışmaz; yetki kararı yine ClubRole'den okunacak.
        var targetRole = request.ClubRole;
        int? targetDefinitionId = null;
        // A-68: unvansız atamada kapasite makamdan türetilir — Faz 34 öncesi davranış.
        var targetCapabilities = ClubCapabilityDefaults.ForRole(request.ClubRole);

        if (request.ClubRoleDefinitionId is { } definitionId)
        {
            // O-20: tanım BU kulübe ait olmalı — başka kulübün unvanı atanamaz.
            var definition = await clubRoleDefinitionRepository
                .GetAsync(d => d.Id == definitionId && d.ClubId == clubId, cancellationToken)
                .ConfigureAwait(false);
            if (definition is null)
            {
                return Result.NotFound(Messages.ClubRoleDefinitionNotFound);
            }

            targetRole = definition.ClubRole;
            targetDefinitionId = definition.Id;
            targetCapabilities = definition.Capabilities;
        }

        // A-39: kendi başkanlık rolünü kendi kendine kaldıramaz/değiştiremez — CannotRemoveOwnAdminRole
        // muhafızıyla aynı sınıf (kilitlenme değil, kendine dokunmayı engelleyen bir öz-kısıtlama).
        // Danışman her zaman başka bir başkan atayabilir; bu yalnızca President'in KENDİ eylemini kapatır.
        if (membership.ClubRole == ClubRole.President && targetRole != ClubRole.President
            && await IsActingAsThisStudentAsync(membership.StudentId, cancellationToken).ConfigureAwait(false))
        {
            return Result.Conflict(Messages.CannotChangeOwnPresidentRole);
        }

        if (targetRole == ClubRole.President && membership.ClubRole != ClubRole.President)
        {
            var existingPresident = await clubMembershipRepository
                .GetAsync(m => m.ClubId == clubId && m.AcademicTermId == membership.AcademicTermId && m.ClubRole == ClubRole.President, cancellationToken)
                .ConfigureAwait(false);
            if (existingPresident is not null)
            {
                return Result.Conflict(Messages.ClubAlreadyHasPresident);
            }
        }

        membership.ClubRole = targetRole;
        membership.ClubRoleDefinitionId = targetDefinitionId;
        membership.Capabilities = targetCapabilities;
        clubMembershipRepository.Update(membership);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.ClubRoleUpdated);
    }

    public async Task<IResult> RemoveMemberAsync(int clubId, int membershipId, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return Result.NotFound(Messages.ClubNotFound);
        }

        var membership = await clubMembershipRepository
            .GetAsync(m => m.Id == membershipId && m.ClubId == clubId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return Result.NotFound(Messages.ClubMembershipNotFound);
        }

        var accessError = await EnsureRoleManagementAccessAsync(club, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return Result.Forbidden(accessError);
        }

        if (membership.ClubRole == ClubRole.President
            && await IsActingAsThisStudentAsync(membership.StudentId, cancellationToken).ConfigureAwait(false))
        {
            return Result.Conflict(Messages.CannotChangeOwnPresidentRole);
        }

        // Y-16: olay kaydı — fiziksel silme yok, soft delete + audit interceptor'ın Delete olarak tanıması (Y-44).
        membership.IsDeleted = true;
        membership.DeletedAtUtc = clock.UtcNow;
        clubMembershipRepository.Update(membership);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.ClubMemberRemoved);
    }

    public async Task<IResult> LeaveAsync(int clubId, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Unauthorized(Messages.NotAStudent);
        }

        var student = await studentRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false);
        if (student is null)
        {
            return Result.Forbidden(Messages.NotAStudent);
        }

        var term = await academicTermRepository.GetAsync(t => t.IsCurrent, cancellationToken).ConfigureAwait(false);
        if (term is null)
        {
            return Result.NotFound(Messages.NoCurrentAcademicTerm);
        }

        var membership = await clubMembershipRepository
            .GetAsync(m => m.ClubId == clubId && m.StudentId == student.Id && m.AcademicTermId == term.Id, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return Result.NotFound(Messages.ClubMembershipNotFound);
        }

        // §19.2: son başkan ayrılırsa kulüp yönetilemez kalır — EnsureClubWriteAccessAsync kimseyi
        // geçirmez. RemoveMemberAsync'in CannotChangeOwnPresidentRole muhafızının aynı gerekçesi.
        if (membership.ClubRole == ClubRole.President)
        {
            var otherPresidents = await clubMembershipRepository
                .GetListAsync(
                    m => m.ClubId == clubId && m.AcademicTermId == term.Id && m.ClubRole == ClubRole.President && m.Id != membership.Id,
                    cancellationToken)
                .ConfigureAwait(false);

            if (otherPresidents.Count == 0)
            {
                return Result.Conflict(Messages.LastPresidentCannotLeave);
            }
        }

        // Y-16: soft delete + audit interceptor'ın Delete olarak tanıması (Y-44).
        membership.IsDeleted = true;
        membership.DeletedAtUtc = clock.UtcNow;
        clubMembershipRepository.Update(membership);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.ClubLeft);
    }

    public async Task<IDataResult<IReadOnlyList<ClubRoleDefinitionDto>>> GetRoleDefinitionsAsync(
        int clubId, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return DataResult<IReadOnlyList<ClubRoleDefinitionDto>>.NotFound(Messages.ClubNotFound);
        }

        // Y-23: mevcut kapsam metodu DEĞİŞTİRİLMEDEN çağrılır — bu fazın sözü budur.
        var accessError = await EnsureMemberViewAccessAsync(club, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return DataResult<IReadOnlyList<ClubRoleDefinitionDto>>.Forbidden(accessError);
        }

        var definitions = await clubRoleDefinitionRepository
            .GetListAsync(d => d.ClubId == clubId, cancellationToken)
            .ConfigureAwait(false);

        // Y-64: DisplayOrder artan, Id son kırıcı.
        IReadOnlyList<ClubRoleDefinitionDto> items = definitions
            .OrderBy(d => d.DisplayOrder)
            .ThenBy(d => d.Id)
            .Select(ToRoleDefinitionDto)
            .ToList();

        return DataResult<IReadOnlyList<ClubRoleDefinitionDto>>.Success(items);
    }

    public async Task<IDataResult<ClubRoleDefinitionDto>> CreateRoleDefinitionAsync(
        int clubId, CreateClubRoleDefinitionRequestDto request, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return DataResult<ClubRoleDefinitionDto>.NotFound(Messages.ClubNotFound);
        }

        var accessError = await EnsureRoleManagementAccessAsync(club, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return DataResult<ClubRoleDefinitionDto>.Forbidden(accessError);
        }

        var name = request.Name.Trim();
        var duplicate = await clubRoleDefinitionRepository
            .GetAsync(d => d.ClubId == clubId && d.Name == name, cancellationToken)
            .ConfigureAwait(false);
        if (duplicate is not null)
        {
            return DataResult<ClubRoleDefinitionDto>.Conflict(Messages.ClubRoleDefinitionNameTaken);
        }

        var definition = new ClubRoleDefinition
        {
            ClubId = clubId,
            Name = name,
            ClubRole = request.ClubRole,
            Capabilities = request.Capabilities,
            DisplayOrder = request.DisplayOrder,
        };

        await clubRoleDefinitionRepository.AddAsync(definition, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return DataResult<ClubRoleDefinitionDto>.Success(ToRoleDefinitionDto(definition), Messages.ClubRoleDefinitionCreated);
    }

    public async Task<IResult> UpdateRoleDefinitionAsync(
        int clubId, int definitionId, UpdateClubRoleDefinitionRequestDto request, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return Result.NotFound(Messages.ClubNotFound);
        }

        var accessError = await EnsureRoleManagementAccessAsync(club, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return Result.Forbidden(accessError);
        }

        var definition = await clubRoleDefinitionRepository
            .GetAsync(d => d.Id == definitionId && d.ClubId == clubId, cancellationToken)
            .ConfigureAwait(false);
        if (definition is null)
        {
            return Result.NotFound(Messages.ClubRoleDefinitionNotFound);
        }

        var name = request.Name.Trim();
        var duplicate = await clubRoleDefinitionRepository
            .GetAsync(d => d.Id != definitionId && d.ClubId == clubId && d.Name == name, cancellationToken)
            .ConfigureAwait(false);
        if (duplicate is not null)
        {
            return Result.Conflict(Messages.ClubRoleDefinitionNameTaken);
        }

        var roleChanged = definition.ClubRole != request.ClubRole;
        var capabilitiesChanged = definition.Capabilities != request.Capabilities;

        // O-27: makam VEYA kapasite değiştiyse taşıyıcıları topla — ikisi de üyeliğe yansır.
        // Yalnızca makama bakmak, "kapasiteyi kıstım ama üye hâlâ yapabiliyor" hatasını üretirdi.
        List<ClubMembership> holders = [];
        if (roleChanged || capabilitiesChanged)
        {
            holders = await clubMembershipRepository
                .GetListAsync(m => m.ClubRoleDefinitionId == definitionId, cancellationToken)
                .ConfigureAwait(false);

            // A-39: bir kulüpte tek başkan makamı. Filtreli unique index de yakalar ama sebebi söylemez.
            if (roleChanged && request.ClubRole == ClubRole.President && holders.Count > 1)
            {
                return Result.Conflict(Messages.ClubRoleDefinitionWouldCreateSecondPresident);
            }
        }

        definition.Name = name;
        definition.ClubRole = request.ClubRole;
        definition.Capabilities = request.Capabilities;
        definition.DisplayOrder = request.DisplayOrder;
        clubRoleDefinitionRepository.Update(definition);

        foreach (var membership in holders)
        {
            membership.ClubRole = request.ClubRole;
            membership.Capabilities = request.Capabilities;
            clubMembershipRepository.Update(membership);
        }

        // [TransactionAspect] tek transaction'ı garanti eder — tanım ve üyelikler birlikte yazılır.
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.ClubRoleDefinitionUpdated);
    }

    public async Task<IResult> DeleteRoleDefinitionAsync(int clubId, int definitionId, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return Result.NotFound(Messages.ClubNotFound);
        }

        var accessError = await EnsureRoleManagementAccessAsync(club, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return Result.Forbidden(accessError);
        }

        var definition = await clubRoleDefinitionRepository
            .GetAsync(d => d.Id == definitionId && d.ClubId == clubId, cancellationToken)
            .ConfigureAwait(false);
        if (definition is null)
        {
            return Result.NotFound(Messages.ClubRoleDefinitionNotFound);
        }

        // A-61: kullanımdaysa açık mesajla reddet. FK Restrict de yakalar ama sebebi o söylemez.
        var inUse = await clubMembershipRepository
            .GetAsync(m => m.ClubRoleDefinitionId == definitionId, cancellationToken)
            .ConfigureAwait(false);
        if (inUse is not null)
        {
            return Result.Conflict(Messages.ClubRoleDefinitionInUse);
        }

        clubRoleDefinitionRepository.Delete(definition);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.ClubRoleDefinitionDeleted);
    }

    private static ClubRoleDefinitionDto ToRoleDefinitionDto(ClubRoleDefinition d) => new()
    {
        Id = d.Id, Name = d.Name, ClubRole = d.ClubRole, Capabilities = d.Capabilities, DisplayOrder = d.DisplayOrder,
    };

    /// <summary>Y-10: unvan adları tek toplu sorguyla — üyelik başına sorgu N+1 üretirdi.</summary>
    private async Task<Dictionary<int, string>> GetRoleDefinitionNamesAsync(
        IEnumerable<int?> definitionIds, CancellationToken cancellationToken)
    {
        var ids = definitionIds.Where(id => id is not null).Select(id => id!.Value).Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return (await clubRoleDefinitionRepository.GetListAsync(d => ids.Contains(d.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(d => d.Id, d => d.Name);
    }

    // Y-23: memberships.read izni yeterli değil — danışman, o kulüpte güncel dönemde Officer/President
    // olan öğrenci, ya da reports.read.all taşıyan yönetici (blanket) üye listesini görebilir.
    private async Task<string?> EnsureMemberViewAccessAsync(Club club, CancellationToken cancellationToken)
    {
        // Y-66: yönetici kontrolü her kapsam metodunun İLK satırıdır (A-55).
        // v4.0'a kadar burada `reports.read.all` vardı — rapor izni fiilen yönetim izni gibi
        // davranıyordu; artık ayrı ve açık bir izin (bkz. PLAN-V5 §25.1, bulgu 15).
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

        // A-68: karar kapasiteden. Y-75: uçtaki [SecuredOperation(memberships.read)] birinci kapı.
        return membership is not null && membership.Capabilities.HasFlag(ClubCapability.MembersView)
            ? null
            : Messages.NotClubAdvisorOrOfficer;
    }

    // Y-23: rol atama/çıkarma yalnızca danışmana veya o kulübün GÜNCEL BAŞKANINA açıktır —
    // events.approve'un "yalnızca danışman" precedent'iyle aynı sınıf, blanket admin bypass'ı yok.
    private async Task<string?> EnsureRoleManagementAccessAsync(Club club, CancellationToken cancellationToken)
    {
        // Y-66: yönetici kontrolü her kapsam metodunun İLK satırıdır (A-55).
        if (currentUser.Permissions.Contains(IdentitySeedData.Permissions.ClubsManageAll))
        {
            return null;
        }

        if (currentUser.UserId is not { } userId)
        {
            return Messages.NotClubAdvisorOrPresident;
        }

        var advisor = await academicStaffRepository.GetAsync(s => s.Id == club.AdvisorId, cancellationToken).ConfigureAwait(false);
        if (advisor is not null && advisor.ApplicationUserId == userId)
        {
            return null;
        }

        var term = await academicTermRepository.GetAsync(t => t.IsCurrent, cancellationToken).ConfigureAwait(false);
        if (term is null)
        {
            return Messages.NotClubAdvisorOrPresident;
        }

        var student = await studentRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false);
        if (student is null)
        {
            return Messages.NotClubAdvisorOrPresident;
        }

        var membership = await clubMembershipRepository
            .GetAsync(m => m.ClubId == club.Id && m.StudentId == student.Id && m.AcademicTermId == term.Id, cancellationToken)
            .ConfigureAwait(false);

        // A-68: karar kapasiteden. Bu, Faz 34'te "yalnızca President" olan kapıydı —
        // MembersManage varsayılan olarak yalnızca President makamına verilir (ClubCapabilityDefaults).
        return membership is not null && membership.Capabilities.HasFlag(ClubCapability.MembersManage)
            ? null
            : Messages.NotClubAdvisorOrPresident;
    }

    private async Task<bool> IsActingAsThisStudentAsync(int studentId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return false;
        }

        var student = await studentRepository.GetAsync(s => s.Id == studentId, cancellationToken).ConfigureAwait(false);
        return student is not null && student.ApplicationUserId == userId;
    }

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
