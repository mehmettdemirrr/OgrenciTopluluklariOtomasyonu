# Faz 42 — Etkinlik Detay Sayfası Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Etkinlik detayı, referans tasarımdaki iki sütunlu düzene geçsin — solda afiş ve biçimlendirilmiş açıklama, sağda düzenleyen topluluk kartı, zaman çizelgesi, konum + yol tarifi, görüntülenme/katılımcı/kontenjan istatistikleri ve duruma göre birincil eylem; ayrıca **anonim ziyaretçi** de `/etkinlikler/:id` adresinden yayınlanmış ve herkese açık bir etkinliğin detayını görebilsin.

**Architecture:** Düzen **tek bir bileşende** (`EventDetailLayout`) yaşar; hem anonim vitrin sayfası hem panel sayfası onu giydirir, aralarındaki fark yalnızca birincil eylem alanı (anonimde "Giriş Yap", panelde katıl/iptal) ve panele özgü yönetim bölümleridir. Anonim uç `PublicContentController` içindeki tek anonim yüzeye eklenir (A-42) ve vitrin listesiyle **aynı** filtreyi uygular. Görüntülenme sayacı okuma yolunda değil, ayrı bir yazma ucundadır ve `RowVersion` çakışması doğurmamak için `ExecuteUpdateAsync` ile tek SQL cümlesinde artırılır.

**Tech Stack:** .NET 8, EF Core 8, React 18 + MUI 9, TanStack Query.

**Spec:** `docs/MIMARI.md` (v6.8 → v6.9 bu fazda). İlgili kararlar: A-42 (anonim yüzey tek dosyada), A-71/A-73 (zengin metin, harita), Y-58 (vitrin sızıntısı), Y-72 (kitle), Y-79 (geri sayım karar vermez), Y-42 (sayım SQL'de).

## Global Constraints

- Belge önce, kod sonra (Task 1).
- Çalışma dalı `master`; faz başına **tek commit**; commit mesajında araç imzası/süreç anlatımı yok.
- **Y-58:** anonim uç yalnızca `Status == Published && Audience == Public` etkinliği döndürür; aksi hâlde `NotFound` (Forbidden değil — varlığı sızdırmaz, `GetClubByIdAsync`'in pasif kulüp precedent'i).
- **Y-42:** katılımcı sayısı SQL'de sayılır: `(await repo.GetListPagedAsync(0, 1, filtre, ct)).TotalCount`. Satır çekip `.Count` almak yasak.
- Y-79: geri sayım yalnızca bilgilendirir; katılım/durum kararları backend alanlarından okunur.
- A-54: yeni anonim detay ucuna `[CacheAspect]` **konmaz** — görüntülenme sayısı ve katılımcı sayısı tek satırlık okumadır, 10 dakikalık bayat sayı göstermek istemiyoruz.
- Y-34: her test kendi verisini tohumlar.
- Test komutları: `dotnet test`; `cd arayuz && npm run build && npm run lint`.

### Bu planın kabul ettiği varsayımlar (kullanıcı onayı alınamadı, oturum "soru sorma" modundaydı)

1. **Kapsam ikisi birden:** ekrandaki tasarım public menülü ve "Giriş Yap" düğmeli, yani anonim bir sayfa. Bizde public etkinlik detayı **yoktu** (`/etkinlikler` listesi hiçbir yere link vermiyordu). Bu faz hem anonim sayfayı ekler hem panel sayfasını aynı düzene taşır. Yalnızca panel istenirse Task 3 ve Task 5 düşülür.
2. **Görüntülenme sayacı dahil:** ekranda "GÖRÜNTÜLENME 201" olduğu için sayaç planlanmıştır (Task 2/3'te izole). Sayaç yaklaşık bir değerdir: oturum başına bir kez sayılır, bot/yenileme trafiği yine de şişirebilir. İstenmezse `ViewCount` alanı ve `/view` ucu (Task 2 Step 3, Task 3 Step 5-7) düşülür, düzen bozulmaz.

### Referans tasarımdan bilinçli bir sapma

Ekranda "🔒 Sadece Topluluk Üyeleri" rozeti **anonim** bir sayfada duruyor: referans sitede üyelere özel etkinlik herkese görünüyor, yalnızca kayıt kısıtlı. Bizde bu mümkün değil — **Y-72/A-65** gereği `Audience == ClubMembers` etkinlikler anonim vitrinde **hiç görünmez**. Bu yüzden rozet yalnızca panel sayfasında (giriş yapmış kullanıcıya) çizilir; anonim sayfada zaten yalnızca herkese açık etkinlikler listelenir. Bu davranışın değişmesi Y-72'yi tadil etmek demektir ve bu fazın kapsamı dışındadır — istenirse ayrı bir faz olarak ele alınmalıdır.

---

### Task 1: MIMARI'ye kapsam, karar ve kuralı yaz (belge önce)

**Files:**
- Modify: `docs/MIMARI.md`

**Interfaces:**
- Produces: K-46, A-76, A-77, Y-82.

- [ ] **Step 1: Sürüm ve sayaçlar**

Başlıktaki `**Sürüm:** v6.8` etiketini `v6.9` yap ve kronolojik listenin sonuna kalın olarak ekle:

```markdown
· v6.8: 5 Eylül 2026 (K-45, A-75, Y-81, Faz 41 — üye olmayanın kulüp görünürlüğü) · **v6.9: 6 Eylül 2026 (K-46, A-76, A-77, Y-82, Faz 42 — etkinlik detay sayfası)**
```

Giriş paragrafının sonuna ekle:

```markdown
**v6.9** etkinlik detayını iki sütunlu düzene taşıyıp anonim ziyaretçiye açarak 1 kapsam
maddesi (K-46), 2 karar (A-76, A-77) ve 1 kural (Y-82) ekledi.
```

Sayaç tablosunu güncelle: `Karar | 77 (… + 2 v6.9)`, `Yasak kural | 82 (… + 1 v6.9)`, `Uygulama fazı | 42 (… + 1 v6.9)`.
İçindekiler aralıklarını güncelle: `Y-01 … Y-82`, `K-01 … K-46`, `A-01 … A-76` → `A-01 … A-77`.

- [ ] **Step 2: K-46 satırını V6 kapsam tablosuna ekle (K-45 satırının altına)**

```markdown
| **K-46** | **Etkinlik detay sayfası** | Etkinlik detayı iki sütundur: solda afiş ve biçimlendirilmiş açıklama, sağda düzenleyen topluluk, zaman çizelgesi, konum + yol tarifi, görüntülenme/katılımcı/kontenjan sayıları ve birincil eylem. Anonim ziyaretçi de yayınlanmış ve herkese açık etkinliğin detayını görür; katılmak için giriş yapması istenir | `EventDetailLayout` (tek düzen, iki kabuk), `GET /api/public/events/{id}`, `EventListItemDto.ParticipantCount`/`ClubLogoFileId`, `Event.ViewCount` (A-76, A-77, Y-82) |
```

- [ ] **Step 3: A-76 satırını karar kaydına ekle (A-75 satırının altına)**

```markdown
| **A-76** | Etkinlik detay **düzeni tek bileşendir**, sayfa onu giydirir | A | Afiş, açıklama kartı ve sağ sütun (düzenleyen/zaman/konum/istatistik) `arayuz/src/components/events/EventDetailLayout.tsx` içinde bir kez yazılır; anonim sayfa (`/etkinlikler/:id`) ve panel sayfası (`/events/:id`) onu sarar. Fark yalnızca **birincil eylem alanı** (`primaryAction` slotu: anonimde "Giriş Yap", panelde katıl/iptal) ve panele özgü ek bölümlerdir (katılımcı listesi, düzenleme/iptal). Gerekçe: A-73'ün harita yardımcılarında öğrendiğimiz ders — aynı görünümün iki kopyası, biri güncellenirken öbürünün unutulması demektir (K-46) |
```

- [ ] **Step 4: A-77 satırını karar kaydına ekle**

```markdown
| **A-77** | Görüntülenme sayacı **ayrı bir yazma ucudur**, okuma yolunda artmaz | A | Detay ucu (`GET`) sayacı **artırmaz**; arayüz sayfayı açtığında ayrı bir `POST /api/public/events/{id}/view` çağırır ve bunu `sessionStorage` ile oturum başına bir kereye indirir. Artırma `IEventViewDal.IncrementAsync` içinde `ExecuteUpdateAsync` ile **tek SQL cümlesidir**: `Event.RowVersion` taşıdığı için oku-değiştir-kaydet döngüsü eşzamanlı okumalarda `ConcurrencyConflictException` üretirdi (A-15/Y-53). Sayaç **yaklaşıktır** ve bir karar dayanağı değildir; kontenjan ve katılım kararları hâlâ yalnızca katılım ucundan gelir. Gerekçe: GET'in yan etkisi olmaması hem önbelleklenebilirliği hem "okumak veriyi değiştirmez" beklentisini korur (K-46) |
```

- [ ] **Step 5: Y-82 satırını yasak tablosuna ekle (Y-81 satırının altına)**

```markdown
| **Y-82** | Anonim etkinlik detay ucundan taslak/onay bekleyen/reddedilen ya da `ClubMembers` kitleli bir etkinliği döndürmek; "bulunamadı" yerine 403 dönerek varlığını sızdırmak | `PublicContentManager.GetEventByIdAsync` vitrin listesiyle **aynı** filtreyi uygular (`Status == Published && Audience == Public`); eşleşmeyen her istek `NotFound` döner. Gerekçe: Y-58 ve Y-72'nin detay ucundaki karşılığı — liste sızdırmıyorken detayın sızdırması, filtrenin iki yerde ayrı yazılmasının klasik sonucudur. `PublicSurfaceLeakTests` bu ucu da tarar (K-46, A-76) |
```

---

### Task 2: Katılımcı sayısı, kulüp logosu ve görüntülenme alanı (panel tarafı)

**Files:**
- Modify: `src/Entities/Event.cs`
- Modify: `src/Business/DTOs/Events/EventListItemDto.cs`
- Modify: `src/Business/Concrete/EventManager.cs`
- Modify: `src/Business/Concrete/EventParticipationManager.cs`
- Create: `src/DataAccess/Migrations/<timestamp>_20260906_Faz42_EtkinlikGoruntulenme.cs` (EF üretir)
- Test: `tests/Business.Tests/EventManagerTests.cs`

**Interfaces:**
- Produces: `Event.ViewCount` (int), `EventListItemDto.ParticipantCount` (int?), `EventListItemDto.ClubLogoFileId` (int?), `EventListItemDto.ViewCount` (int).

- [ ] **Step 1: Başarısız testi yaz**

`tests/Business.Tests/EventManagerTests.cs` sonuna ekle (dosyadaki mevcut mock alan adlarını kullan; `_eventParticipationRepository` yoksa sınıfın başına `private readonly Mock<IEntityRepository<EventParticipation>> _eventParticipationRepository = new();` ekle ve `EventManager` kurucusuna geçir):

```csharp
[Fact(DisplayName = "GetByIdAsync: katılımcı sayısını ve kulüp logosunu doldurur (K-46)")]
public async Task GetByIdAsync_FillsParticipantCountAndClubLogo()
{
    var club = new Club { Id = 3, Name = "Kulüp", AdvisorId = 1, IsActive = true, CreatedAtUtc = DateTime.UtcNow, LogoFileId = 42 };
    var entity = new Event
    {
        Id = 7, ClubId = 3, Title = "Etkinlik", Status = EventStatus.Published, Audience = EventAudience.Public,
        StartDateUtc = DateTime.UtcNow.AddDays(1), EndDateUtc = DateTime.UtcNow.AddDays(1).AddHours(2),
        CreatedAtUtc = DateTime.UtcNow, ViewCount = 11,
    };
    _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
    _clubRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([club]);
    _eventParticipationRepository
        .Setup(r => r.GetListPagedAsync(0, 1, It.IsAny<Expression<Func<EventParticipation, bool>>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new PagedResult<EventParticipation>([], 5, 0, 1));

    var result = await _sut.GetByIdAsync(7);

    Assert.True(result.IsSuccess);
    Assert.Equal(5, result.Data!.ParticipantCount);
    Assert.Equal(42, result.Data.ClubLogoFileId);
    Assert.Equal(11, result.Data.ViewCount);
}
```

- [ ] **Step 2: Çalıştır, başarısız olduğunu gör**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~GetByIdAsync_FillsParticipantCount"
```
Beklenen: derleme hatası (`ViewCount`/`ParticipantCount` yok).

- [ ] **Step 3: `Event`'e sayaç alanını ekle**

`src/Entities/Event.cs` içinde `PosterFileId`'nin altına:

```csharp
    /// <summary>
    /// docs/MIMARI.md · K-46/A-77: yaklaşık görüntülenme sayısı. Yalnızca
    /// `IEventViewDal.IncrementAsync` (tek SQL UPDATE) artırır; okuma yolu dokunmaz.
    /// </summary>
    public int ViewCount { get; set; }
```

- [ ] **Step 4: DTO alanlarını ekle**

`src/Business/DTOs/Events/EventListItemDto.cs` içinde `PosterFileId`'nin altına:

```csharp
    /// <summary>docs/MIMARI.md · K-46: düzenleyen topluluğun logosu (kart görseli). Null = logosuz kulüp.</summary>
    public int? ClubLogoFileId { get; set; }

    /// <summary>
    /// docs/MIMARI.md · K-46: bu etkinliğe kayıtlı kişi sayısı — isim/öğrenci no taşımaz (Y-58).
    /// <b>null</b> = bu listede hesaplanmadı (IsRegistered ile aynı sözleşme): yalnızca detay ucu doldurur,
    /// liste uçları sayfa başına N sorgu açmasın diye boş bırakır.
    /// </summary>
    public int? ParticipantCount { get; set; }

    /// <summary>docs/MIMARI.md · A-77: yaklaşık görüntülenme sayısı; karar dayanağı değildir.</summary>
    public int ViewCount { get; set; }
```

- [ ] **Step 5: `EventManager`'da doldur**

`EventManager` kurucusuna `IEntityRepository<EventParticipation> eventParticipationRepository` parametresini ekle (zaten varsa tekrar ekleme). `MapToDto`/`MapWithClubNamesAsync` içinde `ClubLogoFileId` ve `ViewCount`'u kulüp sözlüğünden/varlıktan taşı; `GetByIdAsync` içinde DTO kurulduktan sonra:

```csharp
        // Y-42: sayım SQL'de — TotalCount okunur, satırlar çekilmez.
        dto.ParticipantCount = (await eventParticipationRepository
            .GetListPagedAsync(0, 1, p => p.EventId == eventId, cancellationToken)
            .ConfigureAwait(false)).TotalCount;
```

- [ ] **Step 6: İKİNCİ yapım noktasını güncelle**

`EventListItemDto` iki yerde kurulur. İkincisini bul ve `ClubLogoFileId` + `ViewCount` alanlarını orada da doldur (`ParticipantCount` null kalır — sözleşme bu):

```bash
grep -rn "new EventListItemDto" src/Business
```
Beklenen iki dosya: `src/Business/Concrete/EventManager.cs` ve `src/Business/Concrete/EventParticipationManager.cs`. İkincisinde zaten Faz 30'dan kalma bir uyarı yorumu var; alan eklemeyi atlarsan "kulüplerim/etkinliklerim" listesinde logo sessizce kaybolur.

- [ ] **Step 7: Migration üret ve uygula**

```bash
dotnet ef migrations add 20260906_Faz42_EtkinlikGoruntulenme --project src/DataAccess
dotnet ef database update --project src/DataAccess
dotnet build
```
**Not:** `--startup-project src/WebAPI` EKLEME — WebAPI `Microsoft.EntityFrameworkCore.Design` referansı taşımaz; tasarım zamanı fabrikası `src/DataAccess/AppDbContextFactory.cs`'tedir.

- [ ] **Step 8: Testleri çalıştır**

```bash
dotnet test tests/Business.Tests
```
Beklenen: PASS (mevcut `EventManager`/`EventParticipationManager` testleri kurucu değişikliği yüzünden güncellenmiş olmalı).

---

### Task 3: Anonim etkinlik detayı ve görüntülenme ucu

**Files:**
- Create: `src/Business/DTOs/Public/PublicEventDetailDto.cs`
- Create: `src/DataAccess/Repositories/IEventViewDal.cs`
- Create: `src/DataAccess/Repositories/EfEventViewDal.cs`
- Modify: `src/DataAccess/DependencyResolvers/DataAccessAutofacModule.cs`
- Modify: `src/Business/Abstract/IPublicContentService.cs`
- Modify: `src/Business/Concrete/PublicContentManager.cs`
- Modify: `src/WebAPI/Controllers/PublicContentController.cs`
- Test: `tests/WebAPI.IntegrationTests/PublicEventDetailTests.cs`

**Interfaces:**
- Consumes: `Event.ViewCount` (Task 2).
- Produces: `GET /api/public/events/{id}` → `PublicEventDetailDto`, `POST /api/public/events/{id}/view`.

- [ ] **Step 1: Başarısız entegrasyon testlerini yaz**

`tests/WebAPI.IntegrationTests/PublicEventDetailTests.cs` — tohumlama desenini `tests/WebAPI.IntegrationTests/ClubDetailVisibilityTests.cs`'ten birebir kopyala (fakülte → bölüm → dönem → danışman → kulüp), sonra:

```csharp
[Fact(DisplayName = "Anonim ziyaretçi yayınlanmış ve herkese açık etkinliğin detayını görür (K-46)")]
public async Task PublicEventDetail_ReturnsPublishedPublicEvent()
{
    var (clubId, eventId) = await SeedEventAsync("pubdetail-ok", EventStatus.Published, EventAudience.Public);

    var response = await _client.GetAsync($"/api/public/events/{eventId}");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var body = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.Equal(eventId, body.GetProperty("id").GetInt32());
    Assert.Equal(clubId, body.GetProperty("clubId").GetInt32());
    Assert.Equal(0, body.GetProperty("participantCount").GetInt32());
}

[Fact(DisplayName = "Y-82: taslak etkinliğin detayı anonim uçtan 404 döner")]
public async Task PublicEventDetail_ReturnsNotFound_ForDraft()
{
    var (_, eventId) = await SeedEventAsync("pubdetail-draft", EventStatus.Draft, EventAudience.Public);

    var response = await _client.GetAsync($"/api/public/events/{eventId}");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}

[Fact(DisplayName = "Y-82: üyelere özel etkinliğin detayı anonim uçtan 404 döner")]
public async Task PublicEventDetail_ReturnsNotFound_ForMembersOnly()
{
    var (_, eventId) = await SeedEventAsync("pubdetail-members", EventStatus.Published, EventAudience.ClubMembers);

    var response = await _client.GetAsync($"/api/public/events/{eventId}");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}

[Fact(DisplayName = "A-77: /view ucu görüntülenme sayısını artırır, GET artırmaz")]
public async Task ViewEndpoint_IncrementsCounter_ButGetDoesNot()
{
    var (_, eventId) = await SeedEventAsync("pubdetail-view", EventStatus.Published, EventAudience.Public);

    var first = await (await _client.GetAsync($"/api/public/events/{eventId}")).Content.ReadFromJsonAsync<JsonElement>();
    Assert.Equal(0, first.GetProperty("viewCount").GetInt32());

    var viewResponse = await _client.PostAsync($"/api/public/events/{eventId}/view", null);
    Assert.Equal(HttpStatusCode.OK, viewResponse.StatusCode);

    var second = await (await _client.GetAsync($"/api/public/events/{eventId}")).Content.ReadFromJsonAsync<JsonElement>();
    Assert.Equal(1, second.GetProperty("viewCount").GetInt32());
}
```

Tohumlama yardımcısı:

```csharp
private async Task<(int ClubId, int EventId)> SeedEventAsync(string suffix, EventStatus status, EventAudience audience)
{
    var clubId = await SeedClubAsync(suffix);      // ClubDetailVisibilityTests.SeedScenarioAsync'ten uyarlanmış
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var entity = new Event
    {
        ClubId = clubId, Title = $"Etkinlik-{suffix}", Status = status, Audience = audience,
        StartDateUtc = DateTime.UtcNow.AddDays(3), EndDateUtc = DateTime.UtcNow.AddDays(3).AddHours(2),
        CreatedAtUtc = DateTime.UtcNow,
    };
    db.Events.Add(entity);
    await db.SaveChangesAsync();
    return (clubId, entity.Id);
}
```

- [ ] **Step 2: Çalıştır, başarısız olduklarını gör**

```bash
dotnet test tests/WebAPI.IntegrationTests --filter "FullyQualifiedName~PublicEventDetailTests"
```
Beklenen: dördü de FAIL (404 — uç yok).

- [ ] **Step 3: DTO'yu yaz**

```csharp
namespace Business.DTOs.Public;

/// <summary>docs/MIMARI.md · K-46/Y-82: anonim etkinlik detayı — katılımcı listesi/öğrenci no yok, yalnızca sayı.</summary>
public sealed class PublicEventDetailDto
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public required string ClubName { get; set; }

    public int? ClubLogoFileId { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    /// <summary>docs/MIMARI.md · A-71: null ise Description düz metin olarak gösterilir.</summary>
    public string? DescriptionJson { get; set; }

    public string? Location { get; set; }

    public DateTime StartDateUtc { get; set; }

    public DateTime EndDateUtc { get; set; }

    public int? Capacity { get; set; }

    public int ParticipantCount { get; set; }

    public int ViewCount { get; set; }

    public int? PosterFileId { get; set; }
}
```

- [ ] **Step 4: Servis sözleşmesini genişlet**

`IPublicContentService`'e ekle (**`[CacheAspect]` YOK** — bkz. Global Constraints):

```csharp
    /// <summary>docs/MIMARI.md · K-46/Y-82: vitrin listesiyle aynı filtre; eşleşmezse NotFound.</summary>
    Task<IDataResult<PublicEventDetailDto>> GetEventByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>docs/MIMARI.md · A-77: sayacı tek SQL cümlesiyle artırır; okuma yolu bunu çağırmaz.</summary>
    Task<IResult> RegisterEventViewAsync(int id, CancellationToken cancellationToken = default);
```

- [ ] **Step 5: Sayaç DAL'ini yaz (RowVersion tuzağı)**

`src/DataAccess/Repositories/IEventViewDal.cs`:

```csharp
namespace DataAccess.Repositories;

/// <summary>
/// docs/MIMARI.md · A-77: görüntülenme sayacı tek SQL UPDATE ile artar. Generic repository'nin
/// oku-değiştir-kaydet döngüsü `Event.RowVersion` yüzünden eşzamanlı isteklerde çakışırdı (A-15/Y-53).
/// </summary>
public interface IEventViewDal
{
    /// <summary>Eşleşen satır yoksa 0 döner (etkinlik yok ya da vitrin filtresine uymuyor).</summary>
    Task<int> IncrementAsync(int eventId, CancellationToken cancellationToken = default);
}
```

`src/DataAccess/Repositories/EfEventViewDal.cs`:

```csharp
using Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

/// <summary>docs/MIMARI.md · A-77/Y-82: filtre burada da uygulanır — gizli etkinliğin sayacı artmaz.</summary>
public sealed class EfEventViewDal(AppDbContext context) : IEventViewDal
{
    public async Task<int> IncrementAsync(int eventId, CancellationToken cancellationToken = default) =>
        await context.Events
            .Where(e => e.Id == eventId && e.Status == EventStatus.Published && e.Audience == EventAudience.Public)
            .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.ViewCount, e => e.ViewCount + 1), cancellationToken)
            .ConfigureAwait(false);
}
```

`DataAccessAutofacModule.Load` içine, diğer DAL kayıtlarının yanına:

```csharp
        builder.RegisterType<EfEventViewDal>()
            .As<IEventViewDal>()
            .InstancePerLifetimeScope();
```

- [ ] **Step 6: `PublicContentManager`'a uygula**

Kurucuya `IEntityRepository<EventParticipation> eventParticipationRepository` ve `IEventViewDal eventViewDal` ekle, sonra:

```csharp
    public async Task<IDataResult<PublicEventDetailDto>> GetEventByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // Y-82: vitrin listesiyle AYNI filtre — eşleşmezse varlığı sızdırmadan NotFound.
        var entity = await eventRepository
            .GetAsync(e => e.Id == id && e.Status == EventStatus.Published && e.Audience == EventAudience.Public, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            return DataResult<PublicEventDetailDto>.NotFound(Messages.EventNotFound);
        }

        var club = await clubRepository.GetAsync(c => c.Id == entity.ClubId, cancellationToken).ConfigureAwait(false);

        // Y-42: sayım SQL'de.
        var participantCount = (await eventParticipationRepository
            .GetListPagedAsync(0, 1, p => p.EventId == id, cancellationToken)
            .ConfigureAwait(false)).TotalCount;

        return DataResult<PublicEventDetailDto>.Success(new PublicEventDetailDto
        {
            Id = entity.Id,
            ClubId = entity.ClubId,
            ClubName = club?.Name ?? string.Empty,
            ClubLogoFileId = club?.LogoFileId,
            Title = entity.Title,
            Description = entity.Description,
            DescriptionJson = entity.DescriptionJson,
            Location = entity.Location,
            StartDateUtc = entity.StartDateUtc,
            EndDateUtc = entity.EndDateUtc,
            Capacity = entity.Capacity,
            ParticipantCount = participantCount,
            ViewCount = entity.ViewCount,
            PosterFileId = entity.PosterFileId,
        });
    }

    public async Task<IResult> RegisterEventViewAsync(int id, CancellationToken cancellationToken = default)
    {
        var affected = await eventViewDal.IncrementAsync(id, cancellationToken).ConfigureAwait(false);
        return affected == 0 ? Result.NotFound(Messages.EventNotFound) : Result.Success();
    }
```

- [ ] **Step 7: Controller uçlarını ekle**

`PublicContentController` (A-42: anonim yüzeyin TEK dosyası) içine `GetEvents`'in altına:

```csharp
    [HttpGet("events/{id:int}")]
    public async Task<IActionResult> GetEventById(int id, CancellationToken cancellationToken)
    {
        var result = await publicContentService.GetEventByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>docs/MIMARI.md · A-77: sayaç ucu; gövde almaz, oturum başına bir kez çağrılır.</summary>
    [HttpPost("events/{id:int}/view")]
    public async Task<IActionResult> RegisterEventView(int id, CancellationToken cancellationToken)
    {
        var result = await publicContentService.RegisterEventViewAsync(id, cancellationToken);
        return result.ToActionResult();
    }
```

- [ ] **Step 8: Testleri çalıştır**

```bash
dotnet test tests/WebAPI.IntegrationTests --filter "FullyQualifiedName~PublicEventDetailTests"
```
Beklenen: dördü de PASS.

- [ ] **Step 9: Sızıntı testine yeni ucu ekle**

`tests/WebAPI.IntegrationTests/PublicSurfaceLeakTests.cs` içindeki anonim yüzey listesine `/api/public/events/{id}` yolunu ekle; yanıtın `studentNumber`, `email`, `participants` gibi alan **taşımadığını** doğrula (dosyadaki mevcut yardımcı ne bekliyorsa onu kullan).

```bash
dotnet test tests/WebAPI.IntegrationTests --filter "FullyQualifiedName~PublicSurfaceLeakTests"
```
**Not:** Bu sınıftaki `GetPublicStats_Anonymous_ReturnsCountsWithoutPii` testi bu fazdan ÖNCE de kırmızıydı (`/api/public/stats` 500 döndürüyor, Faz 36-41 boyunca aynı). Yeni eklediğin test yeşilse görevini yapmıştır; o iki bilinen hata bu fazın sorumluluğunda değildir.

---

### Task 4: Ortak düzen bileşeni ve takvim dosyası yardımcısı

**Files:**
- Create: `arayuz/src/components/events/EventDetailLayout.tsx`
- Create: `arayuz/src/utils/calendar.ts`
- Modify: `arayuz/src/api/types.ts`
- Modify: `arayuz/src/i18n/messages.ts`

**Interfaces:**
- Consumes: `PublicEventDetailDto`, `EventListItemDto.ParticipantCount/ClubLogoFileId/ViewCount` (Task 2-3).
- Produces: `<EventDetailLayout>` bileşeni, `downloadEventIcs(event)`.

- [ ] **Step 1: Tipleri ekle**

`arayuz/src/api/types.ts` — `EventListItemDto` içine:

```ts
  /** docs/MIMARI.md · K-46 */
  clubLogoFileId: number | null
  /** null = bu listede hesaplanmadı (yalnızca detay ucu doldurur). */
  participantCount: number | null
  viewCount: number
```

ve yeni arayüz (`PublicEventListItemDto`'nun altına):

```ts
// src/Business/DTOs/Public/PublicEventDetailDto.cs
export interface PublicEventDetailDto {
  id: number
  clubId: number
  clubName: string
  clubLogoFileId: number | null
  title: string
  description: string | null
  descriptionJson: string | null
  location: string | null
  startDateUtc: string
  endDateUtc: string
  capacity: number | null
  participantCount: number
  viewCount: number
  posterFileId: number | null
}
```

- [ ] **Step 2: i18n anahtarlarını ekle**

`arayuz/src/i18n/messages.ts` içindeki `event` ad alanına (tr ve en için ayrı ayrı):

```ts
    'event.details': 'Etkinlik Detayları',      // en: 'Event Details'
    'event.organizer': 'DÜZENLEYEN',            // en: 'ORGANIZER'
    'event.time': 'Zaman',                      // en: 'Time'
    'event.addToCalendar': 'Takvime ekle',      // en: 'Add to calendar'
    'event.views': 'GÖRÜNTÜLENME',              // en: 'VIEWS'
    'event.participants': 'KATILIMCI',          // en: 'PARTICIPANTS'
    'event.capacityLabel': 'KONTENJAN',         // en: 'CAPACITY'
    'event.unlimited': 'Sınırsız',              // en: 'Unlimited'
    'event.locationMissing': 'Belirtilmemiş',   // en: 'Not specified'
    'event.membersOnlyBadge': 'Sadece Topluluk Üyeleri', // en: 'Club Members Only'
    'event.loginToJoin': 'Giriş Yap',           // en: 'Sign In'
```

- [ ] **Step 3: Takvim yardımcısını yaz**

`arayuz/src/utils/calendar.ts` — yeni bağımlılık yok, dosya istemcide üretilir:

```ts
/** docs/MIMARI.md · K-46: "takvime ekle" tamamen istemci tarafıdır; sunucuya .ics ucu eklenmez. */
function toIcsDate(iso: string): string {
  return new Date(iso).toISOString().replace(/[-:]/g, '').replace(/\.\d{3}/, '')
}

function escapeIcsText(value: string): string {
  return value.replace(/\\/g, '\\\\').replace(/\n/g, '\\n').replace(/,/g, '\\,').replace(/;/g, '\\;')
}

export function buildEventIcs(input: { id: number; title: string; startIso: string; endIso: string; location?: string | null; description?: string | null }): string {
  return [
    'BEGIN:VCALENDAR',
    'VERSION:2.0',
    'PRODID:-//Ogrenci Topluluklari//TR',
    'BEGIN:VEVENT',
    `UID:event-${input.id}@ogrencitopluluklari`,
    `DTSTAMP:${toIcsDate(new Date().toISOString())}`,
    `DTSTART:${toIcsDate(input.startIso)}`,
    `DTEND:${toIcsDate(input.endIso)}`,
    `SUMMARY:${escapeIcsText(input.title)}`,
    input.location ? `LOCATION:${escapeIcsText(input.location)}` : '',
    input.description ? `DESCRIPTION:${escapeIcsText(input.description)}` : '',
    'END:VEVENT',
    'END:VCALENDAR',
  ].filter(Boolean).join('\r\n')
}

export function downloadEventIcs(input: Parameters<typeof buildEventIcs>[0]): void {
  const blob = new Blob([buildEventIcs(input)], { type: 'text/calendar;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = `etkinlik-${input.id}.ics`
  link.click()
  URL.revokeObjectURL(url)
}
```

- [ ] **Step 4: Düzen bileşenini yaz**

`arayuz/src/components/events/EventDetailLayout.tsx`. **MUI 9 uyarısı:** `Stack`/`Typography` üzerinde `alignItems`, `gap`, `flexWrap`, `fontWeight` gibi sistem prop'ları **doğrudan verilmez**, `sx` içine yazılır (Faz 40'ta derleme bu yüzden kırılmıştı); satır aralığı için `spacing` + `useFlexGap` kullan.

```tsx
import { Box, Button, Card, CardContent, CardMedia, Chip, Divider, Grid, IconButton, Stack, Typography } from '@mui/material'
import CalendarMonthOutlinedIcon from '@mui/icons-material/CalendarMonthOutlined'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import LockOutlinedIcon from '@mui/icons-material/LockOutlined'
import NearMeOutlinedIcon from '@mui/icons-material/NearMeOutlined'
import PlaceOutlinedIcon from '@mui/icons-material/PlaceOutlined'
import ScheduleRoundedIcon from '@mui/icons-material/ScheduleRounded'
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined'
import type { ReactNode } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { EventTimeline } from '../ui/EventTimeline'
import { RichTextContent } from '../richtext/RichTextContent'
import { useLocale } from '../../i18n/LocaleContext'
import { downloadEventIcs } from '../../utils/calendar'
import { mapsDirectionsUrl, mapsEmbedUrl } from '../../utils/maps'

export interface EventDetailLayoutProps {
  event: {
    id: number
    clubId: number
    clubName: string
    clubLogoFileId: number | null
    title: string
    description: string | null
    descriptionJson: string | null
    location: string | null
    startDateUtc: string
    endDateUtc: string
    capacity: number | null
    participantCount: number | null
    viewCount: number
    posterFileId: number | null
  }
  /** Kulüp sayfasının adresi — vitrinde /kulupler/:id, panelde /clubs/:id. */
  clubHref: string
  /** Birincil eylem alanı: anonimde "Giriş Yap", panelde katıl/iptal (A-76). */
  primaryAction?: ReactNode
  /** Kitle rozeti gibi eylemin altına giren küçük not. */
  actionNote?: ReactNode
  /** Panele özgü ek bölümler (katılımcı listesi, yönetim kartları). */
  children?: ReactNode
}

export function EventDetailLayout({ event, clubHref, primaryAction, actionNote, children }: EventDetailLayoutProps) {
  const { t, dateLocale } = useLocale()

  return (
    <Grid container spacing={3}>
      <Grid size={{ xs: 12, md: 8 }}>
        {event.posterFileId && (
          <Card variant="outlined" sx={{ borderRadius: 3, mb: 3, bgcolor: 'common.black' }}>
            <CardMedia
              component="img"
              image={`/api/files/${event.posterFileId}`}
              alt=""
              sx={{ maxHeight: 420, objectFit: 'contain' }}
            />
          </Card>
        )}

        <Card variant="outlined" sx={{ borderRadius: 3 }}>
          <CardContent sx={{ p: { xs: 2.5, md: 3.5 } }}>
            <Typography variant="h6" sx={{ fontWeight: 800, mb: 2 }}>
              {t('event.details')}
            </Typography>
            <Divider sx={{ mb: 2 }} />
            {/* İmza doğrulandı: RichTextContent({ json, fallbackText }) — prop adı `json`, `contentJson` DEĞİL. */}
            <RichTextContent json={event.descriptionJson} fallbackText={event.description ?? ''} />
          </CardContent>
        </Card>

        {children}
      </Grid>

      <Grid size={{ xs: 12, md: 4 }}>
        <Stack spacing={2} sx={{ position: { md: 'sticky' }, top: { md: 88 } }}>
          <Card variant="outlined" sx={{ borderRadius: 3 }}>
            <CardContent
              component={RouterLink}
              to={clubHref}
              sx={{ display: 'flex', alignItems: 'center', gap: 1.5, textDecoration: 'none', color: 'inherit' }}
            >
              <Box
                component={event.clubLogoFileId ? 'img' : 'div'}
                src={event.clubLogoFileId ? `/api/files/${event.clubLogoFileId}` : undefined}
                alt=""
                sx={{ width: 44, height: 44, borderRadius: '50%', objectFit: 'cover', bgcolor: 'action.hover' }}
              />
              <Box sx={{ minWidth: 0 }}>
                <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700, letterSpacing: 0.4 }}>
                  {t('event.organizer')}
                </Typography>
                <Typography variant="subtitle2" sx={{ fontWeight: 700 }} noWrap>
                  {event.clubName}
                </Typography>
              </Box>
            </CardContent>
          </Card>

          <Card variant="outlined" sx={{ borderRadius: 3 }}>
            <CardContent>
              <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 1.5 }}>
                <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                  <ScheduleRoundedIcon fontSize="small" color="action" />
                  <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                    {t('event.time')}
                  </Typography>
                </Stack>
                <IconButton
                  size="small"
                  aria-label={t('event.addToCalendar')}
                  title={t('event.addToCalendar')}
                  onClick={() =>
                    downloadEventIcs({
                      id: event.id,
                      title: event.title,
                      startIso: event.startDateUtc,
                      endIso: event.endDateUtc,
                      location: event.location,
                      description: event.description,
                    })
                  }
                >
                  <CalendarMonthOutlinedIcon fontSize="small" />
                </IconButton>
              </Stack>
              {/* Y-79: geri sayım yalnızca bilgilendirir. */}
              <EventTimeline startIso={event.startDateUtc} endIso={event.endDateUtc} />
            </CardContent>
          </Card>

          <Card variant="outlined" sx={{ borderRadius: 3 }}>
            <CardContent>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 1 }}>
                <PlaceOutlinedIcon fontSize="small" color="action" />
                <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                  {t('event.location')}
                </Typography>
              </Stack>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
                {event.location || t('event.locationMissing')}
              </Typography>
              {event.location && (
                <>
                  <Box
                    component="iframe"
                    title={t('event.location')}
                    src={mapsEmbedUrl(event.location)}
                    sx={{ width: '100%', height: 180, border: 0, borderRadius: 2, mb: 1.5 }}
                    loading="lazy"
                    referrerPolicy="no-referrer-when-downgrade"
                  />
                  <Button
                    fullWidth
                    variant="outlined"
                    color="error"
                    startIcon={<NearMeOutlinedIcon />}
                    component="a"
                    href={mapsDirectionsUrl(event.location)}
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    {t('event.directions')}
                  </Button>
                </>
              )}
            </CardContent>
          </Card>

          <Card variant="outlined" sx={{ borderRadius: 3 }}>
            <CardContent>
              <Grid container spacing={1} sx={{ textAlign: 'center' }}>
                <Grid size={4}>
                  <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700 }}>
                    {t('event.views')}
                  </Typography>
                  <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center', justifyContent: 'center' }}>
                    <VisibilityOutlinedIcon fontSize="small" color="action" />
                    <Typography sx={{ fontWeight: 800 }}>{event.viewCount}</Typography>
                  </Stack>
                </Grid>
                <Grid size={4}>
                  <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700 }}>
                    {t('event.participants')}
                  </Typography>
                  <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center', justifyContent: 'center' }}>
                    <GroupsOutlinedIcon fontSize="small" color="action" />
                    <Typography sx={{ fontWeight: 800 }}>{event.participantCount ?? '—'}</Typography>
                  </Stack>
                </Grid>
                <Grid size={4}>
                  <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700 }}>
                    {t('event.capacityLabel')}
                  </Typography>
                  <Typography sx={{ fontWeight: 800 }}>{event.capacity ?? t('event.unlimited')}</Typography>
                </Grid>
              </Grid>
            </CardContent>
          </Card>

          {primaryAction}
          {actionNote}
        </Stack>
      </Grid>
    </Grid>
  )
}
```

**Doğrulanmış imzalar** (plan yazılırken kodda kontrol edildi, tahmin değil):
- `RichTextContent({ json: string | null, fallbackText: string })`
- `EventTimeline({ startIso: string, endIso: string })`
- `mapsEmbedUrl(query: string)`, `mapsDirectionsUrl(query: string)` — `arayuz/src/utils/maps.ts`
- `t('event.location')`, `t('event.directions')` anahtarları Faz 39'da eklendi, yeniden tanımlama.

- [ ] **Step 5: Derle**

```bash
cd arayuz && npm run build
```
Beklenen: PASS (bileşen henüz kullanılmıyor, yalnızca tip kontrolü).

---

### Task 5: Anonim etkinlik detay sayfası

**Files:**
- Create: `arayuz/src/pages/public/PublicEventDetailPage.tsx`
- Modify: `arayuz/src/App.tsx`
- Modify: `arayuz/src/pages/public/PublicEventsPage.tsx`

**Interfaces:**
- Consumes: `EventDetailLayout` (Task 4), `GET /api/public/events/{id}` (Task 3).

- [ ] **Step 1: Sayfayı yaz**

```tsx
import { useQuery } from '@tanstack/react-query'
import { Button, Card, CardContent, Skeleton, Stack, Typography } from '@mui/material'
import EventOutlinedIcon from '@mui/icons-material/EventOutlined'
import LoginOutlinedIcon from '@mui/icons-material/LoginOutlined'
import { useEffect } from 'react'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { apiClient } from '../../api/client'
import { useAuth } from '../../auth/AuthContext'
import { BackButton } from '../../components/ui/BackButton'
import { EmptyState } from '../../components/ui/EmptyState'
import { EventDetailLayout } from '../../components/events/EventDetailLayout'
import { useDocumentTitle } from '../../hooks/useDocumentTitle'
import { useLocale } from '../../i18n/LocaleContext'
import type { PublicEventDetailDto } from '../../api/types'

export function PublicEventDetailPage() {
  const { t } = useLocale()
  const { id } = useParams<{ id: string }>()
  const eventId = Number(id)
  const { isAuthenticated } = useAuth()

  const eventQuery = useQuery({
    queryKey: ['public-event', eventId],
    queryFn: async () => (await apiClient.get<PublicEventDetailDto>(`/public/events/${eventId}`)).data,
  })

  useDocumentTitle(eventQuery.data?.title)

  // docs/MIMARI.md · A-77: sayaç ayrı uçta ve oturum başına bir kez; hata yutulur, sayfa etkilenmez.
  useEffect(() => {
    if (!eventQuery.isSuccess) {
      return
    }
    const key = `event-view-${eventId}`
    if (sessionStorage.getItem(key)) {
      return
    }
    sessionStorage.setItem(key, '1')
    apiClient.post(`/public/events/${eventId}/view`).catch(() => undefined)
  }, [eventQuery.isSuccess, eventId])

  if (eventQuery.isLoading) {
    return (
      <Stack spacing={2}>
        <BackButton to="/etkinlikler" />
        <Skeleton variant="rounded" height={360} />
      </Stack>
    )
  }

  if (eventQuery.isError || !eventQuery.data) {
    return (
      <Stack spacing={2}>
        <BackButton to="/etkinlikler" />
        <EmptyState icon={EventOutlinedIcon} title={t('public.eventMissing')} description={t('public.eventMissingLead')} />
      </Stack>
    )
  }

  const event = eventQuery.data

  return (
    <Stack spacing={3}>
      <BackButton to="/etkinlikler" />
      <Typography variant="h4" component="h1" sx={{ fontWeight: 800 }}>
        {event.title}
      </Typography>

      <EventDetailLayout
        event={event}
        clubHref={`/kulupler/${event.clubId}`}
        primaryAction={
          isAuthenticated ? (
            <Button fullWidth size="large" variant="contained" component={RouterLink} to={`/events/${event.id}`}>
              {t('common.goPanel')}
            </Button>
          ) : (
            <Button fullWidth size="large" variant="contained" startIcon={<LoginOutlinedIcon />} component={RouterLink} to="/login">
              {t('event.loginToJoin')}
            </Button>
          )
        }
        actionNote={
          <Card variant="outlined" sx={{ borderRadius: 3 }}>
            <CardContent sx={{ py: 1.5 }}>
              <Typography variant="caption" color="text.secondary">
                {t('public.loginToRegisterHint')}
              </Typography>
            </CardContent>
          </Card>
        }
      />
    </Stack>
  )
}
```

`public.eventMissing`, `public.eventMissingLead`, `public.loginToRegisterHint` anahtarlarını `messages.ts`'e tr+en ekle ("Etkinlik bulunamadı." / "Bu etkinlik yayından kalkmış ya da üyelere özel olabilir." / "Etkinliğe kayıt olmak için giriş yapmanız gerekir.").

- [ ] **Step 2: Rotayı ekle**

`arayuz/src/App.tsx`, `PublicLayout` bloğuna:

```tsx
                  <Route path="/etkinlikler/:id" element={<PublicEventDetailPage />} />
```

- [ ] **Step 3: Listeden detaya bağla**

`arayuz/src/pages/public/PublicEventsPage.tsx` içindeki `<Card variant="outlined" …>` bileşenini tıklanabilir yap:

```tsx
              <Card
                variant="outlined"
                component={RouterLink}
                to={`/etkinlikler/${event.id}`}
                sx={{ height: '100%', display: 'flex', flexDirection: 'column', textDecoration: 'none', color: 'inherit' }}
              >
```

`import { Link as RouterLink } from 'react-router-dom'` satırını ekle.

- [ ] **Step 4: Derle ve lint'le**

```bash
cd arayuz && npm run build && npm run lint
```

---

### Task 6: Panel etkinlik detay sayfasını aynı düzene taşı

**Files:**
- Modify: `arayuz/src/pages/EventDetailPage.tsx`

**Interfaces:**
- Consumes: `EventDetailLayout` (Task 4), `EventListItemDto.participantCount/clubLogoFileId/viewCount` (Task 2).

- [ ] **Step 1: Gövdeyi düzene devret**

`DetailHero` + `SectionCard(konum)` bloklarını (bugün ~304-422. satırlar) `EventDetailLayout` çağrısıyla değiştir. Başlık/durum rozetleri `PageHeader`'ın altında kalır; katılımcı listesi ve yönetim diyalogları `children` içine taşınır:

```tsx
      <EventDetailLayout
        event={{ ...event, participantCount: event.participantCount, viewCount: event.viewCount }}
        clubHref={`/clubs/${event.clubId}`}
        primaryAction={
          isRegistered ? (
            <Button fullWidth size="large" color="error" variant="outlined" onClick={() => cancelMutation.mutate()} disabled={cancelMutation.isPending}>
              Kaydımı İptal Et
            </Button>
          ) : (
            <Button fullWidth size="large" variant="contained" onClick={() => registerMutation.mutate()} disabled={registerMutation.isPending}>
              Etkinliğe Katıl
            </Button>
          )
        }
        actionNote={<EventAudienceChip audience={event.audience} />}
      >
        {canViewParticipants && (
          <SectionCard title="Katılımcılar" sx={{ mt: 3 }}>
            {/* mevcut DataTable bloğu buraya taşınır, içeriği değişmez */}
          </SectionCard>
        )}
      </EventDetailLayout>
```

**Bu adımda yalnızca yerleşim değişir.** Katılım/iptal/düzenle/afiş yükle/iptal etme akışlarının koşulları, mutasyonları ve yetki kontrolleri **aynen korunur** — düzenleme ve iptal diyalogları Faz 43'te sayfaya taşınacak, bu fazda dokunma.

- [ ] **Step 2: Derle ve lint'le**

```bash
cd arayuz && npm run build && npm run lint
```

- [ ] **Step 3: Elle doğrula**

- Anonim pencerede `/etkinlikler` → bir karta tıkla → detay açılmalı, "Giriş Yap" düğmesi görünmeli, sayfayı yenilediğinde görüntülenme **artmamalı** (aynı oturum), yeni sekmede artmalı.
- Panelde aynı etkinlikte katıl/iptal, düzenle, afiş yükle akışları bozulmamalı.
- Taslak bir etkinliğin id'siyle `/etkinlikler/:id` aç → "Etkinlik bulunamadı" görmelisin (Y-82).

---

### Task 7: Tam doğrulama ve tek commit

- [ ] **Step 1: Tüm testler**

```bash
dotnet test
cd arayuz && npm run build && npm run lint
```
Beklenen: `PublicEndpointAuthorizationTests` ve `PublicSurfaceLeakTests` içindeki **iki bilinen** `/api/public/stats` 500 hatası dışında tamamı PASS.

- [ ] **Step 2: Commit**

```bash
git add -A -- docs/MIMARI.md docs/superpowers/plans/2026-09-06-faz-42-etkinlik-detay-sayfasi.md src tests arayuz/src
git commit -m "$(cat <<'EOF'
Faz 42: etkinlik detay sayfasi ve anonim etkinlik detayi

Docs: docs/MIMARI.md v6.9 (K-46, A-76, A-77, Y-82).
EOF
)"
```
