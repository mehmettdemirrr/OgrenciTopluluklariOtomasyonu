using Business.Abstract;
using Business.Concrete;
using Business.DTOs.Admin;
using Core.DataAccess;
using Core.Utilities.Security;
using DataAccess.Repositories;
using DataAccess.Seed;
using Entities.Dtos.Admin;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>docs/MIMARI.md · A-20: her iş kuralı için bir kabul + bir ret.</summary>
public class RoleAdminManagerTests
{
    private readonly Mock<IIdentityAdminDal> _identityAdminDal = new();
    private readonly Mock<IIdentityAdminGateway> _identityAdminGateway = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly RoleAdminManager _sut;

    public RoleAdminManagerTests()
    {
        _currentUser.Setup(c => c.UserId).Returns(999);
        _sut = new RoleAdminManager(_identityAdminDal.Object, _identityAdminGateway.Object, _currentUser.Object);
    }

    [Fact(DisplayName = "CreateRole: aynı isimde rol varsa Conflict döner")]
    public async Task CreateRoleAsync_NameTaken_ReturnsConflict()
    {
        _identityAdminGateway.Setup(g => g.RoleExistsByNameAsync("Editor")).ReturnsAsync(true);

        var result = await _sut.CreateRoleAsync(new CreateRoleRequestDto { Name = "Editor" });

        Assert.False(result.IsSuccess);
        _identityAdminGateway.Verify(g => g.CreateRoleAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact(DisplayName = "CreateRole: benzersiz isimle rol oluşturulur")]
    public async Task CreateRoleAsync_UniqueName_ReturnsSuccessWithId()
    {
        _identityAdminGateway.Setup(g => g.RoleExistsByNameAsync("Editor")).ReturnsAsync(false);
        _identityAdminGateway.Setup(g => g.CreateRoleAsync("Editor")).ReturnsAsync(42);

        var result = await _sut.CreateRoleAsync(new CreateRoleRequestDto { Name = "Editor" });

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Data);
    }

    [Fact(DisplayName = "DeleteRole: sistem rolü silinemez")]
    public async Task DeleteRoleAsync_SystemRole_ReturnsConflict()
    {
        var result = await _sut.DeleteRoleAsync(IdentitySeedData.MemberRoleId);

        Assert.False(result.IsSuccess);
        _identityAdminGateway.Verify(g => g.DeleteRoleAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact(DisplayName = "DeleteRole: kullanıcısı olan rol silinemez")]
    public async Task DeleteRoleAsync_RoleHasUsers_ReturnsConflict()
    {
        const int roleId = 100;
        _identityAdminDal.Setup(d => d.CountUsersInRoleAsync(roleId, It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var result = await _sut.DeleteRoleAsync(roleId);

        Assert.False(result.IsSuccess);
        _identityAdminGateway.Verify(g => g.DeleteRoleAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact(DisplayName = "DeleteRole: kullanıcısı olmayan sistem-dışı rol silinir")]
    public async Task DeleteRoleAsync_UnusedNonSystemRole_ReturnsSuccess()
    {
        const int roleId = 100;
        _identityAdminDal.Setup(d => d.CountUsersInRoleAsync(roleId, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _identityAdminGateway.Setup(g => g.DeleteRoleAsync(roleId)).ReturnsAsync(true);

        var result = await _sut.DeleteRoleAsync(roleId);

        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "SetRolePermissions: Admin rolünden roles.manage kaldırılamaz")]
    public async Task SetRolePermissionsAsync_RemovingRolesManageFromAdmin_ReturnsConflict()
    {
        _identityAdminGateway.Setup(g => g.RoleExistsByIdAsync(IdentitySeedData.AdminRoleId)).ReturnsAsync(true);

        var result = await _sut.SetRolePermissionsAsync(
            IdentitySeedData.AdminRoleId,
            new SetRolePermissionsRequestDto { Permissions = [IdentitySeedData.Permissions.ClubsRead] });

        Assert.False(result.IsSuccess);
        _identityAdminGateway.Verify(g => g.SetRolePermissionsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<string>>()), Times.Never);
    }

    [Fact(DisplayName = "SetRolePermissions: bilinmeyen izin kodu reddedilir")]
    public async Task SetRolePermissionsAsync_UnknownPermissionCode_ReturnsValidationError()
    {
        const int roleId = 100;
        _identityAdminGateway.Setup(g => g.RoleExistsByIdAsync(roleId)).ReturnsAsync(true);

        var result = await _sut.SetRolePermissionsAsync(roleId, new SetRolePermissionsRequestDto { Permissions = ["not.a.real.permission"] });

        Assert.False(result.IsSuccess);
        _identityAdminGateway.Verify(g => g.SetRolePermissionsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<string>>()), Times.Never);
    }

    [Fact(DisplayName = "SetRolePermissions: geçerli izin kümesi uygulanır")]
    public async Task SetRolePermissionsAsync_ValidPermissions_ReturnsSuccess()
    {
        const int roleId = 100;
        _identityAdminGateway.Setup(g => g.RoleExistsByIdAsync(roleId)).ReturnsAsync(true);

        var result = await _sut.SetRolePermissionsAsync(
            roleId, new SetRolePermissionsRequestDto { Permissions = [IdentitySeedData.Permissions.ClubsRead] });

        Assert.True(result.IsSuccess);
        _identityAdminGateway.Verify(
            g => g.SetRolePermissionsAsync(roleId, It.Is<IReadOnlyCollection<string>>(p => p.Contains(IdentitySeedData.Permissions.ClubsRead))),
            Times.Once);
    }

    [Fact(DisplayName = "SetUserRoles: çağıran kendi Admin rolünü kaldıramaz")]
    public async Task SetUserRolesAsync_CallerRemovesOwnAdminRole_ReturnsConflict()
    {
        _currentUser.Setup(c => c.UserId).Returns(5);
        _identityAdminGateway.Setup(g => g.GetUserRoleNamesAsync(5)).ReturnsAsync(new[] { IdentitySeedData.AdminRoleName });

        var result = await _sut.SetUserRolesAsync(5, new SetUserRolesRequestDto { RoleNames = [IdentitySeedData.MemberRoleName] });

        Assert.False(result.IsSuccess);
        _identityAdminGateway.Verify(g => g.SetUserRolesAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<string>>()), Times.Never);
    }

    [Fact(DisplayName = "SetUserRoles: başkasının rollerini değiştiren admin kısıtlanmaz")]
    public async Task SetUserRolesAsync_ValidRoles_ReturnsSuccess()
    {
        const int targetUserId = 7;
        _identityAdminGateway.Setup(g => g.RoleExistsByNameAsync(IdentitySeedData.MemberRoleName)).ReturnsAsync(true);
        _identityAdminGateway.Setup(g => g.SetUserRolesAsync(targetUserId, It.IsAny<IReadOnlyCollection<string>>())).ReturnsAsync(true);

        var result = await _sut.SetUserRolesAsync(targetUserId, new SetUserRolesRequestDto { RoleNames = [IdentitySeedData.MemberRoleName] });

        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "SetUserRoles: bilinmeyen rol adı NotFound döner")]
    public async Task SetUserRolesAsync_UnknownRoleName_ReturnsNotFound()
    {
        const int targetUserId = 7;
        _identityAdminGateway.Setup(g => g.RoleExistsByNameAsync("HayaliRol")).ReturnsAsync(false);

        var result = await _sut.SetUserRolesAsync(targetUserId, new SetUserRolesRequestDto { RoleNames = ["HayaliRol"] });

        Assert.False(result.IsSuccess);
        _identityAdminGateway.Verify(g => g.SetUserRolesAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<string>>()), Times.Never);
    }

    [Fact(DisplayName = "GetRolesPaged: her rolün izinleri ve sistem-rolü bayrağı doğru eşlenir")]
    public async Task GetRolesPagedAsync_MapsPermissionsAndSystemFlag()
    {
        var paged = new PagedResult<RoleRowDto>(
            [new RoleRowDto { Id = IdentitySeedData.MemberRoleId, Name = "Member" }, new RoleRowDto { Id = 100, Name = "Editor" }],
            totalCount: 2, pageIndex: 0, pageSize: 20);

        _identityAdminDal.Setup(d => d.GetRolesPagedAsync(0, 20, It.IsAny<CancellationToken>())).ReturnsAsync(paged);
        _identityAdminDal
            .Setup(d => d.GetRolePermissionsAsync(It.Is<IReadOnlyCollection<int>>(ids => ids.Contains(IdentitySeedData.MemberRoleId) && ids.Contains(100)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new RolePermissionRowDto { RoleId = IdentitySeedData.MemberRoleId, Permission = IdentitySeedData.Permissions.ClubsRead },
                new RolePermissionRowDto { RoleId = 100, Permission = IdentitySeedData.Permissions.RolesManage },
            ]);

        var result = await _sut.GetRolesPagedAsync(0, 20);

        Assert.True(result.IsSuccess);
        var memberRole = result.Data.Items.Single(r => r.Id == IdentitySeedData.MemberRoleId);
        var editorRole = result.Data.Items.Single(r => r.Id == 100);
        Assert.True(memberRole.IsSystemRole);
        Assert.False(editorRole.IsSystemRole);
        Assert.Contains(IdentitySeedData.Permissions.ClubsRead, memberRole.Permissions);
        Assert.Contains(IdentitySeedData.Permissions.RolesManage, editorRole.Permissions);
    }

    [Fact(DisplayName = "CreateUser: geçerli rollerle kullanıcı oluşturulur")]
    public async Task CreateUserAsync_ValidRoles_ReturnsSuccessWithId()
    {
        _identityAdminGateway.Setup(g => g.RoleExistsByNameAsync("Member")).ReturnsAsync(true);
        _identityAdminGateway
            .Setup(g => g.CreateUserAsync("new@test.local", "Str0ng!Pass", It.Is<IReadOnlyCollection<string>>(r => r.Contains("Member"))))
            .ReturnsAsync(77);

        var result = await _sut.CreateUserAsync(new CreateUserRequestDto { Email = "new@test.local", Password = "Str0ng!Pass", RoleNames = ["Member"] });

        Assert.True(result.IsSuccess);
        Assert.Equal(77, result.Data);
    }

    [Fact(DisplayName = "CreateUser: var olmayan role atanmak istenirse NotFound döner, kullanıcı oluşturulmaz")]
    public async Task CreateUserAsync_UnknownRole_ReturnsNotFound()
    {
        _identityAdminGateway.Setup(g => g.RoleExistsByNameAsync("Hayalet")).ReturnsAsync(false);

        var result = await _sut.CreateUserAsync(new CreateUserRequestDto { Email = "new@test.local", Password = "Str0ng!Pass", RoleNames = ["Hayalet"] });

        Assert.False(result.IsSuccess);
        _identityAdminGateway.Verify(g => g.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>()), Times.Never);
    }

    [Fact(DisplayName = "SetLockout: başka bir kullanıcı kilitlenebilir")]
    public async Task SetLockoutAsync_OtherUser_ReturnsSuccess()
    {
        _identityAdminGateway.Setup(g => g.SetLockoutAsync(123, true)).ReturnsAsync(true);

        var result = await _sut.SetLockoutAsync(123, new SetLockoutRequestDto { Locked = true });

        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "SetLockout: yönetici kendi hesabını kilitleyemez")]
    public async Task SetLockoutAsync_OwnAccount_ReturnsConflict()
    {
        var result = await _sut.SetLockoutAsync(999, new SetLockoutRequestDto { Locked = true });

        Assert.False(result.IsSuccess);
        _identityAdminGateway.Verify(g => g.SetLockoutAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
    }
}
