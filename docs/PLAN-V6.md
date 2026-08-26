# V6 — Kategori, dinamik roller, kuruluş evrakları ve başvuru takvimi (Faz 30 → 34)

## Context

PLAN-V5'in altı fazı da bitti ve pushlandı (son commit `60ad493`). V5 sistemi **yönetilebilir** hâle
getirdi: yönetici kapsamı açıldı, kişilere isim verildi, gerçek kurumsal veri yerleşti. V6 bunun
üzerine **kurumsal süreci** koyuyor — MTÜ'nün topluluk yönergesinin sistemde karşılığı olmayan beş
parçası.

İstenen beş özellik ve bugünkü durumları:

| # | İstenen | Bugünkü kod |
|---|---|---|
| 1 | Topluluk kategorisi | `Club` sınıfında **kategori alanı yok**; kulüpler tek düz liste |
| 2 | Dinamik topluluk içi roller | `ClubRole` **üç değerli sabit enum** (`Member`/`Officer`/`President`) — hem unvan hem yetki seviyesi olarak kullanılıyor |
| 3 | Kuruluş evrakları + onay ekranında görüntüleme | `ClubApplication`'da **hiç dosya alanı yok**; `FileSignatureInspector` PDF'i **tanımıyor** (yalnızca JPEG/PNG/WebP) |
| 4 | Etkinlik katılım kitlesi | `Event`'te **kitle alanı yok**; yayındaki her etkinliğe her öğrenci kaydolabiliyor |
| 5 | Başvuru takvimi | `ClubApplicationManager.SubmitAsync` **her zaman açık**; kapatma mekanizması yok |

### Kod okumasından çıkan, planı şekillendiren üç kısıt

| # | Bulgu | Sonuç |
|---|---|---|
| 1 | `ClubRole` enum'ı **yük taşıyor** — 7 `Ensure*Access*` metodu `Officer`/`President` karşılaştırmasıyla yetki kararı veriyor (`ClubMemberManager.cs:291`, `EventParticipationManager.cs:258`, `EventManager`, `AnnouncementManager`, `FileManager.cs:115`) | "Dinamik rol" enum'ı kaldırırsa **yetki yüzeyinin tamamı** yeniden test edilmek zorunda kalır. Bu yüzden unvan yetkiden ayrılıyor (O-19) |
| 2 | `FileSignatureInspector.Detect` yalnızca JPEG/PNG/WebP döner (`FileSignatureInspector.cs:9-32`); `FileManager.StoreFileAsync` **tek ortak yol** — logo, afiş ve gelecekteki evrak aynı metottan geçiyor | PDF'i imza listesine eklemek, **logo ucunun da PDF kabul etmesi** demektir. Sessiz onaydaki "logo/afiş yalnızca JPEG/PNG/WebP" kuralı sessizce delinir. `StoreFileAsync` izin verilen tip kümesini parametre almak zorunda (O-23) |
| 3 | Adli sicil / kurucu üye dilekçesi **kişisel veri**; K-19 (KVKK silme akışı) V1 dışı | Evraklar `Protected` görünürlükle saklanır (Y-52), ayrı bir korumalı uçtan servis edilir, ve reddedilen başvurunun evrakları süresiz durmaz (O-24) |

**Hedef:** V1-V5'in disiplinini (beş katman, Y-01…Y-68, tek paradigma) hiç bozmadan, beş bağımsız
fazda kurumsal süreci sisteme koymak. Faz sırası **risk artan** yönde: en küçük ve en izole olan
önce, yetki yüzeyine en yakın olan en sonda.

---

## Kararlar (onaylandı)

> Soru aracı bu oturumda da kullanılamadı (PLAN-V5 §Kararlar'daki durumun aynısı). Kararlar sohbette
> gerekçesiyle sunuldu ve **dokuzu da onaylandı**. Değiştirmek istediğin madde olursa uygulamaya
> geçmeden söylemen yeterli.

### O-18 — Kategori: `Faculty`/`Department` ile aynı sınıf referans verisi

`ClubCategory` yeni referans varlığı olur: `reference.manage` izni, Referans Verisi ekranına yeni
sekme, hard delete serbest (A-12) ama **kullanımdaysa 409** (Faz 13'te fakülte için kurulan desen).

`Club.ClubCategoryId` ve `ClubApplication.ProposedCategoryId` **nullable**. Gerekçe: mevcut kulüpler
kategorisiz; zorunlu kılmak onları geçersiz duruma sokar ve bir kerelik "kategori atama" akışı
gerektirirdi — O-15'in (ad soyad nullable) aynı gerekçesi.

| Seçenek | Neden seçilmedi |
|---|---|
| Kodda sabit enum | Kategori listesi kurumun kararı; her değişiklik için dağıtım gerekirdi |
| `Club`'a serbest metin alan | Filtreleme ve raporlama imkânsızlaşır; her kulüp aynı kategoriyi farklı yazar |

### O-19 — Dinamik roller: **unvan yetkiden ayrılır** (iki katmanlı model)

`ClubRole` enum'ı **yetki seviyesi** olarak aynen kalır. Yeni `ClubRoleDefinition` tablosu **unvan**
tutar ve her unvan bir yetki seviyesine bağlanır. `ClubMembership` iki alanı birden taşır:

| Alan | Anlamı | Kim okur |
|---|---|---|
| `ClubRole` (mevcut, değişmez) | Yetki seviyesi | 7 `Ensure*Access*` metodu, A-39 unique index'i |
| `ClubRoleDefinitionId` (yeni, nullable) | Görünen unvan | Yalnızca arayüz ve listeler |

**Kazanç:** yetki yüzeyi hiç değişmez, `Ensure*Access*` metotlarına tek satır bile eklenmez, ek DB
okuması olmaz, A-39'un filtreli unique index'i olduğu gibi çalışmaya devam eder.

| Seçenek | Neden seçilmedi |
|---|---|
| `ClubRole` → `ClubRoleId` FK (enum tamamen tabloya) | Her yetki kontrolü ek DB okuması yapar; Y-18 unique index'leri ve A-39 yeniden kurulur; yetki yüzeyinin tamamı yeniden test edilir. Kazancı yalnızca "kurum 3 yerine 5 yetki seviyesi tanımlayabilsin" — istenmedi |
| `ClubMembership.Title` serbest metin | Tablo yok, yönetim ekranı yok, tutarlılık yok, raporlanamaz. "Dinamik rol" sayılmaz |

### O-20 — Rol tanımları **kulübe özeldir**

`ClubRoleDefinition.ClubId` doludur. Kulübün **başkanı veya danışmanı** (ya da yönetici) kendi
kulübünün rollerini yönetir — `EnsureRoleManagementAccessAsync`'in aynı yolu. Kulüpler birbirinin
listesini görmez.

Yeni kulüp doğduğunda (`ClubApplicationManager.DecideAsync` onayı veya `ClubManager.CreateAsync`)
**varsayılan set kopyalanır**: Başkan→`President`, Başkan Yardımcısı→`Officer`, Sayman→`Officer`,
Sekreter→`Officer`, Üye→`Member`.

| Seçenek | Neden seçilmedi |
|---|---|
| Merkezî katalog (yalnız admin) | Kulüp kendine özgü bir rol ekleyemez ("Yazılım Ekibi Lideri"); kurumsal kontrol kazancı bu maliyeti karşılamıyor |
| İkisi birden (merkezî + kulübe özel) | İki ekran, iki yetki yolu, iki kat test — bugün karşılığı olmayan bir esneklik |

### O-21 — Evrak tipi kataloğu: zorunluluk bayrağı katalogda

`ClubDocumentType` = kod (`FR-0230`), ad, **`IsRequired`**, `IsActive`, `DisplayOrder`. Ekran
görüntüsündeki `*`'lı ve `*`'sız satırların tek kaynağı bu bayrak. Admin yönetir (`reference.manage`).

Ekran görüntüsündeki **sekiz gerçek MTÜ formu** migration `HasData` ile gelir — A-58'in kuralı:
gerçek kurumsal referans verisi kalıcı, belirleyici, üretime gider.

| Kod | Ad | Zorunlu |
|---|---|---|
| FR-0230 | Topluluk Akademik Danışman Dilekçesi | ✔ |
| FR-0240 | Topluluk Asıl Üyeler (Yönetim Kurulu) | ✔ |
| FR-0241 | Topluluk Faaliyet Planı | ✔ |
| FR-0242 | Topluluk Kapak Sayfası | ✔ |
| FR-0243 | Topluluk Kurucu Üye Dilekçesi | ✔ |
| FR-0244 | Topluluk Kuruluş Dilekçesi | ✔ |
| FR-0245 | Topluluk Üye Listesi | ✔ |
| FR-0272 | Topluluk Örnek Tüzük | ✔ |

### O-22 — Evrak yükleme: **tek multipart istek**

`POST /api/club-applications` multipart olur; form alanları + tüm dosyalar tek seferde gider.

**Gerekçe — atomiklik:** eksik evrakla başvuru veritabanına hiç yazılmaz ve yetim `StoredFile`
satırı kalmaz. Ekran görüntüsündeki tek "Başvuruyu Gönder" düğmesiyle birebir.

| Seçenek | Neden seçilmedi |
|---|---|
| Önce dosyalar → `fileId`'lerle JSON başvuru | Başvuru hiç gönderilmezse yetim dosya kalır; gecelik bakıma ikinci bir temizlik yolu eklemek gerekirdi |
| Taslak başvuru + tek tek yükleme + "Gönder" | `ApplicationStatus`'e `Draft` eklemek, "başvurum" ekranını düzenlenebilir yapmak ve yarım başvuru için ayrı yetki yolu kurmak demek — en çok iş |

### O-23 — PDF Core'a eklenir, **ama izin verilen tip kümesi çağrı yerine göre değişir**

`DetectedFileType.Pdf` + `%PDF-` imzası Core'a eklenir. Kritik nokta: `StoreFileAsync` **izin
verilen tip kümesini parametre alır.**

| Çağrı yeri | İzin verilen | Görünürlük |
|---|---|---|
| `UploadClubLogoAsync` / `UploadEventPosterAsync` | JPEG, PNG, WebP | `Public` |
| Kuruluş evrakı | **Yalnızca PDF** | `Protected` |

Parametre olmadan PDF eklemek, logo ucunun da PDF kabul etmesi demekti — sessiz onaydaki
"logo/afiş yalnızca JPEG/PNG/WebP" kuralı **sessizce** delinirdi. Bu regresyon için ayrı bir test
yazılır (§33.9).

### O-24 — Evrak görünürlüğü, indirme yetkisi ve saklama süresi

| Konu | Karar |
|---|---|
| Görünürlük | `FileVisibility.Protected` — anonim `/api/files/{id}` ucundan **asla** dönmez (Y-52, yeni Y-70) |
| İndirme ucu | Yeni korumalı uç: `GET /api/club-applications/{id}/documents/{documentId}` |
| Kim indirebilir | Başvuran öğrenci **veya** `clubs.write` / `clubs.manage.all` taşıyan inceleyici |
| Ne zaman kontrol edilir | **İndirme anında yeniden** — Y-51'in rapor için kurduğu kuralın evrak karşılığı |
| Saklama | **Reddedilen** başvurunun evrakları 90 gün sonra gecelik bakım işiyle silinir (dosya + `StoredFile` + `ClubApplicationDocument`). Onaylananınki kalır — kulübün kuruluş dosyası |

90 gün, `TrafficLog`'un 30 günlük saklama sınırının (A-44) aynı gerekçesiyle: K-19 (KVKK silme
akışı) V1 dışı olduğu için kişisel veri yüzeyi **süresiz büyümemeli.** İtiraz akışı için 30 gün az,
süresiz saklama ise gerekçesiz.

### O-25 — Etkinlik kitlesi: duyurunun birebir kardeşi

`EventAudience { Public = 0, ClubMembers = 1 }` — `AnnouncementVisibility` (A-43) ile aynı biçim,
aynı kural: **yazma anında belirlenir, okuma anında yorumlanmaz.**

`ClubMembers` kitleli etkinlik anonim vitrinde **hiç görünmez** — duyurunun `Members` görünürlüğüyle
(Y-57) birebir aynı davranış. İstisna yok, sızma yapısal olarak kapalı.

| Seçenek | Neden seçilmedi |
|---|---|
| Vitrinde görünsün, rozetle, kayıt kapalı | Duyuru ve etkinlik aynı soruya farklı cevap verirdi; "hangi içerik anonim yüzeye çıkar" sorusunun tek cümlelik cevabı kaybolurdu (A-42) |

### O-26 — Başvuru takvimi: dönemin özelliği + üç durumlu geçersiz kılma

Pencere `AcademicTerm` üzerinde **üç alan** olarak yaşar. Yeni varlık, yeni izin, yeni ekran yok —
`reference.manage` zaten dönem yönetiminin izni, Referans Verisi ekranında Dönem sekmesi zaten var.

| Alan | Tip |
|---|---|
| `ClubApplicationStartUtc` | `DateTime?` |
| `ClubApplicationEndUtc` | `DateTime?` |
| `ClubApplicationOverride` | `ClubApplicationWindowOverride` enum |

`ClubApplicationWindowOverride { FollowSchedule = 0, ForceOpen = 1, ForceClosed = 2 }`

**Karar tablosu** — tek yerde, tek metotta:

| Override | Tarihler | Sonuç |
|---|---|---|
| `ForceOpen` | (bakılmaz) | **Açık** |
| `ForceClosed` | (bakılmaz) | **Kapalı** |
| `FollowSchedule` | ikisi de tanımlı, `now` aralıkta | **Açık** |
| `FollowSchedule` | ikisi de tanımlı, `now` aralık dışında | **Kapalı** |
| `FollowSchedule` | tarih tanımlı değil | **Kapalı** (fail-closed) |

**Fail-closed gerekçesi:** başvuru sezonuna kurum karar verir; pencere tanımlamak bilinçli bir eylem
olmalı. Dağıtım günü hiçbir şeyin durmaması için **migration mevcut güncel dönemi `ForceOpen`
işaretler** — bugünkü davranış aynen sürer, admin dilediğinde takvime geçer. Dönem devrinde (K-30)
yeni dönem boş ve `FollowSchedule` ile gelir, yani kapalı: yeni sezonu admin açar.

| Seçenek | Neden seçilmedi |
|---|---|
| Ayrı `ClubApplicationPeriod` varlığı | Bir dönemde birden fazla pencere ihtiyacı bugün yok; yeni tablo + ekran + uçlar karşılığı olmayan iş |
| "Kapat" = bitiş tarihini şimdiye çekmek | Planlanmış tarih kalıcı olarak kaybolur; "geri aç" elle yeniden yazmayı gerektirir |
| Tarih tanımsızken açık | Admin hiç tarih girmezse özellik fiilen yok sayılır ve kimse fark etmez |

### O-27 — Rol tanımının yetki seviyesi **değiştirilebilir, değişiklik yayılır**

`ClubRoleDefinition.ClubRole` düzenlenebilir. Değiştiğinde o unvanı taşıyan **tüm üyeliklerin
`ClubMembership.ClubRole` alanı aynı transaction içinde güncellenir** — iki alan asla ayrışmaz.

Tek istisna: değişiklik **ikinci bir başkan üretecekse** A-39'un filtreli unique index'i reddeder;
Business bunu önceden yakalayıp `409` + açık mesaj döner ("Bu unvanı taşıyan birden fazla üye var,
Başkan seviyesine yükseltilemez").

Alternatif — seviyeyi oluşturduktan sonra dondurmak — daha basitti ama yanlış seviyeyle oluşturulan
bir unvanı düzeltmek için tüm üyelerin elle yeniden atanmasını gerektirirdi. Kulüp üye sayıları
onlarla ifade edildiği için A-51'in audit patlaması uyarısı burada geçerli değil.

> Bu, O-19'un içinden çıkan türev bir karar — sohbette ayrıca sorulmadı. Ters tarafını tercih
> edersen Faz 34 küçülür.

---

## Faz 30 — Etkinlik katılım kitlesi (K-38, A-65, Y-72)

En küçük faz. Tek enum, tek alan, iki kural. V6'nın "yazma anında belirlenen görünürlük" desenini
kurar; Faz 33'ün evrak görünürlüğü aynı desenin devamı.

### 30.1 Şema

`Entities/Enums/EventAudience.cs` — `Public = 0`, `ClubMembers = 1`.
`Event.Audience` **nullable değil**; migration mevcut satırlara `Public` yazar (bugünkü davranış).

Migration: `20260826_Faz30_EtkinlikKatilimKitlesi`.

### 30.2 Kayıt kuralı

`EventParticipationManager.RegisterAsync` — mevcut `Published`/`StartDateUtc` kontrolünden **sonra**,
kontenjan kontrolünden **önce**:

```
Audience == ClubMembers ise:
    güncel dönemde (ClubId, StudentId) üyeliği aranır
    yoksa → Result.Forbidden(Messages.EventForClubMembersOnly)
```

Üyelik sorgusu güncel dönemi kullanır — `EnsureClubWriteAccessAsync` ve `GetMineAsync` ile aynı
dönemi konuşmak zorunda (PLAN-V4 §22.3'ün düzelttiği tuzağın aynısı).

### 30.3 Anonim vitrin

`PublicContentManager.GetEventsAsync` filtresine `e.Audience == EventAudience.Public` eklenir.
**Filtre kodda sabit** — `Status`/`IsActive` filtreleri gibi, dışarıdan parametrelenmez (Y-58).

### 30.4 Arayüz

| Yer | Değişiklik |
|---|---|
| `schemas/eventForm.ts` | `audience` alanı + payload'a eklenmesi |
| Etkinlik oluştur/düzenle diyalogları | İki seçenekli radyo grubu, varsayılan "Herkese açık" |
| `EventsPage`, `EventDetailPage` | `ClubMembers` ise rozet |
| `MyEventsPage`, `ClubDetailEventsTab` | Aynı rozet |

`EventListItemDto.Audience` eklenir. `PublicEventListItemDto`'ya **eklenmez** — o uçtan zaten yalnızca
`Public` kitleli etkinlik döner, alanı taşımak anlamsız.

### 30.5 Testler

| Test | Nerede |
|---|---|
| Üye olmayan öğrenci `ClubMembers` etkinliğine kaydolamaz (403) | `EventParticipationManagerTests` |
| Güncel dönem üyesi kaydolabilir | `EventParticipationManagerTests` |
| Geçen dönemin üyesi kaydolamaz | `EventParticipationManagerTests` |
| `ClubMembers` kitleli etkinlik anonim listede yok | `PublicContentManagerTests` |
| Aynısı uçtan uca | `PublicSurfaceLeakTests` |
| Geçersiz `audience` değeri 400 | `ValidationRulesTests` |

**Bitti sayılır:** üye olmayan öğrenci üyelere özel etkinliğe kaydolamıyor; o etkinlik anonim
vitrinde ve ana sayfada görünmüyor; herkese açık etkinliklerde hiçbir davranış değişmemiş.

---

## Faz 31 — Topluluk kurma başvuru takvimi (K-39, A-66, Y-73)

Üç alan, tek muhafız, tek okuma ucu. Şema değişikliği küçük; asıl iş kararın **tek yerde**
yaşamasını sağlamak.

### 31.1 Şema

`AcademicTerm`'e üç alan (§O-26 tablosu) + `Entities/Enums/ClubApplicationWindowOverride.cs`.

Migration: `20260826_Faz31_BasvuruTakvimi` — **mevcut güncel dönemi `ForceOpen` işaretler.** Bu satır
olmadan dağıtım anında başvurular sessizce kapanır.

### 31.2 Karar tek metotta

`ClubApplicationManager` içinde tek bir değerlendirme metodu; hem muhafız hem okuma ucu **aynı**
metodu çağırır. İki yerde ayrı ayrı `if` yazmak, ekranın "açık" derken API'nin "kapalı" demesinin
garantili yoludur (PLAN-V4 §22.3'ün öğrettiği ders).

Yeni DTO — `Business/DTOs/ClubApplications/ClubApplicationWindowDto.cs`:

| Alan | Ne için |
|---|---|
| `IsOpen` | Arayüz düğmeyi gösterir/gizler |
| `StartUtc`, `EndUtc` | "3 Ekim'de açılıyor" metnini arayüz kurar |
| `Override` | Admin ekranı mevcut durumu gösterir |
| `TermName` | Hangi dönemin penceresi |

### 31.3 Muhafız

`SubmitAsync`, güncel dönemi bulduktan **hemen sonra**:

```
window = Evaluate(term, clock.UtcNow)
!window.IsOpen ise → Result.Conflict(Messages.ClubApplicationsClosed)
```

Mesaj metni sebebi ve nereye bakılacağını söyler:
*"Topluluk kurma başvuruları şu anda kapalı. Başvuru takvimini Başvurularım sayfasından
görebilirsiniz."*
Kesin tarihler okuma ucundan gelir — Y-29 (mesaj tek yerde) ile Y-25 (nötr mesaj) birlikte korunur.

**Pencere yalnızca gönderimi kapatır.** `DecideAsync` hiç değişmez: admin kapalı dönemde de bekleyen
başvuruları karara bağlar.

### 31.4 Uçlar

| Uç | Yetki | Not |
|---|---|---|
| `GET /api/club-applications/window` | Giriş yapmış kullanıcı | **Anonim yüzeye eklenmez** (A-42: `/api/public/*` dar kalır) |
| `PUT /api/academic-terms/{id}/club-application-window` | `reference.manage` | Üç alanı birlikte yazar |

Yeni izin **yok**. `reference.manage` zaten dönem yönetiminin izni.

### 31.5 Arayüz

| Yer | Değişiklik |
|---|---|
| `ReferenceDataPage` → Dönem sekmesi | Satır başına "Başvuru Takvimi" düzenleme diyaloğu: iki tarih + üç seçenekli override + mevcut durumun rozeti |
| `ClubsPage` → "Topluluk Kurmak İstiyorum" | Kapalıysa düğme pasif + sebep metni |
| `MyApplicationsPage` → Topluluk Kurma sekmesi | Pencere durumu ve tarihleri her zaman görünür |

Düğmeyi gizlemek yetki değildir (Y-35): API muhafızı 409 döndürmeye devam eder.

### 31.6 Testler

| Test | Nerede |
|---|---|
| `FollowSchedule` + aralık içi → başvuru kabul | `ClubApplicationFlowTests` |
| `FollowSchedule` + aralık dışı → 409 | `ClubApplicationFlowTests` |
| `FollowSchedule` + tarih yok → 409 (fail-closed) | `ClubApplicationFlowTests` |
| `ForceOpen` + aralık dışı → kabul | `ClubApplicationFlowTests` |
| `ForceClosed` + aralık içi → 409 | `ClubApplicationFlowTests` |
| Kapalıyken `DecideAsync` çalışmaya devam eder | `ClubApplicationFlowTests` |
| Okuma ucu ile muhafız **aynı** cevabı verir (5 durumun hepsinde) | `ClubApplicationFlowTests` |
| Bitiş < başlangıç → 400 | `ValidationRulesTests` |

Son iki test önemli: biri ekran/API ayrışmasını, diğeri anlamsız pencere tanımını kapatır.

**Bitti sayılır:** admin takvim tanımlayabiliyor; aralık dışında başvuru 409 alıyor ve mesaj sebebi
söylüyor; "zorla aç" tarih dışında çalışıyor; "zorla kapat" tarih içinde çalışıyor; arayüzün
gösterdiği durum ile API'nin kararı beş senaryoda da aynı.

---

## Faz 32 — Topluluk kategorisi (K-35, A-60)

Referans verisi deseninin dördüncü uygulaması (fakülte, bölüm, dönem, **kategori**). Faz 33'ün evrak
tipi kataloğunun ısınma turu — aynı CRUD, aynı 409, aynı sekme yapısı.

### 32.1 Şema

`Entities/ClubCategory.cs` : `IEntity` — `Id`, `Name`. `Faculty` ile birebir aynı biçim.
`Club.ClubCategoryId` (`int?`), `ClubApplication.ProposedCategoryId` (`int?`); FK'lar `Restrict`.
`ClubCategory.Name` üzerinde unique index.

Migration: `20260826_Faz32_ToplulukKategorisi`.

### 32.2 Servis ve uçlar

`IReferenceDataService`'e dört metot; yeni `ClubCategoriesController`:

| Uç | Yetki |
|---|---|
| `GET /api/club-categories` | `clubs.read` — kategori seçici her başvuru sahibine lazım |
| `POST /api/club-categories` | `reference.manage` |
| `PUT /api/club-categories/{id}` | `reference.manage` |
| `DELETE /api/club-categories/{id}` | `reference.manage` — kullanımdaysa **409** |

**Cache dikkat (Y-45):** kategori yazma uçları `[CacheRemoveAspect("ClubManager.", "PublicContentManager.")]`
taşır. Kategori adı kulüp listesi DTO'sunda göründüğü için, düşürülmeyen cache eski adı servis eder.

### 32.3 Filtreleme

`IClubService.GetListPagedAsync` ve `PublicContentManager.GetClubsAsync` `int? categoryId` alır.
Filtre **SQL'de** (A-50/Y-62) — istemci tarafında ayıklama yok. Sıralama Y-64'e tabi.

### 32.4 Arayüz

| Yer | Değişiklik |
|---|---|
| `ReferenceDataPage` | Yeni "Topluluk Kategorileri" sekmesi (Fakülteler sekmesinin kopyası) |
| `schemas/clubForm.ts` | `clubCategoryId` (opsiyonel) + kulüp oluştur/düzenle diyaloglarına seçici |
| `schemas/clubApplicationForm.ts` | `proposedCategoryId` (opsiyonel) — ekran görüntüsündeki "Kategori" alanı |
| `ClubsPage`, `PublicClubsPage` | Kategori filtresi (sunucu taraflı) |
| `ClubDetailPage`, `PublicClubDetailPage` | Kategori rozeti |

### 32.5 Testler

| Test | Nerede |
|---|---|
| Aynı adla ikinci kategori üretilmez | `ReferenceDataManagerTests` |
| Kullanımdaki kategori silinemez (409) | `ReferenceDataManagerTests` |
| Kullanılmayan kategori silinir | `ReferenceDataManagerTests` |
| Kategori filtresi doğru kulüpleri döner | `ClubSearchPagingTests` |
| Kategori adı değişince kulüp listesi yeni adı gösterir (cache düştü) | `ReferenceDataEndpointTests` |

Son test Y-45'in kategori karşılığı — `CacheRemoveAspect` unutulursa kırmızıya döner.

**Bitti sayılır:** admin kategori tanımlayabiliyor; kulüp ve başvuru formunda seçilebiliyor; kulüp
listesi kategoriye göre filtrelenebiliyor; kullanımdaki kategori silinemiyor; kategori adı
değiştiğinde liste anında yeni adı gösteriyor.

---

## Faz 33 — Topluluk kuruluş evrakları (K-37, A-62, A-63, A-64, Y-70, Y-71)

V6'nın en büyük fazı. Core'a dokunan tek faz. Üç adımda ilerler: **önce Core, sonra katalog, sonra
akış** — her adım kendi başına derlenip test edilebilir.

### 33.1 Core: PDF imzası ve tip kümesi (O-23)

| Dosya | Değişiklik |
|---|---|
| `Core/Utilities/Files/DetectedFileType.cs` | `Pdf` değeri + `application/pdf` + `.pdf` |
| `Core/Utilities/Files/FileSignatureInspector.cs` | `%PDF-` (`0x25 0x50 0x44 0x46 0x2D`) imzası |
| `Business/Concrete/FileManager.cs` | `StoreFileAsync` **izin verilen tip kümesini parametre alır** |

Mevcut iki çağrı (`UploadClubLogoAsync`, `UploadEventPosterAsync`) görsel kümesini geçer. Evrak yolu
yalnızca PDF geçer. Bu parametre olmadan §Context'teki 2 numaralı bulgu gerçekleşir.

`Messages.UnsupportedFileType` bugün **"Yalnızca JPEG, PNG veya WebP yüklenebilir."** diyor; artık
iki farklı bağlam var, ikinci bir sabit gerekiyor (`UnsupportedDocumentFileType` → "Yalnızca PDF").

### 33.2 Şema

| Varlık | Alanlar |
|---|---|
| `ClubDocumentType` : `IEntity` | `Id`, `Code`, `Name`, `IsRequired`, `IsActive`, `DisplayOrder` · `Code` unique |
| `ClubApplicationDocument` : `IEntity` | `Id`, `ClubApplicationId`, `ClubDocumentTypeId`, `StoredFileId` · `(ClubApplicationId, ClubDocumentTypeId)` unique |

`ClubDocumentType` hard delete edilebilir (referans verisi, A-12) ama **kullanımdaysa 409**.
`IsActive = false` yapmak, geçmiş başvuruları bozmadan bir formu yürürlükten kaldırmanın yoludur.

Migration: `20260826_Faz33_KurulusEvraklari` — sekiz FR formu `HasData` ile (O-21 tablosu).

### 33.3 Katalog yönetimi

`IReferenceDataService`'e dört metot + `ClubDocumentTypesController`:

| Uç | Yetki |
|---|---|
| `GET /api/club-document-types` | `clubs.read` — başvuru formu katalogdan render edilir |
| `POST` / `PUT` / `DELETE` | `reference.manage` |

`ReferenceDataPage`'e "Kuruluş Evrakları" sekmesi: kod, ad, zorunlu (switch), aktif (switch), sıra.

### 33.4 Başvuru gönderimi multipart olur (O-22)

**Katman kuralı:** `IFormFile` Business'a **girmez** (Y-09, Y-05). `FilesController`'ın bugünkü
deseni sürer — controller stream'i açar, Business ilkel bir DTO alır.

| Katman | Tip |
|---|---|
| WebAPI | `WebAPI/Models/SubmitClubApplicationForm.cs` — `[FromForm]` bağlama modeli, `IFormFile` burada kalır |
| Business (giriş) | `SubmitClubApplicationRequestDto` + `IReadOnlyList<ClubApplicationDocumentUploadDto>` (`DocumentTypeId` + mevcut `UploadFileRequestDto`) |

`RequestSizeLimit` / `RequestFormLimits`: evrak başına 5 MB, toplam 60 MB (12 evrak × 5 MB).

**İş kuralı (Y-71):** `SubmitAsync`, `IsActive && IsRequired` olan **her** evrak tipi için dosya
gelmiş mi diye katalogu okuyarak kontrol eder. Eksikse `ValidationError` + hangi evrakların eksik
olduğu. Bu kontrol FluentValidation'a **konulamaz** — katalog DB'den okunur, biçimsel doğrulama
değil iş kuralıdır (Y-03, aspect kataloğundaki "bu isimde topluluk var mı" örneğinin aynısı).

Sıra: pencere muhafızı (Faz 31) → ad çakışması → danışman → **evrak bütünlüğü** → dosyaları yaz →
tek `SaveChanges`.

**Disk/DB ayrışması:** dosyalar diske `SaveChanges`'ten önce yazılır; transaction geri alınırsa
`StoredFile` satırları gider ama disk dosyaları kalır. `NightlyMaintenanceJob`'a **sahipsiz dosya
temizliği** eklenir: yükleme klasöründe olup `StoredFile` tablosunda karşılığı olmayan dosyalar
silinir. Rapor dosyası temizliğinin yanına, aynı işe.

### 33.5 İnceleme ekranında görüntüleme

`ClubApplicationListItemDto`'ya `Documents` listesi eklenir: `DocumentTypeId`, `Code`, `Name`,
`IsRequired`, `DocumentId`, `OriginalFileName`, `FileSizeBytes`.

`ClubApplicationsReviewPage` satırı genişletilebilir olur; her evrak için ad + indirme düğmesi.
Zorunlu ama yüklenmemiş evrak (eski başvurular, `IsRequired` sonradan açılmış tipler) **eksik**
rozetiyle görünür — inceleyici sebebi görebilmeli.

### 33.6 Korumalı indirme (O-24)

`GET /api/club-applications/{applicationId}/documents/{documentId}`

| Adım | Kural |
|---|---|
| Yetki | Başvuran öğrenci **veya** `clubs.write` / `clubs.manage.all` |
| Ne zaman | **İndirme anında yeniden** — Y-51'in evrak karşılığı |
| Cevap | `File(stream, contentType)`, **`Cache-Control` yok** (`FilesController`'daki 24 saatlik açık görsel önbelleği burada yasaktır) |
| Anonim uç | `GetPublicFileAsync` `Visibility == Public` filtresiyle bu kaydı **zaten** göremez (Y-52) — ikinci savunma katmanı |

Arayüz indirmeyi `axios` blob + geçici nesne URL'i ile yapar (A-36) — `<a href>` `Authorization`
başlığı gönderemez.

### 33.7 Başvuru formu sayfaya taşınır

Bugün `ClubsPage` içinde bir diyalog. Sekiz evrak alanı diyaloğa sığmaz. Yeni rota `/topluluk-kur`,
ekran görüntüsündeki düzen: **Topluluk Bilgileri** (ad, kategori, açıklama, danışman, logo) →
**Zorunlu Evraklar** (katalogdan render edilen kartlar, zorunlular `*` ile) → tek gönder düğmesi.

`ClubsPage`'deki düğme bu rotaya yönlendirir. Diyalog kaldırılır (iki yerde iki form bakımı Y-29'un
"tekrarlama" gerekçesiyle aynı sınıf).

### 33.8 Saklama temizliği (O-24)

`NightlyMaintenanceJob`'a ikinci adım: **reddedilmiş** ve karar tarihi 90 günden eski başvuruların
evrakları silinir — disk dosyası, `StoredFile` satırı ve `ClubApplicationDocument` satırı. Başvuru
kaydının kendisi durur (Y-16: olay kaydı silinmez).

İş idempotent (Y-47): ikinci çalıştırma sıfır satır işler.

### 33.9 Testler

| Test | Nerede | Neyi korur |
|---|---|---|
| `%PDF-` imzası `Pdf` döner; bozuk başlık `Unknown` | `FileSignatureInspectorTests` | O-23 |
| **Logo ucuna PDF yüklenince 400** | `FileEndpointTests` | §Context bulgu 2 — regresyon muhafızı |
| Evrak ucuna JPEG yüklenince 400 | `ClubApplicationFlowTests` | O-23 |
| Eksik zorunlu evrakla başvuru 400 + eksik listesi | `ClubApplicationFlowTests` | Y-71 |
| `IsActive = false` evrak tipi zorunlu sayılmaz | `ClubApplicationFlowTests` | O-21 |
| Tam evrakla başvuru kabul; evrak sayısı doğru | `ClubApplicationFlowTests` | O-22 |
| Başvuru reddedilirse (400) **hiç** `StoredFile` satırı kalmaz | `ClubApplicationFlowTests` | O-22 atomiklik |
| Evrak `Protected` görünürlükle kaydedilir | `ClubApplicationFlowTests` | Y-70 |
| Anonim `/api/files/{id}` ile evrak indirilemez (404) | `PublicSurfaceLeakTests` | Y-70 |
| Başka bir öğrenci evrağı indiremez (403) | `ClubApplicationFlowTests` | O-24 |
| Başvuran ve `clubs.manage.all` indirir | `ClubApplicationFlowTests` | O-24 |
| 90 günü geçmiş reddedilmiş başvurunun evrakları silinir; onaylananınki durur | `MaintenanceManagerTests` | O-24 |
| Sahipsiz disk dosyası temizlenir | `MaintenanceManagerTests` | §33.4 |
| Kullanımdaki evrak tipi silinemez (409) | `ReferenceDataManagerTests` | §33.2 |

**Bitti sayılır:** öğrenci `/topluluk-kur` sayfasından sekiz zorunlu evrağı PDF olarak yükleyip tek
düğmeyle başvuruyor; eksik evrakla başvuru reddediliyor ve hangi evrağın eksik olduğu söyleniyor;
admin inceleme ekranında her evrağı açıp okuyabiliyor; aynı evrak anonim uçtan **indirilemiyor**;
kulüp logosuna PDF yüklenemiyor.

---

## Faz 34 — Dinamik topluluk içi roller (K-36, A-61, Y-69)

Yetki yüzeyine en yakın faz, o yüzden en sonda. **Sözü:** `Ensure*Access*` metotlarının hiçbirine
tek satır eklenmez.

### 34.1 Şema

`Entities/ClubRoleDefinition.cs` : `IEntity`

| Alan | Not |
|---|---|
| `ClubId` | Kulübe özel (O-20) |
| `Name` | Unvan — `(ClubId, Name)` unique |
| `ClubRole` | Yetki seviyesi — mevcut enum |
| `DisplayOrder` | Listeleme sırası |

`ClubMembership.ClubRoleDefinitionId` (`int?`). Mevcut `ClubRole` alanı **durur ve yetkinin tek
kaynağı olarak kalır.**

Migration: `20260826_Faz34_DinamikToplulukRolleri` — **mevcut her kulübe varsayılan beş tanım**
yazan veri adımıyla birlikte (O-20). Bu adım olmadan mevcut kulüplerin rol sekmesi boş açılır.

### 34.2 Y-69: rol tanımı izin taşımaz

Yeni yasak kuralın gerekçesi Y-37'nin aynısı: Identity'nin yanına **ikinci bir yetki sistemi**
açmak. `ClubRoleDefinition` yalnızca ilkel alanlar + `ClubRole` enum'ı taşır; izin kodu, claim,
`RoleClaim` referansı taşımaz.

Mimari test (`Architecture.Tests`): `ClubRoleDefinition` tipinin property'leri ilkel tipler ve
`ClubRole` dışında bir tip içeremez. Belgede kalan kural ihlal edilir, testte kalan edilmez.

### 34.3 İki alanın ayrışmaması

| Yazma | Kural |
|---|---|
| Üyeye unvan atama | `ClubRoleDefinitionId` ve `ClubRole` **birlikte** yazılır; `ClubRole` daima tanımdan okunur, istemciden değil (Y-22) |
| Tanımın seviyesini değiştirme (O-27) | O unvanı taşıyan tüm üyeliklerin `ClubRole`'ü aynı transaction'da güncellenir |
| İkinci başkan üretecek değişiklik | Business önden yakalar → **409**; yakalayamazsa A-39 unique index'i zaten reddeder |
| Kullanımdaki tanımı silme | **409** — önce üyeler başka unvana taşınmalı |

`SetClubRoleRequestDto` `ClubRoleDefinitionId` alır. Doğrudan `ClubRole` göndermek de **desteklenmeye
devam eder** (unvansız atama) — mevcut testler ve akışlar kırılmaz.

### 34.4 Uçlar

| Uç | Yetki |
|---|---|
| `GET /api/clubs/{clubId}/role-definitions` | `EnsureMemberViewAccessAsync` (danışman / Officer / President / yönetici) |
| `POST` / `PUT` / `DELETE .../role-definitions/{id}` | `EnsureRoleManagementAccessAsync` (danışman / President / yönetici) |

Yeni izin kodu **yok**. Kulüp içi rol yönetimi kulüp kapsamının parçası — `clubs.manage.all` zaten
yöneticinin anahtarı (A-55, Y-66).

### 34.5 Arayüz

| Yer | Değişiklik |
|---|---|
| `ClubDetailPage` | Yeni "Roller" sekmesi — tanım listesi + CRUD, yalnızca yetkiliye |
| Üye listesi | "Rol" kolonu unvanı gösterir; unvan yoksa yetki seviyesine düşer |
| Rol atama diyaloğu | Unvan seçici; seçilen unvanın yetki seviyesi yardımcı metin olarak görünür |
| `MyClubsPage` | Unvan gösterir |

Yetki seviyesinin unvanın yanında görünmesi kasıtlı: "Sayman" unvanını veren kişi, bunun aynı zamanda
Officer yetkisi verdiğini **görmeden** vermemeli.

### 34.6 Testler

| Test | Nerede | Neyi korur |
|---|---|---|
| Unvan atanınca `ClubRole` tanımdan gelir | `ClubMemberManagerTests` | §34.3 |
| İstemcinin gönderdiği `ClubRole` yok sayılır | `ClubMemberManagerTests` | Y-22 |
| Tanımın seviyesi değişince üyelerin `ClubRole`'ü de değişir | `ClubMemberManagerTests` | O-27 |
| İkinci başkan üretecek seviye değişikliği 409 | `ClubMemberManagerTests` | A-39 |
| Kullanımdaki tanım silinemez (409) | `ClubMemberManagerTests` | §34.3 |
| A kulübünün tanımı B kulübünde kullanılamaz | `ClubMemberManagementTests` | O-20 |
| Yeni kulüp beş varsayılan tanımla doğar | `ClubApplicationFlowTests` | O-20 |
| Unvanlı President hâlâ etkinlik oluşturabiliyor | `EventManagerTests` | **Yetki yüzeyi bozulmadı** |
| `ClubRoleDefinition` izin/claim tipi taşımaz | `Architecture.Tests` | Y-69 |
| Yetkisiz üye rol tanımı oluşturamaz (403) | `ClubMemberManagementTests` | §34.4 |

Sondan üçüncü test bu fazın sözünü tutuyor mu diye bakar: unvan katmanı eklendikten sonra **yetki
davranışı birebir aynı** kalmalı.

**Bitti sayılır:** başkan kendi kulübüne "Sayman" unvanı tanımlayıp bir üyeye atayabiliyor; o üye
Officer yetkisi kazanıyor; başka kulüp bu unvanı göremiyor; kullanımdaki unvan silinemiyor; V5'ten
gelen tüm yetki testleri değişmeden yeşil.

---

## Kapsam dışı bırakılanlar

Bunlar ekran görüntüsünde var veya konuşuldu ama **bu plana alınmadı.** Sonradan istenirse ayrı faz
olur.

| Konu | Neden |
|---|---|
| **Üç aşamalı onay** (danışman → kurul → admin) | Ekran görüntüsünün metninde var. `ClubApplication` durum makinesini yeniden kurmak demek — `ApplicationStatus`'e ara durumlar, her aşamaya ayrı yetki, ayrı bildirim. Bugünkü tek aşamalı admin onayı korunuyor |
| Kulübün kuruluş evraklarını **kulüp detay sayfasında** göstermek | Evraklar başvuruya bağlı kalıyor; onaydan sonra kulüp sayfasında ayrı bir "Belgeler" sekmesi istenmedi |
| **Üyelik** başvurularına takvim | Sen "kulüp kurma başvuruları" dedin; `MembershipApplication` etkilenmiyor |
| Rol tanımlarının **izin taşıması** | Y-69 ile açıkça yasaklandı (Y-37'nin kulüp karşılığı) |
| Kategori/rol/evrak tipi için **çok dillilik** | K-11 hâlâ V1 dışı |
| Evrakların **KVKK silme/anonimleştirme akışı** | K-19 V1 dışı. O-24'ün 90 günlük saklama sınırı bunun yerine geçmez, yalnızca yüzeyi sınırlar |

---

## Yeni madde numaraları

`docs/MIMARI.md` v6.0 ile eklenenler:

| Tür | Numaralar |
|---|---|
| **Kapsam** | K-35 (kategori) · K-36 (dinamik roller) · K-37 (kuruluş evrakları) · K-38 (etkinlik kitlesi) · K-39 (başvuru takvimi) |
| **Karar** | A-60 … A-66 |
| **Yasak** | Y-69 … Y-73 |

---

## Doğrulama

Her fazın sonunda, PLAN-V5'in kapanış yordamı:

1. `dotnet build` — sıfır uyarı (nullable uyarıları zaten hata, Y-31)
2. `dotnet test` — V5'ten gelen testlerin **tamamı** yeşil kalmalı; bu planın hiçbir fazı mevcut bir
   testi değiştirmek zorunda bırakmamalı. Bırakıyorsa, kural değişmiş demektir: önce `MIMARI.md`
3. `npm run build && npm run lint` (`arayuz/`)
4. `git diff --stat` — faz tanımının dışına taşan dosya var mı
5. Faz commit'i: `Faz NN: <başlık> (K-xx, A-yy, Y-zz)`

**Faz 33 için ek adım:** `Architecture.Tests` içindeki katman testleri, `IFormFile`'ın Business'a
sızmadığını doğrulamalı (Y-05/Y-09). Yeni bir test gerekmiyorsa mevcut `LayerDependencyTests` zaten
yakalar — doğrulanacak.

---

*Uygulama planı v6.0 · 5 faz, 5 kapsam maddesi, 7 karar, 5 yasak kural · referans: docs/MIMARI.md v6.0*
