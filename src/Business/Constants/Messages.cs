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
}
