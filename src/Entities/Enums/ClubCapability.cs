namespace Entities.Enums;

/// <summary>
/// docs/MIMARI.md · K-36/A-68/Y-69: kulüp içi yetki matrisi. <b>Kapalı küme</b> — yeni kapasite
/// eklemek kod değişikliği ve migration ister; arayüzden tanımlanamaz. Bu altı değer,
/// <c>ClubRole</c>'ün bugüne kadar yetki kararı verdiği yerlerin TAMAMIDIR.
///
/// <b>Y-75:</b> bir kapasiteyi işaretlemek, kullanıcının Identity izninin vermediği bir şeyi
/// VERMEZ — uçtaki [SecuredOperation] birinci kapı olarak yerinde kalır, bu ikinci kapıdır.
///
/// <b>Y-06:</b> sayısal değerler sabittir — veritabanında int olarak saklanır.
/// </summary>
[Flags]
public enum ClubCapability
{
    None = 0,

    /// <summary>Üye listesini görebilir (ClubMemberManager.GetMembersPagedAsync).</summary>
    MembersView = 1,

    /// <summary>Üye rolü atayabilir, üye çıkarabilir, rol tanımı yönetebilir. Bugünkü President ayrıcalığı.</summary>
    MembersManage = 2,

    /// <summary>Etkinlik oluşturabilir, düzenleyebilir, silebilir (EventManager).</summary>
    EventsManage = 4,

    /// <summary>Etkinlik katılımcı listesini görebilir (EventParticipationManager).</summary>
    EventParticipantsView = 8,

    /// <summary>Duyuru yazabilir (AnnouncementManager).</summary>
    AnnouncementsManage = 16,

    /// <summary>Kulübün raporlarını alabilir (ReportScopeResolver).</summary>
    ReportsView = 32,
}

/// <summary>
/// docs/MIMARI.md · A-68: unvansız üyenin ve migration geri doldurmasının kapasitesi.
/// Bu eşleme <b>Faz 34 öncesi davranışı birebir korur</b> — değiştirmek yetki yüzeyini değiştirmektir.
/// </summary>
public static class ClubCapabilityDefaults
{
    /// <summary>Bugünkü "Officer/President geçer" kapılarının tamamı — üye yönetimi hariç.</summary>
    private const ClubCapability OfficerCapabilities =
        ClubCapability.MembersView
        | ClubCapability.EventsManage
        | ClubCapability.EventParticipantsView
        | ClubCapability.AnnouncementsManage
        | ClubCapability.ReportsView;

    public static ClubCapability ForRole(ClubRole role) => role switch
    {
        ClubRole.President => OfficerCapabilities | ClubCapability.MembersManage,
        ClubRole.Officer => OfficerCapabilities,
        _ => ClubCapability.None,
    };
}
