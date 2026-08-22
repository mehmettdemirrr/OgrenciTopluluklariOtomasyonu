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
    public const int AdvisorRoleId = 4;

    public const string AdminRoleName = "Admin";
    public const string ClubOfficerRoleName = "ClubOfficer";
    public const string MemberRoleName = "Member";

    /// <summary>
    /// Faz 5: danışman (AcademicStaff) rolü. ClubOfficer yeniden kullanılmadı — ClubOfficer,
    /// öğrencinin kulüp-içi rütbesiyle (ClubRole enum) aynı adı taşıdığı için akademik personeli
    /// bu role atamak K-17 yetki matrisi ekranında yanıltıcı olurdu.
    /// </summary>
    public const string AdvisorRoleName = "Advisor";

    public static class Permissions
    {
        public const string ClubsRead = "clubs.read";
        public const string ClubsWrite = "clubs.write";
        public const string MembershipsRead = "memberships.read";
        public const string MembershipsWrite = "memberships.write";
        public const string EventsRead = "events.read";
        public const string EventsWrite = "events.write";
        public const string DiagnosticsProtected = "diagnostics.protected";
        public const string HangfireDashboard = "hangfire.dashboard";

        /// <summary>Faz 6: rapor talebi + kendi kapsamında özet/indirme.</summary>
        public const string ReportsRead = "reports.read";

        /// <summary>Faz 6: tüm kulüpleri kapsayan özet — rol bypass'ı değil, açık izin (Admin).</summary>
        public const string ReportsReadAll = "reports.read.all";

        /// <summary>Faz 6: logo/afiş yükleme (Y-40 riski taşıyan yetenek).</summary>
        public const string FilesUpload = "files.upload";

        /// <summary>Faz 7: K-17 yetki matrisinin tamamı (rol + izin + kullanıcı-rol ataması).</summary>
        public const string RolesManage = "roles.manage";

        /// <summary>Faz 7: fakülte, bölüm, akademik dönem yönetimi (A-12/A-27 tek kategori).</summary>
        public const string ReferenceManage = "reference.manage";

        /// <summary>Faz 7: etkinlik onay/ret — yazarı (events.write) onaylayandan ayırır (A-25).</summary>
        public const string EventsApprove = "events.approve";

        /// <summary>Faz 10: kulüp duyurusu oluştur/düzenle/sil (A-43).</summary>
        public const string AnnouncementsWrite = "announcements.write";

        /// <summary>Faz 10: kulübe bağlı olmayan sistem duyurusu (ClubId = null) — Admin'e özgü.</summary>
        public const string AnnouncementsGlobal = "announcements.global";

        /// <summary>Faz 13: denetim izi görüntüleme (K-12) — yalnız Admin.</summary>
        public const string AuditRead = "audit.read";
    }

    /// <summary>Y-03: bu roller silinemez/yeniden adlandırılamaz; izinleri değiştirilebilir.</summary>
    public static readonly IReadOnlyCollection<int> SystemRoleIds = [AdminRoleId, ClubOfficerRoleId, MemberRoleId, AdvisorRoleId];

    public static bool IsSystemRole(int roleId) => SystemRoleIds.Contains(roleId);

    /// <summary>K-17 "Yeni İzinler" ekranının kaynağı — izin kodu koddan gelir, sabit frontend listesi değil.</summary>
    public static IReadOnlyCollection<string> AllPermissionCodes() =>
    [
        Permissions.ClubsRead,
        Permissions.ClubsWrite,
        Permissions.MembershipsRead,
        Permissions.MembershipsWrite,
        Permissions.EventsRead,
        Permissions.EventsWrite,
        Permissions.DiagnosticsProtected,
        Permissions.HangfireDashboard,
        Permissions.ReportsRead,
        Permissions.ReportsReadAll,
        Permissions.FilesUpload,
        Permissions.RolesManage,
        Permissions.ReferenceManage,
        Permissions.EventsApprove,
        Permissions.AnnouncementsWrite,
        Permissions.AnnouncementsGlobal,
        Permissions.AuditRead,
    ];

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
        new()
        {
            Id = AdvisorRoleId, Name = AdvisorRoleName, NormalizedName = "ADVISOR",
            ConcurrencyStamp = "44444444-4444-4444-4444-444444444444",
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
        Claim(15, AdminRoleId, Permissions.HangfireDashboard),
        Claim(16, AdvisorRoleId, Permissions.ClubsRead),
        Claim(17, AdvisorRoleId, Permissions.MembershipsRead),
        Claim(18, AdvisorRoleId, Permissions.MembershipsWrite),
        Claim(19, AdminRoleId, Permissions.ReportsRead),
        Claim(20, AdminRoleId, Permissions.ReportsReadAll),
        Claim(21, AdminRoleId, Permissions.FilesUpload),
        Claim(22, ClubOfficerRoleId, Permissions.ReportsRead),
        Claim(23, ClubOfficerRoleId, Permissions.FilesUpload),
        Claim(24, AdvisorRoleId, Permissions.ReportsRead),
        Claim(25, AdvisorRoleId, Permissions.FilesUpload),

        // Faz 7 — K-17 yetki matrisi + referans veri + etkinlik onay kuyruğu.
        Claim(26, AdminRoleId, Permissions.RolesManage),
        Claim(27, AdminRoleId, Permissions.ReferenceManage),
        Claim(28, AdminRoleId, Permissions.EventsApprove),
        Claim(29, AdvisorRoleId, Permissions.EventsRead),
        Claim(30, AdvisorRoleId, Permissions.EventsWrite),
        Claim(31, AdvisorRoleId, Permissions.EventsApprove),

        // Faz 10 — Etkinlik katılımı ve duyurular.
        Claim(32, AdminRoleId, Permissions.AnnouncementsWrite),
        Claim(33, AdminRoleId, Permissions.AnnouncementsGlobal),
        Claim(34, ClubOfficerRoleId, Permissions.AnnouncementsWrite),
        Claim(35, AdvisorRoleId, Permissions.AnnouncementsWrite),

        // Faz 13 — Denetim izi ve referans veri olgunluğu.
        Claim(36, AdminRoleId, Permissions.AuditRead),
    ];

    private static IdentityRoleClaim<int> Claim(int id, int roleId, string permission) => new()
    {
        Id = id,
        RoleId = roleId,
        ClaimType = CurrentUserClaimTypes.Permission,
        ClaimValue = permission,
    };
}
