using Business.Abstract;
using Business.Constants;
using Business.DTOs.Admin;
using Core.DataAccess;
using Core.Utilities.Results;
using Core.Utilities.Security;
using DataAccess.Repositories;
using DataAccess.Seed;
using Entities;

namespace Business.Concrete;

public sealed class RoleAdminManager(
    IIdentityAdminDal identityAdminDal,
    IIdentityAdminGateway identityAdminGateway,
    IEntityRepository<Student> studentRepository,
    IEntityRepository<AcademicStaff> academicStaffRepository,
    IEntityRepository<Department> departmentRepository,
    IEntityRepository<Club> clubRepository,
    IEntityRepository<ClubMembership> clubMembershipRepository,
    IEntityRepository<EventParticipation> eventParticipationRepository,
    IEntityRepository<MembershipApplication> membershipApplicationRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IRoleAdminService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public Task<IDataResult<IReadOnlyCollection<PermissionCatalogItemDto>>> GetPermissionCatalogAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<PermissionCatalogItemDto> items = IdentitySeedData.AllPermissionCodes()
            .Select(code =>
            {
                var (displayName, description) = PermissionCatalog.Descriptions.TryGetValue(code, out var entry)
                    ? entry
                    : (code, string.Empty);

                return new PermissionCatalogItemDto { Code = code, DisplayName = displayName, Description = description };
            })
            .ToList();

        return Task.FromResult<IDataResult<IReadOnlyCollection<PermissionCatalogItemDto>>>(DataResult<IReadOnlyCollection<PermissionCatalogItemDto>>.Success(items));
    }

    public async Task<IDataResult<PagedResult<RoleListItemDto>>> GetRolesPagedAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var paged = await identityAdminDal.GetRolesPagedAsync(pageIndex, ClampPageSize(pageSize), cancellationToken).ConfigureAwait(false);

        var roleIds = paged.Items.Select(r => r.Id).ToList();
        var permissionRows = await identityAdminDal.GetRolePermissionsAsync(roleIds, cancellationToken).ConfigureAwait(false);
        var permissionsByRole = permissionRows
            .GroupBy(p => p.RoleId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<string>)g.Select(p => p.Permission).ToArray());

        var items = paged.Items
            .Select(r => new RoleListItemDto
            {
                Id = r.Id,
                Name = r.Name,
                IsSystemRole = IdentitySeedData.IsSystemRole(r.Id),
                Permissions = permissionsByRole.GetValueOrDefault(r.Id, []),
            })
            .ToList();

        var result = new PagedResult<RoleListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize);
        return DataResult<PagedResult<RoleListItemDto>>.Success(result);
    }

    public async Task<IDataResult<int>> CreateRoleAsync(CreateRoleRequestDto request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();

        if (await identityAdminGateway.RoleExistsByNameAsync(name).ConfigureAwait(false))
        {
            return DataResult<int>.Conflict(Messages.RoleNameTaken);
        }

        var roleId = await identityAdminGateway.CreateRoleAsync(name).ConfigureAwait(false);
        if (roleId is not { } createdRoleId)
        {
            return DataResult<int>.Conflict(Messages.RoleNameTaken);
        }

        return DataResult<int>.Success(createdRoleId, Messages.RoleCreated);
    }

    public async Task<IResult> DeleteRoleAsync(int roleId, CancellationToken cancellationToken = default)
    {
        if (IdentitySeedData.IsSystemRole(roleId))
        {
            return Result.Conflict(Messages.SystemRoleCannotBeDeleted);
        }

        var userCount = await identityAdminDal.CountUsersInRoleAsync(roleId, cancellationToken).ConfigureAwait(false);
        if (userCount > 0)
        {
            return Result.Conflict(Messages.RoleHasUsers);
        }

        var deleted = await identityAdminGateway.DeleteRoleAsync(roleId).ConfigureAwait(false);
        if (!deleted)
        {
            return Result.NotFound(Messages.RoleNotFound);
        }

        return Result.Success(Messages.RoleDeleted);
    }

    public async Task<IResult> SetRolePermissionsAsync(
        int roleId, SetRolePermissionsRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!await identityAdminGateway.RoleExistsByIdAsync(roleId).ConfigureAwait(false))
        {
            return Result.NotFound(Messages.RoleNotFound);
        }

        var unknown = request.Permissions.Except(IdentitySeedData.AllPermissionCodes()).Any();
        if (unknown)
        {
            return Result.ValidationError(Messages.UnknownPermissionCode);
        }

        // Y-03: Admin, K-17 ekranına erişimini kaybedecek şekilde kendi kendini kilitleyemez.
        if (roleId == IdentitySeedData.AdminRoleId && !request.Permissions.Contains(IdentitySeedData.Permissions.RolesManage))
        {
            return Result.Conflict(Messages.AdminRoleMustKeepRolesManage);
        }

        await identityAdminGateway.SetRolePermissionsAsync(roleId, request.Permissions).ConfigureAwait(false);

        return Result.Success(Messages.PermissionsUpdated);
    }

    public async Task<IDataResult<PagedResult<UserListItemDto>>> GetUsersPagedAsync(
        int pageIndex, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        var paged = await identityAdminDal
            .GetUsersPagedAsync(pageIndex, ClampPageSize(pageSize), search, cancellationToken)
            .ConfigureAwait(false);

        var userIds = paged.Items.Select(u => u.Id).ToList();
        var roleRows = await identityAdminDal.GetUserRoleNamesAsync(userIds, cancellationToken).ConfigureAwait(false);
        var rolesByUser = roleRows
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<string>)g.Select(r => r.RoleName).ToArray());

        var items = paged.Items
            .Select(u => new UserListItemDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsLockedOut = u.IsLockedOut,
                Roles = rolesByUser.GetValueOrDefault(u.Id, []),
            })
            .ToList();

        var result = new PagedResult<UserListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize);
        return DataResult<PagedResult<UserListItemDto>>.Success(result);
    }

    public async Task<IResult> SetUserRolesAsync(int userId, SetUserRolesRequestDto request, CancellationToken cancellationToken = default)
    {
        // Y-03: kendi Admin rolünü kaldırarak K-17 ekranına erişimini kaybetmesin.
        if (currentUser.UserId == userId)
        {
            var ownRoles = await identityAdminGateway.GetUserRoleNamesAsync(userId).ConfigureAwait(false);
            if (ownRoles.Contains(IdentitySeedData.AdminRoleName) && !request.RoleNames.Contains(IdentitySeedData.AdminRoleName))
            {
                return Result.Conflict(Messages.CannotRemoveOwnAdminRole);
            }
        }

        foreach (var roleName in request.RoleNames)
        {
            if (!await identityAdminGateway.RoleExistsByNameAsync(roleName).ConfigureAwait(false))
            {
                return Result.NotFound(Messages.RoleNotFound);
            }
        }

        var updated = await identityAdminGateway.SetUserRolesAsync(userId, request.RoleNames).ConfigureAwait(false);
        if (!updated)
        {
            return Result.NotFound(Messages.UserNotFound);
        }

        return Result.Success(Messages.RolesUpdated);
    }

    public async Task<IDataResult<int>> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        foreach (var roleName in request.RoleNames)
        {
            if (!await identityAdminGateway.RoleExistsByNameAsync(roleName).ConfigureAwait(false))
            {
                return DataResult<int>.NotFound(Messages.RoleNotFound);
            }
        }

        // Y-67: profil gerektiren rol seçildiyse alanlar ZORUNLU. Identity kaydı yazıldıktan sonra
        // eksik alan fark edilirse geriye yarım kullanıcı kalırdı — doğrulama önce yapılır.
        var needsStudent = request.RoleNames.Contains(IdentitySeedData.MemberRoleName);
        var needsStaff = request.RoleNames.Contains(IdentitySeedData.AdvisorRoleName);

        var profileError = await ValidateProfileFieldsAsync(request, needsStudent, needsStaff, cancellationToken).ConfigureAwait(false);
        if (profileError is not null)
        {
            return DataResult<int>.ValidationError(profileError);
        }

        var userId = await identityAdminGateway
            .CreateUserAsync(request.Email.Trim(), request.Password, request.RoleNames, request.FirstName, request.LastName)
            .ConfigureAwait(false);

        if (userId is not { } id)
        {
            return DataResult<int>.Conflict(Messages.UserCreationFailed);
        }

        if (needsStudent)
        {
            await studentRepository.AddAsync(
                new Student
                {
                    ApplicationUserId = id,
                    StudentNumber = request.StudentNumber!.Trim(),
                    DepartmentId = request.DepartmentId!.Value,
                    EnrollmentYear = request.EnrollmentYear!.Value,
                },
                cancellationToken).ConfigureAwait(false);
        }

        if (needsStaff)
        {
            await academicStaffRepository.AddAsync(
                new AcademicStaff
                {
                    ApplicationUserId = id,
                    Title = request.Title!.Trim(),
                    DepartmentId = request.DepartmentId!.Value,
                },
                cancellationToken).ConfigureAwait(false);
        }

        if (needsStudent || needsStaff)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return DataResult<int>.Success(id, Messages.UserCreated);
    }

    /// <summary>Y-67: rol seçimi profil alanlarını zorunlu kılar; eksikse kullanıcı hiç oluşturulmaz.</summary>
    private async Task<string?> ValidateProfileFieldsAsync(
        CreateUserRequestDto request, bool needsStudent, bool needsStaff, CancellationToken cancellationToken)
    {
        if (!needsStudent && !needsStaff)
        {
            return null;
        }

        if (request.DepartmentId is not { } departmentId)
        {
            return Messages.ProfileDepartmentRequired;
        }

        var department = await departmentRepository.GetAsync(d => d.Id == departmentId, cancellationToken).ConfigureAwait(false);
        if (department is null)
        {
            return Messages.DepartmentNotFound;
        }

        if (needsStudent)
        {
            if (string.IsNullOrWhiteSpace(request.StudentNumber) || request.EnrollmentYear is null)
            {
                return Messages.StudentProfileRequired;
            }

            var numberTaken = await studentRepository
                .GetAsync(s => s.StudentNumber == request.StudentNumber.Trim(), cancellationToken)
                .ConfigureAwait(false);

            if (numberTaken is not null)
            {
                return Messages.StudentNumberTaken;
            }
        }

        if (needsStaff && string.IsNullOrWhiteSpace(request.Title))
        {
            return Messages.AdvisorProfileRequired;
        }

        return null;
    }

    public async Task<IResult> DeleteUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Y-03/A-57: yönetici kendi hesabını silemez — CannotLockOwnAccount ile aynı öz-kısıtlama.
        if (currentUser.UserId == userId)
        {
            return Result.Conflict(Messages.CannotDeleteOwnAccount);
        }

        var blocker = await FindDeletionBlockerAsync(userId, cancellationToken).ConfigureAwait(false);
        if (blocker is not null)
        {
            return Result.Conflict(blocker);
        }

        var deleted = await identityAdminGateway.DeleteUserAsync(userId).ConfigureAwait(false);
        return deleted ? Result.Success(Messages.UserDeleted) : Result.NotFound(Messages.UserNotFound);
    }

    /// <summary>
    /// A-57: silmeyi engelleyen ilk bağı döner. Sessizce yetim kayıt bırakmak yerine hangi bağın
    /// engellediği söylenir — FK'lar `Restrict`, Y-16 soft delete var ve K-19 (KVKK) V1 dışı.
    /// </summary>
    private async Task<string?> FindDeletionBlockerAsync(int userId, CancellationToken cancellationToken)
    {
        var staff = await academicStaffRepository.GetAsync(a => a.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false);
        if (staff is not null)
        {
            var advisedClub = await clubRepository.GetAsync(c => c.AdvisorId == staff.Id, cancellationToken).ConfigureAwait(false);
            if (advisedClub is not null)
            {
                return Messages.UserHasAdvisedClubs;
            }
        }

        var student = await studentRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false);
        if (student is null)
        {
            return null;
        }

        if (await clubMembershipRepository.GetAsync(m => m.StudentId == student.Id, cancellationToken).ConfigureAwait(false) is not null)
        {
            return Messages.UserHasClubMemberships;
        }

        if (await eventParticipationRepository.GetAsync(p => p.StudentId == student.Id, cancellationToken).ConfigureAwait(false) is not null)
        {
            return Messages.UserHasEventParticipations;
        }

        if (await membershipApplicationRepository.GetAsync(a => a.StudentId == student.Id, cancellationToken).ConfigureAwait(false) is not null)
        {
            return Messages.UserHasApplications;
        }

        return null;
    }

    public async Task<IResult> SetLockoutAsync(int userId, SetLockoutRequestDto request, CancellationToken cancellationToken = default)
    {
        // Y-03: Admin kendi hesabını kilitleyerek K-17 ekranına erişimini kaybedecek şekilde
        // kendi kendini kilitleyemez — CannotRemoveOwnAdminRole ile aynı öz-kısıtlama sınıfı.
        if (request.Locked && currentUser.UserId == userId)
        {
            return Result.Conflict(Messages.CannotLockOwnAccount);
        }

        var updated = await identityAdminGateway.SetLockoutAsync(userId, request.Locked).ConfigureAwait(false);
        return updated ? Result.Success(Messages.LockoutUpdated) : Result.NotFound(Messages.UserNotFound);
    }

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
