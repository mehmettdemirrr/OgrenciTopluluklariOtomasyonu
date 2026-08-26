using Entities;
using Entities.Enums;

namespace DataAccess.Seed;

/// <summary>
/// docs/MIMARI.md · A-27/A-58 (K-34): kurumsal referans verisi — üniversitenin 19 fakültesi ve
/// 121 bölümü.
///
/// <b>Bu katalog HasData'ya verilMEZ</b> (gerekçe: AppDbContext.OnModelCreating). Migration aynı
/// listeyi ada dayalı, idempotent SQL olarak yazar; buradaki dizi ise testlerin doğrulama
/// kaynağıdır — migration ile katalog ayrışırsa test kırılır.
///
/// Sabit kalan tek şey Id 1'dir: Faz 28 öncesi seed <c>Faculty Id=1</c> ve <c>Department Id=1</c>
/// satırlarını üretmişti ve demo öğrencinin <c>DepartmentId = 1</c> yabancı anahtarı buna bağlı.
/// Bu satırlar silinmez; fakülte yalnızca yeniden adlandırılır.
/// </summary>
public static class DomainSeedData
{
    public const int FacultyId = 1;
    public const int DepartmentId = 1;
    public const int AcademicTermId = 1;

    /// <summary>Faz 28 öncesi seed'in verdiği ad — migration yalnızca bu adı yeniden adlandırır.</summary>
    public const string LegacyPinnedFacultyName = "Mühendislik Fakültesi";

    public const string PinnedFacultyName = "Mühendislik ve Doğa Bilimleri Fakültesi";

    public const string PinnedDepartmentName = "Bilgisayar Mühendisliği";

    public static AcademicTerm AcademicTerm() => new()
    {
        Id = AcademicTermId,
        Name = "2026-2027 Güz",
        StartDateUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
        EndDateUtc = new DateTime(2027, 1, 31, 0, 0, 0, DateTimeKind.Utc),
        IsCurrent = true,

        // docs/MIMARI.md · K-39/A-66: başvuru penceresinin varsayılanı FollowSchedule'dır ve
        // takvim tanımsızken KAPALI demektir (fail-closed). İlk dönem bunun istisnasıdır:
        // hem yeni kurulumda hem yükseltmede başvuru akışı çalışır durumda başlamalı, aksi hâlde
        // özellik "dağıtımda sessizce her şeyi kapatan" bir değişiklik olurdu. Kurum takvimi
        // tanımlayınca FollowSchedule'a geçer. Dönem devrinde (K-30) doğan dönemler kapalı gelir.
        ClubApplicationOverride = ClubApplicationWindowOverride.ForceOpen,
    };

    public static string[] FacultyNames() =>
    [
        "Mühendislik ve Doğa Bilimleri Fakültesi",
        "Akçadağ Meslek Yüksekokulu",
        "Arapgir Meslek Yüksekokulu",
        "Battalgazi Meslek Yüksekokulu",
        "Darende Bekir Ilıcak Meslek Yüksekokulu",
        "Doğanşehir Vahap Küçük Meslek Yüksekokulu",
        "Hekimhan Mehmet Emin Sungur Meslek Yüksekokulu",
        "İşletme ve Yönetim Bilimleri Fakültesi",
        "Kale Turizm ve Otel İşletmeciliği Meslek Yüksekokulu",
        "Lisansüstü Eğitim Enstitüsü",
        "Rektörlük",
        "Sanat Tasarım ve Mimarlık Fakültesi",
        "Sağlık Bilimleri Fakültesi",
        "Sağlık Hizmetleri Meslek Yüksekokulu",
        "Sivil Havacılık Yüksekokulu",
        "Sosyal ve Beşeri Bilimler Fakültesi",
        "Tıp Fakültesi",
        "Yeşilyurt Meslek Yüksekokulu",
        "Ziraat Fakültesi",
    ];

    /// <summary>
    /// Bölümler fakülte adıyla eşleştirilir. Aynı bölüm adı farklı fakültelerde tekrar edebilir
    /// (ör. "Bilgisayar Teknolojileri" hem Akçadağ hem Arapgir MYO'da) — benzersizlik
    /// <c>(FacultyId, Name)</c> üzerindedir, yalnızca <c>Name</c> üzerinde değil.
    /// </summary>
    public static DepartmentEntry[] Departments() =>
    [
        new("Mühendislik ve Doğa Bilimleri Fakültesi", "Bilgisayar Mühendisliği"),
        new("Mühendislik ve Doğa Bilimleri Fakültesi", "Biyomühendislik"),
        new("Mühendislik ve Doğa Bilimleri Fakültesi", "Elektrik Elektronik Mühendisliği"),
        new("Mühendislik ve Doğa Bilimleri Fakültesi", "İnşaat Mühendisliği"),
        new("Mühendislik ve Doğa Bilimleri Fakültesi", "Yazılım Mühendisliği"),
        new("Akçadağ Meslek Yüksekokulu", "Bilgisayar Teknolojileri"),
        new("Akçadağ Meslek Yüksekokulu", "Bitkisel ve Hayvansal Üretim"),
        new("Akçadağ Meslek Yüksekokulu", "Ulaştırma Hizmetleri"),
        new("Akçadağ Meslek Yüksekokulu", "Veterinerlik"),
        new("Akçadağ Meslek Yüksekokulu", "Yönetim ve Organizasyon"),
        new("Arapgir Meslek Yüksekokulu", "Bilgisayar Teknolojileri"),
        new("Arapgir Meslek Yüksekokulu", "Büro Hizmetleri ve Sekreterlik"),
        new("Arapgir Meslek Yüksekokulu", "Elektrik ve Enerji"),
        new("Arapgir Meslek Yüksekokulu", "Elektronik ve Otomasyon"),
        new("Arapgir Meslek Yüksekokulu", "Finans-Bankacılık ve Sigortacılık"),
        new("Arapgir Meslek Yüksekokulu", "Mimarlık ve Şehir Planlama"),
        new("Arapgir Meslek Yüksekokulu", "Motorlu Araçlar ve Ulaştırma Teknolojileri"),
        new("Arapgir Meslek Yüksekokulu", "Muhasebe ve Vergi"),
        new("Battalgazi Meslek Yüksekokulu", "Bitkisel ve Hayvansal Üretim"),
        new("Battalgazi Meslek Yüksekokulu", "Görsel İşitsel Teknikler ve Medya Yapımcılığı"),
        new("Battalgazi Meslek Yüksekokulu", "Kültür Bitkileri"),
        new("Battalgazi Meslek Yüksekokulu", "Park ve Bahçe Bitkileri"),
        new("Battalgazi Meslek Yüksekokulu", "Yönetim ve Organizasyon"),
        new("Darende Bekir Ilıcak Meslek Yüksekokulu", "Gıda İşleme"),
        new("Darende Bekir Ilıcak Meslek Yüksekokulu", "Mimarlık ve Şehir Planlama"),
        new("Darende Bekir Ilıcak Meslek Yüksekokulu", "Tıbbi Hizmetler ve Teknikler"),
        new("Doğanşehir Vahap Küçük Meslek Yüksekokulu", "Büro Hizmetleri ve Sekreterlik"),
        new("Doğanşehir Vahap Küçük Meslek Yüksekokulu", "Dış Ticaret"),
        new("Doğanşehir Vahap Küçük Meslek Yüksekokulu", "Mülkiyet Koruma ve Güvenlik"),
        new("Hekimhan Mehmet Emin Sungur Meslek Yüksekokulu", "İnşaat"),
        new("Hekimhan Mehmet Emin Sungur Meslek Yüksekokulu", "Madencilik ve Maden Çıkarma"),
        new("Hekimhan Mehmet Emin Sungur Meslek Yüksekokulu", "Mülkiyet Koruma ve Güvenlik"),
        new("Hekimhan Mehmet Emin Sungur Meslek Yüksekokulu", "Tasarım"),
        new("İşletme ve Yönetim Bilimleri Fakültesi", "Muhasebe ve Finans Yönetimi"),
        new("İşletme ve Yönetim Bilimleri Fakültesi", "Turizm İşletmeciliği"),
        new("İşletme ve Yönetim Bilimleri Fakültesi", "Uluslararası İşletme Yönetimi"),
        new("İşletme ve Yönetim Bilimleri Fakültesi", "Uluslararası Ticaret ve Finansman"),
        new("İşletme ve Yönetim Bilimleri Fakültesi", "Yönetim Bilişim Sistemleri"),
        new("Kale Turizm ve Otel İşletmeciliği Meslek Yüksekokulu", "Otel, Lokanta ve İkram Hizmetleri"),
        new("Kale Turizm ve Otel İşletmeciliği Meslek Yüksekokulu", "Seyahat, Turizm ve Eğlence Hizmetleri"),
        new("Lisansüstü Eğitim Enstitüsü", "Anatomi Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Bahçe Bitkileri Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Bilgisayar Mühendisliği Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Bitki Koruma Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Biyoloji Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Biyomühendislik Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Cerrahi Hastalıklar Hemşireliği Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Disiplinlerarası Biyomedikal Mühendisliği Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Disiplinlerarası Enformatik Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Disiplinlerarası İletişim Bilimleri Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Disiplinlerarası Tarım Danışmanlığı Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Egzersiz ve Spor Bilimleri Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Elektrik Elektronik Mühendisliği Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Finansal Ekonometri Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Fizik Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Fizyoloji Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Halk Sağlığı Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "İç Hastalıkları Hemşireliği Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "İşletme Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Kimya Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Makine Mühendisliği Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Muhasebe ve Finansman Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Müzik Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Siyaset Bilimi ve Kamu Yönetimi Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Su Ürünleri Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Tarih Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Tarım Ekonomisi Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Tarımsal Biyoteknoloji Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Turizm İşletmeciliği Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Tıbbi Biyokimya Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Tıbbi Biyoloji Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Tıbbi Mikrobiyoloji Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Uluslararası Ticaret ve Finansman Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Yazılım Mühendisliği Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Zootekni Ana Bilim Dalı"),
        new("Lisansüstü Eğitim Enstitüsü", "Bilgisayar Mühendisliği"),
        new("Rektörlük", "Rektörlük"),
        new("Sanat Tasarım ve Mimarlık Fakültesi", "Görsel İletişim Tasarımı"),
        new("Sanat Tasarım ve Mimarlık Fakültesi", "İç Mimarlık"),
        new("Sanat Tasarım ve Mimarlık Fakültesi", "Müzik"),
        new("Sanat Tasarım ve Mimarlık Fakültesi", "Radyo, Televizyon ve Sinema"),
        new("Sanat Tasarım ve Mimarlık Fakültesi", "Resim"),
        new("Sanat Tasarım ve Mimarlık Fakültesi", "Sahne Sanatları"),
        new("Sağlık Bilimleri Fakültesi", "Acil Yardım ve Afet Yönetimi"),
        new("Sağlık Bilimleri Fakültesi", "Beslenme ve Diyetetik"),
        new("Sağlık Bilimleri Fakültesi", "Ebelik"),
        new("Sağlık Bilimleri Fakültesi", "Egzersiz ve Spor Bilimleri"),
        new("Sağlık Bilimleri Fakültesi", "Hemşirelik"),
        new("Sağlık Bilimleri Fakültesi", "Sosyal Hizmet"),
        new("Sağlık Hizmetleri Meslek Yüksekokulu", "İstatistik"),
        new("Sağlık Hizmetleri Meslek Yüksekokulu", "Tıbbi Hizmetler ve Teknikler"),
        new("Sağlık Hizmetleri Meslek Yüksekokulu", "Çocuk Bakımı ve Gençlik Hizmetleri"),
        new("Sivil Havacılık Yüksekokulu", "Havacılık Yönetimi"),
        new("Sosyal ve Beşeri Bilimler Fakültesi", "İngilizce Mütercim ve Tercümanlık"),
        new("Sosyal ve Beşeri Bilimler Fakültesi", "Kültür Varlıklarını Koruma ve Onarım"),
        new("Sosyal ve Beşeri Bilimler Fakültesi", "Muhasebe ve Finans Yönetimi"),
        new("Sosyal ve Beşeri Bilimler Fakültesi", "Psikoloji"),
        new("Sosyal ve Beşeri Bilimler Fakültesi", "Siyaset Bilimi ve Kamu Yönetimi"),
        new("Sosyal ve Beşeri Bilimler Fakültesi", "Sosyoloji"),
        new("Sosyal ve Beşeri Bilimler Fakültesi", "Tarih"),
        new("Sosyal ve Beşeri Bilimler Fakültesi", "Turizm İşletmeciliği"),
        new("Sosyal ve Beşeri Bilimler Fakültesi", "Türk Dili ve Edebiyatı"),
        new("Sosyal ve Beşeri Bilimler Fakültesi", "Uluslararası İşletme Yönetimi"),
        new("Sosyal ve Beşeri Bilimler Fakültesi", "Uluslararası Ticaret ve Finansman"),
        new("Sosyal ve Beşeri Bilimler Fakültesi", "Yeni Medya ve İletişim"),
        new("Sosyal ve Beşeri Bilimler Fakültesi", "Yönetim Bilişim Sistemleri"),
        new("Tıp Fakültesi", "Tıp"),
        new("Yeşilyurt Meslek Yüksekokulu", "Elektrik ve Enerji"),
        new("Yeşilyurt Meslek Yüksekokulu", "Elektronik ve Otomasyon"),
        new("Yeşilyurt Meslek Yüksekokulu", "Malzeme ve Malzeme İşleme Teknolojileri"),
        new("Yeşilyurt Meslek Yüksekokulu", "Motorlu Araçlar ve Ulaştırma Teknolojileri"),
        new("Yeşilyurt Meslek Yüksekokulu", "Tasarım"),
        new("Yeşilyurt Meslek Yüksekokulu", "Tekstil, Giyim, Ayakkabı ve Deri"),
        new("Ziraat Fakültesi", "Bahçe Bitkileri"),
        new("Ziraat Fakültesi", "Bitki Koruma"),
        new("Ziraat Fakültesi", "Biyosistem Mühendisliği"),
        new("Ziraat Fakültesi", "Su Ürünleri Mühendisliği"),
        new("Ziraat Fakültesi", "Tarla Bitkileri"),
        new("Ziraat Fakültesi", "Tarım Ekonomisi"),
        new("Ziraat Fakültesi", "Toprak Bilimi ve Bitki Besleme"),
        new("Ziraat Fakültesi", "Zootekni"),
    ];

    public sealed record DepartmentEntry(string FacultyName, string Name);
}
