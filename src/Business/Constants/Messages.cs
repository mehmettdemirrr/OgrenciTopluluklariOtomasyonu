namespace Business.Constants;

/// <summary>docs/MIMARI.md · Y-29: kullanıcıya giden mesajlar tek yerde toplanır, koda gömülmez/tekrarlanmaz.</summary>
public static class Messages
{
    public const string ClubNotFound = "Topluluk bulunamadı.";
    public const string NotAStudent = "Bu işlem yalnızca öğrenci profiline sahip kullanıcılar tarafından yapılabilir.";
    public const string NoCurrentAcademicTerm = "Şu anda aktif bir akademik dönem tanımlı değil.";
    public const string ClubNotActive = "Bu topluluk şu anda üyelik başvurusuna kapalı.";
    public const string AlreadyClubMember = "Bu döneme ait bu toplulukta zaten bir üyeliğiniz var.";
    public const string DuplicatePendingApplication = "Bu topluluğa bu dönem için zaten bekleyen bir başvurunuz var.";
    public const string ApplicationSubmitted = "Başvurunuz alındı, danışman onayı bekleniyor.";
    public const string MembershipApplicationNotFound = "Başvuru bulunamadı.";
    public const string ApplicationAlreadyReviewed = "Bu başvuru için zaten bir karar verilmiş.";
    public const string NotClubAdvisor = "Bu işlemi yalnızca topluluğun danışmanı yapabilir.";
    public const string InvalidReviewDecision = "Karar yalnızca 'Onaylandı' veya 'Reddedildi' olabilir.";
    public const string ApplicationApproved = "Başvuru onaylandı.";
    public const string ApplicationRejected = "Başvuru reddedildi.";

    // Faz 6 — Dosya
    public const string FileTooLarge = "Dosya boyutu izin verilen sınırı (5 MB) aşıyor.";
    public const string UnsupportedFileType = "Desteklenmeyen dosya türü. Yalnızca JPEG, PNG veya WebP yüklenebilir.";
    public const string FileNotFound = "Dosya bulunamadı.";
    public const string EventNotFound = "Etkinlik bulunamadı.";
    public const string LogoUploaded = "Topluluk logosu güncellendi.";
    public const string PosterUploaded = "Etkinlik afişi güncellendi.";

    // Faz 6 — Rapor
    public const string ReportNotFound = "Rapor talebi bulunamadı.";
    public const string ReportNotReady = "Rapor henüz hazır değil.";
    public const string ReportFileMissing = "Rapor dosyası bulunamadı veya süresi dolmuş.";
    public const string ReportNotYours = "Bu rapor talebi size ait değil.";
    public const string ReportScopeDenied = "Bu kulüp/etkinlik için rapor talep etme yetkiniz yok.";
    public const string ReportScopeLost = "Bu raporu indirme yetkiniz artık bulunmuyor.";
    public const string ReportQueued = "Talebiniz kuyruğa alındı.";
    public const string InvalidReportParameters = "Rapor parametreleri geçersiz.";

    // Faz 7 — Yetki matrisi (K-17)
    public const string InvalidRoleName = "Rol adı yalnızca harf, rakam, boşluk, nokta, tire ve alt çizgi içerebilir.";
    public const string RoleNotFound = "Rol bulunamadı.";
    public const string RoleNameTaken = "Bu isimde bir rol zaten var.";
    public const string SystemRoleCannotBeDeleted = "Sistem rolleri silinemez.";
    public const string RoleHasUsers = "Bu role atanmış kullanıcılar olduğu için rol silinemez.";
    public const string AdminRoleMustKeepRolesManage = "Admin rolünden 'roles.manage' izni kaldırılamaz.";
    public const string CannotRemoveOwnAdminRole = "Kendi Admin rolünüzü kaldıramazsınız.";
    public const string UnknownPermissionCode = "Bilinmeyen izin kodu.";
    public const string UserNotFound = "Kullanıcı bulunamadı.";
    public const string RoleCreated = "Rol oluşturuldu.";
    public const string RoleDeleted = "Rol silindi.";
    public const string PermissionsUpdated = "Rol izinleri güncellendi.";
    public const string RolesUpdated = "Kullanıcı rolleri güncellendi.";
}
