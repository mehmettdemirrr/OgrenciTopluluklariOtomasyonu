# Öğrenci Toplulukları Otomasyonu — Mimari Taslak

**Sürüm:** v6.15 · v1.0: 17 Ağustos 2026 (Faz 1-7, kararlar kapandı) · v2.0: 21 Ağustos 2026 (Faz 8-14
eklendi) · v3.0: 23 Ağustos 2026 (Faz 15-18 eklendi) · v4.0: 23 Ağustos 2026 (Faz 19-23 eklendi)
· v5.0: 25 Ağustos 2026 (Faz 24-29 eklendi) · v6.0: 26 Ağustos 2026 (Faz 30-34 eklendi) · v6.1: 26 Ağustos 2026 (A-67, Y-74 — onay kuyruklarında yönetici kapsamı) · v6.2: 27 Ağustos 2026 (A-68, Y-75, Faz 35 — kulüp içi yetki matrisi; A-61 ve Y-69 tadil edildi) · v6.3: 5 Eylül 2026 (K-40, A-69, Y-76, Faz 36 — kuruluş başvurusunda logo) · v6.4: 5 Eylül 2026 (K-41, A-70, Y-77, Faz 37 — danışmanın toplulukları) · v6.5: 5 Eylül 2026 (K-42, A-71, A-72, Y-78, Faz 38 — zengin duyuru içeriği) · v6.6: 5 Eylül 2026 (K-43, A-73, Y-79, Faz 39 — zengin etkinlik detayı) · v6.7: 5 Eylül 2026 (K-44, A-74, Y-80, Faz 40 — kulüp iletişimi ve sosyal medya) · v6.8: 5 Eylül 2026 (K-45, A-75, Y-81, Faz 41 — üye olmayanın kulüp görünürlüğü) · v6.9: 6 Eylül 2026 (K-46, A-76, A-77, Y-82, Faz 42 — etkinlik detay sayfası) · v6.10: 6 Eylül 2026 (K-47, A-78, Y-83, Faz 43 — form sayfaları) · v6.11: 6 Eylül 2026 (K-48, A-79, Y-84, Faz 44 — erişilebilirlik paneli) · v6.12: 6 Eylül 2026 (K-49, A-80, Y-85, Faz 45 — topluluğun birden çok kategorisi; K-35 ve A-60 tadil edildi) · v6.13: 6 Eylül 2026 (K-50, A-81, Y-86, Faz 46 — topluluk listesi sayfası) · v6.14: 6 Eylül 2026 (K-51, A-82, Y-87, Faz 47 — topluluk detay sayfası) · **v6.15: 10 Eylül 2026 (K-52, A-83, Y-88, Faz 48 — evrak şablonu ve OOXML tespiti)**
**Referans mimari:** [engindemirog/NetCoreBackend](https://github.com/engindemirog/NetCoreBackend)
**Uygulama planları:** [docs/PLAN-V2.md](PLAN-V2.md) (Faz 8-14) · [docs/PLAN-V3.md](PLAN-V3.md) (Faz 15-18)
· [docs/PLAN-V4.md](PLAN-V4.md) (Faz 19-23) · [docs/PLAN-V5.md](PLAN-V5.md) (Faz 24-29)
· [docs/PLAN-V6.md](PLAN-V6.md) (Faz 30-34) — gerekçe, sıra ve doğrulama adımları

Tek uygulama, beş katman, tek veritabanı. v1.0'ın 36 kararı, v2.0'ın 7 yeni kararı (A-37…A-43) verildi;
v3.0 dört yeni fazla (K-28, K-29) 4 karar (A-44…A-47) ve 2 kural (Y-59, Y-60) ekledi; v4.0 beş yeni fazla
(K-30) 7 karar (A-48…A-54) ve 4 kural (Y-61…Y-64) ekledi; v5.0 altı yeni fazla (K-31…K-34) 5 karar
(A-55…A-59) ve 4 kural (Y-65…Y-68) ekledi; v6.0 beş yeni fazla (K-35…K-39) 7 karar (A-60…A-66) ve
5 kural (Y-69…Y-73) ekledi; **v6.1** Faz 30'un elle doğrulamasında çıkan bir kör noktayı kapatarak
1 karar (A-67) ve 1 kural (Y-74) ekledi; **v6.2** üç seviyeli kulüp rol merdivenini kapalı bir yetki
matrisine çevirerek 1 karar (A-68) ve 1 kural (Y-75) ekledi ve **A-61 ile Y-69'u tadil etti**;
**v6.3** kuruluş başvurusuna logoyu ekleyerek 1 kapsam maddesi (K-40), 1 karar (A-69) ve 1 kural
(Y-76) ekledi; **v6.4** danışmanın topluluklarını "Kulüplerim" listesine ekleyerek 1 kapsam maddesi
(K-41), 1 karar (A-70) ve 1 kural (Y-77) ekledi; **v6.5** duyurulara biçimlendirilmiş içerik ve kapak
görseli ekleyerek 1 kapsam maddesi (K-42), 2 karar (A-71, A-72) ve 1 kural (Y-78) ekledi; **v6.6**
etkinlik detayına harita, zaman çizelgesi ve biçimlendirilmiş açıklama ekleyerek 1 kapsam maddesi
(K-43), 1 karar (A-73) ve 1 kural (Y-79) ekledi; **v6.7** topluluğa iletişim e-postası, telefonu ve
sosyal medya bağlantıları ekleyerek 1 kapsam maddesi (K-44), 1 karar (A-74) ve 1 kural (Y-80) ekledi;
**v6.8** üye olmayan öğrencinin kulüp sayfasında 403 yerine yayınlanmış etkinlikleri görmesini
sağlayarak 1 kapsam maddesi (K-45), 1 karar (A-75) ve 1 kural (Y-81) ekledi; **v6.9** etkinlik
detayını iki sütunlu düzene taşıyıp anonim ziyaretçiye açarak 1 kapsam maddesi (K-46), 2 karar
(A-76, A-77) ve 1 kural (Y-82) ekledi; **v6.10** etkinlik ve duyuru formlarını modaldan tam
sayfaya taşıyarak 1 kapsam maddesi (K-47), 1 karar (A-78) ve 1 kural (Y-83) ekledi; **v6.11**
her sayfaya erişilebilirlik tercihleri panelini ekleyerek 1 kapsam maddesi (K-48), 1 karar (A-79)
ve 1 kural (Y-84) ekledi; **v6.12** topluluğa birden çok kategori tanımlayarak 1 kapsam maddesi
(K-49), 1 karar (A-80) ve 1 kural (Y-85) ekledi ve **K-35 ile A-60'ı tadil etti**; **v6.13**
vitrin topluluk listesine üye/etkinlik sayısı ve katılım eylemi ekleyerek 1 kapsam maddesi
(K-50), 1 karar (A-81) ve 1 kural (Y-86) ekledi; **v6.14** vitrin topluluk detayını künye/eylem/
içerik sütunlarına ayırarak 1 kapsam maddesi (K-51), 1 karar (A-82) ve 1 kural (Y-87) ekledi;
**v6.15** evrak tipi kataloğuna indirilebilir şablon ekleyip OOXML tespitini paket açarak yapmaya
çevirerek 1 kapsam maddesi (K-52), 1 karar (A-83) ve 1 kural (Y-88) ekledi.
Yığın, kapsam ve kurallar sabit; bundan sonrası uygulama.

> **v6.2 neden bir tadil, iptal değil:** A-61'in kazancı ("yetki kontrolleri ek DB okuması yapmaz")
> korunuyor — yetki alanı `ClubMembership`'e denormalize edilmeye devam ediyor. Değişen tek şey o
> alanın **üç değerli bir enum yerine kapalı bir bayrak kümesi** olması. Y-69'un yasağı da duruyor:
> rol tanımı hâlâ izin kodu/claim taşıyamaz — yalnızca kodda tanımlı, sonlu bir kapasite kümesinden
> seçim yapar (A-68).

> **Bu belge tek doğruluk kaynağıdır.** Bir kural veya kapsam değişikliği gerekirse önce burası
> güncellenir, sonra kod. Aksi hâlde belge ile kod arasındaki fark sessizce büyür ve mimari testler
> tek başına kalır.

| | |
|---|---|
| Karar | 83 (36 v1.0 + 7 v2.0 + 4 v3.0 + 7 v4.0 + 5 v5.0 + 9 v6.0 + 1 v6.3 + 1 v6.4 + 2 v6.5 + 1 v6.6 + 1 v6.7 + 1 v6.8 + 2 v6.9 + 1 v6.10 + 1 v6.11 + 1 v6.12 + 1 v6.13 + 1 v6.14 + 1 v6.15) |
| Yasak kural | 88 (52 v1.0 + 6 v2.0 + 2 v3.0 + 4 v4.0 + 4 v5.0 + 7 v6.0 + 1 v6.3 + 1 v6.4 + 1 v6.5 + 1 v6.6 + 1 v6.7 + 1 v6.8 + 1 v6.9 + 1 v6.10 + 1 v6.11 + 1 v6.12 + 1 v6.13 + 1 v6.14 + 1 v6.15) |
| V1 dışı madde | 13 (K-13 v4.0'da **ikiye bölündü** — bkz. §3) |
| Uygulama fazı | 48 (7 v1.0 + 7 v2.0 + 4 v3.0 + 5 v4.0 + 6 v5.0 + 6 v6.0 + 1 v6.3 + 1 v6.4 + 1 v6.5 + 1 v6.6 + 1 v6.7 + 1 v6.8 + 1 v6.9 + 1 v6.10 + 1 v6.11 + 1 v6.12 + 1 v6.13 + 1 v6.14 + 1 v6.15) |

**Yığın:** .NET 8 LTS · ASP.NET Core Identity · EF Core 8 / MSSQL · Autofac + async AOP ·
FluentValidation · AutoMapper · Hangfire · Serilog → MSSQL · ClosedXML · React 18 + Vite + TypeScript + MUI

---

## İçindekiler

1. [Katmanlar ve bağımlılık yönü](#1-katmanlar-ve-bağımlılık-yönü)
2. [Açıkça yasak (Y-01 … Y-88)](#2-açıkça-yasak)
3. [V1 kapsamı (K-01 … K-52)](#3-v1-kapsamı)
4. [Teknoloji ve domain](#4-teknoloji-ve-domain)
5. [Uygulama sırası](#5-uygulama-sırası)
6. [Karar kaydı (A-01 … A-83)](#6-karar-kaydı)
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

Yetmiş dört kural. Tartışmaya kapalı. Bir kuralın gerçekten yanlış olduğunu düşünüyorsan kuralı
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

### V5 eklentileri (Faz 24-29)

| # | Yasak | Bunun yerine |
|---|---|---|
| **Y-65** | Hangfire köprü çerezini `/hangfire` dışında bir yola yazmak veya API kimlik doğrulamasında kabul etmek | Çerez `Path=/hangfire`, `HttpOnly`, `Secure`, `SameSite=Strict` ve access token ile aynı ömürlü (A-59). Gerekçe: çerez `/api/*`'a da giderse K-01'in "access token çerezde taşınmaz" kararı ve Y-48'in CSRF savunması sessizce delinir |
| **Y-66** | Kulüp kapsamı kontrol eden bir metodu yönetici kontrolü olmadan yazmak | Her `Ensure*Access*` metodu **ilk satırda** `clubs.manage.all` iznine bakar (A-55). Gerekçe: satır unutulunca yönetici kendi yönettiği sistemde sessizce kilitlenir ve hata mesajı ("danışmanı olmanız gerekir") sebebi söylemez. `ScopeGuardArchitectureTest` ihlali IL taramasıyla yakalar |
| **Y-67** | Öğrenci veya danışman rolü atanan kullanıcıyı domain profili (`Student` / `AcademicStaff`) olmadan oluşturmak | Rol seçimi profil alanlarını zorunlu kılar, eksikse `400` (A-57). Gerekçe: profilsiz kullanıcı giriş yapar ama hiçbir şey yapamaz; ne kullanıcı ne de onu oluşturan yönetici sebebi görebilir |
| **Y-68** | Demo veriyi üretim veritabanına yazmak veya geri bulunamayacak şekilde üretmek | `DemoDataSeeder` yalnızca açık konfigürasyon anahtarıyla (`Seed:Demo`) çalışır ve ürettiği kayıtları işaretler (A-58). Gerekçe: işaretsiz demo veri gerçek veriyle bir kez karıştığında ayrıştırılamaz |

### V6 eklentileri (Faz 30-34)

| # | Yasak | Bunun yerine |
|---|---|---|
| **Y-69** *(v6.2'de tadil edildi)* | Topluluk içi rol tanımına **izin kodu (string), claim veya `RoleClaim` referansı** bağlamak; arayüzden yeni bir yetki türü tanımlanabilir hâle getirmek | Tanım, kodda `[Flags] enum ClubCapability` olarak sabitlenmiş **kapalı** kümeden seçim yapar; tek bir `int` alanda taşınır (A-68). Gerekçe değişmedi — Y-37'nin kulüp karşılığı: Identity'nin yanına ikinci bir yetki *sistemi* açmak, izinlerin nereden geldiğini sorulamaz hâle getirir. Kapalı bir enum sistem değildir; yeni kapasite eklemek kod değişikliği + migration ister. Mimari test tipin `string`/koleksiyon bir izin alanı taşımadığını doğrulamaya devam eder. **v6.0-v6.1'deki hâli:** tanım yalnızca bir `ClubRole` seviyesine bağlanabiliyordu; Faz 35 bunu matrise genişletti |
| **Y-76** | Kuruluş başvurusunun logosunu, karar dalından **bağımsız** olarak bir kulübe taşımak — `Club.LogoFileId`'yi `DecideAsync`'in `Approved` dalı dışında yazmak | Atama yalnızca onay dalındadır; reddedilen ya da bekleyen başvurunun logosu hiçbir kulübe geçmez. Gerekçe: A-69 dosyayı **paylaşımlı** bıraktı (kopya yok, aynı `StoredFile`), bu yüzden "hangi kaydın hakkı var" sorusunun tek cevabı olmalı — koşulsuz kopyalayan bir satır, reddedilmiş bir başvurunun görselini yaşayan bir kulübün kimliği hâline getirir (K-40, A-69) |
| **Y-77** | Danışmanlıktan "ayrılınmak" — `Relationship == Advisor` satırında `DELETE /api/clubs/{id}/membership` çağırmak veya arayüzde "Ayrıl" düğmesi göstermek | Danışman bir üye değildir; danışmanlığı sonlandırmanın yolu kulüp düzenleme akışının `AdvisorId` alanını değiştirmektir, üyelikten çıkma akışı değil. Gerekçe: bu uç `ClubMembership` satırı arar — danışmanlık satırının arkasında öyle bir satır yoktur; çağrı 404 yerine yanlış bir üyeliği silme riski taşımaz ama arayüzün düğmeyi göstermesi bile "buradan çıkılabilir" yanlış izlenimini verir (K-41, A-70) |
| **Y-78** | Zengin metin içeriğini izin listesinde **olmayan** bir düğüm/işaret/öznitelik/renk token'ıyla kaydetmek; reddedilen içeriği sessizce temizleyip yine de kaydetmek; arayüzde `dangerouslySetInnerHTML` veya HTML ayrıştıran bir render yolu açmak | `ContentJson` yazılmadan önce sunucu ağacı `RichTextSchema`'ya göre dolaşır (`RichTextDocumentValidator`); izin listesinde olmayan herhangi bir şey **isteği reddeder** — sessizce filtrelemek değil, 400 dönmek. Gerekçe: sessiz temizleme, "neden biçimlendirmem gitti" diye sormayan bir kullanıcıya güvenmek demektir; fail-closed, kullanıcıya söyler. Arayüz tarafında ağaç gezilir, React öğesi üretilir — HTML dizesi hiç oluşmaz, dolayısıyla sanitize edilecek bir yer de yoktur. İhlali `Architecture.Tests` kaynak taramasıyla yakalar (K-42, A-71, A-72) |
| **Y-79** | Geri sayım/kalan süre göstergesini bir **karar** yerine geçirmek — "tarayıcıda tarih geçmiş görünüyor" diye katılım düğmesini açıp kapatmak, kontenjan veya durum kararını istemci saatinden vermek | Kalan süre yalnızca bilgilendirir; katılım açıklığı, etkinlik durumu ve kontenjan kararları her zaman backend'in döndürdüğü alanlardan (`EventListItemDto.Status`, katılım ucunun kendi kararı) okunur. Gerekçe: istemci saati kullanıcı tarafından değiştirilebilir — bir görüntü öğesi onun üstüne bina edilemez (K-43) |
| **Y-80** | Sosyal medya bağlantısını `http://` şemasıyla veya platformun bilinen alan adı dışında bir adrese kaydetmek; `javascript:` gibi bir şemayı kabul etmek | `ClubSocialLink.Url` sunucu tarafında doğrulanır: şema **daima** `https`, ana bilgisayar adı o platformun bilinen alan adları listesindedir (`Website` platformu hariç — kulübün kendi alan adı serbesttir, şema kısıtı yeterlidir). İstemci tarafı doğrulama yalnızca kolaylıktır; kapı `SetClubSocialLinksRequestValidator`'dadır (K-44, A-74) |
| **Y-81** | Yetkisiz kullanıcının kulüp etkinlik sekmesini yönetim ucundan (`GET /api/clubs/{id}/events`) okutmak — o kulüpte `EventsManage` kapasitesi yokken bu uca düşmek | Arayüz `ClubDetailDto.myCapabilities`'e bakar: `EventsManage` yoksa yayınlanmış etkinlik ucu (`GET /api/events?clubId=`) çağrılır, yönetim ucu hiç çağrılmaz. Uçların kendi 403'leri **kaldırılmaz** — bu kural yalnızca arayüzün doğru ucu seçmesini garanti eder, ikinci bir yetki kapısı açmaz (K-45, A-75) |
| **Y-82** | Anonim etkinlik detay ucundan taslak/onay bekleyen/reddedilen ya da `ClubMembers` kitleli bir etkinliği döndürmek; "bulunamadı" yerine 403 dönerek varlığını sızdırmak | `PublicContentManager.GetEventByIdAsync` vitrin listesiyle **aynı** filtreyi uygular (`Status == Published && Audience == Public`); eşleşmeyen her istek `NotFound` döner. Gerekçe: Y-58 ve Y-72'nin detay ucundaki karşılığı — liste sızdırmıyorken detayın sızdırması, filtrenin iki yerde ayrı yazılmasının klasik sonucudur. `PublicSurfaceLeakTests` bu ucu da tarar (K-46, A-76) |
| **Y-83** | Etkinlik/duyuru form alanlarını bir `Dialog` içinde (yeniden) kurmak — zengin metin editörünü modalda açmak | Form alanları yalnızca form sayfası bileşenlerinde tanımlanır; modal, onay sorularına (`ConfirmDialog`, "İptal gerekçesi") ayrılmıştır. Gerekçe: aynı formun iki kopyası er ya da geç ayrışır — biri `descriptionJson`'a geçerken öbürü düz metinde kalır. İhlali `Architecture.Tests` kaynak taramasıyla yakalar: `arayuz/src/pages` altında hem `<Dialog` hem `RichTextEditor` geçen bir dosya olamaz (K-47, A-78) |
| **Y-84** | Erişilebilirlik tercihini bir bileşenin içinde okuyup elle stil uygulamak (`if (fontScale === 130) …`, `style={{ fontSize: … }}`); aynı ayarı panelin dışında ikinci bir yerde tanımlamak | Tercih yalnızca `AccessibilityProvider` ve `createAppTheme` tarafından okunur; bileşenler sonucu temadan alır. Gerekçe: ayarı okuyan her bileşen, ayar değiştiğinde güncellenmesi gereken yeni bir yerdir — bir tanesi unutulduğunda kullanıcı "bazı yerler büyüdü, bazıları büyümedi" ile kalır. İhlali `Architecture.Tests` kaynak taramasıyla yakalar: `useAccessibility` yalnızca `arayuz/src/a11y/` altında ve tema sağlayıcısında geçebilir (K-48, A-79) |
| **Y-85** | Kulüp kategorilerini satır başına sorguyla okumak veya kategori filtresini belleğe çekip `Where` ile uygulamak; `Club` üzerine ikinci bir kategori kolonu geri getirmek | Kategoriler sayfa başına **tek** toplu sorguyla okunur (`GetNamesByClubAsync`), filtre önce kategorideki kulüp id'lerini SQL'de bulur (`GetClubIdsByCategoryAsync`), sonra mevcut sayfalama sorgusuna `ids.Contains(c.Id)` olarak girer. Gerekçe: Y-10'un ve `FillCategoryNamesAsync`'in kategori karşılığı — 12 kulüplük bir sayfa 12 sorgu açmaz; kategori tek kaynakta (bağ tablosu) durur (K-49, A-80) |
| **Y-86** | Vitrin kartındaki "Katıl" düğmesini arayüzde uygunluk kararına bağlamak — öğrenci mi, kulüp aktif mi, dönem açık mı, zaten üye mi diye bakıp düğmeyi gizlemek/pasifleştirmek | Düğme yalnızca üyelik başvurusu ucunu çağırır; kararı ve mesajı sunucu döndürür (`NotAStudent`, `ClubNotActive`, `AlreadyClubMember`, `DuplicatePendingApplication`, `NoCurrentAcademicTerm`). Gerekçe: Y-35'in kart karşılığı — arayüzde verilen "uygun değilsin" kararı, kuralın ikinci bir kopyasıdır ve sunucudaki kural değişince sessizce yanlışa döner; ayrıca kullanıcı **neden** olmadığını öğrenemez (K-50, A-81) |
| **Y-87** | Kuruluş yılını `Club.CreatedAtUtc`'den (kaydın sisteme girildiği tarih) türetip "Kuruluş: 2026" diye basmak; alan boşken yıl uydurmak | Kuruluş yılı ayrı ve isteğe bağlı bir alandır (`Club.FoundedYear`, nullable); **boşsa rozet hiç çizilmez**. Gerekçe: 2012'de kurulmuş bir topluluk sisteme 2026'da girildiyse `CreatedAtUtc` 2026'dır — türetilen değer sessizce yanlış bir kurumsal bilgidir ve kullanıcı bunu doğru sanır (K-51, A-82) |
| **Y-88** | Sıkıştırılmış bir kapsayıcının (ZIP/OOXML) tipini ham baytta dize arayarak doğrulamak; ZIP sihirli baytını tek başına Docx saymak | Kapsayıcı `ZipArchive` ile açılır, girdi adı **ve** içerik tipi birlikte doğrulanır (A-83). Gerekçe: sıkıştırılmış içerikteki bir dize ham baytlarda bulunmaz — böyle bir kontrol üretimde **her zaman** yanlış negatif verir ve sahte içerikle yazılmış bir test bunu gizler. Regresyon, gerçek bir OOXML paketiyle (test içinde `ZipArchive` ile üretilmiş, sıkıştırılmış) kilitlenir; ham baytta işaretin bulunmadığı testte açıkça doğrulanır (K-52, A-83, Y-40) |
| **Y-75** | Kulüp içi kapasiteyi, kullanıcının Identity izninin **vermediği** bir şeyi verecek şekilde kullanmak; uç noktadan `[SecuredOperation]`'ı kaldırıp kararı kulüp matrisine bırakmak | Kulüp matrisi yalnızca **daraltır**, asla genişletmez. Uçtaki `[SecuredOperation(events.write)]` birinci kapı olarak yerinde kalır; kulüp kapasitesi ikinci kapıdır. Bir kulüp rolüne "etkinlik yönet" işaretlemek, Identity rolünde `events.write` olmayan birine bu hakkı **veremez** (A-68). Gerekçe: Y-37 ancak izinlerin tek kaynağı Identity kalırsa ayakta durur — matris bir *filtre*, bir *kaynak* değil. Mimari test kapasite okuyan her metodun aynı zamanda `[SecuredOperation]` taşıyan bir uçtan çağrıldığını doğrular |
| **Y-70** | Başvuru evrakını `Public` görünürlükle kaydetmek veya anonim dosya ucundan servis etmek; indirmede yetkiyi yeniden kontrol etmemek | Evrak daima `FileVisibility.Protected`; erişim yalnızca `GET /api/club-applications/{id}/documents/{documentId}` ucundan, **indirme anında** yeniden kontrol edilen yetkiyle (A-63). Y-52'nin ve Y-51'in evrak karşılığı — adli sicil ve kurucu üye dilekçesi kişisel veridir, K-19 (KVKK akışı) hâlâ V1 dışıdır |
| **Y-71** | Zorunlu evrak bütünlüğünü yalnızca arayüzde kontrol etmek veya FluentValidation kuralına gömmek | Kontrol `ClubApplicationManager` içinde, evrak tipi katalogunu okuyarak (A-62). Gerekçe: "hangi evrak zorunlu" cevabı veritabanındadır — biçimsel doğrulama değil iş kuralıdır (Y-03, Y-35) |
| **Y-72** | Kitle alanı olmadan etkinlik kaydetmek; anonim vitrin ucunun `ClubMembers` kitleli etkinliği döndürmesi; kayıt anında üyelik kontrolünü atlamak | `Event.Audience` yazma anında zorunlu; anonim uç yalnızca `Audience == Public` döner; `RegisterAsync` `ClubMembers` etkinlikte **güncel dönem** üyeliği arar (A-65). Y-57'nin etkinlik karşılığı — duyuru ve etkinlik aynı soruya aynı cevabı verir |
| **Y-73** | Başvuru penceresi kontrolünü yalnızca arayüzde yapmak; kapalı olduğunu söylerken sebebini ve ne zaman açılacağını söylememek; pencere kararını iki ayrı yerde hesaplamak | Muhafız `SubmitAsync`'in içinde (Y-35: düğmeyi gizlemek yetki değildir); mesaj sebebi söyler ve takvimin nerede görüleceğini gösterir; hem muhafız hem okuma ucu **aynı** değerlendirme metodunu çağırır (A-66). Gerekçe: Y-66'nın öğrettiği ders — sebebi söylemeyen bir kapı, kullanıcıyı da yöneticiyi de kör bırakır. İkinci bir `if`, ekranın "açık" derken API'nin "kapalı" demesinin garantili yoludur |
| **Y-74** | **Çağıranı danışmana çözen** (`AcademicStaff.ApplicationUserId == currentUser.UserId`) bir metodu `clubs.manage.all` yolu olmadan yazmak — metodun adı `Ensure*` olsun ya da olmasın | Kapsam daraltmanın yapısal imzası **"ben kimim"** sorusudur, metodun adı değil. `s.Id == request.AdvisorId` (verilen danışman var mı) zararsız doğrulamadır ve bu kuralın dışındadır. Gerekçe: Y-66 aynı şeyi söylüyordu ama `ScopeGuardTests` metotları **ada göre** (`Ensure*Access`) tarıyordu; kapsamı bir sorgunun içine gömen 4 metot testin kör noktasında kaldı ve yönetici hem etkinlik hem üyelik onay kuyruğunu **sessizce boş** gördü (A-67). Test artık IL'de `AcademicStaff::get_ApplicationUserId` referansını arar — adlandırmaya bağlı değildir |

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

### V5'e alınanlar (Faz 24-29)

| # | Özellik | Ne var | Getirdiği iş |
|---|---|---|---|
| **K-31** | **Yönetici kapsamı** | Yönetici, danışmanı olmadığı kulüplerde de etkinlik/duyuru/üye rolü/logo işlemi yapabilir | `clubs.manage.all` izni, 7 `Ensure*Access` metodunun tamamında ilk kontrol; mevcut `reports.read.all` bypass'ları buraya taşınır (A-55, Y-66) |
| **K-32** | **Kişi kimliği ve kullanıcı yönetimi** | Ad soyad; kullanıcı oluşturma/pasife alma/silme arayüzü; rol seçimine göre domain profili | `ApplicationUser.FirstName/LastName`, `DELETE /api/users/{id}`, `POST /users` profil de üretir (A-56, A-57, Y-67) |
| **K-33** | **Danışman yönetimi** | Akademik personel oluşturulabilir/düzenlenebilir; kulübün danışmanı değiştirilebilir | `AcademicStaff` CRUD uçları, `UpdateClubRequestDto.AdvisorId` |
| **K-34** | **Kurumsal referans verisi ve demo veri** | 19 fakülte / 121 bölüm; kulüp-etkinlik-duyuru-üyelik demo verisi | Referans `HasData` ile kalıcı, demo `DemoDataSeeder` ile config kapılı (A-58, Y-68) |

> **K-01 notu (v5.0):** access token bellekte kalmaya devam eder, ama uygulama açılışında **bir kez**
> sessiz refresh denenir (A-59). Bu, K-01'i gevşetmez: token yine hiçbir zaman kalıcı depoya yazılmaz;
> yalnızca zaten var olan httpOnly refresh çerezi kullanılır.

> **K-14 notu (v5.0):** Hangfire panelinin token köprüsü query string'den çereze taşınır (A-59, Y-65) —
> panelin kendi CSS/JS istekleri query parametresi taşımadığı için bugün 401 alıyor ve panel stilsiz açılıyor.

### V6'ya alınanlar (Faz 30-34)

Ayrıntılı gerekçe, uçlar ve testler için [docs/PLAN-V6.md](PLAN-V6.md).

| # | Özellik | Ne var | Getirdiği iş |
|---|---|---|---|
| **K-35** *(v6.12'de tadil edildi)* | **Topluluk kategorisi** | Kategori referans verisi; kulüp **bir veya birden çok** kategori taşır, kuruluş başvurusu tek kategori önerir, listeler kategoriye göre filtrelenir | `ClubCategory`, **`ClubCategoryAssignment` (ClubId + ClubCategoryId)**, `ClubApplication.ProposedCategoryId` (nullable, tekil kalır), cache geçersizleştirme. **v6.0-v6.11'deki hâli:** `Club.ClubCategoryId` tek ve nullable bir kolondu (A-60, A-80, Y-45, Y-85) |
| **K-36** | **Dinamik topluluk içi roller** | Kulüp kendi unvanlarını tanımlar ("Sayman", "Sekreter") ve her unvanın kulüp içinde **hangi işlemleri** yapabileceğini seçer | `ClubRoleDefinition` (kulübe özel), `ClubMembership.ClubRoleDefinitionId`. **Faz 34:** unvan katmanı, yetki üç seviyeli. **Faz 35:** yetki altı kapasiteli kapalı matrise genişledi; `ClubRole` makam olarak kaldı (A-61, A-68, Y-69, Y-75) |
| **K-37** | **Topluluk kuruluş evrakları** | Admin evrak tipi kataloğu tanımlar (8 gerçek MTÜ formu seed'li); başvuru evraklarla birlikte tek istekte gider; inceleyici her evrağı açar | `ClubDocumentType` + `ClubApplicationDocument`, multipart başvuru ucu, Core'a PDF imzası, korumalı indirme ucu, 90 günlük saklama (A-62, A-63, A-64, Y-70, Y-71) |
| **K-38** | **Etkinlik katılım kitlesi** | Etkinlik "herkese açık" ya da "sadece topluluk üyelerine"; ikincisi anonim vitrinde görünmez | `Event.Audience`, `RegisterAsync`'te güncel dönem üyelik kontrolü, vitrin filtresi (A-65, Y-72) |
| **K-39** | **Topluluk kurma başvuru takvimi** | Başvurular admin'in belirlediği tarihler arasında açık; admin ayrıca elle açıp kapatabilir | `AcademicTerm`'e üç alan, tek değerlendirme metodu, `SubmitAsync` muhafızı, durum okuma ucu (A-66, Y-73) |
| **K-40** | **Kuruluş başvurusunda logo** | Öğrenci, topluluk kurma başvurusunda önerilen logoyu evraklarla aynı istekte yükler; zorunlu değildir, başvuru logosuz da geçerlidir | `ClubApplication.LogoFileId` (nullable), `IFileService.StoreApplicationLogoAsync`, multipart forma `Logo` alanı, onayda `Club.LogoFileId`'ye devir (A-69, Y-76) |
| **K-41** | **Danışmanın toplulukları** | Akademik danışman, "Kulüplerim" sayfasında danışmanı olduğu toplulukları görür — öğrenci üyelikleriyle aynı listede, ilişkisi ayırt edilerek | `GET /api/clubs/mine` iki dalda üretir: üyelik satırları (dönemsel, §22.3) ve danışmanlık satırları (dönemsel değil, `Club.AdvisorId`); `MyClubMembershipDto.Relationship` ayrımı taşır (A-70, Y-77) |
| **K-42** | **Zengin duyuru** | Duyuru metni araç çubuğundan biçimlendirilebilir (kalın, italik, altı çizili, üstü çizili, başlık, madde/numara listesi, hizalama, bağlantı, renk) ve duyuruya bir kapak görseli eklenebilir; ikisi de isteğe bağlıdır | `Announcement.ContentJson` (düğüm ağacı) + `ImageFileId`; sunucu tarafı kapalı izin listesi, düz metin aynası `Content`'te kalır (A-71, A-72, Y-78) |
| **K-43** | **Zengin etkinlik detayı** | Etkinlik detay sayfası afiş, biçimlendirilmiş açıklama, konum haritası ve yol tarifi bağlantısı ile başlangıç/bitiş ve kalan süreyi gösteren bir zaman çizelgesi taşır | `Event.DescriptionJson` (A-71'in aynı altyapısı), gömülü harita iframe'i, istemci tarafı geri sayım (A-73, Y-79) |
| **K-44** | **Topluluk iletişimi** | Topluluk kendi iletişim e-postasını, telefonunu ve sosyal medya hesaplarını tanımlar; bunlar kulüp sayfasında herkese görünür. Tümü isteğe bağlıdır | `Club.ContactEmail`/`ContactPhone`, `ClubSocialLink` (satır başına platform), `PUT /api/clubs/{id}/contact` (A-74, Y-80) |
| **K-45** | **Üye olmayanın kulüp görünürlüğü** | Giriş yapmış ama kulübe üye olmayan öğrenci, kulübün genel bilgilerini ve YAYINLANMIŞ etkinliklerini görür; üye listesini ve rol tanımlarını görmez. Taslak/onay bekleyen etkinlikler yalnızca yetkililere görünür | `ClubDetailDto.MyRelationship`/`MyCapabilities`, arayüz sekmeleri buradan çizilir, etkinlik sekmesi yetkisizken `GET /api/events?clubId=` ucuna düşer (A-75, Y-81) |
| **K-46** | **Etkinlik detay sayfası** | Etkinlik detayı iki sütundur: solda afiş ve biçimlendirilmiş açıklama, sağda düzenleyen topluluk, zaman çizelgesi, konum + yol tarifi, görüntülenme/katılımcı/kontenjan sayıları ve birincil eylem. Anonim ziyaretçi de yayınlanmış ve herkese açık etkinliğin detayını görür; katılmak için giriş yapması istenir | `EventDetailLayout` (tek düzen, iki kabuk), `GET /api/public/events/{id}`, `EventListItemDto.ParticipantCount`/`ClubLogoFileId`, `Event.ViewCount` (A-76, A-77, Y-82) |
| **K-47** | **Form sayfaları** | Etkinlik ve duyuru oluşturma/düzenleme kendi adresinde tam sayfadır (`/events/new`, `/events/{id}/edit`, `/announcements/new`, `/announcements/{id}/edit`); modal yalnızca onay sorularına kalır. Form, geldiği ekrana geri döner ve kaydedilmemiş değişiklikle çıkarken uyarır | Tek `EventFormPage`/`AnnouncementFormPage` iki kipte (oluştur/düzenle), `clubId` ve `returnTo` sorgu parametreleri (A-78, Y-83) |
| **K-48** | **Erişilebilirlik tercihleri** | Her sayfada sol altta bir erişilebilirlik düğmesi vardır; panelden yazı boyutu (%100/%115/%130), yüksek kontrast, hareketi azalt ve bağlantıların altını çiz ayarlanır, tek düğmeyle sıfırlanır. Klavye kullanıcısı için sayfanın ilk odağı "içeriğe atla" bağlantısıdır. Tercihler tarayıcıda saklanır, sunucuya gitmez | `AccessibilityProvider` + `createAppTheme(mode, locale, a11y)`, `SkipToContentLink`, `localStorage` (A-79, Y-84) |
| **K-49** | **Topluluğun birden çok kategorisi** | Bir topluluk en fazla **3** kategori taşır; vitrin kartında ve detayda hepsi rozet olarak görünür. Kategori filtresi, kulübün kategorilerinden **herhangi biri** eşleşince kulübü listeler | `ClubCategoryAssignment`, `IClubCategoryAssignmentDal` (iki toplu sorgu), `Update/CreateClubRequestDto.ClubCategoryIds` (A-80, Y-85) |
| **K-50** | **Topluluk listesi kartı** | Vitrin kartı logoyu, kategori rozetlerini, **üye ve etkinlik sayısını**, kısa açıklamayı ve iki eylemi taşır: giriş yapmamışa "Giriş Yap ve Katıl", giriş yapmışa "Katıl" ve her ikisinde "İncele". Kulübe soru sorma özelliği kapsam dışıdır | `PublicClubListItemDto.MemberCount`/`EventCount`, `IClubStatsDal` (tek toplu sorgu), `POST /api/clubs/{id}/membership-applications` (mevcut uç) (A-81, Y-86) |
| **K-51** | **Topluluk detay sayfası** | Vitrin detayı daire logolu kapak, künye rozetleri (kuruluş yılı, üye, görüntülenme, etkinlik) ve iki sütun taşır: solda topluluk durumu/giriş, iletişim & sosyal, paylaşım (QR); sağda "Hakkımızda", etkinlikler ve duyurular. Kulüp yöneticilerine mesaj gönderme kapsam dışıdır | `Club.FoundedYear`/`ViewCount`, `PublicClubDetailDto` künye alanları, `POST /api/public/clubs/{id}/view`, `SocialPlatform.Facebook` (A-82, Y-87) |
| **K-52** | **Evrak şablonu** | Admin, evrak tipi kataloğundaki her tipe boş kurumsal formu (Word veya PDF) yükler; başvuru sahibi formu başvuru sayfasından indirir. Şablon boş formdur, doldurulmuş evrak değildir | `ClubDocumentType.TemplateFileId`, `POST/GET /api/club-document-types/{id}/template`, `IFileService.StoreDocumentTemplateAsync` (A-83, Y-88, A-64) |

> **K-29 notu (v6.0):** topluluk kurma başvurusu V6'da iki yerden birden büyüyor — önüne bir **kapı**
> (K-39 takvimi), içine **evrak** (K-37) geliyor. Onay akışının kendisi değişmiyor: tek aşamalı admin
> onayı korunuyor, çok aşamalı (danışman → kurul → admin) onay V6 kapsamına **alınmadı.**

> **K-21 notu (v6.0):** "üye rolleri" K-36 ile ikiye ayrışıyor — görünen **unvan** ve karar veren
> **yetki seviyesi**. A-39'un başkan tekilliği ve `ClubRole` enum'ı olduğu gibi kalır; unvan katmanı
> yetki yüzeyine dokunmaz.

> **K-05 notu (v6.0):** dosya yükleme ilk kez görsel dışına çıkıyor (PDF). İzin verilen tip kümesi
> artık **çağrı yerine göre** belirlenir: logo/afiş görsel, evrak yalnızca PDF (A-64). Tek ortak
> `StoreFileAsync` yolu bu parametre olmadan logo ucunu da PDF'e açardı.

### V1 dışında kalanlar — bilinçli kararlar

Bunlar eksik değil, ertelenmiş özellikler. "Kapı" sütunu, bugün ne yapmamız gerektiğini söyler ki
sonradan eklemek pahalı olmasın. **Bu tabloyu yalnızca v4.0 değiştirdi ve o da yalnızca K-13'ü ikiye
böldü** (rate limiting kapsama girdi, API versiyonlama kaldı); v2.0, v3.0, v5.0 ve v6.0 hiç
dokunmadı. Geri kalan her şey — aidat/ödeme (K-15), forum/anket/QR (K-16), gerçek zamanlı bildirim
(K-04), KVKK silme/anonimleştirme akışının kendisi (K-19) — hiçbir fazda yapılmaz.

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
| `ApplicationUser` | Identity kullanıcısı, `int` anahtarlı, PBKDF2 parola. **`FirstName`/`LastName` (nullable)** — anonim uçlardan dönmez | A-09, A-11, A-28, A-56, Y-58 |
| `ApplicationRole` · `RoleClaim` | Roller ve ince taneli izinler | A-10, K-17, Y-37 |
| `RefreshToken` | Tek kullanımlık, iptal edilebilir, çerezde taşınır | K-01, A-30, Y-38 |
| `Student` · `AcademicStaff` | 1-1 profil tabloları | A-14 |
| `Faculty` · `Department` | Referans verisi, hard delete serbest | A-12, A-27 |
| `ClubCategory` | Topluluk kategorisi; referans verisi, kullanımdaysa silinemez (409) | K-35, A-60 |
| `AcademicTerm` | Akademik dönem. **Topluluk kurma başvuru penceresini de taşır** (iki tarih + üç durumlu geçersiz kılma) | A-13, K-39, A-66, Y-73 |
| `Club` | Topluluk, danışmanı, logosu, durumu, **kategorisi** (nullable) | K-05, A-31, K-35, A-60 |
| `ClubMembership` | Dönemsel üyelik + topluluk rolü; `(ClubId, StudentId, TermId)` unique. Dönem devrinde rolüyle birlikte yeni döneme kopyalanır. `ClubRoleDefinitionId` (nullable) görünen unvanı, **`Capabilities` yetki matrisini** taşır; `ClubRole` makam olarak kalır (A-39) | A-13, A-15, K-30, A-51, K-36, A-61, A-68 |
| `ClubRoleDefinition` | Kulübe özel rol **unvanı** → `ClubCapability` bayrak kümesi + `ClubRole` makamı; `(ClubId, Name)` unique. String izin kodu/claim taşımaz | K-36, A-61, A-68, Y-69 |
| `MembershipApplication` | Başvuru, onay/ret, soft delete, bildirim tetikler | A-12, K-03 |
| `Event` | Taslak → onay bekliyor → yayında/reddedildi → **iptal edildi** (gerekçesiyle); kontenjan `rowversion`; **katılım kitlesi** (herkese açık / üyelere özel) yazma anında zorunlu | A-25, A-15, K-05, A-49, Y-61, K-38, A-65, Y-72 |
| `EventParticipation` | `(EventId, StudentId)` unique | A-15 |
| `Announcement` | Topluluk duyurusu; `ClubId` nullable (sistem duyurusu) + **görünürlük** (üye/herkese açık) | K-23, A-43, Y-57 |
| `AuditLog` | Kim, ne zaman, hangi alan; interceptor yazar; **`CorrelationId`** (nullable) trafik logu satırına bağlar | K-12, A-33, Y-44, K-28 |
| `StoredFile` | Üretilen ad, tip, boyut, sahibi ve **görünürlük** (açık/korumalı) | K-05, A-31, A-36, Y-52 |
| `ReportRequest` | Rapor talebi: tür, parametreler, durum (kuyrukta → üretiliyor → hazır/hatalı), üretilen dosya | K-06, A-35, Y-51 |
| `ClubApplication` | Topluluk kurma başvurusu: ad/açıklama/gerekçe/önerilen danışman, **önerilen kategori** (nullable), onay/ret, soft delete; onayda `CreatedClubId` yazılır | K-29, A-45, K-35 |
| `ClubDocumentType` | Kuruluş evrakı tipi: kod (`FR-0230`), ad, **zorunlu mu**, aktif mi, sıra. 8 gerçek MTÜ formu `HasData` ile | K-37, A-62, A-58 |
| `ClubApplicationDocument` | Başvuruya yüklenen evrak; `(ClubApplicationId, ClubDocumentTypeId)` unique. Dosya daima `Protected` | K-37, A-63, Y-70 |
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

### 24 — Oturum sürekliliği ve Hangfire paneli
Açılışta bir kez sessiz refresh (`bootstrapping` durumu); Hangfire token'ı `/hangfire` kapsamlı çerezden okunur (A-59, Y-65).
**Bitti sayılır:** F5 sonrası oturum açık kalıyor; panel stilli açılıyor ve iç sayfalarında 401 alınmıyor; çerez `/api/*`'a gitmiyor.

### 25 — Yönetici kapsamı
`clubs.manage.all` izni 7 kapsam metodunun tamamına girer (K-31, A-55, Y-66); mimari test unutmayı yakalar.
**Bitti sayılır:** admin kulüp etkinliği/duyurusu/üye rolü/logosunu yönetebiliyor; izni olmayan aynı uçlarda 403.

### 26 — Kişi kimliği ve kullanıcı yönetimi
Ad soyad eklenir; kullanıcı oluşturma rol'e göre domain profili de üretir; pasife alma ve korumalı silme arayüze gelir (K-32, A-56, A-57, Y-67).
**Bitti sayılır:** `Member` rolüyle oluşturulan kullanıcı gerçekten kulübe başvurabiliyor; bağlı kaydı olan kullanıcı silinemiyor (409); anonim uçlarda ad soyad geçmiyor.

### 27 — Danışman yönetimi
`AcademicStaff` CRUD ve kulübün danışmanının değiştirilmesi (K-33).
**Bitti sayılır:** yeni danışman oluşturulup kulübe atanabiliyor; eski danışman o kulüpte artık işlem yapamıyor.

### 28 — Kurumsal referans verisi ve demo veri
19 fakülte / 121 bölüm `HasData` ile; kulüp/etkinlik/duyuru/üyelik demo verisi config kapılı seeder ile (K-34, A-58, Y-68).
**Bitti sayılır:** bölüm seçicide 121 bölüm aranabiliyor; seeder ikinci çalıştırmada sıfır satır üretiyor; `Seed:Demo` kapalıyken hiç demo kayıt yok.

### 29 — Yetki matrisi ikiye ayrılır
`/authorization/roles` ve `/authorization/users` ayrı rotalar olur.
**Bitti sayılır:** iki rota ayrı açılıyor, eski `/authorization` linki yönleniyor, menü hâlâ kaydırmasız sığıyor.

---

### v6.0 — Faz 30-34

Ayrıntılı gerekçe, uçlar ve testler için [docs/PLAN-V6.md](PLAN-V6.md). Sıra **risk artan** yönde:
en izole olan önce, yetki yüzeyine en yakın olan en sonda.

### 30 — Etkinlik katılım kitlesi
`Event.Audience` (herkese açık / üyelere özel); kayıtta güncel dönem üyelik kontrolü, vitrinde filtre (K-38, A-65, Y-72).
**Bitti sayılır:** üye olmayan öğrenci üyelere özel etkinliğe kaydolamıyor; o etkinlik anonim vitrinde görünmüyor; herkese açık etkinliklerde hiçbir davranış değişmemiş.

### 31 — Topluluk kurma başvuru takvimi
`AcademicTerm`'e pencere alanları; `SubmitAsync` muhafızı ve durum okuma ucu **aynı** metodu çağırır (K-39, A-66, Y-73).
**Bitti sayılır:** aralık dışında başvuru 409 alıyor ve mesaj sebebi söylüyor; "zorla aç"/"zorla kapat" takvimi geçersiz kılıyor; arayüzün gösterdiği durum ile API'nin kararı beş senaryoda da aynı.

### 32 — Topluluk kategorisi
Referans verisi deseninin dördüncü uygulaması; kulüp ve başvuruda seçim, listelerde sunucu taraflı filtre (K-35, A-60).
**Bitti sayılır:** kategori tanımlanıp seçilebiliyor, liste filtreleniyor, kullanımdaki kategori silinemiyor, ad değişince cache düşüyor (Y-45).

### 33 — Topluluk kuruluş evrakları
Core'a PDF imzası ve tip kümesi parametresi; evrak tipi kataloğu; multipart başvuru; korumalı indirme; 90 günlük saklama (K-37, A-62, A-63, A-64, Y-70, Y-71).
**Bitti sayılır:** sekiz zorunlu evrak tek düğmeyle yükleniyor; eksik evrakla başvuru reddediliyor ve eksik olan söyleniyor; inceleyici her evrağı açabiliyor; aynı evrak anonim uçtan indirilemiyor; kulüp logosuna PDF yüklenemiyor.

### 34 — Dinamik topluluk içi roller
`ClubRoleDefinition` unvan katmanı; `ClubRole` enum'ı yetki seviyesi olarak yerinde kalır (K-36, A-61, Y-69).
**Bitti sayılır:** başkan kendi kulübüne unvan tanımlayıp atayabiliyor; unvan doğru yetki seviyesini veriyor; kulüpler birbirinin unvanını görmüyor; **V5'ten gelen tüm yetki testleri değişmeden yeşil.**

### 35 — Kulüp içi yetki matrisi
Üç seviyeli merdiven `ClubCapability` bayrak kümesine genişler; `ClubRole` makam olarak kalır (K-36, A-68, Y-75; A-61 ve Y-69 tadil edildi).
**Bitti sayılır:** yeni unvan tanımlanırken altı kapasite tek tek seçilebiliyor; kapasitesi kısılan üye o işlemi **yapamıyor**, açılan yapabiliyor; matris Identity izninin vermediğini veremiyor (Y-75 mimari testi); unvansız üyelerin davranışı Faz 34'teki gibi kalıyor; A-39 başkan tekilliği ve dönem devri değişmeden çalışıyor.

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
| **A-55** | Yönetici kapsamı ayrı bir izindir | A | `clubs.manage.all`; Admin rolüne verilir, 7 kapsam metodunun ilk kontrolü olur. `reports.read.all`'ın fiilen "admin mi" bayrağı olarak kullanılması **sona erer** — o izin yalnızca `ReportScopeResolver`'da rapor kapsamı için kalır (K-31, Y-66) |
| **A-56** | Kişi adları ve anonim yüzey sınırı | A | `ApplicationUser.FirstName/LastName` (nullable). Kayıt, admin kullanıcı oluşturma, profil ve **danışman seçici** ad soyad gösterir; `/api/public/*` uçlarından **asla** dönmez (Y-58). Boşsa arayüz e-postaya düşer — mevcut hesaplar geçersiz duruma düşmez (K-32) |
| **A-57** | Kullanıcı silme sözleşmesi | B | Pasife alma = mevcut `lockout` (geri alınabilir). Silme kalıcıdır ama bağlı kayıt (üyelik, etkinlik kaydı, başvuru, danışmanlık) varsa **409** ve sebep mesajda. Kendini silemez/kilitleyemez. Koşulsuz silme seçilmedi: FK'lar `Restrict`, Y-16 soft delete var, K-19 (KVKK) V1 dışı (K-32) |
| **A-58** | Seed ikiye ayrılır | A | Gerçek referans verisi (19 fakülte / 121 bölüm) migration `HasData` ile kalıcı ve üretime gider; kulüp/etkinlik/duyuru/üyelik demo verisi `DemoDataSeeder` ile yalnızca `Seed:Demo=true` iken ve idempotent üretilir (K-34, Y-68) |
| **A-59** | Oturum önyüklemesi ve panel köprüsü | B | `AuthProvider` açılışta bir kez `/auth/refresh` dener ve bu sürede **üçüncü bir durum** (`bootstrapping`) yayınlar — `ProtectedRoute` o sırada karar vermez. Hangfire token'ı query yerine `/hangfire` kapsamlı çerezden okunur (K-01, K-14, Y-65) |
| **A-60** | Topluluk kategorisi referans verisidir | A | `ClubCategory`, `Faculty`/`Department` ile aynı sınıf: `reference.manage`, hard delete serbest ama kullanımdaysa **409** (A-12). `Club.ClubCategoryId` ve `ClubApplication.ProposedCategoryId` **nullable** — mevcut kulüpler kategorisiz, zorunlu kılmak onları geçersiz duruma sokardı (O-15'in ad-soyad gerekçesiyle aynı). Kategori adı kulüp DTO'sunda göründüğü için yazma uçları `CacheRemoveAspect` taşır (K-35, Y-45). **v6.12 tadili:** kategori artık `Club` üzerinde bir kolon değil, `ClubCategoryAssignment` satırıdır. A-60'ın kazancı korunur — kullanımdaki kategori hâlâ silinemez; FK `Restrict` bu kez bağ tablosundan kategoriye kurulur ve `ReferentialIntegrityConflictException` üzerinden aynı 409'u üretir (A-80) |
| **A-61** *(v6.2'de tadil edildi)* | Topluluk içi rolde **unvan yetkiden ayrılır** | A | `ClubRoleDefinition` yalnızca unvan taşır; yetki ayrı bir alandan okunur ve `ClubMembership`'e **denormalize** edilir — yetki kontrolleri ek DB okuması yapmaz. **v6.0'daki hâli:** yetki alanı üç değerli `ClubRole` enum'ıydı. **v6.2:** o alan `ClubCapability` bayrak kümesine genişledi (A-68); `ClubRole` yerinde kalır ama artık **makam** anlamı taşır (A-39 başkan tekilliği, dönem devri, bildirim hedefi) — yetki kararı vermez. Denormalizasyon kararının kendisi değişmedi ve asıl kazanç oydu. Tanımın yetkisi düzenlenebilir; değişiklik o unvanı taşıyan tüm üyeliklere **aynı transaction'da** yayılır, ikinci başkan üretecekse 409 (K-36, Y-69, O-27) |
| **A-62** | Evrak tipi kataloğu ve zorunluluk bayrağı | A | `ClubDocumentType` = kod + ad + `IsRequired` + `IsActive` + sıra; admin yönetir (`reference.manage`). Sekiz gerçek MTÜ formu (FR-0230…FR-0272) migration `HasData` ile gelir — A-58'in "gerçek kurumsal referans verisi kalıcı ve belirleyici" kuralı. `IsActive = false`, geçmiş başvuruları bozmadan bir formu yürürlükten kaldırmanın yoludur. Zorunluluk kontrolü Business'ta, katalog okunarak yapılır — FluentValidation'a konulamaz (K-37, Y-71) |
| **A-63** | Kuruluş evrakı korumalı veridir | A | Evrak dosyası daima `FileVisibility.Protected`; erişim tek ve yeni bir uçtan (`GET /api/club-applications/{id}/documents/{documentId}`), yetki **indirme anında yeniden** kontrol edilerek. Görebilenler: başvuran öğrenci veya `clubs.write`/`clubs.manage.all`. **Reddedilen** başvurunun evrakları karar tarihinden 90 gün sonra gecelik bakım işiyle silinir; onaylananınki kulübün kuruluş dosyası olarak kalır. 90 gün, `TrafficLog`'un 30 gününün (A-44) aynı gerekçesi: K-19 V1 dışı olduğu sürece kişisel veri yüzeyi süresiz büyümemeli (K-37, Y-70, Y-51) |
| **A-64** | İzin verilen dosya tipi **çağrı yerine göre** belirlenir | A | Core'a PDF imzası (`%PDF-`) eklenir, ama `StoreFileAsync` izin verilen tip kümesini **parametre alır**: logo/afiş JPEG/PNG/WebP, evrak yalnızca PDF. Parametresiz eklemek, tek ortak yol yüzünden logo ucunu da PDF'e açar ve "logo/afiş yalnızca görsel" sessiz onayını **sessizce** delerdi. Regresyon ayrı bir testle kilitlenir: logo ucuna PDF → 400 (K-37, K-05, Y-40) |
| **A-65** | Etkinlik kitlesi duyuru görünürlüğünün birebir kardeşidir | A | `EventAudience { Public, ClubMembers }`; **yazma anında zorunlu, okuma anında yorumlanmaz** (A-43'ün aynı cümlesi). `ClubMembers` etkinlik anonim vitrinde **hiç görünmez** — Y-57'nin etkinlik karşılığı; kayıt için güncel dönem üyeliği aranır. "Vitrinde görünsün ama kayıt kapalı" seçilmedi: duyuru ve etkinlik aynı soruya farklı cevap verseydi "hangi içerik anonim yüzeye çıkar" sorusunun tek cümlelik cevabı kaybolurdu (K-38, A-42, Y-72) |
| **A-66** | Başvuru penceresi dönemin bir özelliğidir | A | `AcademicTerm`'e `ClubApplicationStartUtc`, `ClubApplicationEndUtc` ve üç durumlu `ClubApplicationOverride` (`FollowSchedule`/`ForceOpen`/`ForceClosed`). Yeni varlık, yeni izin, yeni ekran yok — `reference.manage` zaten dönem yönetiminin izni. **Fail-closed:** takvim tanımlı değilse kapalı; başvuru sezonuna kurum karar verir. Dağıtımda kesinti olmasın diye migration mevcut güncel dönemi `ForceOpen` işaretler; dönem devrinde (K-30) yeni dönem kapalı gelir. Karar **tek metotta** hesaplanır; muhafız da okuma ucu da onu çağırır (K-39, Y-73) |
| **A-67** | **Onay kuyrukları da yönetici kapsamına girer** | A | `clubs.manage.all` taşıyan yönetici **etkinlik** ve **üyelik** onaylarını da görür ve karara bağlar. Bu, A-25'in ve `MembershipApplicationManager.ReviewAsync`'in "yalnızca danışman" kuralını yönetici için **açıkça gevşetir** — o kural danışman-öğrenci ilişkisini korumak için yazılmıştı, ama yan etkisi yöneticinin danışmanı ulaşılamayan bir kulübün tıkanmasını açamaması oldu. Dört metot: `EventManager.GetApprovalQueueAsync`, `EventManager.DecideAsync`, `MembershipApplicationManager.GetPendingAsync`, `MembershipApplicationManager.ReviewAsync`. **Danışmanın kendi yetkisi değişmez** — yönetici yolu eklenir, danışman yolu kaldırılmaz (K-31'in tamamlanması, Y-74) |
| **A-68** | Kulüp içi yetki **kapalı bir matristir**, merdiven değil | A | Üç değerli `ClubRole` merdiveni yerine `[Flags] enum ClubCapability` — **altı** kulüp içi kapasite: `EventsManage`, `EventParticipantsView`, `AnnouncementsManage`, `MembersView`, `MembersManage`, `ReportsView`. Bu altı, `ClubRole`'ün bugün yetki kararı verdiği **tam** yer kümesidir; sayım koddan yapıldı, tahminle değil. Tek `int` alanda taşınır: `ClubRoleDefinition.Capabilities` (kaynak) ve `ClubMembership.Capabilities` (denormalize kopya, A-61). **Neden enum, neden tablo değil:** string izin kodları taşıyan bir tablo, Identity'nin yanında ikinci bir yetki sistemidir (Y-37/Y-69); kapalı bir enum ise kod değişikliği + migration olmadan büyüyemez. **Neden `ClubRole` silinmiyor:** A-39'un filtreli unique index'i, dönem devri ve bildirim hedefi "başkan kim" sorusuna cevap ister — enum **makam** olarak kalır, yetki kararı vermez. **Unvansız üyenin varsayılanı** eski davranışı birebir korur: `Member` → boş, `Officer` → `MembersView\|EventsManage\|EventParticipantsView\|AnnouncementsManage\|ReportsView`, `President` → hepsi. Migration mevcut üyelikleri bu eşlemeyle geri doldurur. Matris **yalnızca daraltır** (Y-75) (K-36, A-61, Y-69) |
| **A-69** | Başvuru logosu **bir kez saklanır, onayda kopyalanır** | A | Logo başvuru anında `FileVisibility.Public` olarak saklanır (kulüp kimliğidir, kişisel veri değil — A-63'ün evrak gerekçesi buraya uymaz) ve `ClubApplication.LogoFileId`'de tutulur. Onay dalında `Club.LogoFileId` **aynı `StoredFile` satırını** işaret ederek alır; ikinci yükleme veya dosya kopyası yapılmaz. Depoda tek kayıt kalır ve onay anında yükleme hatası riski doğmaz. Kulüp henüz yokken danışman kontrolü yapılamayacağı için `UploadClubLogoAsync` yeniden kullanılmaz; `StoreApplicationLogoAsync` (evrak yolunun kardeşi, ama Public + görsel tipleri) eklenir — yetki `SubmitAsync`'te zaten kurulmuştur (K-40, A-64, Y-76) |
| **A-70** | "Kulüplerim" **tek uçtur, iki ilişki üretir** | A | `GET /api/clubs/mine` aynı kalır; yanıt satırlarına `Relationship` (`Member`\|`Officer`\|`President`\|`Advisor`\|`Administrator`) eklenir. Üyelik satırları §22.3 gereği YALNIZCA güncel döneme aittir; danışmanlık dönemsel bir kayıt değildir (`Club.AdvisorId`, `AcademicStaff` 1-1), bu yüzden danışman satırlarına dönem filtresi uygulanmaz — güncel dönem tanımsızken bile danışman kulübünü görür. Bir kullanıcı aynı kulübe hem üye hem danışman olamaz (biri öğrenci profili, diğeri personel profili), bu yüzden satırlar çakışmaz. `Relationship` enum'ı, Faz 41'in `ClubDetailDto.MyRelationship`'ı için baştan altı değerle açılır — sonradan araya değer sokmak, tel üzerinde metin giden bir enum'da eski istemcileri bozar (K-41) |
| **A-71** | Zengin metin **yapısal JSON'dur, HTML değildir** | A | Duyuru (ve Faz 39'da etkinlik) içeriği editörün düğüm ağacı olarak (`ContentJson`) saklanır; HTML dizesi ne veritabanına yazılır ne arayüzde ayrıştırılır. Yazma anında sunucu ağacı kapalı izin listesine göre dolaşır (`RichTextDocumentValidator`), okuma anında arayüz ağacı gezip React öğeleri üretir (`dangerouslySetInnerHTML` YOK). Gerekçe: HTML saklamak her okuma noktasında sanitize zorunluluğu doğurur; bir noktayı atlamak depolanmış XSS demektir — ağaçta böyle bir kapı yoktur. Düz metin aynası (`Content`) korunur: arama (`GetFeedAsync`), e-posta ve önizleme oradan okumaya devam eder; `ContentJson` null olan eski kayıtlar düz metin olarak render edilir, veri taşıma gerekmez (K-42, Y-78) |
| **A-72** | İçerikteki renk **anlamsal token'dır, hex değildir** | A | Yazar `accent\|success\|warning\|danger\|muted` arasından seçer; hangi hex'e karşılık geldiğine `arayuz/src/theme/tokens.ts`'teki `richTextColors` (Y-56'nın tek istisnası değil, onun **uzantısı**) karar verir — koyu/açık mod için ayrı tablo. Gerekçe: uygulamanın koyu modu var; içeriğe gömülen sabit bir hex koyu temada zeminle birleşebilir, token ise moda göre çözülür. Sunucudaki `RichTextSchema.AllowedColorTokens` ile arayüzdeki token listesi **aynı beş isim** olmalıdır — biri diğerine eklenmeden değişirse kullanıcı seçtiği rengi kaydedemez; bu eşleşme mimari testle kilitlenir (K-42, Y-78) |
| **A-73** | Harita **gömülü iframe'dir, kütüphane değildir** | A | Konum, Google Maps'in `output=embed` ucuna gömülür ve yol tarifi bağlantısı aynı sorgu metniyle kurulur; harita SDK'sı veya API anahtarı projeye girmez. Yardımcılar `arayuz/src/utils/maps.ts` altında tek yerde durur — ana sayfanın kampüs haritası da (önceden `data/campuses.ts` içinde kopyası duran) buradan besleniyor. Gerekçe: iki ayrı harita yardımcı seti, biri güncellenirken öbürünün unutulması demektir (K-43) |
| **A-74** | Sosyal hesaplar **satırdır, kolon değil**; yazma kapısı **`AnnouncementsManage`** kapasitesidir | A | Sosyal medya bağlantıları `ClubSocialLink` tablosunda (`ClubId`, `Platform`, `Url`, `DisplayOrder`) tutulur; `Club` satırına platform başına kolon eklenmez. Gerekçe: platform kümesi zamanla değişir, kolon eklemek her seferinde migration ve DTO şişmesi demektir; sıralama ve "aynı platformdan iki hesap" ihtiyacı satır modeliyle doğal karşılanır. E-posta ve telefon kulübe birebir olduğu için `Club` üzerinde kalır. **Yazma kapısı:** `Club.Name`/`Description` gibi çekirdek alanlar hâlâ yalnızca Admin'in (`clubs.write`) kapısındadır — bu faz onu genişletmez. İletişim/sosyal bağlantılar ise **dışa dönük iletişimin bir parçası** sayılır ve `AnnouncementManager.EnsureClubWriteAccessAsync` ile birebir aynı desenle korunur: danışman veya `ClubCapability.AnnouncementsManage` taşıyan üye (varsayılan olarak başkan ve yetkili), ya da `clubs.manage.all` taşıyan yönetici. Yeni bir `ClubCapability` bayrağı **açılmaz** — A-68 kapalı bir kümedir; en yakın anlamsal karşılık (iletişim ⇄ duyuru) yeniden kullanılır (K-44, Y-80) |
| **A-75** | Sekme görünürlüğü global izinle değil, **kulüpteki ilişkiyle** belirlenir | A | `ClubDetailDto`, çağıran kullanıcının o kulüpteki ilişkisini (`MyRelationship`, Faz 37'nin `ClubRelationship` enum'ı) ve kapasitelerini (`MyCapabilities`, A-68) taşır; arayüz "Üyeler"/"Roller"/"Etkinlik yönetimi" sekmelerini ve düğmelerini bu iki alandan çizer. Gerekçe: `ClubDetailPage` daha önce **global** `memberships.read`/`events.write` iznine bakıyordu — bir kulüpte yetkili olan öğrenci, hiç üyesi olmadığı başka bir kulübün "Üyeler" sekmesini de açık görüyor ve uç kendi 403'ünü döndürüyordu (Y-35 doğru çalışıyordu ama kullanıcı hatasız bir ekran yerine kırık bir ekranla karşılaşıyordu). `Club.Name`/`Description` gibi çekirdek alanların düzenleme kapısı bu fazda **değişmez** — hâlâ yalnızca Admin'in (`clubs.write`) elinde (A-74'ün notu) (K-45, Y-81) |
| **A-76** | Etkinlik detay **düzeni tek bileşendir**, sayfa onu giydirir | A | Afiş, açıklama kartı ve sağ sütun (düzenleyen/zaman/konum/istatistik) `arayuz/src/components/events/EventDetailLayout.tsx` içinde bir kez yazılır; anonim sayfa (`/etkinlikler/:id`) ve panel sayfası (`/events/:id`) onu sarar. Fark yalnızca **birincil eylem alanı** (`primaryAction` slotu: anonimde "Giriş Yap", panelde katıl/iptal) ve panele özgü ek bölümlerdir (katılımcı listesi, düzenleme/iptal). Gerekçe: A-73'ün harita yardımcılarında öğrendiğimiz ders — aynı görünümün iki kopyası, biri güncellenirken öbürünün unutulması demektir (K-46) |
| **A-77** | Görüntülenme sayacı **ayrı bir yazma ucudur**, okuma yolunda artmaz | A | Detay ucu (`GET`) sayacı **artırmaz**; arayüz sayfayı açtığında ayrı bir `POST /api/public/events/{id}/view` çağırır ve bunu `sessionStorage` ile oturum başına bir kereye indirir. Artırma `IEventViewDal.IncrementAsync` içinde `ExecuteUpdateAsync` ile **tek SQL cümlesidir**: `Event.RowVersion` taşıdığı için oku-değiştir-kaydet döngüsü eşzamanlı okumalarda `ConcurrencyConflictException` üretirdi (A-15/Y-53). Sayaç **yaklaşıktır** ve bir karar dayanağı değildir; kontenjan ve katılım kararları hâlâ yalnızca katılım ucundan gelir. Gerekçe: GET'in yan etkisi olmaması hem önbelleklenebilirliği hem "okumak veriyi değiştirmez" beklentisini korur (K-46) |
| **A-78** | Form **bir sayfadır**, kip rotadan gelir | A | Her varlığın formu tek bileşendedir; oluşturma ile düzenleme aynı sayfanın iki kipidir (`:id` varsa düzenleme). Çağıran ekran formu parametreyle bağlar: `clubId` bağlamı verir (yoksa sayfa kulüp seçtirir), `returnTo` kaydettikten sonra nereye dönüleceğini söyler. Gerekçe: K-37'de kuruluş başvurusu için verilen "evrak yüklemeli form diyaloga sığmaz" kararının genellenmesi — zengin metin editörü, tarih alanları ve görsel yükleme `maxWidth="sm"` bir diyalogda sıkışıyordu. Aynı alanların oluşturma ve düzenleme için iki kez yazılması, projede iki kez yaşanan "ikinci yapım noktası" hatasının arayüz karşılığıdır (K-47, Y-83) |
| **A-79** | Erişilebilirlik tercihleri **tema katmanında** çözülür | A | Tercihler tek bir bağlamda tutulur ve `createAppTheme`'e argüman olarak girer: yazı ölçeği `typography.fontSize`, yüksek kontrast palet + odak halkası, hareket azaltma `transitions` ve `CssBaseline` üzerinden uygulanır. Bileşenler tercihi okumaz, yalnızca temadan boyanır (Y-84). Yüksek kontrast renkleri `theme/tokens.ts` içindeki `contrastPalette`'ten gelir — Y-56'nın istisnası değil, A-72 gibi **uzantısıdır**. Tercihler kullanıcı profiline değil `localStorage`'a yazılır: cihaza özgü bir ayardır, sunucuya taşımak yeni bir uç, yeni bir migration ve "hangi cihaz kazanır" sorusu demektir. Hareketi azalt ayarının başlangıç değeri `prefers-reduced-motion` sistem tercihinden okunur (K-48) |
| **A-80** | Kategori bağı **satırdır, kolon değil**; tek kaynak bırakılır | A | `Club.ClubCategoryId` **kaldırılır**, veri migration içinde `ClubCategoryAssignment`'a taşınır. "Birincil kategori kolonu + çoklu bağ tablosu" ikilisi bilinçli olarak reddedildi: iki kaynak, biri güncellenirken öbürünün unutulması ve "hangisi doğru" sorusu demektir (A-74'ün sosyal hesaplar için verdiği kararın aynısı). Bu kod tabanında navigation property olmadığı için join'ler `IClubCategoryAssignmentDal`'de SQL tarafında yapılır: `GetClubIdsByCategoryAsync` (filtre) ve `GetNamesByClubAsync` (gösterim) — ikisi de tek sorgu, sayfa başına bir kez. Kuruluş başvurusundaki `ProposedCategoryId` **tekil kalır** (öneri tek bir sınıflandırmadır); onay dalında tek bir bağ satırına dönüşür (K-49, Y-85, A-60 tadili) |
| **A-81** | Vitrindeki sayılar **vitrinde görülebilen veriyle** tutarlıdır ve tek sorgudan gelir | A | "Üye sayısı" **güncel dönemin** `ClubMembership` satırlarıdır (§22.3: üyelik dönemseldir; geçmiş dönemleri toplamak, bugün 12 üyesi olan kulübü 300 üyeli göstermek demektir). "Etkinlik sayısı" anonim yüzeyde görünen etkinliklerdir: `Status == Published && Audience == Public` (Y-58/Y-72) — vitrinde göremeyeceği bir etkinliği sayan bir rozet, kullanıcıya bulamayacağı bir şeyi vaat eder. İkisi de `IClubStatsDal.GetCountsAsync(clubIds)` ile **sayfa başına tek** `GROUP BY` sorgusundan okunur; satır başına sayım 12 kartlık sayfada 24 sorgu açardı (Y-42, Y-10) (K-50) |
| **A-82** | Vitrin detayı **künye/eylem** ve **içerik** olarak iki sütundur; sayaç deseni tekrar edilir, yeniden icat edilmez | A | Sol sütun kullanıcının **yapabileceği** şeyi (giriş/katıl), kulübe **ulaşma** yolunu (Faz 40'ın iletişim/sosyal verisi) ve **paylaşımı** taşır; sağ sütun okunacak içeriği (hakkımızda, etkinlikler, duyurular). Görüntülenme sayacı A-77'nin **birebir aynı** desenidir: `GET` sayacı artırmaz, ayrı `POST /api/public/clubs/{id}/view` ucu `IClubViewDal.IncrementAsync` içinde `ExecuteUpdateAsync` ile tek SQL cümlesi çalıştırır (`Club.RowVersion` yüzünden oku-değiştir-kaydet çakışırdı), istemci `sessionStorage` ile oturum başına bir kez çağırır. Gerekçe: aynı sorunun ikinci kez farklı çözülmesi, iki ayrı hata yüzeyi demektir (K-51, A-77, Y-87) |
| **A-83** | OOXML tespiti paketi **açarak** yapılır, ham baytta dize aranmaz | A | `.docx` bir ZIP paketidir: girdi **adları** yerel başlıkta sıkıştırılmadan durur, girdi **içerikleri** deflate ile sıkıştırılır. Bu yüzden `word/document.xml` ham baytta görünür ama `[Content_Types].xml`'in içindeki `wordprocessingml` **görünmez** — dize taramasıyla yazılan ilk sürüm (Faz 47 sonrası) Word'ün ürettiği her dosyayı reddediyordu. Tespit `ZipArchive` ile yapılır: `word/document.xml` girdisi aranır **ve** `[Content_Types].xml` okunup içeriğinde `wordprocessingml` doğrulanır; ikisi birlikte olmadan Docx sayılmaz (xlsx `xl/`+`spreadsheetml`, pptx `ppt/`+`presentationml` taşır). Untrusted girdi olduğu için yalnızca girdi adları listelenir ve tek küçük girdi, boyut tavanıyla okunur. Şablonun boş form olması `Public` görünürlüğü haklı çıkarır; doldurulmuş evrak A-63 gereği `Protected` kalır (K-52, Y-88, Y-40) |

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
| A-61 × A-39 × A-51 | Kurala bağlandı | Unvan katmanı `ClubRole`'ü yerinde bıraktığı için başkan tekilliği (filtreli unique index) ve dönem devri (1:1 kopya) hiç değişmez. Tanımın seviyesi değişince güncelleme tüm üyeliklere yayılır — kulüp üye sayıları onlarla ifade edildiği için A-51'in audit patlaması uyarısı burada geçerli değil |
| A-61 × Y-37 | Kurala bağlandı | `ClubRoleDefinition` izin/claim taşımaz; Identity'nin yanına ikinci bir yetki sistemi açılmaz. Mimari test tipin ilkel alanlar + enum dışında bir şey taşımadığını doğrular (Y-69) |
| **A-68 × Y-37 × Y-75** | **Dikkat** | Matris Identity'nin *yerine* değil, *içinde* çalışır: uçtaki `[SecuredOperation]` birinci kapı, kapasite ikinci kapı. Bu sıra bozulursa kulüp başkanı kendi kulübüne, sistemde hiç var olmayan bir yetkiyi dağıtabilir hâle gelir. Mimari test kapasite okuyan metodun `[SecuredOperation]`'lı bir uçtan çağrıldığını doğrular |
| **A-68 × A-39 × A-51** | Kurala bağlandı | `ClubRole` enum'ı **makam** olarak yerinde kalır; başkan tekilliğinin filtreli unique index'i, dönem devrinin 1:1 kopyası ve `EventDecisionNotificationJob`'ın hedef sorgusu değişmez. Yetki matrisi ayrı bir alanda taşınır ve `ClubRole` ile **ayrışabilir** — bir "Sayman" makamı `Member`, kapasitesi `EventsManage` olabilir. Ayrışma kasıtlıdır: makam törensel, kapasite işlevseldir |
| **A-68 × A-61** | Uyumlu | Denormalizasyon deseni aynen sürer: kapasite `ClubMembership`'e kopyalanır, tanım değişince O-27 ile tüm taşıyıcılara aynı transaction'da yayılır. Yetki kontrolü hâlâ tek satır okur |
| A-64 × K-05 × Y-40 | **Dikkat** | Tek ortak `StoreFileAsync` yolu var. PDF'i imza listesine parametresiz eklemek logo/afiş ucunu da PDF'e açar. Tip kümesi çağrı yerine göre geçilir; regresyon "logo ucuna PDF → 400" testiyle kilitlenir |
| A-63 × Y-26 × K-19 | Kabul edilen sınır | Kuruluş evrakları (adli sicil, kurucu üye dilekçesi) kişisel veri yüzeyini büyütür. Karşılık A-44'ün deseni: dar erişim (iki taraf), korumalı görünürlük, indirmede yeniden yetki, reddedilenler için 90 gün saklama. K-19'un KVKK akışının kendisi hâlâ V1 dışı |
| A-66 × A-51 × K-30 | **Dikkat** | Pencere döneme bağlı olduğu için dönem devri onu **taşımaz** — yeni dönem boş ve `FollowSchedule` ile, yani kapalı gelir. Bu kasıtlı: her sezonu admin açar. Devrin sessizce açık bırakması, "başvurular kapalı olmalıydı" hatasının en pahalı hâli olurdu |
| A-60 × A-17 × Y-45 | Kurala bağlandı | Kategori adı kulüp listesi DTO'sunda taşınır; kategori yazma uçları `ClubManager.` ve `PublicContentManager.` anahtarlarını düşürmezse liste eski adı servis eder. Y-45'in referans verisi karşılığı |
| A-62 × A-58 | Uyumlu | Sekiz FR formu **gerçek kurumsal referans verisi** — `HasData` ile kalıcı ve üretime gider. Admin'in sonradan eklediği tipler normal CRUD; demo verisiyle karışmaz (Y-68) |

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
| Yükleme sınırları | Logo/afiş 5 MB, yalnızca JPEG/PNG/WebP. **Kuruluş evrakı** 5 MB, yalnızca PDF; bir başvuruda toplam 60 MB. Hepsi içerik imzasıyla doğrulanır, izin verilen tip kümesi çağrı yerine göre geçilir (A-64) |
| Evrak saklama | Reddedilen başvurunun evrakları karar tarihinden 90 gün sonra silinir; onaylananınki kalır (A-63) |
| Rapor akışı | Tüm Excel talepleri kuyruğa girer — boyut eşiği yok, çünkü iki yol iki kod yolu demek |
| Rapor saklama | Üretilen dosyalar 7 gün sonra silinir; kullanıcı raporu yeniden talep edebilir |
| Gecelik bakım işi | Tek yinelenen iş: süresi geçmiş refresh token'lar + eskimiş rapor dosyaları + 30 günü geçmiş trafik logu + 90 günü geçmiş reddedilmiş başvuru evrakları + sahipsiz disk dosyaları |
| Excel kütüphanesi | ClosedXML (MIT). EPPlus lisans koşulları nedeniyle kullanılmaz |
| Audit kapsamı | Tüm entity'ler; parola, token ve hash alanları audit'e yazılmaz |
| Migration adlandırma | `YYYYMMDD_AçıklayıcıAd`, her PR'da en fazla bir migration |
| Dal stratejisi | `master` korumalı; iş `feature/*` dallarında, PR ile birleşir |
| Belge sahipliği | Bu belge + mimari testler tek doğruluk kaynağı; kural değişimi önce burada yazılır |

---

*Mimari taslak v6.2 · 68 karar, 75 kural, 13 V1 dışı madde (K-13 yarısı kapsama alındı), 35 faz · referans: engindemirog/NetCoreBackend*
