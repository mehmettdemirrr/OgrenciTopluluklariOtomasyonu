using DataAccess.Seed;

namespace Business.Constants;

/// <summary>
/// docs/MIMARI.md · K-17: izin kodu koddan gelir (bkz. IdentitySeedData.AllPermissionCodes) —
/// bu sözlük yalnızca K-17 ekranı için Türkçe ad/açıklama eşlemesidir, izin listesinin kendisi değil.
/// </summary>
public static class PermissionCatalog
{
    public static readonly IReadOnlyDictionary<string, (string DisplayName, string Description)> Descriptions =
        new Dictionary<string, (string DisplayName, string Description)>
        {
            [IdentitySeedData.Permissions.ClubsRead] = ("Toplulukları Görüntüle", "Topluluk listesini ve detaylarını görüntüler."),
            [IdentitySeedData.Permissions.ClubsWrite] = ("Toplulukları Yönet", "Topluluk bilgilerini oluşturur/günceller."),
            [IdentitySeedData.Permissions.MembershipsRead] = ("Üyelik Başvurularını Görüntüle", "Bekleyen üyelik başvurularını görüntüler."),
            [IdentitySeedData.Permissions.MembershipsWrite] = ("Üyelik Başvurularını Yönet", "Üyelik başvurularını onaylar/reddeder."),
            [IdentitySeedData.Permissions.EventsRead] = ("Etkinlikleri Görüntüle", "Yayındaki etkinlikleri görüntüler."),
            [IdentitySeedData.Permissions.EventsWrite] = ("Etkinlik Oluştur", "Etkinlik oluşturur ve onaya gönderir."),
            [IdentitySeedData.Permissions.DiagnosticsProtected] = ("Tanılama", "Korumalı tanılama uçlarına erişir."),
            [IdentitySeedData.Permissions.HangfireDashboard] = ("Hangfire Paneli", "Arka plan iş kuyruğu panelini görüntüler."),
            [IdentitySeedData.Permissions.ReportsRead] = ("Rapor Talep Et", "Kendi kapsamında rapor talep eder/indirir."),
            [IdentitySeedData.Permissions.ReportsReadAll] = ("Tüm Raporlar", "Tüm kulüpleri kapsayan raporlara erişir."),
            [IdentitySeedData.Permissions.FilesUpload] = ("Dosya Yükle", "Topluluk logosu / etkinlik afişi yükler."),
            [IdentitySeedData.Permissions.RolesManage] = ("Yetki Matrisi", "Rolleri, izinleri ve kullanıcı-rol atamalarını yönetir."),
            [IdentitySeedData.Permissions.ReferenceManage] = ("Referans Verisi", "Fakülte, bölüm ve akademik dönemleri yönetir."),
            [IdentitySeedData.Permissions.EventsApprove] = ("Etkinlik Onayı", "Etkinlikleri onaylar veya reddeder."),
        };
}
