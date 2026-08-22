# V2 — Kapsamlı öğrenci topluluğu platformu (Faz 8 → 14)

## Context

`docs/MIMARI.md` v1.0'ın yedi fazı da bitti, test edildi ve pushlandı (son commit `4b5d398`; 156 test yeşil). Ortaya çıkan sistem mimari olarak sağlam ama **işlevsel olarak bir öğrenci topluluğu otomasyonunun çekirdeğini eksik bırakıyor**: öğrenci kendi hesabını açamıyor, bir etkinliğe katılamıyor, kulüpler yalnızca seed ile doğuyor ve arayüz sıfır özelleştirilmiş MUI varsayılanı.

Kod okunarak doğrulanan üç yapısal boşluk planın omurgasını belirledi:

| Bulgu | Kanıt | Sonuç |
|---|---|---|
| **`ClubRole.Officer/President` erişilemez** | `MembershipApplicationManager.cs:191` yalnızca `ClubRole.Member` yazıyor; başka hiçbir yerde `ClubRole` ataması yok | `EventManager.cs:219`'daki Officer/President dalı ve `ReportScopeResolver.cs:61`'deki kapsam dalı **ölü kod**. Bugün hiçbir öğrenci etkinlik oluşturamaz. |
| **`Announcement` + `EventParticipation` yalnızca şema** | Entity + Configuration + DbSet + unique index var; `src/Business` ve `src/WebAPI` altında **hiç referans yok** | İki hazır entity, unique index'leriyle birlikte atıl bekliyor. `Event.Capacity` yazılıyor ama hiçbir yerde okunmuyor/zorlanmıyor. |
| **K-03 yarım** | MIMARI.md K-03 "şifre sıfırlama, e-posta doğrulama"yı **V1 kapsamında** sayıyor; `IEmailSender`'ın tek çağıranı `MembershipDecisionNotificationJob.cs:51` | Belge kendi V1 tanımına göre eksik. Identity'nin token sağlayıcıları hazır, kod yok. |

Ayrıca `clubs.write` izni katalogta var ve **hiçbir yerde kullanılmıyor** (kulüp oluşturma/güncelleme servisi yok), `AuditLog` yazılıyor ama okunamıyor, referans verisi yalnızca create+list.

**Dördüncü boşluk — sistemin kamuya bakan yüzü yok.** Bugün `/login` dışındaki her rota `ProtectedRoute` arkasında; giriş yapmamış bir öğrenci hangi kulüpler var, ne etkinlik düzenleniyor göremiyor. Anonim uç sayısı ikidir: `GET /api/diagnostics/ping` ve `GET /api/files/{id}` (yalnızca `FileVisibility.Public`). Bu, Faz 14'ün konusudur ve **anonim yüzey açmak Y-21/Y-52'ye doğrudan temas ettiği için ayrı kural ve kararlar gerektirir**.

**Hedef:** V1'in mimari disiplinini (Y-01…Y-52, beş katman, tek paradigma) hiç bozmadan, yedi bağımsız fazda hem eksik işlevi tamamlamak hem de arayüzü kurumsal kimlikle yeniden tasarlamak.

### Onaylanan kararlar (bu oturum)

1. **`docs/MIMARI.md` güncellenir** — belgenin kendi kuralı gereği (*"kapsam değişikliği gerekirse önce burası güncellenir, sonra kod"*) yeni K/A/Y maddeleri koddan **önce** belgeye yazılır.
2. **Tam yeniden tasarım** — tema + sidebar kabuk + her sayfanın içeriğinin yeniden kurgulanması.
3. **Kayıt + e-posta doğrulama + şifre sıfırlama** uygulanır (K-03'ün tamamlanması).
4. **Faz sırası:** tasarım sistemi → kulüp yönetimi → etkinlik katılımı/duyurular → hesap yaşam döngüsü → dashboard/self-servis → denetim/referans → **herkese açık vitrin ve ana sayfa** (kullanıcı talebiyle eklendi).

---

## 0. Kurumsal renk sistemi (A-37)

Ekteki kurumsal renk kılavuzundan alınan beş renk. **Turkuaz birincil marka rengi** olarak kullanılır (kullanıcı talebi).

| Token | Hex | RGB | MUI eşlemesi | Kullanım |
|---|---|---|---|---|
| **Turkuaz** | `#12A7CD` | 18·167·205 | `primary.main` | Marka, aktif menü, link, odak halkası, seçili sekme |
| **Lacivert** | `#262F59` | 38·47·89 | `secondary.main` | Sidebar zemini, başlıklar, koyu yüzeyler |
| **Turuncu** | `#EF7F1A` | 239·127·26 | `warning.main` | `PendingApproval`, `Queued`, bekleyen işlem rozetleri |
| **Gold** | `#B99C71` | 185·156·113 | `palette.accent` (özel) | `President`/`Officer` rozetleri, öne çıkan kart kenarı |
| **Gri** | `#A0A0A0` | 160·160·160 | `grey` skalası + `divider` | Kenarlık, ikincil metin, pasif durum |

> **Kaynak belgedeki tutarsızlık:** GRİ satırında CMYK `0-0-0-50` ve RGB `160-160-160` (`#A0A0A0`) yazarken basılı hex `#727271` görünüyor — bunlar farklı renkler. Plan **RGB'yi kanonik** kabul eder: `#A0A0A0` → kenarlık/pasif, `#727271` → `text.secondary`. Uygulama başlarken tek satırlık teyit alınır.

**Kontrast (WCAG AA) — göz ardı edilmeyecek detay:** turkuaz `#12A7CD` üzerine beyaz metin **2.8:1** ile AA'yı geçemez. Bu yüzden:
- `primary.main = #12A7CD` yalnızca **zemin üstü vurgu** (kenarlık, ikon, chip, aktif çizgi) olarak kullanılır.
- `primary.dark = #0B6E87` (beyaz metinle **5.9:1**) `MuiButton` `contained` varyantının zeminidir.
- Lacivert sidebar üzerinde turkuaz metin **4.6:1** ile AA'yı geçer → aktif menü vurgusu güvenli.

**Y-56 (yeni kural):** marka renkleri bileşen kodunda hex olarak yazılmaz; yalnızca `src/theme/tokens.ts` içinde tanımlanır ve palet üzerinden tüketilir. *(Bugün `src/` altında sıfır hex var — bu kural mevcut temiz durumu kilitler.)*

**Tipografi:** `@fontsource-variable/inter` ile **self-hosted** Inter. Google Fonts CDN **kullanılmaz** — A-23 "tek origin, CORS açılmaz" kararı üçüncü taraf font isteğiyle çelişirdi. Inter'in Latin Extended alt kümesi Türkçe diyakritikleri (ğ ı ş ç ö ü) kapsar.

---

## Faz 8 — Tasarım sistemi ve uygulama kabuğu

Backend'e **hiç dokunulmaz**. Amaç: yeni sayfaların üzerine kurulacağı zemini önce atmak.

### 8.1 Tema modülü (yeni)

```
arayuz/src/theme/
  tokens.ts      marka hex sabitleri (tek kaynak — Y-56)
  index.ts       createTheme(): palette, typography, shape, components
```

`components` bloğunda merkezîleştirilecek olanlar: `MuiButton` (`textTransform: 'none'`, `borderRadius: 8`, contained → `primary.dark`), `MuiPaper` (yumuşak gölge, `borderRadius: 12`), `MuiCard`, `MuiChip`, `MuiDataGrid` (kenarlıksız, başlık zemini `grey.50`, satır hover turkuaz `alpha(.06)`), `MuiTextField` (`size: 'small'` varsayılan), `MuiAppBar`.

`App.tsx:16`'daki `const theme = createTheme()` bu modülden import edilir. **Bugün `src/` altında hiç hex yok** → palet değişimi tüm uygulamaya temiz yayılır.

### 8.2 Kabuk (sidebar layout)

| Dosya | İş |
|---|---|
| `components/layout/AppShell.tsx` | Kalıcı `Drawer` (md+) / geçici drawer (xs) + `AppBar` + `<Outlet/>`. `AppLayout.tsx`'in yerini alır. |
| `components/layout/SideNav.tsx` | İkonlu, izin bazlı menü; `useLocation` ile **aktif rota vurgusu**; gruplu (Genel / Yönetim). |
| `components/layout/TopBar.tsx` | Sayfa başlığı, kullanıcı menüsü (e-posta + çıkış), Hangfire linki. |
| `components/layout/PublicLayout.tsx` | **Giriş yapmamış ziyaretçi kabuğu**: sidebar yok; sade üst bar (logo + "Giriş Yap" / "Kayıt Ol") + footer. Faz 14'te dolar, iskeleti burada kurulur ki tema tek seferde oturtulsun. |

**Mevcut `NavBar.tsx`'in üç sorunu birden çözülür:** 7 buton mobilde taşıyor (drawer), aktif rota vurgusu yok (`NavLink`), kullanıcı kimliği görünmüyor.

> Kullanıcı kimliği için **backend değişikliği gerekmez**: `tokenStore.ts` bugün JWT'den `permission` claim'ini çözüyor; aynı yerde `email`/`unique_name` claim'i de çözülür ve `AuthContext` `user: { email }` alanını açar.

### 8.3 Ortak bileşenler — kopyala-yapıştırın sonu

Bugün `Snackbar` bloğu **6 sayfada**, `type Snack` 3 sayfada, `DEFAULT_PAGE_SIZE` 6 sayfada, durum→renk haritaları her sayfada ayrı ayrı yazılı.

| Bileşen | Ne yapar |
|---|---|
| `notifications/NotifierProvider.tsx` + `useNotifier()` | Uygulama kökünde **tek** Snackbar. 6 sayfadaki bloğu siler. |
| `components/ui/PageHeader.tsx` | Başlık + açıklama + sağ aksiyon slotu |
| `components/ui/SectionCard.tsx` | Başlıklı `Paper` sarmalayıcı |
| `components/ui/StatCard.tsx` | KPI kutusu (ikon, büyük sayı, etiket, trend) |
| `components/ui/StatusChip.tsx` | `ApplicationStatus`/`EventStatus`/`ReportStatus`/`ClubRole` → Türkçe etiket + renk, **tek yerde** |
| `components/ui/DataTable.tsx` | `DataGrid` sarmalayıcı: sunucu sayfalama varsayılanları, `trTR` locale, boş/yükleniyor overlay'i, esnek yükseklik (`height: 480` sabitini kaldırır) |
| `components/ui/ConfirmDialog.tsx` | Silme/geri alınamaz işlemler için |
| `components/ui/EmptyState.tsx` | İkon + mesaj + aksiyon |
| `hooks/usePagedQuery.ts` | Tekrar eden TanStack Query + `paginationModel` kalıbı |

**Rota yapısı** iç içe rotalara (`<Route element={<AppShell/>}><Route .../></Route>`) çevrilir → `App.tsx`'te her rota için tekrarlanan `<ProtectedRoute><AppLayout>` sarmalayıcısı kalkar.

### 8.4 Mevcut 7 sayfanın yeniden kurgusu

| Sayfa | Değişim |
|---|---|
| `LoginPage` | İki panelli: solda lacivert marka paneli, sağda form. Alt linkler (kayıt/şifremi unuttum) Faz 11'de bağlanır. |
| `ClubsPage` | DataGrid → **kart galerisi** (logo, ad, açıklama, üye sayısı rozeti, "Detay"). Arama + aktif/pasif filtresi. |
| `EventsPage` | Kulüp seçmeye zorlayan mevcut akış kaldırılır (bugün kulüp seçilene kadar boş). Varsayılan görünüm "yaklaşan tüm etkinlikler" (Faz 10 ucu), kulüp filtresi opsiyonel olur. |
| `MembershipReviewPage` | `PageHeader` + `DataTable` + `ConfirmDialog`; durum filtresi. |
| `ReportsPage` | Metin kartları → `StatCard` + `@mui/x-charts` (MIT — Y-50 yalnızca **ücretli** MUI katmanını yasaklar) dönem grafiği. |
| `AuthorizationPage` | İzin checkbox listesi kategorilere gruplanır; rol satırları kart görünümü. |
| `ReferenceDataPage` | Fakülte→bölüm master-detail düzeni korunur, `SectionCard` ile toparlanır. |

### 8.5 Yeni paketler

`@mui/icons-material`, `@fontsource-variable/inter`, `@mui/x-charts`, `@mui/x-date-pickers` (native `type="date"` yerine). **Hepsi MIT/ücretsiz katman → Y-50 yeşil.**

### Çıkış koşulu
`npm run build` + `npm run lint` temiz · yedi sayfa da yeni kabukta açılıyor · tarayıcıda canlı doğrulama (Faz 5-7'deki Playwright yürüyüşü) · **backend'de sıfır değişiklik** (`git diff --stat src/` boş).

---

## Faz 9 — Topluluk yönetimi ve üye rolleri

Ölü kodu canlandıran faz. `clubs.write` ilk kez kullanılır.

### 9.1 Uçlar

| Metot | Rota | İzin | Y-23 kapsam kuralı |
|---|---|---|---|
| POST | `/api/clubs` | `clubs.write` | — (danışman `AcademicStaff` olarak var olmalı) |
| PUT | `/api/clubs/{id}` | `clubs.write` | Kulübün danışmanı **veya** `clubs.write` taşıyan yönetici |
| PUT | `/api/clubs/{id}/status` | `clubs.write` | aynı |
| GET | `/api/clubs/{id}/members` | `memberships.read` | Danışman / o kulüpte Officer-President / `reports.read.all` |
| PUT | `/api/clubs/{id}/members/{membershipId}/role` | `memberships.write` | **Yalnızca danışman veya o kulübün President'i** |
| DELETE | `/api/clubs/{id}/members/{membershipId}` | `memberships.write` | aynı (soft delete — Y-16) |
| GET | `/api/academic-staff` | `reference.manage` | — (danışman seçici için sayfalı liste) |

### 9.2 İş kuralları

- Kulüp adı unique index'te → önden kontrol, Türkçe `Conflict` (500 yerine 409).
- Danışman `AcademicStaff` kaydı yoksa → `NotFound`.
- **Başkan tekilliği (A-39):** bir kulüpte bir dönemde tek `President`. A-15 gereği **veritabanı son sözü söyler** → filtreli unique index:
  `HasIndex(m => new { m.ClubId, m.AcademicTermId }).IsUnique().HasFilter("[ClubRole] = 2 AND [IsDeleted] = 0")`
  Business önden kontrol edip `Conflict` döner; index yarış durumunu kapatır.
- Son President çıkarılamaz/düşürülemez → `Conflict` (`CannotRemoveOwnAdminRole` muhafızının aynı sınıfı).
- Bekleyen başvurusu olan kulüp pasife alınamaz → `Conflict`.

### 9.3 Kritik: cache düşürme

`ClubManager.GetListPagedAsync` `[CacheAspect(5)]` taşıyor. **Her kulüp yazma metodu `[CacheRemoveAspect("ClubManager.")]` almalı** — precedent: `FileManager.UploadClubLogoAsync`. Atlanırsa yeni kulüp 5 dakika listede görünmez.

Aspect sırası Faz 7'de kurulan sözleşmeye uyar: `[SecuredOperation] [ValidationAspect] [CacheRemoveAspect] [TransactionAspect]` — cache, commit'ten **sonra** düşsün diye transaction'ı sarar.

### 9.4 Dokunulan dosyalar

`IClubService`/`ClubManager` (genişletme) · yeni `IClubMemberService`/`ClubMemberManager` · `DTOs/Clubs/` (Create/Update/ClubMemberListItem/SetClubRole) · `ValidationRules/` · `ClubsController` genişletme + yeni `ClubMembersController`, `AcademicStaffController` · `ClubMembershipConfiguration` (President index'i) · migration.

**Frontend:** `ClubsPage` kart galerisine oluştur/düzenle diyalogları · yeni `ClubDetailPage` (`/clubs/:id`) sekmeli: *Genel · Üyeler · Etkinlikler · Duyurular* (son iki sekme Faz 10'da dolar).

### Çıkış koşulu
Bir öğrenciye `President` verilip **`EventManager.EnsureClubWriteAccessAsync`'in Officer/President dalı ilk kez uçtan uca çalışıyor**; `ReportScopeResolver` officer kapsamı gerçek veriyle test ediliyor.

> **Durum:** Tamamlandı. Backend: `IClubService` (Create/Update/SetStatus) + yeni `IClubMemberService`
> (`ClubMemberManager` — GetMembersPagedAsync/SetRoleAsync/RemoveMemberAsync), `ClubMembersController`,
> `AcademicStaffController` (+ `IAcademicStaffDal` — AcademicStaff↔Identity email join'i), President
> filtreli unique index migration'ı (`20260824_TopluluklarVeUyeRolleri` — tek `CreateIndex`).
> Frontend: `ClubsPage`'e oluştur diyaloğu, yeni `ClubDetailPage` (Genel/Üyeler sekmeleri, rol değiştir/çıkar).
> Test: 13 yeni Business.Tests (`ClubManagerTests`, `ClubMemberManagerTests`) + 4 yeni
> `WebAPI.IntegrationTests` (`ClubMemberManagementTests` — danışman öğrenciyi President yapar → öğrenci
> etkinlik oluşturabilir kanıtı, A-39 ikinci-President 409, Y-23 kapsam red'leri). 173/173 test yeşil,
> canlı Playwright doğrulaması yapıldı.

---

## Faz 10 — Etkinlik katılımı ve duyurular

İki atıl entity'yi hayata geçirir. MIMARI.md K-16 notu bunu açıkça kapsamda tutuyor: *"Etkinlik katılım kaydı V1'de var."*

### 10.1 Yeni izinler

| İzin | Kapsam | Seed |
|---|---|---|
| `announcements.write` | Kulüp duyurusu oluştur/düzenle/sil | Admin, ClubOfficer, Advisor |
| `announcements.global` | **Kulübe bağlı olmayan sistem duyurusu** (`ClubId = null`) — Faz 14'ün ana sayfa duyuruları | Admin |

Etkinliğe **kayıt olmak izin gerektirmez** — `MembershipApplicationManager.ApplyAsync`'in `[SecuredOperation]`'sız precedent'i izlenir (her kimliği doğrulanmış öğrenci).

### 10.1b Duyuru görünürlüğü (A-43) — Faz 14'ün ön koşulu

`Announcement` bugün `ClubId` (zorunlu) + `Title` + `Content` + `PublishedAtUtc` taşıyor; **görünürlük alanı yok**. Anonim vitrin ucu bu alan olmadan hangi duyurunun herkese açık olduğunu tahmin etmek zorunda kalırdı.

Y-52'nin `StoredFile.Visibility` kararı birebir kopyalanır — **görünürlük yazma anında belirlenir, okuma anında yorumlanmaz**:

- `Announcement.Visibility` → `AnnouncementVisibility { Members = 0, Public = 1 }`, varsayılan `Members`.
- `Announcement.ClubId` **nullable** olur (`null` = sistem duyurusu, `announcements.global` gerektirir). FK `SetNull` → `Restrict` yerine, kulüp silinse bile duyuru kalır.
- Migration: `Visibility` sütunu (default 0 — **mevcut satırlar sessizce herkese açılmaz**), `ClubId` nullable, `HasIndex(a => new { a.Visibility, a.PublishedAtUtc })`.

**Y-57 (yeni kural):** duyuru görünürlük alanı olmadan kaydedilemez; anonim vitrin ucu yalnızca `Visibility == Public` **ve** silinmemiş kaydı döndürür. *(Y-52'nin duyuru karşılığı.)*

### 10.2 Etkinlik uçları

| Metot | Rota | İzin | Not |
|---|---|---|---|
| POST | `/api/events/{id}/participation` | — (authenticated) | Kendi kaydı; `Published` + başlamamış olmalı |
| DELETE | `/api/events/{id}/participation` | — (authenticated) | Kendi kaydını iptal (soft — Y-16) |
| GET | `/api/events/{id}/participants` | `events.write` + kapsam | Sayfalı katılımcı listesi |
| GET | `/api/events/mine` | `events.read` | Kayıtlı olduğum etkinlikler |
| GET | `/api/events/upcoming` | `events.read` | **Tüm kulüplerde yayındaki yaklaşan etkinlikler** (bugün kulüp seçmeden liste yok) |
| PUT | `/api/events/{id}` | `events.write` + kapsam | Yalnızca `Draft`/`Rejected` iken |
| DELETE | `/api/events/{id}` | `events.write` + kapsam | Yalnızca `Draft` iken (soft) |

### 10.3 Kontenjan eşzamanlılığı (A-38) — fazın teknik kalbi

`Event.RowVersion` bugün tanımlı ama **hiç kullanılmıyor**; `Event.Capacity` yazılıyor ama okunmuyor. A-15 "unique index + rowversion son sözü söyler" diyor, Y-18 "kontenjan kuralını yalnızca uygulama kodunda tutmak" yasak.

`RegisterAsync` — `[TransactionAspect]` altında:
1. Etkinliği **izlenen** hâlde oku; `Published` değilse veya başlamışsa → `Conflict`.
2. `Capacity is not null` ise aktif katılımcı sayısını say; `>= Capacity` → `Conflict(Messages.EventCapacityFull)`.
3. `EventParticipation` ekle **ve** `eventRepository.Update(@event)` çağır → satır sürümü kontrolü zorlanır.
4. `SaveChangesAsync`. `DbUpdateConcurrencyException` → `Conflict(Messages.EventCapacityFull)`.

Böylece iki eşzamanlı kayıt aynı `RowVersion`'ı hedefler, biri kaybeder → **son kontenjan iki kişiye satılamaz**. Çift kayıt zaten `(EventId, StudentId)` unique index'iyle kapalı.

**Y-53 (yeni kural):** kontenjan yalnızca uygulama kodunda sayılarak korunamaz; yazma işlemi `Event.RowVersion`'ı tüketmelidir.

### 10.4 Duyuru uçları

`GET /api/clubs/{clubId}/announcements` (`clubs.read`) · `GET /api/announcements` (`clubs.read`, akış) · `POST /api/clubs/{clubId}/announcements` · `PUT|DELETE /api/announcements/{id}` (`announcements.write` + kapsam; silme **soft** — Y-16) · `POST /api/announcements` (`announcements.global`, sistem duyurusu).

Oluşturma DTO'su `Visibility` alanını **zorunlu** taşır (varsayılana güvenilmez) — arayüzde "Herkese açık / Yalnızca üyeler" seçimi.

### 10.5 E-posta bildirimi

`EventDecisionNotificationJob` — `MembershipDecisionNotificationJob`'ın birebir kalıbı (yalnızca `eventId` alır, kendi scope'unu açar, idempotent).

> **Dikkat — Y-41/Y-46:** `EventManager.DecideAsync` bugün `[TransactionAspect]` taşıyor. Kuyruğa ekleme commit'ten **sonra** olmak zorunda olduğu için bu metot, `MembershipApplicationManager.ReviewAsync` gibi **elle transaction** yönetimine çevrilir ve `[TransactionAspect]` kaldırılır.

**Frontend:** `EventsPage` yeniden kurgu (yaklaşan etkinlik kartları, "Katıl/Ayrıl", kontenjan göstergesi) · `EventDetailPage` (`/events/:id`) · `AnnouncementsPage` (`/announcements`) · `ClubDetailPage`'in Etkinlikler/Duyurular sekmeleri.

### Çıkış koşulu
Kontenjanı 1 olan etkinliğe **paralel** iki kayıt → tam biri başarılı (entegrasyon testi). Duyuru oluştur→akışta gör→soft delete çalışıyor.

> **Durum:** Tamamlandı. Backend: `IEventParticipationService`/`EventParticipationManager` (Register/Cancel/
> GetParticipants/GetMine — A-38 kontenjan RowVersion'ı `eventRepository.Update` ile tüketiyor),
> `IAnnouncementService`/`AnnouncementManager` (kulüp + sistem duyurusu, A-43), `EventDecisionNotificationJob`
> (`EventManager.DecideAsync` elle transaction'a çevrildi — `MembershipApplicationManager.ReviewAsync`
> precedent'i), `IEventService`'e `UpdateAsync`/`DeleteAsync`/`GetByIdAsync`/`GetUpcomingAsync` eklendi.
> Migration: `Announcement.Visibility` + nullable `ClubId` (SetNull) + yeni `announcements.write`/
> `announcements.global` claim'leri (tek `AlterColumn`+`AddColumn`+`CreateIndex`).
> **Plan dışı ek (implementasyon sırasında bulundu):** `GetPublishedAsync` yalnızca `Published` durumundaki
> etkinlikleri döndürüyor (Faz 7'den beri) — bu, kulüp yöneticisinin kendi taslak etkinliğini bile
> listede göremediği gerçek bir boşluktu. Yeni `IEventService.GetForClubAsync` (Y-23 kapsamlı, tüm
> durumlar) eklendi ve `ClubDetailPage`'in Etkinlikler sekmesi + `EventsPage`'in "Topluluk Etkinliklerim"
> sekmesi buna geçirildi. **Y-08 uyumu:** `DbUpdateConcurrencyException` Business'a sızamayacağı için
> yeni `Core.DataAccess.ConcurrencyConflictException` eklendi; `AppDbContext.SaveChangesAsync` override'ı
> çeviriyi yapıyor (mevcut `DomainConstraintTests.Event_ConcurrentCapacityUpdates_...` testi buna göre
> güncellendi).
> Frontend: `EventsPage` üç sekme (Yaklaşan Etkinlikler/Topluluk Etkinliklerim/Onay Kuyruğu), yeni
> `EventDetailPage`, yeni `AnnouncementsPage`, `ClubDetailPage`'e Etkinlikler/Duyurular sekmeleri.
> Test: 198/198 yeşil (135 Business.Tests + 10 Architecture.Tests + 53 WebAPI.IntegrationTests, yeni
> `EventCapacityConcurrencyTests` — `Task.WhenAll` ile paralel kayıt, tam biri başarılı — ve
> `AnnouncementFlowTests` dahil). Canlı Playwright doğrulaması yapıldı (danışman: kulüp→etkinlik
> oluştur→onaya gönder→onayla→duyuru yayınla; öğrenci: yaklaşan etkinliğe katıl/ayrıl, duyuru akışında gör).

---

## Faz 11 — Hesap yaşam döngüsü (K-03'ün tamamlanması, A-40)

### 11.1 Uçlar (`AuthController`, `[AllowAnonymous]`)

`POST /api/auth/register` · `POST /api/auth/confirm-email` · `POST /api/auth/resend-confirmation` · `POST /api/auth/forgot-password` · `POST /api/auth/reset-password`
Kimlik doğrulamalı: `POST /api/auth/change-password` · `GET|PUT /api/me` (profil).
Yönetici (`roles.manage`): `POST /api/users` · `PUT /api/users/{id}/lockout`.

`register` bir `ApplicationUser` (`EmailConfirmed = false`) + `Student` profili (öğrenci no, bölüm, kayıt yılı) oluşturur ve `Member` rolünü atar.

### 11.2 Güvenlik kuralları — atlanamaz olanlar

| # | Kural | Gerekçe |
|---|---|---|
| **Y-54 (yeni)** | E-posta doğrulama / şifre sıfırlama token'ı **Hangfire iş parametresine yazılmaz**. İş yalnızca `userId` alır, token'ı kendi scope'unda üretir. | Y-26: *"Hangfire iş parametreleri panelde okunabilir."* Token'ı parametre yapmak, panele erişen herkese hesap devralma verirdi. |
| **Y-55 (yeni)** | `forgot-password`, e-posta kayıtlı olsun olmasın **aynı** `Success` cevabını döner. | Kullanıcı sayımı (enumeration) sızıntısı; Y-25'in aynı ailesi. |
| — | Doğrulanmamış e-posta ile giriş reddedilir (`Messages.EmailNotConfirmed`). | Kayıt spam'i kullanılabilir hesap üretemez — K-13 (rate limiting) V1 dışı olduğu için tek pratik savunma bu. |
| — | Şifre sıfırlama token'ı tek kullanımlık (Identity `SecurityStamp` doğal olarak sağlar). | K-01'in refresh rotasyonuyla aynı ilke. |

> **Kabul edilen risk:** `register` ve `forgot-password` uçlarında oran sınırı yok (K-13 bilinçli olarak V1 dışı). Doğrulama zorunluluğu etkiyi sınırlar; MIMARI.md risk tablosuna yazılır.

### 11.3 Regresyon uyarısı — planın en kırılgan noktası

`SignIn.RequireConfirmedEmail` açıldığında **seed edilen admin/danışman/öğrenci hesapları giriş yapamaz hâle gelir** ve `WebAPI.IntegrationTests`'in tamamı kırmızıya döner (hepsi seed hesapla login oluyor).

**Çözüm:** `IdentitySeeder` üç demo hesabı da `EmailConfirmed = true` ile oluşturur/günceller. Bu, faz başlarken yapılacak **ilk** değişikliktir ve `AuthTests` yeşil kalana kadar başka bir şeye geçilmez.

**Frontend:** `/register`, `/confirm-email`, `/forgot-password`, `/reset-password`, `/profile` sayfaları · `LoginPage`'e alt linkler.

### Çıkış koşulu
kayıt ol → giriş **reddedilir** → e-postadaki linkle doğrula → giriş **başarılı** akışı entegrasyon testinde yeşil; mevcut `AuthTests` bozulmamış.

> **Durum:** Tamamlandı. `IdentitySeeder` zaten üç demo hesabı da `EmailConfirmed = true` ile seed
> ediyordu (Faz 3'ten kalma) — §11.3'ün regresyon riski gerçekleşmedi, `AuthTests` hiç dokunulmadan yeşil kaldı.
> Backend: `AddIdentityCore` `EmailTokenProvider` ile token sağlayıcısına kavuştu (`AddDefaultTokenProviders()`
> tam `Microsoft.AspNetCore.Identity` paketini/FrameworkReference'ı gerektirdiği için kullanılmadı — Core
> paketindeki `EmailTokenProvider` aynı SecurityStamp-tabanlı garantiyi FrameworkReference'sız veriyor).
> Yeni `IAccountService`/`AccountManager` (Register/ConfirmEmail/ResendConfirmation/ForgotPassword/
> ResetPassword/ChangePassword/GetMe/GetRegistrationDepartments) + `IAccountGateway` seam'i, yeni
> `EmailConfirmationJob`/`PasswordResetEmailJob` (Y-54: token job parametresine değil, çalışma anında
> üretiliyor). `AuthManager.LoginAsync`'e `EmailConfirmed` kontrolü eklendi — kasıtlı olarak parola
> kontrolünden SONRA (enumeration sızıntısını sınırlamak için). `IRoleAdminService`'e admin
> `CreateUserAsync`/`SetLockoutAsync` eklendi (K-03 akışını atlayan yönetici yüzeyi, Y-03 kendi-kendini-
> kilitleme guardı ile). `GET /api/me` uygulandı; `PUT /api/me` **kasıtlı olarak atlandı** — ne
> `ApplicationUser` ne `Student` üzerinde kullanıcının kendi güncelleyebileceği bir profil alanı yok
> (StudentNumber/DepartmentId/EnrollmentYear kurumsal alanlar); var olmayan bir alanı düzenlemek için
> uydurma bir DTO eklemek yerine bu kapsam dışı bırakıldı.
> **Plan dışı düzeltme:** eşzamanlı iki `confirm-email` isteği (ör. React StrictMode'un geliştirmede
> effect'i iki kez çalıştırması, çift tıklama, iki sekme) `ApplicationUser.ConcurrencyStamp` çakışmasıyla
> 500 dönüyordu — `AccountManager.ConfirmEmailAsync` artık `ConcurrencyConflictException`'ı yakalayıp
> bunu "büyük olasılıkla az önce başka bir istek zaten doğruladı" sayarak başarı döndürüyor.
> `SmtpEmailSender`, `Smtp:Host` boşken artık e-posta gövdesini de loglar (yalnızca yerel konsol) —
> SMTP sunucusuz geliştirmede doğrulama/sıfırlama bağlantısı test edilebilsin diye.
> Frontend: `/register` (bölüm seçimi `GET /api/auth/departments` — anonim, dar izdüşüm), `/confirm-email`,
> `/forgot-password`, `/reset-password`, `/profile` sayfaları; `LoginPage`'e kayıt/parolamı unuttum linkleri;
> `TopBar`'a "Profilim" menü öğesi.
> Test: 219/219 yeşil (149 Business.Tests + 10 Architecture.Tests + 60 WebAPI.IntegrationTests, yeni
> `AccountLifecycleTests` — planın birebir çıkış koşulu + Y-55 enumeration testi + parola sıfırlama akışı —
> ve `UserAdministrationTests` dahil). Canlı Playwright doğrulaması yapıldı: kayıt → doğrulanmamışken
> giriş reddi → e-postadaki linkle doğrula → giriş başarılı → profil sayfası → parolamı unuttum →
> e-postadaki linkle yeni parola → yeni parolayla giriş.

---

## Faz 12 — Dashboard ve öğrenci self-servisi (K-25)

- `GET /api/dashboard` — **rol farkında** özet DTO. Y-42 gereği toplama SQL'de: yeni `IDashboardDal` doğrudan DTO'ya projekte eder, generic repository ile bellekte toplanmaz. Kapsam `IReportScopeResolver` precedent'iyle çözülür (öğrenci kendi verisini, danışman kulüplerini, admin hepsini görür).
- Sayfalar: `/panel` **DashboardPage** (`StatCard` şeridi + yaklaşan etkinlikler + bekleyen işler + `@mui/x-charts` dönem grafiği), `/my-clubs`, `/my-events`.
- Giriş sonrası varsayılan hedef ve `ProtectedRoute`'un izin eksikliğinde yaptığı yönlendirme `/clubs`'tan `/panel`'e taşınır. **`/` Faz 14'te herkese açık ana sayfaya ayrılacağı için dashboard'a verilmez.**

### Çıkış koşulu
Üç farklı rolle giriş → her biri **yalnızca** kendi kapsamındaki sayıları görüyor (kapsam sızıntısı testi).

### Durum: Tamamlandı

- `IDashboardDal`/`EfDashboardDal` (DataAccess) — `IReportDal` precedent'i birebir izlendi: `GetPersonalStatsAsync` (öğrenci profiline bağlı kulüp/bekleyen başvuru/yaklaşan etkinlik sayıları) ve `GetManagementStatsAsync` (kapsam kulüplerindeki toplam üye/bekleyen başvuru/yaklaşan etkinlik) COUNT'ları tamamen SQL'de, generic repository'ye hiç dokunmadan üretiliyor. Sonuç DTO'ları (`PersonalDashboardStatsDto`, `ManagementDashboardStatsDto`) DAL sınır katmanı olduğu için `Entities/Dtos/Dashboard/` altında — `TermSummaryRowDto` ile aynı gerekçe (DataAccess, Business'a referans veremez).
- `IDashboardService`/`DashboardManager` (Business) — `[SecuredOperation]` **bilinçli olarak yok**: herhangi bir kimliği doğrulanmış kullanıcı kendi özetini görebilir (`MembershipApplicationManager.ApplyAsync` precedent'i). Kapsam ayrımı izin claim'iyle değil, mevcut `IReportScopeResolver` ile içeride çözülüyor — **yeni bir kapsam çözücü yazılmadı**, Faz 6'daki bileşen aynen yeniden kullanıldı. Bir öğrenci profili varsa `Personal` her zaman dolu; `IReportScopeResolver` boş kapsam dönerse (`ReportScope.None` — düz `Member`, `reports.read` yok) `Management` `null` kalır ve dönem trend grafiği hiç üretilmez — kapsamı olmayan kullanıcı için `IReportDal.GetTermSummaryAsync` hiç çağrılmaz.
- `GET /api/dashboard` (`DashboardController`) — tek action, ek izin yok (global fallback policy zaten kimlik doğrulaması şart koşuyor).
- `GET /api/clubs/mine` (`IClubMemberService.GetMineAsync` + `ClubsController`) — planda açıkça yazılmayan ama `/my-clubs` sayfası için zorunlu olan küçük bir ek: çağıranın kendi `ClubMembership` kayıtlarını kulüp adıyla birlikte döner. `[SecuredOperation(ClubsRead)]` taşır — `EventParticipationService.GetMineAsync`'in `EventsRead` gerektiren precedent'iyle aynı sınıf. `/my-events` için ayrı bir uç YAZILMADI — Faz 10'da zaten var olan `GET /api/events/mine` doğrudan yeniden kullanıldı.
- Frontend: `DashboardPage` (`/panel`) — kişisel `StatCard` şeridi + (varsa) "Yönetim Kapsamım" bölümü (`AllClubs` ise "Tüm topluluklar" rozeti) + (birden fazla dönem varsa) `@mui/x-charts` dönem trend grafiği + "Yaklaşan Etkinlikler" listesi (`/events/upcoming`'i yeniden kullanır) + (yalnızca `memberships.write` taşıyanlarda) "Bekleyen Üyelik Başvuruları" listesi (`/membership-applications`'ı yeniden kullanır). `MyClubsPage` (`/my-clubs`) ve `MyEventsPage` (`/my-events`) — kart galerisi, mevcut `ClubRoleChip`/`EventStatusChip` yeniden kullanıldı.
- `ProtectedRoute`'un izin-eksik yönlendirmesi ve `LoginPage`'in giriş-sonrası hedefi `/clubs` → `/panel` olarak güncellendi; wildcard rota da `/panel`'e yönlendiriyor. `SideNav`'a izin gerektirmeyen "Panel" (en üstte) ve "Kulüplerim"/"Etkinliklerim" linkleri eklendi.
- Testler: `DashboardManagerTests` (3 test — düz üye/AllClubs admin/officer kapsamının `GetManagementStatsAsync`'e YALNIZCA kendi `clubIds`'iyle geçtiğinin `Times.Once` + parametre eşleşmesiyle kanıtı) + `ClubMemberManagerTests`'e 2 yeni test (`GetMineAsync` kabul/öğrenci-profili-yok) + `DashboardScopeTests` (4 entegrasyon testi — çıkış koşulunun birebir kanıtı: düz üye yalnızca kişisel görür, danışman yalnızca kendi kulübünü sayar, admin `AllClubs=true` ile en az iki farklı danışmanın kulübünü görür, `/api/clubs/mine` yalnızca çağıranın üyeliğini döner). Toplam 228/228 test yeşil (154 Business.Tests + 10 Architecture.Tests + 64 WebAPI.IntegrationTests).
- Canlı doğrulama: üç seed demo hesabıyla (`ogrenci@`/`danisman@`/`admin@ogrencitoplulugu.local`) sırayla giriş yapılıp `/panel` ekran görüntüleriyle teyit edildi — öğrencide yönetim bölümü hiç yok, danışmanda yalnızca kendi kulübü (1) sayılıyor, adminde "Tüm topluluklar" rozeti çıkıyor; `/my-clubs` ve `/my-events` boş durumları doğru render ediyor; SideNav "Panel" linki çalışıyor. Konsol/HTTP hatası sıfır.

---

## Faz 13 — Denetim izi ve referans veri olgunluğu

- **Yeni izin `audit.read`** (yalnız Admin). `GET /api/audit-logs` — sayfalı, `entityType`/`entityId`/`userId`/tarih filtreli. K-12 ilk kez görünür olur.
  > Y-26 muhafızı: `AuditSaveChangesInterceptor` parola/token/hash alanlarını zaten dışarıda bırakıyor; uç bu varsayımı **teste bağlar** (hassas alan adı içeren kayıt dönmediği doğrulanır).
- Referans verisi olgunlaşır: `PUT /api/faculties/{id}`, `PUT /api/academic-terms/{id}`, `PUT|DELETE /api/faculties/{id}/departments/{deptId}`. A-12 gereği referans verisi **hard delete** edilebilir; FK'lar `Restrict` olduğu için kullanımdaysa Türkçe `Conflict` döner (`DbUpdateException` 500'e düşmez).
- Rapor kapsamı: `ReportType.TermSummary` Excel çıktısı eklenir.
- Sayfalar: `/audit` (AuditLogPage), `ReferenceDataPage`'e düzenle/sil aksiyonları.

---

## Faz 14 — Herkese açık vitrin ve ana sayfa (K-27, A-42)

Giriş yapmamış ziyaretçinin gördüğü ana sayfa: sistem duyuruları, yaklaşan etkinlikler, kulüp vitrini.
**Bağımlılığı yalnızca Faz 10 (duyuru görünürlüğü) ve Faz 11 (kayıt linki)'dir** — istenirse Faz 12'den önce öne alınabilir.

### 14.1 Neden ayrı kural gerekiyor

Y-21 açıkça *"uçların varsayılan olarak anonim olması"*nı yasaklıyor ve anonim uçların **tek tek** `[AllowAnonymous]` ile açılmasını, üstelik **Y-52'ye tabi** olmasını şart koşuyor. Bugün sistemde yalnızca iki anonim uç var. Vitrin, bu yüzeyi genişletiyor — dolayısıyla genişleme **dar, tek dosyada toplanmış ve denetlenebilir** olmak zorunda.

**A-42 kararı — anonim yüzey tek bir dilimde toplanır:**

| İlke | Uygulama |
|---|---|
| Tek rota öneki | Bütün anonim içerik uçları `/api/public/*` altında |
| Tek controller | `PublicContentController` — **projedeki tek çok-action'lı `[AllowAnonymous]` dosyası**; `git diff` ile denetlenebilir |
| Tek servis | `IPublicContentService` / `PublicContentManager` — `[SecuredOperation]` **yok** (bilinçli), filtreler **kodda sabit** |
| Ayrı DTO ailesi | `DTOs/Public/` — mevcut yetkili DTO'lar yeniden kullanılmaz, böylece bir alan eklendiğinde anonim uca sessizce sızamaz |

> **Kritik tasarım gerekçesi:** `ClubListItemDto` gibi mevcut bir DTO'yu anonim uçta yeniden kullanmak, ileride o DTO'ya eklenecek herhangi bir alanın (ör. danışman e-postası) **kimse fark etmeden kamuya açılması** demektir. Ayrı DTO ailesi bu sızıntı sınıfını yapısal olarak kapatır.

### 14.2 Uçlar

| Metot | Rota | Döndürdüğü | Kodda sabit filtre |
|---|---|---|---|
| GET | `/api/public/announcements` | Sistem + kulüp duyuruları | `Visibility == Public && !IsDeleted`, tarihe göre azalan |
| GET | `/api/public/events` | Yaklaşan etkinlikler | `Status == Published && StartDateUtc >= now && !IsDeleted` |
| GET | `/api/public/clubs` | Kulüp vitrini | `IsActive && !IsDeleted` |
| GET | `/api/public/clubs/{id}` | Kulüp tanıtım detayı | aynı + bulunamazsa `NotFound` |

Hepsi sayfalı (Y-11: varsayılan 20, üst sınır 100). Logo ve afişler zaten çalışan `GET /api/files/{id}` anonim ucundan gelir — **koşul: kulüp logosu ve etkinlik afişi `FileVisibility.Public` ile yüklenmiş olmalı** (Y-52). Faz 14 başlarken `FileManager.UploadClubLogoAsync` / `UploadEventPosterAsync`'in görünürlük ataması teyit edilir.

### 14.3 Anonim uçtan **asla** dönmeyecek alanlar

**Y-58 (yeni kural):** anonim vitrin ucu kişisel veri döndüremez.

| Yasak | Neden |
|---|---|
| Danışman/başkan **adı, e-postası, kullanıcı Id'si** | Kişisel veri; K-19 (KVKK) V1 dışı olduğu için yüzey büyütülmez |
| Üye listesi, katılımcı listesi, öğrenci numarası | Aynı |
| Taslak/onay bekleyen/reddedilmiş etkinlik | İç iş akışı sızıntısı |
| Pasif kulüp, silinmiş kayıt | Y-16 soft delete'in anlamı |
| Herhangi bir sayısal Id dışında iç alan (`RowVersion`, `AdvisorId`, `LogoFileId` hariç) | Yüzeyi minimumda tutmak |

İzin verilen: kulüp adı/açıklaması/logosu, **üye sayısı** (toplu sayı — kişisel veri değil), etkinlik başlığı/açıklaması/yeri/tarihi/kontenjanı/afişi, duyuru başlığı/içeriği/tarihi/kulüp adı.

### 14.4 Cache ve kötüye kullanım

Vitrin sonuçları **kullanıcıya özel değildir** → MIMARI.md'nin `CacheAspect` uyarısı (*"kullanıcıya özel sonuçlarda kullanılmaz"*) ihlal edilmez, aksine ideal hedeftir. Üç okuma ucu da `[CacheAspect(10)]` alır; Faz 9/10'daki ilgili yazma uçlarına `[CacheRemoveAspect("PublicContentManager.")]` eklenir.

> **Kabul edilen risk:** anonim uçlarda oran sınırı yok (K-13 bilinçli olarak V1 dışı). Karşılık: 10 dakikalık cache isteklerin çoğunu DB'ye indirmeden karşılar, sayfa boyutu 100'le sınırlı, uçlar salt-okunur. MIMARI.md risk tablosuna yazılır.

### 14.5 Frontend

| Rota | Sayfa | Kabuk |
|---|---|---|
| `/` | **HomePage** — hero (marka + "Kayıt Ol"/"Giriş Yap"), duyuru şeridi, yaklaşan etkinlik kartları, kulüp vitrini | `PublicLayout` |
| `/kulupler` | Herkese açık kulüp listesi (arama) | `PublicLayout` |
| `/kulupler/:id` | Kulüp tanıtımı + o kulübün açık duyuru/etkinlikleri | `PublicLayout` |
| `/etkinlikler` | Yaklaşan etkinlikler | `PublicLayout` |

**Rota yeniden düzeni:** `/` artık **herkese açık ana sayfa**. Faz 12'nin dashboard'u `/panel`'e taşınır. Giriş yapmış kullanıcı `/`'ı ziyaret ederse sayfa görünmeye devam eder, üst barda "Panele Git" çıkar (zorla yönlendirme yok — kullanıcı vitrini görmek isteyebilir). `ProtectedRoute`'un izin eksikliğinde yaptığı `/clubs` yönlendirmesi `/panel`'e çevrilir.

`PublicLayout` altındaki sayfalar `apiClient`'ı kullanır ama **`Authorization` başlığı olmadan da çalışmalıdır** — mevcut axios interceptor'ın 401'de sessiz refresh denemesi anonim sayfada sonsuz döngüye girmemeli. `/api/public/*` istekleri interceptor'ın refresh dalından muaf tutulur (**bu, fazın en somut regresyon riski**).

### Çıkış koşulu
Tarayıcıda **hiç giriş yapmadan** `/` açılıyor; duyuru/etkinlik/kulüp görünüyor; `Members` görünürlüklü bir duyuru **görünmüyor**; pasif kulüp listede yok; ağ sekmesinde `/api/public/*` cevaplarında hiçbir e-posta/öğrenci numarası geçmiyor.

---

## Adım 0 — Belgeler (koddan ÖNCE)

İlk commit iki dosyadır: bu planın repo içindeki kalıcı kopyası **`docs/PLAN-V2.md`** (bu dosya) ve güncellenmiş **`docs/MIMARI.md`**.

`docs/MIMARI.md`'ye eklenecekler:

**§3'e yeni V1+ maddeleri:** K-21 Topluluk yönetimi ve üye rolleri · K-22 Etkinlik katılımı · K-23 Duyurular · K-24 Hesap yaşam döngüsü (*K-03'ün tamamlanması*) · K-25 Dashboard ve self-servis · K-26 Denetim görüntüleme · **K-27 Herkese açık vitrin ve ana sayfa**.
K-16 satırına not: *"forum/mesajlaşma/anket/QR hâlâ V1 dışı; yalnızca katılım kaydı ve duyuru alındı."*

**§6'ya yeni kararlar:** A-37 kurumsal renk sistemi ve tasarım tokenları · A-38 kontenjan eşzamanlılığı (`Event.RowVersion`) · A-39 başkan tekilliği (filtreli unique index) · A-40 kayıt ve e-posta doğrulama · A-41 sidebar kabuk ve ortak bileşen katmanı · **A-42 anonim vitrin yüzeyi (tek önek, tek controller, ayrı DTO ailesi)** · **A-43 duyuru görünürlüğü ve sistem duyurusu**.

**§2'ye yeni yasaklar:** Y-53 kontenjan yalnızca uygulama kodunda korunamaz · Y-54 token'ın Hangfire parametresine yazılması · Y-55 kullanıcı sayımı (enumeration) sızıntısı · Y-56 koda gömülü marka rengi · **Y-57 görünürlük alanı olmayan duyuru / anonim uçtan yayında olmayan kayıt** · **Y-58 anonim uçtan kişisel veri dönmesi**.

**§4 domain tablosuna:** `Announcement` satırı `Visibility` ve nullable `ClubId` ile güncellenir (K-23, A-43, Y-57).

**§5'e Faz 8-14** ve her birinin "bitti sayılır" satırı. Başlıktaki sayaç tablosu (36 karar / 52 kural / 13 V1 dışı madde / 7 faz) → **43 karar / 58 kural / 13 V1 dışı madde / 14 faz**.

**§3'ün "V1 dışında kalanlar" tablosu değişmez** — K-02, K-04, K-07, K-09…K-20'nin tamamı ertelenmiş kalır. Özellikle: forum/mesajlaşma/anket/QR (K-16), aidat/ödeme (K-15), SignalR bildirim (K-04), rate limiting (K-13), KVKK silme akışı (K-19) bu planın **hiçbir fazında** yapılmaz.

> **Durum:** Adım 0 tamamlandı — `docs/MIMARI.md` v2.0'a güncellendi (43 karar / 58 kural / 14 faz), bu dosya (`docs/PLAN-V2.md`) repoya eklendi.

---

## Uygulama sırası ve çıkış koşulları

| # | Faz | Migration | Bitti sayılır |
|---|---|---|---|
| **0** | `docs/MIMARI.md` güncellemesi | — | Belge yeni K/A/Y maddelerini ve Faz 8-14'ü içeriyor |
| **8** | Tasarım sistemi + kabuk | — | `git diff --stat src/` boş; 7 sayfa yeni kabukta; build+lint temiz |
| **9** | Kulüp yönetimi + üye rolleri | President filtreli unique index | Officer/President dalı **ilk kez** uçtan uca çalışıyor |
| **10** | Etkinlik katılımı + duyurular | `Announcement.Visibility` + nullable `ClubId` + yeni claim'ler | Kontenjan 1 · paralel iki kayıt → tam biri başarılı |
| **11** | Hesap yaşam döngüsü | — (Identity şeması hazır) | kayıt→giriş reddi→doğrula→giriş başarılı; `AuthTests` bozulmamış |
| **12** | Dashboard + self-servis | — | Üç rol, üç farklı kapsam; sızıntı yok |
| **13** | Denetim + referans olgunluğu | `audit.read` claim'i | Audit ekranı PII sızdırmıyor; kullanımdaki fakülte silinemiyor (409) |
| **14** | Herkese açık vitrin + ana sayfa | — | Giriş yapmadan `/` çalışıyor; `Members` duyurusu ve pasif kulüp **görünmüyor** |

> **Faz 14 öne alınabilir:** tek sert bağımlılığı Faz 10 (duyuru görünürlüğü) ve Faz 11 (kayıt linki). Vitrin daha erken istenirse sıra 8 → 9 → 10 → 11 → **14** → 12 → 13 olarak uygulanır; plan bu değişimden etkilenmez.

*Sessiz onay korunur: her PR'da en fazla bir migration; adlandırma `YYYYMMDD_AçıklayıcıAd`.*

---

## Doğrulama

**Her fazda:**
- `dotnet build` **0 uyarı** (Y-31) · `dotnet test` dört proje yeşil · `npm run build` + `npm run lint` temiz.
- `Architecture.Tests` değişmeden yeşil — Y-01/02/05/07/08/09/27/30/37 yeni koda karşı da geçerli.
- Her yeni iş kuralı için **bir kabul + bir ret** testi (A-20).
- Tarayıcıda canlı yürüyüş (Faz 5-7'de kurulan Playwright kalıbı, scratchpad'deki `verify.mjs`).

**Faza özel kritik testler:**

| Test | Neyi kanıtlar |
|---|---|
| `EventCapacityConcurrencyTests` | Kontenjanı 1 olan etkinliğe `Task.WhenAll` ile iki kayıt → tam biri `Success`, biri `Conflict`. A-38'in tautolojik olmayan kanıtı. |
| `ClubPresidentUniquenessTests` | İkinci President ataması DB seviyesinde reddediliyor (A-39) |
| `ClubOfficerEventAccessTests` | President yapılan öğrenci etkinlik oluşturabiliyor — **ölü kodun canlandığının kanıtı** |
| `AccountLifecycleTests` | kayıt → giriş 401 → doğrula → giriş 200 |
| `PasswordResetTests` | Var olmayan e-posta ile aynı cevap (Y-55); token tek kullanımlık |
| `AuditLogEndpointTests` | Dönen kayıtlarda parola/token/hash alan adı yok (Y-26) |
| `ClubCacheInvalidationTests` | Yeni kulüp **anında** listede (Y-45'in `ClubManager` ayağı) |
| `PublicSurfaceLeakTests` | **Faz 14'ün kabul testi.** Token **hiç göndermeden** üç `/api/public/*` ucu 200 dönüyor; cevap gövdesinde seed edilen admin/danışman/öğrenci e-postası ve öğrenci numarası **geçmiyor**; `Members` duyurusu, `Draft`/`PendingApproval` etkinlik ve `IsActive = false` kulüp **yok**. |
| `PublicEndpointAuthorizationTests` | `/api/public/*` dışındaki hiçbir uç anonim erişime izin vermiyor (Y-21 regresyonu) — global fallback policy hâlâ yürürlükte |

**Bilinen bakım işi:** Faz 8'in yeniden tasarımı, scratchpad'deki mevcut Playwright betiğinin seçicilerini (`.MuiDataGrid-row`, `a:has-text(...)`) kıracak — betik Faz 8 sonunda yeni kabuğa göre güncellenir.

---

## Açık riskler

| Risk | Karşılık |
|---|---|
| `RequireConfirmedEmail` tüm entegrasyon testlerini kırar | Faz 11'in **ilk** işi `IdentitySeeder`'da `EmailConfirmed = true`; `AuthTests` yeşil olmadan devam edilmez |
| Turkuaz üzerine beyaz metin AA'yı geçmiyor (2.8:1) | `primary.dark = #0B6E87` contained butonların zemini; turkuaz yalnızca vurgu (§0) |
| Kulüp yazma uçlarında `[CacheRemoveAspect("ClubManager.")]` unutulur | Yeni kulüp 5 dk görünmez → `ClubCacheInvalidationTests` bunu yakalar |
| `DecideAsync`'ten `[TransactionAspect]` kaldırılırken elle transaction hatalı kurulur | `MembershipApplicationManager.ReviewAsync` birebir kopyalanacak referans; mevcut `EventApprovalFlowTests` regresyonu yakalar |
| Kayıt/şifre uçlarında oran sınırı yok (K-13 V1 dışı) | Doğrulama zorunluluğu etkiyi sınırlar; MIMARI.md risk tablosuna yazılır |
| Faz 8'in yüzeyi geniş (7 sayfa + kabuk) — regresyon riski | Backend'e sıfır dokunuş; her sayfa ayrı commit; `git diff --stat src/` boş kalmalı |
| Gri renk kaynakta tutarsız (`#A0A0A0` ↔ `#727271`) | Uygulama başlarken tek satırlık teyit; varsayılan RGB (§0) |
| **Anonim yüzey açmak kalıcı bir güvenlik yüzeyidir** | A-42'nin dört kısıtı (tek önek, tek controller, ayrı DTO ailesi, kodda sabit filtreler) + `PublicSurfaceLeakTests` + `PublicEndpointAuthorizationTests`. Yeni bir anonim uç eklemek `PublicContentController` dosyasını değiştirmeyi gerektirir → gözden kaçmaz. |
| Mevcut DTO'ya sonradan eklenen alan anonim uçtan sızar | `DTOs/Public/` ayrı ailesi bu sınıfı yapısal olarak kapatır (§14.1) |
| Anonim sayfada axios interceptor 401'de sonsuz refresh döngüsüne girer | `/api/public/*` istekleri refresh dalından muaf tutulur; Faz 14'ün ilk işi ve canlı doğrulamada ağ sekmesinden teyit edilir |
| `Announcement.ClubId` nullable'a çevrilirken mevcut sorgular/filtreler bozulur | Faz 10'da tek migration; `Visibility` default `Members (0)` → **mevcut satırlar sessizce kamuya açılmaz** |
| Kulüp logosu/etkinlik afişi `Protected` yüklenmişse vitrinde görsel çıkmaz | Faz 14 başlarken `FileManager`'ın görünürlük ataması teyit edilir (§14.2) |
| Anonim uçlarda oran sınırı yok (K-13 V1 dışı) | 10 dk cache + sayfa boyutu ≤100 + salt-okunur uçlar; MIMARI.md risk tablosuna yazılır |
| Yedi faz uzun sürer, ara teslim olmaz | Her faz **tek başına çalışır ve sevk edilebilir**; Faz 8 sonunda görsel yenileme, Faz 14 sonunda kamuya açık site tek başına teslim edilebilir |

### Kritik dosyalar

- `arayuz/src/App.tsx:16` — `createTheme()` seam'i, tüm tasarımın giriş noktası
- `arayuz/src/components/NavBar.tsx` — `AppShell`/`SideNav` ile değişecek
- `src/Business/Concrete/MembershipApplicationManager.cs:191` — `ClubRole.Member` sabiti (Faz 9'un hedefi) ve `:208` elle-transaction/enqueue precedent'i
- `src/Business/Concrete/EventManager.cs:190-222` — `EnsureClubWriteAccessAsync`, Faz 9'da canlanan kapsam kuralı
- `src/Business/Concrete/ReportScopeResolver.cs:61` — Officer/President kapsamı, Faz 9'da gerçek veriyle test edilir
- `src/DataAccess/Seed/IdentitySeedData.cs` — yeni izinler; **bir sonraki boş claim Id = 32**
- `src/DataAccess/Configurations/ClubMembershipConfiguration.cs` — President filtreli unique index (Faz 9)
- `src/Business/BackgroundJobs/MembershipDecisionNotificationJob.cs` — Faz 10/11'deki tüm e-posta işlerinin kalıbı
- `src/Business/Concrete/IdentitySeeder.cs` — Faz 11'in regresyon noktası (`EmailConfirmed`)
- `src/Entities/Announcement.cs` + `AnnouncementConfiguration.cs` — Faz 10'da `Visibility` + nullable `ClubId`; Faz 14'ün ön koşulu
- `src/WebAPI/Controllers/FilesController.cs` — mevcut anonim uç; A-42'nin `[AllowAnonymous]` + görünürlük filtresi precedent'i
- `src/WebAPI/Program.cs` — global fallback policy (Y-21); Faz 14'te **değiştirilmez**, yalnızca yeni controller `[AllowAnonymous]` alır
- `arayuz/src/api/client.ts` — Faz 14'te `/api/public/*` isteklerinin refresh interceptor'ından muaf tutulması
- `docs/MIMARI.md` — Adım 0'da güncellendi, sonrasında tek doğruluk kaynağı olarak kalır
