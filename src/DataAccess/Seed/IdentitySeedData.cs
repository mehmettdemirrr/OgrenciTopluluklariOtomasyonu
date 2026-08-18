using Core.Utilities.Security;
using Entities;
using Microsoft.AspNetCore.Identity;

namespace DataAccess.Seed;

/// <summary>
/// docs/MIMARI.md · A-27/K-17/Y-37: rol ve izin kataloğunun deterministik migration seed'i.
/// Sabit Id/ConcurrencyStamp değerleri kullanılır ki her migration üretiminde diff değişmesin.
/// Admin kullanıcısının kendisi burada seed edilmez — parola hash'i UserManager gerektirir,
/// bkz. Business/Concrete/IdentitySeeder.cs (runtime, idempotent).
/// </summary>
public static class IdentitySeedData
{
    public const int AdminRoleId = 1;
    public const int ClubOfficerRoleId = 2;
    public const int MemberRoleId = 3;

    public const string AdminRoleName = "Admin";
    public const string ClubOfficerRoleName = "ClubOfficer";
    public const string MemberRoleName = "Member";

    public static class Permissions
    {
        public const string ClubsRead = "clubs.read";
        public const string ClubsWrite = "clubs.write";
        public const string MembershipsRead = "memberships.read";
        public const string MembershipsWrite = "memberships.write";
        public const string EventsRead = "events.read";
        public const string EventsWrite = "events.write";
        public const string DiagnosticsProtected = "diagnostics.protected";
    }

    public static IEnumerable<ApplicationRole> Roles() =>
    [
        new()
        {
            Id = AdminRoleId, Name = AdminRoleName, NormalizedName = "ADMIN",
            ConcurrencyStamp = "11111111-1111-1111-1111-111111111111",
        },
        new()
        {
            Id = ClubOfficerRoleId, Name = ClubOfficerRoleName, NormalizedName = "CLUBOFFICER",
            ConcurrencyStamp = "22222222-2222-2222-2222-222222222222",
        },
        new()
        {
            Id = MemberRoleId, Name = MemberRoleName, NormalizedName = "MEMBER",
            ConcurrencyStamp = "33333333-3333-3333-3333-333333333333",
        },
    ];

    public static IEnumerable<IdentityRoleClaim<int>> RoleClaims() =>
    [
        Claim(1, AdminRoleId, Permissions.ClubsRead),
        Claim(2, AdminRoleId, Permissions.ClubsWrite),
        Claim(3, AdminRoleId, Permissions.MembershipsRead),
        Claim(4, AdminRoleId, Permissions.MembershipsWrite),
        Claim(5, AdminRoleId, Permissions.EventsRead),
        Claim(6, AdminRoleId, Permissions.EventsWrite),
        Claim(7, AdminRoleId, Permissions.DiagnosticsProtected),
        Claim(8, ClubOfficerRoleId, Permissions.ClubsRead),
        Claim(9, ClubOfficerRoleId, Permissions.MembershipsRead),
        Claim(10, ClubOfficerRoleId, Permissions.MembershipsWrite),
        Claim(11, ClubOfficerRoleId, Permissions.EventsRead),
        Claim(12, ClubOfficerRoleId, Permissions.EventsWrite),
        Claim(13, MemberRoleId, Permissions.ClubsRead),
        Claim(14, MemberRoleId, Permissions.EventsRead),
    ];

    private static IdentityRoleClaim<int> Claim(int id, int roleId, string permission) => new()
    {
        Id = id,
        RoleId = roleId,
        ClaimType = CurrentUserClaimTypes.Permission,
        ClaimValue = permission,
    };
}
