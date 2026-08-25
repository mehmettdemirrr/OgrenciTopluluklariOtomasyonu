namespace Business.DTOs.Reference;

/// <summary>
/// docs/PLAN-V3.md §17.4: AcademicStaffListItemDto'dan bilinçli olarak ayrı — reference.manage
/// yerine yalnızca kimlik doğrulaması ister (öğrenci danışman seçebilsin diye). Ayrı DTO ailesi,
/// admin ucuna sonradan eklenecek bir alanın buradan sessizce sızmasını yapısal olarak engeller.
/// </summary>
public sealed class SelectableAcademicStaffDto
{
    public int Id { get; set; }

    public required string Title { get; set; }

    /// <summary>
    /// docs/MIMARI.md · A-56: <b>e-posta bu DTO'dan kaldırıldı.</b> v5.0'a kadar burada e-posta
    /// vardı çünkü gösterilecek başka alan yoktu — yani herhangi bir öğrenci tüm danışmanların
    /// e-postasını görebiliyordu (PLAN-V3 §17.4'te bilinçli taviz olarak kaydedilmişti).
    ///
    /// Ad soyad boş olan eski kayıtlarda e-postaya düşülür; seçici kullanılamaz hâle gelmesin diye.
    /// Faz 26 sonrası oluşturulan her danışmanın adı var, yani bu geçiş dönemine özgüdür.
    /// </summary>
    public required string FullName { get; set; }
}
