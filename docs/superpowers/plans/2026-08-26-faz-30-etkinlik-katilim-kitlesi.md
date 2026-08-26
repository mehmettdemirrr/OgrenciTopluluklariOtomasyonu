# Faz 30 — Etkinlik Katılım Kitlesi Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Etkinlik oluştururken katılımın "herkese açık" mı yoksa "sadece topluluk üyelerine özel" mi olduğu belirlenebilsin; üyelere özel etkinliğe üye olmayan kaydolamasın ve o etkinlik anonim vitrinde görünmesin.

**Architecture:** `Announcement.Visibility` (A-43/Y-57) deseninin etkinlik karşılığı. `Event` varlığına `EventAudience` enum alanı eklenir; kitle **yazma anında** belirlenir, okuma anında yorumlanmaz. İki okuma noktası değişir: `EventParticipationManager.RegisterAsync` güncel dönem üyeliği arar, `PublicContentManager.GetEventsAsync` filtresine kodda sabit bir koşul ekler. Yetki metotlarına (`Ensure*Access*`) **dokunulmaz.**

**Tech Stack:** .NET 8 · EF Core 8 / MSSQL · FluentValidation · xUnit + Moq + NetArchTest · React 18 + Vite + TypeScript + MUI + react-hook-form + zod

**Spec:** [docs/PLAN-V6.md](../../PLAN-V6.md) §Faz 30 · [docs/MIMARI.md](../../MIMARI.md) K-38, A-65, Y-72

---

## Global Constraints

Bu bölüm her task'ın gereksinimlerine **örtük olarak dahildir.** Değerler `docs/MIMARI.md`'den birebir alınmıştır.

- **Y-01** — Controller içinde `DbContext`, `IXxxDal`, LINQ sorgusu veya iş kuralı bulunamaz. Controller bir servis metodu çağırır, `IResult`'ı HTTP'ye çevirir.
- **Y-03** — İş kuralı yalnızca Business'ta yaşar: controller'da `if`, entity içinde metot, frontend'de karar yok.
- **Y-09** — Entity sınıfı HTTP request/response gövdesinde yer alamaz; giriş/çıkış DTO.
- **Y-16** — Olay kaydı fiziksel silinmez; soft delete + query filter.
- **Y-17** — `DateTime.Now` yasak; UTC ve enjekte edilen `IClock`.
- **Y-22** — İstemciden gelen `userId`/`clubId` gibi kimlik bilgisine güvenilmez; kimlik yalnızca token claim'inden.
- **Y-27** — `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`, `async void` yasak. Uçtan uca async, `CancellationToken` taşınır, `.ConfigureAwait(false)` kullanılır.
- **Y-29** — Kullanıcıya gidecek mesaj koda gömülmez/tekrarlanmaz; yalnızca `Business/Constants/Messages`.
- **Y-31** — Nullable uyarısı `!` veya `#pragma` ile susturulamaz. **Uyarılar zaten hata.**
- **Y-35** — İş kuralı arayüzde tekrar yazılmaz. UI gizler/gösterir, kararı API verir. **Butonu gizlemek yetki değildir.**
- **Y-58** — Anonim vitrin ucundan kişisel veri dönmez; `DTOs/Public/` ailesi ayrıdır.
- **Y-64** — Sıralama olmadan `Skip`/`Take` yok; `Id` daima son kırıcı.
- **Y-72 (bu fazda doğuyor)** — Kitle alanı olmadan etkinlik kaydedilemez; anonim uç `ClubMembers` kitleli etkinliği döndüremez; kayıt anında üyelik kontrolü atlanamaz.
- **Sessiz onaylar** — Sınıf/metot/tablo adları İngilizce, kullanıcı mesajları ve yorumlar **Türkçe**. Enum'lar DB'de `int`, API'de metin. Tarih/saat UTC + ISO-8601. Migration adı `YYYYMMDD_AçıklayıcıAd`, **her PR'da en fazla bir migration.**
- **Üyelik dönemi** — Üyelik sorgusu daima `AcademicTerm.IsCurrent` dönemini kullanır. Farklı dönem kullanmak PLAN-V4 §22.3'te düzeltilen "arayüz başkansın derken backend değilsin diyordu" hatasını geri getirir.

---

## File Structure

| Dosya | Sorumluluk | Task |
|---|---|---|
| `src/Entities/Enums/EventAudience.cs` | **Yeni.** İki değerli kitle enum'ı | 1 |
| `src/Entities/Event.cs` | `Audience` alanı eklenir | 1 |
| `src/DataAccess/Configurations/EventConfiguration.cs` | `Audience` kolonu için açık `HasDefaultValue` | 1 |
| `src/DataAccess/Migrations/…_Faz30_EtkinlikKatilimKitlesi.cs` | **Üretilecek.** Kolon + mevcut satırlara `Public` | 1 |
| `src/Business/DTOs/Events/CreateEventRequestDto.cs` | `Audience` alanı | 2 |
| `src/Business/DTOs/Events/UpdateEventRequestDto.cs` | `Audience` alanı | 2 |
| `src/Business/DTOs/Events/EventListItemDto.cs` | `Audience` alanı (çıkış) | 2 |
| `src/Business/ValidationRules/CreateEventRequestValidator.cs` | `IsInEnum()` biçim kuralı | 2 |
| `src/Business/ValidationRules/UpdateEventRequestValidator.cs` | `IsInEnum()` biçim kuralı | 2 |
| `src/Business/Concrete/EventManager.cs` | Yazma (`CreateAsync`/`UpdateAsync`) + `MapToDto` | 2 |
| `src/Business/Constants/Messages.cs` | `EventForClubMembersOnly` mesajı | 3 |
| `src/Business/Concrete/EventParticipationManager.cs` | `RegisterAsync` üyelik muhafızı | 3 |
| `src/Business/Concrete/PublicContentManager.cs` | Anonim vitrin filtresi | 4 |
| `arayuz/src/api/types.ts` | `EventAudience` tipi + `EventListItemDto.audience` | 5 |
| `arayuz/src/schemas/eventForm.ts` | `audience` alanı + payload | 5 |
| `arayuz/src/components/ui/StatusChip.tsx` | `EventAudienceChip` bileşeni | 5 |
| `arayuz/src/pages/ClubDetailEventsTab.tsx` | Oluşturma diyaloğuna radyo grubu | 5 |
| `arayuz/src/pages/EventsPage.tsx` | Oluşturma diyaloğuna radyo grubu | 5 |
| `arayuz/src/pages/EventDetailPage.tsx` | Düzenleme diyaloğu + rozet | 5 |
| `arayuz/src/pages/MyEventsPage.tsx` | Kart listesine rozet | 5 |

**Test dosyaları:**

| Dosya | Task |
|---|---|
| `tests/WebAPI.IntegrationTests/DomainConstraintTests.cs` | 1 |
| `tests/Business.Tests/EventManagerTests.cs` | 2 |
| `tests/Business.Tests/ValidationRulesTests.cs` | 2 |
| `tests/Business.Tests/EventParticipationManagerTests.cs` | 3 |
| `tests/Business.Tests/PublicContentManagerTests.cs` | 4 |
| `tests/WebAPI.IntegrationTests/PublicSurfaceLeakTests.cs` | 4 |

**Neden bu sıra:** Task 1 şemayı kurar (alan olmadan hiçbir şey derlenmez). Task 2 alanı API yüzeyine bağlar. Task 3 asıl iş kuralını yazar. Task 4 anonim yüzeyi kapatır. Task 5 arayüzü bağlar. Her task kendi başına derlenir, testi yeşil biter ve commit edilir.

---

### Task 1: Şema — `EventAudience` enum'ı, `Event.Audience` alanı ve migration

**Files:**
- Create: `src/Entities/Enums/EventAudience.cs`
- Modify: `src/Entities/Event.cs`
- Modify: `src/DataAccess/Configurations/EventConfiguration.cs`
- Create (üretilecek): `src/DataAccess/Migrations/<timestamp>_20260826_Faz30_EtkinlikKatilimKitlesi.cs`
- Test: `tests/WebAPI.IntegrationTests/DomainConstraintTests.cs`

**Interfaces:**
- Consumes: — (ilk task)
- Produces:
  - `Entities.Enums.EventAudience` — `Public = 0`, `ClubMembers = 1`
  - `Entities.Event.Audience` — `public EventAudience Audience { get; set; }` (nullable **değil**, varsayılan `Public`)

- [ ] **Step 1: Failing test'i yaz**

`tests/WebAPI.IntegrationTests/DomainConstraintTests.cs` dosyasının **sonundaki kapanış süslü parantezinden önce** şu iki testi ekle. `SeedClubStudentTermAsync` yardımcı metodu dosyada zaten var; imzası `(Club, Student, AcademicTerm)` döner.

```csharp
    [Fact(DisplayName = "K-38: Event.Audience varsayılan olarak Public kaydedilir (mevcut davranış korunur)")]
    public async Task Event_AudienceNotSet_DefaultsToPublic()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (club, _, _) = await SeedClubStudentTermAsync(db, "aud-default");

        var @event = new Event
        {
            ClubId = club.Id,
            Title = "Varsayılan Kitle",
            StartDateUtc = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc),
            Status = EventStatus.Draft,
            CreatedAtUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        db.Events.Add(@event);
        await db.SaveChangesAsync();

        var reloaded = await db.Events.AsNoTracking().SingleAsync(e => e.Id == @event.Id);
        Assert.Equal(EventAudience.Public, reloaded.Audience);
    }

    [Fact(DisplayName = "K-38: Event.Audience = ClubMembers kaydedilip aynı değerle okunur")]
    public async Task Event_AudienceClubMembers_RoundTrips()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (club, _, _) = await SeedClubStudentTermAsync(db, "aud-members");

        var @event = new Event
        {
            ClubId = club.Id,
            Title = "Üyelere Özel",
            StartDateUtc = new DateTime(2026, 10, 2, 10, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc),
            Status = EventStatus.Draft,
            Audience = EventAudience.ClubMembers,
            CreatedAtUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        db.Events.Add(@event);
        await db.SaveChangesAsync();

        var reloaded = await db.Events.AsNoTracking().SingleAsync(e => e.Id == @event.Id);
        Assert.Equal(EventAudience.ClubMembers, reloaded.Audience);
    }
```

- [ ] **Step 2: Test'i çalıştır, derlenmediğini gör**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~DomainConstraintTests.Event_Audience"
```

Beklenen: **derleme hatası** — `error CS0246: The type or namespace name 'EventAudience' could not be found` ve `error CS0117: 'Event' does not contain a definition for 'Audience'`.

- [ ] **Step 3: Enum'ı oluştur**

`src/Entities/Enums/EventAudience.cs`:

```csharp
namespace Entities.Enums;

/// <summary>
/// docs/MIMARI.md · K-38/A-65/Y-72: etkinliğe kimin katılabileceği. AnnouncementVisibility'nin
/// (A-43) birebir kardeşi — yazma anında belirlenir, okuma anında yorumlanmaz.
/// DB'de int, API'de metin (sessiz onay).
/// </summary>
public enum EventAudience
{
    /// <summary>Herkese açık: her öğrenci kaydolabilir, anonim vitrinde görünür.</summary>
    Public = 0,

    /// <summary>Yalnızca topluluk üyelerine: güncel dönem üyeliği şart, anonim vitrinde görünmez.</summary>
    ClubMembers = 1,
}
```

- [ ] **Step 4: `Event` varlığına alanı ekle**

`src/Entities/Event.cs` içinde `Status` özelliğinin **hemen altına**, `CancellationReason`'ın üstüne ekle:

```csharp
    /// <summary>
    /// docs/MIMARI.md · K-38/A-65/Y-72: katılım kitlesi. Nullable değil — yazma anında zorunlu.
    /// Varsayılan Public: migration mevcut satırlara bu değeri yazar, bugünkü davranış korunur.
    /// </summary>
    public EventAudience Audience { get; set; }
```

- [ ] **Step 5: EF konfigürasyonuna açık varsayılan ekle**

`src/DataAccess/Configurations/EventConfiguration.cs` içinde `CancellationReason` bloğunun **hemen altına** ekle:

```csharp
        // K-38/A-65: mevcut satırlar ve varsayılanı olmayan insert'ler Public olur — bugünkü
        // davranış korunur. Y-72: kolon nullable DEĞİL, "kitle belirsiz" diye bir durum yok.
        builder.Property(e => e.Audience)
            .HasDefaultValue(EventAudience.Public);
```

Dosyanın başındaki `using` bloğuna ekle:

```csharp
using Entities.Enums;
```

- [ ] **Step 6: Migration üret**

```bash
dotnet ef migrations add 20260826_Faz30_EtkinlikKatilimKitlesi --project src/DataAccess --startup-project src/DataAccess
```

Üretilen dosyayı **aç ve doğrula**: `Up()` içinde tek bir `AddColumn<int>` olmalı, `defaultValue: 0` taşımalı ve başka hiçbir tabloya dokunmamalı. Beklenen gövde:

```csharp
migrationBuilder.AddColumn<int>(
    name: "Audience",
    table: "Events",
    type: "int",
    nullable: false,
    defaultValue: 0);
```

Başka tablo/kolon değişikliği varsa **dur** — snapshot senkron değil demektir; sebebi bulunmadan devam edilmez (sessiz onay: her PR'da en fazla bir migration).

- [ ] **Step 7: Test'i çalıştır, geçtiğini gör**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~DomainConstraintTests.Event_Audience"
```

Beklenen: **2 passed**.

- [ ] **Step 8: Tüm test takımını çalıştır (regresyon)**

```bash
dotnet build
dotnet test
```

Beklenen: sıfır uyarı, **tüm testler yeşil.** Bu task hiçbir mevcut testi değiştirmemeli.

- [ ] **Step 9: Commit**

```bash
git add src/Entities/Enums/EventAudience.cs src/Entities/Event.cs src/DataAccess/Configurations/EventConfiguration.cs src/DataAccess/Migrations tests/WebAPI.IntegrationTests/DomainConstraintTests.cs
git commit -m "Faz 30 adim 1: EventAudience semasi (K-38, A-65)"
```

---

### Task 2: API yüzeyi — DTO'lar, validasyon ve `EventManager` yazma/okuma

**Files:**
- Modify: `src/Business/DTOs/Events/CreateEventRequestDto.cs`
- Modify: `src/Business/DTOs/Events/UpdateEventRequestDto.cs`
- Modify: `src/Business/DTOs/Events/EventListItemDto.cs`
- Modify: `src/Business/ValidationRules/CreateEventRequestValidator.cs`
- Modify: `src/Business/ValidationRules/UpdateEventRequestValidator.cs`
- Modify: `src/Business/Concrete/EventManager.cs` (`CreateAsync`, `UpdateAsync`, `MapToDto`)
- Test: `tests/Business.Tests/EventManagerTests.cs`, `tests/Business.Tests/ValidationRulesTests.cs`

**Interfaces:**
- Consumes: `Entities.Enums.EventAudience` (Task 1), `Entities.Event.Audience` (Task 1)
- Produces:
  - `Business.DTOs.Events.CreateEventRequestDto.Audience` — `public EventAudience Audience { get; set; }`
  - `Business.DTOs.Events.UpdateEventRequestDto.Audience` — `public EventAudience Audience { get; set; }`
  - `Business.DTOs.Events.EventListItemDto.Audience` — `public EventAudience Audience { get; set; }`

> **Controller'a dokunulmuyor.** `EventsController.Create` ve `Update` DTO'yu gövdeden bağlıyor; yeni alan otomatik gelir. Y-01 gereği orada hiçbir değişiklik yapılmaz.

- [ ] **Step 1: Failing test'leri yaz — `EventManagerTests`**

`tests/Business.Tests/EventManagerTests.cs` dosyasının sonundaki kapanış süslü parantezinden önce ekle.

> **Kritik:** `CreateAsync` önce `EnsureClubWriteAccessAsync`'ten geçer. Test sınıfının constructor'ı `_currentUser.UserId`'yi **kurmaz** (null) — erişim kurulmazsa test 403 ile *yanlış sebepten* kırılır. Aşağıdaki iki test, dosyadaki `CreateAsync_ClubAdvisor_ReturnsSuccess` testinin danışman kurulumunu birebir tekrarlar.

```csharp
    [Fact(DisplayName = "K-38: CreateAsync istekteki kitleyi entity'ye yazar")]
    public async Task CreateAsync_ClubMembersAudience_PersistsAudience()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });

        Event? captured = null;
        _eventRepository
            .Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback((Event e, CancellationToken _) => captured = e)
            .Returns(Task.CompletedTask);

        var request = new CreateEventRequestDto
        {
            Title = "Üyelere Özel Atölye",
            StartDateUtc = FixedNow.AddDays(10),
            EndDateUtc = FixedNow.AddDays(10).AddHours(3),
            Audience = EventAudience.ClubMembers,
        };

        var result = await _sut.CreateAsync(1, request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Equal(EventAudience.ClubMembers, captured!.Audience);
    }

    [Fact(DisplayName = "K-38: CreateAsync kitle verilmezse Public yazar (varsayılan davranış)")]
    public async Task CreateAsync_AudienceNotSet_PersistsPublic()
    {
        _currentUser.Setup(c => c.UserId).Returns(100);
        _academicStaffRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 100, Title = "Dr.", DepartmentId = 1 });

        Event? captured = null;
        _eventRepository
            .Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback((Event e, CancellationToken _) => captured = e)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(1, ValidCreateRequest());

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Equal(EventAudience.Public, captured!.Audience);
    }
```

`using` bloğunda gerekli her şey (`Business.DTOs.Events`, `Entities.Enums`, `System.Linq.Expressions`, `Moq`) zaten var.

- [ ] **Step 2: Failing test'i yaz — `ValidationRulesTests`**

`tests/Business.Tests/ValidationRulesTests.cs` dosyasının sonundaki kapanış süslü parantezinden önce ekle:

```csharp
    [Fact(DisplayName = "Y-72: tanımsız EventAudience değeri biçimsel doğrulamada reddedilir")]
    public void CreateEventRequestValidator_UndefinedAudience_IsInvalid()
    {
        var validator = new CreateEventRequestValidator();
        var request = new CreateEventRequestDto
        {
            Title = "Etkinlik",
            StartDateUtc = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc),
            Audience = (EventAudience)99,
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateEventRequestDto.Audience));
    }

    [Fact(DisplayName = "Y-72: tanımlı EventAudience değeri biçimsel doğrulamadan geçer")]
    public void CreateEventRequestValidator_DefinedAudience_IsValid()
    {
        var validator = new CreateEventRequestValidator();
        var request = new CreateEventRequestDto
        {
            Title = "Etkinlik",
            StartDateUtc = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc),
            Audience = EventAudience.ClubMembers,
        };

        var result = validator.Validate(request);

        Assert.True(result.IsValid);
    }
```

- [ ] **Step 3: Test'leri çalıştır, derlenmediğini gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~CreateAsync_ClubMembersAudience|FullyQualifiedName~CreateAsync_AudienceNotSet|FullyQualifiedName~CreateEventRequestValidator_UndefinedAudience|FullyQualifiedName~CreateEventRequestValidator_DefinedAudience"
```

Beklenen: **derleme hatası** — `error CS0117: 'CreateEventRequestDto' does not contain a definition for 'Audience'`.

- [ ] **Step 4: Giriş DTO'larına alanı ekle**

`src/Business/DTOs/Events/CreateEventRequestDto.cs` — `Capacity` özelliğinin altına:

```csharp
    /// <summary>docs/MIMARI.md · K-38/A-65: katılım kitlesi. Verilmezse Public (enum varsayılanı).</summary>
    public EventAudience Audience { get; set; }
```

Dosyanın başına ekle:

```csharp
using Entities.Enums;
```

`src/Business/DTOs/Events/UpdateEventRequestDto.cs` — dosyanın başına:

```csharp
using Entities.Enums;
```

ve `Capacity` özelliğinin altına:

```csharp
    /// <summary>docs/MIMARI.md · K-38/A-65: katılım kitlesi. Verilmezse Public (enum varsayılanı).</summary>
    public EventAudience Audience { get; set; }
```

- [ ] **Step 5: Çıkış DTO'suna alanı ekle**

`src/Business/DTOs/Events/EventListItemDto.cs` — `Status` özelliğinin **hemen altına** (dosyada `using Entities.Enums;` zaten var):

```csharp
    /// <summary>docs/MIMARI.md · K-38/A-65: arayüz "Üyelere Özel" rozetini bu alandan çizer.</summary>
    public EventAudience Audience { get; set; }
```

- [ ] **Step 6: Validator'lara biçim kuralı ekle**

`src/Business/ValidationRules/CreateEventRequestValidator.cs` — constructor'daki son `RuleFor`'un altına:

```csharp
        // Y-72: kitle alanı zorunlu ve tanımlı bir enum değeri olmalı. Y-35: bu YALNIZCA biçim
        // kontrolü — "bu öğrenci üye mi" kararı EventParticipationManager'da kalır.
        RuleFor(x => x.Audience).IsInEnum();
```

`src/Business/ValidationRules/UpdateEventRequestValidator.cs` — constructor'daki son `RuleFor`'un altına:

```csharp
        // Y-72: kitle alanı zorunlu ve tanımlı bir enum değeri olmalı. Y-35: bu YALNIZCA biçim
        // kontrolü — "bu öğrenci üye mi" kararı EventParticipationManager'da kalır.
        RuleFor(x => x.Audience).IsInEnum();
```

- [ ] **Step 7: `EventManager`'ı güncelle**

`src/Business/Concrete/EventManager.cs` içinde üç yer:

**(a)** `CreateAsync` — `new Event { … }` başlatıcısında `Capacity = request.Capacity,` satırının altına:

```csharp
            Audience = request.Audience,
```

**(b)** `UpdateAsync` — `@event.Capacity = request.Capacity;` satırının altına:

```csharp
        @event.Audience = request.Audience;
```

**(c)** `MapToDto` — `Status = e.Status,` satırının altına:

```csharp
        Audience = e.Audience,
```

- [ ] **Step 8: Test'leri çalıştır, geçtiğini gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~CreateAsync_ClubMembersAudience|FullyQualifiedName~CreateAsync_AudienceNotSet|FullyQualifiedName~CreateEventRequestValidator_UndefinedAudience|FullyQualifiedName~CreateEventRequestValidator_DefinedAudience"
```

Beklenen: **4 passed**.

- [ ] **Step 9: Tüm test takımını çalıştır**

```bash
dotnet build
dotnet test
```

Beklenen: sıfır uyarı, tüm testler yeşil.

> **Dikkat — `MappingProfileTests`.** Repo'da `AssertConfigurationIsValid` testi var (Y-32). `EventListItemDto` AutoMapper profiliyle eşleniyorsa yeni alan eşlenmemiş sayılıp test kırılabilir. Kırılırsa `src/Business/Mappings/ClubMappingProfile.cs`'i aç ve eşlemeyi ekle — **koşullu mantık veya DB çağrısı ekleme** (Y-32), yalnızca alan taşı.

- [ ] **Step 10: Commit**

```bash
git add src/Business/DTOs/Events src/Business/ValidationRules/CreateEventRequestValidator.cs src/Business/ValidationRules/UpdateEventRequestValidator.cs src/Business/Concrete/EventManager.cs tests/Business.Tests/EventManagerTests.cs tests/Business.Tests/ValidationRulesTests.cs
git commit -m "Faz 30 adim 2: kitle alani API yuzeyine baglandi (K-38)"
```

---

### Task 3: İş kuralı — üyelere özel etkinliğe kayıt muhafızı

**Files:**
- Modify: `src/Business/Constants/Messages.cs`
- Modify: `src/Business/Concrete/EventParticipationManager.cs:44-64` (`RegisterAsync`)
- Test: `tests/Business.Tests/EventParticipationManagerTests.cs`

**Interfaces:**
- Consumes: `Entities.Enums.EventAudience` (Task 1), `Entities.Event.Audience` (Task 1)
- Produces: `Business.Constants.Messages.EventForClubMembersOnly` — `const string`

**Kural:** Kitle `ClubMembers` ise, **güncel dönemde** `(ClubId, StudentId)` üyeliği aranır. Yoksa `Result.Forbidden`. Muhafız, `Published`/`StartDateUtc` kontrolünden **sonra**, `existing` (çift kayıt) kontrolünden **önce** durur — sırası önemli: iptal edilmiş etkinliğe üye olmayan biri "üye değilsin" değil "etkinlik kapalı" cevabı almalı.

- [ ] **Step 1: Failing test'leri yaz**

`tests/Business.Tests/EventParticipationManagerTests.cs` dosyasının sonundaki kapanış süslü parantezinden önce ekle. Dosyada `PublishedEvent(int? capacity = null)` yardımcısı ve `_clubMembershipRepository`, `_academicTermRepository` alanları zaten var.

```csharp
    private static Event MembersOnlyEvent() => new()
    {
        Id = 1, ClubId = 1, Title = "Üyelere Özel Atölye",
        StartDateUtc = FixedNow.AddDays(1), EndDateUtc = FixedNow.AddDays(1).AddHours(2),
        Status = EventStatus.Published, Audience = EventAudience.ClubMembers, CreatedAtUtc = FixedNow,
    };

    private static AcademicTerm CurrentTerm() => new()
    {
        Id = 3, Name = "2026-2027 Güz",
        StartDateUtc = FixedNow.AddMonths(-1), EndDateUtc = FixedNow.AddMonths(4), IsCurrent = true,
    };

    [Fact(DisplayName = "Y-72: üye olmayan öğrenci üyelere özel etkinliğe kaydolamaz")]
    public async Task RegisterAsync_MembersOnlyEvent_NonMember_ReturnsForbidden()
    {
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(MembersOnlyEvent());
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(CurrentTerm());
        _clubMembershipRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((ClubMembership?)null);

        var result = await _sut.RegisterAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _participationRepository.Verify(r => r.AddAsync(It.IsAny<EventParticipation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Y-72: güncel dönem üyesi üyelere özel etkinliğe kaydolabilir")]
    public async Task RegisterAsync_MembersOnlyEvent_CurrentTermMember_ReturnsSuccess()
    {
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(MembersOnlyEvent());
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(CurrentTerm());
        _clubMembershipRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubMembership { Id = 9, ClubId = 1, StudentId = 5, AcademicTermId = 3, ClubRole = ClubRole.Member, JoinedAtUtc = FixedNow });
        _participationRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<EventParticipation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((EventParticipation?)null);
        _participationRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<EventParticipation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await _sut.RegisterAsync(1);

        Assert.True(result.IsSuccess);
        _participationRepository.Verify(r => r.AddAsync(It.IsAny<EventParticipation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Y-72: güncel dönem yoksa üyelere özel etkinliğe kaydolunamaz (fail-closed)")]
    public async Task RegisterAsync_MembersOnlyEvent_NoCurrentTerm_ReturnsForbidden()
    {
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(MembersOnlyEvent());
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((AcademicTerm?)null);

        var result = await _sut.RegisterAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _participationRepository.Verify(r => r.AddAsync(It.IsAny<EventParticipation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Y-72: geçen dönemin üyesi üyelere özel etkinliğe kaydolamaz (dönem filtresi gerçekten uygulanır)")]
    public async Task RegisterAsync_MembersOnlyEvent_PreviousTermMemberOnly_ReturnsForbidden()
    {
        var currentTerm = CurrentTerm();
        // Geçen dönemin üyeliği: AcademicTermId = 2, güncel dönem 3.
        var previousTermMembership = new ClubMembership
        {
            Id = 8, ClubId = 1, StudentId = 5, AcademicTermId = 2, ClubRole = ClubRole.Member, JoinedAtUtc = FixedNow.AddMonths(-8),
        };

        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(MembersOnlyEvent());
        _academicTermRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(currentTerm);

        // Predicate GERÇEKTEN çalıştırılır (PublicContentManagerTests'in SetupPagedFilter deseni) —
        // Moq salt-geçiş olsaydı bu test dönem filtresinin varlığını kanıtlamazdı.
        _clubMembershipRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ClubMembership, bool>> filter, CancellationToken _) =>
                new[] { previousTermMembership }.AsQueryable().Where(filter).FirstOrDefault());

        var result = await _sut.RegisterAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _participationRepository.Verify(r => r.AddAsync(It.IsAny<EventParticipation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "K-38: herkese açık etkinlikte üyelik hiç sorgulanmaz (mevcut davranış bozulmadı)")]
    public async Task RegisterAsync_PublicEvent_DoesNotQueryMembership()
    {
        _eventRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(PublishedEvent(capacity: 10));
        _participationRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<EventParticipation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((EventParticipation?)null);
        _participationRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<EventParticipation, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await _sut.RegisterAsync(1);

        Assert.True(result.IsSuccess);
        _clubMembershipRepository.Verify(
            r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
```

`using` bloğuna `Core.Utilities.Results;` yoksa ekle (`ResultStatus` için).

- [ ] **Step 2: Test'leri çalıştır, kırmızı olduğunu gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~RegisterAsync_MembersOnlyEvent|FullyQualifiedName~RegisterAsync_PublicEvent_DoesNotQueryMembership"
```

Beklenen: **3 failed, 2 passed.** Üç "reddedilmeli" testi kırmızı (muhafız yok, `Forbidden` yerine `Success` dönüyor).

İki testin baştan yeşil olması doğrudur ve kasıtlıdır:
- `RegisterAsync_MembersOnlyEvent_CurrentTermMember_ReturnsSuccess` — muhafız yokken herkes kaydolabildiği için üye de kaydolur. Kural yazıldıktan sonra **doğru sebepten** yeşil kalır.
- `RegisterAsync_PublicEvent_DoesNotQueryMembership` — regresyon muhafızı; hiçbir zaman kırmızı olmamalı.

- [ ] **Step 3: Mesajı ekle**

`src/Business/Constants/Messages.cs` — dosyada `EventNotOpenForRegistration` sabitinin bulunduğu bloğun altına:

```csharp
    // Faz 30 — Etkinlik katılım kitlesi (K-38, Y-72)
    public const string EventForClubMembersOnly = "Bu etkinliğe yalnızca topluluğun üyeleri katılabilir.";
```

- [ ] **Step 4: Muhafızı yaz**

`src/Business/Concrete/EventParticipationManager.cs` — `RegisterAsync` içinde, `EventNotOpenForRegistration` kontrolünün kapanış parantezinden **sonra**, `var existing = await participationRepository` satırından **önce** ekle:

```csharp
        // Y-72: kitle ClubMembers ise güncel dönem üyeliği şart. Kayıt/kontenjan kontrollerinden
        // ÖNCE gelir — üye olmayan biri "kontenjan doldu" değil, sebebi doğru olan cevabı almalı.
        // Dönem seçimi EnsureClubWriteAccessAsync ile aynı: daima IsCurrent (PLAN-V4 §22.3).
        if (@event.Audience == EventAudience.ClubMembers)
        {
            var term = await academicTermRepository.GetAsync(t => t.IsCurrent, cancellationToken).ConfigureAwait(false);
            if (term is null)
            {
                return Result.Forbidden(Messages.EventForClubMembersOnly);
            }

            var membership = await clubMembershipRepository
                .GetAsync(m => m.ClubId == @event.ClubId && m.StudentId == student.Id && m.AcademicTermId == term.Id, cancellationToken)
                .ConfigureAwait(false);

            if (membership is null)
            {
                return Result.Forbidden(Messages.EventForClubMembersOnly);
            }
        }
```

- [ ] **Step 5: Test'leri çalıştır, geçtiğini gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~RegisterAsync_MembersOnlyEvent|FullyQualifiedName~RegisterAsync_PublicEvent_DoesNotQueryMembership"
```

Beklenen: **5 passed**.

- [ ] **Step 6: Tüm test takımını çalıştır**

```bash
dotnet build
dotnet test
```

Beklenen: sıfır uyarı, tüm testler yeşil. Özellikle `EventCapacityConcurrencyTests` bozulmamalı — muhafız `Public` etkinliklerde hiç çalışmaz.

- [ ] **Step 7: Commit**

```bash
git add src/Business/Constants/Messages.cs src/Business/Concrete/EventParticipationManager.cs tests/Business.Tests/EventParticipationManagerTests.cs
git commit -m "Faz 30 adim 3: uyelere ozel etkinlik kayit muhafizi (K-38, Y-72)"
```

---

### Task 4: Anonim vitrin — `ClubMembers` kitleli etkinlik gizlenir

**Files:**
- Modify: `src/Business/Concrete/PublicContentManager.cs:70-79` (`GetEventsAsync` filtresi)
- Test: `tests/Business.Tests/PublicContentManagerTests.cs`
- Test: `tests/WebAPI.IntegrationTests/PublicSurfaceLeakTests.cs`

**Interfaces:**
- Consumes: `Entities.Enums.EventAudience` (Task 1), `Entities.Event.Audience` (Task 1)
- Produces: — (davranış değişikliği; yeni tip yok)

**Kural:** Filtre **kodda sabit**, dışarıdan parametrelenmez (Y-58). `PublicEventListItemDto`'ya `Audience` alanı **eklenmez** — o uçtan zaten yalnızca `Public` döner.

- [ ] **Step 1: Failing birim testini yaz**

`tests/Business.Tests/PublicContentManagerTests.cs` — `GetEventsAsync_ClubIdFilter_ScopesToSingleClub` testinin **hemen altına** ekle:

```csharp
    [Fact(DisplayName = "Y-72: ClubMembers kitleli etkinlik anonim vitrinde görünmez, Public görünür")]
    public async Task GetEventsAsync_MembersOnlyAudience_IsExcluded()
    {
        var publicEvent = new Event
        {
            Id = 1, ClubId = 1, Title = "Herkese Açık",
            StartDateUtc = FixedNow.AddDays(1), EndDateUtc = FixedNow.AddDays(1).AddHours(2),
            Status = EventStatus.Published, Audience = EventAudience.Public, CreatedAtUtc = FixedNow,
        };
        var membersOnly = new Event
        {
            Id = 2, ClubId = 1, Title = "Üyelere Özel",
            StartDateUtc = FixedNow.AddDays(1), EndDateUtc = FixedNow.AddDays(1).AddHours(2),
            Status = EventStatus.Published, Audience = EventAudience.ClubMembers, CreatedAtUtc = FixedNow,
        };
        SetupPagedFilter<Event, DateTime>(_eventRepository, [publicEvent, membersOnly]);
        _clubRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await _sut.GetEventsAsync(null, 0, 20);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Data!.Items);
        Assert.Equal("Herkese Açık", item.Title);
    }
```

- [ ] **Step 2: Failing entegrasyon testini yaz**

`tests/WebAPI.IntegrationTests/PublicSurfaceLeakTests.cs` dosyasının sonundaki kapanış süslü parantezinden önce ekle.

Bu testin üç kuralı var; üçü de **uygulama sırasında yaşanmış hatalardan** geliyor:

| Kural | Neden |
|---|---|
| Veriyi `SeedScenarioAsync(suffix)` üretir, elle danışman aranmaz | `db.AcademicStaff.…FirstAsync()` demek "başka bir test danışman yaratmıştır" varsaymaktır. Tam takımda geçer, **tek başına çalıştırıldığında** `Sequence contains no elements` ile patlar (Y-34) |
| Sorguya `search={suffix}` eklenir | Sunucu `pageSize`'ı **100'e kırpıyor** (Y-11) ve liste `StartDateUtc` artan sıralı. `search` olmadan bu testin etkinlikleri birikmiş kayıtların arkasında kalır |
| `Assert.Contains` ile birlikte yazılır | Yukarıdaki iki hata da `DoesNotContain`'i **yanlış sebepten** geçirir. `Contains` muhafızı olmasa test yeşil görünüp hiçbir şey kanıtlamazdı |

`Scenario` record'unun alan adı `PublishedEventTitle`'dır (`PublishedTitle` değil).

```csharp
    [Fact(DisplayName = "Y-72: /api/public/events ucundan ClubMembers kitleli etkinlik donmez, Public doner")]
    public async Task GetPublicEvents_Anonymous_ExcludesClubMembersAudience()
    {
        // Y-34: test kendi verisini kurar. Danismani "baska bir test yaratmistir" diye varsaymak
        // testi calistirma sirasina bagimli kilardi; SeedScenarioAsync fakulte/bolum/danisman/
        // ogrenci/kulup/etkinlik zincirinin tamamini kendisi uretir.
        var suffix = $"aud{Guid.NewGuid():N}"[..11];
        var scenario = await SeedScenarioAsync(suffix);
        var membersOnlyTitle = $"pub-leak-members-event-{suffix}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var club = await db.Clubs.SingleAsync(c => c.Name == scenario.ActiveClubName);

            db.Events.Add(new Event
            {
                ClubId = club.Id,
                Title = membersOnlyTitle,
                StartDateUtc = DateTime.UtcNow.AddDays(3),
                EndDateUtc = DateTime.UtcNow.AddDays(3).AddHours(2),
                Status = EventStatus.Published,
                Audience = EventAudience.ClubMembers,
                CreatedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        // A-50: arama sunucuda. `search` olmadan pageSize=200 istemek ise yaramaz — sunucu 100'e
        // kirpiyor (Y-11) ve liste StartDateUtc'ye gore artan sirali oldugu icin bu testin
        // etkinlikleri birikmis kayitlarin arkasinda kalirdi.
        var response = await _client.GetAsync($"/api/public/events?pageIndex=0&pageSize=100&search={suffix}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        // Seed'in yayindaki etkinligi Audience varsayilani (Public) ile gelir ve GORUNMELI.
        Assert.Contains(scenario.PublishedEventTitle, body, StringComparison.Ordinal);
        Assert.DoesNotContain(membersOnlyTitle, body, StringComparison.Ordinal);
    }
```

- [ ] **Step 3: Test'leri çalıştır, kırmızı olduğunu gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~GetEventsAsync_MembersOnlyAudience"
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~GetPublicEvents_Anonymous_ExcludesClubMembersAudience"
```

Beklenen: **her ikisi de FAIL** — birim testi `Assert.Single` yerine 2 kayıt görür ("The collection contained 2 matching item(s)"); entegrasyon testi `DoesNotContain` başlığı gövdede bulur.

> **`--filter` yazım hatasına dikkat.** Eşleşen test bulunamazsa `dotnet test` hata vermez, "No test matches the given testcase filter" deyip **başarılı çıkar.** Her iki komutun çıktısında da `Passed!`/`Failed!` satırındaki test sayısının **1** olduğunu gözle doğrula; 0 ise filtre yanlıştır, kod değil.

- [ ] **Step 4: Filtreyi ekle**

`src/Business/Concrete/PublicContentManager.cs` — `GetEventsAsync` içindeki `GetListPagedAsync` çağrısının predicate'ini şununla değiştir:

```csharp
                e => e.Status == EventStatus.Published && e.Audience == EventAudience.Public
                    && e.StartDateUtc >= now && (clubId == null || e.ClubId == clubId)
                    && (term.Length == 0 || e.Title.Contains(term)),
```

Ve predicate'in üstündeki yoruma ikinci cümleyi ekle:

```csharp
        // Yaklaşan etkinlik listesi tarihe göre artan sıralanır — vitrinde en yakın etkinlik başta (Y-64).
        // Y-72: Audience filtresi de Status gibi KODDA SABİT — search/clubId parametreleri onu gevşetemez.
```

- [ ] **Step 5: Test'leri çalıştır, geçtiğini gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~GetEventsAsync_MembersOnlyAudience"
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~GetPublicEvents_Anonymous_ExcludesClubMembersAudience"
```

Beklenen: **her ikisi de PASS**.

- [ ] **Step 6: Tüm test takımını çalıştır**

```bash
dotnet build
dotnet test
```

Beklenen: sıfır uyarı, tüm testler yeşil. `PublicContentManagerTests`'in mevcut testleri (`Draft/PendingApproval/Rejected` elenmesi, `clubId` filtresi) bozulmamalı — o testlerdeki `Event` nesneleri `Audience` vermediği için varsayılan `Public` alırlar.

- [ ] **Step 7: Commit**

```bash
git add src/Business/Concrete/PublicContentManager.cs tests/Business.Tests/PublicContentManagerTests.cs tests/WebAPI.IntegrationTests/PublicSurfaceLeakTests.cs
git commit -m "Faz 30 adim 4: uyelere ozel etkinlik anonim vitrinden dusuruldu (Y-72)"
```

---

### Task 5: Arayüz — kitle seçimi ve rozeti

**Files:**
- Modify: `arayuz/src/api/types.ts:173-190`
- Modify: `arayuz/src/schemas/eventForm.ts`
- Modify: `arayuz/src/components/ui/StatusChip.tsx`
- Modify: `arayuz/src/pages/ClubDetailEventsTab.tsx`
- Modify: `arayuz/src/pages/EventsPage.tsx`
- Modify: `arayuz/src/pages/EventDetailPage.tsx`

**Interfaces:**
- Consumes: `EventListItemDto.Audience` (Task 2) — JSON'da `audience: 'Public' | 'ClubMembers'`
- Produces:
  - `arayuz/src/api/types.ts` → `export type EventAudience = 'Public' | 'ClubMembers'`
  - `arayuz/src/schemas/eventForm.ts` → `EventFormValues.audience: EventAudience`
  - `arayuz/src/components/ui/StatusChip.tsx` → `export function EventAudienceChip({ audience, size })`

> **Y-35 hatırlatması:** Bu task hiçbir karar vermez. Radyo grubu değeri taşır, rozet durumu gösterir. "Kaydolabilir mi" kararı API'nindir; arayüz 403'ü mesajla gösterir.

- [ ] **Step 1: TS tiplerini ekle**

`arayuz/src/api/types.ts` — `export type EventStatus = …` satırının **hemen altına**:

```typescript
// src/Entities/Enums/EventAudience.cs (JsonStringEnumConverter ile metin olarak taşınır)
export type EventAudience = 'Public' | 'ClubMembers'
```

Aynı dosyada `EventListItemDto` arayüzünde `status: EventStatus` satırının altına:

```typescript
  audience: EventAudience
```

- [ ] **Step 2: Form şemasını güncelle**

`arayuz/src/schemas/eventForm.ts` dosyasını **tamamen** şununla değiştir:

```typescript
import { z } from 'zod'

// Y-35: yalnızca biçim doğrulanır — zorunlu alan, tarih sırası, pozitif sayı.
// "Etkinlik yayınlanabilir mi", kontenjan doldu mu, bu öğrenci üye mi gibi kararlar API'de kalır.
export const eventFormSchema = z
  .object({
    title: z.string().min(1, 'Başlık gerekli.'),
    description: z.string(),
    location: z.string(),
    startDateTime: z.string().min(1, 'Başlangıç tarihi gerekli.'),
    endDateTime: z.string().min(1, 'Bitiş tarihi gerekli.'),
    capacity: z.string(),
    // K-38: kitle seçimi zorunlu; varsayılan "herkese açık".
    audience: z.enum(['Public', 'ClubMembers']),
  })
  .refine((values) => new Date(values.endDateTime) > new Date(values.startDateTime), {
    message: 'Bitiş tarihi başlangıçtan sonra olmalı.',
    path: ['endDateTime'],
  })
  .refine((values) => values.capacity.trim() === '' || Number(values.capacity) > 0, {
    message: 'Kontenjan pozitif bir sayı olmalı.',
    path: ['capacity'],
  })

export type EventFormValues = z.infer<typeof eventFormSchema>

export const emptyEventFormValues: EventFormValues = {
  title: '',
  description: '',
  location: '',
  startDateTime: '',
  endDateTime: '',
  capacity: '',
  audience: 'Public',
}

export function toEventPayload(values: EventFormValues) {
  return {
    title: values.title,
    description: values.description.trim() || null,
    location: values.location.trim() || null,
    startDateUtc: new Date(values.startDateTime).toISOString(),
    endDateUtc: new Date(values.endDateTime).toISOString(),
    capacity: values.capacity.trim() === '' ? null : Number(values.capacity),
    audience: values.audience,
  }
}
```

- [ ] **Step 3: Rozet bileşenini ekle**

`arayuz/src/components/ui/StatusChip.tsx`:

**(a)** Dosyanın ilk satırındaki tip import'una `EventAudience` ekle:

```typescript
import type { AnnouncementVisibility, ApplicationStatus, EventAudience, EventStatus, ReportStatus } from '../../api/types'
```

**(b)** `announcementVisibilityMap` tanımının **hemen altına**:

```typescript
const eventAudienceMap: Record<EventAudience, { label: string; color: ChipColor }> = {
  Public: { label: 'Herkese Açık', color: 'info' },
  ClubMembers: { label: 'Üyelere Özel', color: 'default' },
}
```

**(c)** Dosyanın sonuna, `AnnouncementVisibilityChip`'in altına:

```typescript
export function EventAudienceChip({ audience, size = 'small' }: { audience: EventAudience; size?: ChipProps['size'] }) {
  return buildChip(eventAudienceMap[audience], size)
}
```

- [ ] **Step 4: Oluşturma diyaloglarına radyo grubunu ekle**

`arayuz/src/pages/ClubDetailEventsTab.tsx` **ve** `arayuz/src/pages/EventsPage.tsx` — her iki dosyada `capacity` alanının `Controller`'ından **sonra**, `</DialogContent>` etiketinden **önce** aynı bloğu ekle:

```tsx
          <Controller
            name="audience"
            control={control}
            render={({ field }) => (
              <FormControl margin="dense">
                <FormLabel>Kimler katılabilir?</FormLabel>
                <RadioGroup {...field} row>
                  <FormControlLabel value="Public" control={<Radio />} label="Herkese açık" />
                  <FormControlLabel value="ClubMembers" control={<Radio />} label="Sadece topluluk üyeleri" />
                </RadioGroup>
              </FormControl>
            )}
          />
```

Her iki dosyanın `@mui/material` import'una şunları ekle: `FormControl`, `FormControlLabel`, `FormLabel`, `Radio`, `RadioGroup`.

- [ ] **Step 5: Düzenleme diyaloğunu ve rozeti bağla**

`arayuz/src/pages/EventDetailPage.tsx`:

**(a)** `reset({ … })` çağrısına (dosyada ~175. satır) `capacity` satırının altına ekle:

```typescript
      audience: eventQuery.data.audience,
```

**(b)** Düzenleme diyaloğuna, Step 4'teki **aynı** `Controller` bloğunu ekle (aynı `@mui/material` import'larıyla).

**(c)** Etkinlik başlığının yanındaki `EventStatusChip`'in hemen yanına rozeti koy:

```tsx
<EventAudienceChip audience={eventQuery.data.audience} />
```

`StatusChip` import satırına `EventAudienceChip` ekle.

- [ ] **Step 6: Liste görünümlerine rozeti ekle**

Üç liste, iki farklı düzen. Her üçünde de `StatusChip` import satırına `EventAudienceChip` eklenir.

**(a) `arayuz/src/pages/EventsPage.tsx`** — dosyada **iki ayrı `columns` dizisi** var (biri kulüp etkinlikleri ~210. satır, biri yaklaşan etkinlikler ~414. satır). **Her ikisine de**, `title` kolonundan sonra ekle:

```tsx
    {
      field: 'audience',
      headerName: 'Kitle',
      width: 130,
      renderCell: (params) => <EventAudienceChip audience={params.row.audience} />,
    },
```

Sayfada `mobileHiddenFields` prop'u kullanan `DataTable`'lara `'audience'` değerini de ekle — dar ekranda kolon kalabalığı yapmasın.

**(b) `arayuz/src/pages/ClubDetailEventsTab.tsx`** — `columns` dizisinde `title` kolonundan sonra **aynı** kolon nesnesini ekle:

```tsx
    {
      field: 'audience',
      headerName: 'Kitle',
      width: 130,
      renderCell: (params) => <EventAudienceChip audience={params.row.audience} />,
    },
```

**(c) `arayuz/src/pages/MyEventsPage.tsx`** — bu sayfa `DataTable` değil kart listesi kullanıyor. `<EventStatusChip status={event.status} />` satırının **hemen altına** ekle:

```tsx
                    <EventAudienceChip audience={event.audience} />
```

- [ ] **Step 7: Build ve lint çalıştır**

```bash
cd arayuz && npm run build && npm run lint
```

Beklenen: **sıfır TypeScript hatası, sıfır lint hatası.** Bir sayfada `audience` alanını `reset`'e eklemeyi unuttuysan TS burada kırılır — `emptyEventFormValues` tipi zorunlu kılar.

- [ ] **Step 8: Uygulamayı elle doğrula**

Backend ve arayüzü çalıştır, sonra:

1. Başkan/danışman olarak bir kulüpte **"Sadece topluluk üyeleri"** kitleli etkinlik oluştur → onaya gönder → danışman onaylasın.
2. **Üye olmayan** bir öğrenci hesabıyla o etkinliğe kaydolmayı dene → *"Bu etkinliğe yalnızca topluluğun üyeleri katılabilir."* mesajı gelmeli.
3. **Çıkış yap**, ana sayfaya (`/`) ve `/etkinlikler` vitrinine bak → o etkinlik **görünmemeli.**
4. Aynı kulübün **herkese açık** bir etkinliği vitrinde görünmeye devam etmeli.
5. `/etkinlikler`, `/etkinliklerim` ve kulüp detayının Etkinlikler sekmesinde **"Üyelere Özel"** rozeti görünmeli.

- [ ] **Step 9: Commit**

```bash
git add arayuz/src
git commit -m "Faz 30 adim 5: kitle secimi ve rozeti arayuze baglandi (K-38)"
```

---

## Faz Kapanışı

- [ ] **Tam doğrulama**

```bash
dotnet build
dotnet test
cd arayuz && npm run build && npm run lint && cd ..
git diff --stat master
```

Beklenen: sıfır uyarı, tüm testler yeşil, `git diff --stat` yalnızca **File Structure** tablosundaki dosyaları göstermeli. Listede olmayan bir dosya değiştiyse sebebini açıkla veya geri al.

- [ ] **"Bitti sayılır" kontrolü** (`docs/PLAN-V6.md` §Faz 30)

| Koşul | Nasıl doğrulanır |
|---|---|
| Üye olmayan öğrenci üyelere özel etkinliğe kaydolamıyor | `RegisterAsync_MembersOnlyEvent_NonMember_ReturnsForbidden` + elle doğrulama adım 2 |
| O etkinlik anonim vitrinde ve ana sayfada görünmüyor | `GetPublicEvents_Anonymous_ExcludesClubMembersAudience` + `GetEventsAsync_MembersOnlyAudience_IsExcluded` + elle doğrulama adım 3 |
| Herkese açık etkinliklerde hiçbir davranış değişmemiş | `RegisterAsync_PublicEvent_DoesNotQueryMembership` + tüm mevcut testlerin yeşil kalması |

- [ ] **Faz commit'i**

Adım commit'leri zaten atıldı. Dal `master` ise (repo kuralı: `master` korumalı, iş `feature/*` dallarında) PR aç:

```bash
git log --oneline master..HEAD
```

Beş adım commit'i görünmeli.

---

## Sonraki Faz

Faz 31 (Topluluk kurma başvuru takvimi) bu faza **bağlı değil** — bağımsız olarak planlanıp uygulanabilir. Planı henüz yazılmadı.
