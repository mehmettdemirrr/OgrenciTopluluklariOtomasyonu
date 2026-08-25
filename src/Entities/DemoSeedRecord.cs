using Core.Entities;

namespace Entities;

/// <summary>
/// docs/MIMARI.md · Y-68 (K-34, A-58): <see cref="DemoSeedRecord"/>, <c>DemoDataSeeder</c>'ın
/// ürettiği her kaydın künyesidir.
///
/// Gerekçe: işaretsiz demo veri ile gerçek veri bir kez karıştığında ayrıştırılamaz. Ad öneki
/// gibi bir işaret ("Demo — ...") kullanıcının adı düzenlemesiyle kaybolur; ayrı bir künye
/// tablosu ise <b>ne olursa olsun</b> hangi satırın demo olduğunu söyler. Sıfırlama komutu
/// yalnızca bu tabloda adı geçen satırları siler — üretim verisine dokunamaz.
///
/// Y-16 dışıdır: künye soft delete edilmez, çünkü silinmiş bir künye hedef satırı
/// yetim bırakırdı.
/// </summary>
public sealed class DemoSeedRecord : IEntity
{
    public int Id { get; set; }

    /// <summary>Hedef entity'nin tür adı — <see cref="DemoEntityTypes"/> sabitlerinden biri.</summary>
    public required string EntityType { get; set; }

    /// <summary>Hedef satırın birincil anahtarı.</summary>
    public int EntityId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// <see cref="DemoSeedRecord.EntityType"/> için izinli değerler. Sıralama aynı zamanda
/// <b>silme sırasıdır</b>: yabancı anahtar bağımlılıklarında çocuk önce gelir.
/// </summary>
public static class DemoEntityTypes
{
    public const string EventParticipation = nameof(EventParticipation);
    public const string Announcement = nameof(Announcement);
    public const string Event = nameof(Event);
    public const string MembershipApplication = nameof(MembershipApplication);
    public const string ClubApplication = nameof(ClubApplication);
    public const string ClubMembership = nameof(ClubMembership);
    public const string Club = nameof(Club);
    public const string AcademicStaff = nameof(AcademicStaff);
    public const string Student = nameof(Student);
    public const string ApplicationUser = nameof(ApplicationUser);

    /// <summary>Silme sırası — çocuktan ebeveyne.</summary>
    public static string[] DeletionOrder() =>
    [
        EventParticipation,
        Announcement,
        Event,
        MembershipApplication,
        ClubApplication,
        ClubMembership,
        Club,
        AcademicStaff,
        Student,
        ApplicationUser,
    ];
}
