namespace Core.Entities;

/// <summary>
/// docs/MIMARI.md · Y-16: olay kayıtları (üyelik, başvuru, etkinlik, katılım, duyuru) fiziksel
/// silinmez. Y-44: soft delete de audit interceptor'ı tarafından "silme" olarak kaydedilir —
/// bu arayüz, interceptor'ın IsDeleted geçişini (false→true) Update değil Delete olarak
/// tanımasını sağlar.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }

    DateTime? DeletedAtUtc { get; set; }
}
