# Faz 34 — Dinamik Topluluk İçi Roller Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Her topluluk kendi rol unvanlarını tanımlayabilsin ("Sayman", "Sekreter", "Sosyal Medya Sorumlusu"); her unvan üç yetki seviyesinden birine bağlansın; üyelere unvan atanabilsin — **mevcut yetki davranışı hiç değişmeden.**

**Architecture:** `ClubRole` enum'ı **yetki seviyesi** olarak yerinde kalır ve 7 `Ensure*Access*` metodunun tek kaynağı olmaya devam eder. Yeni `ClubRoleDefinition` tablosu yalnızca **unvan** taşır ve bir seviyeye bağlanır; `ClubMembership` ikisini birden tutar. Bu sayede yetki yüzeyine tek satır eklenmez, A-39'un filtreli unique index'i olduğu gibi çalışır ve yetki kontrolleri ek DB okuması yapmaz. Rol tanımı **izin/claim taşıyamaz** — Y-37'nin kulüp karşılığı (Y-69), mimari testle kilitlenir.

**Tech Stack:** .NET 8 · EF Core 8 / MSSQL · FluentValidation · xUnit + Moq + Reflection · React 18 + Vite + TypeScript + MUI + react-hook-form + zod

**Spec:** [docs/PLAN-V6.md](../../PLAN-V6.md) §Faz 34 · [docs/MIMARI.md](../../MIMARI.md) K-36, A-61, Y-69

**Bağımlılık:** Yok. **Risk sırasında en sonda** — yetki yüzeyine en yakın faz.

---

## Bu fazın sözü

> **7 `Ensure*Access*` metodunun hiçbirine tek satır eklenmez.**

Bu söz tutulmazsa faz yanlış tasarlanmış demektir. Faz kapanışındaki `EventManagerTests` / `ClubMemberManagerTests` / `AdminClubScopeTests` yeşilliği bunun kanıtıdır.

Dokunulmayacak metotlar: `ClubMemberManager.EnsureMemberViewAccessAsync`, `ClubMemberManager.EnsureRoleManagementAccessAsync`, `EventManager.EnsureClubWriteAccessAsync`, `EventParticipationManager.EnsureViewAccessAsync`, `AnnouncementManager.EnsureClubWriteAccessAsync`, `FileManager.EnsureClubAdvisorAsync`, `ReportScopeResolver`.

---

## Global Constraints

- **Y-01** — Controller içinde `DbContext`, LINQ veya iş kuralı bulunamaz.
- **Y-03** — İş kuralı yalnızca Business'ta.
- **Y-16** — Olay kaydı fiziksel silinmez. **Rol tanımı olay kaydı değildir** — referans verisi, hard delete edilir (A-12).
- **Y-17** — `DateTime.Now` yasak; UTC + `IClock`.
- **Y-18** — Benzersizlik kuralı yalnızca uygulama kodunda tutulmaz: unique index son sözü söyler.
- **Y-22** — İstemciden gelen yetki bilgisine güvenilmez. **`ClubRole` istemciden okunmaz, tanımdan okunur.**
- **Y-23** — Rol kontrolü kaynak sahipliği kontrolü değildir; kapsam `ClubMembership` sorgusundan.
- **Y-27** — Uçtan uca async, `CancellationToken`, `.ConfigureAwait(false)`.
- **Y-29** — Kullanıcı mesajı yalnızca `Business/Constants/Messages`.
- **Y-31** — Nullable uyarısı susturulamaz. **Uyarılar zaten hata.**
- **Y-35** — İş kuralı arayüzde tekrar yazılmaz.
- **Y-37** — Identity'nin yanına paralel yetki tablosu açılamaz.
- **Y-64** — Sıralama olmadan `Skip`/`Take` yok.
- **Y-66** — Kulüp kapsamı kontrol eden her metot **ilk satırda** `clubs.manage.all`'a bakar.
- **Y-69 (bu fazda doğuyor)** — Rol tanımı izin kodu/claim/`RoleClaim` referansı taşıyamaz; yetkinin kaynağı olamaz.
- **A-39** — Başkan tekilliği: `(ClubId, AcademicTermId)` üzerinde `ClubRole = President` filtreli unique index.
- **A-61** — Unvan yetkiden ayrılır; `ClubMembership.ClubRole` yetkinin tek kaynağı olarak kalır.
- **O-27** — Tanımın seviyesi düzenlenebilir; değişiklik o unvanı taşıyan tüm üyeliklere **aynı transaction'da** yayılır.
- **Sessiz onaylar** — Adlar İngilizce, mesajlar/yorumlar Türkçe. URL çoğul + kebab-case. Migration adı `YYYYMMDD_AçıklayıcıAd`, **her PR'da en fazla bir migration.**

---

## File Structure

| Dosya | Sorumluluk | Task |
|---|---|---|
| `src/Entities/ClubRoleDefinition.cs` | **Yeni.** Unvan → yetki seviyesi | 1 |
| `src/Entities/ClubMembership.cs` | `ClubRoleDefinitionId` (nullable) | 1 |
| `src/DataAccess/Configurations/ClubRoleDefinitionConfiguration.cs` | **Yeni.** `(ClubId, Name)` unique | 1 |
| `src/DataAccess/Configurations/ClubMembershipConfiguration.cs` | FK `Restrict` | 1 |
| `src/DataAccess/AppDbContext.cs` | `DbSet<ClubRoleDefinition>` | 1 |
| `src/DataAccess/Migrations/…_Faz34_DinamikToplulukRolleri.cs` | **Üretilecek** + mevcut kulüplere varsayılan 5 tanım | 1 |
| `src/Business/Constants/DefaultClubRoles.cs` | **Yeni.** Varsayılan beşli | 1, 4 |
| `tests/Architecture.Tests/ClubRoleDefinitionShapeTests.cs` | **Yeni.** Y-69 | 2 |
| `src/Business/DTOs/Clubs/ClubRoleDefinitionDto.cs` · `Create…` · `Update…` | **Yeni** | 2 |
| `src/Business/ValidationRules/CreateClubRoleDefinitionRequestValidator.cs` · `Update…` | **Yeni** | 2 |
| `src/Business/Abstract/IClubMemberService.cs` · `Concrete/ClubMemberManager.cs` | CRUD + atama + yayılma | 2, 3, 4 |
| `src/Business/Constants/Messages.cs` | Yedi mesaj | 2, 3, 4 |
| `src/WebAPI/Controllers/ClubRoleDefinitionsController.cs` | **Yeni** | 2 |
| `src/Business/DTOs/Clubs/SetClubRoleRequestDto.cs` · `ClubMemberListItemDto.cs` · `MyClubMembershipDto.cs` | Unvan alanları | 3 |
| `src/Business/Concrete/ClubManager.cs` · `ClubApplicationManager.cs` | Yeni kulübe varsayılan set | 4 |
| `arayuz/src/api/types.ts` · `ClubDetailPage.tsx` · `MyClubsPage.tsx` · `schemas/clubRoleDefinitionForm.ts` | Arayüz | 5 |

---

### Task 1: Şema — `ClubRoleDefinition` ve mevcut kulüplere varsayılan set

**Files:**
- Create: `src/Entities/ClubRoleDefinition.cs`, `src/Business/Constants/DefaultClubRoles.cs`
- Create: `src/DataAccess/Configurations/ClubRoleDefinitionConfiguration.cs`
- Modify: `src/Entities/ClubMembership.cs`, `src/DataAccess/Configurations/ClubMembershipConfiguration.cs`, `src/DataAccess/AppDbContext.cs`
- Create (üretilecek): `src/DataAccess/Migrations/<timestamp>_20260826_Faz34_DinamikToplulukRolleri.cs`
- Test: `tests/WebAPI.IntegrationTests/DomainConstraintTests.cs`

**Interfaces:**
- Consumes: — (ilk task)
- Produces:
  - `Entities.ClubRoleDefinition` — `int Id`, `int ClubId`, `required string Name`, `ClubRole ClubRole`, `int DisplayOrder`
  - `Entities.ClubMembership.ClubRoleDefinitionId` — `int?`
  - `Business.Constants.DefaultClubRoles.All` — `IReadOnlyList<(string Name, ClubRole Role, int DisplayOrder)>`

- [ ] **Step 1: Failing test'i yaz**

`tests/WebAPI.IntegrationTests/DomainConstraintTests.cs` — kapanış süslü parantezinden önce:

```csharp
    [Fact(DisplayName = "A-61: aynı kulüpte aynı adla ikinci rol tanımı DB seviyesinde reddedilir")]
    public async Task ClubRoleDefinition_DuplicateNamePerClub_IsRejected()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (club, _, _) = await SeedClubStudentTermAsync(db, "role-dup");

        db.ClubRoleDefinitions.Add(new ClubRoleDefinition { ClubId = club.Id, Name = "Sayman", ClubRole = ClubRole.Officer, DisplayOrder = 1 });
        await db.SaveChangesAsync();

        db.ClubRoleDefinitions.Add(new ClubRoleDefinition { ClubId = club.Id, Name = "Sayman", ClubRole = ClubRole.Member, DisplayOrder = 2 });

        await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
    }

    [Fact(DisplayName = "O-20: iki farklı kulüp aynı unvan adını kullanabilir (kapsam kulüptür)")]
    public async Task ClubRoleDefinition_SameNameAcrossClubs_IsAllowed()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (clubA, _, _) = await SeedClubStudentTermAsync(db, "role-a");
        var (clubB, _, _) = await SeedClubStudentTermAsync(db, "role-b");

        db.ClubRoleDefinitions.AddRange(
            new ClubRoleDefinition { ClubId = clubA.Id, Name = "Sekreter", ClubRole = ClubRole.Officer, DisplayOrder = 1 },
            new ClubRoleDefinition { ClubId = clubB.Id, Name = "Sekreter", ClubRole = ClubRole.Officer, DisplayOrder = 1 });

        await db.SaveChangesAsync();

        Assert.Equal(2, await db.ClubRoleDefinitions.CountAsync(d => d.Name == "Sekreter"));
    }

    [Fact(DisplayName = "A-61: ClubMembership hem yetki seviyesini hem unvanı taşır; unvan nullable")]
    public async Task ClubMembership_CarriesBothRoleAndDefinition()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (club, student, term) = await SeedClubStudentTermAsync(db, "role-both");

        var definition = new ClubRoleDefinition { ClubId = club.Id, Name = "Sayman", ClubRole = ClubRole.Officer, DisplayOrder = 1 };
        db.ClubRoleDefinitions.Add(definition);
        await db.SaveChangesAsync();

        db.ClubMemberships.Add(new ClubMembership
        {
            ClubId = club.Id, StudentId = student.Id, AcademicTermId = term.Id,
            ClubRole = ClubRole.Officer, ClubRoleDefinitionId = definition.Id, JoinedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var reloaded = await db.ClubMemberships.AsNoTracking()
            .SingleAsync(m => m.ClubId == club.Id && m.StudentId == student.Id && m.AcademicTermId == term.Id);

        Assert.Equal(ClubRole.Officer, reloaded.ClubRole);
        Assert.Equal(definition.Id, reloaded.ClubRoleDefinitionId);
    }
```

- [ ] **Step 2: Test'i çalıştır, derlenmediğini gör**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~ClubRoleDefinition_|FullyQualifiedName~ClubMembership_CarriesBoth"
```

Beklenen: **derleme hatası.**

- [ ] **Step 3: Varlığı oluştur**

`src/Entities/ClubRoleDefinition.cs`:

```csharp
using Core.Entities;
using Entities.Enums;

namespace Entities;

/// <summary>
/// docs/MIMARI.md · K-36/A-61/Y-69: kulübe özel rol UNVANI. Yetkinin kaynağı DEĞİLDİR —
/// yalnızca bir `ClubRole` yetki seviyesine bağlanır ve `ClubMembership.ClubRole` o seviyeden
/// yazılır. Yetki kararı veren 7 `Ensure*Access*` metodu bu tipi hiç görmez.
///
/// <b>Y-69:</b> bu sınıfa izin kodu, claim veya `RoleClaim` referansı EKLENEMEZ — Identity'nin
/// yanına ikinci bir yetki sistemi açmak yasaktır (Y-37'nin kulüp karşılığı).
/// `ClubRoleDefinitionShapeTests` ihlali yakalar.
/// </summary>
public sealed class ClubRoleDefinition : IEntity
{
    public int Id { get; set; }

    /// <summary>docs/PLAN-V6.md · O-20: rol tanımları kulübe özeldir; kulüpler birbirinin listesini görmez.</summary>
    public int ClubId { get; set; }

    /// <summary>Görünen unvan: "Sayman", "Sekreter", "Sosyal Medya Sorumlusu". `(ClubId, Name)` unique.</summary>
    public required string Name { get; set; }

    /// <summary>Bu unvanın verdiği yetki seviyesi. Değişirse üyeliklere yayılır (O-27).</summary>
    public ClubRole ClubRole { get; set; }

    public int DisplayOrder { get; set; }
}
```

- [ ] **Step 4: `ClubMembership`'e unvan alanını ekle**

`src/Entities/ClubMembership.cs` — `ClubRole` özelliğinin altına:

```csharp
    /// <summary>
    /// docs/MIMARI.md · K-36/A-61: görünen unvan. Null = unvansız (yalnızca yetki seviyesi).
    /// <b>Yetki kararı bu alandan OKUNMAZ</b> — `ClubRole` yetkinin tek kaynağı olarak kalır.
    /// İkisi asla ayrışmaz: atama ve seviye değişikliği ikisini birlikte yazar (O-27).
    /// </summary>
    public int? ClubRoleDefinitionId { get; set; }
```

- [ ] **Step 5: EF konfigürasyonlarını yaz**

`src/DataAccess/Configurations/ClubRoleDefinitionConfiguration.cs`:

```csharp
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

public sealed class ClubRoleDefinitionConfiguration : IEntityTypeConfiguration<ClubRoleDefinition>
{
    public void Configure(EntityTypeBuilder<ClubRoleDefinition> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Y-18/O-20: aynı kulüpte aynı unvan iki kez tanımlanamaz; farklı kulüpler serbest.
        builder.HasIndex(d => new { d.ClubId, d.Name }).IsUnique();

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(d => d.ClubId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

`src/DataAccess/Configurations/ClubMembershipConfiguration.cs` — mevcut yapılandırmaya ekle:

```csharp
        // A-61: kullanımdaki unvan silinemesin — Restrict, 409'a dönüşür.
        builder.HasOne<ClubRoleDefinition>()
            .WithMany()
            .HasForeignKey(m => m.ClubRoleDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
```

`src/DataAccess/AppDbContext.cs` — `ClubMemberships` satırının üstüne:

```csharp
    public DbSet<ClubRoleDefinition> ClubRoleDefinitions => Set<ClubRoleDefinition>();
```

- [ ] **Step 6: Varsayılan beşliyi tanımla**

`src/Business/Constants/DefaultClubRoles.cs`:

```csharp
using Entities.Enums;

namespace Business.Constants;

/// <summary>
/// docs/PLAN-V6.md · O-20: yeni kulüp doğduğunda kopyalanan varsayılan unvan seti.
/// Kulüp bunları sonradan düzenleyebilir/silebilir — bu bir başlangıç noktası, kural değil.
/// </summary>
public static class DefaultClubRoles
{
    public static readonly IReadOnlyList<(string Name, ClubRole Role, int DisplayOrder)> All =
    [
        ("Başkan", ClubRole.President, 1),
        ("Başkan Yardımcısı", ClubRole.Officer, 2),
        ("Sayman", ClubRole.Officer, 3),
        ("Sekreter", ClubRole.Officer, 4),
        ("Üye", ClubRole.Member, 5),
    ];
}
```

- [ ] **Step 7: Migration üret ve geri doldurma adımını ekle**

```bash
dotnet ef migrations add 20260826_Faz34_DinamikToplulukRolleri --project src/DataAccess --startup-project src/WebAPI
```

`Up()` içinde bir `CreateTable("ClubRoleDefinitions")`, bir `AddColumn<int>("ClubRoleDefinitionId", "ClubMemberships", nullable: true)`, iki `CreateIndex` ve bir `AddForeignKey` olmalı. Fazlası varsa **dur.**

> **Bu adım atlanırsa mevcut kulüplerin "Roller" sekmesi boş açılır** ve kimse sebebini anlamaz.

`Up()` metodunun **sonuna** ekle:

```csharp
        // O-20: mevcut kulüplere varsayılan beşli. Yeni kulüpler bunu Business'ta alır (Task 4);
        // bu satır yalnızca migration anındaki kulüpler içindir. INSERT…SELECT sayesinde tek
        // deyimde ve idempotent değil ama tek seferlik çalıştığı için yeterli.
        migrationBuilder.Sql("""
            INSERT INTO [ClubRoleDefinitions] ([ClubId], [Name], [ClubRole], [DisplayOrder])
            SELECT c.[Id], v.[Name], v.[ClubRole], v.[DisplayOrder]
            FROM [Clubs] c
            CROSS JOIN (VALUES
                (N'Başkan', 2, 1),
                (N'Başkan Yardımcısı', 1, 2),
                (N'Sayman', 1, 3),
                (N'Sekreter', 1, 4),
                (N'Üye', 0, 5)
            ) AS v([Name], [ClubRole], [DisplayOrder]);
            """);
```

> Sayısal değerler `ClubRole` enum'ıyla birebir: `Member = 0`, `Officer = 1`, `President = 2`. Enum değişirse bu SQL de değişmeli — ama Y-06 gereği enum sabittir.

- [ ] **Step 8: Test'i çalıştır ve commit**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~ClubRoleDefinition_|FullyQualifiedName~ClubMembership_CarriesBoth"
dotnet build && dotnet test
git add src/Entities src/DataAccess src/Business/Constants/DefaultClubRoles.cs tests/WebAPI.IntegrationTests/DomainConstraintTests.cs
git commit -m "Faz 34 adim 1: ClubRoleDefinition semasi ve varsayilan set (K-36, A-61)"
```

Beklenen: **3 passed** + tüm takım yeşil.

---

### Task 2: Rol tanımı CRUD ve Y-69 mimari testi

**Files:**
- Create: `tests/Architecture.Tests/ClubRoleDefinitionShapeTests.cs`
- Create: `src/Business/DTOs/Clubs/ClubRoleDefinitionDto.cs`, `CreateClubRoleDefinitionRequestDto.cs`, `UpdateClubRoleDefinitionRequestDto.cs`
- Create: `src/Business/ValidationRules/CreateClubRoleDefinitionRequestValidator.cs`, `UpdateClubRoleDefinitionRequestValidator.cs`
- Modify: `src/Business/Abstract/IClubMemberService.cs`, `src/Business/Concrete/ClubMemberManager.cs`, `src/Business/Constants/Messages.cs`
- Create: `src/WebAPI/Controllers/ClubRoleDefinitionsController.cs`
- Test: `tests/WebAPI.IntegrationTests/ClubMemberManagementTests.cs`

**Interfaces:**
- Consumes: `Entities.ClubRoleDefinition` (Task 1)
- Produces:
  - `ClubRoleDefinitionDto` — `int Id`, `required string Name`, `ClubRole ClubRole`, `int DisplayOrder`
  - `CreateClubRoleDefinitionRequestDto` / `UpdateClubRoleDefinitionRequestDto` — `string Name`, `ClubRole ClubRole`, `int DisplayOrder`
  - `IClubMemberService.GetRoleDefinitionsAsync(int, CancellationToken)` → `Task<IDataResult<IReadOnlyList<ClubRoleDefinitionDto>>>`
  - `.CreateRoleDefinitionAsync` / `.UpdateRoleDefinitionAsync` / `.DeleteRoleDefinitionAsync`
  - `GET/POST/PUT/DELETE /api/clubs/{clubId}/role-definitions`

**Yetki:** okuma `EnsureMemberViewAccessAsync`, yazma `EnsureRoleManagementAccessAsync` — **mevcut metotlar, değiştirilmeden çağrılır.** Yeni izin kodu yok.

- [ ] **Step 1: Y-69 mimari testini yaz**

`tests/Architecture.Tests/ClubRoleDefinitionShapeTests.cs` (yeni dosya):

```csharp
using System.Reflection;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// docs/MIMARI.md · Y-69: topluluk içi rol tanımı izin taşıyamaz — Identity'nin yanına ikinci bir
/// yetki sistemi açmak yasaktır (Y-37'nin kulüp karşılığı). Tanım yalnızca unvan + bir ClubRole
/// yetki seviyesi tutar; yetki kararı ClubMembership.ClubRole'den okunur (A-61).
/// </summary>
public class ClubRoleDefinitionShapeTests
{
    private static readonly string[] ForbiddenPropertyFragments =
        ["Permission", "Claim", "Policy", "Scope", "Grant"];

    [Fact(DisplayName = "Y-69: ClubRoleDefinition izin/claim taşımaz — yalnızca ilkel alanlar ve ClubRole")]
    public void ClubRoleDefinition_Izin_Tasimaz()
    {
        var type = Assembly.Load("Entities").GetType("Entities.ClubRoleDefinition");
        Assert.NotNull(type);

        var properties = type!.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var byName = properties
            .Where(p => ForbiddenPropertyFragments.Any(f => p.Name.Contains(f, StringComparison.OrdinalIgnoreCase)))
            .Select(p => $"{p.Name} (ad)")
            .ToArray();

        // Tip kontrolü: ilkel/string/enum dışında bir şey taşıyorsa (koleksiyon, RoleClaim, vb.) ihlaldir.
        var byType = properties
            .Where(p => !p.PropertyType.IsPrimitive
                        && p.PropertyType != typeof(string)
                        && !p.PropertyType.IsEnum
                        && Nullable.GetUnderlyingType(p.PropertyType) is not { } u
                        || (Nullable.GetUnderlyingType(p.PropertyType) is { } inner && !inner.IsPrimitive && !inner.IsEnum))
            .Select(p => $"{p.Name} ({p.PropertyType.Name})")
            .ToArray();

        var violations = byName.Concat(byType).ToArray();

        Assert.True(violations.Length == 0,
            "ClubRoleDefinition izin/claim taşıyor veya ilkel olmayan bir alan içeriyor: " +
            string.Join(", ", violations) +
            ". Bkz. docs/MIMARI.md Y-69 / A-61 — unvan yetkiden ayrıdır, izinler rol claim'lerinden yönetilir.");
    }
}
```

- [ ] **Step 2: Failing entegrasyon testlerini yaz**

`tests/WebAPI.IntegrationTests/ClubMemberManagementTests.cs` — kapanış süslü parantezinden önce. Dosyadaki `_client`, token yardımcıları ve kulüp/üyelik kurulumunu **dosyadan doğrula** ve adlarını kullan:

```csharp
    [Fact(DisplayName = "O-20: başkan/danışman kendi kulübüne rol tanımı ekler; başka kulüp göremez")]
    public async Task RoleDefinitions_ScopedToClub()
    {
        var advisorToken = await LoginAsync(AdvisorEmail, AdvisorPassword);

        var createResponse = await SendWithBearerAsync(
            HttpMethod.Post, $"/api/clubs/{_clubId}/role-definitions", advisorToken,
            new { Name = "Sosyal Medya Sorumlusu", ClubRole = "Officer", DisplayOrder = 6 });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var listResponse = await SendWithBearerAsync(HttpMethod.Get, $"/api/clubs/{_clubId}/role-definitions", advisorToken);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Contains("Sosyal Medya Sorumlusu", await listResponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Y-23: sıradan üye rol tanımı oluşturamaz — 403")]
    public async Task CreateRoleDefinition_PlainMember_ReturnsForbidden()
    {
        var memberToken = await LoginAsync(MemberEmail, MemberPassword);

        var response = await SendWithBearerAsync(
            HttpMethod.Post, $"/api/clubs/{_clubId}/role-definitions", memberToken,
            new { Name = "Yetkisiz Unvan", ClubRole = "Officer", DisplayOrder = 9 });

        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden,
            $"Beklenen 403, gelen {response.StatusCode}.");
    }

    [Fact(DisplayName = "A-61: kullanımdaki unvan silinemez — 409")]
    public async Task DeleteRoleDefinition_InUse_ReturnsConflict()
    {
        var advisorToken = await LoginAsync(AdvisorEmail, AdvisorPassword);

        var createResponse = await SendWithBearerAsync(
            HttpMethod.Post, $"/api/clubs/{_clubId}/role-definitions", advisorToken,
            new { Name = $"Kullanımda {Guid.NewGuid():N}"[..20], ClubRole = "Officer", DisplayOrder = 7 });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        int definitionId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var definition = await db.ClubRoleDefinitions
                .Where(d => d.ClubId == _clubId)
                .OrderByDescending(d => d.Id)
                .FirstAsync();
            definitionId = definition.Id;

            var membership = await db.ClubMemberships.FirstAsync(m => m.ClubId == _clubId);
            membership.ClubRoleDefinitionId = definitionId;
            membership.ClubRole = definition.ClubRole;
            await db.SaveChangesAsync();
        }

        var deleteResponse = await SendWithBearerAsync(
            HttpMethod.Delete, $"/api/clubs/{_clubId}/role-definitions/{definitionId}", advisorToken);

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }
```

- [ ] **Step 3: Test'leri çalıştır, kırmızı olduğunu gör**

```bash
dotnet test tests/Architecture.Tests/Architecture.Tests.csproj --filter "FullyQualifiedName~ClubRoleDefinitionShapeTests"
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~RoleDefinition"
```

Beklenen: mimari test **PASS** (Task 1'de doğru yazıldıysa — bu bir muhafız, yeni davranış değil); entegrasyon testleri **404 ile FAIL.**

- [ ] **Step 4: DTO'ları ve validator'ları oluştur**

`src/Business/DTOs/Clubs/ClubRoleDefinitionDto.cs`:

```csharp
using Entities.Enums;

namespace Business.DTOs.Clubs;

/// <summary>
/// docs/MIMARI.md · K-36/A-61: unvan + verdiği yetki seviyesi. `ClubRole` arayüzde yardımcı metin
/// olarak gösterilir — "Sayman" unvanını veren kişi Officer yetkisi verdiğini GÖRMEDEN vermemeli.
/// `ClubId` taşınmaz: liste zaten kulüp rotasından geliyor.
/// </summary>
public sealed class ClubRoleDefinitionDto
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public ClubRole ClubRole { get; set; }

    public int DisplayOrder { get; set; }
}
```

`src/Business/DTOs/Clubs/CreateClubRoleDefinitionRequestDto.cs`:

```csharp
using Entities.Enums;

namespace Business.DTOs.Clubs;

public sealed class CreateClubRoleDefinitionRequestDto
{
    public string Name { get; set; } = string.Empty;

    public ClubRole ClubRole { get; set; }

    public int DisplayOrder { get; set; }
}
```

`src/Business/DTOs/Clubs/UpdateClubRoleDefinitionRequestDto.cs` — **aynı üç özellik**:

```csharp
using Entities.Enums;

namespace Business.DTOs.Clubs;

public sealed class UpdateClubRoleDefinitionRequestDto
{
    public string Name { get; set; } = string.Empty;

    /// <summary>docs/PLAN-V6.md · O-27: değişirse bu unvanı taşıyan TÜM üyeliklere aynı transaction'da yayılır.</summary>
    public ClubRole ClubRole { get; set; }

    public int DisplayOrder { get; set; }
}
```

`src/Business/ValidationRules/CreateClubRoleDefinitionRequestValidator.cs`:

```csharp
using Business.DTOs.Clubs;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class CreateClubRoleDefinitionRequestValidator : AbstractValidator<CreateClubRoleDefinitionRequestDto>
{
    public CreateClubRoleDefinitionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ClubRole).IsInEnum();
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
```

`src/Business/ValidationRules/UpdateClubRoleDefinitionRequestValidator.cs`:

```csharp
using Business.DTOs.Clubs;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class UpdateClubRoleDefinitionRequestValidator : AbstractValidator<UpdateClubRoleDefinitionRequestDto>
{
    public UpdateClubRoleDefinitionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ClubRole).IsInEnum();
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
```

- [ ] **Step 5: Mesajları ekle**

`src/Business/Constants/Messages.cs`:

```csharp
    // Faz 34 — Dinamik topluluk içi roller (K-36, A-61, Y-69)
    public const string ClubRoleDefinitionNotFound = "Rol tanımı bulunamadı.";
    public const string ClubRoleDefinitionNameTaken = "Bu toplulukta bu isimde bir rol tanımı zaten var.";
    public const string ClubRoleDefinitionCreated = "Rol tanımı eklendi.";
    public const string ClubRoleDefinitionUpdated = "Rol tanımı güncellendi.";
    public const string ClubRoleDefinitionDeleted = "Rol tanımı silindi.";
    public const string ClubRoleDefinitionInUse = "Bu unvanı taşıyan üyeler var; önce onları başka bir unvana taşıyın.";
    public const string ClubRoleDefinitionWouldCreateSecondPresident =
        "Bu unvanı birden fazla üye taşıyor; Başkan seviyesine yükseltilemez (bir toplulukta tek başkan olur).";
```

- [ ] **Step 6: Servis sözleşmesini ve manager'ı yaz**

`src/Business/Abstract/IClubMemberService.cs` — dört bildirim ekle:

```csharp
    /// <summary>docs/MIMARI.md · K-36/O-20: kulübün rol tanımları. Küçük ve kulübe özel — sayfalanmaz.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.MembershipsRead)]
    Task<IDataResult<IReadOnlyList<ClubRoleDefinitionDto>>> GetRoleDefinitionsAsync(
        int clubId, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.MembershipsWrite)]
    [ValidationAspect(typeof(CreateClubRoleDefinitionRequestValidator))]
    [TransactionAspect]
    Task<IDataResult<ClubRoleDefinitionDto>> CreateRoleDefinitionAsync(
        int clubId, CreateClubRoleDefinitionRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>docs/PLAN-V6.md · O-27: seviye değişikliği üyeliklere yayılır; ikinci başkan üretecekse 409.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.MembershipsWrite)]
    [ValidationAspect(typeof(UpdateClubRoleDefinitionRequestValidator))]
    [TransactionAspect]
    Task<IResult> UpdateRoleDefinitionAsync(
        int clubId, int definitionId, UpdateClubRoleDefinitionRequestDto request, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.MembershipsWrite)]
    [TransactionAspect]
    Task<IResult> DeleteRoleDefinitionAsync(int clubId, int definitionId, CancellationToken cancellationToken = default);
```

`src/Business/Concrete/ClubMemberManager.cs` — kurucuya `IEntityRepository<ClubRoleDefinition> clubRoleDefinitionRepository,` ekle ve üç metodu yaz (dördüncüsü Task 4'te):

```csharp
    public async Task<IDataResult<IReadOnlyList<ClubRoleDefinitionDto>>> GetRoleDefinitionsAsync(
        int clubId, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return DataResult<IReadOnlyList<ClubRoleDefinitionDto>>.NotFound(Messages.ClubNotFound);
        }

        // Y-23: mevcut kapsam metodu DEĞİŞTİRİLMEDEN çağrılır — bu fazın sözü budur.
        var accessError = await EnsureMemberViewAccessAsync(club, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return DataResult<IReadOnlyList<ClubRoleDefinitionDto>>.Forbidden(accessError);
        }

        var definitions = await clubRoleDefinitionRepository
            .GetListAsync(d => d.ClubId == clubId, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ClubRoleDefinitionDto> items = definitions
            .OrderBy(d => d.DisplayOrder)
            .ThenBy(d => d.Id)
            .Select(ToDto)
            .ToList();

        return DataResult<IReadOnlyList<ClubRoleDefinitionDto>>.Success(items);
    }

    public async Task<IDataResult<ClubRoleDefinitionDto>> CreateRoleDefinitionAsync(
        int clubId, CreateClubRoleDefinitionRequestDto request, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return DataResult<ClubRoleDefinitionDto>.NotFound(Messages.ClubNotFound);
        }

        var accessError = await EnsureRoleManagementAccessAsync(club, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return DataResult<ClubRoleDefinitionDto>.Forbidden(accessError);
        }

        var name = request.Name.Trim();
        var duplicate = await clubRoleDefinitionRepository
            .GetAsync(d => d.ClubId == clubId && d.Name == name, cancellationToken)
            .ConfigureAwait(false);
        if (duplicate is not null)
        {
            return DataResult<ClubRoleDefinitionDto>.Conflict(Messages.ClubRoleDefinitionNameTaken);
        }

        var definition = new ClubRoleDefinition
        {
            ClubId = clubId, Name = name, ClubRole = request.ClubRole, DisplayOrder = request.DisplayOrder,
        };

        await clubRoleDefinitionRepository.AddAsync(definition, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return DataResult<ClubRoleDefinitionDto>.Success(ToDto(definition), Messages.ClubRoleDefinitionCreated);
    }

    public async Task<IResult> DeleteRoleDefinitionAsync(int clubId, int definitionId, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return Result.NotFound(Messages.ClubNotFound);
        }

        var accessError = await EnsureRoleManagementAccessAsync(club, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return Result.Forbidden(accessError);
        }

        var definition = await clubRoleDefinitionRepository
            .GetAsync(d => d.Id == definitionId && d.ClubId == clubId, cancellationToken)
            .ConfigureAwait(false);
        if (definition is null)
        {
            return Result.NotFound(Messages.ClubRoleDefinitionNotFound);
        }

        // A-61: kullanımdaysa açık mesajla reddet. FK Restrict de yakalar ama mesajı o söylemez.
        var inUse = await clubMembershipRepository
            .GetAsync(m => m.ClubRoleDefinitionId == definitionId, cancellationToken)
            .ConfigureAwait(false);
        if (inUse is not null)
        {
            return Result.Conflict(Messages.ClubRoleDefinitionInUse);
        }

        clubRoleDefinitionRepository.Delete(definition);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.ClubRoleDefinitionDeleted);
    }

    private static ClubRoleDefinitionDto ToDto(ClubRoleDefinition d) => new()
    {
        Id = d.Id, Name = d.Name, ClubRole = d.ClubRole, DisplayOrder = d.DisplayOrder,
    };
```

- [ ] **Step 7: Controller'ı oluştur**

`src/WebAPI/Controllers/ClubRoleDefinitionsController.cs`:

```csharp
using Business.Abstract;
using Business.DTOs.Clubs;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

/// <summary>docs/MIMARI.md · K-36/O-20: rol tanımları kulübe özeldir — rota kulübün altındadır.</summary>
[ApiController]
[Route("api/clubs/{clubId:int}/role-definitions")]
public sealed class ClubRoleDefinitionsController(IClubMemberService clubMemberService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRoleDefinitions(int clubId, CancellationToken cancellationToken)
    {
        var result = await clubMemberService.GetRoleDefinitionsAsync(clubId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoleDefinition(int clubId, CreateClubRoleDefinitionRequestDto request, CancellationToken cancellationToken)
    {
        var result = await clubMemberService.CreateRoleDefinitionAsync(clubId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{definitionId:int}")]
    public async Task<IActionResult> UpdateRoleDefinition(
        int clubId, int definitionId, UpdateClubRoleDefinitionRequestDto request, CancellationToken cancellationToken)
    {
        var result = await clubMemberService.UpdateRoleDefinitionAsync(clubId, definitionId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{definitionId:int}")]
    public async Task<IActionResult> DeleteRoleDefinition(int clubId, int definitionId, CancellationToken cancellationToken)
    {
        var result = await clubMemberService.DeleteRoleDefinitionAsync(clubId, definitionId, cancellationToken);
        return result.ToActionResult();
    }
}
```

> `UpdateRoleDefinitionAsync` Task 4'te yazılacak. Bu adımda derlemenin geçmesi için manager'a geçici bir gövde koyma — **Task 4'ü aynı oturumda tamamla** veya `UpdateRoleDefinitionAsync`'i Task 4'e kadar sözleşmeden çıkar.

- [ ] **Step 8: Test'leri çalıştır ve commit**

```bash
dotnet test tests/Architecture.Tests/Architecture.Tests.csproj
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~RoleDefinitions_ScopedToClub|FullyQualifiedName~CreateRoleDefinition_PlainMember|FullyQualifiedName~DeleteRoleDefinition_InUse"
git add src/Business src/WebAPI/Controllers/ClubRoleDefinitionsController.cs tests
git commit -m "Faz 34 adim 2: rol tanimi CRUD ve Y-69 mimari testi (K-36, Y-69)"
```

---

### Task 3: Unvan atama — iki alan asla ayrışmaz

**Files:**
- Modify: `src/Business/DTOs/Clubs/SetClubRoleRequestDto.cs`, `ClubMemberListItemDto.cs`, `MyClubMembershipDto.cs`
- Modify: `src/Business/Concrete/ClubMemberManager.cs` (`SetRoleAsync`, `GetMembersPagedAsync`, `GetMineAsync`)
- Test: `tests/Business.Tests/ClubMemberManagerTests.cs`

**Interfaces:**
- Consumes: `ClubRoleDefinition` (Task 1), `ClubRoleDefinitionDto` (Task 2)
- Produces:
  - `SetClubRoleRequestDto.ClubRoleDefinitionId` — `int?`
  - `ClubMemberListItemDto.ClubRoleDefinitionId` (`int?`), `.ClubRoleName` (`string?`)
  - `MyClubMembershipDto.ClubRoleName` (`string?`)

**Kural (A-61/Y-22):** `ClubRoleDefinitionId` verilmişse `ClubRole` **tanımdan** okunur, istemciden gelen değer **yok sayılır.** Verilmemişse bugünkü davranış sürer (unvansız atama) — mevcut testler ve akışlar kırılmaz.

- [ ] **Step 1: Failing test'leri yaz**

`tests/Business.Tests/ClubMemberManagerTests.cs` — kapanış süslü parantezinden önce. Alan bloğuna `private readonly Mock<IEntityRepository<ClubRoleDefinition>> _clubRoleDefinitionRepository = new();` ekle ve `_sut` kurucusuna **doğru konumda** geçir.

```csharp
    [Fact(DisplayName = "A-61/Y-22: unvan atanınca ClubRole TANIMDAN gelir, istemcinin gönderdiği değer yok sayılır")]
    public async Task SetRoleAsync_WithDefinition_TakesRoleFromDefinitionNotRequest()
    {
        var membership = new ClubMembership
        {
            Id = 5, ClubId = 1, StudentId = 9, AcademicTermId = 1, ClubRole = ClubRole.Member, JoinedAtUtc = FixedNow,
        };
        _clubMembershipRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
        _clubRoleDefinitionRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubRoleDefinition, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubRoleDefinition { Id = 3, ClubId = 1, Name = "Sayman", ClubRole = ClubRole.Officer, DisplayOrder = 3 });

        GrantAdminScope();

        // İstemci "Member" diyor ama tanım "Officer" — tanım kazanmalı.
        var result = await _sut.SetRoleAsync(1, 5, new SetClubRoleRequestDto
        {
            ClubRole = ClubRole.Member,
            ClubRoleDefinitionId = 3,
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(ClubRole.Officer, membership.ClubRole);
        Assert.Equal(3, membership.ClubRoleDefinitionId);
    }

    [Fact(DisplayName = "A-61: unvansız atama bugünkü davranışı sürdürür — ClubRole istekten gelir, unvan null olur")]
    public async Task SetRoleAsync_WithoutDefinition_KeepsLegacyBehaviour()
    {
        var membership = new ClubMembership
        {
            Id = 5, ClubId = 1, StudentId = 9, AcademicTermId = 1, ClubRole = ClubRole.Member,
            ClubRoleDefinitionId = 7, JoinedAtUtc = FixedNow,
        };
        _clubMembershipRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);

        GrantAdminScope();

        var result = await _sut.SetRoleAsync(1, 5, new SetClubRoleRequestDto { ClubRole = ClubRole.Officer });

        Assert.True(result.IsSuccess);
        Assert.Equal(ClubRole.Officer, membership.ClubRole);
        Assert.Null(membership.ClubRoleDefinitionId);
    }

    [Fact(DisplayName = "O-20: başka kulübün unvanı atanamaz — 404")]
    public async Task SetRoleAsync_DefinitionFromAnotherClub_ReturnsNotFound()
    {
        _clubMembershipRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubMembership { Id = 5, ClubId = 1, StudentId = 9, AcademicTermId = 1, ClubRole = ClubRole.Member, JoinedAtUtc = FixedNow });

        // Predicate GERÇEKTEN çalıştırılır: tanım ClubId = 2, istek ClubId = 1 → eşleşme yok.
        _clubRoleDefinitionRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubRoleDefinition, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ClubRoleDefinition, bool>> filter, CancellationToken _) =>
                new[] { new ClubRoleDefinition { Id = 3, ClubId = 2, Name = "Sayman", ClubRole = ClubRole.Officer, DisplayOrder = 3 } }
                    .AsQueryable().Where(filter).FirstOrDefault());

        GrantAdminScope();

        var result = await _sut.SetRoleAsync(1, 5, new SetClubRoleRequestDto { ClubRoleDefinitionId = 3 });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    /// <summary>Y-66: yönetici yolu — kapsam metotlarının ilk satırı. Bu testler kapsamı değil atamayı sınıyor.</summary>
    private void GrantAdminScope() =>
        _currentUser.Setup(c => c.Permissions).Returns([IdentitySeedData.Permissions.ClubsManageAll]);
```

> Dosyada zaten `_clubRepository` güncel kulübü döndürüyorsa `GrantAdminScope()` yeterlidir; döndürmüyorsa `_clubRepository.Setup(...)` satırını da ekle. `using DataAccess.Seed;` ve `using Core.Utilities.Results;` gerekli.

- [ ] **Step 2: Test'leri çalıştır, kırmızı olduğunu gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~SetRoleAsync_WithDefinition|FullyQualifiedName~SetRoleAsync_WithoutDefinition|FullyQualifiedName~SetRoleAsync_DefinitionFromAnotherClub"
```

Beklenen: **derleme hatası** — `SetClubRoleRequestDto.ClubRoleDefinitionId` yok.

- [ ] **Step 3: DTO'lara alanları ekle**

`src/Business/DTOs/Clubs/SetClubRoleRequestDto.cs` — sınıfa ekle:

```csharp
    /// <summary>
    /// docs/MIMARI.md · K-36/A-61: atanacak unvan. Verilirse `ClubRole` TANIMDAN okunur ve
    /// yukarıdaki alan yok sayılır (Y-22). Null ise unvansız atama — bugünkü davranış.
    /// </summary>
    public int? ClubRoleDefinitionId { get; set; }
```

`src/Business/DTOs/Clubs/ClubMemberListItemDto.cs` — `ClubRole` altına:

```csharp
    public int? ClubRoleDefinitionId { get; set; }

    /// <summary>Görünen unvan. Null ise arayüz yetki seviyesine düşer.</summary>
    public string? ClubRoleName { get; set; }
```

`src/Business/DTOs/Clubs/MyClubMembershipDto.cs` — `ClubRole` altına:

```csharp
    /// <summary>docs/MIMARI.md · K-36: görünen unvan. Null ise arayüz yetki seviyesine düşer.</summary>
    public string? ClubRoleName { get; set; }
```

- [ ] **Step 4: `SetRoleAsync`'e unvan çözümlemesini ekle**

`src/Business/Concrete/ClubMemberManager.cs` — `SetRoleAsync` içinde, erişim kontrolünden **sonra**, `CannotChangeOwnPresidentRole` kontrolünden **önce** ekle:

```csharp
        // A-61/Y-22: unvan verilmişse yetki seviyesi TANIMDAN okunur — istemcinin gönderdiği
        // ClubRole yok sayılır. İki alan asla ayrışmaz; yetki kararı yine ClubRole'den okunacak.
        var targetRole = request.ClubRole;
        int? targetDefinitionId = null;

        if (request.ClubRoleDefinitionId is { } definitionId)
        {
            // O-20: tanım BU kulübe ait olmalı — başka kulübün unvanı atanamaz.
            var definition = await clubRoleDefinitionRepository
                .GetAsync(d => d.Id == definitionId && d.ClubId == clubId, cancellationToken)
                .ConfigureAwait(false);
            if (definition is null)
            {
                return Result.NotFound(Messages.ClubRoleDefinitionNotFound);
            }

            targetRole = definition.ClubRole;
            targetDefinitionId = definition.Id;
        }
```

Ardından, aynı metotta `request.ClubRole` geçen **üç yeri** `targetRole` ile değiştir:
- kendi başkanlığını kaldırma muhafızı (`request.ClubRole != ClubRole.President` → `targetRole != ClubRole.President`)
- başkan tekilliği kontrolü (`request.ClubRole == ClubRole.President` → `targetRole == ClubRole.President`)
- atama satırı

Atama satırlarını şununla değiştir:

```csharp
        membership.ClubRole = targetRole;
        membership.ClubRoleDefinitionId = targetDefinitionId;
        clubMembershipRepository.Update(membership);
```

- [ ] **Step 5: Liste metotlarına unvan adını ekle**

`src/Business/Concrete/ClubMemberManager.cs`:

**(a)** `GetMembersPagedAsync` — `studentNumbers` sözlüğünün altına:

```csharp
        var definitionIds = paged.Items.Where(m => m.ClubRoleDefinitionId is not null)
            .Select(m => m.ClubRoleDefinitionId!.Value).Distinct().ToList();
        var definitionNames = definitionIds.Count == 0
            ? new Dictionary<int, string>()
            : (await clubRoleDefinitionRepository.GetListAsync(d => definitionIds.Contains(d.Id), cancellationToken).ConfigureAwait(false))
                .ToDictionary(d => d.Id, d => d.Name);
```

ve `new ClubMemberListItemDto { … }` başlatıcısına:

```csharp
            ClubRoleDefinitionId = m.ClubRoleDefinitionId,
            ClubRoleName = m.ClubRoleDefinitionId is { } did ? definitionNames.GetValueOrDefault(did) : null,
```

**(b)** `GetMineAsync` — `clubsById` sözlüğünün altına **aynı iki bloğu** ekle (`memberships` üzerinde):

```csharp
        var definitionIds = memberships.Where(m => m.ClubRoleDefinitionId is not null)
            .Select(m => m.ClubRoleDefinitionId!.Value).Distinct().ToList();
        var definitionNames = definitionIds.Count == 0
            ? new Dictionary<int, string>()
            : (await clubRoleDefinitionRepository.GetListAsync(d => definitionIds.Contains(d.Id), cancellationToken).ConfigureAwait(false))
                .ToDictionary(d => d.Id, d => d.Name);
```

ve `new MyClubMembershipDto { … }` başlatıcısına:

```csharp
                    ClubRoleName = m.ClubRoleDefinitionId is { } did ? definitionNames.GetValueOrDefault(did) : null,
```

- [ ] **Step 6: Test'leri çalıştır ve commit**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~ClubMemberManagerTests"
dotnet build && dotnet test
git add src/Business tests/Business.Tests/ClubMemberManagerTests.cs
git commit -m "Faz 34 adim 3: unvan atama, iki alan ayrismiyor (K-36, A-61)"
```

Beklenen: yeni 3 test + **mevcut `ClubMemberManagerTests`'in tamamı yeşil** — unvansız akış hiç değişmedi.

---

### Task 4: Seviye değişikliğinin yayılması ve yeni kulübün varsayılan seti

**Files:**
- Modify: `src/Business/Concrete/ClubMemberManager.cs` (`UpdateRoleDefinitionAsync`)
- Modify: `src/Business/Concrete/ClubManager.cs` (`CreateAsync`)
- Modify: `src/Business/Concrete/ClubApplicationManager.cs` (`DecideAsync`)
- Test: `tests/Business.Tests/ClubMemberManagerTests.cs`, `tests/WebAPI.IntegrationTests/ClubApplicationFlowTests.cs`

**Interfaces:**
- Consumes: `DefaultClubRoles.All` (Task 1), `ClubRoleDefinition` (Task 1)
- Produces: `IClubMemberService.UpdateRoleDefinitionAsync` implementasyonu (sözleşme Task 2'de)

- [ ] **Step 1: Failing test'leri yaz**

`tests/Business.Tests/ClubMemberManagerTests.cs` — ekle:

```csharp
    [Fact(DisplayName = "O-27: tanımın seviyesi değişince o unvanı taşıyan TÜM üyeliklerin ClubRole'ü de değişir")]
    public async Task UpdateRoleDefinitionAsync_LevelChange_CascadesToMemberships()
    {
        var definition = new ClubRoleDefinition { Id = 3, ClubId = 1, Name = "Sayman", ClubRole = ClubRole.Member, DisplayOrder = 3 };
        var holders = new List<ClubMembership>
        {
            new() { Id = 11, ClubId = 1, StudentId = 21, AcademicTermId = 1, ClubRole = ClubRole.Member, ClubRoleDefinitionId = 3, JoinedAtUtc = FixedNow },
            new() { Id = 12, ClubId = 1, StudentId = 22, AcademicTermId = 1, ClubRole = ClubRole.Member, ClubRoleDefinitionId = 3, JoinedAtUtc = FixedNow },
        };

        _clubRoleDefinitionRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubRoleDefinition, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);
        _clubMembershipRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(holders);

        GrantAdminScope();

        var result = await _sut.UpdateRoleDefinitionAsync(1, 3, new UpdateClubRoleDefinitionRequestDto
        {
            Name = "Sayman", ClubRole = ClubRole.Officer, DisplayOrder = 3,
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(ClubRole.Officer, definition.ClubRole);
        Assert.All(holders, m => Assert.Equal(ClubRole.Officer, m.ClubRole));
    }

    [Fact(DisplayName = "O-27/A-39: iki üyenin taşıdığı unvan Başkan seviyesine yükseltilemez — 409")]
    public async Task UpdateRoleDefinitionAsync_WouldCreateSecondPresident_ReturnsConflict()
    {
        var definition = new ClubRoleDefinition { Id = 3, ClubId = 1, Name = "Sayman", ClubRole = ClubRole.Officer, DisplayOrder = 3 };
        var holders = new List<ClubMembership>
        {
            new() { Id = 11, ClubId = 1, StudentId = 21, AcademicTermId = 1, ClubRole = ClubRole.Officer, ClubRoleDefinitionId = 3, JoinedAtUtc = FixedNow },
            new() { Id = 12, ClubId = 1, StudentId = 22, AcademicTermId = 1, ClubRole = ClubRole.Officer, ClubRoleDefinitionId = 3, JoinedAtUtc = FixedNow },
        };

        _clubRoleDefinitionRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubRoleDefinition, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);
        _clubMembershipRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(holders);

        GrantAdminScope();

        var result = await _sut.UpdateRoleDefinitionAsync(1, 3, new UpdateClubRoleDefinitionRequestDto
        {
            Name = "Sayman", ClubRole = ClubRole.President, DisplayOrder = 3,
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal(ClubRole.Officer, definition.ClubRole);
    }
```

`tests/WebAPI.IntegrationTests/ClubApplicationFlowTests.cs` — ekle:

```csharp
    [Fact(DisplayName = "O-20: onayla doğan kulüp beş varsayılan rol tanımıyla gelir")]
    public async Task Approve_CreatesDefaultRoleDefinitions()
    {
        var proposedName = $"Varsayilan Rol {Guid.NewGuid():N}"[..30];
        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);

        Assert.Equal(HttpStatusCode.OK, (await SubmitApplicationAsync(studentToken)).StatusCode);

        int applicationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            applicationId = (await db.ClubApplications.OrderByDescending(a => a.Id).FirstAsync()).Id;
        }

        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        var decision = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/club-applications/{applicationId}/decision", adminToken,
            new { Status = "Approved", ReviewNote = (string?)null });
        Assert.Equal(HttpStatusCode.OK, decision.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var application = await db.ClubApplications.SingleAsync(a => a.Id == applicationId);
            Assert.NotNull(application.CreatedClubId);

            var definitions = await db.ClubRoleDefinitions
                .Where(d => d.ClubId == application.CreatedClubId!.Value)
                .OrderBy(d => d.DisplayOrder)
                .ToListAsync();

            Assert.Equal(5, definitions.Count);
            Assert.Equal("Başkan", definitions[0].Name);
            Assert.Equal(ClubRole.President, definitions[0].ClubRole);
        }
    }
```

> `SubmitApplicationAsync` yardımcısı Faz 31 Task 3'te eklendi. Faz 31 uygulanmadıysa gövdeyi oradaki gibi elle yaz (`ProposedName = proposedName` olacak şekilde).

- [ ] **Step 2: Test'leri çalıştır, kırmızı olduğunu gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~UpdateRoleDefinitionAsync"
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~Approve_CreatesDefaultRoleDefinitions"
```

- [ ] **Step 3: `UpdateRoleDefinitionAsync`'i yaz (O-27)**

`src/Business/Concrete/ClubMemberManager.cs` — `DeleteRoleDefinitionAsync`'in üstüne:

```csharp
    public async Task<IResult> UpdateRoleDefinitionAsync(
        int clubId, int definitionId, UpdateClubRoleDefinitionRequestDto request, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return Result.NotFound(Messages.ClubNotFound);
        }

        var accessError = await EnsureRoleManagementAccessAsync(club, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return Result.Forbidden(accessError);
        }

        var definition = await clubRoleDefinitionRepository
            .GetAsync(d => d.Id == definitionId && d.ClubId == clubId, cancellationToken)
            .ConfigureAwait(false);
        if (definition is null)
        {
            return Result.NotFound(Messages.ClubRoleDefinitionNotFound);
        }

        var name = request.Name.Trim();
        var duplicate = await clubRoleDefinitionRepository
            .GetAsync(d => d.Id != definitionId && d.ClubId == clubId && d.Name == name, cancellationToken)
            .ConfigureAwait(false);
        if (duplicate is not null)
        {
            return Result.Conflict(Messages.ClubRoleDefinitionNameTaken);
        }

        var levelChanged = definition.ClubRole != request.ClubRole;

        // O-27: seviye değişikliği bu unvanı taşıyan TÜM üyeliklere yayılır — iki alan asla ayrışmaz.
        // Kulüp üye sayıları onlarla ifade edildiği için A-51'in audit patlaması uyarısı burada geçerli değil.
        List<ClubMembership> holders = [];
        if (levelChanged)
        {
            holders = await clubMembershipRepository
                .GetListAsync(m => m.ClubRoleDefinitionId == definitionId, cancellationToken)
                .ConfigureAwait(false);

            // A-39: bir kulüpte tek başkan. Filtreli unique index de yakalar ama sebebi söylemez.
            if (request.ClubRole == ClubRole.President && holders.Count > 1)
            {
                return Result.Conflict(Messages.ClubRoleDefinitionWouldCreateSecondPresident);
            }
        }

        definition.Name = name;
        definition.ClubRole = request.ClubRole;
        definition.DisplayOrder = request.DisplayOrder;
        clubRoleDefinitionRepository.Update(definition);

        foreach (var membership in holders)
        {
            membership.ClubRole = request.ClubRole;
            clubMembershipRepository.Update(membership);
        }

        // [TransactionAspect] tek transaction'ı garanti eder — tanım ve üyelikler birlikte yazılır.
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.ClubRoleDefinitionUpdated);
    }
```

- [ ] **Step 4: Yeni kulübe varsayılan seti ekle**

`src/Business/Concrete/ClubManager.cs`:

**(a)** Kurucuya `IEntityRepository<ClubRoleDefinition> clubRoleDefinitionRepository,` ekle.

**(b)** `CreateAsync` içinde, kulüp kaydedildikten (`SaveChangesAsync` ile `club.Id` üretildikten) **sonra**:

```csharp
        // O-20: yeni kulüp varsayılan unvan setiyle doğar — "Roller" sekmesi boş açılmasın.
        foreach (var (roleName, role, displayOrder) in DefaultClubRoles.All)
        {
            await clubRoleDefinitionRepository.AddAsync(
                new ClubRoleDefinition { ClubId = club.Id, Name = roleName, ClubRole = role, DisplayOrder = displayOrder },
                cancellationToken).ConfigureAwait(false);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
```

`src/Business/Concrete/ClubApplicationManager.cs`:

**(a)** Kurucuya `IEntityRepository<ClubRoleDefinition> clubRoleDefinitionRepository,` ekle.

**(b)** `DecideAsync` içinde, `club` kaydedildikten sonra ve `membership` eklenmeden **önce** ekle:

```csharp
                    // O-20: onayla doğan kulüp de varsayılan unvan setini alır (ClubManager.CreateAsync ile aynı).
                    foreach (var (roleName, role, displayOrder) in DefaultClubRoles.All)
                    {
                        await clubRoleDefinitionRepository.AddAsync(
                            new ClubRoleDefinition { ClubId = club.Id, Name = roleName, ClubRole = role, DisplayOrder = displayOrder },
                            cancellationToken).ConfigureAwait(false);
                    }
```

İki dosyaya da `using Business.Constants;` ekle (yoksa).

- [ ] **Step 5: Test'leri çalıştır ve commit**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~UpdateRoleDefinitionAsync"
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~Approve_CreatesDefaultRoleDefinitions"
dotnet build && dotnet test
git add src/Business tests
git commit -m "Faz 34 adim 4: seviye yayilimi ve yeni kulup varsayilan seti (K-36, O-27, O-20)"
```

---

### Task 5: Arayüz — Roller sekmesi ve unvan gösterimi

**Files:**
- Modify: `arayuz/src/api/types.ts`
- Create: `arayuz/src/schemas/clubRoleDefinitionForm.ts`
- Modify: `arayuz/src/pages/ClubDetailPage.tsx`, `arayuz/src/pages/MyClubsPage.tsx`, `arayuz/src/components/ui/StatusChip.tsx`

**Interfaces:**
- Consumes: `/api/clubs/{clubId}/role-definitions` (Task 2), unvan alanları (Task 3)
- Produces: `arayuz/src/api/types.ts` → `ClubRoleDefinitionDto`

- [ ] **Step 1: TS tiplerini ekle**

`arayuz/src/api/types.ts`:

```typescript
// src/Business/DTOs/Clubs/ClubRoleDefinitionDto.cs
export interface ClubRoleDefinitionDto {
  id: number
  name: string
  clubRole: ClubRole
  displayOrder: number
}
```

> `ClubRole` tipi `StatusChip.tsx` içinde yerel olarak tanımlı (`'Member' | 'Officer' | 'President'`). **Onu `api/types.ts`'e taşı** ve `StatusChip.tsx`'ten import et — iki yerde iki tanım, Y-29'un "tekrarlama" gerekçesiyle aynı sınıf.

`ClubMemberListItemDto` ve `MyClubMembershipDto` arayüzlerine ekle:

```typescript
  clubRoleDefinitionId: number | null   // yalnızca ClubMemberListItemDto
  clubRoleName: string | null
```

- [ ] **Step 2: Form şemasını oluştur**

`arayuz/src/schemas/clubRoleDefinitionForm.ts`:

```typescript
import { z } from 'zod'

// Y-35: yalnızca biçim. "Bu unvan alınmış mı", "ikinci başkan üretir mi" kararları API'nin.
export const clubRoleDefinitionFormSchema = z.object({
  name: z.string().min(1, 'Unvan adı gerekli.').max(100),
  clubRole: z.enum(['Member', 'Officer', 'President']),
  displayOrder: z.number().int().min(0),
})

export type ClubRoleDefinitionFormValues = z.infer<typeof clubRoleDefinitionFormSchema>

export const emptyClubRoleDefinitionFormValues: ClubRoleDefinitionFormValues = {
  name: '',
  clubRole: 'Member',
  displayOrder: 0,
}
```

- [ ] **Step 3: `ClubDetailPage`'e "Roller" sekmesini ekle**

Sekme yalnızca yetkiliye görünür; **karar API'nin** — sekme açılır, uç 403 dönerse hata mesajı gösterilir (Y-35: sekmeyi gizlemek yetki değildir, kolaylıktır).

Sekme içeriği: `ClubRoleDefinitionDto[]` listesi (`GET /clubs/{clubId}/role-definitions`), "Yeni Unvan" düğmesi, satır başına düzenle/sil. Form alanları: ad (`TextField`), yetki seviyesi (`TextField select` — üç `MenuItem`), sıra (`TextField type="number"`).

Yetki seviyesi seçicisinin altına **zorunlu** yardımcı metin:

```tsx
<Typography variant="caption" color="text.secondary">
  Bu unvanı alan üye, seçtiğiniz yetki seviyesinin tüm haklarını kazanır.
</Typography>
```

> Bu metin kasıtlı: "Sayman" unvanını veren kişi, aynı zamanda Officer yetkisi verdiğini **görmeden** vermemeli (A-61).

Silme onayında `ConfirmDialog` kullan; 409 mesajını API'den olduğu gibi göster.

- [ ] **Step 4: Üye listesinde ve "Kulüplerim"de unvanı göster**

`ClubDetailPage` üye listesi "Rol" kolonu:

```tsx
      renderCell: (params) =>
        params.row.clubRoleName ? (
          <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
            <Chip size="small" label={params.row.clubRoleName} />
            <ClubRoleChip role={params.row.clubRole} />
          </Stack>
        ) : (
          <ClubRoleChip role={params.row.clubRole} />
        ),
```

Rol atama diyaloğuna unvan seçici ekle; seçilen unvanın yetki seviyesi yardımcı metin olarak görünsün. Payload: `{ clubRoleDefinitionId: <id> }` — **`clubRole` gönderme**, sunucu tanımdan okuyor (Y-22).

`arayuz/src/pages/MyClubsPage.tsx` — üyelik kartında `clubRoleName` varsa onu, yoksa `ClubRoleChip`'i göster.

- [ ] **Step 5: Build, lint ve elle doğrulama**

```bash
cd arayuz && npm run build && npm run lint
```

1. Başkan/danışman olarak kulüp detayı → **Roller** sekmesi; beş varsayılan unvan görünmeli.
2. "Sosyal Medya Sorumlusu" (Officer) ekle.
3. Bir üyeye bu unvanı ata → üye listesinde unvan **ve** yetki rozeti görünmeli.
4. O üyeyle giriş yap → etkinlik oluşturabilmeli (**Officer yetkisi gerçekten geldi**).
5. Unvanın seviyesini Member'a düşür → aynı üye artık etkinlik oluşturamamalı.
6. Kullanımdaki unvanı silmeye çalış → **409** ve açık mesaj.
7. Başka bir kulübün detayında bu unvan **görünmemeli**.

- [ ] **Step 6: Commit**

```bash
git add arayuz/src
git commit -m "Faz 34 adim 5: Roller sekmesi ve unvan gosterimi (K-36)"
```

---

## Faz Kapanışı

- [ ] **Fazın sözünü doğrula**

```bash
git diff master --stat -- src/Business/Concrete/EventManager.cs src/Business/Concrete/EventParticipationManager.cs src/Business/Concrete/AnnouncementManager.cs src/Business/Concrete/FileManager.cs src/Business/Concrete/ReportScopeResolver.cs
```

Beklenen: **hiçbir çıktı yok.** Bu beş dosya değişmişse fazın sözü tutulmamış demektir — sebebi araştır.

`ClubMemberManager.cs` değişti (yeni metotlar eklendi) ama iki `Ensure*Access*` metodunun **gövdesi** değişmemiş olmalı:

```bash
git diff master -- src/Business/Concrete/ClubMemberManager.cs | grep -A 3 "EnsureMemberViewAccessAsync\|EnsureRoleManagementAccessAsync"
```

Yalnızca **çağrı** satırları görünmeli, gövde değişikliği değil.

- [ ] **Tam doğrulama**

```bash
dotnet build
dotnet test
cd arayuz && npm run build && npm run lint && cd ..
```

- [ ] **"Bitti sayılır" kontrolü** (`docs/PLAN-V6.md` §Faz 34)

| Koşul | Nasıl doğrulanır |
|---|---|
| Başkan kendi kulübüne unvan tanımlayıp atayabiliyor | `RoleDefinitions_ScopedToClub` + elle doğrulama 2-3 |
| Unvan doğru yetki seviyesini veriyor | `SetRoleAsync_WithDefinition_TakesRoleFromDefinitionNotRequest` + elle doğrulama 4 |
| Başka kulüp bu unvanı göremiyor | `ClubRoleDefinition_SameNameAcrossClubs_IsAllowed` + `SetRoleAsync_DefinitionFromAnotherClub` + elle doğrulama 7 |
| Kullanımdaki unvan silinemiyor | `DeleteRoleDefinition_InUse_ReturnsConflict` + elle doğrulama 6 |
| Seviye değişikliği yayılıyor, ikinci başkan üretmiyor | `UpdateRoleDefinitionAsync_LevelChange_Cascades` + `…_WouldCreateSecondPresident` |
| Rol tanımı izin taşımıyor | `ClubRoleDefinitionShapeTests` |
| **V5'ten gelen tüm yetki testleri değişmeden yeşil** | `EventManagerTests`, `ClubMemberManagerTests`, `AdminClubScopeTests`, `ScopeGuardTests` + yukarıdaki `git diff` kontrolü |

- [ ] **Faz commit'i**

```bash
git log --oneline master..HEAD
```

Beş adım commit'i görünmeli.

---

## V6 Tamamlandı

Beş fazın hepsi bittiğinde `docs/MIMARI.md` v6.0'daki K-35…K-39, A-60…A-66 ve Y-69…Y-73 maddelerinin tamamı koda dönüşmüş olur. Belge ile kod arasında fark kalmamalı — kalıyorsa **önce belge güncellenir, sonra kod** (MIMARI.md §Belge sahipliği).
