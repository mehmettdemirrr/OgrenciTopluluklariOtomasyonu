# V3 — Operasyonel olgunluk ve kurumsal kimlik (Faz 15 → 18)

## Context

`docs/PLAN-V2.md`'nin yedi fazı (8-14) bitti, test edildi ve pushlandı (son commit `a21dd66`; 271 test yeşil).
Sistem artık işlevsel olarak bütün: öğrenci kendi hesabını açıyor, kulübe başvuruyor, etkinliğe katılıyor,
duyuru akışı var, panel/denetim/vitrin çalışıyor.

V3 yeni bir özellik alanı açmıyor. **Dört somut boşluğu kapatıyor** — üçü kullanıcı tarafından bildirildi,
biri o incelemede ortaya çıktı. Hepsi kodda tek tek doğrulandı:

| # | Bulgu | Kanıt | Sonuç |
|---|---|---|---|
| **1** | **E-posta hiç gönderilmiyor** | `SmtpEmailSender.cs:29` — `SmtpClient` kuruluyor ama `EnableSsl` **hiç set edilmiyor**. Backend logunda: `SmtpException: 5.7.0 Must issue a STARTTLS command first` | Gmail/Office365 587 portunda STARTTLS zorunlu. Kimlik bilgisi eklemek tek başına yetmez — K-03'ün tamamı (doğrulama + sıfırlama + bildirim) sessizce ölü. |
| **2** | **SMTP kimlik bilgisi kaynak koda gömülü** | `SmtpSettings.cs:17-19` — `Username`/`Password` property'lerinin **C# varsayılan değeri** olarak gerçek Gmail uygulama parolası yazılmış. Ayrıca `appsettings.json`'da ikinci bir parola var. | **Y-20'nin doğrudan ihlali** ve `appsettings`'ten daha kötü: varsayılan değer, config boş olsa bile devreye girer. İki parola da iptal edilmeli. |
| **3** | **Trafik/erişim logu yok** | Pipeline'da tek middleware `GlobalExceptionMiddleware` (`Program.cs:176`). Kullanıcı/IP/tarayıcı/URL/süre kaydeden hiçbir bileşen yok. | `AuditLog` yalnızca **veri değişikliğini** tutar, isteği tutmaz. "Şu kullanıcı şu sayfaya girdi" sorusu bugün cevaplanamaz. |
| **4** | **Öğrenci topluluk kuramıyor** | `ClubApplication` diye bir entity yok; `POST /api/clubs` doğrudan `clubs.write` istiyor (yalnızca Admin claim'i). | Kulüpler yalnızca Admin tarafından doğrudan doğuyor. `MembershipApplication` deseninin kulüp karşılığı eksik. |

**Beşinci boşluk — yazma formları ve ölü uçlar.** Faz 9-13'te backend uçları açıldı ama arayüz tarafı
eksik kaldı; üç uç bugün **hiç çağrılmıyor**:

| Uç / alan | Durum |
|---|---|
| `POST /api/events/{id}/poster` | Backend + `files.upload` izni var, **arayüzde sıfır çağıran**. Faz 14'ün `PublicEventListItemDto.PosterFileId` alanı bu yüzden her zaman `null`. |
| `PUT /api/announcements/{id}` | Backend var, arayüzde yalnızca `DELETE` çağrılıyor (`ClubDetailAnnouncementsTab.tsx:56`). Duyuru düzenlenemiyor. |
| Etkinlik **oluşturma** formu | `EventsPage.tsx:166` yalnızca `title`/`startDateUtc`/`endDateUtc` gönderiyor. `CreateEventRequestDto` ayrıca `Description`/`Location`/`Capacity` taşıyor — **düzenleme** formu (`EventDetailPage.tsx:90`) üçünü de soruyor. Asimetrik: kontenjan yalnızca sonradan girilebiliyor. |
| Diyalog doğrulaması | `LoginPage`/`RegisterPage` react-hook-form + zod kullanıyor; **diğer tüm yazma diyalogları** ham `useState` + `disabled={x.trim() === ''}` ile çalışıyor. Alan bazlı hata mesajı yok. |

**Altıncı boşluk — kurumsal kimlik.** Footer yalnızca `PublicLayout`'ta var ve metni jenerik
("Öğrenci Toplulukları Otomasyonu"). `AppShell` altındaki tüm sayfalarda ve `LoginPage`/`RegisterPage`
gibi kabuk dışı sayfalarda footer hiç yok. Üniversite logosu hiçbir yerde kullanılmıyor — marka
`GroupsRoundedIcon` (MUI jenerik ikonu) ile temsil ediliyor.

**Hedef:** V1/V2'nin mimari disiplinini (Y-01…Y-58, beş katman, tek paradigma) hiç bozmadan bu altı
boşluğu kapatmak. V3 **yeni bir domain alanı açmaz**; K-15/K-16/K-04/K-13/K-19 hâlâ V1 dışıdır.

---

## Onaylanan kararlar (2026-08-22)

Üç açık soru **önerilen varsayılanlarla onaylandı**. Bundan sonra plan bu üç seçimi veri kabul eder;
değişirlerse Faz 17 ve 18 yeniden yazılır (Faz 15/16 etkilenmez).

| # | Karar | **Onaylanan** | Reddedilen alternatif |
|---|---|---|---|
| **O-1** | Trafik logunda IP | **Tam IP + 30 gün saklama.** Y-26'ya belgelenmiş istisna (A-44): yalnızca `TrafficLog` tablosu, yalnızca `audit.read` ile okunur, gecelik bakım işi 30 günden eskisini siler. | Son okteti maskeli IP (`192.168.1.0`) — Y-26 tartışması kapanırdı, tek kullanıcı takibi zayıflardı. |
| **O-2** | Log kapsamı | **Yazma + auth + hata.** `POST`/`PUT`/`DELETE` + `/api/auth/*` + 4xx/5xx dönen her istek. "Kim ne yaptı" tam cevaplanır, hacim yönetilebilir kalır. | Tüm istekler (GET dahil) — gezinti izi de tutulurdu ama her sayfa 5-10 istek attığı için tablo çok hızlı büyürdü. |
| **O-3** | Topluluk başvurusu onayı | **Öğrenci danışman seçer → Admin onaylar.** Onayda `Club` otomatik oluşur, başvuran öğrenci o kulübe `President` atanır. | Danışmanı admin atar; veya iki aşamalı danışman+admin onayı. |

> **O-1'in bedeli kayıt altına alınır:** IP adresi KVKK kapsamında kişisel veridir. `docs/MIMARI.md` §3
> zaten *"audit, DB log, Hangfire parametreleri ve rapor dosyaları kişisel veri yüzeyini büyütüyor"*
> uyarısını taşıyor ve K-19 (silme/anonimleştirme akışı) bilinçli olarak V1 dışı. Bu yüzden trafik logu
> **Y-26'yı sessizce esnetmek yerine** açık bir karara (A-44) ve saklama sınırına bağlanıyor.
>
> **O-3'ün Y-58 teması:** öğrenciye danışman listesi gösterilmesi gerektiği için §17.4'teki dar izdüşüm
> ucu (yalnızca `Id` + unvan + görünen ad, **e-posta yok**) artık opsiyonel değil, fazın parçasıdır.

---

## Faz 15 — Acil düzeltmeler ve kurumsal kimlik

En küçük faz, en yüksek aciliyet. Backend'de iki dosya, frontend'de kabuk katmanı.

> **Durum: 15.1 ve 15.2 uygulandı ve doğrulandı** (commit bekliyor). 15.3 logo dosyası geldiği için
> artık açık; uygulanmadı.

### 15.1 SMTP düzeltmesi (K-03'ün gerçekten çalışması) — ✅ tamamlandı

**İki ayrı hata var; ikisi de düzeltilmeden e-posta gitmez.**

| Sorun | Düzeltme |
|---|---|
| `EnableSsl` set edilmiyor → STARTTLS reddi | `SmtpSettings`'e `bool EnableSsl { get; init; } = true;` eklenir, `SmtpEmailSender` `client.EnableSsl = smtp.EnableSsl;` yapar. **Varsayılan `true`** — güvenli tarafta başlar, şifresiz sunucu istisna olarak kapatılır. |
| Kimlik bilgisi kaynak kodda varsayılan değer | `SmtpSettings.Username`/`Password` varsayılanları `null`'a döner. Gerçek değerler **yalnızca user-secrets**: `dotnet user-secrets set "Smtp:Username" ...` / `"Smtp:Password"`. `appsettings.json`'daki `Username`/`Password` anahtarları da silinir. |

Ek sertleştirme: `SmtpEmailSender` `SmtpException`'ı yutmaz (Hangfire yeniden denesin — Y-47).

**Uygulamada ortaya çıkan üçüncü hata:** `appsettings.json`'da `Smtp:Host` **boştu**; `SmtpEmailSender`
bu durumda gönderimi tamamen atlayıp yalnızca log yazıyordu. Yani `EnableSsl` düzeltilse bile e-posta
gitmezdi. Host `smtp.gmail.com` olarak yazıldı. Ayrıca `FromAddress` boşsa `Username`'e düşülüyor —
böylece kişisel e-posta adresi commit'lenen bir dosyada durmak zorunda kalmıyor; ikisi de boşsa
açıklayıcı `InvalidOperationException` atılıyor (sessiz başarısızlık yerine).

> **Bu fazın ilk işi kod değil:** her iki sızmış Gmail uygulama parolası da Google hesabından iptal
> edilmeli. `SmtpSettings.cs` ve `appsettings.json` git geçmişinde duruyor; parolalar iptal edilmeden
> dosyayı temizlemek yeterli değildir.

**Y-60 (yeni kural):** dış servis kimlik bilgisi (SMTP, API anahtarı, bağlantı dizesi) C# property
varsayılan değeri, `const` veya `readonly` alan olarak kaynak koda yazılamaz. *(Y-20'nin güçlendirilmesi —
kural `appsettings.json`'ı sayıyordu, ihlal kaynak kodda gerçekleşti.)*

### 15.2 Ortak footer (A-47) — ✅ tamamlandı

Yeni `arayuz/src/components/layout/AppFooter.tsx` — tek metin, tek yer:

> `© {yıl} Malatya Turgut Özal Üniversitesi Dijital Dönüşüm Koordinatörlüğü. Tüm Hakları Saklıdır`

Yıl `new Date().getFullYear()` ile üretilir (sabit yazılmaz). Üç kabuğa bağlanır:

| Kabuk | Öncesi | Sonrası |
|---|---|---|
| `AppShell` (giriş yapılmış tüm sayfalar) | footer **yok** | `Container` altına `AppFooter` |
| `PublicLayout` (vitrin) | jenerik metin | `AppFooter` ile değişti |
| `LoginPage`/`RegisterPage`/`ForgotPassword`/`ResetPassword`/`ConfirmEmail` | footer **yok** | Yeni `AuthLayout` kabuğu |

**Uygulamada çıkan tuzak — `100vh` + footer:** beş kimlik sayfasının kökü `minHeight: '100vh'` kullanıyordu.
Altlarına footer eklemek her birinde gereksiz bir kaydırma çubuğu üretirdi. Footer'ı beş sayfaya tek tek
eklemek yerine ince bir `AuthLayout` kabuğu (`minHeight: 100vh` flex sütun + `<Outlet/>` + `AppFooter`)
yazıldı ve sayfa kökleri `flex: 1`'e çevrildi. Ölçülen dikey taşma: beşinde de **0px**.
`LoginPage`/`RegisterPage`'in marka panelindeki kendi telif satırları kaldırıldı (tekrar olurdu).

Y-56 geçerli: renkler `theme` üzerinden (`text.secondary`, `divider`), hex yazılmaz.

### 15.3 Kurumsal logo (A-47) — ✅ tamamlandı

Logo repoda: `arayuz/src/assets/logo.png`. Kullanılacağı yerler:

- `SideNav` başlığı ve `PublicLayout` üst barı → `GroupsRoundedIcon` yerine gerçek logo
- `LoginPage`/`RegisterPage`'in lacivert marka paneli
- `arayuz/index.html` favicon + `<title>` (bugün Vite varsayılanı)
- `AppFooter`'da küçük boyutta (opsiyonel)

**Uygulandı:** orijinal dosya (2745×2744 px, RGBA, 510 KB) Playwright/canvas ile 256×256'ya indirgendi
(`src/assets/logo.png`, **41.7 KB**) + 32×32 favicon (`public/favicon.png`, 2.7 KB). Orijinal
`docs/logo-orijinal.png` altında saklanıyor. Logo beyaz daire zemin üzerinde tasarlanmış olduğu için
lacivert sidebar'da da okunuyor — **beyaz varyant gerekmedi**, ikinci dosya eklenmedi.

Bağlandığı dört yer: `SideNav` başlığı, `PublicLayout` üst barı, `LoginPage`/`RegisterPage`'in marka
paneli, `index.html` favicon + `<title>`. `HomePage.tsx`'teki `GroupsRoundedIcon` kullanımı (boş kulüp
listesi ikonu) **kasıtlı olarak değiştirilmedi** — o marka değil, semantik bir durum ikonu.

### Çıkış koşulu — hepsi ✅
Gerçek bir SMTP hesabıyla e-posta **gerçekten geldi** (15 Hangfire işi `Succeeded`, logda sıfır
`SmtpException`) · kaynakta hiçbir kimlik bilgisi kalmadı (`git grep` sıfır eşleşme) · footer 8 sayfada
Playwright ile doğrulandı (kimlik sayfalarında 0px taşma) · logo dört yerde görünüyor, dosya 41.7 KB
(< 100 KB hedefi) · `dotnet build` 0 uyarı/0 hata · `npm run build` + `npm run lint` temiz.

---

## Faz 16 — Form ve arayüz olgunluğu (A-46) — ✅ 16.1/16.2 tamamlandı, 16.3 ertelendi

Backend'e **hiç dokunulmadı** — `git diff --stat src/ tests/` boş kaldı, üç uç zaten vardı, yalnızca
çağıran eklendi.

### 16.1 Ölü uçların canlandırılması — ✅ tamamlandı

| Uç | Nereye bağlandı |
|---|---|
| `POST /api/events/{id}/poster` | `EventDetailPage`'e "Afiş Yükle" (`Permissions.FilesUpload` + backend'de danışman kapsamı). `ClubsPage`'in logo yükleme kalıbı (`FormData`, gizli `<input type="file">`) birebir kopyalandı. |
| **Ek bulgu:** anonim vitrin `posterFileId`'yi hiç render etmiyordu | `PublicEventsPage`, `HomePage`, `PublicClubDetailPage` üçüne de `CardMedia` eklendi — uç açık olsa da görsel hiçbir yerde çıkmıyordu. |
| `PUT /api/announcements/{id}` | `ClubAnnouncementsTab`'a (kulüp duyurusu) ve `AnnouncementsPage`'e (yalnızca `clubId === null` sistem duyurusu, `AnnouncementsGlobal` izniyle) düzenle diyaloğu. |
| `CreateEventRequestDto`'nun eksik alanları | `EventsPage`'in oluşturma diyaloğuna Açıklama/Yer/Kontenjan eklendi. |
| **Ek bulgu:** `ClubDetailEventsTab.tsx`'te **ikinci, unutulmuş bir kopya** oluşturma diyaloğu vardı | Aynı eksik alan sorununu taşıyordu (yalnızca başlık/tarih); aynı paylaşılan şema ile düzeltildi. |

Canlı doğrulama (Playwright, danışman hesabıyla): etkinlik oluştur (tam alan seti) → afiş yükle → onaya
gönder → aynı danışman kendi kulübünün onay kuyruğunda görüp onaylar → anonim `/etkinlikler` sayfasında
afiş **görselle** çıkıyor. Duyuru: oluştur → düzenle → güncellenmiş içerik uçtan uca görünüyor.

> **Bulunan ama kasıtlı olarak değiştirilmeyen:** `GetApprovalQueueAsync` yalnızca isteği yapanın
> **danışmanı olduğu** kulüplerin `PendingApproval` etkinliklerini döner (`EventManager.cs:157-172`) —
> yani bir kulübün onay kuyruğunu yalnızca o kulübün danışmanı görür, admin dahil kimse başkasınınkini
> göremez. Bu Faz 9'dan kalma mevcut ve kasıtlı bir kapsam kuralı; Faz 16 dokunmadı, yalnızca doğrulama
> sırasında keşfedildi ve doğru hesapla test edildi.

### 16.2 Form sözleşmesi — ✅ tamamlandı

**A-46:** yazma diyalogları react-hook-form + zod'a geçirildi — etkinlik (oluştur/düzenle, iki kopya),
duyuru (oluştur/düzenle, iki yer), topluluk (oluştur/düzenle), fakülte/bölüm (oluştur/düzenle × 2),
akademik dönem (oluştur/düzenle), rol (oluştur). Ortak `hooks/useFormDialog.ts` (open/close) ve
`schemas/{eventForm,announcementForm,clubForm,referenceForm}.ts` (zod şeması + boş değer + DTO dönüşümü)
çıkarıldı — aynı şema birden çok yazma noktasında tekrar kullanıldığında (ör. `EventsPage` +
`ClubDetailEventsTab`) tek yerden düzeltilir.

**Bilinçli olarak dönüştürülmeyenler:** yalnızca `<select>`'ten oluşan, boş/geçersiz durumu olmayan
diyaloglar (üye rolü değiştirme, rol izin matrisi checkbox'ları, kullanıcı-rol atama checkbox'ları).
Bunlarda doğrulanacak bir "biçim" yok — RHF+zod eklemek saf ceremony olurdu.

Y-35 sınırı korundu: yalnızca biçim doğrulanıyor (zorunlu alan, tarih sırası, pozitif sayı); "bu isimde
kulüp var mı" gibi kararlar API'de kalıyor, dönen `Conflict` `extractErrorMessage` ile gösteriliyor.

### 16.3 Arayüz iyileştirmeleri — ertelendi

| Konu | Bugün | Sonra |
|---|---|---|
| Yükleniyor durumu | Sayfa boş, sonra içerik "zıplıyor" | `Skeleton` iskeletleri (kart galerileri ve detay sayfaları) |
| Sayfa başlığı | Sekmede hep aynı Vite başlığı | `useDocumentTitle` hook'u — her sayfa kendi başlığını yazar |
| Boş durumlar | Bazı sayfalarda ham "No rows" | Hepsi `EmptyState` + aksiyon linki |
| Mobil | `AppShell` drawer var ama tablolar taşıyor | `DataTable` dar ekranda kolon gizleme (`columnVisibilityModel`) |
| Hata gösterimi | Yalnızca snackbar | Kalıcı hatalar için sayfa içi `Alert` (ör. detay 404) |

> Bu dört madde Faz 16'nın çıkış koşuluna bağlı değildi (kod kalitesi/görsel cila, işlevsel boşluk değil);
> zaman kısıtı nedeniyle bilinçli olarak bu turda yapılmadı. İstenirse ayrı, küçük bir faz olarak açılabilir.

### Çıkış koşulu — 16.1/16.2 hepsi ✅
`git diff --stat src/` **boş** · afiş yüklenen bir etkinlik anonim vitrinde görselle çıkıyor (üç sayfada
da) · duyuru düzenleme uçtan uca çalışıyor · etkinlik oluşturma ve düzenleme formları **aynı alan
kümesini** soruyor (iki oluşturma kopyasında da) · `npm run build` + `npm run lint` temiz.

---

## Faz 17 — Topluluk kurma başvurusu (K-29, A-45) — ✅ tamamlandı

`MembershipApplication`'ın kulüp karşılığı. Faz 9'da açılan `clubs.write` yüzeyine **öğrenci tarafını** ekler.

### 17.1 Yeni entity

`ClubApplication` — `MembershipApplication`'ın birebir kalıbı (A-12: olay kaydı → soft delete):

| Alan | Not |
|---|---|
| `StudentId` | Başvuran (Y-22: istemciden gelmez, `ICurrentUser`'dan çözülür) |
| `AcademicTermId` | Güncel dönem (A-13) |
| `ProposedName` | Kulüp adı — unique index'te değil (henüz kulüp değil), onayda çakışma kontrol edilir |
| `Description` | Amaç/açıklama |
| `Justification` | Gerekçe (neden bu topluluk gerekli) |
| `ProposedAdvisorId` | `AcademicStaff` FK (O-3: öğrenci seçer) |
| `Status` | `ApplicationStatus` yeniden kullanılır — `Pending`/`Approved`/`Rejected` |
| `AppliedAtUtc` · `ReviewedAtUtc?` · `ReviewedByUserId?` · `ReviewNote?` | `MembershipApplication` ile aynı |
| `CreatedClubId?` | Onaylandıysa oluşan kulüp — izlenebilirlik |
| `IsDeleted` · `DeletedAtUtc` | Y-16 |

**Filtreli unique index:** `(StudentId, AcademicTermId)` üzerinde `Status = Pending AND IsDeleted = 0` —
bir öğrenci aynı dönemde tek bekleyen başvuru tutabilir (A-39'un `President` index'iyle aynı teknik).

### 17.2 Uçlar

| Metot | Rota | İzin | Not |
|---|---|---|---|
| POST | `/api/club-applications` | — (authenticated) | Kendi başvurusu — `MembershipApplicationManager.ApplyAsync`'in `[SecuredOperation]`'sız precedent'i |
| GET | `/api/club-applications/mine` | — (authenticated) | Kendi başvurularının durumu |
| GET | `/api/club-applications` | `clubs.write` | Admin inceleme kuyruğu, `Status` filtreli |
| PUT | `/api/club-applications/{id}/decision` | `clubs.write` | Onay/ret + not |

### 17.3 Onay akışı — fazın teknik kalbi

`ReviewAsync` **elle transaction** yönetir (Y-41/Y-46 — `MembershipApplicationManager.ReviewAsync` ve
`EventManager.DecideAsync` precedent'i), çünkü commit sonrası Hangfire'a bildirim işi eklenir:

1. Başvuru `Pending` değilse → `Conflict` (idempotentlik).
2. **Ret:** `Status = Rejected`, not kaydedilir, commit, `ClubApplicationDecisionNotificationJob` kuyruğa.
3. **Onay:**
   - `ProposedName` ile aktif kulüp var mı → varsa `Conflict` (kulüp adı unique index'te).
   - `ProposedAdvisorId` hâlâ geçerli mi → yoksa `NotFound`.
   - `Club` oluşturulur (`IsActive = true`, `AdvisorId = ProposedAdvisorId`).
   - Başvuran öğrenci o kulübe **güncel dönemde `ClubRole.President`** üyeliğiyle eklenir (O-3) —
     A-39'un filtreli unique index'i bunu zaten korur.
   - `CreatedClubId` yazılır, `Status = Approved`.
   - Tek `SaveChangesAsync` + commit, **sonra** kuyruğa ekleme.

**Cache:** onay yeni bir kulüp doğurduğu için `[CacheRemoveAspect("ClubManager.", "PublicContentManager.")]`
zorunlu — Faz 14'te çok-desenli hâle getirilen attribute burada ikinci kez kullanılır. Atlanırsa yeni kulüp
hem yetkili listede hem anonim vitrinde 5-10 dakika görünmez.

### 17.4 Frontend

| Rota | Sayfa |
|---|---|
| `/clubs` | "Topluluk Kurmak İstiyorum" butonu (her authenticated kullanıcı) + başvuru diyaloğu |
| `/my-club-applications` | Öğrencinin kendi başvuruları ve durumları (`/my-clubs` komşusu) |
| `/club-applications` | Admin inceleme kuyruğu — `MembershipReviewPage` kalıbı, `ConfirmDialog` ile onay/ret |

Danışman seçici `GET /api/academic-staff` ucunu **kullanamaz** — o uç `reference.manage` istiyor (yalnızca
Admin) ve `AcademicStaff`'ın tüm alanlarını döndürüyor. O-3 onaylandığı için öğrenciye personel listesi
gösterilmek zorunda; çözüm Faz 11'in `GET /api/auth/departments` deseni:

**Yeni dar izdüşüm ucu** — `GET /api/academic-staff/selectable`, authenticated (reference.manage değil).
Mevcut `reference.manage` ucu **olduğu gibi kalır** — ayrı DTO (`SelectableAcademicStaffDto`), ayrı action,
A-42'nin "ayrı DTO ailesi" mantığının aynısı: mevcut DTO'ya sonradan eklenen bir alan bu uçtan sızamaz.

> **Uygulamada değişen karar — e-posta korundu:** Bu şemada kişi adı **hiçbir yerde** tutulmuyor
> (`AcademicStaff.Title` yalnızca akademik unvan, ör. "Dr. Öğr. Üyesi" — kişi adı değil; `ApplicationUser`
> Identity'nin kendisi de ayrı bir ad alanı taşımıyor). E-postayı da gizlersek öğrenci aynı unvana sahip
> birden fazla danışmanı ayırt edemez — dropdown işlevsiz kalır. Bu yüzden **e-posta korundu**; Y-58'in
> lafzı yalnızca **anonim** (`/api/public/*`) yüzeyi bağlar, bu uç authenticated olduğu için kural
> ihlal edilmedi. Gerçek kazanım: `reference.manage`'siz herkes artık danışman e-postasını görebiliyor
> (öncesinde yalnızca Admin) — bu bilinçli, düşük riskli bir genişleme (kurumsal iş e-postası, öğrenci PII'si değil).

### Çıkış koşulu
Öğrenci başvurur → admin kuyruğunda görür → onaylar → **kulüp oluşur, öğrenci o kulübün President'i olur** ·
yeni kulüp **anında** hem `/clubs` hem `/kulupler` (anonim vitrin) listesinde görünür ·
aynı dönemde ikinci bekleyen başvuru DB seviyesinde reddedilir.

> **Düzeltme (uygulama sırasında bulundu):** bu §'ün ilk taslağı "öğrenci artık `EventManager`'ın
> Officer/President dalından etkinlik oluşturabilir" diyordu — bu **yanlış**. `ClubRole.President`
> tek başına `events.write` JWT claim'ini taşımaz; o claim yalnızca Identity **rolünden** gelir
> (Admin/ClubOfficer/Advisor). Onay akışı `ClubMembership.ClubRole = President` yazar ama hiçbir
> Identity rol ataması yapmaz — bu iki kavram (kulüp içi rol ve Identity rolü) bu kod tabanında
> kasıtlı olarak ayrık (bkz. `EventApprovalFlowTests`: officer'a Identity `ClubOfficer` rolü *ayrıca*
> ve elle atanıyor). Yeni başkanın etkinlik oluşturabilmesi için bir admin'in `/authorization`
> ekranından ona `ClubOfficer` rolünü **ayrıca** vermesi gerekir — bu Faz 17'nin kapsamı dışında.

**Doğrulandı:** `ClubApplicationFlowTests` (2 test — onay ve ret akışı, filtreli unique index'in çift
başvuruyu reddetmesi, yetkisiz karar 403, cache geçersizleştirme, audit, Hangfire bildirimi) dahil
**273/273 backend testi yeşil** (271 önceki + 2 yeni). Playwright ile canlı uçtan uca doğrulandı: öğrenci
başvurur → "Başvurularım"da görür → admin kuyrukta görür → onaylar → yeni kulüp `/clubs` listesinde
**anında** (cache düşürme çalışıyor) görünür.

---

## Faz 18 — Trafik ve erişim izi (K-28, A-44)

Kullanıcı isteğinin birebir karşılığı: *kullanıcı id, ip, tarayıcı, method, url, neler değişti, süre (ms)*.
**Y-26'ya belgelenmiş bir istisna gerektirdiği için en sona konuldu** — plandaki tek kural gevşetmesi budur
ve diğer üç faz buna bağlı değildir (istenirse tamamen iptal edilebilir).

### 18.1 Neden ayrı kural gerekiyor

Y-26 *"Kişisel/gizli veriyi log'a, audit'e veya Hangfire iş parametresine yazmak"*ı yasaklıyor. IP adresi
KVKK kapsamında kişisel veridir. Dolayısıyla trafik logu ya kuralı ihlal eder ya da kuralı **açıkça ve
sınırlı biçimde** değiştirir. Seçilen yol ikincisi:

**A-44 kararı — erişim izi dar ve süreli tutulur:**

| İlke | Uygulama |
|---|---|
| Tek tablo | `TrafficLog` — `AuditLog`'dan ayrı; audit veri değişikliğini, bu istek meta verisini tutar |
| Tek okuma ucu | `GET /api/traffic-logs`, **mevcut `audit.read` izni** yeniden kullanılır (yeni claim yok) |
| Saklama sınırı | 30 gün; mevcut `NightlyMaintenanceJob` temizler (refresh token + rapor dosyası temizliğinin yanına üçüncü adım) |
| Gövde yazılmaz | Yalnızca meta veri. İstek/cevap gövdesi, header'lar ve `Authorization` **hiç** kaydedilmez |

**Y-59 (yeni kural):** trafik logu istek/cevap gövdesi, header veya query string'i **ham** kaydedemez;
bilinen hassas anahtarlar (`access_token`, `token`, `password`, `code`) redakte edilir.

> **Somut tehlike:** `SideNav.tsx:54` Hangfire panelini `?access_token=...` ile açıyor. Query string'i ham
> kaydeden bir middleware, **geçerli JWT'leri düz metin olarak veritabanına yazar** ve bunları `audit.read`
> taşıyan herkese gösterir. Redaksiyon bu fazın en kritik tek satırıdır.

### 18.2 Bileşenler

| Katman | Bileşen | İş |
|---|---|---|
| Core | `ICorrelationContext` (scoped) | `CorrelationId` taşır. **Bugün yok**: `context.TraceIdentifier` yalnızca `GlobalExceptionMiddleware`'de kullanılıyor, Business/DataAccess'e hiç geçmiyor. |
| Entities | `TrafficLog` | `IEntity` implemente **etmez** — `AuditLog` gibi generic repository'den erişilmez, kendi kendini loglamaz |
| DataAccess | `ITrafficLogDal`/`EfTrafficLogDal` | `IAuditLogDal` precedent'i (filtreli, sayfalı okuma) |
| DataAccess | `AuditLog.CorrelationId` | **Yeni nullable kolon** — audit satırını isteğe bağlar |
| WebAPI | `RequestLoggingMiddleware` | `Stopwatch` + `HttpContext` → `TrafficLog`. `GlobalExceptionMiddleware`'den **sonra**, `UseAuthentication`'dan **sonra** (kullanıcı kimliği çözülmüş olmalı) |
| Business | `ITrafficLogService`/`TrafficLogManager` | `[SecuredOperation(audit.read)]`, filtreleri DAL'a iletir |

**`TrafficLog` alanları:** `Id`, `CorrelationId`, `UserId?`, `IpAddress` (45 — IPv6), `UserAgent` (512),
`HttpMethod`, `Path` (512), `RedactedQueryString?`, `StatusCode`, `DurationMs`, `TimestampUtc`.

### 18.3 "Neler değişti" — audit'i çoğaltmadan

Kullanıcının istediği *"edit veya silme işlemi yapıldıysa neler değişti"* bilgisi `AuditLog`'da **zaten var**
(K-12: `OldValues`/`NewValues` JSON). Trafik loguna kopyalamak Y-44'ü (*"tek yer: SaveChanges interceptor'ı"*)
ihlal eder ve iki kaynak arasında sapma üretir.

Çözüm — **join**: `AuditSaveChangesInterceptor` yazdığı her satıra `ICorrelationContext.CorrelationId`
değerini de yazar. Trafik logu ekranında bir satıra tıklandığında aynı `CorrelationId`'ye sahip audit
satırları açılır: *"14:32'de kullanıcı 7, 88.x.x.x'ten, Chrome ile `PUT /api/clubs/3` çağırdı (142 ms) →
`Club#3.Name`: 'Eski' → 'Yeni'"*.

Bu ayrıca mevcut `GlobalExceptionMiddleware`'in `TraceIdentifier`'ını da aynı kimliğe bağlar — kullanıcıya
gösterilen destek kimliği artık hem hata logunu hem trafik satırını hem audit satırını işaret eder.

### 18.4 Kapsam ve hacim (O-2)

Kaydedilen: `POST`/`PUT`/`DELETE` + `/api/auth/*` (giriş/çıkış/refresh — başarısız girişler dahil) +
`StatusCode >= 400` dönen **her** istek.
Kaydedilmeyen: başarılı `GET`'ler, `/api/files/*` (görsel), `/api/public/*` (anonim vitrin), `/hangfire`.

> **Kabul edilen risk:** salt okuma gezintisi ("şu kullanıcı şu sayfaya girdi") bu kapsamda tutulmuyor.
> O-2'nin ikinci seçeneği bunu açar; karşılığı tablo hacminin ~10 katına çıkması ve KVKK yüzeyinin
> büyümesidir. Hacim yönetilebilirse sonradan tek `if` ile genişletilebilir.

### 18.5 Frontend

`/audit` sayfası iki sekmeye çevrilir: **Veri Değişiklikleri** (mevcut `AuditLogPage`) ve **Erişim İzi**
(yeni). İkincisinde kullanıcı/IP/tarih/method filtreleri + satır genişletince ilgili audit değişiklikleri.

### Çıkış koşulu
Bir kullanıcı giriş yapıp bir kulüp adını değiştirir → `/audit` → Erişim İzi sekmesinde `PUT /api/clubs/{id}`
satırı IP/tarayıcı/süre ile görünür, satır açılınca eski→yeni değer gelir · `?access_token=...` içeren bir
istek atılır ve **token veritabanında hiçbir yerde bulunmaz** (`PublicSurfaceLeakTests` ruhunda bir sızıntı
testi) · 30 günden eski satır gecelik işten sonra kalmıyor · `audit.read` taşımayan kullanıcı 403 alıyor.

---

## Adım 0 — Belgeler (koddan ÖNCE)

`docs/MIMARI.md`'nin kendi kuralı: *"kapsam değişikliği gerekirse önce burası güncellenir, sonra kod."*
İlk commit iki dosyadır: bu plan (`docs/PLAN-V3.md`) ve güncellenmiş `docs/MIMARI.md`.

> O-1/O-2/O-3 onaylandığına göre Y-26 istisnasının metni artık kesin — Adım 0 bloke değil, uygulama
> başlarken ilk iş odur. (Faz 15'in kodu bu sırayı bir kez bozdu: SMTP arızası acil olduğu için
> belgeden önce düzeltildi. Faz 16-18'de sıra korunur.)

**§3'e yeni V1+ maddeleri:** K-28 Trafik ve erişim izi · K-29 Topluluk kurma başvurusu.

**§6'ya yeni kararlar:** A-44 erişim izi ve saklama sınırı · A-45 topluluk kurma başvurusu ·
A-46 form sözleşmesi (react-hook-form + zod) · A-47 kurumsal kimlik (logo + ortak footer).

**§2'ye yeni yasaklar:** Y-59 trafik logunda ham gövde/header/query · Y-60 kaynak koda gömülü dış servis
kimlik bilgisi.

**§2'de Y-26 güncellenir:** "Bunun yerine" sütununa A-44 istisnası eklenir — *"tek istisna: `TrafficLog`,
30 gün saklama, yalnızca `audit.read`, gövde/header/token yazılmaz (A-44, Y-59)."*

**§3'ün "V1 dışında kalanlar" tablosu değişmez** — K-02, K-04, K-07, K-09…K-20 ertelenmiş kalır.
**K-19 satırına not:** *"V3'ün trafik logu (K-28) kişisel veri yüzeyini bir kez daha büyütüyor; 30 günlük
saklama sınırı bunun karşılığıdır."*

**§4 domain tablosuna:** `ClubApplication` ve `TrafficLog` satırları; `AuditLog` satırı `CorrelationId` ile güncellenir.

**§5'e Faz 15-18.** Başlıktaki sayaç tablosu: **43 karar / 58 kural / 14 faz** → **47 karar / 60 kural / 18 faz**
(V1 dışı madde sayısı 13'te kalır).

---

## Uygulama sırası ve çıkış koşulları

| # | Faz | Migration | Bitti sayılır |
|---|---|---|---|
| **0** | `docs/MIMARI.md` güncellemesi | — | Belge K-28/K-29, A-44…A-47, Y-59/Y-60 ve Faz 15-18'i içeriyor |
| **15** | Acil düzeltmeler + kurumsal kimlik | — | ✅ tamamlandı: SMTP + footer + logo |
| **16** | Form ve arayüz olgunluğu | — | ✅ 16.1/16.2 tamamlandı (afiş vitrinde çıkıyor, formlar simetrik, RHF+zod); 16.3 (Skeleton/başlık/mobil) ertelendi |
| **17** | Topluluk kurma başvurusu | `ClubApplication` + filtreli unique index | ✅ tamamlandı — öğrenci başvurur → admin onaylar → kulüp doğar, öğrenci President olur |
| **18** | Trafik ve erişim izi | `TrafficLog` + `AuditLog.CorrelationId` | İstek satırı audit değişikliğine bağlanıyor; token loga sızmıyor; 30 gün temizliği çalışıyor |

*Sessiz onay korunur: her PR'da en fazla bir migration; adlandırma `YYYYMMDD_AçıklayıcıAd`.*

> **Faz 18 tamamen opsiyoneldir:** tek kural gevşetmesi orada. İptal edilirse Faz 15-17 hiç etkilenmez.
> **Faz 16 öne alınabilir:** hiçbir bağımlılığı yok, backend'e dokunmuyor, en hızlı görünür kazanç.

---

## Doğrulama

**Her fazda (V1/V2 ile aynı):**
- `dotnet build` **0 uyarı** (Y-31) · `dotnet test` dört proje yeşil · `npm run build` + `npm run lint` temiz.
- `Architecture.Tests` değişmeden yeşil.
- Her yeni iş kuralı için **bir kabul + bir ret** testi (A-20).
- Tarayıcıda canlı yürüyüş (scratchpad Playwright kalıbı).

**Faza özel kritik testler:**

| Test | Neyi kanıtlar |
|---|---|
| `SmtpSettingsSecretTests` | `SmtpSettings`'in hiçbir property'sinde boş olmayan varsayılan yok (Y-60'ın derlemede kilitlenmesi) |
| `ClubApplicationFlowTests` (2 test — ✅ yazıldı) | Onay: başvuru → çift bekleyen başvuru reddediliyor (filtreli unique index) → yetkisiz karar 403 → onay → **kulüp oluştu + başvuran President** + audit + Hangfire bildirimi + `/api/clubs` cache'i **anında** güncel. Ret: kulüp oluşmuyor, ikinci karar Conflict, ret bildirimi gönderiliyor. (Ayrı dosyalar yerine tek vertical-slice dosyasında — `MembershipVerticalSliceTests` kalıbı.) |
| `TrafficLogRedactionTests` | **Faz 18'in kabul testi.** `?access_token=<jwt>` içeren istek atılır; `TrafficLog` tablosunda JWT'nin hiçbir parçası bulunmaz (Y-59) |
| `TrafficLogCorrelationTests` | Tek bir `PUT` isteği → bir `TrafficLog` satırı + aynı `CorrelationId`'li `AuditLog` satır(lar)ı |
| `TrafficLogAuthorizationTests` | `audit.read` taşımayan kullanıcı `GET /api/traffic-logs`'ta 403 |
| `TrafficLogRetentionTests` | Gecelik bakım işi 30 günden eski satırları siliyor, yenilerini bırakıyor |

---

## Açık riskler

| Risk | Karşılık |
|---|---|
| **Sızmış iki Gmail parolası git geçmişinde kalıcı** | Dosyayı temizlemek yetmez — parolalar Google hesabından **iptal edilmeli**. Faz 15'in kod öncesi ilk adımı budur. |
| Trafik logu KVKK yüzeyini büyütüyor (K-19 hâlâ V1 dışı) | A-44'ün üç kısıtı: 30 gün saklama + `audit.read` + gövde/header yazılmaması. MIMARI.md risk tablosuna yazılır. |
| Query string ham kaydedilirse JWT veritabanına düşer | Y-59 + `TrafficLogRedactionTests`. Hangfire linki (`SideNav.tsx:54`) bunun canlı örneği. |
| `TrafficLog` her yazma isteğinde ikinci bir DB yazması ekliyor | O-2 kapsamı (yazma + auth + hata) hacmi sınırlar. Yazma **`SaveChanges`'ten bağımsız** olmalı — iş transaction'ı geri alınsa bile erişim izi kalmalı (Y-43'ün Serilog'daki mantığının aynısı). |
| `AuditLog.CorrelationId` nullable eklenirken mevcut satırlar boş kalır | Beklenen: geçmiş satırlar `null`; ekran bunu "ilişkilendirilemedi" olarak gösterir, migration veri doldurmaz. |
| Öğrenciye danışman listesi göstermek Y-58 ruhuna dokunuyor | O-3 onaylandığı için kaçınılmaz. Karşılık: ayrı ve dar izdüşüm ucu — yalnızca `Id` + unvan + görünen ad, **e-posta yok** (§17.4). Mevcut `reference.manage` ucu değişmez. |
| Onay akışı kulüp + üyelik + rol'ü tek işlemde yazıyor | `MembershipApplicationManager.ReviewAsync` birebir kopyalanacak referans; elle transaction + commit sonrası enqueue (Y-41/Y-46). |
| Faz 16 yüzeyi geniş (~10 diyalog) | Backend'e sıfır dokunuş; `git diff --stat src/` boş kalmalı; her sayfa ayrı commit (Faz 8 disiplini). |
| ~~Logo 510 KB / 2745×2744 px~~ | ✅ Çözüldü — 256×256'ya indirgendi (41.7 KB), koyu zeminde okunuyor, ikinci varyant gerekmedi. |

### Kritik dosyalar

- `src/Core/Utilities/Email/SmtpSettings.cs:17-19` — **sızmış kimlik bilgisi**, Faz 15'in ilk hedefi
- `src/Core/Utilities/Email/SmtpEmailSender.cs:29` — `EnableSsl` eksik; K-03'ün gerçek arızası
- `src/WebAPI/Program.cs:176-188` — middleware sırası; `RequestLoggingMiddleware` buraya girer
- `src/WebAPI/Middleware/GlobalExceptionMiddleware.cs:18` — `TraceIdentifier`, `ICorrelationContext`'in tohumu
- `src/DataAccess/Interceptors/AuditSaveChangesInterceptor.cs` — Faz 18'de `CorrelationId` yazacak tek yer (Y-44)
- `src/Business/Concrete/MembershipApplicationManager.cs` — Faz 17'nin birebir kopyalanacak akış referansı
- `src/Business/Concrete/MaintenanceManager.cs` — Faz 18'in 30 günlük temizliği buraya eklenir
- `src/Core/Aspects/Autofac/CacheRemoveAspectAttribute.cs` — Faz 14'te çok-desenli oldu; Faz 17 ikinci kullanıcısı
- `arayuz/src/components/layout/AppShell.tsx` + `PublicLayout.tsx` — Faz 15.2'nin footer bağlantı noktaları
- `arayuz/src/pages/EventsPage.tsx:163-181` — eksik alanlı oluşturma diyaloğu (Faz 16.1)
- `arayuz/src/components/layout/SideNav.tsx:54` — `?access_token=` içeren Hangfire linki (Y-59'un canlı örneği)
- `docs/MIMARI.md` — Adım 0'da güncellenir, sonrasında tek doğruluk kaynağı olarak kalır
