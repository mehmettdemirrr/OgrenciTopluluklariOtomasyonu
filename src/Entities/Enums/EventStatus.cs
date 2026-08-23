namespace Entities.Enums;

/// <summary>docs/MIMARI.md · A-25: taslak → onay bekliyor → yayında/reddedildi → iptal edildi.</summary>
public enum EventStatus
{
    Draft = 0,
    PendingApproval = 1,
    Published = 2,
    Rejected = 3,

    /// <summary>
    /// docs/MIMARI.md · A-49/Y-61: yayınlanmış etkinlik **silinmez, iptal edilir** — katılımcı
    /// kayıtları durur, öğrenci kaydını iptal rozetiyle görmeye devam eder. Yeni kayıt kabul
    /// edilmez ve anonim vitrinde listelenmez.
    /// </summary>
    Cancelled = 4,
}
