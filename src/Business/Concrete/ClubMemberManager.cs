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
    IEntityRepository<Student> studentRepository,
    IEntityRepository<AcademicStaff> academicStaffRepository,
    IEntityRepository<AcademicTerm> academicTermRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IClubMemberService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

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

        var items = paged.Items.Select(m => new ClubMemberListItemDto
        {
            MembershipId = m.Id,
            StudentId = m.StudentId,
            StudentNumber = studentNumbers.GetValueOrDefault(m.StudentId, string.Empty),
            ClubRole = m.ClubRole,
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

        // A-39: kendi başkanlık rolünü kendi kendine kaldıramaz/değiştiremez — CannotRemoveOwnAdminRole
        // muhafızıyla aynı sınıf (kilitlenme değil, kendine dokunmayı engelleyen bir öz-kısıtlama).
        // Danışman her zaman başka bir başkan atayabilir; bu yalnızca President'in KENDİ eylemini kapatır.
        if (membership.ClubRole == ClubRole.President && request.ClubRole != ClubRole.President
            && await IsActingAsThisStudentAsync(membership.StudentId, cancellationToken).ConfigureAwait(false))
        {
            return Result.Conflict(Messages.CannotChangeOwnPresidentRole);
        }

        if (request.ClubRole == ClubRole.President && membership.ClubRole != ClubRole.President)
        {
            var existingPresident = await clubMembershipRepository
                .GetAsync(m => m.ClubId == clubId && m.AcademicTermId == membership.AcademicTermId && m.ClubRole == ClubRole.President, cancellationToken)
                .ConfigureAwait(false);
            if (existingPresident is not null)
            {
                return Result.Conflict(Messages.ClubAlreadyHasPresident);
            }
        }

        membership.ClubRole = request.ClubRole;
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

    // Y-23: memberships.read izni yeterli değil — danışman, o kulüpte güncel dönemde Officer/President
    // olan öğrenci, ya da reports.read.all taşıyan yönetici (blanket) üye listesini görebilir.
    private async Task<string?> EnsureMemberViewAccessAsync(Club club, CancellationToken cancellationToken)
    {
        if (currentUser.Permissions.Contains(IdentitySeedData.Permissions.ReportsReadAll))
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

        return membership is not null && membership.ClubRole is ClubRole.Officer or ClubRole.President
            ? null
            : Messages.NotClubAdvisorOrOfficer;
    }

    // Y-23: rol atama/çıkarma yalnızca danışmana veya o kulübün GÜNCEL BAŞKANINA açıktır —
    // events.approve'un "yalnızca danışman" precedent'iyle aynı sınıf, blanket admin bypass'ı yok.
    private async Task<string?> EnsureRoleManagementAccessAsync(Club club, CancellationToken cancellationToken)
    {
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

        return membership is not null && membership.ClubRole == ClubRole.President
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
