# Faz 35 — Kulüp İçi Yetki Matrisi Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bir topluluk yeni bir unvan tanımlarken, o unvanın kulüp içinde **hangi işlemleri** yapabileceğini tek tek seçebilsin — üç basamaklı `Member/Officer/President` merdiveni yerine altı kutucuklu kapalı bir matris.

**Architecture:** `[Flags] enum ClubCapability` Core'da değil **Entities.Enums**'ta tanımlanır (ClubRole'ün yanında, aynı sınıf veri). Altı değer, `ClubRole`'ün bugün yetki kararı verdiği **tam** yer kümesidir — sayım koddan yapıldı. Kapasite `ClubRoleDefinition.Capabilities`'te (kaynak) ve `ClubMembership.Capabilities`'te (denormalize kopya) yaşar; yetki kontrolleri bugün olduğu gibi **tek satır** okur, ek sorgu yapmaz (A-61 korunuyor). `ClubRole` enum'ı silinmez — **makam** anlamıyla kalır ve yalnızca A-39 başkan tekilliği, dönem devri ve bildirim hedefi için kullanılır.

**Tech Stack:** .NET 8 · EF Core 8 / MSSQL · FluentValidation · xUnit + Moq + Mono.Cecil · React 18 + Vite + TypeScript + MUI + react-hook-form + zod

**Spec:** [docs/MIMARI.md](../../MIMARI.md) v6.2 — K-36, A-61 (tadil), A-68, Y-69 (tadil), Y-75

**Bağımlılık:** Faz 34 (ClubRoleDefinition şeması ve CRUD'u burada varsayılıyor).

---

## Bu fazın tehlikesi

Faz 34'ün sözü "yetki yüzeyine dokunma"ydı. **Bu fazın işi tam olarak yetki yüzeyine dokunmak.** Dolayısıyla güvenlik ağı sözde değil, testlerde:

| Ne | Nasıl korunuyor |
|---|---|
| Mevcut davranış hiç değişmemeli (unvansız üyeler) | Varsayılan eşleme `Officer → 61`, `President → 63`, `Member → 0`; **V5'ten gelen tüm yetki testleri değişmeden yeşil** kalmalı |
| Matris Identity'yi genişletmemeli | Y-75 mimari testi: kapasite okuyan her servis metodu `[SecuredOperation]` taşır |
| Başkan tekilliği bozulmamalı | `ClubRole` makam olarak yerinde; A-39 index'i ve `AcademicTermManager` devri hiç değişmez |
| Kapasite kümesi büyümemeli | Enum kodda sabit; arayüzden yeni kapasite tanımlanamaz (Y-69) |

---

## Global Constraints

Bu bölüm her task'ın gereksinimlerine **örtük olarak dahildir.** Değerler `docs/MIMARI.md` v6.2'den birebir alınmıştır.

- **Y-01** — Controller içinde `DbContext`, LINQ veya iş kuralı bulunamaz.
- **Y-03** — İş kuralı yalnızca Business'ta.
- **Y-06** — Enum değerleri sabittir; var olan bir değerin sayısı değiştirilemez.
- **Y-18** — Benzersizlik kuralı yalnızca uygulama kodunda tutulmaz.
- **Y-22** — İstemciden gelen yetki bilgisine güvenilmez. **Kapasite istemciden okunmaz, tanımdan okunur.**
- **Y-23** — Rol kontrolü kaynak sahipliği kontrolü değildir.
- **Y-27** — Uçtan uca async, `CancellationToken`, `.ConfigureAwait(false)`.
- **Y-29** — Kullanıcı mesajı yalnızca `Business/Constants/Messages`.
- **Y-31** — Nullable uyarısı susturulamaz. **Uyarılar zaten hata.**
- **Y-35** — İş kuralı arayüzde tekrar yazılmaz.
- **Y-37** — Identity'nin yanına paralel yetki tablosu açılamaz.
- **Y-64** — Sıralama olmadan `Skip`/`Take` yok.
- **Y-66** — Kulüp kapsamı kontrol eden her metot **ilk satırda** `clubs.manage.all`'a bakar.
- **Y-69 (v6.2'de tadil)** — Rol tanımı **string** izin kodu/claim taşıyamaz; yalnızca kapalı `ClubCapability` enum'ından seçer.
- **Y-75 (bu fazda doğuyor)** — Kulüp matrisi yalnızca **daraltır**. Uçtaki `[SecuredOperation]` birinci kapı olarak kalır.
- **A-39** — Başkan tekilliği: `(ClubId, AcademicTermId)` üzerinde `ClubRole = President` filtreli unique index.
- **A-61 (v6.2'de tadil)** — Yetki alanı `ClubMembership`'e denormalize edilir; kontroller ek DB okuması yapmaz.
- **A-68** — Altı kapasite, kapalı `[Flags]` enum, tek `int` alan.
- **O-27** — Tanımın kapasitesi değişince o unvanı taşıyan tüm üyeliklere **aynı transaction'da** yayılır.
- **Sessiz onaylar** — Adlar İngilizce, mesajlar/yorumlar Türkçe. Migration adı `YYYYMMDD_AçıklayıcıAd`, **her PR'da en fazla bir migration.**

---

## File Structure

| Dosya | Sorumluluk | Task |
|---|---|---|
| `src/Entities/Enums/ClubCapability.cs` | **Yeni.** Altı kapasitelik `[Flags]` enum + varsayılan eşleme | 1 |
| `src/Entities/ClubRoleDefinition.cs` | `Capabilities` alanı | 1 |
| `src/Entities/ClubMembership.cs` | `Capabilities` alanı (denormalize) | 1 |
| `src/DataAccess/Migrations/…_Faz35_KulupYetkiMatrisi.cs` | **Üretilecek** + iki tabloya geri doldurma | 1 |
| `tests/Architecture.Tests/CapabilityGuardTests.cs` | **Yeni.** Y-75 | 2 |
| `src/Business/Concrete/EventManager.cs` · `EventParticipationManager.cs` · `AnnouncementManager.cs` · `ClubMemberManager.cs` · `ReportScopeResolver.cs` | Altı kapının kapasiteye geçmesi | 3 |
| `src/Business/DTOs/Clubs/ClubRoleDefinitionDto.cs` · `Create…` · `Update…` | `Capabilities` alanı | 4 |
| `src/Business/ValidationRules/CreateClubRoleDefinitionRequestValidator.cs` · `Update…` | Kapasite doğrulaması | 4 |
| `src/Business/Concrete/ClubMemberManager.cs` | CRUD + atama + O-27 yayılımı | 4 |
| `src/Business/Constants/DefaultClubRoles.cs` | Varsayılan beşliye kapasite | 4 |
| `arayuz/src/api/types.ts` · `schemas/clubRoleDefinitionForm.ts` · `pages/ClubDetailPage.tsx` | Arayüz | 5 |

---

### Task 1: Şema — `ClubCapability` enum'ı ve iki tabloya geri doldurma

**Files:**
- Create: `src/Entities/Enums/ClubCapability.cs`
- Modify: `src/Entities/ClubRoleDefinition.cs`, `src/Entities/ClubMembership.cs`
- Create (üretilecek): `src/DataAccess/Migrations/<timestamp>_20260827_Faz35_KulupYetkiMatrisi.cs`
- Test: `tests/WebAPI.IntegrationTests/DomainConstraintTests.cs`

**Interfaces:**
- Consumes: — (ilk task)
- Produces:
  - `Entities.Enums.ClubCapability` — `[Flags]`: `None = 0`, `MembersView = 1`, `MembersManage = 2`, `EventsManage = 4`, `EventParticipantsView = 8`, `AnnouncementsManage = 16`, `ReportsView = 32`
  - `ClubCapabilityDefaults.ForRole(ClubRole)` → `ClubCapability`
  - `Entities.ClubRoleDefinition.Capabilities` — `ClubCapability`
  - `Entities.ClubMembership.Capabilities` — `ClubCapability`

- [ ] **Step 1: Failing test'i yaz**

`tests/WebAPI.IntegrationTests/DomainConstraintTests.cs` — kapanış süslü parantezinden önce:

```csharp
    [Fact(DisplayName = "A-68: migration mevcut üyelikleri ClubRole'e göre doğru kapasiteyle doldurur")]
    public async Task Migration_BackfillsCapabilitiesFromClubRole()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (club, student, term) = await SeedClubStudentTermAsync(db, "cap-backfill");

        // Migration ÇALIŞMIŞ durumda; bu satırlar migration'dan SONRA ekleniyor, yani
        // varsayılanı uygulama kodu değil, testin kendisi kuruyor. Geri doldurmanın
        // kanıtı aşağıdaki eşleme sabitlerinin doğruluğu.
        Assert.Equal(ClubCapability.None, ClubCapabilityDefaults.ForRole(ClubRole.Member));

        Assert.Equal(
            ClubCapability.MembersView | ClubCapability.EventsManage | ClubCapability.EventParticipantsView
                | ClubCapability.AnnouncementsManage | ClubCapability.ReportsView,
            ClubCapabilityDefaults.ForRole(ClubRole.Officer));

        // Başkan = Officer + üye yönetimi. Bugünkü EnsureRoleManagementAccessAsync yalnızca
        // President'i geçiriyor — eşleme bunu birebir korumalı.
        Assert.Equal(
            ClubCapabilityDefaults.ForRole(ClubRole.Officer) | ClubCapability.MembersManage,
            ClubCapabilityDefaults.ForRole(ClubRole.President));

        db.ClubMemberships.Add(new ClubMembership
        {
            ClubId = club.Id, StudentId = student.Id, AcademicTermId = term.Id,
            ClubRole = ClubRole.Officer,
            Capabilities = ClubCapabilityDefaults.ForRole(ClubRole.Officer),
            JoinedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var reloaded = await db.ClubMemberships.AsNoTracking()
            .SingleAsync(m => m.ClubId == club.Id && m.StudentId == student.Id && m.AcademicTermId == term.Id);

        // Kapasite tek int kolonda round-trip etmeli (bayrak kümesi bozulmadan).
        Assert.True(reloaded.Capabilities.HasFlag(ClubCapability.EventsManage));
        Assert.False(reloaded.Capabilities.HasFlag(ClubCapability.MembersManage));
    }
```

`using Entities.Enums;` zaten var; `ClubCapabilityDefaults` için `using Entities.Enums;` yeterli.

- [ ] **Step 2: Test'i çalıştır, derlenmediğini gör**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~Migration_BackfillsCapabilities"
```

Beklenen: **derleme hatası** — `ClubCapability` yok.

- [ ] **Step 3: Enum'ı ve varsayılan eşlemeyi oluştur**

`src/Entities/Enums/ClubCapability.cs`:

```csharp
namespace Entities.Enums;

/// <summary>
/// docs/MIMARI.md · K-36/A-68/Y-69: kulüp içi yetki matrisi. <b>Kapalı küme</b> — yeni kapasite
/// eklemek kod değişikliği ve migration ister; arayüzden tanımlanamaz. Bu altı değer,
/// `ClubRole`'ün bugüne kadar yetki kararı verdiği yerlerin TAMAMIDIR.
///
/// <b>Y-75:</b> bir kapasiteyi işaretlemek, kullanıcının Identity izninin vermediği bir şeyi
/// VERMEZ — uçtaki [SecuredOperation] birinci kapı olarak yerinde kalır, bu ikinci kapıdır.
///
/// <b>Y-06:</b> sayısal değerler sabittir — veritabanında int olarak saklanır.
/// </summary>
[Flags]
public enum ClubCapability
{
    None = 0,

    /// <summary>Üye listesini görebilir (ClubMemberManager.GetMembersPagedAsync).</summary>
    MembersView = 1,

    /// <summary>Üye rolü atayabilir, üye çıkarabilir, rol tanımı yönetebilir. Bugünkü President ayrıcalığı.</summary>
    MembersManage = 2,

    /// <summary>Etkinlik oluşturabilir, düzenleyebilir, silebilir (EventManager).</summary>
    EventsManage = 4,

    /// <summary>Etkinlik katılımcı listesini görebilir (EventParticipationManager).</summary>
    EventParticipantsView = 8,

    /// <summary>Duyuru yazabilir (AnnouncementManager).</summary>
    AnnouncementsManage = 16,

    /// <summary>Kulübün raporlarını alabilir (ReportScopeResolver).</summary>
    ReportsView = 32,
}

/// <summary>
/// docs/MIMARI.md · A-68: unvansız üyenin ve migration geri doldurmasının kapasitesi.
/// Bu eşleme <b>Faz 34 öncesi davranışı birebir korur</b> — değiştirmek yetki yüzeyini değiştirmektir.
/// </summary>
public static class ClubCapabilityDefaults
{
    /// <summary>Bugünkü "Officer/President geçer" kapılarının tamamı — üye yönetimi hariç.</summary>
    private const ClubCapability OfficerCapabilities =
        ClubCapability.MembersView
        | ClubCapability.EventsManage
        | ClubCapability.EventParticipantsView
        | ClubCapability.AnnouncementsManage
        | ClubCapability.ReportsView;

    public static ClubCapability ForRole(ClubRole role) => role switch
    {
        ClubRole.President => OfficerCapabilities | ClubCapability.MembersManage,
        ClubRole.Officer => OfficerCapabilities,
        _ => ClubCapability.None,
    };
}
```

- [ ] **Step 4: İki varlığa alanı ekle**

`src/Entities/ClubRoleDefinition.cs` — `ClubRole` özelliğinin altına:

```csharp
    /// <summary>
    /// docs/MIMARI.md · A-68: bu unvanın kulüp içinde yapabileceği işlemler. Yetkinin KAYNAĞI budur;
    /// atamada `ClubMembership.Capabilities`'e kopyalanır ve değişince O-27 ile yayılır.
    /// </summary>
    public ClubCapability Capabilities { get; set; }
```

`src/Entities/ClubMembership.cs` — `ClubRoleDefinitionId` özelliğinin altına:

```csharp
    /// <summary>
    /// docs/MIMARI.md · A-61/A-68: yetki matrisinin denormalize kopyası. <b>Yetki kararı BURADAN okunur</b> —
    /// bu sayede kontroller tek satır okur, tanım tablosuna ikinci bir sorgu gitmez.
    /// Unvansız üyede `ClubCapabilityDefaults.ForRole(ClubRole)` ile doldurulur.
    /// </summary>
    public ClubCapability Capabilities { get; set; }
```

`ClubRole` özelliğinin yorumunu da güncelle (anlamı daraldı):

```csharp
    /// <summary>
    /// docs/MIMARI.md · A-68: <b>makam.</b> A-39 başkan tekilliği, dönem devri ve bildirim hedefi
    /// bunu kullanır. <b>Yetki kararı vermez</b> — o `Capabilities`'ten okunur.
    /// </summary>
    public ClubRole ClubRole { get; set; }
```

- [ ] **Step 5: Migration üret ve geri doldurmayı ekle**

```bash
dotnet ef migrations add 20260827_Faz35_KulupYetkiMatrisi --project src/DataAccess --startup-project src/DataAccess
```

`Up()` içinde **iki** `AddColumn<int>` olmalı (`ClubRoleDefinitions.Capabilities`, `ClubMemberships.Capabilities`), başka bir şey olmamalı. Fazlası varsa **dur.**

`Up()` metodunun **sonuna** ekle:

```csharp
            // A-68: mevcut satırların kapasitesi ClubRole'den türetilir — Faz 34 öncesi davranış birebir korunur.
            // Sayılar ClubCapability ile birebir: MembersView=1, MembersManage=2, EventsManage=4,
            // EventParticipantsView=8, AnnouncementsManage=16, ReportsView=32.
            // Officer = 1+4+8+16+32 = 61, President = 61+2 = 63, Member = 0.
            // ClubRole: Member=0, Officer=1, President=2.
            migrationBuilder.Sql("""
                UPDATE [ClubMemberships] SET [Capabilities] = CASE [ClubRole]
                    WHEN 2 THEN 63
                    WHEN 1 THEN 61
                    ELSE 0 END;

                UPDATE [ClubRoleDefinitions] SET [Capabilities] = CASE [ClubRole]
                    WHEN 2 THEN 63
                    WHEN 1 THEN 61
                    ELSE 0 END;
                """);
```

> Bu adım atlanırsa **mevcut her üye kapasitesiz kalır** ve Task 3'ten sonra hiç kimse etkinlik oluşturamaz. Sessiz değil, gürültülü bir hata olur ama sebebi bulmak zaman alır.

- [ ] **Step 6: Test'i çalıştır, geçtiğini gör**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~Migration_BackfillsCapabilities"
dotnet build && dotnet test
```

Beklenen: yeni test yeşil, **tüm takım da yeşil** — henüz hiçbir kapı kapasiteye bakmıyor, davranış değişmedi.

---

### Task 2: Y-75 mimari testi — matris Identity'yi genişletemez

**Files:**
- Create: `tests/Architecture.Tests/CapabilityGuardTests.cs`
- Test: kendisi

**Interfaces:**
- Consumes: `ClubCapability` (Task 1)
- Produces: — (muhafız)

> Bu test Task 3'ten **önce** yazılır ve o an **boş küme üzerinde yeşildir** (henüz kapasite okuyan metot yok). Asıl işi Task 3'ten sonra başlar. `ScopeGuardTests.cs`'teki `EffectiveBodies` yardımcısını örnek al — async state machine ve closure gövdelerini yürüyen kısım orada zaten yazılı.

- [ ] **Step 1: Testi yaz**

`tests/Architecture.Tests/CapabilityGuardTests.cs` (yeni dosya):

```csharp
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// docs/MIMARI.md · Y-75: kulüp içi kapasite matrisi yalnızca DARALTIR, asla genişletmez.
/// Uçtaki [SecuredOperation] birinci kapıdır; kapasite ikincisi. Sıra bozulursa kulüp başkanı
/// kendi kulübüne, sistemde hiç var olmayan bir yetkiyi dağıtabilir hâle gelir (Y-37).
///
/// Test şunu doğrular: `ClubMembership.Capabilities` okuyan bir kod yoluna ulaşan HER servis
/// arayüzü metodu [SecuredOperation] taşır.
/// </summary>
public class CapabilityGuardTests
{
    private const string CapabilityGetter = "get_Capabilities";
    private const string MembershipType = "Entities.ClubMembership";
    private const string SecuredOperationAttribute = "SecuredOperationAttribute";

    [Fact(DisplayName = "Y-75: kapasite okuyan her servis metodu [SecuredOperation] taşır")]
    public void KapasiteOkuyanMetotlar_SecuredOperation_Tasir()
    {
        // ScopeGuardTests ile aynı desen: derlenmiş çıktıyı yoldan oku, tipe bağımlılık kurma.
        var path = Path.Combine(AppContext.BaseDirectory, "Business.dll");
        using var business = AssemblyDefinition.ReadAssembly(path);

        var concreteTypes = business.MainModule.Types
            .Where(t => t.Namespace == "Business.Concrete" && t.IsClass)
            .ToList();

        // 1. Kapasite okuyan somut metotları bul (async state machine ve closure'lar dâhil).
        var capabilityReaders = concreteTypes
            .SelectMany(t => t.Methods)
            .Where(ReadsCapabilities)
            .Select(m => $"{m.DeclaringType.Name}.{StripCompilerNoise(m.Name)}")
            .ToHashSet(StringComparer.Ordinal);

        // 2. Her arayüz metodunu, aynı adı taşıyan somut karşılığıyla eşleştir.
        var violations = new List<string>();

        foreach (var interfaceType in business.MainModule.Types.Where(t => t.Namespace == "Business.Abstract" && t.IsInterface))
        {
            var implementation = concreteTypes.FirstOrDefault(t => t.Interfaces.Any(i => i.InterfaceType.Name == interfaceType.Name));
            if (implementation is null)
            {
                continue;
            }

            foreach (var interfaceMethod in interfaceType.Methods)
            {
                var key = $"{implementation.Name}.{interfaceMethod.Name}";
                var reachesCapability = capabilityReaders.Contains(key)
                    || ReachesCapabilityThroughPrivateHelpers(implementation, interfaceMethod.Name);

                if (!reachesCapability)
                {
                    continue;
                }

                var secured = interfaceMethod.CustomAttributes
                    .Any(a => a.AttributeType.Name == SecuredOperationAttribute);

                if (!secured)
                {
                    violations.Add($"{interfaceType.Name}.{interfaceMethod.Name}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Y-75 ihlali — kulüp kapasitesi okuyan şu metotlar [SecuredOperation] taşımıyor, " +
            "yani yetki kararı yalnızca kulüp matrisine bırakılmış: " + string.Join(", ", violations) +
            ". Bkz. docs/MIMARI.md Y-75 / A-68 — matris bir FİLTRE, bir KAYNAK değil.");
    }

    /// <summary>Metot gövdesi (state machine ve closure'lar dâhil) ClubMembership.Capabilities okuyor mu?</summary>
    private static bool ReadsCapabilities(MethodDefinition method) =>
        EffectiveBodies(method).Any(body => body.Instructions.Any(i =>
            i.Operand is MethodReference reference
            && reference.Name == CapabilityGetter
            && reference.DeclaringType.FullName == MembershipType));

    /// <summary>
    /// Kapı metotları (EnsureXxxAccessAsync) private'tır; arayüz metodu onları çağırır.
    /// Bir seviye derinlik yeterli — bu kod tabanında kapı metotları doğrudan çağrılıyor.
    /// </summary>
    private static bool ReachesCapabilityThroughPrivateHelpers(TypeDefinition type, string entryMethodName)
    {
        var entries = type.Methods.Where(m => StripCompilerNoise(m.Name) == entryMethodName).ToList();
        if (entries.Count == 0)
        {
            return false;
        }

        var calledNames = entries
            .SelectMany(EffectiveBodies)
            .SelectMany(b => b.Instructions)
            .Select(i => i.Operand as MethodReference)
            .Where(r => r is not null && r.DeclaringType.FullName == type.FullName)
            .Select(r => StripCompilerNoise(r!.Name))
            .ToHashSet(StringComparer.Ordinal);

        return type.Methods
            .Where(m => calledNames.Contains(StripCompilerNoise(m.Name)))
            .Any(ReadsCapabilities);
    }

    /// <summary>Metodun kendi gövdesi + async state machine MoveNext + iç içe closure gövdeleri.</summary>
    private static IEnumerable<MethodBody> EffectiveBodies(MethodDefinition method)
    {
        if (method.HasBody)
        {
            yield return method.Body;
        }

        var stateMachines = method.DeclaringType.NestedTypes
            .Where(n => n.Name.Contains($"<{StripCompilerNoise(method.Name)}>", StringComparison.Ordinal));

        foreach (var nested in stateMachines)
        {
            foreach (var nestedMethod in nested.Methods.Where(m => m.HasBody))
            {
                yield return nestedMethod.Body;
            }
        }
    }

    /// <summary>`&lt;GetMembersPagedAsync&gt;d__12` gibi derleyici adlarından asıl adı çıkarır.</summary>
    private static string StripCompilerNoise(string name)
    {
        var start = name.IndexOf('<', StringComparison.Ordinal);
        if (start < 0)
        {
            return name;
        }

        var end = name.IndexOf('>', start, StringComparison.Ordinal);
        return end < 0 ? name : name[(start + 1)..end];
    }
}
```

- [ ] **Step 2: Testi çalıştır — şimdilik yeşil olmalı**

```bash
dotnet test tests/Architecture.Tests/Architecture.Tests.csproj --filter "FullyQualifiedName~CapabilityGuardTests"
```

Beklenen: **PASS** — henüz kapasite okuyan metot yok, ihlal kümesi boş.

- [ ] **Step 3: Testin gerçekten çalıştığını kanıtla**

`IClubMemberService.LeaveAsync` **kasıtlı olarak** `[SecuredOperation]` taşımıyor (öğrencinin kendi eylemi). Geçici olarak `ClubMemberManager.LeaveAsync` içine, `membership` bulunduktan sonra bir kapasite okuması ekle:

```csharp
        // GEÇİCİ — testin yakaladığını kanıtlamak için. Sonra SİL.
        if (membership.Capabilities.HasFlag(ClubCapability.MembersManage)) { }
```

Testi çalıştır: **FAIL** olmalı ve mesaj `IClubMemberService.LeaveAsync` demeli. Sonra bu iki satırı **sil** ve testin yeniden yeşile döndüğünü gör.

> Bu adımı atlama. Task 3'ten önce yeşil olan bir muhafız, çalıştığı kanıtlanmadıkça hiçbir şey kanıtlamaz.

- [ ] **Step 4: Tam takımı çalıştır**

```bash
dotnet build && dotnet test
```

---

### Task 3: Altı kapının kapasiteye geçmesi

**Files:**
- Modify: `src/Business/Concrete/EventManager.cs` (`EnsureClubWriteAccessAsync`)
- Modify: `src/Business/Concrete/EventParticipationManager.cs` (`EnsureViewAccessAsync`)
- Modify: `src/Business/Concrete/AnnouncementManager.cs` (`EnsureClubWriteAccessAsync`)
- Modify: `src/Business/Concrete/ClubMemberManager.cs` (`EnsureMemberViewAccessAsync`, `EnsureRoleManagementAccessAsync`)
- Modify: `src/Business/Concrete/ReportScopeResolver.cs` (`ResolveAsync`)
- Test: `tests/Business.Tests/ClubMemberManagerTests.cs`, `tests/Business.Tests/EventManagerTests.cs`

**Interfaces:**
- Consumes: `ClubCapability` (Task 1)
- Produces: — (davranış değişikliği; imzalar aynı)

**Değişimin şekli:** dört kapıda son satır birebir aynı biçimde:

```csharp
        return membership is not null && membership.ClubRole is ClubRole.Officer or ClubRole.President
```
→
```csharp
        return membership is not null && membership.Capabilities.HasFlag(ClubCapability.XXX)
```

- [ ] **Step 1: Failing test'leri yaz**

`tests/Business.Tests/ClubMemberManagerTests.cs` — kapanış süslü parantezinden önce:

```csharp
    [Fact(DisplayName = "A-68: kapasitesi MembersView olan üye listeyi görür — ClubRole hâlâ Member olsa bile")]
    public async Task GetMembersPagedAsync_MemberWithMembersViewCapability_ReturnsSuccess()
    {
        _currentUser.Setup(c => c.UserId).Returns(200);
        _academicStaffRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 999, Title = "Dr.", DepartmentId = 1 });
        _studentRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = 9, ApplicationUserId = 200, StudentNumber = "S1", DepartmentId = 1, EnrollmentYear = 2026 });

        // Makam Member, ama kapasite açık — matrisin merdivenden ayrıldığı yer burası.
        _clubMembershipRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubMembership
            {
                Id = 5, ClubId = 1, StudentId = 9, AcademicTermId = 1,
                ClubRole = ClubRole.Member,
                Capabilities = ClubCapability.MembersView,
                JoinedAtUtc = FixedNow,
            });
        _clubMembershipRepository
            .Setup(r => r.GetListPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ClubMembership>([], 0, 0, 20));

        var result = await _sut.GetMembersPagedAsync(1, 0, 20);

        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "A-68: kapasitesi kısılmış BAŞKAN üye listesini göremez — makam yetki vermez")]
    public async Task GetMembersPagedAsync_PresidentWithoutMembersView_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(200);
        _academicStaffRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicStaff { Id = 10, ApplicationUserId = 999, Title = "Dr.", DepartmentId = 1 });
        _studentRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = 9, ApplicationUserId = 200, StudentNumber = "S1", DepartmentId = 1, EnrollmentYear = 2026 });
        _clubMembershipRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubMembership
            {
                Id = 5, ClubId = 1, StudentId = 9, AcademicTermId = 1,
                ClubRole = ClubRole.President,
                Capabilities = ClubCapability.EventsManage,
                JoinedAtUtc = FixedNow,
            });

        var result = await _sut.GetMembersPagedAsync(1, 0, 20);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }
```

- [ ] **Step 2: Test'leri çalıştır, kırmızı olduğunu gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~MemberWithMembersViewCapability|FullyQualifiedName~PresidentWithoutMembersView"
```

Beklenen: birincisi **FAIL** (Member reddediliyor), ikincisi **FAIL** (President geçiyor).

- [ ] **Step 3: Dört kapıyı çevir**

`src/Business/Concrete/EventManager.cs` — `EnsureClubWriteAccessAsync` son `return`:

```csharp
        // A-68: karar artık makamdan değil KAPASİTEDEN. Y-75: uçtaki [SecuredOperation(events.write)]
        // birinci kapı olarak yerinde — bu ikinci kapı, yalnızca daraltır.
        return membership is not null && membership.Capabilities.HasFlag(ClubCapability.EventsManage)
            ? null
            : Messages.NotClubAdvisorOrOfficer;
```

`src/Business/Concrete/EventParticipationManager.cs` — `EnsureViewAccessAsync` son `return`:

```csharp
        return membership is not null && membership.Capabilities.HasFlag(ClubCapability.EventParticipantsView)
            ? null
            : Messages.NotClubAdvisorOrOfficer;
```

`src/Business/Concrete/AnnouncementManager.cs` — `EnsureClubWriteAccessAsync` son `return`:

```csharp
        return membership is not null && membership.Capabilities.HasFlag(ClubCapability.AnnouncementsManage)
            ? null
            : Messages.NotClubAdvisorOrOfficer;
```

`src/Business/Concrete/ClubMemberManager.cs` — `EnsureMemberViewAccessAsync` son `return`:

```csharp
        return membership is not null && membership.Capabilities.HasFlag(ClubCapability.MembersView)
            ? null
            : Messages.NotClubAdvisorOrOfficer;
```

`src/Business/Concrete/ClubMemberManager.cs` — `EnsureRoleManagementAccessAsync` son `return`:

```csharp
        return membership is not null && membership.Capabilities.HasFlag(ClubCapability.MembersManage)
            ? null
            : Messages.NotClubAdvisorOrPresident;
```

Her beş dosyaya `using Entities.Enums;` gerekiyorsa ekle (çoğunda zaten var).

- [ ] **Step 4: Rapor kapsamını çevir**

`src/Business/Concrete/ReportScopeResolver.cs` — `officerMemberships` sorgusu:

```csharp
            // A-68: EF Core enum bayrak testini SQL'e çevirir — (Capabilities & 32) = 32.
            // HasFlag burada KULLANILAMAZ: sorgu ifadesi içinde çevrilemez, bellekte değerlendirilirdi.
            var officerMemberships = await clubMembershipRepository
                .GetListAsync(
                    m => m.StudentId == student.Id
                        && m.AcademicTermId == term.Id
                        && (m.Capabilities & ClubCapability.ReportsView) == ClubCapability.ReportsView,
                    cancellationToken)
                .ConfigureAwait(false);
```

> **Tuzak:** `HasFlag` bir LINQ-to-Entities ifadesinde çevrilemez. Bit maskesi yazmazsan EF ya patlar ya da tüm tabloyu belleğe çeker. Diğer beş yer bellekte çalıştığı için `HasFlag` orada serbest.

- [ ] **Step 5: Bildirim işini kontrol et — burada DEĞİŞİKLİK YOK**

`src/Business/BackgroundJobs/EventDecisionNotificationJob.cs:39` `ClubRole.Officer/President` sorgusunu koruyor. **Bilinçli:** bu bir yetki kararı değil, "kimi haberdar edelim" sorusu — makam sorusudur (A-68). Dosyaya dokunma; satırın üstüne gerekçeyi yaz:

```csharp
            // A-68: bu bir YETKİ kararı değil, bildirim hedefi — makam sorusu olduğu için
            // ClubRole'de kaldı. Kapasiteye çevirmek "duyuru yetkisi olan herkese haber ver"
            // demek olurdu; kastedilen o değil.
```

- [ ] **Step 6: Test'leri çalıştır**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj
dotnet test tests/Architecture.Tests/Architecture.Tests.csproj
```

Beklenen: yeni 2 test yeşil; `CapabilityGuardTests` **hâlâ yeşil** (kapasite okuyan metotların hepsi `[SecuredOperation]`'lı).

Mevcut `ClubMemberManagerTests`/`EventManagerTests` testleri `Capabilities` kurmadığı için **kırılacak** — bu beklenen. Kırılan her testte `ClubMembership` başlatıcısına şunu ekle:

```csharp
            Capabilities = ClubCapabilityDefaults.ForRole(ClubRole.Officer),   // veya ilgili rol
```

> **Bunu toplu arama-değiştirmeyle yapma.** Her testte hangi rolün kastedildiğine bak; yanlış eşleme testi yeşile boyar ama kuralı ölçmez.

- [ ] **Step 7: Entegrasyon testlerini çalıştır**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj
```

Entegrasyon testleri üyelikleri `db.ClubMemberships.Add(...)` ile kuruyor; `Capabilities` verilmediği için `None` kalır ve **kapılar kapanır**. Kırılan her kurulum noktasına ekle:

```csharp
            Capabilities = ClubCapabilityDefaults.ForRole(ClubRole.Officer),
```

Ayrıca `ClubApplicationManager.DecideAsync`'te onayla oluşan başkan üyeliği ve `AcademicTermManager` dönem devri de kapasiteyi yazmalı — Task 4'te ele alınıyor, ama entegrasyon testleri burada kırılırsa Task 4'ün o iki adımını öne al.

- [ ] **Step 8: Tam takım**

```bash
dotnet build && dotnet test
```

---

### Task 4: CRUD, atama, O-27 yayılımı ve varsayılan set

**Files:**
- Modify: `src/Business/DTOs/Clubs/ClubRoleDefinitionDto.cs`, `CreateClubRoleDefinitionRequestDto.cs`, `UpdateClubRoleDefinitionRequestDto.cs`
- Modify: `src/Business/ValidationRules/CreateClubRoleDefinitionRequestValidator.cs`, `UpdateClubRoleDefinitionRequestValidator.cs`
- Modify: `src/Business/Concrete/ClubMemberManager.cs`
- Modify: `src/Business/Constants/DefaultClubRoles.cs`
- Modify: `src/Business/Concrete/ClubApplicationManager.cs`, `src/Business/Concrete/AcademicTermManager.cs`
- Test: `tests/Business.Tests/ClubMemberManagerTests.cs`

**Interfaces:**
- Consumes: `ClubCapability` (Task 1)
- Produces:
  - `ClubRoleDefinitionDto.Capabilities` — `ClubCapability`
  - `CreateClubRoleDefinitionRequestDto.Capabilities` / `UpdateClubRoleDefinitionRequestDto.Capabilities` — `ClubCapability`
  - `DefaultClubRoles.All` — dörtlü tuple: `(string Name, ClubRole Role, ClubCapability Capabilities, int DisplayOrder)`

- [ ] **Step 1: Failing test'leri yaz**

`tests/Business.Tests/ClubMemberManagerTests.cs`:

```csharp
    [Fact(DisplayName = "A-61/A-68: unvan atanınca kapasite TANIMDAN kopyalanır")]
    public async Task SetRoleAsync_WithDefinition_CopiesCapabilitiesFromDefinition()
    {
        var membership = new ClubMembership
        {
            Id = 5, ClubId = 1, StudentId = 9, AcademicTermId = 1,
            ClubRole = ClubRole.Member, Capabilities = ClubCapability.None, JoinedAtUtc = FixedNow,
        };
        _clubMembershipRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
        _clubRoleDefinitionRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubRoleDefinition, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubRoleDefinition
            {
                Id = 3, ClubId = 1, Name = "Sayman", ClubRole = ClubRole.Member,
                Capabilities = ClubCapability.EventsManage | ClubCapability.ReportsView, DisplayOrder = 3,
            });

        GrantAdminScope();

        var result = await _sut.SetRoleAsync(1, 5, new SetClubRoleRequestDto { ClubRoleDefinitionId = 3 });

        Assert.True(result.IsSuccess);
        Assert.Equal(ClubCapability.EventsManage | ClubCapability.ReportsView, membership.Capabilities);
    }

    [Fact(DisplayName = "A-68: unvansız atamada kapasite ClubRole'den türetilir — eski davranış")]
    public async Task SetRoleAsync_WithoutDefinition_DerivesCapabilitiesFromRole()
    {
        var membership = new ClubMembership
        {
            Id = 5, ClubId = 1, StudentId = 9, AcademicTermId = 1,
            ClubRole = ClubRole.Member, Capabilities = ClubCapability.None, JoinedAtUtc = FixedNow,
        };
        _clubMembershipRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);

        GrantAdminScope();

        var result = await _sut.SetRoleAsync(1, 5, new SetClubRoleRequestDto { ClubRole = ClubRole.Officer });

        Assert.True(result.IsSuccess);
        Assert.Equal(ClubCapabilityDefaults.ForRole(ClubRole.Officer), membership.Capabilities);
        Assert.Null(membership.ClubRoleDefinitionId);
    }

    [Fact(DisplayName = "O-27: tanımın KAPASİTESİ değişince taşıyan tüm üyeliklere yayılır")]
    public async Task UpdateRoleDefinitionAsync_CapabilityChange_CascadesToMemberships()
    {
        var definition = new ClubRoleDefinition
        {
            Id = 3, ClubId = 1, Name = "Sayman", ClubRole = ClubRole.Member,
            Capabilities = ClubCapability.MembersView, DisplayOrder = 3,
        };
        var holders = new List<ClubMembership>
        {
            new() { Id = 11, ClubId = 1, StudentId = 21, AcademicTermId = 1, ClubRole = ClubRole.Member, Capabilities = ClubCapability.MembersView, ClubRoleDefinitionId = 3, JoinedAtUtc = FixedNow },
            new() { Id = 12, ClubId = 1, StudentId = 22, AcademicTermId = 1, ClubRole = ClubRole.Member, Capabilities = ClubCapability.MembersView, ClubRoleDefinitionId = 3, JoinedAtUtc = FixedNow },
        };

        _clubRoleDefinitionRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubRoleDefinition, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ClubRoleDefinition, bool>> filter, CancellationToken _) =>
                new[] { definition }.AsQueryable().Where(filter).FirstOrDefault());
        _clubMembershipRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(holders);

        GrantAdminScope();

        var result = await _sut.UpdateRoleDefinitionAsync(1, 3, new UpdateClubRoleDefinitionRequestDto
        {
            Name = "Sayman",
            ClubRole = ClubRole.Member,
            Capabilities = ClubCapability.MembersView | ClubCapability.EventsManage,
            DisplayOrder = 3,
        });

        Assert.True(result.IsSuccess);
        Assert.All(holders, m => Assert.True(m.Capabilities.HasFlag(ClubCapability.EventsManage)));
    }
```

- [ ] **Step 2: Test'leri çalıştır, kırmızı olduğunu gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~CopiesCapabilitiesFromDefinition|FullyQualifiedName~DerivesCapabilitiesFromRole|FullyQualifiedName~CapabilityChange_Cascades"
```

Beklenen: **derleme hatası** — DTO'larda `Capabilities` yok.

- [ ] **Step 3: DTO'lara alanı ekle**

`src/Business/DTOs/Clubs/ClubRoleDefinitionDto.cs` — `ClubRole` altına:

```csharp
    /// <summary>docs/MIMARI.md · A-68: bu unvanın kulüp içinde yapabilecekleri.</summary>
    public ClubCapability Capabilities { get; set; }
```

`CreateClubRoleDefinitionRequestDto.cs` ve `UpdateClubRoleDefinitionRequestDto.cs` — ikisine de **aynı alan**:

```csharp
    /// <summary>docs/PLAN-V6.md · O-27: değişirse bu unvanı taşıyan TÜM üyeliklere aynı transaction'da yayılır.</summary>
    public ClubCapability Capabilities { get; set; }
```

Üç dosyaya da `using Entities.Enums;` (zaten var).

- [ ] **Step 4: Validator'lara kapasite kuralını ekle**

`CreateClubRoleDefinitionRequestValidator.cs` ve `UpdateClubRoleDefinitionRequestValidator.cs` — **ikisine de** aynı kural:

```csharp
        // Y-69: yalnızca tanımlı bayrakların birleşimi kabul edilir — istemci uydurma bir
        // sayı gönderip kapalı kümenin dışına çıkamaz. IsInEnum() [Flags] için YETMEZ:
        // birleşik değerler (örn. 5 = MembersView|EventsManage) enum'da tanımlı değildir.
        RuleFor(x => x.Capabilities)
            .Must(value => (value & ~AllClubCapabilities) == ClubCapability.None)
            .WithMessage("Tanımsız yetki değeri gönderildi.");
```

Her iki validator sınıfının başına aynı sabiti koy:

```csharp
    private const ClubCapability AllClubCapabilities =
        ClubCapability.MembersView
        | ClubCapability.MembersManage
        | ClubCapability.EventsManage
        | ClubCapability.EventParticipantsView
        | ClubCapability.AnnouncementsManage
        | ClubCapability.ReportsView;
```

`using Entities.Enums;` ekle.

- [ ] **Step 5: Manager'ı güncelle**

`src/Business/Concrete/ClubMemberManager.cs`:

**(a)** `CreateRoleDefinitionAsync` — `new ClubRoleDefinition { … }` başlatıcısına ekle:

```csharp
            Capabilities = request.Capabilities,
```

**(b)** `UpdateRoleDefinitionAsync` — `levelChanged` satırının **altına** ekle ve yayılımı genişlet:

```csharp
        var capabilitiesChanged = definition.Capabilities != request.Capabilities;

        // O-27: makam VEYA kapasite değiştiyse taşıyıcıları topla — ikisi de üyeliğe yansır.
        List<ClubMembership> holders = [];
        if (levelChanged || capabilitiesChanged)
        {
            holders = await clubMembershipRepository
                .GetListAsync(m => m.ClubRoleDefinitionId == definitionId, cancellationToken)
                .ConfigureAwait(false);

            // A-39: bir kulüpte tek başkan makamı.
            if (levelChanged && request.ClubRole == ClubRole.President && holders.Count > 1)
            {
                return Result.Conflict(Messages.ClubRoleDefinitionWouldCreateSecondPresident);
            }
        }
```

`definition.ClubRole = request.ClubRole;` satırının altına:

```csharp
        definition.Capabilities = request.Capabilities;
```

`foreach (var membership in holders)` gövdesini şununla değiştir:

```csharp
        foreach (var membership in holders)
        {
            membership.ClubRole = request.ClubRole;
            membership.Capabilities = request.Capabilities;
            clubMembershipRepository.Update(membership);
        }
```

**(c)** `SetRoleAsync` — unvan çözümleme bloğuna kapasiteyi ekle:

```csharp
        var targetRole = request.ClubRole;
        int? targetDefinitionId = null;
        // A-68: unvansız atamada kapasite makamdan türetilir — Faz 34 öncesi davranış.
        var targetCapabilities = ClubCapabilityDefaults.ForRole(request.ClubRole);

        if (request.ClubRoleDefinitionId is { } definitionId)
        {
            var definition = await clubRoleDefinitionRepository
                .GetAsync(d => d.Id == definitionId && d.ClubId == clubId, cancellationToken)
                .ConfigureAwait(false);
            if (definition is null)
            {
                return Result.NotFound(Messages.ClubRoleDefinitionNotFound);
            }

            targetRole = definition.ClubRole;
            targetDefinitionId = definition.Id;
            targetCapabilities = definition.Capabilities;
        }
```

Atama satırlarına ekle:

```csharp
        membership.ClubRole = targetRole;
        membership.ClubRoleDefinitionId = targetDefinitionId;
        membership.Capabilities = targetCapabilities;
        clubMembershipRepository.Update(membership);
```

**(d)** `ToRoleDefinitionDto` — `Capabilities = d.Capabilities,` ekle.

`using Entities.Enums;` (zaten var).

- [ ] **Step 6: Varsayılan seti güncelle**

`src/Business/Constants/DefaultClubRoles.cs`:

```csharp
using Entities.Enums;

namespace Business.Constants;

/// <summary>
/// docs/PLAN-V6.md · O-20: yeni kulüp doğduğunda kopyalanan varsayılan unvan seti.
/// A-68: her unvanın kapasitesi makamının varsayılanıyla başlar — kulüp sonradan daraltabilir/genişletebilir.
/// </summary>
public static class DefaultClubRoles
{
    public static readonly IReadOnlyList<(string Name, ClubRole Role, ClubCapability Capabilities, int DisplayOrder)> All =
    [
        ("Başkan", ClubRole.President, ClubCapabilityDefaults.ForRole(ClubRole.President), 1),
        ("Başkan Yardımcısı", ClubRole.Officer, ClubCapabilityDefaults.ForRole(ClubRole.Officer), 2),
        ("Sayman", ClubRole.Officer, ClubCapabilityDefaults.ForRole(ClubRole.Officer), 3),
        ("Sekreter", ClubRole.Officer, ClubCapabilityDefaults.ForRole(ClubRole.Officer), 4),
        ("Üye", ClubRole.Member, ClubCapabilityDefaults.ForRole(ClubRole.Member), 5),
    ];
}
```

İki çağrı yerini dörtlü tuple'a uyarla — `src/Business/Concrete/ClubManager.cs` ve `src/Business/Concrete/ClubApplicationManager.cs`:

```csharp
        foreach (var (roleName, role, capabilities, displayOrder) in DefaultClubRoles.All)
        {
            await clubRoleDefinitionRepository.AddAsync(
                new ClubRoleDefinition
                {
                    ClubId = club.Id, Name = roleName, ClubRole = role,
                    Capabilities = capabilities, DisplayOrder = displayOrder,
                },
                cancellationToken).ConfigureAwait(false);
        }
```

- [ ] **Step 7: Üyelik oluşturan iki yeri düzelt**

`src/Business/Concrete/ClubApplicationManager.cs` — `DecideAsync` içindeki başkan üyeliği:

```csharp
                    var membership = new ClubMembership
                    {
                        ClubId = club.Id,
                        StudentId = application.StudentId,
                        AcademicTermId = application.AcademicTermId,
                        ClubRole = ClubRole.President,
                        // A-68: kapasite makamla birlikte yazılır, yoksa yeni başkan hiçbir şey yapamaz.
                        Capabilities = ClubCapabilityDefaults.ForRole(ClubRole.President),
                        JoinedAtUtc = now,
                    };
```

`src/Business/Concrete/AcademicTermManager.cs` — dönem devrinde yeni üyelik oluşturan satır. `role` değişkeni orada zaten hesaplanıyor (çift başkan `Officer`'a düşürülüyor); yeni üyeliğe ekle:

```csharp
                    Capabilities = ClubCapabilityDefaults.ForRole(role),
```

> **Tuzak:** devirde `role` değişebiliyor (ikinci başkan Officer'a düşüyor). Kapasiteyi `membership.Capabilities`'ten **kopyalama** — düşürülen başkan üye yönetimi yetkisini taşımaya devam ederdi. `role`'den türet.

`using Entities.Enums;` iki dosyada da var.

- [ ] **Step 8: Test'leri çalıştır ve tam takım**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj
dotnet build && dotnet test
```

---

### Task 5: Arayüz — kapasite kutucukları

**Files:**
- Modify: `arayuz/src/api/types.ts`
- Modify: `arayuz/src/schemas/clubRoleDefinitionForm.ts`
- Modify: `arayuz/src/pages/ClubDetailPage.tsx`

**Interfaces:**
- Consumes: `/api/clubs/{clubId}/role-definitions` (Faz 34), `Capabilities` alanı (Task 4)
- Produces: `arayuz/src/api/types.ts` → `ClubCapability`, `CLUB_CAPABILITIES`

> **Taşıma biçimi:** `[Flags]` enum JSON'a **sayı** olarak gider (`JsonStringEnumConverter` bileşik değerleri metne çeviremez). Arayüz bit maskesiyle çalışır; `ClubRole` metin olarak gelmeye devam eder.

- [ ] **Step 1: TS tiplerini ekle**

`arayuz/src/api/types.ts` — `ClubRoleDefinitionDto` arayüzünün üstüne:

```typescript
// src/Entities/Enums/ClubCapability.cs — [Flags] enum SAYI olarak taşınır (metin değil).
export const ClubCapability = {
  None: 0,
  MembersView: 1,
  MembersManage: 2,
  EventsManage: 4,
  EventParticipantsView: 8,
  AnnouncementsManage: 16,
  ReportsView: 32,
} as const

/** Kutucuk listesi — sıra ekranda göründüğü sıradır. */
export const CLUB_CAPABILITIES: { value: number; label: string; description: string }[] = [
  { value: ClubCapability.EventsManage, label: 'Etkinlik yönetimi', description: 'Etkinlik oluşturabilir, düzenleyebilir ve silebilir.' },
  { value: ClubCapability.EventParticipantsView, label: 'Katılımcı listesi', description: 'Etkinliklere kimlerin kaydolduğunu görebilir.' },
  { value: ClubCapability.AnnouncementsManage, label: 'Duyuru yönetimi', description: 'Topluluk duyurusu yazabilir.' },
  { value: ClubCapability.MembersView, label: 'Üye listesi', description: 'Topluluk üyelerini görebilir.' },
  { value: ClubCapability.MembersManage, label: 'Üye ve rol yönetimi', description: 'Üye çıkarabilir, rol atayabilir, unvan tanımlayabilir.' },
  { value: ClubCapability.ReportsView, label: 'Raporlar', description: 'Topluluğun raporlarını alabilir.' },
]
```

`ClubRoleDefinitionDto` arayüzüne ekle:

```typescript
  capabilities: number
```

- [ ] **Step 2: Form şemasını güncelle**

`arayuz/src/schemas/clubRoleDefinitionForm.ts`:

```typescript
import { z } from 'zod'

// Y-35: yalnızca biçim. "Bu unvan alınmış mı", "ikinci başkan üretir mi" kararları API'nin.
export const clubRoleDefinitionFormSchema = z.object({
  name: z.string().min(1, 'Unvan adı gerekli.').max(100),
  clubRole: z.enum(['Member', 'Officer', 'President']),
  // A-68: bit maskesi. Tanımsız bit gönderilirse API 400 döner (Y-69) — burada sınır kontrolü yok.
  capabilities: z.number().int().min(0),
  displayOrder: z.number().int().min(0),
})

export type ClubRoleDefinitionFormValues = z.infer<typeof clubRoleDefinitionFormSchema>

export const emptyClubRoleDefinitionFormValues: ClubRoleDefinitionFormValues = {
  name: '',
  clubRole: 'Member',
  capabilities: 0,
  displayOrder: 0,
}
```

- [ ] **Step 3: Forma kutucukları ekle**

`arayuz/src/pages/ClubDetailPage.tsx` — `ClubRoleDefinitionFormFields` içinde, "Yetki seviyesi" seçicisinin **altına**. Mevcut `clubRole` seçicisinin etiketini **"Makam"** olarak değiştir ve yardımcı metnini güncelle:

```tsx
      <Controller
        name="clubRole"
        control={control}
        render={({ field }) => (
          <TextField
            {...field}
            select
            fullWidth
            margin="dense"
            label="Makam"
            helperText="Yalnızca başkan tekilliği ve dönem devri için kullanılır; yetki aşağıdan seçilir."
          >
            {CLUB_ROLES.map((role) => (
              <MenuItem key={role} value={role}>
                {clubRoleLabel(role)}
              </MenuItem>
            ))}
          </TextField>
        )}
      />

      <Controller
        name="capabilities"
        control={control}
        render={({ field }) => (
          <FormControl component="fieldset" sx={{ mt: 2, display: 'block' }}>
            <FormLabel component="legend">Bu unvan neler yapabilir?</FormLabel>
            <FormGroup>
              {CLUB_CAPABILITIES.map((capability) => (
                <FormControlLabel
                  key={capability.value}
                  control={
                    <Checkbox
                      checked={(field.value & capability.value) === capability.value}
                      onChange={(event) =>
                        field.onChange(
                          event.target.checked
                            ? field.value | capability.value
                            : field.value & ~capability.value,
                        )
                      }
                    />
                  }
                  label={
                    <Box>
                      <Typography variant="body2">{capability.label}</Typography>
                      <Typography variant="caption" color="text.secondary">
                        {capability.description}
                      </Typography>
                    </Box>
                  }
                />
              ))}
            </FormGroup>
            {/* Y-75: kullanıcıya sınırı söyle — kutucuk, kişinin sistemdeki rolünden fazlasını vermez. */}
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
              Bu yetkiler yalnızca bu toplulukta geçerlidir ve kişinin sistemdeki rolünün izin
              verdiğinden fazlasını veremez.
            </Typography>
          </FormControl>
        )}
      />
```

`@mui/material` import bloğuna ekle: `Checkbox`, `FormControl`, `FormControlLabel`, `FormGroup`, `FormLabel`.
`api/types` import'una ekle: `CLUB_CAPABILITIES`.

Eski "Bu unvanı alan üye, seçtiğiniz yetki seviyesinin tüm haklarını kazanır." metnini **sil** — artık yanlış (seviye değil, seçilen kutucuklar geçerli).

- [ ] **Step 4: Düzenleme formunun kapasiteyi doldurduğundan emin ol**

`RoleDefinitionsTab` içindeki `editForm.reset({...})` çağrısına ekle:

```tsx
                      capabilities: params.row.capabilities,
```

- [ ] **Step 5: Roller tablosuna kapasite sütunu ekle**

`columns` dizisinde `clubRole` sütununun **altına**:

```tsx
    {
      field: 'capabilities',
      headerName: 'Yetkiler',
      flex: 1,
      minWidth: 240,
      sortable: false,
      renderCell: (params) => {
        const granted = CLUB_CAPABILITIES.filter((c) => (params.row.capabilities & c.value) === c.value)
        return granted.length === 0 ? (
          <Typography variant="caption" color="text.secondary">
            Yetki yok
          </Typography>
        ) : (
          <Stack direction="row" spacing={0.5} sx={{ flexWrap: 'wrap', gap: 0.5 }}>
            {granted.map((c) => (
              <Chip key={c.value} size="small" variant="outlined" label={c.label} />
            ))}
          </Stack>
        )
      },
    },
```

`DataTable`'a `getRowHeight={() => 'auto'}` ekle — rozetler sığmazsa satır kırpılır.

Mevcut `clubRole` sütununun başlığını **"Makam"** yap.

- [ ] **Step 6: Build ve lint**

```bash
cd arayuz && npm run build && npm run lint
```

Beklenen: `tsc` temiz, `oxlint` yalnızca `AuthContext.tsx` ve `NotifierProvider.tsx`'teki iki mevcut uyarıyı verir.

- [ ] **Step 7: Elle doğrulama — bu fazın asıl kanıtı**

1. Başkan/danışman → topluluk detayı → **Roller** → "Sayman" unvanını düzenle.
2. Kutucuklardan **yalnızca "Duyuru yönetimi"**ni işaretle, kaydet.
3. Bir üyeye "Sayman" unvanını ata.
4. O üyeyle giriş yap → **duyuru yazabilmeli**, **etkinlik oluşturamamalı**, **üye listesini görememeli**.
5. Unvana "Etkinlik yönetimi"ni de ekle → aynı üye (yeniden giriş gerekmez, kapasite üyelikte) artık etkinlik oluşturabilmeli.
6. Makamı "Başkan" olan varsayılan unvanı iki üyeye atamayı dene → **409**.
7. Unvansız bir üyeyi "Yönetici" makamına al → Faz 34'teki gibi davranmalı (etkinlik + duyuru + üye listesi, üye yönetimi yok).
8. Başka bir kulübün detayında bu unvan **görünmemeli**.

---

## Faz Kapanışı

- [ ] **Tam doğrulama**

```bash
dotnet build
dotnet test
cd arayuz && npm run build && npm run lint && cd ..
```

- [ ] **Y-75 muhafızını özellikle çalıştır**

```bash
dotnet test tests/Architecture.Tests/Architecture.Tests.csproj
```

`CapabilityGuardTests` ve `ClubRoleDefinitionShapeTests` (Faz 34) yeşil olmalı.

- [ ] **"Bitti sayılır" kontrolü** (`docs/MIMARI.md` §5 Faz 35)

| Koşul | Nasıl doğrulanır |
|---|---|
| Altı kapasite tek tek seçilebiliyor | Elle doğrulama 1-2 |
| Kapasitesi kısılan üye o işlemi yapamıyor | `GetMembersPagedAsync_PresidentWithoutMembersView_ReturnsForbidden` + elle doğrulama 4 |
| Kapasitesi açılan yapabiliyor | `GetMembersPagedAsync_MemberWithMembersViewCapability_ReturnsSuccess` + elle doğrulama 5 |
| Matris Identity'nin vermediğini veremiyor | `CapabilityGuardTests` (Task 2 Step 3'te kırıldığı kanıtlandı) |
| Unvansız üyeler Faz 34'teki gibi | `SetRoleAsync_WithoutDefinition_DerivesCapabilitiesFromRole` + elle doğrulama 7 |
| A-39 ve dönem devri değişmedi | `UpdateRoleDefinitionAsync_WouldCreateSecondPresident_ReturnsConflict` + `AcademicTermManagerTests` yeşil |
| O-27 kapasiteyi de yayıyor | `UpdateRoleDefinitionAsync_CapabilityChange_CascadesToMemberships` |

- [ ] **Faz commit'i**

Tek commit, faz sonunda:

```bash
git add -A
git commit
```

---

## Sonraki Faz

Yok — V6 bu fazla kapanıyor. `docs/MIMARI.md` v6.2'deki K-36, A-61 (tadil), A-68, Y-69 (tadil) ve Y-75 maddelerinin tamamı koda dönüşmüş olmalı.
