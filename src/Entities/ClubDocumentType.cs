using Core.Entities;

namespace Entities;

/// <summary>
/// docs/MIMARI.md · K-37/A-62: kuruluş evrakı tipi. Referans verisi — hard delete serbest (A-12),
/// kullanımdaysa FK Restrict 409 üretir. IsActive = false, geçmiş başvuruları bozmadan bir formu
/// yürürlükten kaldırmanın yoludur.
/// </summary>
public sealed class ClubDocumentType : IEntity
{
    public int Id { get; set; }

    /// <summary>Kurumsal form kodu, ör. "FR-0230". Benzersiz.</summary>
    public required string Code { get; set; }

    public required string Name { get; set; }

    /// <summary>docs/MIMARI.md · Y-71: başvuru formundaki `*` işaretinin ve bütünlük kontrolünün tek kaynağı.</summary>
    public bool IsRequired { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>
    /// Boş kurumsal şablon (Word/PDF). A-63 Protected kuralı doldurulmuş başvuru evrakına aittir;
    /// boş form Public saklanır. Silinirse FK SetNull.
    /// </summary>
    public int? TemplateFileId { get; set; }
}
