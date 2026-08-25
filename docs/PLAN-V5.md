# V5 — Yönetici kapsamı, kişi kimliği ve gerçek veri (Faz 24 → 29)

## Context

PLAN-V4'ün beş fazı da bitti ve pushlandı (son commit `cf86287`; 296 test yeşil). Sistem mimari
olarak sağlam, işlevsel olarak da geniş — ama **kullanıcının bildirdiği dokuz sorunun tamamı koddan
doğrulandı** ve altı tanesi daha kod okumasında çıktı. Ortak yanları var: sistem tek bir kulübün
danışmanı/başkanı için tasarlanmış, **yöneticinin ve gerçek kurumsal verinin yeri boş bırakılmış.**

### Bildirilen dokuz maddenin kanıtı

| # | Bildirilen | Koddaki kök neden |
|---|---|---|
| 1 | Fakülte/bölüm seed'i | `DomainSeedData.cs:16-27` → **tek fakülte, tek bölüm**. Gerçek liste 19 fakülte / 121 bölüm |
| 2 | Kulüp/etkinlik/duyuru seed'leri silinip yenilensin | **Öyle bir seed yok.** Tek kulüp `IdentitySeeder.cs:93` içinde danışman seed'ine gömülü; sıfır etkinlik, sıfır duyuru, sıfır üyelik. Dev veritabanındaki 100+ kulüp ve "Faz14/Faz20/FAZ21…" kayıtları **doğrulama betiklerimin artığı**, seed değil |
| 3 | Danışman değiştirilemiyor | `UpdateClubRequestDto` yalnızca `Name`+`Description`. Dahası: **`AcademicStaff` salt-okunur** — hiçbir uç oluşturmuyor, yani sisteme yeni danışman hiç eklenemiyor |
| 4 | Admin'e "danışmanı ya da başkanı olmanız gerekir" | 7 `Ensure*Access` metodundan yalnızca 3'ünde yönetici yolu var (`ClubMemberManager.cs:256`, `EventParticipationManager.cs:224`, `ReportScopeResolver.cs:31`) — o da `reports.read.all` iznini fiilen "admin mi" bayrağı olarak kullanarak |
| 5 | Admin kulüp etkinliklerini göremiyor | `EventManager.cs:431` `EnsureClubWriteAccessAsync` — yönetici dalı **yok**. Arayüz sekmeyi `events.read` ile açıyor, API 403 dönüyor |
| 6 | Yetki matrisi ikiye ayrılsın | Tek sayfa, iki sekme (`AuthorizationPage.tsx`) |
| 7 | Kullanıcı sayfası basit | `UserListItemDto` = Id/Email/Roles. **Şemada ad-soyad hiç yok** (`ApplicationUser` boş `IdentityUser<int>`, `Student`'ta da yok). `SetLockoutAsync` ve `CreateUserAsync` backend'de **var ama arayüzü yok**; silme ucu hiç yok |
| 8 | Sayfa yenilenince oturum kapanıyor | `AuthContext.tsx:17` yalnızca bellekteki store'u okuyor; **açılışta sessiz refresh denemesi yok**. `ProtectedRoute.tsx:17` token null görüp `/login`'e atıyor — refresh çerezi hâlâ geçerliyken |
| 9 | Hangfire paneli stilsiz + 401 | `Program.cs:139` `access_token`'ı **yalnızca query string'den** okuyor. Panelin CSS/JS varlıkları ve iç gezinmeleri o parametreyi taşımadığı için 401 alıyor → stil yüklenmiyor |

### Kod okumasında çıkan altı ek bulgu

| # | Bulgu | Sonuç |
|---|---|---|
| 10 | **Admin'in oluşturduğu kullanıcı yarım kalıyor** — `RoleAdminManager.cs:186` yalnızca Identity kaydı + rol yazıyor | `Member` rolü verilen kullanıcının `Student` kaydı yok → kulübe başvuramaz, etkinliğe kaydolamaz (`NotAStudent`). `Advisor` rolü verilenin `AcademicStaff` kaydı yok → kulübe danışman atanamaz |
| 11 | `POST /api/users` ve `PUT /api/users/{id}/lockout` **arayüzsüz** | Backend'de duran, hiçbir ekrandan ulaşılamayan iki uç |
| 12 | Admin kulüp logosu yükleyemiyor | `FileManager.cs:114` yalnızca danışmanı kabul ediyor; arayüz düğmeyi `files.upload` ile gösteriyor → **garanti 403** |
| 13 | Admin üye rolü değiştiremiyor | `ClubMemberManager.EnsureRoleManagementAccessAsync` — danışman veya başkan |
| 14 | Admin kulüp duyurusu yazamıyor | `AnnouncementManager.EnsureClubWriteAccessAsync` — aynı desen |
| 15 | `reports.read.all` fiilen "yönetici" anlamına gelmiş | Yetki matrisinden bir role rapor izni veren kişi, farkında olmadan **kulüp kapsamı** da vermiş olur |

**Hedef:** V1-V4'ün disiplinini (beş katman, Y-01…Y-64, tek paradigma) hiç bozmadan, altı bağımsız
fazda yöneticiyi sisteme geri koymak, kişilere isim vermek ve gerçek kurumsal veriyi yerleştirmek.

---

## Kararlar (onayına açık)

> Soru aracı bu oturumda kullanılamadığı için kararlar **önerilen** hâliyle alındı ve gerekçesi
> yazıldı. Değiştirmek istediğin madde olursa uygulamaya geçmeden söylemen yeterli.

### O-14 — Yönetici kapsamı: yeni `clubs.manage.all` izni

Yeni bir izin kodu eklenir, Admin rolüne verilir ve **7 `Ensure*Access` metodunun tamamında ilk
kontrol** olur.

| Seçenek | Neden seçilmedi |
|---|---|
| `reports.read.all`'ı kalan 4 metoda da yaymak | Bugünkü karışıklığın sebebi zaten bu. Rapor izni yönetim izni gibi davranmaya devam ederdi (bulgu 15) |
| Kodda `"Admin"` rol adına bakmak | K-17 yetki matrisinin amacı izinleri roller üzerinden yönetmek; rol adını koda gömmek matrisi anlamsızlaştırır |

Ayrıca mevcut 3 bypass `reports.read.all`'dan `clubs.manage.all`'a taşınır; `reports.read.all`
yalnızca rapor kapsamında kalır (`ReportScopeResolver`).

### O-15 — Ad soyad: `ApplicationUser`'a eklenir, anonim yüzeye çıkmaz

`FirstName`/`LastName` alanları `ApplicationUser`'a eklenir (tek migration). Kayıt formu, admin
kullanıcı oluşturma, kullanıcı listesi, üye listeleri ve danışman seçici ad soyad gösterir.

- **Y-58 korunur:** ad soyad `/api/public/*` uçlarından **asla** dönmez. Anonim vitrin kişisel veri
  döndüremez kuralı bu alanı da kapsar; DTO ailesi ayrı olduğu için sızma yapısal olarak kapalı.
- **Mevcut kayıtlar:** alanlar nullable başlar, boşsa arayüz e-postaya düşer. Zorunlu kılmak mevcut
  hesapları geçersiz duruma sokup bir kerelik "profil tamamlama" akışı gerektirirdi — V5'e alınmadı.
- Bu değişiklik bugünkü bir gariplği de düzeltir: `/academic-staff/selectable` **herhangi bir
  öğrenciye danışmanların e-postasını** gösteriyor, çünkü gösterecek başka bir alan yok
  (PLAN-V3 §17'de bilinçli taviz olarak kaydedilmişti). Ad soyad gelince e-posta o uçtan çıkar.

### O-16 — Kullanıcı silme: pasife alma + korumalı silme

| Eylem | Davranış |
|---|---|
| **Pasife alma** | Mevcut `PUT /users/{id}/lockout` — giriş yapamaz, verisi durur. Geri alınabilir |
| **Silme** | Kalıcı. Kullanıcının bağlı kaydı varsa (üyelik, etkinlik kaydı, başvuru, danışmanlık) **409** ile reddedilir ve hangi bağın engellediği söylenir |
| **Öz-koruma** | Kendini silemez ve kilitleyemez (`CannotLockOwnAccount` precedent'i) |

Koşulsuz silme seçilmedi: FK'lar `Restrict`, Y-16 soft delete kuralı var ve K-19 (KVKK silme akışı)
V1 dışı. Sessizce yetim kayıt bırakmak bu üçüyle de çelişir.

### O-17 — Seed: referans veri `HasData`, demo veri config kapılı

| Veri | Yer | Gerekçe |
|---|---|---|
| 19 fakülte / 121 bölüm | Migration `HasData` | Gerçek kurumsal referans verisi, üretime de gitmeli, belirleyici olmalı |
| Kulüp / etkinlik / duyuru / üyelik | `DemoDataSeeder`, yalnızca `Seed:Demo=true` iken | Demo veri üretim veritabanına **asla** sızmamalı; `HasData` ile gömülse silmek migration gerektirirdi |

Ek olarak yerel veritabanındaki doğrulama artıklarını temizleyen bir bakım komutu gerekiyor —
bugün dev DB'de 100+ sahte kulüp var (bulgu 2).

---

## Faz 24 — Oturum sürekliliği ve Hangfire paneli — ✅ tamamlandı

En küçük, günlük etkisi en yüksek faz. Şema değişikliği yok.

### 24.1 Sayfa yenilemede oturum (bildirilen #8)

Bugün akış şu: F5 → bellekteki token gider → `AuthContext` `accessToken: null` görür →
`ProtectedRoute` anında `/login`'e yönlendirir. **Refresh çerezi hâlâ geçerlidir ama kimse
kullanmaz** — axios interceptor'ı ancak bir 401 alınca devreye giriyor, oysa hiç istek atılmıyor.

Çözüm: `AuthProvider` açılışta **bir kez** `/auth/refresh` dener ve bu deneme sürerken üçüncü bir
durum yayınlar:

| Durum | Arayüz |
|---|---|
| `bootstrapping` | Tam sayfa yükleme göstergesi — **yönlendirme yok** |
| `authenticated` | Normal |
| `anonymous` | `/login` |

`ProtectedRoute` `bootstrapping` iken hiçbir karar vermez. Bu üçüncü durum olmadan yarış koşulu
kapanmaz: token gelmeden verilen "anonim" kararı geri alınamaz.

> **Not:** `tokenStore.ts` yorumu zaten *"sessiz refresh akışı onu yeniden üretir"* diyor — akış
> hiç yazılmamış. Yorum ile kod arasındaki fark bu fazda kapanıyor.

### 24.2 Hangfire paneli (bildirilen #9)

`Program.cs:139` token'ı yalnızca query string'den okuyor. İlk istek (`/hangfire?access_token=…`)
geçiyor, ama panelin yüklediği `/hangfire/css…`, `/hangfire/js…` ve iç gezinmeler o parametreyi
taşımıyor → hepsi 401 → **stilsiz sayfa** (ekran görüntüsündeki hâl).

Çözüm — bilinen köprü deseni: query token'la kimlik doğrulandığında **yalnızca `/hangfire` yoluna
kapsamlı**, `HttpOnly`, `Secure`, `SameSite=Strict` ve access token ile **aynı ömürlü** bir çerez
yazılır; `OnMessageReceived` token'ı önce query'den, yoksa bu çerezden okur.

| Kısıt | Gerekçe |
|---|---|
| `Path=/hangfire` | Çerez API uçlarına gitmez — K-01'in "access token çerezde taşınmaz" kararı ihlal edilmez |
| `HttpOnly` + `Secure` + `SameSite=Strict` | Y-48'in `__Host-Csrf` çerezindeki aynı sertlik |
| Access token ile aynı ömür | Panel erişimi oturumdan uzun yaşamaz |

**Y-65 (yeni kural):** Hangfire köprü çerezi `/hangfire` dışında bir yola yazılamaz ve API kimlik
doğrulaması bu çerezi asla kabul etmez — aksi hâlde CSRF yüzeyi (Y-48) sessizce geri açılır.

### 24.3 Uygulama sırasında çıkan engel: CSRF token'ı da kayboluyor

Planı yazarken gözden kaçan nokta: `performRefresh()` **bellekteki** `csrfToken`'ı şart koşuyor
(`client.ts`). F5 sonrası o da gittiği için fonksiyon daha ilk satırda `null` dönüp çıkıyordu —
yani "açılışta refresh dene" demek tek başına yetmiyor, denemenin **başlayabilmesi** için yeni bir
CSRF çifti gerekiyor.

Eklenen uç: `GET /api/auth/csrf` (anonim). `antiforgery.GetAndStoreTokens` çerez token'ını çağıranın
kendi tarayıcısına yazar ve ona karşılık gelen istek token'ını döner.

> **Y-48 zayıflamıyor:** başka origin'den çağıran saldırgan kurbanın çerezini okuyamaz (CORS), kendi
> aldığı çift ise kurbanın oturumunda işe yaramaz. Bu, Microsoft'un SPA antiforgery kalıbının aynısı.
> Test bunu ayrıca sınıyor: başlıksız refresh hâlâ reddediliyor.

Ek olarak `localStorage`'a **token olmayan** bir oturum ipucu (`auth.session-hint`) yazılır: hiç
giriş yapmamış ziyaretçi her sayfa açılışında boşuna `csrf` + `refresh` çifti göndermesin. K-01
ihlal edilmez — ipucunun taşıdığı tek bilgi bir bayrak.

### Çıkış koşulu
F5 sonrası panelde kalınıyor, giriş ekranı görünmüyor · refresh çerezi süresi dolmuşsa `/login`'e
düşülüyor · Hangfire paneli **stilli** açılıyor, "İşler / Sunucular" sayfaları arasında 401 almadan
gezilebiliyor · `/hangfire` çerezi `/api/*` isteklerine **gitmiyor** (ağ sekmesinde doğrulanır).

### Tamamlanma notu

**Testler:** 303/303 yeşil (176 Business + 10 Architecture + 117 Integration). Yeni
`SessionBootstrapTests` (3) ve `HangfireCookieScopeTests` (4).

| Canlı kontrol | Sonuç |
|---|---|
| `/panel`'de F5 | `/panel`'de kalındı, panel içeriği göründü |
| `/reference`'ta F5 (derin sayfa) | `/reference`'ta kalındı |
| Hangfire paneli stil | **Yüklendi** (2 CSS isteği, `.navbar` hesaplanmış stilli) |
| Hangfire iç sayfası | `/hangfire/jobs/enqueued` — URL'de `access_token` **yok** |
| Hangfire 401 sayısı | **0** |
| Köprü çerezi | `Path=/hangfire`, `HttpOnly`, `Secure`, `SameSite=Strict` |
| Çerez `/api/*`'a gidiyor mu | **Hayır** |

**Test altyapısı hakkında düzeltilen varsayım:** `WebApplicationFactory.CreateClient()` çerezleri
tarayıcı gibi saklıyor (`HandleCookies` varsayılan `true`). İlk yazdığım test çerezleri elle
taşımaya çalışıyordu ve antiforgery geçerli çerez varken yenisini yazmadığı için `Set-Cookie`
bulunamayıp patladı. Test gerçek akışa çevrildi — F5 zaten "çerezler durur, bellek sıfırlanır"
demek, fabrika istemcisi bunu doğrudan taklit ediyor.

---

## Faz 25 — Yönetici kapsamı (bildirilen #4, #5 + bulgu 12, 13, 14, 15) — ✅ tamamlandı

### 25.1 Sorunun tam tarifi

Sistem "kulüp işlerini kulübün danışmanı veya yetkilisi yapar" varsayımıyla yazılmış. Yönetici bu
tanımın dışında kaldığı için **kendi yönettiği sistemde misafir**:

| Uç | Bugün admin | Kontrol |
|---|---|---|
| `GET/POST /clubs/{id}/events` | **403** | `EventManager.cs:431` |
| `POST /clubs/{id}/announcements` | **403** | `AnnouncementManager.EnsureClubWriteAccessAsync` |
| `PUT /clubs/{id}/members/{m}/role` | **403** | `ClubMemberManager.EnsureRoleManagementAccessAsync` |
| `POST /clubs/{id}/logo` | **403** | `FileManager.cs:114` |
| `GET /clubs/{id}/members` | çalışıyor | `reports.read.all` bypass'ı |
| `GET /events/{id}/participants` | çalışıyor | aynı bypass |

Arayüz bu farkı bilmiyor: sekmeleri ve düğmeleri `events.read` / `files.upload` gibi izinlere göre
gösteriyor, kullanıcı tıklıyor, 403 yiyor. **İzin var ama kapsam yok** — Y-23'ün ters yönde ısırması.

### 25.2 Çözüm — O-14

`clubs.manage.all` izni eklenir (`IdentitySeedData`, sıradaki boş claim Id'sinden), Admin rolüne
verilir ve **7 metodun tamamında ilk kontrol** olur:

```
EventManager.EnsureClubWriteAccessAsync
AnnouncementManager.EnsureClubWriteAccessAsync
AnnouncementManager.EnsureAnnouncementWriteAccessAsync
ClubMemberManager.EnsureMemberViewAccessAsync
ClubMemberManager.EnsureRoleManagementAccessAsync
EventParticipationManager.EnsureViewAccessAsync
FileManager.EnsureClubAdvisorAsync
```

Mevcut `reports.read.all` bypass'ları bu izne taşınır. `ReportScopeResolver.cs:31`'deki
`reports.read.all` **kalır** — orası gerçekten rapor kapsamı.

**Y-66 (yeni kural):** Kulüp kapsamı kontrol eden her metot, kontrolün en başında
`clubs.manage.all` iznine bakmak zorundadır. Yeni bir kapsam metodu eklenip bu satır unutulursa
yönetici sessizce kilitlenir; mimari test bunu yakalar (§25.3).

### 25.3 Mimari test — unutmayı yapısal olarak kapatmak

`Architecture.Tests`'e yeni test: `Business.Concrete` içindeki adı `Ensure*Access*` ile başlayan
her private metodun IL gövdesinde `clubs.manage.all` sabiti geçmelidir. Bugünkü 7 metodun 7'sini de
kapsar; 8.'si eklendiğinde test kırmızıya döner.

> Bu, `AsyncHygieneTests`'in (Y-27) aynı sınıfından bir koruma: kuralı yorum olarak yazmak yerine
> ihlali derleme sonrası tespit etmek.

### 25.4 Arayüz

Kapsam açıldığı için arayüzde gizlenen bir şey yok; aksine bugün gösterilip 403 dönen düğmeler
artık çalışıyor. Tek ekleme: kulüp detayında yönetici için "Bu topluluğu yönetici yetkisiyle
görüntülüyorsunuz" bilgi şeridi — hangi yetkiyle işlem yaptığı belirsiz kalmasın.

### Çıkış koşulu
Admin bir kulübün **etkinliklerini görüyor ve oluşturabiliyor** · duyuru yazabiliyor · üye rolü
değiştirebiliyor · logo yükleyebiliyor · `clubs.manage.all` taşımayan bir kullanıcı bunların
hiçbirini yapamıyor (her biri için bir kabul + bir ret testi, A-20) · mimari test 8. kapsam
metodunu unutmayı yakalıyor.

### Tamamlanma notu

**Testler:** 309/309 yeşil (176 Business + 11 Architecture + 122 Integration). Yeni
`AdminClubScopeTests` (5) ve `ScopeGuardTests` (1).

**Mimari test gerçekten çalışıyor mu — negatif kontrol yapıldı.** `FileManager`'daki muhafız
geçici olarak kaldırıldı; test tam olarak o metodu işaret etti:

```
Kapsam metodu 'clubs.manage.all' iznini kontrol etmiyor — yönetici bu uçta kilitli kalır (Y-66):
FileManager.EnsureClubAdvisorAsync
```

Muhafız geri konunca yeşile döndü. Test "yeşil olduğu için doğru" değil, **kırmızıya dönebildiği
için** anlamlı.

| Canlı kontrol (admin, danışmanı olmadığı kulüpte) | Sonuç |
|---|---|
| Yönetici bilgi şeridi | Görünüyor |
| Etkinlikler sekmesi | Yüklendi, **0** adet 403 (önceden tamamı 403'tü) |
| Etkinlik oluşturma | Başarılı, 0 adet 403 |
| Üyeler sekmesi | 0 adet 403 |
| Duyurular sekmesi | 0 adet 403 |

**Uygulama sırasında çıkan iki engel:**

1. **State machine tuzağı.** İlk yazdığım mimari test 7 metodun 7'sini de "ihlal" olarak işaretledi
   — oysa muhafız hepsine eklenmişti. Sebep: kapsam metotlarının hepsi `async`, derleyici gövdeyi
   ayrı bir state machine tipine taşıyor ve geriye yalnızca onu kuran bir saplama bırakıyor.
   `AsyncHygieneTests`'in ters yönde tarif ettiği aynı tuzak. Test `AsyncStateMachineAttribute`
   üzerinden `MoveNext` gövdesini de tarayacak şekilde düzeltildi.
2. **Yeni izin `HasData` seed'i, migration ister.** Kabul testleri önce 403 dönmeye devam etti:
   `Claim(37, …)` modele eklenmişti ama migration üretilmemişti, dolayısıyla test veritabanında
   satır yoktu. `20260825_Faz25_YoneticiKapsami` migration'ı eklendi.

**Birim testlerinde beklenen kırılma:** kapsam metotları artık ilk satırda `currentUser.Permissions`
okuduğu için, bu mock'u yapılandırmayan `EventManagerTests` ve `FileManagerTests` düştü (Moq
yapılandırılmamış üye için `null` döner). Varsayılan boş koleksiyon eklendi.
`ClubMemberManagerTests`'teki "reports.read.all taşıyan yönetici" testi de adıyla birlikte
`clubs.manage.all`'a taşındı — testin *niyeti* aynı, izin kodu değişti.

---

## Faz 26 — Kişi kimliği ve kullanıcı yönetimi (bildirilen #7 + bulgu 10, 11) — ✅ tamamlandı

### 26.1 Ad soyad (O-15)

`ApplicationUser`'a `FirstName` / `LastName` (nullable, max 100). Tek migration.

| Nerede toplanır | Nerede görünür | Nerede **görünmez** |
|---|---|---|
| Kayıt formu (`/register`) | Kullanıcı listesi | `/api/public/*` (Y-58) |
| Admin kullanıcı oluşturma | Üye listeleri (öğrenci no ile birlikte) | — |
| Profil sayfası (`/profile`) | Danışman seçici (e-posta yerine) | — |

Boş olan kayıtlarda arayüz e-postaya düşer — geriye dönük veri zorlanmaz.

### 26.2 Yarım kalan kullanıcı sorunu (bulgu 10)

`POST /api/users` bugün yalnızca Identity kaydı yazıyor. Rol seçimine göre **domain profili de**
oluşturulur:

| Seçilen rol | Ek olarak oluşturulan | Gerekli alan |
|---|---|---|
| `Member` | `Student` | Öğrenci no, bölüm, kayıt yılı |
| `Advisor` | `AcademicStaff` | Unvan, bölüm |
| `Admin` / `ClubOfficer` | — | — |

Profil gerektiren rol seçilip alanlar boş bırakılırsa **400**; yarım kullanıcı üretmek yasak.

**Y-67 (yeni kural):** Öğrenci veya danışman rolü atanan bir kullanıcı, ilgili domain profili
(`Student` / `AcademicStaff`) olmadan oluşturulamaz. Gerekçe: profilsiz kullanıcı sisteme giriş
yapar ama hiçbir şey yapamaz — hata mesajı ("öğrenci profiline sahip değilsiniz") kullanıcıya
sebebini söylemez ve yönetici de neyi eksik bıraktığını göremez.

### 26.3 Kullanıcı yönetim ekranı

Bugün: e-posta + roller + rol düzenleme. Sonra:

| Kolon / aksiyon | Not |
|---|---|
| Ad Soyad | Boşsa "—" |
| E-posta | |
| Roller | Mevcut |
| Durum | Aktif / Pasif rozeti (`lockoutEnd`) |
| Rolleri Düzenle | Mevcut |
| Pasife Al / Aktifleştir | Mevcut `lockout` ucu ilk kez arayüze bağlanır (bulgu 11) |
| Sil | O-16: bağlı kayıt varsa 409, kendini silemez |
| **Yeni Kullanıcı** | Mevcut `POST /users` ucu ilk kez arayüze bağlanır |

`UserListItemDto` `FirstName`, `LastName`, `IsLockedOut` kazanır.

### 26.4 Silme ucu

`DELETE /api/users/{id}` (`roles.manage`). Sırayla kontrol: kendisi mi → 409 · `Student` kaydı ve
ona bağlı üyelik/başvuru/etkinlik kaydı var mı → 409 (hangisi engelliyorsa mesajda) · `AcademicStaff`
kaydı bir kulübe danışman mı → 409 · hiçbiri yoksa Identity kaydı silinir.

### Çıkış koşulu
Admin ad soyadla kullanıcı oluşturuyor · `Member` seçince öğrenci no istiyor ve oluşan kullanıcı
**gerçekten kulübe başvurabiliyor** · pasife alınan kullanıcı giriş yapamıyor · üyeliği olan
kullanıcı silinemiyor (409, sebep mesajda) · admin kendini silemiyor/kilitleyemiyor · danışman
seçicide e-posta yerine ad soyad görünüyor · `/api/public/*` cevaplarında ad soyad **geçmiyor**.

### Tamamlanma notu

**Testler:** 317/317 yeşil (177 Business + 11 Architecture + 129 Integration). Yeni
`UserProvisioningTests` (7).

| Canlı kontrol | Sonuç |
|---|---|
| Kullanıcı listesi kolonları | `Ad Soyad · E-posta · Roller · Durum` |
| "Yeni Kullanıcı" düğmesi | Var (uç ilk kez arayüze bağlandı) |
| Profil alanları rol seçilmeden | **Gizli** |
| `Member` seçilince | Öğrenci no / bölüm / kayıt yılı **beliriyor** |
| Ad soyadla arama | Bulundu |
| Pasife alma | Rozet `Aktif` → `Pasif` |
| Silme (bağsız kullanıcı) | Başarılı |

**Beklenen ve kastedilen iki test kırılması** — ikisi de Y-67'nin kanıtı:
`RoleAdminManagerTests.CreateUserAsync_ValidRoles` ve `UserAdministrationTests`'in "admin doğrudan
kullanıcı oluşturur" testi, `Member` rolüyle **profilsiz** kullanıcı oluşturuyordu. Artık bu bir
hata; ikisi de profil alanlarını sağlayacak şekilde güncellendi ve yanına "alanlar eksikse Identity
kaydı da yazılmaz" reddi eklendi.

**Planda olmayan bir iyileştirme:** `SelectableAcademicStaffDto`'dan **e-posta kaldırıldı.**
Bu uç `reference.manage` istemiyor — yani herhangi bir öğrenci tüm danışmanların e-postasını
görebiliyordu (PLAN-V3 §17.4'te "gösterecek başka alan yok" gerekçesiyle bilinçli taviz olarak
kaydedilmişti). Ad soyad gelince gerekçe ortadan kalktı; DTO artık `FullName` taşıyor ve yalnızca
adı boş olan eski kayıtlarda e-postaya düşüyor. Yönetici ucu (`AcademicStaffListItemDto`) e-postayı
korur — orada gerçekten gerekli.

**Y-31 ihlali yakalandı:** `RoleAdminManager`'a eklediğim `IClock` hiç kullanılmıyordu
(`CS9113`). Kaldırıldı — tam yeniden derlemede 0 uyarı.

---

## Faz 27 — Danışman yönetimi ve kulüp danışmanı değişimi (bildirilen #3) — ✅ tamamlandı

### 27.1 `AcademicStaff` salt-okunur olmaktan çıkar

Bugün `AcademicStaff` yalnızca demo seed'de doğuyor. Uçlar (`reference.manage`):

`POST /api/academic-staff` · `PUT /api/academic-staff/{id}` (unvan, bölüm) ·
`DELETE /api/academic-staff/{id}` — kulübe danışmansa **409**.

Oluşturma iki yoldan da mümkün olur: mevcut bir `Advisor` rollü kullanıcıya profil eklemek, veya
Faz 26'daki "Yeni Kullanıcı" akışında birlikte oluşturmak.

### 27.2 Kulübün danışmanı değiştirilebilir

`UpdateClubRequestDto` `AdvisorId` kazanır; `ClubManager.UpdateAsync` yeni danışmanın varlığını
doğrular (`AdvisorNotFound` precedent'i) ve `[CacheRemoveAspect]` zaten yerinde.

> **Dikkat — yetki devri:** danışman değişimi, eski danışmanın o kulüpteki **tüm** yetkisini
> (etkinlik oluşturma, onaylama, üye rolü, logo) anında kaybettirir; `Ensure*` metotları
> `club.AdvisorId`'yi okuduğu için ek bir iş gerekmez. Arayüz bunu onay diyaloğunda açıkça söyler.

### 27.3 Arayüz

`ClubDetailPage` → Genel sekmesinde "Danışman" satırı ve `RemoteSelect` ile değiştirme ·
`ReferenceDataPage`'e üçüncü sekme: **Akademik Personel** (liste, ekle, düzenle, sil).

### Çıkış koşulu
Admin yeni bir danışman oluşturup kulübe atayabiliyor · eski danışman o kulüpte artık işlem
yapamıyor, yenisi yapabiliyor · kulübe danışmanlık yapan personel silinemiyor (409).

### Tamamlanma notu

**Testler:** 325/325 yeşil (180 Business + 11 Architecture + 134 Integration). Yeni
`AdvisorTransferTests` (5) ve `ReferenceDataManagerTests`'e 3 birim testi.

**Yetki devri simetrik olarak sınandı** — testin asıl konusu buydu:

| An | Eski danışman | Yeni danışman |
|---|---|---|
| Devirden **önce** | Etkinlik oluşturabiliyor (200) | **403** |
| Devirden **sonra** | **403** | Etkinlik oluşturabiliyor (200) |

Ek bir kod gerekmedi: `Ensure*` metotları `club.AdvisorId`'yi okuduğu için devir kendiliğinden
yetkiyi taşıyor. Test bunun gerçekten böyle olduğunu kanıtlıyor.

| Canlı kontrol | Sonuç |
|---|---|
| Referans Verisi → **Akademik Personel** sekmesi | Var (`Ad Soyad · Unvan · E-posta`) |
| Kullanıcı seçici (sunucu aramalı) | Kullanıcıyı buldu |
| Akademik personel oluşturma | Başarılı |
| Kulüp düzenleme diyaloğunda **Danışman** alanı | Var |
| Danışman değiştirme | Başarılı |

**Tasarım notu:** `UpdateClubRequestDto.AdvisorId` **nullable** — `null` gelirse mevcut danışman
korunur (kısmi güncelleme). Aksi hâlde yalnızca adı düzeltmek isteyen her istek danışmanı da
göndermek zorunda kalır, unutulduğunda kulüp sessizce danışmansız kalırdı.

`UpdateAcademicStaffRequestDto`'da `ApplicationUserId` **yok**: profilin hangi kullanıcıya ait
olduğu kimliğin parçası — `Student.StudentNumber` ile aynı gerekçe (PLAN-V4 §19.2).

---

## Faz 28 — Gerçek referans verisi ve demo veri (bildirilen #1, #2)

### 28.1 Fakülte ve bölümler — `HasData` (O-17)

19 fakülte, 121 bölüm. Dosyadaki kodlama bozuktu (UTF-8 baytları Latin-1 okunmuş:
`AkÃ§adaÄ` → **Akçadağ**); liste düzeltilmiş hâliyle girilir.

| Fakülte | Bölüm |
|---|---|
| Akçadağ MYO | 5 |
| Arapgir MYO | 8 |
| Battalgazi MYO | 5 |
| Darende Bekir Ilıcak MYO | 3 |
| Doğanşehir Vahap Küçük MYO | 3 |
| Hekimhan Mehmet Emin Sungur MYO | 4 |
| İşletme ve Yönetim Bilimleri Fakültesi | 5 |
| Kale Turizm ve Otel İşletmeciliği MYO | 2 |
| Lisansüstü Eğitim Enstitüsü | 36 |
| Mühendislik ve Doğa Bilimleri Fakültesi | 5 |
| Rektörlük | 1 |
| Sanat Tasarım ve Mimarlık Fakültesi | 6 |
| Sağlık Bilimleri Fakültesi | 6 |
| Sağlık Hizmetleri MYO | 3 |
| Sivil Havacılık Yüksekokulu | 1 |
| Sosyal ve Beşeri Bilimler Fakültesi | 13 |
| Tıp Fakültesi | 1 |
| Yeşilyurt MYO | 6 |
| Ziraat Fakültesi | 8 |
| **Toplam** | **121** |

**Kritik migration detayı:** bugünkü seed `Faculty Id=1 "Mühendislik Fakültesi"` ve
`Department Id=1 "Bilgisayar Mühendisliği"`. Demo öğrencinin `DepartmentId = 1` FK'sı buna bağlı.
Bu yüzden **Id 1 silinmez**: fakülte 1 *"Mühendislik ve Doğa Bilimleri Fakültesi"* olarak
yeniden adlandırılır, bölüm 1 *"Bilgisayar Mühendisliği"* olarak onun altında kalır. Diğer 18
fakülte ve 120 bölüm eklenir. Id'siz yazılsaydı migration mevcut satırı silmeye çalışıp FK
kısıtına takılırdı.

`Department`'ın `(FacultyId, Name)` benzersizliği aynı bölüm adının farklı fakültelerde tekrar
etmesine (ör. "Bilgisayar Teknolojileri" hem Akçadağ hem Arapgir'de) izin veriyor — kontrol edildi.

### 28.2 Demo veri — `DemoDataSeeder` (O-17)

Yalnızca `Seed:Demo=true` iken çalışır, **idempotent** (`IdentitySeeder`'ın create-if-missing
deseni). Üretecekleri:

| Veri | Adet | Not |
|---|---|---|
| Danışman (kullanıcı + `AcademicStaff`) | 6 | Farklı fakültelerden, ad soyadlı |
| Öğrenci (kullanıcı + `Student`) | 25 | Farklı bölümlerden |
| Kulüp | 8 | Gerçekçi adlar, farklı danışmanlar, biri pasif |
| Üyelik | ~40 | Rol dağılımı: her kulüpte 1 başkan, 1-2 yetkili, kalanı üye |
| Etkinlik | ~20 | Her durumdan: `Draft`, `PendingApproval`, `Published`, `Rejected`, `Cancelled`, geçmiş ve gelecek |
| Etkinlik katılımı | ~60 | Kontenjan dolu bir etkinlik dahil |
| Duyuru | ~12 | Hem `Public` hem `Members`, biri sistem duyurusu (`ClubId = null`) |

Amaç sadece "veri olsun" değil: **her ekranın boş olmayan hâlini** görebilmek ve her durum rozetinin
gerçek bir kayıtla test edilebilmesi.

### 28.3 Yerel veritabanı temizliği (bildirilen #2'nin asıl karşılığı)

Dev veritabanında 100+ sahte kulüp ve "Faz14/Faz20/FAZ21…" etkinlikleri birikmiş —
**doğrulama betiklerinin artığı, seed değil.** Yeni bir bakım komutu (`Seed:ResetDemo=true`)
demo verisini ve bu artıkları silip yeniden üretir. Üretim verisine dokunmaz: yalnızca
`DemoDataSeeder`'ın kendi işaretlediği kayıtları hedefler.

**Y-68 (yeni kural):** Demo veri üretim ortamına yazılamaz; `DemoDataSeeder` yalnızca açık
konfigürasyon anahtarıyla ve ürettiği kayıtları geri bulabilecek şekilde işaretleyerek çalışır.
Gerekçe: işaretsiz demo veri ile gerçek veri bir kez karıştığında ayrıştırılamaz.

### Çıkış koşulu
Kayıt formundaki bölüm seçicisinde 19 fakültenin 121 bölümü aranabiliyor · demo seed sonrası her
ekran dolu ve her durum rozeti en az bir gerçek kayıtla görülebiliyor · seeder iki kez
çalıştırıldığında **yeni satır üretmiyor** · `Seed:Demo` kapalıyken hiçbir demo kayıt oluşmuyor.

---

## Faz 29 — Yetki matrisi ikiye ayrılır (bildirilen #6)

Tek sayfa iki sekme → iki rota:

| Rota | Sayfa | İzin |
|---|---|---|
| `/authorization/roles` | **Roller ve İzinler** | `roles.manage` |
| `/authorization/users` | **Kullanıcılar** (Faz 26'nın zenginleşmiş ekranı) | `roles.manage` |

Sidebar "Yönetim" grubunda iki ayrı öğe olur (Faz 23'ün grup yapısı bunu kaldırıyor: grup 3'ten
4 öğeye çıkar, ölçülen taşma payı buna yeter). `/authorization` eski rotası
`/authorization/roles`'a yönlendirilir — dışarıda kalmış link kırılmaz.

Bu faz en sona konuldu: Faz 26 kullanıcı ekranını baştan yazıyor, önce bölmek iki kez iş olurdu.

### Çıkış koşulu
İki rota ayrı ayrı açılıyor, sekme başlıkları farklı, sidebar'da iki öğe var ve menü hâlâ
kaydırmasız sığıyor · eski `/authorization` linki çalışıyor.

---

## Adım 0 — Belgeler (koddan ÖNCE)

`docs/MIMARI.md` v5.0:

- **§2 yeni yasaklar:** Y-65 (Hangfire köprü çerezinin kapsamı) · Y-66 (kapsam metodu yönetici
  kontrolüyle başlar) · Y-67 (profilsiz öğrenci/danışman kullanıcısı) · Y-68 (işaretsiz demo veri).
- **§6 yeni kararlar:** A-55 `clubs.manage.all` · A-56 kişi adları ve anonim yüzey sınırı ·
  A-57 kullanıcı silme sözleşmesi · A-58 seed ayrımı (referans `HasData` / demo config kapılı) ·
  A-59 oturum önyüklemesi (üçüncü durum).
- **§3 kapsam:** K-31 Yönetici kapsamı · K-32 Kişi kimliği ve kullanıcı yönetimi · K-33 Danışman
  yönetimi · K-34 Kurumsal referans verisi ve demo veri.
- **§4 domain tablosu:** `ApplicationUser` satırına `FirstName`/`LastName`.
- Sayaçlar: `54 karar / 64 kural / 23 faz` → `59 karar / 68 kural / 29 faz`.

**§3'ün "V1 dışında kalanlar" tablosu değişmez** — K-02, K-04, K-07, K-09…K-12, K-14…K-20 ertelenmiş
kalır. Özellikle forum/mesajlaşma/anket (K-16), aidat (K-15), SignalR (K-04), KVKK silme akışı
(K-19) bu planın **hiçbir fazında** yapılmaz.

---

## Uygulama sırası

| # | Faz | Migration | Bağımlılık |
|---|---|---|---|
| **0** | MIMARI v5.0 | — | — |
| **24** | Oturum + Hangfire | — | yok — bağımsız, hemen sevk edilebilir |
| **25** | Yönetici kapsamı | `clubs.manage.all` claim'i | yok |
| **26** | Kişi kimliği + kullanıcı yönetimi | `FirstName`/`LastName` | yok |
| **27** | Danışman yönetimi | — | Faz 26 (isim + profil oluşturma) |
| **28** | Referans + demo veri | 19 fakülte / 121 bölüm | Faz 26, 27 (demo veri isim ve danışman ister) |
| **29** | Yetki matrisi ayrımı | — | Faz 26 (ekranı o yazıyor) |

Sıra gerekçesi: **24 en önde** çünkü şema değişikliği yok, günlük acıyı hemen dindiriyor ve
diğer fazların canlı doğrulamasını kolaylaştırıyor (her F5'te yeniden giriş yapmak zorunda kalmamak).
**28 sonlarda** çünkü demo veri ancak isimler ve danışman yönetimi varken gerçekçi üretilebilir.

---

## Doğrulama

**Her fazda (V1-V4 ile aynı):**
- `dotnet build` **0 uyarı** (Y-31) · `dotnet test` dört proje yeşil · `npm run build` + `npm run lint` temiz.
- `Architecture.Tests` yeşil (Faz 25 bir test **ekler**).
- Her yeni iş kuralı için bir kabul + bir ret testi (A-20).
- Tarayıcıda canlı yürüyüş (scratchpad Playwright kalıbı).

**Faza özel kritik testler:**

| Test | Neyi kanıtlar |
|---|---|
| `SessionBootstrapTests` | Refresh çerezi geçerliyken açılışta oturum geri geliyor; çerez yokken `/login`'e düşülüyor, **arada boş ekran/yanlış yönlendirme yok** |
| `HangfireCookieScopeTests` | Köprü çerezi `Path=/hangfire`, `HttpOnly`, `Secure`; `/api/*` isteğine **gönderilmiyor**; çerezle API'ye kimlik doğrulanamıyor (Y-65) |
| `AdminClubScopeTests` | **Faz 25'in kabul testi.** `clubs.manage.all` taşıyan kullanıcı 4 kilitli ucun hepsinde başarılı; taşımayan aynı uçlarda 403 |
| `ScopeGuardArchitectureTest` | `Ensure*Access*` metotlarının **tamamı** `clubs.manage.all` sabitini içeriyor (Y-66) |
| `UserProvisioningTests` | `Member` rolüyle oluşturulan kullanıcının `Student` kaydı var ve **kulübe başvurabiliyor**; profil alanları eksikse 400 (Y-67) |
| `UserDeletionGuardTests` | Üyeliği olan kullanıcı 409; bağsız kullanıcı siliniyor; admin kendini silemiyor |
| `PublicSurfaceLeakTests` *(genişletilir)* | Anonim uçlarda ad soyad **geçmiyor** (Y-58'in yeni alanla sınanması) |
| `AdvisorTransferTests` | Danışman değişince eski danışman 403, yeni danışman 200; danışmanlık yapan personel silinemiyor |
| `ReferenceSeedTests` | 19 fakülte / 121 bölüm yüklü; fakülte 1 ve bölüm 1 **korunmuş** (FK kırılmamış) |
| `DemoSeedIdempotencyTests` | İkinci çalıştırma sıfır yeni satır; `Seed:Demo` kapalıyken sıfır kayıt |

---

## Açık riskler

| Risk | Karşılık |
|---|---|
| `clubs.manage.all` bir kapsam metodunda unutulur → yönetici sessizce kilitli kalır | `ScopeGuardArchitectureTest` derleme sonrası IL taraması (§25.3) |
| Ad soyad anonim vitrine sızar | `DTOs/Public/` ayrı ailesi + `PublicSurfaceLeakTests`'in yeni alanla genişletilmesi (Y-58) |
| Hangfire çerezi API'ye de gider → Y-48'in CSRF savunması delinir | `Path=/hangfire` + `SameSite=Strict` + `HangfireCookieScopeTests`'in çerezle API çağrısını denemesi |
| 121 bölümlük `HasData` migration'ı mevcut FK'ları kırar | Fakülte 1 / bölüm 1 **yeniden adlandırılır, silinmez** (§28.1); migration önce boş bir DB'de, sonra mevcut dev DB'de denenir |
| Demo veri üretime sızar | `Seed:Demo` varsayılan **kapalı** + üretilen kayıtların işaretlenmesi (Y-68) |
| Oturum önyüklemesi her açılışta gereksiz `/auth/refresh` atar | Yalnızca **bir kez**, uygulama kökünde; anonim sayfalarda (`PublicLayout`) atlanır — Faz 14'te `/api/public/*` için kurulan muafiyetin aynısı |
| Faz 26 migration'ı mevcut kullanıcıları bozar | Alanlar **nullable**; boşsa arayüz e-postaya düşer; zorunluluk V5'e alınmadı (O-15) |
| Kullanıcı silme yanlış kaydı siler | Silme öncesi bağ kontrolü + 409; `DELETE` yalnızca `roles.manage`; öz-silme yasak |

### Kritik dosyalar

- `arayuz/src/auth/AuthContext.tsx:17` — oturum önyüklemesinin gireceği yer (Faz 24)
- `arayuz/src/components/ProtectedRoute.tsx:17` — üçüncü durumu bilmesi gereken yönlendirme
- `src/WebAPI/Program.cs:139` — Hangfire token köprüsü
- `src/Business/Concrete/EventManager.cs:431` — kapsam metotlarının kanonik örneği (Faz 25)
- `src/Business/Concrete/FileManager.cs:114` — yalnızca danışman kabul eden en dar kontrol
- `src/DataAccess/Seed/IdentitySeedData.cs` — yeni izin; **bir sonraki boş claim Id = 37**
- `src/Business/Concrete/RoleAdminManager.cs:186` — yarım kullanıcı üreten satır (Faz 26)
- `src/DataAccess/Seed/DomainSeedData.cs` — 19 fakülte / 121 bölüm (Faz 28)
- `src/Business/Concrete/IdentitySeeder.cs:93` — kulübün seed'e gömülü olduğu yer; `DemoDataSeeder`'a taşınır
- `docs/MIMARI.md` — Adım 0'da güncellenir, sonrasında tek doğruluk kaynağı
