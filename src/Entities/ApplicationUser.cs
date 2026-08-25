using Microsoft.AspNetCore.Identity;

namespace Entities;

/// <summary>docs/MIMARI.md · A-09/A-11/A-28: Identity kullanıcısı, int anahtarlı, davranışsız.</summary>
public sealed class ApplicationUser : IdentityUser<int>
{
    /// <summary>
    /// docs/MIMARI.md · A-56 (K-32): kişi adı. v5.0'a kadar sistemde ad soyad hiç yoktu —
    /// bu yüzden danışman seçici öğrencilere <b>e-posta</b> gösteriyordu (PLAN-V3 §17'de bilinçli
    /// taviz olarak kaydedilmişti).
    ///
    /// Nullable: mevcut hesaplar geçersiz duruma düşmesin, boşsa arayüz e-postaya düşer.
    /// <b>Y-58:</b> bu alan anonim vitrin uçlarından (<c>/api/public/*</c>) asla dönmez.
    /// </summary>
    public string? FirstName { get; set; }

    /// <inheritdoc cref="FirstName"/>
    public string? LastName { get; set; }

    // A-09/A-28: burada hesaplanmış "DisplayName" YOK — entity davranışsız kalır ve EF'in
    // eşlemeye çalışacağı fantom bir sütun doğmaz. Ad soyadın e-postaya düşmesi DTO/DAL
    // projeksiyonlarının işidir.
}
