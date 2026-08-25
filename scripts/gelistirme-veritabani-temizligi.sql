/*
    docs/PLAN-V5.md · Faz 28 §28.3 — YEREL GELİŞTİRME VERİTABANI TEMİZLİĞİ

    Bu betik uygulamanın parçası DEĞİLDİR ve hiçbir kod yolundan çağrılmaz. Elle,
    yalnızca geliştirici makinesinde çalıştırılır.

    NEDEN AYRI BİR BETİK?
    --------------------
    Y-68: DemoDataSeeder yalnızca KENDİ künyelediği (DemoSeedRecords) satırları silebilir.
    Ama geliştirme veritabanındaki artık — "FAZ21020937 Kulup 014", "Faz16 Test Etkinligi
    1787488122926" gibi 100+ satır — Playwright doğrulama betiklerinin ürünüdür ve künyesizdir.
    Ad kalıbına bakan toplu silmeyi uygulamaya koymak, tam olarak Y-68'in yasakladığı şeydir:
    üretimde gerçek bir kulübün adı kalıba uyduğu gün veri kaybı olur.

    Bu yüzden temizlik uygulamanın dışında, elle ve gözden geçirilerek yapılır.

    KULLANIM
    --------
      1. Önce ÖN İZLEME bölümünü çalıştırın, silinecekleri gözünüzle görün.
      2. Doğruysa SİLME bölümünü çalıştırın.

      sqlcmd -S "(localdb)\mssqllocaldb" -d OgrenciTopluluklariOtomasyonu -i scripts/gelistirme-veritabani-temizligi.sql

    DOKUNMADIKLARI
    --------------
      * AspNetUsers — hiçbir hesap silinmez (gerçek hesaplarınız burada).
      * Faculties / Departments / AcademicTerms — migration'ın seed'i (HasData).
      * Club Id = 1 — IdentitySeeder'ın demo kulübü; seeder onu yeniden üretmez.
      * DemoSeedRecords'ta künyeli satırlar — onlar Seed:ResetDemo=true ile yönetilir.
*/

SET NOCOUNT ON;

-- sqlcmd bu ayarı varsayılan olarak KAPALI başlatır; ClubMemberships üzerindeki filtreli
-- benzersiz indeks (A-39, başkan tekilliği) açık olmasını şart koşar, aksi hâlde DELETE
-- "SET options have incorrect settings" ile düşer.
SET QUOTED_IDENTIFIER ON;

-- Silinecek kulüpler: Id 1 dışındaki ve demo künyesi OLMAYAN her şey.
IF OBJECT_ID('tempdb..#ArtikKulup') IS NOT NULL DROP TABLE #ArtikKulup;
SELECT c.Id
INTO #ArtikKulup
FROM Clubs c
WHERE c.Id <> 1
  AND NOT EXISTS (SELECT 1 FROM DemoSeedRecords r WHERE r.EntityType = 'Club' AND r.EntityId = c.Id);

IF OBJECT_ID('tempdb..#ArtikEtkinlik') IS NOT NULL DROP TABLE #ArtikEtkinlik;
SELECT e.Id
INTO #ArtikEtkinlik
FROM Events e
WHERE NOT EXISTS (SELECT 1 FROM DemoSeedRecords r WHERE r.EntityType = 'Event' AND r.EntityId = e.Id);

IF OBJECT_ID('tempdb..#ArtikDuyuru') IS NOT NULL DROP TABLE #ArtikDuyuru;
SELECT a.Id
INTO #ArtikDuyuru
FROM Announcements a
WHERE NOT EXISTS (SELECT 1 FROM DemoSeedRecords r WHERE r.EntityType = 'Announcement' AND r.EntityId = a.Id);

/* ---------------------------- ÖN İZLEME ---------------------------- */

SELECT 'Silinecek kulup'    AS Kapsam, COUNT(*) AS Adet FROM #ArtikKulup
UNION ALL SELECT 'Silinecek etkinlik', COUNT(*) FROM #ArtikEtkinlik
UNION ALL SELECT 'Silinecek duyuru',   COUNT(*) FROM #ArtikDuyuru;

/* ----------------------------- SİLME ------------------------------- */

BEGIN TRANSACTION;

DELETE FROM EventParticipations   WHERE EventId IN (SELECT Id FROM #ArtikEtkinlik);
DELETE FROM Events                WHERE Id      IN (SELECT Id FROM #ArtikEtkinlik);
DELETE FROM Announcements         WHERE Id      IN (SELECT Id FROM #ArtikDuyuru);

DELETE FROM MembershipApplications WHERE ClubId IN (SELECT Id FROM #ArtikKulup);
DELETE FROM ClubMemberships        WHERE ClubId IN (SELECT Id FROM #ArtikKulup);
DELETE FROM ClubApplications       WHERE CreatedClubId IN (SELECT Id FROM #ArtikKulup);
DELETE FROM Clubs                  WHERE Id     IN (SELECT Id FROM #ArtikKulup);

-- Doğrulama betiklerinin arayüzden eklediği boş fakülteler ("Faz13 Fakülte ..." gibi).
-- Koşul dar: hiçbir bölümü olmayan VE kurumsal katalogda bulunmayan fakülteler. Gerçek bir
-- fakültenin bölümü olur, bu yüzden bu koşul kurumsal veriye dokunamaz.
DELETE FROM Faculties
WHERE NOT EXISTS (SELECT 1 FROM Departments d WHERE d.FacultyId = Faculties.Id);

COMMIT TRANSACTION;

DROP TABLE #ArtikKulup;
DROP TABLE #ArtikEtkinlik;
DROP TABLE #ArtikDuyuru;

SELECT 'Kalan kulup' AS Kapsam, COUNT(*) AS Adet FROM Clubs
UNION ALL SELECT 'Kalan etkinlik', COUNT(*) FROM Events
UNION ALL SELECT 'Kalan duyuru',   COUNT(*) FROM Announcements;
