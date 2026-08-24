# Öğrenci Toplulukları Otomasyonu — Mimari Taslak

**Sürüm:** v4.0 · v1.0: 17 Ağustos 2026 (Faz 1-7, kararlar kapandı) · v2.0: 21 Ağustos 2026 (Faz 8-14
eklendi) · v3.0: 23 Ağustos 2026 (Faz 15-18 eklendi) · v4.0: 23 Ağustos 2026 (Faz 19-23 eklendi)
**Referans mimari:** [engindemirog/NetCoreBackend](https://github.com/engindemirog/NetCoreBackend)
**Uygulama planları:** [docs/PLAN-V2.md](PLAN-V2.md) (Faz 8-14) · [docs/PLAN-V3.md](PLAN-V3.md) (Faz 15-18)
· [docs/PLAN-V4.md](PLAN-V4.md) (Faz 19-23) — gerekçe, sıra ve doğrulama adımları

Tek uygulama, beş katman, tek veritabanı. v1.0'ın 36 kararı, v2.0'ın 7 yeni kararı (A-37…A-43) verildi;
v3.0 dört yeni fazla (K-28, K-29) 4 karar (A-44…A-47) ve 2 kural (Y-59, Y-60) ekledi; v4.0 beş yeni fazla
(K-30) 7 karar (A-48…A-54) ve 4 kural (Y-61…Y-64) ekledi. Yığın, kapsam ve kurallar sabit; bundan sonrası
uygulama.

> **Bu belge tek doğruluk kaynağıdır.** Bir kural veya kapsam değişikliği gerekirse önce burası
> güncellenir, sonra kod. Aksi hâlde belge ile kod arasındaki fark sessizce büyür ve mimari testler
> tek başına kalır.

| | |
|---|---|
| Karar | 53 (36 v1.0 + 7 v2.0 + 4 v3.0 + 6 v4.0) |
| Yasak kural | 63 (52 v1.0 + 6 v2.0 + 2 v3.0 + 3 v4.0) |
| V1 dışı madde | 13 (K-13 v4.0'da **ikiye bölündü** — bkz. §3) |
| Uygulama fazı | 23 (7 v1.0 + 7 v2.0 + 4 v3.0 + 5 v4.0) |

**Yığın:** .NET 8 LTS · ASP.NET Core Identity · EF Core 8 / MSSQL · Autofac + async AOP ·
FluentValidation · AutoMapper · Hangfire · Serilog → MSSQL · ClosedXML · React 18 + Vite + TypeScript + MUI

---

## İçindekiler

1. [Katmanlar ve bağımlılık yönü](#1-katmanlar-ve-bağımlılık-yönü)
2. [Açıkça yasak (Y-01 … Y-64)](#2-açıkça-yasak)
3. [V1 kapsamı (K-01 … K-30)](#3-v1-kapsamı)
4. [Teknoloji ve domain](#4-teknoloji-ve-domain)
5. [Uygulama sırası](#5-uygulama-sırası)
6. [Karar kaydı (A-01 … A-54)](#6-karar-kaydı)
7. [Sessiz onaylar](#7-sessiz-onaylar)

---

## 1. Katmanlar ve bağımlılık yönü

Beş proje, tek yön. Ok her zaman yukarıdan aşağıya; ters ok yok, çapraz ok yok, yeni katman yok.

| Katman | Sorumluluk | Referans verir |
|---|---|---|
| **WebAPI** | Controller, Autofac kurulumu, middleware, Swagger, Identity/JWT konfigürasyonu, Hangfire sunucusu ve paneli, dosya servis ucu, arayüz build çıktısı | Business, Entities, Core |
| **Business** | İş kuralları, servis arayüzleri + Manager'lar, FluentValidation kuralları, AutoMapper profilleri, mesaj sabitleri, Autofac modülü, rapor servisleri, Hangfire iş sınıfları | DataAccess, Entities, Core |
| **DataAccess** | `IXxxDal`, EF implementasyonları, `AppDbContext` (Identity dahil), entity konfigürasyonları, migration'lar, audit interceptor'ı, aggregate rapor sorguları | Entities, Core |
| **Entities** | POCO entity'ler, `ApplicationUser`, DTO'lar. Davranış yok. | Core |
| **Core** | Generic repository sözleşmesi, async aspect altyapısı, `ICacheManager`, `ICurrentUser`, `IResult` ailesi, güvenlik ve dosya yardımcıları, IoC araçları | — (yok) |

**İstek akışı**

```
HTTP → Controller → IXxxService → [SecuredOperation → Validation → Transaction → Cache]
     → IXxxDal → EF Core → MSSQL

commit sonrası → Hangfire kuyruğu → worker → IXxxService
```

**Klasör düzeni** (A-02)

```
OgrenciTopluluklariOtomasyonu/
├── src/
│   ├── Core/
│   ├── Entities/
│   ├── DataAccess/
│   ├── Business/
│   └── WebAPI/          ← mevcut proje bu ada dönüşür
├── tests/
│   ├── Business.Tests/
│   ├── WebAPI.IntegrationTests/
│   └── Architecture.Tests/
├── arayuz/              ← React + Vite + TypeScript
├── legacy/              ← eski Platform/SharedKernel kodu, çözüme dahil değil
└── docs/MIMARI.md       ← bu belge
```

### Test projeleri

| Proje | Neyi test eder | Nasıl |
|---|---|---|
| `Business.Tests` | İş kuralları, yetki kararları, onay akışı, dönem kuralları, rapor yetki kapsamı | xUnit + DAL mock'ları; her kural için bir kabul + bir ret (A-20) |
| `WebAPI.IntegrationTests` | Auth ve refresh akışı, yetki, eşzamanlılık kısıtları, dosya yükleme/servis, indirme yetkisi | `WebApplicationFactory` + LocalDB (A-19); Hangfire testte senkron |
| `Architecture.Tests` | Yasak listesinin ölçülebilir kısmı | NetArchTest.Rules — paket zaten repoda |

> **Yasak listesini teste çevir.** Y-01, Y-02, Y-05, Y-07, Y-08, Y-09, Y-27, Y-30, Y-37 kuralları
> NetArchTest ile derlemede kırmızıya döner. Belgede kalan kural ihlal edilir; testte kalan edilmez.

---

## 2. Açıkça yasak

Elli iki kural. Tartışmaya kapalı. Bir kuralın gerçekten yanlış olduğunu düşünüyorsan kuralı
değiştiririz — ama önce belge değişir, sonra kod.

### Katman ve bağımlılık

| # | Yasak | Bunun yerine |
|---|---|---|
| **Y-01** | Controller içinde `DbContext`, `IXxxDal`, LINQ sorgusu veya iş kuralı bulunması | Controller bir servis metodu çağırır, `IResult`'ı HTTP'ye çevirir (A-03) |
| **Y-02** | Core'un Entities dışında herhangi bir üst katmana referans vermesi | Core projeye özgü hiçbir şey bilmez |
| **Y-03** | İş kuralının Business dışında yaşaması: controller'da `if`, entity içinde metot, DB trigger'ı, stored procedure, frontend'de karar | Tek doğruluk kaynağı `XxxManager` ve `BusinessRules.Run(...)` |
| **Y-04** | DataAccess içinde validasyon, yetki kontrolü, kullanıcı mesajı veya iş kuralı | Tek istisna: audit interceptor'ı — kural işletmez, kayıt tutar |
| **Y-05** | Katman atlamak: WebAPI'nin DataAccess tiplerini kullanması (transitif referansla görünür olsa bile) | Mimari testle kilitlenir |
| **Y-06** | V1'e ikinci paradigma sokmak: MediatR/CQRS, DDD aggregate, domain event, event bus, outbox, Minimal API, Onion/Clean klasör düzeni | Tek paradigma: klasik n-layer. `legacy/` altındaki kod referans bile alınmaz (A-01) |
| **Y-07** | Yeni katman/proje eklemek (Application, Infrastructure, Shared, Common…) | Beş proje sabit. Ortak kod Core'a gider |
| **Y-08** | `IQueryable`, `DbSet`, `ChangeTracker`, `EntityState` gibi EF tiplerinin DataAccess dışına sızması | DAL metodu daima materyalize sonuç döner |
| **Y-09** | Entity sınıfının HTTP request/response gövdesinde yer alması | Giriş/çıkış DTO; dönüşüm Business'ta AutoMapper ile |

### Veri erişimi ve veritabanı

| # | Yasak | Bunun yerine |
|---|---|---|
| **Y-10** | Lazy loading açmak veya navigation property'ye tembel erişerek N+1 üretmek | Açık `Include` ya da doğrudan DTO'ya projeksiyon |
| **Y-11** | Liste uçlarının sayfalama olmadan tüm tabloyu dönmesi | `GetListPagedAsync` + `PagedResult<T>`; varsayılan 20, üst sınır 100 (A-16) |
| **Y-12** | String birleştirerek SQL kurmak, `FromSqlRaw`'a interpolasyonla parametre geçmek | LINQ; zorunluysa parametreli çağrı |
| **Y-13** | `EnsureCreated()`, elle DDL, migration dışı şema değişikliği | Şemanın tek kaynağı EF migration'ları. Hangfire kendi şemasını kendi kurar |
| **Y-14** | İkinci bir iş `DbContext`'i, static/singleton context, `new AppDbContext()` | Tek context, scoped ömür. Serilog ve Hangfire kendi bağlantılarını kullanır |
| **Y-15** | Repository metodunun kendi içinde `SaveChanges` çağırması | Repo işaretler; kaydetmeyi Business açıkça yapar (A-05) |
| **Y-16** | Bir **olayı** temsil eden kaydı fiziksel silmek: üyelik, başvuru, etkinlik, katılım, duyuru, audit | Soft delete + query filter. Referans verisi hard delete edilebilir (A-12) |
| **Y-17** | `DateTime.Now` kullanmak | UTC ve enjekte edilen saat soyutlaması |
| **Y-18** | Benzersizlik ve kontenjan kurallarını yalnızca uygulama kodunda tutmak | Unique index + `rowversion` son sözü söyler (A-15) |

### Güvenlik

| # | Yasak | Bunun yerine |
|---|---|---|
| **Y-19** | Identity'nin parola altyapısını devre dışı bırakmak veya kendi hash yöntemini takmak (A-28) | `UserManager` üzerinden parola işlemleri; ayrı `PasswordSalt` sütunu yok |
| **Y-20** | JWT anahtarı, SMTP parolası, prod connection string veya herhangi bir secret'ı `appsettings.json` ile commit etmek | Dev'de user-secrets, prod'da ortam değişkeni |
| **Y-21** | Uçların varsayılan olarak anonim olması | Global fallback policy; anonim uçlar tek tek `[AllowAnonymous]` (Y-52'ye tabi) |
| **Y-22** | İstemciden gelen `userId`, `role`, `clubId` gibi kimlik/yetki bilgisine güvenmek | Kimlik yalnızca token claim'inden |
| **Y-23** | Rol kontrolünü kaynak sahipliği kontrolü sanmak | İzin token'dan, kapsam her yazma işleminde `ClubMembership` sorgusundan (A-10) |
| **Y-24** | Prod'da `AllowAnyOrigin` + credentials | A-23 gereği tek origin; CORS hiç açılmaz |
| **Y-25** | Exception mesajını, stack trace'i veya SQL hatasını istemciye dönmek | Log'a tam detay, istemciye korelasyon kimliğiyle nötr ProblemDetails |
| **Y-26** | Kişisel/gizli veriyi log'a, audit'e veya Hangfire iş parametresine yazmak | Yalnızca teknik kimlikler. Hangfire iş parametreleri panelde okunabilir. **Tek istisna (v3.0):** `TrafficLog` — yalnızca IP/tarayıcı/URL, 30 gün saklama, yalnızca `audit.read`, gövde/header/token asla yazılmaz (A-44, Y-59) |

### Kod hijyeni

| # | Yasak | Bunun yerine |
|---|---|---|
| **Y-27** | `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`, `async void` | Uçtan uca async (A-04), `CancellationToken` taşınır |
| **Y-28** | Controller'da `try/catch` ile hata yönetimi | Global exception middleware tek yerden |
| **Y-29** | Kullanıcıya gidecek mesajı koda gömmek veya tekrarlamak | `Business/Constants/Messages` |
| **Y-30** | Servis imzasının `object`, `dynamic` veya anonim tip dönmesi | Her uç tipli DTO döner |
| **Y-31** | Nullable uyarısını `!` ya da `#pragma warning disable` ile susturmak | Uyarı zaten hata; modeli düzelt |
| **Y-32** | AutoMapper profillerinde koşullu mantık, DB çağrısı, iş kuralı; Entity → Entity map'lemek | Mapping yalnızca alan taşır. `AssertConfigurationIsValid` testi zorunlu |
| **Y-33** | `ServiceTool` / service locator'ı Business veya Controller kodunda kullanmak | Yalnızca aspect içinde |
| **Y-34** | Testlerde paylaşılan/gerçek veritabanı, `Thread.Sleep`, sıraya bağımlı testler | Her test kendi verisini kurar ve temizler |

### Frontend (`arayuz/`)

| # | Yasak | Bunun yerine |
|---|---|---|
| **Y-35** | İş kuralını arayüzde tekrar yazmak (yetki kararı, kontenjan hesabı, onay akışı kuralı) | UI gizler/gösterir; kararı API verir. **Butonu gizlemek yetki değildir** |
| **Y-36** | Arayüzden doğrudan veritabanına erişim, secret'ın frontend koduna girmesi, her ekran için özel uç açmak | Kaynak odaklı REST; ekran farkı sorgu parametresiyle |

### Kararlardan doğan kurallar

| # | Yasak | Bunun yerine |
|---|---|---|
| **Y-37** | Identity'nin yanına paralel kimlik/yetki tablosu açmak (A-09) | Kullanıcı Identity'de, izinler rol claim'lerinde, öğrenci alanları `Student` profilinde |
| **Y-38** | Access token ömrünü uzatarak refresh akışını atlamak; refresh token'ı kullanıldıktan sonra geçerli bırakmak (K-01) | Access 15 dk, refresh tek kullanımlık ve iptal edilebilir; izinler her refresh'te yeniden çözülür |
| **Y-39** | Access token'ı `localStorage`'da veya URL'de taşımak; refresh token'ı JavaScript'in okuyabileceği yerde tutmak (A-22, A-30) | Access `sessionStorage`, refresh `httpOnly` + `SameSite=Strict` çerez |
| **Y-40** | Yüklenen dosyayı istemcinin verdiği adla kaydetmek; uzantıya bakarak tip doğrulamak; yürütülebilir içeriğe izin vermek (K-05) | Sunucu tarafında üretilen ad, içerik imzasıyla tip kontrolü, boyut sınırı |
| **Y-41** | E-posta gönderimini iş transaction'ının içinde yapmak (K-03) | Önce commit, sonra kuyruğa ekleme |
| **Y-42** | Rapor uçlarının veriyi generic repository ile çekip bellekte toplaması (K-06) | Toplama SQL'de; DAL doğrudan rapor DTO'suna projekte eder. Excel sunucuda üretilir |
| **Y-43** | Log kayıtlarını iş `DbContext`'i veya iş transaction'ı üzerinden yazmak (A-18) | Serilog kendi bağlantısını kullanır — geri alınan transaction hata kaydını silemez |
| **Y-44** | Audit kaydını iş kodunda elle yazmak veya bazı yazma işlemlerinde atlamak (K-12) | Tek yer: `SaveChanges` interceptor'ı. Soft delete de "silme" olarak kaydedilir |
| **Y-45** | İzin/rol değişikliği sonrası izin cache'ini düşürmemek (K-17 × A-17) | Yetki matrisini değiştiren her uç ilgili anahtarları temizler |
| **Y-46** | Hangfire işini transaction içinde kuyruğa eklemek; paneli yetkilendirmeden açmak (A-29) | Kuyruğa ekleme commit'ten sonra — aksi hâlde worker henüz var olmayan kaydı arar. Panel yalnızca yönetici izniyle |
| **Y-47** | Arka plan işine entity, `DbContext` veya servis örneği geçmek; işi idempotent olmayacak şekilde yazmak (A-29) | Yalnızca kimlik ve ilkel değerler; iş kendi scope'unu açar. Yeniden deneme kaçınılmaz |
| **Y-48** | Refresh ucunu CSRF korumasız bırakmak (A-30) | Çerezle çalışan tek uç bu; antiforgery token veya özel başlık kontrolü zorunlu |
| **Y-49** | Yükleme klasörünü `UseStaticFiles` ile doğrudan yayınlamak (A-31) | Dosyalar `wwwroot` dışında; erişim tek servis ucundan, doğru `Content-Type` ile |
| **Y-50** | MUI'nin ücretli katmanına bağımlı bileşen kullanmak: satır gruplama, aggregation, kolon sabitleme, ağaç veri, istemci taraflı Excel (A-34) | Ücretsiz DataGrid yeterli: sıralama, filtreleme, sunucu taraflı sayfalama |
| **Y-51** | Rapor işinin, talebi yapan kullanıcının göremeyeceği veriyi üretmesi; indirme anında yetkinin yeniden kontrol edilmemesi (A-35) | Rapor sorgusu talep sahibinin kapsamıyla sınırlı çalışır; dosya indirilirken yetki **yeniden** kontrol edilir — kuyrukta beklerken yetki değişmiş olabilir |
| **Y-52** | Anonim dosya ucunun görünürlüğü "herkese açık" olmayan bir kaydı döndürmesi; `StoredFile`'ı görünürlük alanı olmadan kaydetmek (A-36) | Görünürlük yükleme anında belirlenir: logo/afiş açık, rapor ve liste çıktıları korumalı |

### V2 eklentileri (Faz 8-14)

| # | Yasak | Bunun yerine |
|---|---|---|
| **Y-53** | Kontenjan/benzersizlik kuralını yalnızca uygulama kodunda sayarak korumak (etkinlik katılımı) | Yazma işlemi `Event.RowVersion`'ı tüketir; eşzamanlı iki kayıttan biri `DbUpdateConcurrencyException` ile kaybeder (A-38) |
| **Y-54** | E-posta doğrulama / şifre sıfırlama token'ını Hangfire iş parametresine yazmak | İş yalnızca `userId` alır, token'ı kendi scope'unda üretir (Y-26'nın "panel okunabilir" ilkesiyle çelişmesin diye) |
| **Y-55** | `forgot-password` gibi uçlarda e-postanın kayıtlı olup olmadığını cevaptan sızdırmak | Kayıtlı/kayıtsız e-posta için **aynı** cevap; kullanıcı sayımı (enumeration) engellenir |
| **Y-56** | Marka renklerini bileşen kodunda hex olarak yazmak | Yalnızca `arayuz/src/theme/tokens.ts`; palet üzerinden tüketilir (A-37) |
| **Y-57** | Görünürlük alanı olmadan duyuru kaydetmek; anonim vitrin ucunun yayında olmayan/görünürlüğü kısıtlı kaydı döndürmesi | `Announcement.Visibility` yazma anında zorunlu; anonim uç yalnızca `Visibility == Public` ve silinmemiş kaydı döner (A-43, Y-52'nin duyuru karşılığı) |
| **Y-58** | Anonim vitrin ucundan kişisel veri (ad, e-posta, öğrenci no, üye/katılımcı listesi) döndürmek | Vitrin DTO ailesi (`DTOs/Public/`) yalnızca toplu sayı ve genel içerik taşır; mevcut yetkili DTO'lar anonim uçta yeniden kullanılmaz (A-42) |

### V3 eklentileri (Faz 15-18)

| # | Yasak | Bunun yerine |
|---|---|---|
| **Y-59** | Trafik logunun istek/cevap gövdesini, header'ları veya ham query string'i kaydetmesi | Yalnızca meta veri (IP, tarayıcı, method, URL, süre); bilinen hassas anahtarlar (`access_token`, `token`, `password`, `code`) query string'den redakte edilir — `SideNav.tsx`'in `?access_token=` içeren Hangfire linki bunun somut örneği (A-44) |
| **Y-60** | Dış servis kimlik bilgisini (SMTP, API anahtarı, bağlantı dizesi) C# property varsayılan değeri, `const` veya `readonly` alan olarak kaynak koda yazmak | Yalnızca user-secrets (dev) / ortam değişkeni (prod); property varsayılanları boş döner. Y-20'nin güçlendirilmesi — o kural `appsettings.json`'ı sayıyordu, gerçek ihlal kaynak kodda (`SmtpSettings.cs`) yaşandı |

### V4 eklentileri (Faz 19-23)

| # | Yasak | Bunun yerine |
|---|---|---|
| **Y-61** | İptal edilen etkinliğe yeni katılımcı kaydı almak; iptal edilmiş etkinliği anonim vitrinde veya "yaklaşan etkinlikler"de listelemek | `EventStatus.Cancelled` her okuma filtresinde `Published` dışında sayılır. Mevcut katılımcı kayıtları **silinmez** — öğrenci kaydını iptal rozetiyle görmeye devam eder (A-49) |
| **Y-62** | Liste ucunun tamamını çekip filtrelemeyi/aramayı istemcide yapmak (`pageSize: 200` + `.filter()`) | Arama ve filtre sunucuda, sayfalama `PagedResult` sözleşmesiyle. Y-11'in arayüz tarafındaki karşılığı: Y-11 sunucunun tüm tabloyu dönmesini yasaklıyordu, Y-62 arayüzün "hepsini iste, ben ayıklarım" kaçamağını kapatır (A-50) |
| **Y-63** | E-posta üreten anonim ucu (`register`, `forgot-password`, `resend-confirmation`) oran sınırı olmadan yayına almak | `[EnableRateLimiting]` ile IP bazlı sınır (A-53). Gerekçe: tek uçtan SMTP kotasının tüketilmesi **tüm** bildirim altyapısını durdurur — kayıt doğrulama dahil |
| **Y-64** | Belirlenmiş bir sıralama olmadan `Skip`/`Take` ile sayfalamak | Sıra, sayfalamanın parçasıdır: `IEntity.Id` üzerinden **daima** son kırıcı (tie-breaker) uygulanır, alan bazlı sıralama `OrderBy`+`ThenBy(Id)` ile gelir. Gerekçe: SQL Server `OFFSET/FETCH` için `ORDER BY` şart olduğundan EF Core sırasız sorguya `ORDER BY (SELECT 1)` üretir — 2. sayfa 1. sayfanın satırlarını **tekrarlayabilir veya atlayabilir**. Sayfalama bunu yaparsa "sunucu taraflı sayfalama" bir görüntüden ibarettir (Y-11, Y-62) |

---

## 3. V1 kapsamı

### V1'e alınanlar

| # | Özellik | Ne var | Getirdiği iş |
|---|---|---|---|
| **K-01** | Refresh token ve oturum yönetimi | Access token 15 dk (`sessionStorage`), refresh token 7 gün, tek kullanımlık, `httpOnly` çerezde. İzinler her refresh'te yeniden çözülür | Token tablosu, rotasyon, iptal, CSRF koruması (Y-48), arayüzde sessiz yenileme |
| **K-03** | E-posta gönderimi | Şifre sıfırlama, e-posta doğrulama, başvuru/onay bildirimi. Identity'nin token sağlayıcıları hazır | SMTP konfigürasyonu, şablonlar; gönderim Hangfire işi olarak commit sonrası (Y-41, Y-46) |
| **K-05** | Dosya ve görsel yükleme | Topluluk logosu ve etkinlik afişi. `wwwroot` dışında klasör; açık görseller anonim uçtan | Tip/boyut doğrulama, ad üretimi, görünürlük alanı (Y-52), silinen kaydın dosyası |
| **K-06** | Raporlama ve Excel çıktısı | Özet ekranı (dönem bazlı topluluk/üye/etkinlik sayıları) + üye ve katılım listelerinin Excel çıktısı. Üretim arka planda. **PDF yok** | Aggregate DAL metotları, `ReportRequest` durum makinesi, "raporlarım" ekranı, indirme yetkisi (Y-51) |
| **K-08** | Arka plan işleri | Hangfire + SQL Server storage, uygulama içi worker, yetkilendirilmiş panel. İşler: e-posta, Excel üretimi, gecelik bakım | Hangfire şeması, panel yetkisi, iş sınıfları, idempotentlik (Y-47) |
| **K-12** | Denetim izi | Kim, ne zaman, hangi kaydı, hangi alanları değiştirdi. `SaveChanges` interceptor'ı ile otomatik | Audit tablosu, eski/yeni değer serileştirme, `ICurrentUser` (A-33), PII filtresi (Y-26) |
| **K-17** | Yetki matrisi yönetim ekranı | Rol oluşturma, role izin atama, kullanıcıya rol atama — arayüzden. İzinler Identity rol claim'lerinde | İzin kataloğu, yönetim uçları, cache geçersizleştirme (Y-45) |

### V2'ye alınanlar (Faz 8-14)

| # | Özellik | Ne var | Getirdiği iş |
|---|---|---|---|
| **K-21** | Topluluk yönetimi ve üye rolleri | Kulüp oluşturma/düzenleme (`clubs.write` ilk kez kullanılır), üye listesi, Officer/President atama | `IClubMemberService`, başkan tekilliği (A-39), `ClubManager` cache geçersizleştirme |
| **K-22** | Etkinlik katılımı | Öğrenci kendi kaydını açar/iptal eder, kontenjan `rowversion` ile korunur | `EventParticipation`'ın ilk kullanımı, eşzamanlılık testi (A-38, Y-53) |
| **K-23** | Duyurular | Kulüp duyurusu + görünürlük alanlı (üye/herkese açık) sistem duyurusu | `Announcement`'ın ilk kullanımı, görünürlük alanı (A-43, Y-57) |
| **K-24** | Hesap yaşam döngüsü | Self-servis kayıt, e-posta doğrulama, şifre sıfırlama — **K-03'ün tamamlanması** | Identity token sağlayıcıları, `EmailConfirmed` zorunluluğu, token güvenliği (A-40, Y-54, Y-55) |
| **K-25** | Dashboard ve öğrenci self-servisi | Rol farkında özet ekranı, "kulüplerim"/"etkinliklerim" | Yeni `IDashboardDal`, `IReportScopeResolver` precedent'i |
| **K-26** | Denetim görüntüleme | `AuditLog`'un ilk okuma ucu | `audit.read` izni, PII filtresi testi (Y-26) |
| **K-27** | Herkese açık vitrin ve ana sayfa | Giriş yapmadan görülen duyuru/etkinlik/kulüp vitrini | Dar anonim yüzey: tek önek, tek controller, ayrı DTO ailesi (A-42, Y-58) |

> **K-16 notu (v2.0):** forum, mesajlaşma, anket, QR yoklama, takvim hâlâ V1 dışı — Faz 8-14 yalnızca
> etkinlik katılım kaydını (K-22) ve duyuruyu (K-23) ekliyor, sosyal özellik setinin geri kalanına dokunmuyor.

### V3'e alınanlar (Faz 15-18)

| # | Özellik | Ne var | Getirdiği iş |
|---|---|---|---|
| **K-28** | Trafik ve erişim izi | Kullanıcı/IP/tarayıcı/method/URL/süre kaydı; `AuditLog`'a `CorrelationId` ile bağlı | `TrafficLog` tablosu, `RequestLoggingMiddleware`, 30 gün saklama (A-44, Y-59) |
| **K-29** | Topluluk kurma başvurusu | Öğrenci danışman seçip başvurur, admin onaylar; onayda kulüp doğar ve başvuran `President` olur | `ClubApplication` entity'si, `MembershipApplication` akışının kulüp karşılığı (A-45) |

> **K-19 notu (v3.0):** trafik logu (K-28) kişisel veri yüzeyini bir kez daha büyütüyor; 30 günlük
> saklama sınırı ve dar okuma yetkisi (yalnızca `audit.read`) bunun karşılığıdır — KVKK silme/anonimleştirme
> akışının kendisi hâlâ V1 dışıdır.

### V4'e alınanlar (Faz 19-23)

| # | Özellik | Ne var | Getirdiği iş |
|---|---|---|---|
| **K-30** | Dönem devri | Yeni dönem güncel yapıldığında aktif üyelikler rolleriyle birlikte yeni döneme otomatik kopyalanır | `AcademicTermManager.SetCurrentAsync` genişler; idempotent, pasif kulüp ve soft-delete üyelik atlanır (A-51) |
| **K-13** *(kısmi)* | **Rate limiting** — API versiyonlama V1 dışında kalır | Anonim kimlik uçlarında (`register`, `forgot-password`, `resend-confirmation`, `login`) IP bazlı oran sınırı | `AddRateLimiter` (çerçeve içi, paket yok), 429 → ProblemDetails (Y-25), `UseRateLimiter` erişim izinden **sonra** (A-53, Y-63) |

> **K-25 notu (v4.0):** "Dashboard ve öğrenci self-servisi" Faz 19'da tamamlanır — öğrenci artık üyelik
> başvurusunun durumunu görebiliyor, geri çekebiliyor, kulüpten ayrılabiliyor ve profilini düzenleyebiliyor.
> Faz 12'de yalnızca *okuma* tarafı yapılmıştı (A-48).

> **K-22 notu (v4.0):** etkinlik yaşam döngüsüne `Cancelled` eklenir (A-49) — yayınlanmış etkinlik
> **silinmez, iptal edilir** ve kayıtlı katılımcılara e-posta gider. Silme yalnızca `Draft` için kalır.

### V1 dışında kalanlar — bilinçli kararlar

Bunlar eksik değil, ertelenmiş özellikler. "Kapı" sütunu, bugün ne yapmamız gerektiğini söyler ki
sonradan eklemek pahalı olmasın. **v2.0 ve v3.0 bu tabloyu hiç değiştirmedi; v4.0 yalnızca K-13'ü
ikiye böldü** (rate limiting kapsama girdi, API versiyonlama kaldı). Geri kalan her şey — aidat/ödeme
(K-15), forum/anket/QR (K-16), gerçek zamanlı bildirim (K-04), KVKK silme/anonimleştirme akışının
kendisi (K-19) — hiçbir fazda yapılmaz.

| # | Ertelenen | Kapı |
|---|---|---|
| **K-02** | Üniversite SSO / LDAP / OAuth | Identity harici sağlayıcı desteğini hazır getiriyor |
| **K-04** | Gerçek zamanlı bildirim (SignalR, push, bildirim merkezi) | Rapor durumu periyodik sorguyla izlenir; bildirim tetikleyen noktalar tek serviste |
| **K-07** | Dağıtık cache ve ölçekleme (Redis, çok sunuculu) | `ICacheManager` soyutlaması. Hangfire worker'ı ve bellek içi cache tek instance varsayar |
| **K-09** | Gelişmiş arama (Elasticsearch, full-text) | Arama tek DAL metodunda |
| **K-10** | Çok kampüslü / çok kiracılı yapı | Fakülte/Bölüm tablosu var |
| **K-11** | Çok dillilik | Mesajlar `Messages` sabitlerinde toplu |
| **K-13** *(yarısı)* | **API versiyonlama** — rate limiting v4.0'da kapsama alındı (A-53, §V4'e alınanlar) | Tek sürüm; kırıcı değişiklik gerekirse rota öneki (`/api/v2`) eklenebilir, mevcut controller'lar taşınmaz |
| **K-14** | Docker, CI/CD, observability | Konfigürasyon ortam değişkeniyle geçersiz kılınabilir. **Hangfire paneli V1'in tek izleme aracı** |
| **K-15** | Aidat, ödeme, bütçe | Bağımsız modül olarak sonradan |
| **K-16** | Sosyal özellikler (forum, mesajlaşma, anket, QR yoklama, takvim) | Etkinlik katılım kaydı V1'de var |
| **K-18** | Mobil uygulama / PWA | API istemciden bağımsız |
| **K-19** | KVKK silme/anonimleştirme akışı | Y-26 ve gecelik rapor temizliği. **Dikkat:** audit, DB log, Hangfire parametreleri ve rapor dosyaları kişisel veri yüzeyini büyütüyor |
| **K-20** | Event bus / outbox | `legacy/` altına alınan altyapı V1'e dahil değil; Hangfire benzer ihtiyacı karşılıyor |

---

## 4. Teknoloji ve domain

### Çekirdek yığın

"Referanstan sapma" sütunu NetCoreBackend'den bilinçli olarak ayrıldığımız yerleri gösterir.

| Konu | Seçim | Nerede | Referanstan sapma |
|---|---|---|---|
| Runtime | .NET 8 LTS, C# 12, nullable enable | hepsi | — |
| API stili | ASP.NET Core Controllers | WebAPI | — |
| ORM | EF Core 8 + SQL Server, Code First | DataAccess | `IEntityTypeConfiguration` ile konfigürasyon |
| Repository | `IEntityRepository<T>` + `EfEntityRepositoryBase` + `GetListPagedAsync` | Core + DataAccess | **Sayfalama sözleşmesi eklendi** (A-16) |
| Birim iş | Scoped context; Business açıkça `SaveChangesAsync` | Business | **Repo kendi kaydını yapmıyor** (A-05) |
| DI | Microsoft DI + Autofac | WebAPI kurar, Business modülü tanımlar | — |
| AOP | Castle DynamicProxy + **async farkında** interceptor | Core | **Senkron `MethodInterception` kullanılmıyor** (A-04) |
| Validasyon | FluentValidation + `ValidationAspect` | Business/ValidationRules | DTO'larda DataAnnotation yok |
| Mapping | AutoMapper | Business/Mappings | **Lisans eşiği teyit edilecek** (A-07) |
| Cache | `ICacheManager` → `MemoryCacheManager` | Core | Referans verisi + topluluk listesi + izin kataloğu |
| Kimlik | **ASP.NET Core Identity**, `<int>` anahtarlı, kendi parola hash'iyle | Entities + DataAccess | **Referansın custom şeması ve `HashingHelper`'ı kullanılmıyor** (A-09, A-28) |
| Oturum | JWT access 15 dk + tek kullanımlık refresh (çerezde) | Core/Utilities/Security | **Refresh akışı referansta yok** (K-01, A-30) |
| Yetkilendirme | İzin claim'leri + `SecuredOperation` + Business'ta kapsam kuralı | Business/BusinessAspects | İki katman kontrol |
| Sonuç modeli | `IResult`/`IDataResult<T>` → HTTP koduna çevrilir | Core + WebAPI | **İş kuralı ihlali 200 dönmüyor** (A-03) |
| Hata yönetimi | Global middleware → ProblemDetails (RFC 7807) | WebAPI | Tek hata gövdesi formatı |
| Loglama | Serilog → MSSQL, ayrı bağlantı, `LogAspect`, CorrelationId | Core + WebAPI | **Log DB'de, transaction'dan bağımsız** (A-18) |
| Arka plan | **Hangfire** + SQL Server storage, uygulama içi worker | WebAPI barındırır, işler Business'ta | **Referansta yok** (A-29, A-35) |
| E-posta | SMTP; Hangfire işi, commit sonrası | Core/Utilities + Business | **Referansta yok** (K-03) |
| Denetim izi | `SaveChanges` interceptor'ı + `ICurrentUser` | DataAccess + Core | **Referansta yok** (K-12, A-33) |
| Dosya | `wwwroot` dışı klasör, görünürlük alanlı `StoredFile`, iki servis ucu (anonim/korumalı) | Business + WebAPI | **Referansta yok** (K-05, A-31, A-36) |
| Excel | ClosedXML, arka planda üretim | Business | **Referansta yok** (K-06, A-32, A-35) |
| Test | xUnit + mock + NetArchTest; entegrasyon LocalDB | tests/ | Mimari testler eklendi |

> **Lisans teyidi (tek kalan ev ödevi).**
> **AutoMapper** — gelir eşikli ücretsiz kademe var, projenin girmesi çok muhtemel; uymazsa 13.x'e sabitlenir, kod değişmez.
> **Hangfire** — Core ve SqlServer paketleri bu kullanım için uygun lisansta; Pro paketlerine ihtiyaç yok.
> **ClosedXML** — MIT. EPPlus 5. sürümden sonra koşullu lisansa geçtiği için tercih edilmiyor.

### Aspect kataloğu

Sıra önemlidir: **yetki → validasyon → transaction → cache.** Hepsi async farkında (A-04).

| Aspect | Ne yapar | Nerede kullanılır | Dikkat |
|---|---|---|---|
| `SecuredOperation` | Token'daki izin claim'ini kontrol eder | Yazma metotları, yönetici okumaları | Kaynak sahipliğini **kontrol etmez** — o iş kuralıdır (Y-23) |
| `ValidationAspect` | FluentValidation kuralını çalıştırır | DTO alan her metot | Yalnızca biçimsel doğrulama; "bu isimde topluluk var mı" iş kuralıdır |
| `TransactionAspect` | EF transaction'ı açar/commit eder | Üyelik onayı, etkinlik kaydı, dönem kapatma, yetki atama | Kuyruğa ekleme ve e-posta **içine girmez** (Y-41, Y-46) |
| `CacheAspect` | Sonucu anahtarla saklar | Referans verisi, topluluk listesi, izin kataloğu | Kullanıcıya özel sonuçlarda **kullanılmaz** |
| `CacheRemoveAspect` | Desene uyan anahtarları düşürür | Yazma metotları, yetki matrisi uçları | Y-45 — eksik bırakılırsa kaldırılan yetki çalışmaya devam eder |
| `PerformanceAspect` | Eşik üstü süren metodu log'lar | Rapor sorguları, Excel üretimi | Arka plan işlerinde de devrede |
| `LogAspect` | Çağrıyı ve parametreleri log'lar | Kritik yazma metotları | PII maskelenir (Y-26) |

> **Teknik uyarı — AOP + async.** Referans repodaki `MethodInterception` senkron tasarlanmıştır:
> `invocation.Proceed()` bir `Task` döndürdüğünde "başarılı" ve "sonrası" adımları görev *tamamlanmadan*
> çalışır. Sonuç: transaction metot bitmeden commit edilir, cache'e henüz dolmamış sonuç yazılabilir,
> hatalar aspect tarafından görülmez. Bu yüzden async farkında interceptor **şart** (A-04).

### Frontend (`arayuz/`)

| Konu | Seçim | Not |
|---|---|---|
| Dil / araç | React 18 + Vite + TypeScript | A-21 |
| Bileşen seti | MUI + ücretsiz DataGrid | A-34 · ücretli katman yasak (Y-50) |
| Sunucu durumu | TanStack Query | Rapor durumu için periyodik sorgu (K-04 dışarıda) |
| HTTP | Tek axios instance + interceptor | Token ekleme, 401'de sessiz refresh, ProblemDetails çözümü |
| Oturum | Access `sessionStorage`, refresh çerezde | A-22, A-30 |
| Form | React Hook Form + Zod | Sadece biçim doğrulaması (Y-35) |
| Tablolar | DataGrid, sunucu taraflı sayfalama | `PagedResult` sözleşmesiyle birebir |
| Görseller | `<img src>` ile anonim uçtan | A-36 · tarayıcı önbelleği çalışır |
| İndirmeler | axios blob + geçici nesne URL'i | A-36 · Excel ve korumalı dosyalar |
| API tipleri | OpenAPI şemasından üretim | Sözleşme değişince derlemede kırılır |
| Yayın | Build çıktısı API'nin `wwwroot`'una | A-23 · tek origin, CORS yok |

### V1 çekirdek domain

| Varlık | Rolü | Karar izi |
|---|---|---|
| `ApplicationUser` | Identity kullanıcısı, `int` anahtarlı, PBKDF2 parola | A-09, A-11, A-28 |
| `ApplicationRole` · `RoleClaim` | Roller ve ince taneli izinler | A-10, K-17, Y-37 |
| `RefreshToken` | Tek kullanımlık, iptal edilebilir, çerezde taşınır | K-01, A-30, Y-38 |
| `Student` · `AcademicStaff` | 1-1 profil tabloları | A-14 |
| `Faculty` · `Department` | Referans verisi, hard delete serbest | A-12, A-27 |
| `AcademicTerm` | Akademik dönem | A-13 |
| `Club` | Topluluk, danışmanı, logosu, durumu | K-05, A-31 |
| `ClubMembership` | Dönemsel üyelik + topluluk rolü; `(ClubId, StudentId, TermId)` unique. **Dönem devrinde rolüyle birlikte yeni döneme kopyalanır** | A-13, A-15, K-30, A-51 |
| `MembershipApplication` | Başvuru, onay/ret, soft delete, bildirim tetikler | A-12, K-03 |
| `Event` | Taslak → onay bekliyor → yayında/reddedildi → **iptal edildi** (gerekçesiyle); kontenjan `rowversion` | A-25, A-15, K-05, A-49, Y-61 |
| `EventParticipation` | `(EventId, StudentId)` unique | A-15 |
| `Announcement` | Topluluk duyurusu; `ClubId` nullable (sistem duyurusu) + **görünürlük** (üye/herkese açık) | K-23, A-43, Y-57 |
| `AuditLog` | Kim, ne zaman, hangi alan; interceptor yazar; **`CorrelationId`** (nullable) trafik logu satırına bağlar | K-12, A-33, Y-44, K-28 |
| `StoredFile` | Üretilen ad, tip, boyut, sahibi ve **görünürlük** (açık/korumalı) | K-05, A-31, A-36, Y-52 |
| `ReportRequest` | Rapor talebi: tür, parametreler, durum (kuyrukta → üretiliyor → hazır/hatalı), üretilen dosya | K-06, A-35, Y-51 |
| `ClubApplication` | Topluluk kurma başvurusu: ad/açıklama/gerekçe/önerilen danışman, onay/ret, soft delete; onayda `CreatedClubId` yazılır | K-29, A-45 |
| `TrafficLog` | İstek meta verisi: `CorrelationId`, kullanıcı, IP, tarayıcı, method, URL (redakte), durum kodu, süre. `IEntity` değil — generic repository'den erişilmez | K-28, A-44, Y-59 |

---

## 5. Uygulama sırası

Sıra tesadüfi değil: her faz bir sonrakinin bağımlılığını kurar ve kuralları yazıldıkları anda
uygulanabilir hâle getirir. **Dikey dilim (faz 5) mimarinin tamamını tek akışta sınadığı için ondan
önce hiçbir ikinci özelliğe başlanmaz.**

### 1 — İskelet ve kurallar
`legacy/` taşıması (A-01), beş projelik çözüm (A-02), merkezi paket sürümleri, `Architecture.Tests`.
**Bitti sayılır:** Y-01, Y-02, Y-05, Y-07, Y-08, Y-09, Y-27, Y-30, Y-37 mimari testle kırmızıya dönüyor.

### 2 — Core altyapı
`IResult` ailesi, generic repository + sayfalama, async aspect altyapısı, `ICurrentUser`,
`ICacheManager`, ProblemDetails middleware, Serilog (ayrı bağlantı).
**Bitti sayılır:** boş bir servis metodu aspect zincirinden geçip doğru HTTP kodunu üretiyor.

### 3 — Kimlik ve yetki
Identity `<int>`, JWT + refresh (çerez, CSRF), izin claim'leri, `SecuredOperation`, izin/rol seed'i.
**Bitti sayılır:** giriş → 15 dk sonra sessiz refresh → izinsiz uçta 403.

### 4 — Domain ve migration
Dönem, kulüp, üyelik, başvuru, etkinlik, duyuru, profil tabloları, audit interceptor'ı, `StoredFile`,
`ReportRequest`; unique index'ler ve `rowversion`.
**Bitti sayılır:** çift üyelik ve kontenjan aşımı veritabanı seviyesinde reddediliyor (A-15 testi).

### 5 — Dikey dilim
Giriş → topluluk listesi → üyelik başvurusu → danışman onayı → bildirim e-postası (Hangfire).
Backend + arayüz birlikte.
**Bitti sayılır:** yığının her parçası (aspect, transaction, audit, kuyruk, DataGrid, refresh) tek akışta çalışıyor.

### 6 — Dosya ve raporlama
Logo/afiş yükleme, anonim ve korumalı servis uçları, aggregate rapor sorguları, Excel işi,
"raporlarım" ekranı, gecelik bakım işi.
**Bitti sayılır:** yetkisi alınmış kullanıcı kuyruktaki raporunu indiremiyor (Y-51 testi).

### 7 — Yönetim ekranları
Yetki matrisi (K-17), fakülte/bölüm seed ucu, dönem yönetimi, etkinlik onay kuyruğu, Hangfire paneli erişimi.
**Bitti sayılır:** yetki değişikliği bir sonraki refresh'te etkili oluyor, cache düşüyor (Y-45).

---

### v2.0 — Faz 8-14

Ayrıntılı gerekçe, uçlar ve testler için [docs/PLAN-V2.md](PLAN-V2.md). Aşağıdaki özet yalnızca
sıralama ve "bitti sayılır" koşullarını taşır.

### 8 — Tasarım sistemi ve uygulama kabuğu
Kurumsal renk paleti (A-37), sidebar kabuk, ortak bileşen katmanı, yedi mevcut sayfanın yeniden kurgusu. Backend'e dokunulmaz.
**Bitti sayılır:** `git diff --stat src/` boş; yedi sayfa da yeni kabukta açılıyor; build+lint temiz.

### 9 — Topluluk yönetimi ve üye rolleri
`clubs.write` ilk kez kullanılır; kulüp CRUD, üye listesi, Officer/President atama, başkan tekilliği (A-39).
**Bitti sayılır:** bir öğrenciye `President` verilip `EventManager`'ın Officer/President dalı ilk kez uçtan uca çalışıyor.

### 10 — Etkinlik katılımı ve duyurular
`EventParticipation` ilk kullanımı, kontenjan eşzamanlılığı (A-38, Y-53), `Announcement` görünürlüğü (A-43, Y-57).
**Bitti sayılır:** kontenjanı 1 olan etkinliğe paralel iki kayıttan tam biri başarılı.

### 11 — Hesap yaşam döngüsü
Self-servis kayıt, e-posta doğrulama, şifre sıfırlama (K-03'ün tamamlanması, A-40).
**Bitti sayılır:** kayıt → giriş reddi → doğrula → giriş başarılı; mevcut `AuthTests` bozulmamış.

### 12 — Dashboard ve öğrenci self-servisi
Rol farkında özet ekranı (K-25), `/panel` rotası.
**Bitti sayılır:** üç farklı rol, üç farklı kapsam; kapsam sızıntısı yok.

### 13 — Denetim izi ve referans veri olgunluğu
`AuditLog` ilk okuma ucu (K-26), referans verisinde güncelleme/silme.
**Bitti sayılır:** audit ekranı PII sızdırmıyor (Y-26); kullanımdaki fakülte silinemiyor (409).

### 14 — Herkese açık vitrin ve ana sayfa
Dar, denetlenebilir anonim yüzey (A-42): tek önek `/api/public/*`, tek controller, ayrı DTO ailesi (K-27).
**Bitti sayılır:** giriş yapmadan `/` çalışıyor; `Members` görünürlüklü duyuru ve pasif kulüp görünmüyor; anonim cevapta hiçbir kişisel veri yok (Y-58).

---

### v3.0 — Faz 15-18

Ayrıntılı gerekçe, uçlar ve testler için [docs/PLAN-V3.md](PLAN-V3.md). Aşağıdaki özet yalnızca
sıralama ve "bitti sayılır" koşullarını taşır.

### 15 — Acil düzeltmeler ve kurumsal kimlik
SMTP'nin gerçekten çalışması (`EnableSsl`, kimlik bilgisi kaynak koddan kaldırılır — Y-60), ortak footer,
üniversite logosu (A-47).
**Bitti sayılır:** gerçek SMTP ile e-posta geliyor; kaynakta sıfır kimlik bilgisi; footer her sayfada.

### 16 — Form ve arayüz olgunluğu
Var olan ama arayüzden hiç çağrılmayan üç uç (afiş yükleme, duyuru düzenleme, etkinlik oluşturma alanları)
bağlanır; yazma diyalogları react-hook-form + zod'a geçer (A-46). Backend'e dokunulmaz.
**Bitti sayılır:** `git diff --stat src/` boş; oluşturma ve düzenleme formları aynı alan kümesini soruyor.

### 17 — Topluluk kurma başvurusu
`ClubApplication` — `MembershipApplication`'ın kulüp karşılığı (K-29, A-45). Öğrenci danışman seçer,
admin onaylar; onayda kulüp doğar ve başvuran `President` olur.
**Bitti sayılır:** başvuru → onay → kulüp oluşur, öğrenci o kulübün President'i olur; aynı dönemde ikinci
bekleyen başvuru DB seviyesinde reddediliyor.

### 18 — Trafik ve erişim izi
`TrafficLog` — kullanıcı/IP/tarayıcı/method/URL/süre; `AuditLog.CorrelationId` ile "neler değişti"ye
bağlanır (K-28, A-44). Y-26'nın tek, belgelenmiş istisnası. **Opsiyonel** — diğer üç faz buna bağlı değil.
**Bitti sayılır:** istek satırı audit değişikliğine bağlanıyor; `?access_token=...` içeren istek atılır ve
token veritabanında hiçbir yerde bulunmaz (Y-59); 30 günden eski satır gecelik işten sonra kalmıyor.

---

### v4.0 — Faz 19-23

Ayrıntılı gerekçe, uçlar ve testler için [docs/PLAN-V4.md](PLAN-V4.md).

### 19 — Öğrenci self-servisi, ters proxy IP'si ve oran sınırı
K-25'in tamamlanması (A-48): başvuru takibi/geri çekme, kulüpten ayrılma, profil düzenleme.
Ön iş olarak `UseForwardedHeaders` (Faz 18'in IP kolonunu ters proxy arkasında doğrular) ve ardından
anonim kimlik uçlarına oran sınırı (A-53, Y-63).
**Bitti sayılır:** öğrenci başvurusunu geri çekip yeniden başvurabiliyor; son başkan kulüpten ayrılamıyor;
`forgot-password`'e 6. istek 429 + ProblemDetails alıyor ve bu 429 erişim izinde görünüyor.

### 20 — Etkinlik iptali
`EventStatus.Cancelled` + gerekçe (A-49); katılımcılara Hangfire ile e-posta (Y-41/Y-46).
**Bitti sayılır:** yayındaki etkinlik iptal ediliyor, anonim vitrinden düşüyor, yeni kayıt `Conflict`
veriyor, katılımcı kayıtları **silinmiyor** (Y-61).

### 21 — Sunucu taraflı arama ve gerçek sayfalama
Arayüzün `pageSize: 200` isteyip 100 alması ve kalanı istemcide filtrelemesi düzeltilir (A-50, Y-62).
**Bitti sayılır:** 101. kayıt aranarak bulunabiliyor; `git grep "pageSize: 200"` sıfır sonuç.

### 22 — Otomatik dönem devri
`SetCurrentAsync` üyelikleri rolleriyle yeni döneme taşır (K-30, A-51); `GetMineAsync`'in dönem
filtresi eksikliği düzeltilir.
**Bitti sayılır:** devirden sonra başkan yetkisini koruyup etkinlik oluşturabiliyor; ikinci çalıştırma
sıfır yeni satır üretiyor (idempotent).

### 23 — Arayüz cilası
Sidebar yeniden gruplanır, `Skeleton`/`useDocumentTitle`/mobil kolon gizleme eklenir (A-52). Backend'e dokunulmaz.
**Bitti sayılır:** `git diff --stat src/` boş; 1400×900'de admin menüsü kaydırmasız sığıyor.

---

## 6. Karar kaydı

Bir kararı değiştirmek istersen önce bu tablo güncellenir, sonra kod.

| # | Konu | Karar | Sonuç |
|---|---|---|---|
| **A-01** | Mevcut `src/Platform`, `src/SharedKernel` | B · taşı | `legacy/` altına; çözüme dahil değil |
| **A-02** | Klasör düzeni | A | `src/` + `tests/` + `arayuz/`; mevcut proje `WebAPI` olur |
| **A-03** | Sonuç/hata modeli | C · hibrit | Business `IResult`, controller HTTP koduna çevirir, beklenmeyen hata → ProblemDetails |
| **A-04** | Async + AOP | B | Uçtan uca async, async farkında interceptor |
| **A-05** | `SaveChanges` sahibi | B | Repo işaretler, Business kaydeder (Y-15) |
| **A-06** | Transaction | B | EF `BeginTransactionAsync`; `TransactionScope` kullanılmaz |
| **A-07** | Mapping | B | AutoMapper güncel sürüm; lisans eşiği teyit edilecek |
| **A-08** | Parola hash | *iptal* | A-28 ile geçersiz kaldı |
| **A-09** | Kimlik altyapısı | B | ASP.NET Core Identity, `<int>` anahtarlı |
| **A-10** | Yetki modeli | C | İnce taneli izin claim'leri + Business'ta topluluk kapsamı |
| **A-11** | Anahtar tipi | A · int | — |
| **A-12** | Silme politikası | C · seçici | Olay kayıtları soft, referans verisi hard (Y-16) |
| **A-13** | Dönemsellik | B | `AcademicTerm`; üyelik ve roller döneme bağlı |
| **A-14** | Kullanıcı ve profil | B | `Student`/`AcademicStaff` 1-1 profiller |
| **A-15** | Eşzamanlılık | B | Unique index'ler + `rowversion` |
| **A-16** | Sayfalama | A | `GetListPagedAsync` + `PagedResult<T>` |
| **A-17** | Cache kapsamı | C | Referans verisi + topluluk listesi + izin kataloğu |
| **A-18** | Log hedefi | B · MSSQL | Ayrı bağlantı, iş transaction'ından bağımsız (Y-43) |
| **A-19** | Test veritabanı | A · LocalDB | Bağlantı dizesi konfigürasyondan |
| **A-20** | Test ölçütü | B | Yüzde hedefi yok; her iş kuralı için bir kabul + bir ret |
| **A-21** | Frontend dili | A · TS | React 18 + Vite + TypeScript |
| **A-22** | Access token saklama | sessionStorage | Sekme başına oturum; refresh çerezi yumuşatır |
| **A-23** | Yayın ve CORS | A | Tek origin; CORS açılmaz |
| **A-24** | Dosya yükleme | B · V1'de | Logo ve afiş |
| **A-25** | Etkinlik onayı | B | Taslak → onay bekliyor → yayında/reddedildi |
| **A-26** | Şifre sıfırlama | C · e-posta | Identity token sağlayıcısı |
| **A-27** | Başlangıç verisi | A + B | İzin/rol/dönem `HasData`; fakülte/bölüm seed ucu |
| **A-28** | Parola hash çelişkisi | A | Identity'nin kendi hash'i (PBKDF2) (Y-19) |
| **A-29** | E-posta gönderim biçimi | C · Hangfire | K-08 V1'e açıldı (Y-46, Y-47) |
| **A-30** | Refresh token yeri | B | `httpOnly` + `SameSite=Strict` çerez (Y-39, Y-48) |
| **A-31** | Dosya depolama | B | `wwwroot` dışı klasör, servis tek uçtan (Y-49) |
| **A-32** | Rapor kapsamı | B | Özet ekranı + Excel; PDF yok |
| **A-33** | Audit kullanıcı bilgisi | A | Core'da `ICurrentUser`; HTTP ve sistem implementasyonları |
| **A-34** | Frontend bileşen seti | B · MUI | Ücretsiz DataGrid; ücretli katman yasak (Y-50) |
| **A-35** | Arka plan işleri | B | E-posta + Excel üretimi (Y-51) |
| **A-36** | Dosya erişimi | A | Açık görsel anonim, korumalı indirme blob (Y-52) |
| **A-37** | Kurumsal renk sistemi | B | Turkuaz `#12A7CD` birincil vurgu, `#0B6E87` contained buton zemini (kontrast), tokenlar `theme/tokens.ts`'te (Y-56) |
| **A-38** | Kontenjan eşzamanlılığı | B | `Event.RowVersion` tüketilir; `DbUpdateConcurrencyException` → `Conflict` (Y-53) |
| **A-39** | Başkan tekilliği | B | `(ClubId, AcademicTermId)` üzerinde `ClubRole = President` filtreli unique index |
| **A-40** | Kayıt ve e-posta doğrulama | B | Self-servis kayıt + zorunlu doğrulama; token Hangfire parametresine yazılmaz (Y-54) |
| **A-41** | Sidebar kabuk ve ortak bileşen katmanı | B | `AppShell`/`SideNav` + paylaşılan `Notifier`/`DataTable`/`StatusChip` |
| **A-42** | Anonim vitrin yüzeyi | C | Tek önek `/api/public/*`, tek controller, ayrı DTO ailesi (Y-58) |
| **A-43** | Duyuru görünürlüğü ve sistem duyurusu | B | `Announcement.Visibility` zorunlu, `ClubId` nullable = sistem duyurusu (Y-57) |
| **A-44** | Erişim izi ve saklama sınırı | B | `TrafficLog` ayrı tablo, tam IP + 30 gün saklama, yalnızca `audit.read`, gövde/header/token yazılmaz (Y-26 istisnası, Y-59) |
| **A-45** | Topluluk kurma başvurusu | B | Öğrenci danışman seçer → admin onaylar; onayda `Club` oluşur, başvuran `President` olur (K-29) |
| **A-46** | Form sözleşmesi | A | Her yazma diyaloğu react-hook-form + zod; yalnızca biçim doğrulanır (Y-35) |
| **A-47** | Kurumsal kimlik | A | Ortak footer (tek metin, tek bileşen) + üniversite logosu; marka rengi Y-56'ya tabi |
| **A-48** | Self-servis simetrisi | A | Her başvuru/üyelik akışının öğrenci tarafında karşılığı olur: *başvur → durumu gör → geri çek*. K-25'in tamamlanması |
| **A-49** | Etkinlik iptali | B | Yayınlanmış etkinlik **silinmez, iptal edilir** (`EventStatus.Cancelled` + gerekçe); katılımcı kayıtları durur, e-posta gider. Silme yalnızca `Draft` (Y-61) |
| **A-50** | Arama sözleşmesi | A | Liste uçları `search` alır, filtreleme SQL'de (`LIKE`), sayfalama sunucuda. K-09'un kapısının kullanılması — full-text/Elasticsearch V1 dışı kalır (Y-62) |
| **A-51** | Dönem devri | A · otomatik | `SetCurrentAsync` aktif üyelikleri rolleriyle yeni döneme kopyalar; idempotent, pasif kulüp ve soft-delete üyelik atlanır (K-30) |
| **A-52** | Arayüz cilası sözleşmesi | A | Her sayfa `useDocumentTitle` ile kendi sekme başlığını yazar; veri çeken her görünüm yüklenirken `Skeleton` gösterir |
| **A-53** | Anonim kimlik uçlarında oran sınırı | B | `AddRateLimiter` (çerçeve içi, NuGet paketi yok), IP bazlı `FixedWindow`; **global limiter yok**, giriş yapmış kullanıcı sınırlanmaz; 429 → ProblemDetails (Y-25, Y-63) |
| **A-54** | Önbellek sınırlıdır | B | `AddMemoryCache(SizeLimit)` + her girdi `Size = 1`; `MemoryCacheManager` anahtar defterini **tahliye geri çağrısıyla** temizler. Gerekçe: A-50 ile birlikte cache anahtarına **serbest metin** (`search`) girdi — anonim `/api/public/*` ucundan rastgele arama üreterek hem `IMemoryCache`'i hem anahtar defterini sınırsız büyütmek mümkün olurdu (A-17'nin sınırı) |

### Kararların birbirini etkilediği yerler

| Etkileşim | Durum | Çözüm |
|---|---|---|
| A-28 × A-09 | Çözüldü | Identity'nin parola altyapısı olduğu gibi; `PasswordSalt` sütunu yok (Y-19) |
| A-29 × K-03 | Çözüldü | E-posta Hangfire işi; kuyruğa ekleme commit'ten sonra (Y-41, Y-46) |
| A-29 × A-06 | Kurala bağlandı | İşe entity değil kimlik geçilir, iş kendi scope'unu açar, idempotent yazılır (Y-47) |
| A-29 × A-33 | Çözüldü | `ICurrentUser`'ın sistem kullanıcısı implementasyonu arka plan işlerinde devrede |
| A-30 × A-23 | Çözüldü | Tek origin sayesinde `SameSite=Strict` çerez sorunsuz; refresh ucu CSRF korumalı (Y-48) |
| A-31 × A-22 × A-32 | A-36 ile çözüldü | `<img>` ve indirme `Authorization` başlığı gönderemez; açık görseller anonim uçtan, korumalı indirmeler blob ile (Y-52) |
| A-35 × A-10 | **Dikkat** | Rapor kuyrukta beklerken yetki değişebilir. İçerik üretim anında, indirme yetkisi indirme anında kontrol edilir (Y-51) |
| A-35 × A-36 | Uyumlu | Üretilen Excel bir `StoredFile`; görünürlüğü "korumalı" |
| A-34 × A-32 | Uyumlu | Ücretsiz DataGrid sunucu taraflı sayfalamayı karşılıyor; Excel backend'de üretiliyor (Y-42, Y-50) |
| A-29 × K-14 | Kabul edilen sınır | Worker uygulama içinde barındırılır; tek instance varsayımı (K-07) |
| A-10 × K-01 × K-17 | Çözüldü | Kısa ömürlü access token; izinler her refresh'te yeniden çözülür, topluluk kapsamı her istekte DB'den |
| A-18 × A-05 × A-06 | Kurala bağlandı | Serilog kendi bağlantısını kullanır — geri alınan transaction hata kaydını silemez (Y-43) |
| K-06 × Y-08 × A-16 | Kurala bağlandı | Rapor sorguları özel DAL metotlarıyla, doğrudan rapor DTO'suna; bellekte toplama yasak (Y-42) |
| A-44 × Y-26 × K-19 | Kurala bağlandı | Trafik logu Y-26'yı sessizce esnetmez — dar istisna (tek tablo, 30 gün, tek izin, gövde/header yazılmaz); K-19'un KVKK akışı hâlâ V1 dışı (Y-59) |
| A-45 × A-39 | Uyumlu | Onayda üyelik `ClubRole.President` ile yazılır; A-39'un filtreli unique index'i başkan tekilliğini zaten koruyor |
| A-46 × Y-35 | Çözüldü | Form kütüphanesi yalnızca biçim doğrular; "bu isimde kulüp var mı" gibi kararlar API'de kalır |
| A-51 × A-13 × Y-44 | **Dikkat** | Dönem devri tek transaction'da binlerce `ClubMembership` yazar; audit interceptor entity başına bir satır ürettiği için audit patlaması kaçınılmaz. Baskılanamaz (Y-44). Karşılık: idempotent + yılda 2-3 çalışır. Hangfire'a taşımak audit'te "kim" bilgisini kaybettirir (`ICurrentUser` sistem kullanıcısına düşer) |
| A-51 × A-39 | Uyumlu | Devir 1:1 kopya olduğu için `(ClubId, AcademicTermId)` filtreli başkan unique index'i ihlal edilmez |
| A-53 × A-44 | Kurala bağlandı | `UseRateLimiter`, `RequestLoggingMiddleware`'den **sonra** kaydedilir — aksi hâlde 429'lar erişim izine hiç düşmez (Faz 18'in `UseAuthorization` tuzağının aynısı) |
| A-53 × K-28 | Ön koşul | IP bazlı bölümleme doğru IP ister: `UseForwardedHeaders` olmadan ters proxy arkasında hem trafik logu hem limiter tek IP görür. Faz 19.0 ikisinin de ön koşulu |
| A-49 × Y-52 × Y-58 | Uyumlu | İptal edilen etkinlik anonim vitrinden düşer; görünürlük kararı yine yazma anındaki `Status` alanından okunur, okuma anında yorumlanmaz |

---

## 7. Sessiz onaylar

Ayrı karar beklemeyen, itiraz gelmedikçe geçerli varsayılan kurallar.

| Konu | Varsayılan |
|---|---|
| İsimlendirme dili | Sınıf/metot/tablo adları İngilizce, kullanıcıya giden mesajlar ve yorumlar Türkçe |
| URL biçimi | `/api/clubs`, `/api/clubs/{id}/members` — çoğul, kebab-case, fiil yok |
| Tarih/saat | DB'de ve API'de UTC, ISO-8601; yerel saate çevirmek arayüzün işi |
| Sayfalama | `pageSize` varsayılan 20, üst sınır 100 |
| Enum'lar | DB'de `int`, API'de metin |
| Token ömürleri | Access 15 dakika, refresh 7 gün, tek kullanımlık |
| Yükleme sınırları | Logo/afiş 5 MB, yalnızca JPEG/PNG/WebP, içerik imzasıyla doğrulanır |
| Rapor akışı | Tüm Excel talepleri kuyruğa girer — boyut eşiği yok, çünkü iki yol iki kod yolu demek |
| Rapor saklama | Üretilen dosyalar 7 gün sonra silinir; kullanıcı raporu yeniden talep edebilir |
| Gecelik bakım işi | Tek yinelenen iş: süresi geçmiş refresh token'lar + eskimiş rapor dosyaları |
| Excel kütüphanesi | ClosedXML (MIT). EPPlus lisans koşulları nedeniyle kullanılmaz |
| Audit kapsamı | Tüm entity'ler; parola, token ve hash alanları audit'e yazılmaz |
| Migration adlandırma | `YYYYMMDD_AçıklayıcıAd`, her PR'da en fazla bir migration |
| Dal stratejisi | `master` korumalı; iş `feature/*` dallarında, PR ile birleşir |
| Belge sahipliği | Bu belge + mimari testler tek doğruluk kaynağı; kural değişimi önce burada yazılır |

---

*Mimari taslak v4.0 · 53 karar, 63 kural, 13 V1 dışı madde (K-13 yarısı kapsama alındı), 23 faz · referans: engindemirog/NetCoreBackend*
