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

    /// <summary>docs/MIMARI.md · A-51: {0} = yeni döneme taşınan üyelik sayısı.</summary>
    public const string AcademicTermSetCurrentWithRollover = "Güncel dönem güncellendi. {0} üyelik yeni döneme taşındı.";
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

    // Faz 30 — Etkinlik katılım kitlesi (K-38, Y-72)
    public const string EventForClubMembersOnly = "Bu etkinliğe yalnızca topluluğun üyeleri katılabilir.";
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

    // Faz 26 — Kişi kimliği ve kullanıcı yönetimi (K-32, A-56, A-57, Y-67)
    public const string CannotDeleteOwnAccount = "Kendi hesabınızı silemezsiniz.";
    public const string UserDeleted = "Kullanıcı silindi.";
    public const string UserHasAdvisedClubs = "Bu kullanıcı bir topluluğun danışmanı olduğu için silinemez. Önce topluluğa yeni bir danışman atayın.";
    public const string UserHasClubMemberships = "Bu kullanıcının topluluk üyelikleri olduğu için silinemez. Önce üyeliklerini sonlandırın.";
    public const string UserHasEventParticipations = "Bu kullanıcının etkinlik kayıtları olduğu için silinemez.";
    public const string UserHasApplications = "Bu kullanıcının üyelik başvuruları olduğu için silinemez.";
    public const string ProfileDepartmentRequired = "Seçilen rol için bölüm zorunludur.";
    public const string StudentProfileRequired = "Öğrenci rolü için öğrenci numarası ve kayıt yılı zorunludur.";
    public const string StudentNumberTaken = "Bu öğrenci numarası zaten kayıtlı.";
    public const string AdvisorProfileRequired = "Danışman rolü için akademik unvan zorunludur.";

    // Faz 27 — Danışman yönetimi (K-33)
    public const string AcademicStaffAlreadyExists = "Bu kullanıcının zaten bir akademik personel profili var.";
    public const string AcademicStaffCreated = "Akademik personel kaydı oluşturuldu.";
    public const string AcademicStaffUpdated = "Akademik personel kaydı güncellendi.";
    public const string AcademicStaffDeleted = "Akademik personel kaydı silindi.";
    public const string AcademicStaffHasClubs = "Bu personel bir topluluğa danışmanlık yaptığı için silinemez. Önce topluluğa yeni bir danışman atayın.";
    public const string ClubAdvisorChanged = "Topluluk danışmanı değiştirildi.";

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

    // Faz 31 — Başvuru takvimi (K-39, Y-73)
    // Y-73: mesaj sebebi VE takvimin nerede görüleceğini söyler. Kesin tarihler
    // GET /club-applications/window ucundan gelir — Y-25 gereği mesajın kendisi nötr kalır.
    public const string ClubApplicationsClosed =
        "Topluluk kurma başvuruları şu anda kapalı. Başvuru takvimini Başvurularım sayfasından görebilirsiniz.";

    public const string ClubApplicationWindowUpdated = "Başvuru takvimi güncellendi.";

    // Faz 32 — Topluluk kategorisi (K-35, A-60)
    public const string ClubCategoryNotFound = "Topluluk kategorisi bulunamadı.";
    public const string ClubCategoryAlreadyExists = "Bu isimde bir topluluk kategorisi zaten var.";
    public const string ClubCategoryUpdated = "Topluluk kategorisi güncellendi.";
    public const string ClubCategoryDeleted = "Topluluk kategorisi silindi.";
    public const string ClubCategoryInUse = "Bu kategori kullanımda olduğu için silinemez.";

    // Faz 33 — Kuruluş evrakları (K-37, A-62, A-63, A-64)
    // A-64: iki farklı bağlam, iki farklı mesaj. UnsupportedFileType görsel yolunda kalır.
    public const string UnsupportedDocumentFileType = "Desteklenmeyen dosya türü. Evraklar yalnızca PDF olarak yüklenebilir.";

    public const string ClubDocumentTypeNotFound = "Evrak tipi bulunamadı.";
    public const string ClubDocumentTypeCodeTaken = "Bu kodla bir evrak tipi zaten var.";
    public const string ClubDocumentTypeUpdated = "Evrak tipi güncellendi.";
    public const string ClubDocumentTypeDeleted = "Evrak tipi silindi.";
    public const string ClubDocumentTypeInUse = "Bu evrak tipi başvurularda kullanıldığı için silinemez. Yürürlükten kaldırmak için pasife alın.";

    public const string MissingRequiredClubDocuments = "Zorunlu evrakların tamamı yüklenmeden başvuru gönderilemez.";
    public const string UnknownClubDocumentType = "Gönderilen evrak tiplerinden biri tanımlı değil.";
    public const string DuplicateClubDocumentUpload = "Aynı evrak tipi için birden fazla dosya gönderildi.";
    public const string ClubApplicationDocumentForbidden = "Bu evrağı görüntüleme yetkiniz yok.";

    // Faz 34 — Dinamik topluluk içi roller (K-36, A-61, Y-69)
    public const string ClubRoleDefinitionNotFound = "Rol tanımı bulunamadı.";
    public const string ClubRoleDefinitionNameTaken = "Bu toplulukta bu isimde bir rol tanımı zaten var.";
    public const string ClubRoleDefinitionCreated = "Rol tanımı eklendi.";
    public const string ClubRoleDefinitionUpdated = "Rol tanımı güncellendi.";
    public const string ClubRoleDefinitionDeleted = "Rol tanımı silindi.";
    public const string ClubRoleDefinitionInUse = "Bu unvanı taşıyan üyeler var; önce onları başka bir unvana taşıyın.";

    public const string ClubRoleDefinitionWouldCreateSecondPresident =
        "Bu unvanı birden fazla üye taşıyor; Başkan seviyesine yükseltilemez (bir toplulukta tek başkan olur).";
}
