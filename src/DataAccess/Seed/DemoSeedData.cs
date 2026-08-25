using Entities.Enums;

namespace DataAccess.Seed;

/// <summary>
/// docs/MIMARI.md · Y-68 (K-34): <c>DemoDataSeeder</c>'ın üreteceği kayıtların katalogu.
/// Veri koddan ayrılmıştır ki seeder'ın mantığı (idempotanlık, künyeleme, sıfırlama)
/// tek başına okunabilsin.
///
/// <b>E-posta alan adı bilinçli olarak yönlendirilemezdir</b> (<c>.invalid</c>, RFC 2606):
/// demo hesaplara yanlışlıkla gerçek e-posta gönderilemez.
///
/// Tarihler burada <b>yok</b> — "geçmiş/gelecek etkinlik" ayrımı sabit literalle bir yıl sonra
/// bozulur, bu yüzden gün farkı olarak tutulur ve seeder <c>IClock</c> ile bugüne oturtur.
/// </summary>
public static class DemoSeedData
{
    public const string EmailDomain = "@demo.ogrenci-topluluklari.invalid";

    /// <param name="DepartmentName">
    /// Bölüm <b>adla</b> aranır, Id ile değil: kurumsal referans verisi ada dayalı idempotent SQL
    /// ile geldiği için Id'ler veritabanına göre değişir (bkz. AppDbContext.OnModelCreating).
    /// Aynı ad birden çok fakültede varsa en küçük Id'li kayıt seçilir — sonuç deterministiktir.
    /// </param>
    public sealed record DemoAdvisor(string FirstName, string LastName, string Title, string DepartmentName);

    /// <inheritdoc cref="DemoAdvisor"/>
    public sealed record DemoStudent(string FirstName, string LastName, string StudentNumber, string DepartmentName, int EnrollmentYear);

    public sealed record DemoClub(string Name, string Description, int AdvisorIndex, bool IsActive);

    /// <param name="StartDayOffset">Bugüne göre gün farkı — negatif geçmiş, pozitif gelecek.</param>
    public sealed record DemoEvent(
        int ClubIndex, string Title, string Location, EventStatus Status, int StartDayOffset, int DurationHours, int? Capacity);

    /// <param name="ClubIndex">-1 ise sistem duyurusu (ClubId = null).</param>
    public sealed record DemoAnnouncement(int ClubIndex, string Title, string Content, AnnouncementVisibility Visibility, int PublishedDayOffset);

    public static DemoAdvisor[] Advisors() =>
    [
        new("Elif", "Yıldırım", "Prof. Dr.", "Bilgisayar Mühendisliği"),
        new("Mert", "Aslan", "Doç. Dr.", "Yazılım Mühendisliği"),
        new("Zeynep", "Kaya", "Dr. Öğr. Üyesi", "Psikoloji"),
        new("Burak", "Şahin", "Doç. Dr.", "Egzersiz ve Spor Bilimleri"),
        new("Selin", "Demir", "Dr. Öğr. Üyesi", "Görsel İletişim Tasarımı"),
        new("Ahmet", "Korkmaz", "Prof. Dr.", "Bahçe Bitkileri"),
    ];

    public static DemoStudent[] Students() =>
    [
        new("Ada", "Yılmaz", "20261001", "Bilgisayar Mühendisliği", 2023),
        new("Kerem", "Doğan", "20261002", "Yazılım Mühendisliği", 2023),
        new("Elif", "Şahin", "20261003", "Elektrik Elektronik Mühendisliği", 2024),
        new("Mustafa", "Aydın", "20261004", "İnşaat Mühendisliği", 2024),
        new("Zeynep", "Çelik", "20261005", "Biyomühendislik", 2025),
        new("Emir", "Kurt", "20261006", "Psikoloji", 2023),
        new("Naz", "Öztürk", "20261007", "Sosyoloji", 2024),
        new("Barış", "Yalçın", "20261008", "Türk Dili ve Edebiyatı", 2025),
        new("Ceren", "Aksoy", "20261009", "Hemşirelik", 2023),
        new("Deniz", "Erdoğan", "20261010", "Beslenme ve Diyetetik", 2024),
        new("Ege", "Polat", "20261011", "Görsel İletişim Tasarımı", 2025),
        new("Fatma", "Kılıç", "20261012", "Müzik", 2026),
        new("Gökhan", "Tekin", "20261013", "Tıp", 2023),
        new("Hande", "Arslan", "20261014", "Havacılık Yönetimi", 2024),
        new("İsmail", "Bulut", "20261015", "Yönetim Bilişim Sistemleri", 2025),
        new("Jale", "Koç", "20261016", "Bahçe Bitkileri", 2026),
        new("Kaan", "Özdemir", "20261017", "Tarım Ekonomisi", 2023),
        new("Lale", "Güneş", "20261018", "İstatistik", 2024),
        new("Melis", "Ateş", "20261019", "Acil Yardım ve Afet Yönetimi", 2025),
        new("Nihat", "Sarı", "20261020", "Uluslararası Ticaret ve Finansman", 2026),
        new("Ozan", "Yiğit", "20261021", "Muhasebe ve Finans Yönetimi", 2023),
        new("Pınar", "Duman", "20261022", "Uluslararası İşletme Yönetimi", 2024),
        new("Rüya", "Çetin", "20261023", "Biyoloji Ana Bilim Dalı", 2025),
        new("Serkan", "Bilgin", "20261024", "Fizik Ana Bilim Dalı", 2026),
        new("Tuğçe", "Avcı", "20261025", "Kimya Ana Bilim Dalı", 2026),
    ];

    public static DemoClub[] Clubs() =>
    [
        new("Yapay Zekâ ve Veri Bilimi Topluluğu", "Makine öğrenmesi çalışma grupları, veri maratonları ve seminerler.", 0, true),
        new("Robotik ve Gömülü Sistemler Topluluğu", "Gömülü yazılım, otonom araçlar ve robot yarışmaları.", 1, true),
        new("Girişimcilik ve İnovasyon Kulübü", "Fikirden ürüne: iş modeli atölyeleri ve mentorluk buluşmaları.", 5, true),
        new("Psikoloji ve Farkındalık Topluluğu", "Ruh sağlığı söyleşileri, akran destek grupları ve film analizleri.", 2, true),
        new("Dağcılık ve Doğa Sporları Kulübü", "Doğa yürüyüşleri, kamp eğitimleri ve tırmanış çalışmaları.", 3, true),
        new("Fotoğrafçılık ve Görsel Sanatlar Kulübü", "Şehir çekimleri, karanlık oda atölyeleri ve yıl sonu sergisi.", 4, true),
        new("Tarım ve Gıda Teknolojileri Topluluğu", "Sera uygulamaları, gıda güvenliği seminerleri ve arazi gezileri.", 5, true),
        new("Havacılık ve Uzay Topluluğu", "Model uçak ve roket çalışmaları. Yeterli üye sayısına ulaşılamadığı için askıya alındı.", 1, false),
    ];

    /// <summary>
    /// 20 etkinlik: her durum rozetinin (Draft, PendingApproval, Published, Rejected, Cancelled)
    /// en az bir gerçek kaydı, hem geçmiş hem gelecek tarihli örnekler ve kontenjanı dolacak bir etkinlik.
    /// </summary>
    public static DemoEvent[] Events() =>
    [
        new(0, "Derin Öğrenmeye Giriş Atölyesi", "Mühendislik Fakültesi B-201", EventStatus.Published, 6, 4, 30),
        new(0, "Veri Maratonu 2026", "Merkezî Araştırma Laboratuvarı", EventStatus.Published, 21, 8, 3),
        new(0, "Yapay Zekâ Etiği Paneli", "Kongre Merkezi Salon A", EventStatus.Published, -14, 3, 120),
        new(0, "Doğal Dil İşleme Çalışma Grubu", "Mühendislik Fakültesi B-105", EventStatus.Draft, 30, 2, 20),
        new(1, "Çizgi İzleyen Robot Yarışması", "Spor Salonu", EventStatus.Published, 12, 6, 60),
        new(1, "Arduino ile Gömülü Sistemler", "Mühendislik Fakültesi Elektronik Lab.", EventStatus.PendingApproval, 25, 4, 25),
        new(1, "Drone Uçuş Günü", "Kampüs Açık Alan", EventStatus.Cancelled, 9, 5, 40),
        new(2, "Fikirden Ürüne: Startup Kampı", "İşletme Fakültesi Konferans Salonu", EventStatus.Published, 17, 9, 50),
        new(2, "Yatırımcı Buluşması", "Teknokent Toplantı Salonu", EventStatus.PendingApproval, 34, 3, 35),
        new(2, "İş Modeli Kanvası Atölyesi", "İşletme Fakültesi D-3", EventStatus.Published, -28, 3, 40),
        new(3, "Sınav Kaygısıyla Başa Çıkma Semineri", "Sağlık Bilimleri Fakültesi Amfi 1", EventStatus.Published, 4, 2, 90),
        new(3, "Akran Destek Grubu İlk Buluşma", "Öğrenci Yaşam Merkezi", EventStatus.Published, -7, 2, 15),
        new(3, "Film Analizi: Bellek ve Kimlik", "Kültür Merkezi Sinema Salonu", EventStatus.Rejected, 11, 3, 80),
        new(4, "Nemrut Dağı Zirve Yürüyüşü", "Nemrut Dağı", EventStatus.Published, 19, 10, 25),
        new(4, "Temel Kamp ve Barınma Eğitimi", "Kampüs Kamp Alanı", EventStatus.Published, -21, 6, 20),
        new(4, "Kaya Tırmanışı Güvenlik Semineri", "Spor Salonu Tırmanma Duvarı", EventStatus.Draft, 40, 3, 18),
        new(5, "Şehir Fotoğrafçılığı Gezisi", "Battalgazi Tarihî Kent Merkezi", EventStatus.Published, 8, 5, 22),
        new(5, "Yıl Sonu Fotoğraf Sergisi", "Kültür Merkezi Fuaye", EventStatus.Published, 45, 6, null),
        new(6, "Sera Uygulamaları Arazi Gezisi", "Ziraat Fakültesi Uygulama Serası", EventStatus.Published, 14, 5, 30),
        new(6, "Gıda Güvenliği Semineri", "Ziraat Fakültesi Amfi 2", EventStatus.Rejected, 27, 2, 60),
    ];

    public static DemoAnnouncement[] Announcements() =>
    [
        new(-1, "2026-2027 Güz Dönemi Topluluk Kayıtları Açıldı", "Tüm topluluklara üyelik başvuruları 30 Eylül'e kadar açıktır. Başvurularınızı topluluk sayfalarından yapabilirsiniz.", AnnouncementVisibility.Public, -3),
        new(-1, "Topluluk Etkinlik Bütçesi Başvuru Takvimi", "Dönemlik etkinlik bütçesi başvuruları için son tarih 15 Ekim'dir. Başvurular danışman onayıyla değerlendirilir.", AnnouncementVisibility.Public, -10),
        new(0, "Derin Öğrenme Atölyesi İçin Ön Hazırlık", "Atölyeye katılacak arkadaşların Python ve NumPy kurulumlarını önceden tamamlaması rica olunur.", AnnouncementVisibility.Members, -2),
        new(0, "Veri Maratonu Takım Kayıtları", "Takımlar en fazla üç kişiliktir. Kontenjan sınırlıdır, kayıtlar etkinlik sayfasından yapılır.", AnnouncementVisibility.Public, -5),
        new(1, "Robot Yarışması Kural Kitapçığı Yayımlandı", "Yarışma kuralları ve pist ölçüleri topluluk panosunda paylaşılmıştır.", AnnouncementVisibility.Public, -6),
        new(1, "Elektronik Laboratuvarı Kullanım Saatleri", "Laboratuvar hafta içi 17.00-20.00 arasında topluluk üyelerine açıktır.", AnnouncementVisibility.Members, -9),
        new(2, "Mentor Eşleştirme Formu", "Startup kampına katılacak üyelerin mentor tercih formunu doldurması gerekmektedir.", AnnouncementVisibility.Members, -4),
        new(2, "Girişimcilik Sertifika Programı", "Programı tamamlayan üyelere katılım sertifikası verilecektir.", AnnouncementVisibility.Public, -12),
        new(3, "Akran Destek Grubu Gönüllü Çağrısı", "Gönüllü olmak isteyen üyeler topluluk yönetimiyle iletişime geçebilir.", AnnouncementVisibility.Members, -1),
        new(4, "Zirve Yürüyüşü Ekipman Listesi", "Katılımcıların kışlık mont, bere ve en az iki litre su getirmesi zorunludur.", AnnouncementVisibility.Members, -8),
        new(5, "Sergi İçin Eser Teslimi", "Sergiye katılacak fotoğrafların dijital kopyaları 1 Aralık'a kadar teslim edilmelidir.", AnnouncementVisibility.Public, -11),
        new(6, "Arazi Gezisi Servis Saatleri", "Servis kampüs ana kapıdan saat 08.30'da hareket edecektir.", AnnouncementVisibility.Public, -7),
    ];
}
