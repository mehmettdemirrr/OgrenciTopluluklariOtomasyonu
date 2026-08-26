# Faz 31 — Topluluk Kurma Başvuru Takvimi Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Topluluk kurma başvuruları yönetici tarafından belirlenen tarihler arasında açık olsun; yönetici bunun dışında başvuruları elle açıp kapatabilsin ve kapalıyken öğrenci sebebi ve takvimi görebilsin.

**Architecture:** Pencere `AcademicTerm`'in üç yeni alanı olarak yaşar (yeni varlık, yeni izin, yeni ekran yok). Açık/kapalı kararı **tek bir metotta** hesaplanır; hem `SubmitAsync` muhafızı hem durum okuma ucu o metodu çağırır — iki ayrı `if`, ekranın "açık" derken API'nin "kapalı" demesinin garantili yoludur (Y-73). Karar üç durumlu: takvime uy / zorla açık / zorla kapalı. Takvim tanımlı değilse **kapalı** (fail-closed); dağıtımda kesinti olmasın diye migration mevcut güncel dönemi "zorla açık" işaretler.

**Tech Stack:** .NET 8 · EF Core 8 / MSSQL · FluentValidation · xUnit + Moq · React 18 + Vite + TypeScript + MUI + react-hook-form + zod

**Spec:** [docs/PLAN-V6.md](../../PLAN-V6.md) §Faz 31 · [docs/MIMARI.md](../../MIMARI.md) K-39, A-66, Y-73

**Bağımlılık:** Yok. Faz 30 uygulanmamış olsa da bu faz tek başına çalışır.

---

## Global Constraints

Bu bölüm her task'ın gereksinimlerine **örtük olarak dahildir.** Değerler `docs/MIMARI.md`'den birebir alınmıştır.

- **Y-01** — Controller içinde `DbContext`, `IXxxDal`, LINQ sorgusu veya iş kuralı bulunamaz.
- **Y-03** — İş kuralı yalnızca Business'ta yaşar: controller'da `if`, entity içinde metot, frontend'de karar yok.
- **Y-09** — Entity sınıfı HTTP request/response gövdesinde yer alamaz; giriş/çıkış DTO.
- **Y-17** — `DateTime.Now` yasak; UTC ve enjekte edilen `IClock`.
- **Y-21** — Uçlar varsayılan olarak anonim olamaz; global fallback policy geçerli.
- **Y-25** — İstemciye nötr mesaj; exception/SQL detayı sızmaz.
- **Y-27** — Uçtan uca async, `CancellationToken` taşınır, `.ConfigureAwait(false)` kullanılır. `.Result`/`.Wait()`/`async void` yasak.
- **Y-29** — Kullanıcı mesajı koda gömülmez/tekrarlanmaz; yalnızca `Business/Constants/Messages`.
- **Y-31** — Nullable uyarısı `!` veya `#pragma` ile susturulamaz. **Uyarılar zaten hata.**
- **Y-35** — İş kuralı arayüzde tekrar yazılmaz. **Butonu gizlemek yetki değildir** — API muhafızı her hâlükârda çalışır.
- **Y-73 (bu fazda doğuyor)** — Pencere kontrolü yalnızca arayüzde yapılamaz; kapalı mesajı sebebi ve takvimin nerede görüleceğini söyler; pencere kararı **iki ayrı yerde hesaplanamaz.**
- **A-42** — Anonim yüzey dar kalır: `/api/public/*` yalnızca tek önek, tek controller. **Pencere okuma ucu oraya eklenmez.**
- **A-66** — Fail-closed: `FollowSchedule` + tarih tanımsız = **kapalı.**
- **Sessiz onaylar** — Sınıf/metot/tablo adları İngilizce, kullanıcı mesajları ve yorumlar **Türkçe**. Enum'lar DB'de `int`, API'de metin. Tarih/saat UTC + ISO-8601. Migration adı `YYYYMMDD_AçıklayıcıAd`, **her PR'da en fazla bir migration.**

---

## Karar tablosu (uygulanacak kural)

Bu tablo Task 2'nin **tek doğruluk kaynağıdır.** Beş satırın her biri bir teste karşılık gelir.

| Override | Tarihler | `now` | Sonuç |
|---|---|---|---|
| `ForceOpen` | (bakılmaz) | (bakılmaz) | **Açık** |
| `ForceClosed` | (bakılmaz) | (bakılmaz) | **Kapalı** |
| `FollowSchedule` | ikisi de tanımlı | aralıkta | **Açık** |
| `FollowSchedule` | ikisi de tanımlı | aralık dışında | **Kapalı** |
| `FollowSchedule` | biri veya ikisi tanımsız | (bakılmaz) | **Kapalı** (fail-closed) |

Aralık **kapsayıcıdır**: `now >= StartUtc && now <= EndUtc`.

---

## File Structure

| Dosya | Sorumluluk | Task |
|---|---|---|
| `src/Entities/Enums/ClubApplicationWindowOverride.cs` | **Yeni.** Üç durumlu geçersiz kılma | 1 |
| `src/Entities/AcademicTerm.cs` | Üç pencere alanı | 1 |
| `src/DataAccess/Migrations/…_Faz31_BasvuruTakvimi.cs` | **Üretilecek.** Üç kolon + mevcut güncel dönemi `ForceOpen` yapan veri adımı | 1 |
| `src/Business/DTOs/ClubApplications/ClubApplicationWindowDto.cs` | **Yeni.** Pencere durumu (çıkış) | 2 |
| `src/Business/Concrete/ClubApplicationManager.cs` | `EvaluateWindow` (özel statik) + `GetWindowAsync` + `SubmitAsync` muhafızı | 2, 3 |
| `src/Business/Abstract/IClubApplicationService.cs` | `GetWindowAsync` sözleşmesi | 2 |
| `src/Business/Constants/Messages.cs` | `ClubApplicationsClosed` | 3 |
| `src/WebAPI/Controllers/ClubApplicationsController.cs` | `GET /club-applications/window` | 3 |
| `src/Business/DTOs/Reference/SetClubApplicationWindowRequestDto.cs` | **Yeni.** Yönetici girişi | 4 |
| `src/Business/ValidationRules/SetClubApplicationWindowRequestValidator.cs` | **Yeni.** Biçim kuralı | 4 |
| `src/Business/DTOs/Reference/AcademicTermListItemDto.cs` | Pencere alanları (çıkış) | 4 |
| `src/Business/Abstract/IAcademicTermService.cs` | `SetClubApplicationWindowAsync` sözleşmesi | 4 |
| `src/Business/Concrete/AcademicTermManager.cs` | `SetClubApplicationWindowAsync` + liste eşlemesi | 4 |
| `src/WebAPI/Controllers/AcademicTermsController.cs` | `PUT /{id}/club-application-window` | 4 |
| `arayuz/src/api/types.ts` | İki yeni tip + alanlar | 5 |
| `arayuz/src/schemas/clubApplicationWindowForm.ts` | **Yeni.** Yönetici formu | 5 |
| `arayuz/src/pages/ReferenceDataPage.tsx` | Dönem sekmesine pencere kolonu + diyalog | 5 |
| `arayuz/src/pages/ClubsPage.tsx` | Kapalıysa düğme pasif + sebep | 5 |
| `arayuz/src/pages/MyApplicationsPage.tsx` | Takvim durumu her zaman görünür | 5 |

**Test dosyaları:**

| Dosya | Task |
|---|---|
| `tests/Business.Tests/ClubApplicationManagerTests.cs` | **Yeni.** 2 |
| `tests/WebAPI.IntegrationTests/ClubApplicationFlowTests.cs` | 3 |
| `tests/Business.Tests/ValidationRulesTests.cs` | 4 |

---

### Task 1: Şema — pencere alanları ve migration

**Files:**
- Create: `src/Entities/Enums/ClubApplicationWindowOverride.cs`
- Modify: `src/Entities/AcademicTerm.cs`
- Create (üretilecek): `src/DataAccess/Migrations/<timestamp>_20260826_Faz31_BasvuruTakvimi.cs`

**Interfaces:**
- Consumes: — (ilk task)
- Produces:
  - `Entities.Enums.ClubApplicationWindowOverride` — `FollowSchedule = 0`, `ForceOpen = 1`, `ForceClosed = 2`
  - `Entities.AcademicTerm.ClubApplicationStartUtc` — `DateTime?`
  - `Entities.AcademicTerm.ClubApplicationEndUtc` — `DateTime?`
  - `Entities.AcademicTerm.ClubApplicationOverride` — `ClubApplicationWindowOverride`

- [ ] **Step 1: Enum'ı oluştur**

`src/Entities/Enums/ClubApplicationWindowOverride.cs`:

```csharp
namespace Entities.Enums;

/// <summary>
/// docs/MIMARI.md · K-39/A-66/Y-73: başvuru penceresinin takvimi geçersiz kılma durumu.
/// DB'de int, API'de metin (sessiz onay).
/// </summary>
public enum ClubApplicationWindowOverride
{
    /// <summary>Takvime uy: tarihler tanımlıysa aralıkta açık, tanımsızsa KAPALI (fail-closed).</summary>
    FollowSchedule = 0,

    /// <summary>Tarihlere bakılmaksızın açık.</summary>
    ForceOpen = 1,

    /// <summary>Tarihlere bakılmaksızın kapalı.</summary>
    ForceClosed = 2,
}
```

- [ ] **Step 2: `AcademicTerm`'e üç alanı ekle**

`src/Entities/AcademicTerm.cs` — `IsCurrent` özelliğinin altına:

```csharp
    /// <summary>docs/MIMARI.md · K-39/A-66: topluluk kurma başvurularının açıldığı an. Null = takvim tanımsız.</summary>
    public DateTime? ClubApplicationStartUtc { get; set; }

    /// <summary>docs/MIMARI.md · K-39/A-66: başvuruların kapandığı an. Null = takvim tanımsız.</summary>
    public DateTime? ClubApplicationEndUtc { get; set; }

    /// <summary>
    /// docs/MIMARI.md · K-39/A-66: takvimi geçersiz kılma. Varsayılan FollowSchedule —
    /// tarihler tanımsızken bu KAPALI demektir (fail-closed); başvuru sezonuna kurum karar verir.
    /// </summary>
    public ClubApplicationWindowOverride ClubApplicationOverride { get; set; }
```

Dosyanın başına ekle:

```csharp
using Entities.Enums;
```

> **EF konfigürasyonuna dokunulmuyor.** `AcademicTermConfiguration` yalnızca `Name` ve `IsCurrent` index'lerini kuruyor; üç yeni alan varsayılan eşleme ile yeterli (iki nullable `datetime2`, bir `int`).

- [ ] **Step 3: Migration üret**

```bash
dotnet ef migrations add 20260826_Faz31_BasvuruTakvimi --project src/DataAccess --startup-project src/WebAPI
```

Üretilen dosyayı aç ve `Up()` içinde **tam olarak üç `AddColumn`** olduğunu doğrula:

```csharp
migrationBuilder.AddColumn<DateTime>(
    name: "ClubApplicationStartUtc", table: "AcademicTerms", type: "datetime2", nullable: true);

migrationBuilder.AddColumn<DateTime>(
    name: "ClubApplicationEndUtc", table: "AcademicTerms", type: "datetime2", nullable: true);

migrationBuilder.AddColumn<int>(
    name: "ClubApplicationOverride", table: "AcademicTerms", type: "int", nullable: false, defaultValue: 0);
```

Başka tablo/kolon değişikliği varsa **dur** — snapshot senkron değil demektir.

- [ ] **Step 4: Veri adımını migration'a elle ekle**

> **Bu adım atlanırsa dağıtım anında başvurular sessizce kapanır.** Fail-closed varsayılan (`FollowSchedule` + tarih yok = kapalı) bugüne kadar hep açık olan bir akışı durdurur. Planın en kolay unutulan satırı budur.

Üretilen migration dosyasında, `Up()` metodunun **son `AddColumn` çağrısından sonra** ekle:

```csharp
        // A-66: dağıtım anında kesinti olmasın diye mevcut güncel dönem "zorla açık" başlar.
        // Yönetici takvimi tanımlayınca FollowSchedule'a geçirir. Yeni dönemler (K-30 devri)
        // varsayılan FollowSchedule ile, yani KAPALI doğar — sezonu yönetici açar.
        migrationBuilder.Sql("UPDATE [AcademicTerms] SET [ClubApplicationOverride] = 1 WHERE [IsCurrent] = 1;");
```

`Down()` metoduna dokunma — üç kolon zaten düşürülüyor, veri adımının geri alınacak bir izi kalmıyor.

- [ ] **Step 5: Derle ve tüm testleri çalıştır**

```bash
dotnet build
dotnet test
```

Beklenen: sıfır uyarı, **tüm testler yeşil.** Bu task davranış değiştirmez — hiçbir kod yeni alanları henüz okumuyor.

- [ ] **Step 6: Migration'ın gerçekten uygulandığını doğrula**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~UygulamaAyagaKalkarTests"
```

Beklenen: PASS. Bu test `Database.MigrateAsync()` çağırıyor; migration bozuksa burada patlar.

- [ ] **Step 7: Commit**

```bash
git add src/Entities/Enums/ClubApplicationWindowOverride.cs src/Entities/AcademicTerm.cs src/DataAccess/Migrations
git commit -m "Faz 31 adim 1: basvuru penceresi semasi (K-39, A-66)"
```

---

### Task 2: Karar kuralı — `EvaluateWindow` ve durum okuma metodu

**Files:**
- Create: `src/Business/DTOs/ClubApplications/ClubApplicationWindowDto.cs`
- Modify: `src/Business/Abstract/IClubApplicationService.cs`
- Modify: `src/Business/Concrete/ClubApplicationManager.cs`
- Test: `tests/Business.Tests/ClubApplicationManagerTests.cs` (**yeni dosya**)

**Interfaces:**
- Consumes: `Entities.Enums.ClubApplicationWindowOverride`, `AcademicTerm.ClubApplication*` (Task 1)
- Produces:
  - `Business.DTOs.ClubApplications.ClubApplicationWindowDto` — `bool IsOpen`, `DateTime? StartUtc`, `DateTime? EndUtc`, `ClubApplicationWindowOverride Override`, `string TermName`
  - `IClubApplicationService.GetWindowAsync(CancellationToken)` → `Task<IDataResult<ClubApplicationWindowDto>>`
  - `ClubApplicationManager.EvaluateWindow(AcademicTerm, DateTime)` → `bool` (**private static**)

> **Y-73'ün kalbi:** `EvaluateWindow` tek karar noktasıdır. Task 3 muhafızı da bu metodu çağıracak. İkinci bir `if` yazma.

- [ ] **Step 1: Failing test dosyasını yaz**

`tests/Business.Tests/ClubApplicationManagerTests.cs` (yeni dosya):

```csharp
using System.Linq.Expressions;
using Business.Concrete;
using Core.DataAccess;
using Core.Utilities.Security;
using Core.Utilities.Time;
using DataAccess.Repositories;
using Entities;
using Entities.Enums;
using Hangfire;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>
/// docs/PLAN-V6.md §Faz 31 · Y-73: pencere kararı TEK metotta hesaplanır. Bu sınıf o metodun
/// beş satırlık karar tablosunu (A-66) doğrular — muhafız ve okuma ucu aynı cevabı vermek zorunda.
/// </summary>
public class ClubApplicationManagerTests
{
    private static readonly DateTime FixedNow = new(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IEntityRepository<ClubApplication>> _clubApplicationRepository = new();
    private readonly Mock<IEntityRepository<Club>> _clubRepository = new();
    private readonly Mock<IEntityRepository<ClubMembership>> _clubMembershipRepository = new();
    private readonly Mock<IEntityRepository<Student>> _studentRepository = new();
    private readonly Mock<IEntityRepository<AcademicStaff>> _academicStaffRepository = new();
    private readonly Mock<IEntityRepository<AcademicTerm>> _academicTermRepository = new();
    private readonly Mock<IAcademicStaffDal> _academicStaffDal = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IBackgroundJobClient> _backgroundJobClient = new();
    private readonly ClubApplicationManager _sut;

    public ClubApplicationManagerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(FixedNow);
        _currentUser.Setup(c => c.UserId).Returns(100);
        _currentUser.Setup(c => c.Permissions).Returns([]);

        _sut = new ClubApplicationManager(
            _clubApplicationRepository.Object,
            _clubRepository.Object,
            _clubMembershipRepository.Object,
            _studentRepository.Object,
            _academicStaffRepository.Object,
            _academicTermRepository.Object,
            _academicStaffDal.Object,
            _unitOfWork.Object,
            _currentUser.Object,
            _clock.Object,
            _backgroundJobClient.Object);
    }

    /// <summary>Güncel dönem; pencere alanları teste göre doldurulur.</summary>
    private void SetupCurrentTerm(
        ClubApplicationWindowOverride windowOverride,
        DateTime? startUtc = null,
        DateTime? endUtc = null)
    {
        var term = new AcademicTerm
        {
            Id = 1,
            Name = "2026-2027 Güz",
            StartDateUtc = FixedNow.AddMonths(-2),
            EndDateUtc = FixedNow.AddMonths(3),
            IsCurrent = true,
            ClubApplicationStartUtc = startUtc,
            ClubApplicationEndUtc = endUtc,
            ClubApplicationOverride = windowOverride,
        };

        _academicTermRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(term);
    }

    [Fact(DisplayName = "A-66 satır 3: FollowSchedule + now aralıkta → pencere AÇIK")]
    public async Task GetWindowAsync_FollowSchedule_NowInRange_IsOpen()
    {
        SetupCurrentTerm(ClubApplicationWindowOverride.FollowSchedule, FixedNow.AddDays(-2), FixedNow.AddDays(2));

        var result = await _sut.GetWindowAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsOpen);
        Assert.Equal("2026-2027 Güz", result.Data.TermName);
    }

    [Fact(DisplayName = "A-66 satır 4: FollowSchedule + now aralıktan ÖNCE → pencere KAPALI")]
    public async Task GetWindowAsync_FollowSchedule_NowBeforeRange_IsClosed()
    {
        SetupCurrentTerm(ClubApplicationWindowOverride.FollowSchedule, FixedNow.AddDays(5), FixedNow.AddDays(10));

        var result = await _sut.GetWindowAsync();

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsOpen);
    }

    [Fact(DisplayName = "A-66 satır 4: FollowSchedule + now aralıktan SONRA → pencere KAPALI")]
    public async Task GetWindowAsync_FollowSchedule_NowAfterRange_IsClosed()
    {
        SetupCurrentTerm(ClubApplicationWindowOverride.FollowSchedule, FixedNow.AddDays(-10), FixedNow.AddDays(-5));

        var result = await _sut.GetWindowAsync();

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsOpen);
    }

    [Fact(DisplayName = "A-66 satır 5: FollowSchedule + tarih tanımsız → pencere KAPALI (fail-closed)")]
    public async Task GetWindowAsync_FollowSchedule_NoDates_IsClosed()
    {
        SetupCurrentTerm(ClubApplicationWindowOverride.FollowSchedule);

        var result = await _sut.GetWindowAsync();

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsOpen);
    }

    [Fact(DisplayName = "A-66 satır 5: FollowSchedule + yalnızca başlangıç tanımlı → pencere KAPALI")]
    public async Task GetWindowAsync_FollowSchedule_OnlyStartDefined_IsClosed()
    {
        SetupCurrentTerm(ClubApplicationWindowOverride.FollowSchedule, FixedNow.AddDays(-2), null);

        var result = await _sut.GetWindowAsync();

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsOpen);
    }

    [Fact(DisplayName = "A-66 satır 1: ForceOpen + tarih aralık dışında bile pencere AÇIK")]
    public async Task GetWindowAsync_ForceOpen_OutOfRange_IsOpen()
    {
        SetupCurrentTerm(ClubApplicationWindowOverride.ForceOpen, FixedNow.AddDays(-10), FixedNow.AddDays(-5));

        var result = await _sut.GetWindowAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsOpen);
    }

    [Fact(DisplayName = "A-66 satır 1: ForceOpen + hiç tarih yokken pencere AÇIK")]
    public async Task GetWindowAsync_ForceOpen_NoDates_IsOpen()
    {
        SetupCurrentTerm(ClubApplicationWindowOverride.ForceOpen);

        var result = await _sut.GetWindowAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsOpen);
    }

    [Fact(DisplayName = "A-66 satır 2: ForceClosed + now aralıkta olsa bile pencere KAPALI")]
    public async Task GetWindowAsync_ForceClosed_NowInRange_IsClosed()
    {
        SetupCurrentTerm(ClubApplicationWindowOverride.ForceClosed, FixedNow.AddDays(-2), FixedNow.AddDays(2));

        var result = await _sut.GetWindowAsync();

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsOpen);
    }

    [Fact(DisplayName = "K-39: güncel dönem yoksa pencere KAPALI döner, çökmez")]
    public async Task GetWindowAsync_NoCurrentTerm_IsClosed()
    {
        _academicTermRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcademicTerm?)null);

        var result = await _sut.GetWindowAsync();

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsOpen);
        Assert.Equal(string.Empty, result.Data.TermName);
    }

    [Fact(DisplayName = "K-39: pencere DTO'su tarihleri ve override'ı olduğu gibi taşır (arayüz metni buradan kurar)")]
    public async Task GetWindowAsync_CarriesDatesAndOverride()
    {
        var start = FixedNow.AddDays(-2);
        var end = FixedNow.AddDays(2);
        SetupCurrentTerm(ClubApplicationWindowOverride.FollowSchedule, start, end);

        var result = await _sut.GetWindowAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(start, result.Data!.StartUtc);
        Assert.Equal(end, result.Data.EndUtc);
        Assert.Equal(ClubApplicationWindowOverride.FollowSchedule, result.Data.Override);
    }
}
```

- [ ] **Step 2: Test'i çalıştır, derlenmediğini gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~ClubApplicationManagerTests"
```

Beklenen: **derleme hatası** — `error CS1061: 'ClubApplicationManager' does not contain a definition for 'GetWindowAsync'`.

- [ ] **Step 3: DTO'yu oluştur**

`src/Business/DTOs/ClubApplications/ClubApplicationWindowDto.cs`:

```csharp
using Entities.Enums;

namespace Business.DTOs.ClubApplications;

/// <summary>
/// docs/MIMARI.md · K-39/A-66/Y-73: başvuru penceresinin durumu. `IsOpen` kararı sunucuda
/// verilir — arayüz tarihlere bakıp kendi kararını vermez (Y-35). Tarihler yalnızca
/// "3 Ekim'de açılıyor" metnini kurmak için taşınır.
/// </summary>
public sealed class ClubApplicationWindowDto
{
    public bool IsOpen { get; set; }

    public DateTime? StartUtc { get; set; }

    public DateTime? EndUtc { get; set; }

    public ClubApplicationWindowOverride Override { get; set; }

    /// <summary>Güncel dönem yoksa boş string.</summary>
    public required string TermName { get; set; }
}
```

- [ ] **Step 4: Servis sözleşmesine ekle**

`src/Business/Abstract/IClubApplicationService.cs` — `GetMineAsync` bildiriminin **üstüne**:

```csharp
    /// <summary>
    /// docs/MIMARI.md · K-39/Y-73: pencerenin güncel durumu. `[SecuredOperation]` yok — herhangi bir
    /// kimliği doğrulanmış kullanıcı görebilir (SubmitAsync ile aynı erişim sınıfı). Anonim yüzeye
    /// eklenmez (A-42). Muhafız ile bu uç AYNI EvaluateWindow metodunu çağırır.
    /// </summary>
    Task<IDataResult<ClubApplicationWindowDto>> GetWindowAsync(CancellationToken cancellationToken = default);
```

- [ ] **Step 5: Karar metodunu ve okuma metodunu yaz**

`src/Business/Concrete/ClubApplicationManager.cs`:

**(a)** Sınıfın **en altına**, mevcut `ClampPageSize` metodunun üstüne, karar metodunu ekle:

```csharp
    /// <summary>
    /// docs/MIMARI.md · A-66/Y-73: pencere kararının TEK yeri. SubmitAsync muhafızı ve
    /// GetWindowAsync bu metodu çağırır — ikinci bir if yazmak, ekranın "açık" derken API'nin
    /// "kapalı" demesinin garantili yoludur.
    /// Fail-closed: FollowSchedule + eksik tarih = kapalı.
    /// </summary>
    private static bool EvaluateWindow(AcademicTerm term, DateTime nowUtc) => term.ClubApplicationOverride switch
    {
        ClubApplicationWindowOverride.ForceOpen => true,
        ClubApplicationWindowOverride.ForceClosed => false,
        _ => term.ClubApplicationStartUtc is { } start
             && term.ClubApplicationEndUtc is { } end
             && nowUtc >= start
             && nowUtc <= end,
    };
```

**(b)** `GetMineAsync` metodunun **üstüne** okuma metodunu ekle:

```csharp
    public async Task<IDataResult<ClubApplicationWindowDto>> GetWindowAsync(CancellationToken cancellationToken = default)
    {
        var term = await academicTermRepository.GetAsync(t => t.IsCurrent, cancellationToken).ConfigureAwait(false);

        // Güncel dönem yoksa pencere kapalıdır — SubmitAsync zaten NoCurrentAcademicTerm ile durur.
        // Burada hata değil "kapalı" dönüyoruz: arayüz düğmeyi pasif gösterip sebebi yazabilsin.
        if (term is null)
        {
            return DataResult<ClubApplicationWindowDto>.Success(new ClubApplicationWindowDto
            {
                IsOpen = false,
                Override = ClubApplicationWindowOverride.FollowSchedule,
                TermName = string.Empty,
            });
        }

        return DataResult<ClubApplicationWindowDto>.Success(new ClubApplicationWindowDto
        {
            IsOpen = EvaluateWindow(term, clock.UtcNow),
            StartUtc = term.ClubApplicationStartUtc,
            EndUtc = term.ClubApplicationEndUtc,
            Override = term.ClubApplicationOverride,
            TermName = term.Name,
        });
    }
```

- [ ] **Step 6: Test'i çalıştır, geçtiğini gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~ClubApplicationManagerTests"
```

Beklenen: **10 passed**. Çıktıda test sayısının 10 olduğunu gözle doğrula — `--filter` yazım hatası yaptığında `dotnet test` "No test matches" deyip **başarılı çıkar.**

- [ ] **Step 7: Tüm test takımını çalıştır**

```bash
dotnet build
dotnet test
```

Beklenen: sıfır uyarı, tüm testler yeşil.

> **`ServiceContractTests` uyarısı.** `Architecture.Tests` içinde servis sözleşmesi kuralları var (dönüş tipi `IResult`/`IDataResult`, `CancellationToken` parametresi vb.). `GetWindowAsync` bu kuralların hepsine uyuyor; kırılırsa test mesajını oku ve **sözleşmeyi düzelt**, testi değil.

- [ ] **Step 8: Commit**

```bash
git add src/Business/DTOs/ClubApplications/ClubApplicationWindowDto.cs src/Business/Abstract/IClubApplicationService.cs src/Business/Concrete/ClubApplicationManager.cs tests/Business.Tests/ClubApplicationManagerTests.cs
git commit -m "Faz 31 adim 2: pencere karar kurali ve durum okuma (K-39, A-66)"
```

---

### Task 3: Muhafız — kapalı pencerede başvuru reddedilir

**Files:**
- Modify: `src/Business/Constants/Messages.cs`
- Modify: `src/Business/Concrete/ClubApplicationManager.cs` (`SubmitAsync`)
- Modify: `src/WebAPI/Controllers/ClubApplicationsController.cs`
- Test: `tests/WebAPI.IntegrationTests/ClubApplicationFlowTests.cs`

**Interfaces:**
- Consumes: `ClubApplicationManager.EvaluateWindow` (Task 2), `IClubApplicationService.GetWindowAsync` (Task 2)
- Produces:
  - `Business.Constants.Messages.ClubApplicationsClosed` — `const string`
  - `GET /api/club-applications/window` → `ClubApplicationWindowDto`

**Kural:** Muhafız, güncel dönem bulunduktan **hemen sonra** durur — danışman/ad çakışması/çift başvuru kontrollerinden **önce.** Sırası önemli: pencere kapalıyken öğrenci "bu isim alınmış" değil, sebebi doğru olan cevabı almalı.

**Pencere yalnızca gönderimi kapatır.** `DecideAsync`'e dokunulmaz.

- [ ] **Step 1: Failing entegrasyon testlerini yaz**

`tests/WebAPI.IntegrationTests/ClubApplicationFlowTests.cs` dosyasının sonundaki kapanış süslü parantezinden önce ekle. Dosyadaki `_client`, `_factory`, `_proposedAdvisorId`, `LoginAsync`, `SendWithBearerAsync` üyeleri zaten var.

```csharp
    /// <summary>docs/MIMARI.md · A-66: güncel dönemin pencere alanlarını testin istediği hâle getirir.</summary>
    private async Task SetWindowAsync(ClubApplicationWindowOverride windowOverride, DateTime? startUtc, DateTime? endUtc)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var term = await db.AcademicTerms.SingleAsync(t => t.IsCurrent);

        term.ClubApplicationOverride = windowOverride;
        term.ClubApplicationStartUtc = startUtc;
        term.ClubApplicationEndUtc = endUtc;
        await db.SaveChangesAsync();
    }

    private async Task<HttpResponseMessage> SubmitApplicationAsync(string accessToken) =>
        await SendWithBearerAsync(HttpMethod.Post, "/api/club-applications", accessToken, new
        {
            ProposedName = $"Pencere Kulübü {Guid.NewGuid():N}"[..30],
            Description = "Pencere testi",
            Justification = "Pencere testi gerekçesi",
            ProposedAdvisorId = _proposedAdvisorId,
        });

    [Theory(DisplayName = "Y-73: pencere kapalıyken başvuru 409, açıkken kabul — beş senaryonun tamamı")]
    [InlineData(ClubApplicationWindowOverride.ForceOpen, -10, -5, true)]
    [InlineData(ClubApplicationWindowOverride.ForceClosed, -2, 2, false)]
    [InlineData(ClubApplicationWindowOverride.FollowSchedule, -2, 2, true)]
    [InlineData(ClubApplicationWindowOverride.FollowSchedule, 5, 10, false)]
    public async Task Submit_RespectsApplicationWindow(
        ClubApplicationWindowOverride windowOverride, int startOffsetDays, int endOffsetDays, bool expectedOpen)
    {
        var now = DateTime.UtcNow;
        await SetWindowAsync(windowOverride, now.AddDays(startOffsetDays), now.AddDays(endOffsetDays));

        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);
        var response = await SubmitApplicationAsync(studentToken);

        if (expectedOpen)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        else
        {
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
    }

    [Fact(DisplayName = "A-66: FollowSchedule + takvim tanımsız → başvuru 409 (fail-closed)")]
    public async Task Submit_FollowScheduleWithoutDates_ReturnsConflict()
    {
        await SetWindowAsync(ClubApplicationWindowOverride.FollowSchedule, null, null);

        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);
        var response = await SubmitApplicationAsync(studentToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact(DisplayName = "Y-73: okuma ucu ile muhafız aynı cevabı verir — beş senaryoda da")]
    public async Task Window_Endpoint_AgreesWithSubmitGuard()
    {
        var now = DateTime.UtcNow;
        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);

        var scenarios = new (ClubApplicationWindowOverride Override, DateTime? Start, DateTime? End)[]
        {
            (ClubApplicationWindowOverride.ForceOpen, now.AddDays(-10), now.AddDays(-5)),
            (ClubApplicationWindowOverride.ForceClosed, now.AddDays(-2), now.AddDays(2)),
            (ClubApplicationWindowOverride.FollowSchedule, now.AddDays(-2), now.AddDays(2)),
            (ClubApplicationWindowOverride.FollowSchedule, now.AddDays(5), now.AddDays(10)),
            (ClubApplicationWindowOverride.FollowSchedule, null, null),
        };

        foreach (var scenario in scenarios)
        {
            await SetWindowAsync(scenario.Override, scenario.Start, scenario.End);

            var windowResponse = await SendWithBearerAsync(HttpMethod.Get, "/api/club-applications/window", studentToken);
            Assert.Equal(HttpStatusCode.OK, windowResponse.StatusCode);
            var windowBody = await windowResponse.Content.ReadFromJsonAsync<WindowProbe>();

            var submitResponse = await SubmitApplicationAsync(studentToken);
            var submitAccepted = submitResponse.StatusCode == HttpStatusCode.OK;

            Assert.True(
                windowBody!.IsOpen == submitAccepted,
                $"Okuma ucu IsOpen={windowBody.IsOpen} derken gönderim {submitResponse.StatusCode} döndü ({scenario.Override}).");
        }
    }

    [Fact(DisplayName = "K-39: pencere kapalıyken yönetici bekleyen başvuruyu yine de karara bağlayabilir")]
    public async Task Decide_WorksWhileWindowIsClosed()
    {
        var now = DateTime.UtcNow;

        // Önce pencereyi aç ve bir başvuru üret.
        await SetWindowAsync(ClubApplicationWindowOverride.ForceOpen, null, null);
        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);
        var submitResponse = await SubmitApplicationAsync(studentToken);
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        // Sonra pencereyi kapat.
        await SetWindowAsync(ClubApplicationWindowOverride.ForceClosed, now.AddDays(-2), now.AddDays(2));

        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        var pendingResponse = await SendWithBearerAsync(HttpMethod.Get, "/api/club-applications?pageIndex=0&pageSize=200", adminToken);
        Assert.Equal(HttpStatusCode.OK, pendingResponse.StatusCode);
        var pending = await pendingResponse.Content.ReadFromJsonAsync<PagedProbe>();
        var applicationId = pending!.Items[^1].Id;

        var decisionResponse = await SendWithBearerAsync(
            HttpMethod.Put, $"/api/club-applications/{applicationId}/decision", adminToken,
            new { Status = "Rejected", ReviewNote = "Pencere kapalıyken karar" });

        Assert.Equal(HttpStatusCode.OK, decisionResponse.StatusCode);
    }

    private sealed record WindowProbe(bool IsOpen, DateTime? StartUtc, DateTime? EndUtc, string Override, string TermName);

    private sealed record PagedProbe(List<PagedProbeItem> Items, int TotalCount, int PageIndex, int PageSize);

    private sealed record PagedProbeItem(int Id, string ProposedName);
```

> **Test izolasyonu (Y-34).** Bu testler paylaşılan güncel dönemin pencere alanlarını değiştiriyor. `ClubApplicationFlowTests` tek bir sınıf koleksiyonunda (`[Collection("WebAPI Integration Tests")]`) sıralı çalıştığı için çakışma olmaz; ama **dosyadaki mevcut testler pencereye bağımlı hâle gelir.** Bunu Step 4'te çözüyoruz.

- [ ] **Step 2: Test'leri çalıştır, kırmızı olduğunu gör**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~Submit_RespectsApplicationWindow|FullyQualifiedName~Submit_FollowScheduleWithoutDates|FullyQualifiedName~Window_Endpoint_AgreesWithSubmitGuard|FullyQualifiedName~Decide_WorksWhileWindowIsClosed"
```

Beklenen: hepsi **FAIL** — `/api/club-applications/window` ucu 404 döner, muhafız olmadığı için kapalı senaryolar da `OK` alır.

- [ ] **Step 3: Mesajı ekle**

`src/Business/Constants/Messages.cs` — `ClubApplicationRejected` sabitinin altına:

```csharp
    // Faz 31 — Başvuru takvimi (K-39, Y-73)
    // Y-73: mesaj sebebi VE takvimin nerede görüleceğini söyler. Kesin tarihler
    // GET /club-applications/window ucundan gelir — Y-25 gereği mesajın kendisi nötr kalır.
    public const string ClubApplicationsClosed =
        "Topluluk kurma başvuruları şu anda kapalı. Başvuru takvimini Başvurularım sayfasından görebilirsiniz.";
```

- [ ] **Step 4: Muhafızı `SubmitAsync`'e ekle**

`src/Business/Concrete/ClubApplicationManager.cs` — `SubmitAsync` içinde, `NoCurrentAcademicTerm` kontrolünün kapanış parantezinden **hemen sonra**, `var advisor = await academicStaffRepository` satırından **önce**:

```csharp
        // Y-73: pencere muhafızı, danışman/ad çakışması/çift başvuru kontrollerinden ÖNCE gelir —
        // kapalı pencerede öğrenci "bu isim alınmış" değil, sebebi doğru olan cevabı almalı.
        // A-66: karar EvaluateWindow'da; ikinci bir if yok (GetWindowAsync da aynı metodu çağırır).
        if (!EvaluateWindow(term, clock.UtcNow))
        {
            return Result.Conflict(Messages.ClubApplicationsClosed);
        }
```

- [ ] **Step 5: Mevcut testleri pencereden bağımsız hâle getir**

`tests/WebAPI.IntegrationTests/ClubApplicationFlowTests.cs` — `InitializeAsync` metodunun **sonunda**, `_client = _factory.CreateClient();` satırının **hemen üstüne** ekle:

```csharp
        // Faz 31: pencere varsayılanı fail-closed (A-66). Bu sınıftaki AKIŞ testleri pencereyi
        // sınamıyor, akışı sınıyor — bu yüzden her kurulumda pencere açık başlar. Pencereyi
        // sınayan testler kendi durumlarını SetWindowAsync ile kurar (Y-34: her test kendi verisini kurar).
        var currentTerm = await db.AcademicTerms.SingleAsync(t => t.IsCurrent);
        currentTerm.ClubApplicationOverride = ClubApplicationWindowOverride.ForceOpen;
        currentTerm.ClubApplicationStartUtc = null;
        currentTerm.ClubApplicationEndUtc = null;
        await db.SaveChangesAsync();
```

- [ ] **Step 6: Controller ucunu ekle**

`src/WebAPI/Controllers/ClubApplicationsController.cs` — `GetMine` action'ının **üstüne**:

```csharp
    /// <summary>docs/MIMARI.md · K-39/A-42: giriş yapmış kullanıcıya açık; anonim vitrine eklenmez.</summary>
    [HttpGet("club-applications/window")]
    public async Task<IActionResult> GetWindow(CancellationToken cancellationToken)
    {
        var result = await clubApplicationService.GetWindowAsync(cancellationToken);
        return result.ToActionResult();
    }
```

> **Rota sırası önemli değil** — `club-applications/window` ile `club-applications/mine` farklı literal segmentler; `{id:int}` kısıtı zaten sayı istiyor, çakışma yok.

- [ ] **Step 7: Test'leri çalıştır, geçtiğini gör**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~Submit_RespectsApplicationWindow|FullyQualifiedName~Submit_FollowScheduleWithoutDates|FullyQualifiedName~Window_Endpoint_AgreesWithSubmitGuard|FullyQualifiedName~Decide_WorksWhileWindowIsClosed"
```

Beklenen: **7 passed** (4 `Theory` satırı + 3 `Fact`).

- [ ] **Step 8: Tüm test takımını çalıştır**

```bash
dotnet build
dotnet test
```

Beklenen: sıfır uyarı, tüm testler yeşil. Özellikle `ClubApplicationFlowTests`'in mevcut akış testleri (`Submit_Then_Approve_CreatesClubAndPresidentMembership`) yeşil kalmalı — Step 5 bunu garanti eder.

- [ ] **Step 9: Commit**

```bash
git add src/Business/Constants/Messages.cs src/Business/Concrete/ClubApplicationManager.cs src/WebAPI/Controllers/ClubApplicationsController.cs tests/WebAPI.IntegrationTests/ClubApplicationFlowTests.cs
git commit -m "Faz 31 adim 3: basvuru penceresi muhafizi ve durum ucu (K-39, Y-73)"
```

---

### Task 4: Yönetici ucu — takvimi tanımlama ve elle açıp kapatma

**Files:**
- Create: `src/Business/DTOs/Reference/SetClubApplicationWindowRequestDto.cs`
- Create: `src/Business/ValidationRules/SetClubApplicationWindowRequestValidator.cs`
- Modify: `src/Business/DTOs/Reference/AcademicTermListItemDto.cs`
- Modify: `src/Business/Abstract/IAcademicTermService.cs`
- Modify: `src/Business/Concrete/AcademicTermManager.cs`
- Modify: `src/WebAPI/Controllers/AcademicTermsController.cs`
- Test: `tests/Business.Tests/ValidationRulesTests.cs`

**Interfaces:**
- Consumes: `Entities.Enums.ClubApplicationWindowOverride`, `AcademicTerm.ClubApplication*` (Task 1)
- Produces:
  - `Business.DTOs.Reference.SetClubApplicationWindowRequestDto` — `DateTime? StartUtc`, `DateTime? EndUtc`, `ClubApplicationWindowOverride Override`
  - `IAcademicTermService.SetClubApplicationWindowAsync(int, SetClubApplicationWindowRequestDto, CancellationToken)` → `Task<IResult>`
  - `AcademicTermListItemDto.ClubApplicationStartUtc` / `.ClubApplicationEndUtc` / `.ClubApplicationOverride`
  - `PUT /api/academic-terms/{id}/club-application-window`

**Yeni izin yok.** `reference.manage` zaten dönem yönetiminin izni (A-66).

- [ ] **Step 1: Failing validator testlerini yaz**

`tests/Business.Tests/ValidationRulesTests.cs` dosyasının sonundaki kapanış süslü parantezinden önce ekle:

```csharp
    [Fact(DisplayName = "K-39: bitiş tarihi başlangıçtan önceyse pencere isteği geçersizdir")]
    public void SetClubApplicationWindowRequestValidator_EndBeforeStart_IsInvalid()
    {
        var validator = new SetClubApplicationWindowRequestValidator();
        var request = new SetClubApplicationWindowRequestDto
        {
            StartUtc = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
            EndUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            Override = ClubApplicationWindowOverride.FollowSchedule,
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "K-39: geçerli tarih aralığı ve tanımlı override kabul edilir")]
    public void SetClubApplicationWindowRequestValidator_ValidRange_IsValid()
    {
        var validator = new SetClubApplicationWindowRequestValidator();
        var request = new SetClubApplicationWindowRequestDto
        {
            StartUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            EndUtc = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
            Override = ClubApplicationWindowOverride.FollowSchedule,
        };

        var result = validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "A-66: tarihsiz istek geçerlidir — takvimi temizlemek meşru bir eylem")]
    public void SetClubApplicationWindowRequestValidator_NullDates_IsValid()
    {
        var validator = new SetClubApplicationWindowRequestValidator();
        var request = new SetClubApplicationWindowRequestDto
        {
            StartUtc = null,
            EndUtc = null,
            Override = ClubApplicationWindowOverride.ForceClosed,
        };

        var result = validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "Y-73: tanımsız override değeri reddedilir")]
    public void SetClubApplicationWindowRequestValidator_UndefinedOverride_IsInvalid()
    {
        var validator = new SetClubApplicationWindowRequestValidator();
        var request = new SetClubApplicationWindowRequestDto
        {
            Override = (ClubApplicationWindowOverride)42,
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
    }
```

- [ ] **Step 2: Test'leri çalıştır, derlenmediğini gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~SetClubApplicationWindowRequestValidator"
```

Beklenen: **derleme hatası** — `SetClubApplicationWindowRequestValidator` ve `SetClubApplicationWindowRequestDto` bulunamıyor.

- [ ] **Step 3: Giriş DTO'sunu oluştur**

`src/Business/DTOs/Reference/SetClubApplicationWindowRequestDto.cs`:

```csharp
using Entities.Enums;

namespace Business.DTOs.Reference;

/// <summary>
/// docs/MIMARI.md · K-39/A-66: dönemin başvuru penceresi. Üç alan BİRLİKTE yazılır —
/// kısmi güncelleme yok, yönetici her seferinde tam durumu bildirir.
/// </summary>
public sealed class SetClubApplicationWindowRequestDto
{
    /// <summary>Null = takvim tanımsız. FollowSchedule ile birlikte pencereyi kapatır (fail-closed).</summary>
    public DateTime? StartUtc { get; set; }

    /// <summary>Null = takvim tanımsız.</summary>
    public DateTime? EndUtc { get; set; }

    public ClubApplicationWindowOverride Override { get; set; }
}
```

- [ ] **Step 4: Validator'ı oluştur**

`src/Business/ValidationRules/SetClubApplicationWindowRequestValidator.cs`:

```csharp
using Business.Constants;
using Business.DTOs.Reference;
using FluentValidation;

namespace Business.ValidationRules;

/// <summary>
/// docs/MIMARI.md · Y-35: yalnızca biçim doğrulanır — "pencere şu anda açık mı" kararı
/// ClubApplicationManager.EvaluateWindow'da kalır (Y-73).
/// </summary>
public sealed class SetClubApplicationWindowRequestValidator : AbstractValidator<SetClubApplicationWindowRequestDto>
{
    public SetClubApplicationWindowRequestValidator()
    {
        RuleFor(x => x.Override).IsInEnum();

        // A-66: ikisi de null olabilir (takvimi temizlemek meşru). İkisi de doluysa sıra doğru olmalı.
        RuleFor(x => x.EndUtc)
            .GreaterThan(x => x.StartUtc)
            .When(x => x.StartUtc is not null && x.EndUtc is not null)
            .WithMessage(Messages.InvalidAcademicTermDateRange);
    }
}
```

- [ ] **Step 5: Çıkış DTO'suna pencere alanlarını ekle**

`src/Business/DTOs/Reference/AcademicTermListItemDto.cs` — `IsCurrent` özelliğinin altına:

```csharp
    /// <summary>docs/MIMARI.md · K-39: yönetim ekranı pencereyi bu üç alandan çizer.</summary>
    public DateTime? ClubApplicationStartUtc { get; set; }

    public DateTime? ClubApplicationEndUtc { get; set; }

    public ClubApplicationWindowOverride ClubApplicationOverride { get; set; }
```

Dosyanın başına ekle:

```csharp
using Entities.Enums;
```

- [ ] **Step 6: Servis sözleşmesine ekle**

`src/Business/Abstract/IAcademicTermService.cs` — `SetCurrentAsync` bildiriminin **altına**:

```csharp
    /// <summary>
    /// docs/MIMARI.md · K-39/A-66: dönemin topluluk kurma başvuru penceresi. Yeni izin yok —
    /// reference.manage zaten dönem yönetiminin izni. Üç alan birlikte yazılır.
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [ValidationAspect(typeof(SetClubApplicationWindowRequestValidator))]
    [TransactionAspect]
    Task<IResult> SetClubApplicationWindowAsync(
        int termId, SetClubApplicationWindowRequestDto request, CancellationToken cancellationToken = default);
```

- [ ] **Step 7: Manager'a implementasyonu ve liste eşlemesini ekle**

`src/Business/Concrete/AcademicTermManager.cs`:

**(a)** `GetTermsPagedAsync` içindeki `Select` projeksiyonunu şununla değiştir:

```csharp
            .Select(t => new AcademicTermListItemDto
            {
                Id = t.Id,
                Name = t.Name,
                StartDateUtc = t.StartDateUtc,
                EndDateUtc = t.EndDateUtc,
                IsCurrent = t.IsCurrent,
                ClubApplicationStartUtc = t.ClubApplicationStartUtc,
                ClubApplicationEndUtc = t.ClubApplicationEndUtc,
                ClubApplicationOverride = t.ClubApplicationOverride,
            })
```

**(b)** `SetCurrentAsync` metodunun **altına**:

```csharp
    public async Task<IResult> SetClubApplicationWindowAsync(
        int termId, SetClubApplicationWindowRequestDto request, CancellationToken cancellationToken = default)
    {
        var term = await academicTermRepository.GetAsync(t => t.Id == termId, cancellationToken).ConfigureAwait(false);
        if (term is null)
        {
            return Result.NotFound(Messages.AcademicTermNotFound);
        }

        // A-66: üç alan birlikte yazılır — kısmi güncelleme yok, yönetici tam durumu bildirir.
        term.ClubApplicationStartUtc = request.StartUtc;
        term.ClubApplicationEndUtc = request.EndUtc;
        term.ClubApplicationOverride = request.Override;

        academicTermRepository.Update(term);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.ClubApplicationWindowUpdated);
    }
```

Dosyanın başına `using Business.DTOs.Reference;` zaten var; yoksa ekle.

- [ ] **Step 8: Başarı mesajını ekle**

`src/Business/Constants/Messages.cs` — Task 3'te eklediğin `ClubApplicationsClosed` sabitinin altına:

```csharp
    public const string ClubApplicationWindowUpdated = "Başvuru takvimi güncellendi.";
```

> `Messages.AcademicTermNotFound` (satır 57) ve `Messages.InvalidAcademicTermDateRange` (satır 64) dosyada **zaten var** — yeniden ekleme, Y-29 tekrarı olur.

- [ ] **Step 9: Controller ucunu ekle**

`src/WebAPI/Controllers/AcademicTermsController.cs` — `SetCurrent` action'ının **altına**:

```csharp
    [HttpPut("{id:int}/club-application-window")]
    public async Task<IActionResult> SetClubApplicationWindow(
        int id, SetClubApplicationWindowRequestDto request, CancellationToken cancellationToken)
    {
        var result = await academicTermService.SetClubApplicationWindowAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }
```

- [ ] **Step 10: Test'leri çalıştır, geçtiğini gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~SetClubApplicationWindowRequestValidator"
```

Beklenen: **4 passed**.

- [ ] **Step 11: Tüm test takımını çalıştır**

```bash
dotnet build
dotnet test
```

Beklenen: sıfır uyarı, tüm testler yeşil.

> **`ControllerContractTests` uyarısı.** `Architecture.Tests` controller kurallarını (dönüş tipi `Task<IActionResult>`, `try/catch` yok, `CancellationToken` taşınır) doğruluyor. Yeni action bunlara uyuyor; kırılırsa mesajı oku ve **controller'ı** düzelt.

- [ ] **Step 12: Commit**

```bash
git add src/Business/DTOs/Reference src/Business/ValidationRules/SetClubApplicationWindowRequestValidator.cs src/Business/Abstract/IAcademicTermService.cs src/Business/Concrete/AcademicTermManager.cs src/Business/Constants/Messages.cs src/WebAPI/Controllers/AcademicTermsController.cs tests/Business.Tests/ValidationRulesTests.cs
git commit -m "Faz 31 adim 4: pencere yonetim ucu (K-39, A-66)"
```

---

### Task 5: Arayüz — takvim yönetimi ve öğrenci görünürlüğü

**Files:**
- Modify: `arayuz/src/api/types.ts`
- Create: `arayuz/src/schemas/clubApplicationWindowForm.ts`
- Modify: `arayuz/src/pages/ReferenceDataPage.tsx` (`TermsTab`)
- Modify: `arayuz/src/pages/ClubsPage.tsx`
- Modify: `arayuz/src/pages/MyApplicationsPage.tsx` (`ClubApplicationsTab`)

**Interfaces:**
- Consumes: `GET /api/club-applications/window` (Task 3), `PUT /api/academic-terms/{id}/club-application-window` (Task 4), `AcademicTermListItemDto` pencere alanları (Task 4)
- Produces:
  - `arayuz/src/api/types.ts` → `ClubApplicationWindowOverride`, `ClubApplicationWindowDto`
  - `arayuz/src/schemas/clubApplicationWindowForm.ts` → `clubApplicationWindowFormSchema`, `ClubApplicationWindowFormValues`, `emptyClubApplicationWindowFormValues`, `toWindowPayload`

- [ ] **Step 1: TS tiplerini ekle**

`arayuz/src/api/types.ts` — `AcademicTermListItemDto` arayüzünün **üstüne**:

```typescript
// src/Entities/Enums/ClubApplicationWindowOverride.cs (JsonStringEnumConverter ile metin olarak taşınır)
export type ClubApplicationWindowOverride = 'FollowSchedule' | 'ForceOpen' | 'ForceClosed'

// src/Business/DTOs/ClubApplications/ClubApplicationWindowDto.cs
export interface ClubApplicationWindowDto {
  isOpen: boolean
  startUtc: string | null
  endUtc: string | null
  override: ClubApplicationWindowOverride
  termName: string
}
```

Aynı dosyada `AcademicTermListItemDto` arayüzüne, `isCurrent` satırının altına:

```typescript
  clubApplicationStartUtc: string | null
  clubApplicationEndUtc: string | null
  clubApplicationOverride: ClubApplicationWindowOverride
```

- [ ] **Step 2: Form şemasını oluştur**

`arayuz/src/schemas/clubApplicationWindowForm.ts` (yeni dosya):

```typescript
import { z } from 'zod'
import type { AcademicTermListItemDto } from '../api/types'

// Y-35: yalnızca biçim doğrulanır — "pencere şu anda açık mı" kararı API'nin.
export const clubApplicationWindowFormSchema = z
  .object({
    override: z.enum(['FollowSchedule', 'ForceOpen', 'ForceClosed']),
    startDate: z.string(),
    endDate: z.string(),
  })
  .refine(
    (values) =>
      values.startDate.trim() === '' ||
      values.endDate.trim() === '' ||
      new Date(values.endDate) > new Date(values.startDate),
    { message: 'Bitiş tarihi başlangıçtan sonra olmalı.', path: ['endDate'] },
  )
  .refine((values) => values.override !== 'FollowSchedule' || (values.startDate.trim() !== '' && values.endDate.trim() !== ''), {
    message: 'Takvime uymak için iki tarih de gerekli. Aksi hâlde başvurular kapalı kalır.',
    path: ['startDate'],
  })

export type ClubApplicationWindowFormValues = z.infer<typeof clubApplicationWindowFormSchema>

export const emptyClubApplicationWindowFormValues: ClubApplicationWindowFormValues = {
  override: 'FollowSchedule',
  startDate: '',
  endDate: '',
}

export function toWindowFormValues(term: AcademicTermListItemDto): ClubApplicationWindowFormValues {
  return {
    override: term.clubApplicationOverride,
    startDate: term.clubApplicationStartUtc?.slice(0, 10) ?? '',
    endDate: term.clubApplicationEndUtc?.slice(0, 10) ?? '',
  }
}

export function toWindowPayload(values: ClubApplicationWindowFormValues) {
  return {
    override: values.override,
    startUtc: values.startDate.trim() === '' ? null : new Date(`${values.startDate}T00:00:00Z`).toISOString(),
    // Bitiş günü dahil olsun diye günün sonuna çekilir — yönetici "10 Ekim'e kadar" derken
    // 10 Ekim'i kastediyor. Aralık kontrolü sunucuda kapsayıcı (A-66).
    endUtc: values.endDate.trim() === '' ? null : new Date(`${values.endDate}T23:59:59Z`).toISOString(),
  }
}
```

- [ ] **Step 3: Dönem sekmesine pencere kolonunu ve diyaloğu ekle**

`arayuz/src/pages/ReferenceDataPage.tsx` → `TermsTab` fonksiyonu içinde:

**(a)** Durum ve form kurulumu — mevcut `editForm` tanımının altına:

```tsx
  const [windowTarget, setWindowTarget] = useState<AcademicTermListItemDto | null>(null)

  const windowForm = useForm<ClubApplicationWindowFormValues>({
    resolver: zodResolver(clubApplicationWindowFormSchema),
    defaultValues: emptyClubApplicationWindowFormValues,
  })

  const windowMutation = useMutation({
    mutationFn: async (values: ClubApplicationWindowFormValues) => {
      await apiClient.put(`/academic-terms/${windowTarget!.id}/club-application-window`, toWindowPayload(values))
    },
    onSuccess: () => {
      notify({ message: 'Başvuru takvimi güncellendi.', severity: 'success' })
      setWindowTarget(null)
      windowForm.reset(emptyClubApplicationWindowFormValues)
      queryClient.invalidateQueries({ queryKey: ['academic-terms'] })
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Başvuru takvimi güncellenemedi.'), severity: 'error' }),
  })
```

**(b)** `columns` dizisinde `isCurrent` kolonundan **sonra**:

```tsx
    {
      field: 'clubApplicationOverride',
      headerName: 'Başvuru Takvimi',
      width: 200,
      renderCell: (params) => {
        if (params.row.clubApplicationOverride === 'ForceOpen') {
          return <Chip size="small" color="success" label="Zorla açık" />
        }
        if (params.row.clubApplicationOverride === 'ForceClosed') {
          return <Chip size="small" color="error" label="Zorla kapalı" />
        }
        if (!params.row.clubApplicationStartUtc || !params.row.clubApplicationEndUtc) {
          return <Chip size="small" color="default" label="Takvim yok — kapalı" />
        }
        return (
          <Chip
            size="small"
            color="info"
            label={`${new Date(params.row.clubApplicationStartUtc).toLocaleDateString('tr-TR')} – ${new Date(params.row.clubApplicationEndUtc).toLocaleDateString('tr-TR')}`}
          />
        )
      },
    },
```

**(c)** `actions` kolonunun `renderCell`'indeki `<Stack>` içine, "Güncel Yap" düğmesinin **yanına**:

```tsx
          <Button
            size="small"
            variant="text"
            onClick={() => {
              setWindowTarget(params.row)
              windowForm.reset(toWindowFormValues(params.row))
            }}
          >
            Takvim
          </Button>
```

`actions` kolonunun `width` değerini `210`'dan `300`'e çıkar.

**(d)** `TermsTab`'ın döndürdüğü JSX'in sonuna, mevcut düzenleme diyaloğunun **altına**:

```tsx
      <Dialog
        open={windowTarget !== null}
        onClose={() => {
          setWindowTarget(null)
          windowForm.reset(emptyClubApplicationWindowFormValues)
        }}
        fullWidth
        maxWidth="xs"
      >
        <DialogTitle>Topluluk Kurma Başvuru Takvimi</DialogTitle>
        <DialogContent>
          <DialogContentText sx={{ mb: 1 }}>
            {windowTarget && `"${windowTarget.name}" dönemi için başvuruların ne zaman açık olacağını belirleyin.`}
          </DialogContentText>
          <Controller
            name="override"
            control={windowForm.control}
            render={({ field }) => (
              <FormControl margin="dense">
                <FormLabel>Durum</FormLabel>
                <RadioGroup {...field}>
                  <FormControlLabel value="FollowSchedule" control={<Radio />} label="Takvime uy" />
                  <FormControlLabel value="ForceOpen" control={<Radio />} label="Zorla açık (tarihe bakma)" />
                  <FormControlLabel value="ForceClosed" control={<Radio />} label="Zorla kapalı (tarihe bakma)" />
                </RadioGroup>
              </FormControl>
            )}
          />
          <Controller
            name="startDate"
            control={windowForm.control}
            render={({ field, fieldState }) => (
              <TextField
                {...field}
                fullWidth
                margin="dense"
                label="Başvuru başlangıcı"
                type="date"
                slotProps={{ inputLabel: { shrink: true } }}
                error={!!fieldState.error}
                helperText={fieldState.error?.message}
              />
            )}
          />
          <Controller
            name="endDate"
            control={windowForm.control}
            render={({ field, fieldState }) => (
              <TextField
                {...field}
                fullWidth
                margin="dense"
                label="Başvuru bitişi"
                type="date"
                slotProps={{ inputLabel: { shrink: true } }}
                error={!!fieldState.error}
                helperText={fieldState.error?.message}
              />
            )}
          />
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => {
              setWindowTarget(null)
              windowForm.reset(emptyClubApplicationWindowFormValues)
            }}
          >
            Vazgeç
          </Button>
          <Button
            variant="contained"
            disabled={windowMutation.isPending}
            onClick={windowForm.handleSubmit((values) => windowMutation.mutate(values))}
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>
```

Dosyanın `@mui/material` import'una şunları ekle (yoksa): `FormControl`, `FormControlLabel`, `FormLabel`, `Radio`, `RadioGroup`, `DialogContentText`.
Yeni import satırı:

```typescript
import {
  clubApplicationWindowFormSchema,
  emptyClubApplicationWindowFormValues,
  toWindowFormValues,
  toWindowPayload,
  type ClubApplicationWindowFormValues,
} from '../schemas/clubApplicationWindowForm'
```

- [ ] **Step 4: `ClubsPage`'de düğmeyi duruma bağla**

`arayuz/src/pages/ClubsPage.tsx`:

**(a)** Mevcut query'lerin yanına:

```tsx
  const windowQuery = useQuery({
    queryKey: ['club-application-window'],
    queryFn: async () => (await apiClient.get<ClubApplicationWindowDto>('/club-applications/window')).data,
  })
```

**(b)** "Topluluk Kurmak İstiyorum" düğmesini şununla değiştir:

```tsx
            <Tooltip
              title={
                windowQuery.data && !windowQuery.data.isOpen
                  ? windowQuery.data.startUtc
                    ? `Başvurular ${new Date(windowQuery.data.startUtc).toLocaleDateString('tr-TR')} tarihinde açılıyor.`
                    : 'Topluluk kurma başvuruları şu anda kapalı.'
                  : ''
              }
            >
              <span>
                <Button variant="outlined" disabled={windowQuery.data?.isOpen !== true} onClick={applyDialog.openDialog}>
                  Topluluk Kurmak İstiyorum
                </Button>
              </span>
            </Tooltip>
```

> `<span>` sarmalayıcı zorunlu: MUI `Tooltip` devre dışı bir düğmenin olaylarını okuyamaz.
> `isOpen !== true` kasıtlı: veri henüz yüklenmemişken (`undefined`) düğme **pasif** kalır — fail-closed'ın arayüz karşılığı.

Üç import düzeltmesi:

- **`useQuery` bu dosyada yok.** Satır 2'yi şununla değiştir:
  ```typescript
  import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
  ```
- `@mui/material` import'una `Tooltip` ekle.
- `api/types` import'una `ClubApplicationWindowDto` ekle.

- [ ] **Step 5: `MyApplicationsPage`'de takvimi göster**

`arayuz/src/pages/MyApplicationsPage.tsx` → `ClubApplicationsTab` fonksiyonu içinde:

**(a)** `myApplicationsQuery` tanımının altına:

```tsx
  const windowQuery = useQuery({
    queryKey: ['club-application-window'],
    queryFn: async () => (await apiClient.get<ClubApplicationWindowDto>('/club-applications/window')).data,
  })
```

**(b)** Fonksiyonun döndürdüğü JSX'te, hem boş durum hem liste yolunda görünmesi için, `if (!myApplicationsQuery.isLoading && items.length === 0)` kontrolünün **üstüne** şu bloğu tanımla ve iki dönüşte de en üste yerleştir:

```tsx
  const windowBanner = windowQuery.data ? (
    <Alert severity={windowQuery.data.isOpen ? 'success' : 'info'} sx={{ mb: 2 }}>
      {windowQuery.data.isOpen
        ? windowQuery.data.endUtc
          ? `Başvurular açık — son gün ${new Date(windowQuery.data.endUtc).toLocaleDateString('tr-TR')}.`
          : 'Topluluk kurma başvuruları şu anda açık.'
        : windowQuery.data.startUtc
          ? `Başvurular kapalı. ${new Date(windowQuery.data.startUtc).toLocaleDateString('tr-TR')} tarihinde açılacak.`
          : 'Topluluk kurma başvuruları şu anda kapalı.'}
    </Alert>
  ) : null
```

Boş durum dönüşünü şununla değiştir:

```tsx
  if (!myApplicationsQuery.isLoading && items.length === 0) {
    return (
      <>
        {windowBanner}
        <EmptyState
          icon={GroupsOutlinedIcon}
          title="Henüz bir topluluk kurma başvurunuz yok"
          description="Kulüpler sayfasındaki “Topluluk Kurmak İstiyorum” butonuyla başvurabilirsiniz."
        />
      </>
    )
  }
```

Ve liste dönüşünü şununla değiştir:

```tsx
  return (
    <>
      {windowBanner}
      <Stack spacing={2}>
```

…kapanışı da `</Stack></>` olacak şekilde düzelt.

`@mui/material` import'una `Alert` ekle; `api/types` import'una `ClubApplicationWindowDto` ekle.

- [ ] **Step 6: Build ve lint çalıştır**

```bash
cd arayuz && npm run build && npm run lint
```

Beklenen: **sıfır TypeScript hatası, sıfır lint hatası.**

- [ ] **Step 7: Uygulamayı elle doğrula**

Backend ve arayüzü çalıştır, sonra:

1. Yönetici olarak **Referans Verisi → Akademik Dönemler**'de güncel dönemin "Takvim" düğmesine bas. Diyalogda "Zorla açık" seçili olmalı (migration'ın veri adımı).
2. **Zorla kapalı** yap. Öğrenci hesabıyla Kulüpler sayfasına git → "Topluluk Kurmak İstiyorum" **pasif** olmalı, üzerine gelince sebep görünmeli.
3. Başvurularım → Topluluk Kurma sekmesinde "başvurular kapalı" bilgisi görünmeli.
4. Yönetici **Takvime uy** yapıp bugünü kapsayan bir aralık girsin → öğrenci tarafında düğme **aktif** olmalı ve başvuru gitmeli.
5. Aralığı geleceğe al → düğme pasifleşmeli, banner "… tarihinde açılacak" demeli.

- [ ] **Step 8: Commit**

```bash
git add arayuz/src
git commit -m "Faz 31 adim 5: takvim yonetimi ve ogrenci gorunurlugu arayuze baglandi (K-39)"
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

Beklenen: sıfır uyarı, tüm testler yeşil, `git diff --stat` yalnızca **File Structure** tablosundaki dosyaları göstermeli.

- [ ] **"Bitti sayılır" kontrolü** (`docs/PLAN-V6.md` §Faz 31)

| Koşul | Nasıl doğrulanır |
|---|---|
| Admin takvim tanımlayabiliyor | `SetClubApplicationWindowRequestValidator_*` (4 test) + elle doğrulama adım 4 |
| Aralık dışında başvuru 409 alıyor, mesaj sebebi söylüyor | `Submit_RespectsApplicationWindow` + `Messages.ClubApplicationsClosed` metni |
| "Zorla aç" tarih dışında, "zorla kapat" tarih içinde çalışıyor | `Submit_RespectsApplicationWindow` satır 1 ve 2 |
| Fail-closed: takvim tanımsızsa kapalı | `Submit_FollowScheduleWithoutDates_ReturnsConflict` + `GetWindowAsync_FollowSchedule_NoDates_IsClosed` |
| Kapalıyken yönetici karar verebiliyor | `Decide_WorksWhileWindowIsClosed` |
| **Arayüzün gösterdiği durum ile API'nin kararı beş senaryoda da aynı** | `Window_Endpoint_AgreesWithSubmitGuard` |
| Dağıtımda kesinti yok | Migration'ın `UPDATE … SET ClubApplicationOverride = 1 WHERE IsCurrent = 1` satırı (Task 1 Step 4) |

- [ ] **Faz commit'i**

```bash
git log --oneline master..HEAD
```

Beş adım commit'i görünmeli.

---

## Sonraki Faz

Faz 32 (Topluluk kategorisi) bu faza **bağlı değil.**
