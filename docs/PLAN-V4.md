# V4 — Öğrenci self-servisi, yaşam döngüsü bütünlüğü ve arayüz cilası (Faz 19 → 23)

## Context

`docs/PLAN-V3.md`'nin dört fazı (15-18) bitti, test edildi ve pushlandı (son commit `4488dd9`; 277 test yeşil).
Sistem işlevsel olarak bütün: kayıt, üyelik, etkinlik, duyuru, panel, denetim, vitrin, topluluk kurma
başvurusu ve erişim izi çalışıyor.

V4 **yeni bir domain alanı açmıyor.** Kod okunarak doğrulanan dört boşluğu kapatıyor — üçü işlevsel
eksik, biri gerçek bir hata — ve bir arayüz cilası fazı ekliyor.

| # | Bulgu | Kanıt | Sonuç |
|---|---|---|---|
| **1** | **Öğrenci kendi üyelik başvurusunu takip edemiyor, geri çekemiyor, kulüpten ayrılamıyor** | `IMembershipApplicationService`'te yalnızca `ApplyAsync` + `GetPendingForAdvisorAsync` (danışman kuyruğu) var; `/mine` ucu yok. `IClubMemberService.RemoveMemberAsync` `memberships.write` istiyor — öğrenci kendi üyeliğini sonlandıramaz. | Öğrenci başvurdu, sonrası karanlık. Faz 17'de topluluk kurma başvurusuna `/club-applications/mine` yazıldı; **üyelik başvurusunda karşılığı yok** — asimetri. |
| **2** | **Yayınlanmış etkinlik iptal edilemiyor** | `EventStatus` = `Draft/PendingApproval/Published/Rejected` — `Cancelled` **yok**. `EventManager.DeleteAsync:294` yalnızca `Draft` iken çalışıyor. | Salon/konuşmacı/hava değişince kulübün elinde araç yok; kayıtlı öğrenciler haberdar edilemiyor. A-25 durum makinesinin eksik dalı. |
| **3** | **100'den fazla kayıtta sessiz veri kaybı** *(hata)* | Arayüzde **12 çağrı** `pageSize: 200` istiyor; `ClampPageSize` sessizce **100**'e kırpıyor (`MaxPageSize = 100`, Y-11). Arama istemci tarafında, yalnızca gelen 100 kayıt üzerinde (`ClubsPage.tsx:68` + `filteredClubs`). | 101. kulüp hiçbir zaman listelenemez, aranamaz. Hata mesajı da yok. |
| **4** | **Dönem geçişi sistemi fiilen sıfırlıyor** | `AcademicTermManager.SetCurrentAsync:75` yalnızca `IsCurrent` bayrağını değiştiriyor. Üyelik `(ClubId, StudentId, AcademicTermId)`'ye bağlı (A-13) ve `EventManager.EnsureClubWriteAccessAsync` **güncel dönemin** üyeliğine bakıyor. | Yeni dönem açıldığı an bütün başkan/yetkililer yetkisini kaybediyor, herkes yeniden başvurmak zorunda. Üstelik "Kulüplerim" (`ClubMemberManager.cs:41`) dönem filtresi **taşımıyor** → arayüz "başkansın" derken backend "değilsin" diyor. |

**Beşinci bulgu — ters proxy arkasında IP yanlış okunuyor.** `UseForwardedHeaders` **hiçbir yerde yok**;
`RequestLoggingMiddleware.cs:70` `context.Connection.RemoteIpAddress` okuyor. Kestrel'in doğrudan dinlediği
geliştirme ortamında doğru, ama IIS/nginx arkasına alındığı an **her satır proxy'nin IP'sini kaydeder** —
Faz 18'in (A-44) IP kolonu sessizce değersizleşir. Aynı sorun IP'ye göre bölümlenen bir rate limiter'ı da
**tüm kullanıcıları tek kovaya** koyarak işlevsiz bırakır. Bu yüzden Faz 19'un ilk maddesi.

**Hedef:** V1/V2/V3'ün mimari disiplinini (Y-01…Y-60, beş katman, tek paradigma) bozmadan bu beş boşluğu
kapatmak ve arayüzü ayrı bir fazda cilalamak.

---

## Onaylanan kararlar (2026-08-23)

| # | Karar | **Onaylanan** |
|---|---|---|
| **O-5** | Faz sırası | **1 → 2 → 3 → 4** (bulgu numaralarıyla): önce öğrenci self-servisi, sonra etkinlik iptali, sonra arama/sayfalama hatası, en son dönem devri. En küçük ve en çok kullanıcıya değen iş önce. |
| **O-6** | Dönem devri biçimi | **Otomatik devir.** Yeni dönem güncel yapıldığı an, önceki güncel dönemin aktif üyelikleri rolleriyle birlikte yeni döneme kopyalanır. |
| **O-7** | Etkinlik iptalinde bildirim | **Evet** — kayıtlı katılımcılara e-posta gider (Hangfire, commit sonrası — Y-41/Y-46). |
| **O-8** | Arayüz cilası | **Ayrı faz** (Faz 23). Backend'e sıfır dokunur; Faz 8/16 disiplini (`git diff --stat src/` boş). |
| **O-9** | Rate limiting | **Alınıyor** — K-13'ün *yalnızca* rate limiting yarısı kapsama girer (A-53); **API versiyonlama V1 dışında kalır**. Uygulanacağı yer Faz 19.5, kapsam yalnızca anonim kimlik uçları. |

---

## O-9'un gerekçesi ve sınırı

**Mevcut durum kanıtı:** `grep -rn "RateLimit" src/` → **sıfır sonuç.** API'de hiçbir oran sınırı yok.

Tek telafi edici kontrol Identity'nin **hesap kilitlemesi**: `AuthManager.cs:43` `IsLockedOutAsync`,
`:50` `AccessFailedAsync`, `:63` `ResetAccessFailedCountAsync`. Bu **yalnızca `login`'i ve yalnızca
hesap başına** korur (varsayılan 5 deneme / 5 dk). Korumasız kalanlar:

| Uç | Korumasız olmasının somut bedeli |
|---|---|
| `POST /api/auth/forgot-password` | **En kritik.** Bir kişinin adresine sınırsız sıfırlama e-postası gönderilebilir (e-posta bombardımanı) ve SMTP kotası tüketilir — Gmail'de 500/gün, birkaç dakikada biter. Kota bitince **sistemin tüm e-postaları** (kayıt doğrulama, onay bildirimi) durur. |
| `POST /api/auth/register` | Sınırsız hesap + sınırsız Hangfire işi. Doğrulama zorunluluğu hesabı kullanılamaz kılar ama satırlar ve e-posta kuyruğu birikir. |
| `POST /api/auth/resend-confirmation` | `forgot-password` ile aynı kota tüketimi. |
| `POST /api/auth/login` | Hesap kilidi **hesap başına**; farklı hesaplara tek parola denemesi (password spray) hiç sınırlanmıyor. |
| `/api/public/*` | 10 dk cache + salt-okunur + ≤100 sayfa boyutu ile hafifletilmiş (Faz 14'ün kabul edilen riski). |

**Neden ayrı karar gerekti:** MIMARI.md'de **K-13 "API versiyonlama ve rate limiting" bilinçli olarak
V1 dışıydı** (§3, satır 257) ve PLAN-V2'de üç ayrı yerde "kabul edilen risk" olarak yazılıydı. Belgenin
kendi kuralı: *"kapsam değişikliği gerekirse önce burası güncellenir, sonra kod."* Bu yüzden Adım 0'da
K-13 satırı **bölünür**: rate limiting kapsama girer, API versiyonlama V1 dışında kalır.

**Maliyet — düşük.** .NET 8'de `Microsoft.AspNetCore.RateLimiting` **çerçevenin içinde** (.NET 7'den beri
`Microsoft.AspNetCore.App` paylaşılan çerçevesinde); `Microsoft.NET.Sdk.Web` kullanıldığı için **NuGet
paketi gerekmez**, Y-50'nin lisans sorgusu da doğmaz.

| İş | Büyüklük |
|---|---|
| `Program.cs`'e `AddRateLimiter` + 2 politika (`auth-strict`, `auth-normal`) | ~25 satır |
| `app.UseRateLimiter()` — `RequestLoggingMiddleware`'den **sonra** (429'lar da erişim izine düşsün) | 1 satır |
| 4 uca `[EnableRateLimiting("...")]` | 4 satır |
| `OnRejected` → Y-25'e uygun ProblemDetails (çıplak 429 değil) | ~10 satır |
| Entegrasyon testi (N+1. istek 429) | 1 test |

**Alınan kapsam (A-53):** K-13'ün yalnızca rate limiting yarısı — K-03'ün Faz 11'de "tamamlanması" ve
K-16'nın Faz 10'da kısmen alınması precedent'i. Sınır **yalnızca anonim kimlik uçları**; global limiter
**konmaz** (SPA'nın meşru trafiğini boğar, `/api/public/*` zaten 10 dk cache ile hafifletilmiş).

> **Ön koşul:** IP'ye göre bölümleme, Faz 19.0'daki `UseForwardedHeaders` düzeltmesi olmadan ters proxy
> arkasında **tüm kullanıcıları tek kovaya koyar** ve ilk N istekten sonra herkesi kilitler. Bu yüzden
> 19.0 ve 19.5 aynı fazda, bu sırayla.

---

## Faz 19 — Öğrenci self-servisinin tamamlanması (K-25, A-48) — ✅ tamamlandı

### 19.0 Ters proxy IP düzeltmesi *(bu fazın ilk işi — Faz 18'i doğrular)*

`Program.cs`'e `UseForwardedHeaders` eklenir (`ForwardedHeaders.XForwardedFor | XForwardedProto`),
`KnownProxies`/`KnownNetworks` konfigürasyondan okunur. **Boş bırakılırsa devre dışı kalır** — güvenli
varsayılan: bilinmeyen proxy'ye güvenmek, istemcinin `X-Forwarded-For` başlığı uydurmasına izin verir
(IP sahteciliği). Geliştirmede Kestrel doğrudan dinlediği için davranış değişmez.

### 19.1 Uçlar

| Metot | Rota | İzin | Not |
|---|---|---|---|
| GET | `/api/membership-applications/mine` | — (authenticated) | `club-applications/mine` ile **birebir simetrik**; kulüp adı + durum + tarih + karar notu |
| DELETE | `/api/membership-applications/{id}` | — (authenticated) | **Yalnızca kendi** ve **yalnızca `Pending`** başvurusunu geri çeker. Y-16: soft delete |
| DELETE | `/api/clubs/{clubId}/membership` | — (authenticated) | Öğrenci kendi üyeliğinden ayrılır. Y-16: soft delete |
| PUT | `/api/me` | — (authenticated) | Öğrenci profili: bölüm + kayıt yılı |

**A-48 (yeni karar) — self-servis simetrisi:** her başvuru/üyelik akışının öğrenci tarafında karşılığı
olur: *başvur → durumu gör → geri çek*. `ClubApplication` bu deseni Faz 17'de kurdu; `MembershipApplication`
buna hizalanır.

### 19.2 İş kuralları — atlanamaz olanlar

| Kural | Gerekçe |
|---|---|
| **Son başkan kulüpten ayrılamaz** → `Conflict` | `ClubMemberManager.RemoveMemberAsync`'in mevcut muhafızının aynısı (`CannotChangeOwnPresidentRole`). Aksi hâlde kulüp başkansız kalır ve `EnsureClubWriteAccessAsync` kimseyi geçirmez. |
| **Öğrenci numarası `PUT /api/me` ile değiştirilemez** | Öğrenci no kimliğin parçası; değiştirilebilir olması `(ClubId, StudentId, TermId)` unique index'ini ve rapor izlenebilirliğini bozar. Değişmesi gerekiyorsa Admin işi. |
| **`PUT /api/me` yalnızca `Student` profilini yazar** | Y-22: `userId` istemciden değil `ICurrentUser`'dan. E-posta değişimi **kapsam dışı** — doğrulama akışını yeniden tetiklemek gerekir (K-03'ün yüzeyini büyütür). |
| Geri çekilen başvuru **yeniden başvurmayı engellemez** | Filtreli unique index `Status = Pending AND IsDeleted = 0` — soft delete edilen satır index'ten düşer, öğrenci tekrar başvurabilir. |

### 19.3 `MeResponseDto` genişler

Bugün yalnızca `Email` + `Roles` + `Permissions` taşıyor — **kayıt sırasında girilen öğrenci numarası,
bölüm ve kayıt yılı hiçbir yerde görünmüyor.** DTO'ya `StudentNumber`, `DepartmentId`, `DepartmentName`,
`FacultyName`, `EnrollmentYear` eklenir (öğrenci profili yoksa `null` — danışman/admin hesapları).

### 19.4 Frontend

`/profile` → "Öğrenci Bilgilerim" bölümü (görüntüle + bölüm/kayıt yılı düzenle) ·
`/my-club-applications` → **iki sekme**: "Topluluk Kurma" (mevcut) + "Üyelik Başvurularım" (yeni), geri
çekme butonuyla · `/my-clubs` kartlarına "Ayrıl" butonu + `ConfirmDialog`.

### 19.5 Rate limiting (K-13 kısmi, A-53, O-9)

**19.0'dan sonra** gelir — IP bölümlemesi doğru IP'ye ihtiyaç duyar.

| Politika | Uçlar | Sınır | Gerekçe |
|---|---|---|---|
| `auth-strict` | `forgot-password`, `resend-confirmation`, `register` | IP başına **5 istek / 15 dk** | E-posta üreten uçlar — SMTP kotasını koruyan asıl sınır |
| `auth-login` | `login` | IP başına **10 istek / 5 dk** | Identity'nin hesap kilidi hesap başına; bu, çok hesaba yayılan denemeyi (password spray) sınırlar |

`PartitionedRateLimiter` + `FixedWindowLimiter`, bölümleme anahtarı `HttpContext.Connection.RemoteIpAddress`.
Kimliği doğrulanmış kullanıcı **hiç sınırlanmaz** — bu uçlar zaten anonim.

**Kurallar:**

- Global limiter **konmaz**; yalnızca `[EnableRateLimiting("...")]` taşıyan action'lar sınırlanır.
- `OnRejected` → **ProblemDetails** (Y-25: çıplak 429 değil, Türkçe nötr mesaj + `Retry-After`).
- `app.UseRateLimiter()`, `RequestLoggingMiddleware`'den **sonra** kaydedilir — 429'lar da erişim izine
  düşsün (O-2 zaten `StatusCode >= 400` diyor; sıra ters olursa reddedilen istekler hiç loglanmaz,
  Faz 18'de `UseAuthorization` ile birebir aynı tuzak).
- Sınır değerleri `appsettings.json`'dan okunur (Y-20: secret değil, ayar) — testte düşürülebilsin diye.

**Y-63 (yeni kural):** e-posta üreten anonim uçlar oran sınırı olmadan yayına alınamaz. *(Gerekçe: tek
uçtan SMTP kotasının tüketilmesi sistemin **tüm** bildirim altyapısını durdurur — kayıt doğrulama dahil.)*

### Çıkış koşulu
Öğrenci başvurur → "Üyelik Başvurularım"da `Bekliyor` görür → geri çeker → listeden düşer → **aynı kulübe
yeniden başvurabilir** · üye olduğu kulüpten ayrılır → "Kulüplerim"den düşer · son başkan ayrılmaya
çalışır → `Conflict` · profilinden bölümünü değiştirir, öğrenci numarası alanı **salt okunur** ·
`forgot-password`'e arka arkaya 6. istek **429 + ProblemDetails** alıyor ve bu 429 erişim izinde görünüyor.

**Doğrulandı — hepsi ✅.** `StudentSelfServiceTests` (4 test) + `RateLimitTests` (2 test) dahil
**283/283 backend testi yeşil** (277 önceki + 6 yeni). Playwright ile canlı: başvuru → `Bekliyor`
görünüyor → geri çekiliyor → listeden düşüyor → **yeniden başvurulabiliyor** (filtreli unique index
soft delete ile doğru davranıyor) · son başkan "Ayrıl" deyince Türkçe `Conflict` mesajı çıkıyor ·
profilde öğrenci numarası salt okunur, bölüm/kayıt yılı düzenlenebilir.

> **Uygulamada bulunan iki hata (ikisi de mevcut kodda):**
>
> 1. **`WriteAsJsonAsync` `Response.ContentType`'ı eziyor.** 429 gövdesi `application/problem+json`
>    yerine `application/json` gidiyordu. Aynı hata **`GlobalExceptionMiddleware`'de de vardı** —
>    yani bugüne kadar *tüm 500 hataları* yanlış content-type ile dönüyordu (Y-25'in gövde formatı
>    doğruydu ama tipi değildi). İkisi de `contentType:` parametresiyle düzeltildi.
> 2. **`OnRejected` async lambda'sı Y-27 mimari testini kırdı.** Üst düzey deyimlerdeki async
>    lambda'nın state machine'i `Program/<>c` altına nested ediliyor ve `AsyncHygieneTests`'in
>    `[CompilerGenerated]` filtresinden kaçıyor. Testi gevşetmek yerine handler
>    `RateLimitRejectionHandler` sınıfına çıkarıldı — kod da böylesi daha temiz.
>
> **Test altyapısında kapatılan tuzak:** TestServer'da `Connection.RemoteIpAddress` **null**'dur;
> oran sınırı bütün testleri tek `"unknown"` kovasında toplar ve arka arkaya login yapan mevcut
> testler (`ClubApplicationFlowTests`, `MembershipVerticalSliceTests`…) 429 almaya başlardı.
> `CustomWebApplicationFactory` sınırı etkisiz kılar; `RateLimitTests` `WithWebHostBuilder` ile
> kendi düşük sınırını verir.

---

## Faz 20 — Etkinlik iptali (K-22, A-49, Y-61) — ✅ tamamlandı

### 20.1 Durum makinesi genişler

`EventStatus`'e **`Cancelled = 4`** eklenir. A-25'in akışı:

```
Draft → PendingApproval → Published → (Cancelled)
  ↓                          ↓
Rejected                 Cancelled
```

**A-49 (yeni karar):** yayınlanmış etkinlik **silinmez, iptal edilir.** Silme (Y-16 soft delete) yalnızca
`Draft` için kalır — taslak henüz kimseye görünmediği için iz bırakmadan kaybolabilir; yayınlanmış
etkinliğin kaydı ise **katılımcılar ona güvendiği için** kalmalı ve iptal edildiği görünmelidir.

| Metot | Rota | İzin | Kural |
|---|---|---|---|
| PUT | `/api/events/{id}/cancellation` | `events.write` + kapsam | Yalnızca `Published` iken; `CancellationReason` zorunlu |

### 20.2 Y-61 (yeni kural)

> **İptal edilen etkinlik yeni katılımcı kabul edemez ve anonim vitrinde yaklaşan etkinlik olarak listelenemez.**

Dokunulacak üç yer — üçü de bugün yalnızca `Published` kontrol ediyor, `Cancelled` eklenince otomatik
doğru davranmaları **garanti değil**, tek tek doğrulanır:

1. `EventParticipationManager.RegisterAsync` — `Status != Published` → `Conflict` *(zaten doğru)*
2. `PublicContentManager` — `e.Status == EventStatus.Published` filtresi *(zaten doğru)*
3. `EventManager.GetUpcomingAsync` — aynı filtre *(doğrulanacak)*

**Mevcut kayıtlar silinmez.** Katılımcı listesi durur; öğrenci "Etkinliklerim"de etkinliği `İptal Edildi`
rozetiyle görür. Bu bilinçli: kaydın kaybolması öğrenciye "ben kaydolmamış mıydım?" dedirtir.

### 20.3 Bildirim (O-7)

`EventCancellationNotificationJob` — `MembershipDecisionNotificationJob` kalıbı, **tek farkla: fan-out.**
İş yalnızca `eventId` alır (Y-47), katılımcıları kendi scope'unda çözer, her birine e-posta atar.

> **Ölçek uyarısı — SMTP kotası.** Kontenjanı 300 olan bir etkinliğin iptali 300 e-posta demek; Gmail'in
> 500/gün sınırında **tek iptal günlük kotanın yarısını** yer. Bu Faz 20'nin bilinen sınırıdır: iş
> `IEmailSender`'ı sırayla çağırır, `SmtpException` yutulmaz (Y-47 gereği Hangfire yeniden dener).
> Kota aşılırsa iş `Failed` olur ve panelde görünür — sessizce kaybolmaz.

`DecideAsync` gibi **elle transaction** (Y-41/Y-46): önce commit, sonra kuyruğa ekleme.

### 20.4 Frontend

`EventDetailPage`'e "Etkinliği İptal Et" butonu (yalnızca `Published` + kapsam sahibi) + gerekçe
diyaloğu · `StatusChip`'e `Cancelled` → gri/kırmızı rozet · `MyEventsPage` ve `EventsPage` kartlarında
iptal rozeti ve gerekçe.

### Çıkış koşulu — hepsi ✅
Yayındaki bir etkinlik iptal edilir → durumu `İptal Edildi` olur, **kaydı silinmez** · anonim
`/etkinlikler` sayfasında **görünmez** · yeni kayıt denemesi `Conflict` · kayıtlı öğrencilere e-posta
gider (Hangfire `Succeeded`) · `Draft` etkinlik hâlâ silinebiliyor (regresyon yok).

**Doğrulandı.** `EventCancellationTests` (3 test) dahil **286/286 backend testi yeşil**. Playwright ile
canlı: etkinlik iptalden önce anonim vitrinde görünüyor → danışman gerekçeyle iptal ediyor → vitrinden
düşüyor (`CacheRemoveAspect` çalışıyor) → öğrencinin "Etkinliklerim"inde `Cancelled` durumuyla **duruyor**
→ katılımcı listesi bozulmamış.

> **Y-61'in "tek tek doğrulanır" maddesi uygulandı:** `EventStatus` karşılaştırması yapan **13 nokta**
> tarandı (`grep -rn "EventStatus\."`). Hepsi `== Published` / `!= Draft` gibi **pozitif** karşılaştırma
> kullandığı için `Cancelled` eklenmesi otomatik doğru davrandı; hiçbirinde "Draft değilse yayındadır"
> gibi bir çıkarım yoktu. `EventParticipationManager.GetMineAsync`'in **durum filtresi taşımaması** ise
> A-49'un istediği davranış (öğrenci iptal edilen kaydını görmeye devam eder) — kasıtlı olarak
> değiştirilmedi.

---

## Faz 21 — Sunucu taraflı arama ve gerçek sayfalama (A-50, Y-62)

Bir hata düzeltmesi fazı. Yeni özellik yok, **var olan yalanı** kaldırıyor.

### 21.1 Sorunun tam tarifi

| Katman | Bugün | Sonuç |
|---|---|---|
| Arayüz | 12 çağrı `pageSize: 200` | — |
| Business | `ClampPageSize` → `Math.Min(200, 100)` = **100** | İstenen 200, dönen 100 — **sessizce** |
| Arayüz | `filteredClubs` = gelen 100 kaydı `.filter()` | 101. kayıt **aranamaz, görülemez** |

Arayüz `totalCount`'u alıyor ama **kullanmıyor**. Yani sistem "200 kulüp var" bilgisini elinde tutup
100 tanesini gösteriyor ve kullanıcıya hiçbir şey söylemiyor.

### 21.2 Çözüm

**A-50 (yeni karar) — arama sözleşmesi:** liste uçları `search` sorgu parametresi alır; filtreleme
**SQL'de** yapılır (`LIKE`, büyük/küçük harf duyarsız), sayfalama sunucu taraflıdır.

> **K-09 ile ilişki:** MIMARI.md K-09'un kapısı zaten *"Arama tek DAL metodunda"* diyor. Bu, o kapının
> kullanılmasıdır — Elasticsearch/full-text index **girmiyor**, K-09 V1 dışı kalmaya devam ediyor.

| Uç | Eklenen |
|---|---|
| `GET /api/clubs` | `search` (ad), `isActive` filtresi |
| `GET /api/public/clubs` | `search` |
| `GET /api/events/upcoming`, `/api/public/events` | `search` (başlık) |
| `GET /api/announcements` | `search` (başlık) |

Arayüz tarafı: `pageSize: 200` çağrılarının tamamı gerçek sayfalamaya çevrilir, arama kutuları
**debounce** ile (300 ms) sunucuya gider, kart galerileri `usePagedQuery` kullanır.

### 21.3 Y-62 (yeni kural)

> **Liste ucunun tamamını çekip istemcide filtrelemek yasaktır.** Arama ve filtre sunucuda, sayfalama
> `PagedResult` sözleşmesiyle. *(Y-11'in arayüz tarafındaki karşılığı — Y-11 sunucunun tüm tabloyu
> dönmesini yasaklıyordu; Y-62 arayüzün "hepsini iste, ben ayıklarım" kaçamağını kapatıyor.)*

### Çıkış koşulu
101 kulüplü bir veri setinde 101. kulüp **aranarak bulunabiliyor** · `git grep "pageSize: 200"` → **0
sonuç** · arama kutusuna yazılınca ağ sekmesinde `search=` parametreli **tek** istek görünüyor (debounce
çalışıyor) · sayfalama kontrolleri gerçek `totalCount`'u gösteriyor.

---

## Faz 22 — Otomatik dönem devri (K-30, A-51)

Planın en riskli fazı: **tek bir işlem binlerce satır yazar.**

### 22.1 A-51 — otomatik devir (O-6)

`SetCurrentAsync` yeni dönemi güncel yaparken, **önceki güncel dönemin** üyeliklerini yeni döneme
kopyalar. Kopyalama kuralları:

| Kural | Gerekçe |
|---|---|
| `ClubRole` **korunur** | Başkan başkan kalır — fazın varlık sebebi |
| `IsDeleted = true` üyelikler **atlanır** | Ayrılmış öğrenci geri gelmemeli |
| **Pasif kulüplerin** üyelikleri atlanır | Kapalı kulüp yeni dönemde üye kazanmamalı |
| Hedef dönemde **zaten varsa** atlanır | Idempotentlik + `(ClubId, StudentId, TermId)` unique index ihlali önlenir |
| `JoinedAtUtc` = devir anı | Yeni dönemdeki üyeliğin başlangıcı |

Tek transaction (`SetCurrentAsync` zaten elle yönetiyor — mevcut iki aşamalı `IsCurrent` sırası korunur,
bkz. `AcademicTermManager.cs:88` yorumu). Sonuç mesajı devredilen üyelik sayısını taşır.

### 22.2 Bilinen risk — audit patlaması

`AuditSaveChangesInterceptor` **entity başına bir satır** yazar. 5.000 üyeliğin devri → tek transaction'da
**5.000 `AuditLog` satırı**. Bu Y-44'ün doğal sonucu ve kural gereği baskılanamaz *(audit yazımını iş
kodunda atlamak yasak)*.

Karşılık: devir **idempotent** olduğu için tekrar çalıştırmak yeni satır üretmez, ve `SetCurrentAsync`
yılda 2-3 kez çalışan bir işlemdir. Ölçek sorun olursa çözüm Hangfire'a taşımaktır (bu fazın kapsamı
dışında) — **ama o zaman `ICurrentUser` sistem kullanıcısına düşer ve audit'te "kim" bilgisi kaybolur**,
o yüzden senkron tutuluyor.

### 22.3 `GetMineAsync` dönem tutarsızlığı düzeltilir

"Kulüplerim" bugün **tüm dönemlerin** üyeliklerini gösteriyor (`ClubMemberManager.cs:41`), ama
`EnsureClubWriteAccessAsync` yalnızca **güncel dönemin** üyeliğine bakıyor. Arayüz "başkansın" derken
backend "değilsin" diyor. Düzeltme: `GetMineAsync` güncel döneme filtrelenir; DTO'ya `AcademicTermName`
eklenir ki hangi döneme ait olduğu görünsün.

### Çıkış koşulu
İki kulüpte üyeliği (biri `President`) olan bir öğrenci · admin yeni dönemi güncel yapar · öğrencinin
üyelikleri **rolleriyle** yeni dönemde · **`President` rolüyle etkinlik oluşturabiliyor** (yetki
kaybı yok) · devir **ikinci kez** çalıştırılınca yeni satır oluşmuyor (idempotent) · pasif kulübün
üyelikleri devredilmemiş · ayrılmış (soft delete) üyelik devredilmemiş.

---

## Faz 23 — Arayüz cilası (O-8, A-52)

Backend'e **hiç dokunulmaz** — `git diff --stat src/ tests/` boş kalmalı (Faz 8/16 disiplini).
PLAN-V3 §16.3'te ertelenen maddeler + Faz 17/18'de büyüyen menünün yarattığı yeni sorun.

### 23.1 Sidebar yeniden düzeni *(en görünür sorun)*

Bugün **14 menü öğesi** iki grupta ("Genel" 10 + "Yönetim" 3 + Hangfire). 900px yükseklikte menü
sığmıyor — Faz 18 doğrulama ekran görüntüsünde Panel/Kulüpler/Kulüplerim **kaydırma dışında kalmıştı.**
Üstelik "Genel" grubu üç ayrı kavramı karıştırıyor: keşif (Kulüpler, Etkinlikler, Duyurular), kişisel
(Kulüplerim, Etkinliklerim, Topluluk Başvurularım), inceleme (Başvuru İncele, Topluluk Kurma Başvuruları).

Yeni gruplama:

| Grup | Öğeler |
|---|---|
| — | Panel |
| **Keşfet** | Kulüpler · Etkinlikler · Duyurular |
| **Benim** | Kulüplerim · Etkinliklerim · Başvurularım |
| **İnceleme** | Başvuru İncele · Topluluk Kurma Başvuruları · Raporlarım |
| **Yönetim** | Yetki Matrisi · Referans Verisi · Denetim İzi |

"Topluluk Başvurularım" + "Üyelik Başvurularım" tek "Başvurularım" sayfasında sekmelenir (Faz 19.4) →
menü **14'ten 13'e** iner ve gruplar 3-4 öğeye düşer.

### 23.2 Kalan maddeler

| Konu | Bugün | Sonra |
|---|---|---|
| Yükleme sıçraması | 14 sayfadan **1**'inde `Skeleton` var | Kart galerileri ve detay sayfalarında iskelet |
| Sekme başlığı | Her sayfa aynı başlıkla açılıyor | `useDocumentTitle` hook'u |
| Mobil tablolar | `columnVisibilityModel` yok, tablolar taşıyor | Dar ekranda ikincil kolonlar gizlenir |
| Kalıcı hatalar | Yalnızca snackbar (kaybolur) | Sayfa içi `Alert` (ör. detay 404) |
| Boş durumlar | Bazı sayfalarda ham "No rows" | Hepsi `EmptyState` + aksiyon linki |

**A-52 (yeni karar):** her sayfa `useDocumentTitle` ile kendi sekme başlığını yazar; her veri çeken
görünüm yükleme durumunda `Skeleton` gösterir (boş ekran + zıplama yerine).

### Çıkış koşulu
`git diff --stat src/ tests/` **boş** · 1400×900'de admin menüsü **kaydırmasız** sığıyor · her sayfanın
sekme başlığı farklı · 375px genişlikte hiçbir tablo yatay taşmıyor · `npm run build` + `npm run lint` temiz.

---

## Adım 0 — Belgeler (koddan ÖNCE)

**§3'e yeni V1+ maddesi:** K-30 Dönem devri.

**§6'ya yeni kararlar:** A-48 self-servis simetrisi · A-49 etkinlik iptali (silme değil) ·
A-50 arama sözleşmesi (sunucu taraflı) · A-51 otomatik dönem devri · A-52 arayüz cilası sözleşmesi ·
**A-53 anonim kimlik uçlarında oran sınırı** (K-13'ün kısmi alınması).

**§2'ye yeni yasaklar:** Y-61 iptal edilen etkinliğe kayıt/vitrinde listeleme ·
Y-62 liste ucunu tamamen çekip istemcide filtreleme · **Y-63 oran sınırsız e-posta üreten anonim uç**.

**§4 domain tablosuna:** `Event` satırı `Cancelled` durumu ve `CancellationReason` ile güncellenir;
`ClubMembership` satırına devir notu.

**§3'te K-13 satırı bölünür (O-9):** "API versiyonlama ve rate limiting" → **rate limiting kapsama girer**
(A-53, yalnızca anonim kimlik uçları); **API versiyonlama V1 dışında kalır.** V1 dışı tablosundaki K-13
satırı bu ayrımı açıkça yazar. PLAN-V2'nin üç "kabul edilen risk" notu (kayıt/şifre uçları, anonim vitrin)
artık yalnızca anonim vitrin için geçerlidir — kimlik uçları A-53 ile kapandı.

**§5'e Faz 19-23.** Sayaç: **47 karar / 60 kural / 18 faz** → **53 karar / 63 kural / 23 faz**
(K sayısı 29 → 30; V1 dışı madde sayısı 13'te kalır — K-13 tamamen çıkmıyor, yarısı duruyor).

**§3'ün "V1 dışında kalanlar" tablosunun geri kalanı değişmez** — K-02, K-04, K-07, K-09…K-12,
K-14…K-20 ertelenmiş kalır.

---

## Uygulama sırası ve çıkış koşulları

| # | Faz | Migration | Bitti sayılır |
|---|---|---|---|
| **0** | `docs/MIMARI.md` güncellemesi | — | Belge K-30, A-48…A-52, Y-61/Y-62 ve Faz 19-23'ü içeriyor |
| **19** | Öğrenci self-servisi + IP düzeltmesi + rate limiting | — (şema değişmez) | ✅ tamamlandı — başvuru geri çekilebiliyor ve yeniden başvurulabiliyor, kulüpten ayrılınabiliyor, profil düzenlenebiliyor; son başkan ayrılamıyor; `forgot-password` sınır aşımında 429 + ProblemDetails |
| **20** | Etkinlik iptali | `EventStatus.Cancelled` + `CancellationReason` | ✅ tamamlandı — yayındaki etkinlik iptal ediliyor, vitrinden düşüyor, kayıtlar duruyor, katılımcılara e-posta gidiyor |
| **21** | Arama ve sayfalama | — | `git grep "pageSize: 200"` boş; 101. kayıt bulunabiliyor |
| **22** | Otomatik dönem devri | — (şema değişmez) | Devirden sonra başkan yetkisini koruyor; ikinci çalıştırma idempotent |
| **23** | Arayüz cilası | — | `git diff --stat src/` boş; menü kaydırmasız sığıyor |

*Sessiz onay korunur: her PR'da en fazla bir migration; adlandırma `YYYYMMDD_AçıklayıcıAd`.*

> **Faz 21 öne alınabilir:** tek başına bir hata düzeltmesi, hiçbir faza bağlı değil.
> **Faz 23 en sona konuldu** çünkü 19 ve 20 yeni sayfa/rozet ekliyor — cilayı önce yapmak iki kez iş olurdu.

---

## Doğrulama

**Her fazda (V1/V2/V3 ile aynı):**
- `dotnet build` **0 uyarı** (Y-31) · `dotnet test` dört proje yeşil · `npm run build` + `npm run lint` temiz.
- `Architecture.Tests` değişmeden yeşil.
- Her yeni iş kuralı için **bir kabul + bir ret** testi (A-20).
- Tarayıcıda canlı yürüyüş (scratchpad Playwright kalıbı).

**Faza özel kritik testler:**

| Test | Neyi kanıtlar |
|---|---|
| `MembershipWithdrawalTests` | Başka öğrencinin başvurusu geri çekilemiyor (403); `Approved` başvuru geri çekilemiyor (409); geri çekildikten sonra **yeniden başvurulabiliyor** (filtreli index'in soft delete ile doğru çalışması) |
| `LastPresidentCannotLeaveTests` | Son başkan ayrılmaya çalışır → `Conflict`; ikinci başkan varken ayrılabiliyor |
| `ProfileUpdateTests` | `PUT /api/me` öğrenci numarasını **değiştirmiyor**; başka kullanıcının profili yazılamıyor (Y-22) |
| `EventCancellationTests` | **Faz 20'nin kabul testi.** `Published` → iptal → `Cancelled`; kayıt denemesi `Conflict`; `/api/public/events`'te **yok**; katılımcı kaydı **silinmemiş**; `Draft` iptal edilemiyor (409) |
| `EventCancellationNotificationTests` | Kayıtlı **her** katılımcıya e-posta gidiyor (fan-out), iş idempotent |
| `ClubSearchPagingTests` | **Faz 21'in kabul testi.** 120 kulüp seed edilir; `search` ile 101+'inci kayıt bulunuyor; `pageSize=200` istense bile `PageSize=100` dönüyor ve `TotalCount=120` doğru |
| `TermRolloverTests` | **Faz 22'nin kabul testi.** Devirden sonra `President` rolü korunuyor ve etkinlik oluşturabiliyor; pasif kulüp ve soft-delete üyelik devredilmiyor; **ikinci çalıştırma sıfır yeni satır** üretiyor |
| `RateLimitTests` | `forgot-password`'e N+1. istek **429** ve gövde ProblemDetails (Y-25, çıplak 429 değil); sınır altındaki istekler geçiyor; kimlik doğrulamalı uç (`/api/clubs`) **hiç sınırlanmıyor** — global limiter yok |

---

## Açık riskler

| Risk | Karşılık |
|---|---|
| **Ters proxy arkasında IP yanlış** (Faz 18'i değersizleştirir, rate limiting'i işlevsizleştirir) | Faz 19.0'ın ilk işi `UseForwardedHeaders`; `KnownProxies` boşken **devre dışı** — bilinmeyen proxy'ye güvenmek IP sahteciliği demektir |
| **Rate limiter meşru kullanıcıyı kilitler** (kampüs NAT'ı arkasında yüzlerce öğrenci tek IP'den çıkar) | Sınır yalnızca **anonim kimlik uçlarında**; giriş yapmış kullanıcı hiç sınırlanmıyor. Değerler `appsettings.json`'dan ayarlanabilir. Yine de en gerçekçi risk bu: yurt/kütüphane NAT'ında `login` sınırı (10/5dk) dar gelebilir — ilk üretim gününde erişim izinden 429 sayısı izlenmeli |
| `UseRateLimiter` yanlış sıraya konursa 429'lar erişim izine düşmez | Faz 18'in `UseAuthorization` tuzağının aynısı; `RequestLoggingMiddleware`'den **sonra** kaydedilir ve test bunu doğrular |
| Dönem devri tek transaction'da binlerce audit satırı üretir | Idempotent olduğu için tekrar maliyeti yok; yılda 2-3 çalışır. Hangfire'a taşımak audit'te "kim" bilgisini kaybettirir (§22.2) |
| Etkinlik iptali SMTP kotasını tüketir (300 kişilik etkinlik = 300 e-posta) | Bilinen sınır; iş `Failed` olup Hangfire panelinde görünür, sessizce kaybolmaz (§20.3) |
| `EventStatus.Cancelled` eklenince mevcut `Published` filtreleri sessizce yanlış davranabilir | Y-61 üç çağrı noktasını tek tek sayıyor; `EventCancellationTests` vitrin ve kayıt yollarını ayrı ayrı kanıtlıyor |
| Son başkanın ayrılması kulübü yönetilemez bırakır | `CannotChangeOwnPresidentRole` muhafızının aynısı yeniden kullanılır; ayrı test |
| Arama `LIKE '%...%'` index kullanmaz, büyük tabloda yavaşlar | Kabul edilen sınır: kulüp/etkinlik sayısı üniversite ölçeğinde (yüzler). K-09 (full-text) V1 dışı kalır |
| Faz 21 arayüzün 12 çağrısını birden değiştiriyor | Her sayfa ayrı commit; `git grep "pageSize: 200"` boşalana kadar bitmiş sayılmaz |
| Faz 23'ün yüzeyi geniş (14 sayfa) | Backend'e sıfır dokunuş; `git diff --stat src/` boş kalmalı |

### Kritik dosyalar

- `src/WebAPI/Program.cs` — `UseForwardedHeaders` (Faz 19.0) ve varsa `UseRateLimiter` (O-9) buraya girer
- `src/WebAPI/Middleware/RequestLoggingMiddleware.cs:70` — `RemoteIpAddress`, Faz 19.0'ın doğruladığı yer
- `src/Business/Concrete/MembershipApplicationManager.cs` — Faz 19'un genişleteceği servis
- `src/Business/Concrete/ClubMemberManager.cs:41` — dönem filtresi eksik `GetMineAsync` (Faz 22.3)
- `src/Business/Concrete/AcademicTermManager.cs:75` — `SetCurrentAsync`, Faz 22'nin kalbi
- `src/Business/Concrete/EventManager.cs:294` — `Draft` kısıtı, Faz 20'nin dokunacağı yer
- `src/Entities/Enums/EventStatus.cs` — `Cancelled` eklenir (A-49)
- `src/Business/DTOs/Auth/MeResponseDto.cs` — öğrenci alanları eklenir (Faz 19.3)
- `src/Business/BackgroundJobs/MembershipDecisionNotificationJob.cs` — Faz 20'nin fan-out işi için kalıp
- `arayuz/src/components/layout/SideNav.tsx` — 14 öğe, Faz 23.1'in hedefi
- `docs/MIMARI.md` — Adım 0'da güncellenir, sonrasında tek doğruluk kaynağı
