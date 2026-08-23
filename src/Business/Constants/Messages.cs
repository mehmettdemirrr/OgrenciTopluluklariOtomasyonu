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

    // Faz 7 — Referans verisi ve dönem yönetimi
    public const string FacultyNotFound = "Fakülte bulunamadı.";
    public const string FacultyAlreadyExists = "Bu isimde bir fakülte zaten kayıtlı.";
    public const string DepartmentAlreadyExists = "Bu isimde bir bölüm bu fakültede zaten kayıtlı.";
    public const string AcademicTermNotFound = "Akademik dönem bulunamadı.";
    public const string AcademicTermNameTaken = "Bu isimde bir akademik dönem zaten var.";
    public const string AcademicTermAlreadyCurrent = "Bu dönem zaten güncel dönem.";
    public const string AcademicTermSetCurrent = "Güncel dönem güncellendi.";
    public const string InvalidAcademicTermDateRange = "Bitiş tarihi başlangıç tarihinden sonra olmalıdır.";

    // Faz 13 — Referans veri olgunluğu (A-12)
    public const string FacultyUpdated = "Fakülte güncellendi.";
    public const string DepartmentUpdated = "Bölüm güncellendi.";
    public const string DepartmentDeleted = "Bölüm silindi.";
    public const string DepartmentInUse = "Bu bölüm kullanımda olduğu için silinemiyor (kayıtlı öğrenci var).";
    public const string AcademicTermUpdated = "Akademik dönem güncellendi.";

    // Faz 7 — Etkinlik onay kuyruğu
    public const string NotClubAdvisorOrOfficer = "Bu işlem için bu topluluğun danışmanı ya da yetkilisi/başkanı olmanız gerekir.";
    public const string EventNotInDraft = "Etkinlik taslak durumunda değil.";
    public const string EventNotPendingApproval = "Etkinlik onay bekleyen durumda değil.";
    public const string EventSubmitted = "Etkinlik onaya gönderildi.";
    public const string EventPublished = "Etkinlik yayınlandı.";
    public const string EventRejected = "Etkinlik reddedildi.";
    public const string InvalidEventDecision = "Karar yalnızca 'Yayınlandı' veya 'Reddedildi' olabilir.";

    // Faz 9 — Topluluk yönetimi ve üye rolleri (K-21, A-39)
    public const string ClubNameTaken = "Bu isimde bir topluluk zaten kayıtlı.";
    public const string AdvisorNotFound = "Danışman bulunamadı.";
    public const string ClubCreated = "Topluluk oluşturuldu.";
    public const string ClubUpdated = "Topluluk güncellendi.";
    public const string ClubStatusUpdated = "Topluluk durumu güncellendi.";
    public const string ClubHasPendingApplications = "Bekleyen üyelik başvurusu olan bir topluluk pasife alınamaz.";
    public const string ClubMembershipNotFound = "Üyelik kaydı bulunamadı.";
    public const string NotClubAdvisorOrPresident = "Bu işlem için bu topluluğun danışmanı ya da başkanı olmanız gerekir.";
    public const string ClubAlreadyHasPresident = "Bu toplulukta bu dönem için zaten bir başkan var.";
    public const string CannotChangeOwnPresidentRole = "Kendi başkanlık rolünüzü kaldıramaz veya değiştiremezsiniz.";
    public const string ClubRoleUpdated = "Üye rolü güncellendi.";
    public const string ClubMemberRemoved = "Üye topluluktan çıkarıldı.";

    // Faz 10 — Etkinlik katılımı ve duyurular (K-22, K-23, A-38, A-43)
    public const string EventNotOpenForRegistration = "Bu etkinlik şu anda kayıt için uygun değil.";
    public const string AlreadyRegisteredForEvent = "Bu etkinliğe zaten kayıtlısınız.";
    public const string EventCapacityFull = "Bu etkinliğin kontenjanı dolu.";
    public const string EventRegistered = "Etkinliğe kaydınız alındı.";
    public const string NotRegisteredForEvent = "Bu etkinliğe kaydınız bulunmuyor.";
    public const string EventRegistrationCancelled = "Etkinlik kaydınız iptal edildi.";
    public const string EventCannotBeUpdated = "Yalnızca taslak veya reddedilmiş etkinlikler düzenlenebilir.";
    public const string EventUpdated = "Etkinlik güncellendi.";
    public const string EventCannotBeDeleted = "Yalnızca taslak durumundaki etkinlikler silinebilir.";
    public const string EventDeleted = "Etkinlik silindi.";
    public const string AnnouncementNotFound = "Duyuru bulunamadı.";
    public const string AnnouncementCreated = "Duyuru yayınlandı.";
    public const string AnnouncementUpdated = "Duyuru güncellendi.";
    public const string AnnouncementDeleted = "Duyuru kaldırıldı.";

    // Faz 11 — Hesap yaşam döngüsü (K-03, A-40)
    public const string EmailAlreadyRegistered = "Bu e-posta adresiyle zaten bir hesap var.";
    public const string DepartmentNotFound = "Bölüm bulunamadı.";
    public const string WeakPassword = "Parola en az 6 karakter olmalı; büyük harf, küçük harf, rakam ve alfanumerik olmayan bir karakter içermelidir.";
    public const string RegistrationSucceeded = "Kaydınız alındı. Hesabınızı etkinleştirmek için e-postanıza gönderilen bağlantıya tıklayın.";
    public const string EmailNotConfirmed = "Hesabınız henüz doğrulanmamış. Lütfen e-postanızı kontrol edin.";
    public const string InvalidOrExpiredToken = "Bağlantı geçersiz veya süresi dolmuş.";
    public const string EmailConfirmedSuccessfully = "E-posta adresiniz doğrulandı, artık giriş yapabilirsiniz.";
    public const string ConfirmationResent = "E-postanız sistemde kayıtlıysa, doğrulama bağlantısı tekrar gönderildi.";
    public const string PasswordResetRequested = "E-postanız sistemde kayıtlıysa, şifre sıfırlama bağlantısı gönderildi.";
    public const string PasswordResetSucceeded = "Parolanız güncellendi, yeni parolanızla giriş yapabilirsiniz.";
    public const string CurrentPasswordIncorrect = "Mevcut parolanız hatalı.";
    public const string PasswordChanged = "Parolanız güncellendi.";
    public const string UserCreated = "Kullanıcı oluşturuldu.";
    public const string UserCreationFailed = "Kullanıcı oluşturulamadı.";
    public const string LockoutUpdated = "Kullanıcı durumu güncellendi.";
    public const string CannotLockOwnAccount = "Kendi hesabınızı kilitleyemezsiniz.";

    // Faz 12 — Dashboard (K-25)
    public const string DashboardAccessDenied = "Panel özetini görüntülemek için oturum açmanız gerekir.";

    // Faz 20 — Etkinlik iptali (K-22, A-49, Y-61)
    public const string OnlyPublishedEventsCanBeCancelled = "Yalnızca yayındaki etkinlikler iptal edilebilir.";
    public const string EventCancelled = "Etkinlik iptal edildi, kayıtlı katılımcılara bildirim gönderiliyor.";

    // Faz 19 — Öğrenci self-servisi (K-25'in tamamlanması, A-48)
    public const string ApplicationNotYours = "Bu başvuru size ait değil.";
    public const string ApplicationWithdrawn = "Başvurunuz geri çekildi.";
    public const string LastPresidentCannotLeave = "Topluluğun tek başkanı olduğunuz için ayrılamazsınız; önce başka bir başkan atanmalı.";
    public const string ClubLeft = "Topluluktan ayrıldınız.";
    public const string ProfileUpdated = "Profil bilgileriniz güncellendi.";

    // Faz 17 — Topluluk kurma başvurusu (K-29, A-45)
    public const string DuplicatePendingClubApplication = "Bu dönem için zaten bekleyen bir topluluk kurma başvurunuz var.";
    public const string ClubApplicationSubmitted = "Topluluk kurma başvurunuz alındı, yönetici onayı bekleniyor.";
    public const string ClubApplicationNotFound = "Topluluk kurma başvurusu bulunamadı.";
    public const string ClubApplicationApproved = "Topluluk kurma başvurusu onaylandı, topluluk oluşturuldu.";
    public const string ClubApplicationRejected = "Topluluk kurma başvurusu reddedildi.";
}
