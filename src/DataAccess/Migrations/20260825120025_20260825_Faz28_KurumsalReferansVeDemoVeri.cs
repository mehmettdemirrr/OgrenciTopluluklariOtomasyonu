using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <summary>
    /// docs/PLAN-V5.md · Faz 28 (K-34, A-58, Y-68).
    ///
    /// İki iş yapar:
    /// <list type="number">
    ///   <item>Kurumsal referans verisini (19 fakülte / 121 bölüm) <b>ada dayalı, idempotent SQL</b>
    ///         ile yazar. HasData kullanılMAZ: Faculties/Departments tablolarına çalışma zamanında
    ///         da satır ekleniyor (ReferenceDataManager), bu yüzden sabit birincil anahtarlar
    ///         mevcut kurulumlarda IDENTITY değerleriyle çakışır ve migration "duplicate key" ile
    ///         düşer. Gerekçenin tamamı: AppDbContext.OnModelCreating.</item>
    ///   <item>Demo veri künyesi tablosunu (<c>DemoSeedRecords</c>) oluşturur — Y-68.</item>
    /// </list>
    ///
    /// SQL bilerek bu dosyaya gömülüdür (DomainSeedData'dan okunmaz): migration geçmişin değişmez
    /// kaydıdır, katalog ileride güncellenirse bu dosyanın anlamı kaymamalıdır. Katalogla bu SQL'in
    /// aynı kaldığını <c>InstitutionalReferenceDataTests</c> sınar.
    /// </summary>
    public partial class _20260825_Faz28_KurumsalReferansVeDemoVeri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemoSeedRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoSeedRecords", x => x.Id);
                });

            // Y-18/Y-68: aynı satır iki kez künyelenemez — seeder'ın idempotanlığını veritabanı garanti eder.
            migrationBuilder.CreateIndex(
                name: "IX_DemoSeedRecords_EntityType_EntityId",
                table: "DemoSeedRecords",
                columns: new[] { "EntityType", "EntityId" },
                unique: true);

            migrationBuilder.Sql(@"
                -- docs/PLAN-V5.md · Faz 28 (K-34, A-58): kurumsal referans verisi.
                -- Id'ler veritabanınca dağıtılır; ""zaten varsa ekleme"" koşulu benzersizlik indekslerine dayanır.
                -- Bu yüzden betik boş, dolu ve yarım kalmış veritabanlarında aynı sonucu verir (idempotent).

                -- Faz 28 öncesi seed Faculty Id=1 satırını ""Mühendislik Fakültesi"" olarak üretmişti. Demo
                -- öğrencinin DepartmentId=1 yabancı anahtarı buna bağlı olduğu için satır SİLİNMEZ, yeniden
                -- adlandırılır. Koşul, adı elle değiştirilmiş bir kurulumu ezmemek için dar tutulmuştur.
                UPDATE Faculties SET Name = N'Mühendislik ve Doğa Bilimleri Fakültesi' WHERE Id = 1 AND Name = N'Mühendislik Fakültesi';

                INSERT INTO Faculties (Name) SELECT N'Mühendislik ve Doğa Bilimleri Fakültesi' WHERE NOT EXISTS (SELECT 1 FROM Faculties WHERE Name = N'Mühendislik ve Doğa Bilimleri Fakültesi');
                INSERT INTO Faculties (Name) SELECT N'Akçadağ Meslek Yüksekokulu' WHERE NOT EXISTS (SELECT 1 FROM Faculties WHERE Name = N'Akçadağ Meslek Yüksekokulu');
                INSERT INTO Faculties (Name) SELECT N'Arapgir Meslek Yüksekokulu' WHERE NOT EXISTS (SELECT 1 FROM Faculties WHERE Name = N'Arapgir Meslek Yüksekokulu');
                INSERT INTO Faculties (Name) SELECT N'Battalgazi Meslek Yüksekokulu' WHERE NOT EXISTS (SELECT 1 FROM Faculties WHERE Name = N'Battalgazi Meslek Yüksekokulu');
                INSERT INTO Faculties (Name) SELECT N'Darende Bekir Ilıcak Meslek Yüksekokulu' WHERE NOT EXISTS (SELECT 1 FROM Faculties WHERE Name = N'Darende Bekir Ilıcak Meslek Yüksekokulu');
                INSERT INTO Faculties (Name) SELECT N'Doğanşehir Vahap Küçük Meslek Yüksekokulu' WHERE NOT EXISTS (SELECT 1 FROM Faculties WHERE Name = N'Doğanşehir Vahap Küçük Meslek Yüksekokulu');
                INSERT INTO Faculties (Name) SELECT N'Hekimhan Mehmet Emin Sungur Meslek Yüksekokulu' WHERE NOT EXISTS (SELECT 1 FROM Faculties WHERE Name = N'Hekimhan Mehmet Emin Sungur Meslek Yüksekokulu');
                INSERT INTO Faculties (Name) SELECT N'İşletme ve Yönetim Bilimleri Fakültesi' WHERE NOT EXISTS (SELECT 1 FROM Faculties WHERE Name = N'İşletme ve Yönetim Bilimleri Fakültesi');
                INSERT INTO Faculties (Name) SELECT N'Kale Turizm ve Otel İşletmeciliği Meslek Yüksekokulu' WHERE NOT EXISTS (SELECT 1 FROM Faculties WHERE Name = N'Kale Turizm ve Otel İşletmeciliği Meslek Yüksekokulu');
                INSERT INTO Faculties (Name) SELECT N'Lisansüstü Eğitim Enstitüsü' WHERE NOT EXISTS (SELECT 1 FROM Faculties WHERE Name = N'Lisansüstü Eğitim Enstitüsü');
                INSERT INTO Faculties (Name) SELECT N'Rektörlük' WHERE NOT EXISTS (SELECT 1 FROM Faculties WHERE Name = N'Rektörlük');
                INSERT INTO Faculties (Name) SELECT N'Sanat Tasarım ve Mimarlık Fakültesi' WHERE NOT EXISTS (SELECT 1 FROM Faculties WHERE Name = N'Sanat Tasarım ve Mimarlık Fakültesi');
                INSERT INTO Faculties (Name) SELECT N'Sağlık Bilimleri Fakültesi' WHERE NOT EXISTS (SELECT 1 FROM Faculties WHERE Name = N'Sağlık Bilimleri Fakültesi');
                INSERT INTO Faculties (Name) SELECT N'Sağlık Hizmetleri Meslek Yüksekokulu' WHERE NOT EXISTS (SELECT 1 FROM Faculties WHERE Name = N'Sağlık Hizmetleri Meslek Yüksekokulu');
                INSERT INTO Faculties (Name) SELECT N'Sivil Havacılık Yüksekokulu' WHERE NOT EXISTS (SELECT 1 FROM Faculties WHERE Name = N'Sivil Havacılık Yüksekokulu');
                INSERT INTO Faculties (Name) SELECT N'Sosyal ve Beşeri Bilimler Fakültesi' WHERE NOT EXISTS (SELECT 1 FROM Faculties WHERE Name = N'Sosyal ve Beşeri Bilimler Fakültesi');
                INSERT INTO Faculties (Name) SELECT N'Tıp Fakültesi' WHERE NOT EXISTS (SELECT 1 FROM Faculties WHERE Name = N'Tıp Fakültesi');
                INSERT INTO Faculties (Name) SELECT N'Yeşilyurt Meslek Yüksekokulu' WHERE NOT EXISTS (SELECT 1 FROM Faculties WHERE Name = N'Yeşilyurt Meslek Yüksekokulu');
                INSERT INTO Faculties (Name) SELECT N'Ziraat Fakültesi' WHERE NOT EXISTS (SELECT 1 FROM Faculties WHERE Name = N'Ziraat Fakültesi');

                INSERT INTO Departments (Name, FacultyId) SELECT N'Bilgisayar Mühendisliği', f.Id FROM Faculties f WHERE f.Name = N'Mühendislik ve Doğa Bilimleri Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Bilgisayar Mühendisliği');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Biyomühendislik', f.Id FROM Faculties f WHERE f.Name = N'Mühendislik ve Doğa Bilimleri Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Biyomühendislik');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Elektrik Elektronik Mühendisliği', f.Id FROM Faculties f WHERE f.Name = N'Mühendislik ve Doğa Bilimleri Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Elektrik Elektronik Mühendisliği');
                INSERT INTO Departments (Name, FacultyId) SELECT N'İnşaat Mühendisliği', f.Id FROM Faculties f WHERE f.Name = N'Mühendislik ve Doğa Bilimleri Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'İnşaat Mühendisliği');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Yazılım Mühendisliği', f.Id FROM Faculties f WHERE f.Name = N'Mühendislik ve Doğa Bilimleri Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Yazılım Mühendisliği');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Bilgisayar Teknolojileri', f.Id FROM Faculties f WHERE f.Name = N'Akçadağ Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Bilgisayar Teknolojileri');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Bitkisel ve Hayvansal Üretim', f.Id FROM Faculties f WHERE f.Name = N'Akçadağ Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Bitkisel ve Hayvansal Üretim');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Ulaştırma Hizmetleri', f.Id FROM Faculties f WHERE f.Name = N'Akçadağ Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Ulaştırma Hizmetleri');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Veterinerlik', f.Id FROM Faculties f WHERE f.Name = N'Akçadağ Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Veterinerlik');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Yönetim ve Organizasyon', f.Id FROM Faculties f WHERE f.Name = N'Akçadağ Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Yönetim ve Organizasyon');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Bilgisayar Teknolojileri', f.Id FROM Faculties f WHERE f.Name = N'Arapgir Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Bilgisayar Teknolojileri');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Büro Hizmetleri ve Sekreterlik', f.Id FROM Faculties f WHERE f.Name = N'Arapgir Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Büro Hizmetleri ve Sekreterlik');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Elektrik ve Enerji', f.Id FROM Faculties f WHERE f.Name = N'Arapgir Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Elektrik ve Enerji');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Elektronik ve Otomasyon', f.Id FROM Faculties f WHERE f.Name = N'Arapgir Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Elektronik ve Otomasyon');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Finans-Bankacılık ve Sigortacılık', f.Id FROM Faculties f WHERE f.Name = N'Arapgir Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Finans-Bankacılık ve Sigortacılık');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Mimarlık ve Şehir Planlama', f.Id FROM Faculties f WHERE f.Name = N'Arapgir Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Mimarlık ve Şehir Planlama');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Motorlu Araçlar ve Ulaştırma Teknolojileri', f.Id FROM Faculties f WHERE f.Name = N'Arapgir Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Motorlu Araçlar ve Ulaştırma Teknolojileri');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Muhasebe ve Vergi', f.Id FROM Faculties f WHERE f.Name = N'Arapgir Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Muhasebe ve Vergi');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Bitkisel ve Hayvansal Üretim', f.Id FROM Faculties f WHERE f.Name = N'Battalgazi Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Bitkisel ve Hayvansal Üretim');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Görsel İşitsel Teknikler ve Medya Yapımcılığı', f.Id FROM Faculties f WHERE f.Name = N'Battalgazi Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Görsel İşitsel Teknikler ve Medya Yapımcılığı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Kültür Bitkileri', f.Id FROM Faculties f WHERE f.Name = N'Battalgazi Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Kültür Bitkileri');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Park ve Bahçe Bitkileri', f.Id FROM Faculties f WHERE f.Name = N'Battalgazi Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Park ve Bahçe Bitkileri');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Yönetim ve Organizasyon', f.Id FROM Faculties f WHERE f.Name = N'Battalgazi Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Yönetim ve Organizasyon');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Gıda İşleme', f.Id FROM Faculties f WHERE f.Name = N'Darende Bekir Ilıcak Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Gıda İşleme');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Mimarlık ve Şehir Planlama', f.Id FROM Faculties f WHERE f.Name = N'Darende Bekir Ilıcak Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Mimarlık ve Şehir Planlama');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Tıbbi Hizmetler ve Teknikler', f.Id FROM Faculties f WHERE f.Name = N'Darende Bekir Ilıcak Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Tıbbi Hizmetler ve Teknikler');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Büro Hizmetleri ve Sekreterlik', f.Id FROM Faculties f WHERE f.Name = N'Doğanşehir Vahap Küçük Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Büro Hizmetleri ve Sekreterlik');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Dış Ticaret', f.Id FROM Faculties f WHERE f.Name = N'Doğanşehir Vahap Küçük Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Dış Ticaret');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Mülkiyet Koruma ve Güvenlik', f.Id FROM Faculties f WHERE f.Name = N'Doğanşehir Vahap Küçük Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Mülkiyet Koruma ve Güvenlik');
                INSERT INTO Departments (Name, FacultyId) SELECT N'İnşaat', f.Id FROM Faculties f WHERE f.Name = N'Hekimhan Mehmet Emin Sungur Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'İnşaat');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Madencilik ve Maden Çıkarma', f.Id FROM Faculties f WHERE f.Name = N'Hekimhan Mehmet Emin Sungur Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Madencilik ve Maden Çıkarma');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Mülkiyet Koruma ve Güvenlik', f.Id FROM Faculties f WHERE f.Name = N'Hekimhan Mehmet Emin Sungur Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Mülkiyet Koruma ve Güvenlik');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Tasarım', f.Id FROM Faculties f WHERE f.Name = N'Hekimhan Mehmet Emin Sungur Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Tasarım');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Muhasebe ve Finans Yönetimi', f.Id FROM Faculties f WHERE f.Name = N'İşletme ve Yönetim Bilimleri Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Muhasebe ve Finans Yönetimi');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Turizm İşletmeciliği', f.Id FROM Faculties f WHERE f.Name = N'İşletme ve Yönetim Bilimleri Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Turizm İşletmeciliği');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Uluslararası İşletme Yönetimi', f.Id FROM Faculties f WHERE f.Name = N'İşletme ve Yönetim Bilimleri Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Uluslararası İşletme Yönetimi');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Uluslararası Ticaret ve Finansman', f.Id FROM Faculties f WHERE f.Name = N'İşletme ve Yönetim Bilimleri Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Uluslararası Ticaret ve Finansman');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Yönetim Bilişim Sistemleri', f.Id FROM Faculties f WHERE f.Name = N'İşletme ve Yönetim Bilimleri Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Yönetim Bilişim Sistemleri');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Otel, Lokanta ve İkram Hizmetleri', f.Id FROM Faculties f WHERE f.Name = N'Kale Turizm ve Otel İşletmeciliği Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Otel, Lokanta ve İkram Hizmetleri');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Seyahat, Turizm ve Eğlence Hizmetleri', f.Id FROM Faculties f WHERE f.Name = N'Kale Turizm ve Otel İşletmeciliği Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Seyahat, Turizm ve Eğlence Hizmetleri');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Anatomi Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Anatomi Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Bahçe Bitkileri Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Bahçe Bitkileri Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Bilgisayar Mühendisliği Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Bilgisayar Mühendisliği Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Bitki Koruma Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Bitki Koruma Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Biyoloji Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Biyoloji Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Biyomühendislik Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Biyomühendislik Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Cerrahi Hastalıklar Hemşireliği Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Cerrahi Hastalıklar Hemşireliği Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Disiplinlerarası Biyomedikal Mühendisliği Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Disiplinlerarası Biyomedikal Mühendisliği Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Disiplinlerarası Enformatik Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Disiplinlerarası Enformatik Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Disiplinlerarası İletişim Bilimleri Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Disiplinlerarası İletişim Bilimleri Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Disiplinlerarası Tarım Danışmanlığı Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Disiplinlerarası Tarım Danışmanlığı Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Egzersiz ve Spor Bilimleri Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Egzersiz ve Spor Bilimleri Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Elektrik Elektronik Mühendisliği Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Elektrik Elektronik Mühendisliği Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Finansal Ekonometri Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Finansal Ekonometri Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Fizik Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Fizik Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Fizyoloji Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Fizyoloji Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Halk Sağlığı Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Halk Sağlığı Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'İç Hastalıkları Hemşireliği Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'İç Hastalıkları Hemşireliği Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'İşletme Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'İşletme Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Kimya Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Kimya Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Makine Mühendisliği Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Makine Mühendisliği Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Muhasebe ve Finansman Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Muhasebe ve Finansman Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Müzik Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Müzik Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Siyaset Bilimi ve Kamu Yönetimi Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Siyaset Bilimi ve Kamu Yönetimi Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Su Ürünleri Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Su Ürünleri Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Tarih Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Tarih Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Tarım Ekonomisi Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Tarım Ekonomisi Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Tarımsal Biyoteknoloji Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Tarımsal Biyoteknoloji Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Turizm İşletmeciliği Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Turizm İşletmeciliği Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Tıbbi Biyokimya Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Tıbbi Biyokimya Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Tıbbi Biyoloji Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Tıbbi Biyoloji Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Tıbbi Mikrobiyoloji Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Tıbbi Mikrobiyoloji Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Uluslararası Ticaret ve Finansman Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Uluslararası Ticaret ve Finansman Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Yazılım Mühendisliği Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Yazılım Mühendisliği Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Zootekni Ana Bilim Dalı', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Zootekni Ana Bilim Dalı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Bilgisayar Mühendisliği', f.Id FROM Faculties f WHERE f.Name = N'Lisansüstü Eğitim Enstitüsü' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Bilgisayar Mühendisliği');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Rektörlük', f.Id FROM Faculties f WHERE f.Name = N'Rektörlük' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Rektörlük');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Görsel İletişim Tasarımı', f.Id FROM Faculties f WHERE f.Name = N'Sanat Tasarım ve Mimarlık Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Görsel İletişim Tasarımı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'İç Mimarlık', f.Id FROM Faculties f WHERE f.Name = N'Sanat Tasarım ve Mimarlık Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'İç Mimarlık');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Müzik', f.Id FROM Faculties f WHERE f.Name = N'Sanat Tasarım ve Mimarlık Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Müzik');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Radyo, Televizyon ve Sinema', f.Id FROM Faculties f WHERE f.Name = N'Sanat Tasarım ve Mimarlık Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Radyo, Televizyon ve Sinema');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Resim', f.Id FROM Faculties f WHERE f.Name = N'Sanat Tasarım ve Mimarlık Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Resim');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Sahne Sanatları', f.Id FROM Faculties f WHERE f.Name = N'Sanat Tasarım ve Mimarlık Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Sahne Sanatları');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Acil Yardım ve Afet Yönetimi', f.Id FROM Faculties f WHERE f.Name = N'Sağlık Bilimleri Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Acil Yardım ve Afet Yönetimi');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Beslenme ve Diyetetik', f.Id FROM Faculties f WHERE f.Name = N'Sağlık Bilimleri Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Beslenme ve Diyetetik');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Ebelik', f.Id FROM Faculties f WHERE f.Name = N'Sağlık Bilimleri Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Ebelik');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Egzersiz ve Spor Bilimleri', f.Id FROM Faculties f WHERE f.Name = N'Sağlık Bilimleri Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Egzersiz ve Spor Bilimleri');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Hemşirelik', f.Id FROM Faculties f WHERE f.Name = N'Sağlık Bilimleri Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Hemşirelik');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Sosyal Hizmet', f.Id FROM Faculties f WHERE f.Name = N'Sağlık Bilimleri Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Sosyal Hizmet');
                INSERT INTO Departments (Name, FacultyId) SELECT N'İstatistik', f.Id FROM Faculties f WHERE f.Name = N'Sağlık Hizmetleri Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'İstatistik');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Tıbbi Hizmetler ve Teknikler', f.Id FROM Faculties f WHERE f.Name = N'Sağlık Hizmetleri Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Tıbbi Hizmetler ve Teknikler');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Çocuk Bakımı ve Gençlik Hizmetleri', f.Id FROM Faculties f WHERE f.Name = N'Sağlık Hizmetleri Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Çocuk Bakımı ve Gençlik Hizmetleri');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Havacılık Yönetimi', f.Id FROM Faculties f WHERE f.Name = N'Sivil Havacılık Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Havacılık Yönetimi');
                INSERT INTO Departments (Name, FacultyId) SELECT N'İngilizce Mütercim ve Tercümanlık', f.Id FROM Faculties f WHERE f.Name = N'Sosyal ve Beşeri Bilimler Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'İngilizce Mütercim ve Tercümanlık');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Kültür Varlıklarını Koruma ve Onarım', f.Id FROM Faculties f WHERE f.Name = N'Sosyal ve Beşeri Bilimler Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Kültür Varlıklarını Koruma ve Onarım');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Muhasebe ve Finans Yönetimi', f.Id FROM Faculties f WHERE f.Name = N'Sosyal ve Beşeri Bilimler Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Muhasebe ve Finans Yönetimi');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Psikoloji', f.Id FROM Faculties f WHERE f.Name = N'Sosyal ve Beşeri Bilimler Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Psikoloji');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Siyaset Bilimi ve Kamu Yönetimi', f.Id FROM Faculties f WHERE f.Name = N'Sosyal ve Beşeri Bilimler Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Siyaset Bilimi ve Kamu Yönetimi');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Sosyoloji', f.Id FROM Faculties f WHERE f.Name = N'Sosyal ve Beşeri Bilimler Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Sosyoloji');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Tarih', f.Id FROM Faculties f WHERE f.Name = N'Sosyal ve Beşeri Bilimler Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Tarih');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Turizm İşletmeciliği', f.Id FROM Faculties f WHERE f.Name = N'Sosyal ve Beşeri Bilimler Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Turizm İşletmeciliği');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Türk Dili ve Edebiyatı', f.Id FROM Faculties f WHERE f.Name = N'Sosyal ve Beşeri Bilimler Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Türk Dili ve Edebiyatı');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Uluslararası İşletme Yönetimi', f.Id FROM Faculties f WHERE f.Name = N'Sosyal ve Beşeri Bilimler Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Uluslararası İşletme Yönetimi');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Uluslararası Ticaret ve Finansman', f.Id FROM Faculties f WHERE f.Name = N'Sosyal ve Beşeri Bilimler Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Uluslararası Ticaret ve Finansman');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Yeni Medya ve İletişim', f.Id FROM Faculties f WHERE f.Name = N'Sosyal ve Beşeri Bilimler Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Yeni Medya ve İletişim');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Yönetim Bilişim Sistemleri', f.Id FROM Faculties f WHERE f.Name = N'Sosyal ve Beşeri Bilimler Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Yönetim Bilişim Sistemleri');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Tıp', f.Id FROM Faculties f WHERE f.Name = N'Tıp Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Tıp');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Elektrik ve Enerji', f.Id FROM Faculties f WHERE f.Name = N'Yeşilyurt Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Elektrik ve Enerji');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Elektronik ve Otomasyon', f.Id FROM Faculties f WHERE f.Name = N'Yeşilyurt Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Elektronik ve Otomasyon');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Malzeme ve Malzeme İşleme Teknolojileri', f.Id FROM Faculties f WHERE f.Name = N'Yeşilyurt Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Malzeme ve Malzeme İşleme Teknolojileri');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Motorlu Araçlar ve Ulaştırma Teknolojileri', f.Id FROM Faculties f WHERE f.Name = N'Yeşilyurt Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Motorlu Araçlar ve Ulaştırma Teknolojileri');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Tasarım', f.Id FROM Faculties f WHERE f.Name = N'Yeşilyurt Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Tasarım');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Tekstil, Giyim, Ayakkabı ve Deri', f.Id FROM Faculties f WHERE f.Name = N'Yeşilyurt Meslek Yüksekokulu' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Tekstil, Giyim, Ayakkabı ve Deri');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Bahçe Bitkileri', f.Id FROM Faculties f WHERE f.Name = N'Ziraat Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Bahçe Bitkileri');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Bitki Koruma', f.Id FROM Faculties f WHERE f.Name = N'Ziraat Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Bitki Koruma');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Biyosistem Mühendisliği', f.Id FROM Faculties f WHERE f.Name = N'Ziraat Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Biyosistem Mühendisliği');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Su Ürünleri Mühendisliği', f.Id FROM Faculties f WHERE f.Name = N'Ziraat Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Su Ürünleri Mühendisliği');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Tarla Bitkileri', f.Id FROM Faculties f WHERE f.Name = N'Ziraat Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Tarla Bitkileri');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Tarım Ekonomisi', f.Id FROM Faculties f WHERE f.Name = N'Ziraat Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Tarım Ekonomisi');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Toprak Bilimi ve Bitki Besleme', f.Id FROM Faculties f WHERE f.Name = N'Ziraat Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Toprak Bilimi ve Bitki Besleme');
                INSERT INTO Departments (Name, FacultyId) SELECT N'Zootekni', f.Id FROM Faculties f WHERE f.Name = N'Ziraat Fakültesi' AND NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = f.Id AND d.Name = N'Zootekni');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemoSeedRecords");

            migrationBuilder.Sql(
                @"UPDATE Faculties SET Name = N'Mühendislik Fakültesi'
                  WHERE Id = 1 AND Name = N'Mühendislik ve Doğa Bilimleri Fakültesi';");

            // Eklenen 18 fakülte ve 120 bölüm BİLİNÇLİ olarak silinmez: geri alma anında bir öğrenci
            // ya da akademik personel o bölümlere bağlanmış olabilir ve FK Restrict silmeyi
            // reddederdi. Geri alınamayan bir Down, eksik bir Down'dan kötüdür.
        }
    }
}
