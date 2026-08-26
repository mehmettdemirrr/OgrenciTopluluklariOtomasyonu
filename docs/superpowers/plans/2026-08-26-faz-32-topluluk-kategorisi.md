# Faz 32 — Topluluk Kategorisi Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Topluluklar kategori taşısın; yönetici kategori listesini yönetebilsin, kulüp ve kuruluş başvurusu formlarında kategori seçilebilsin, kulüp listeleri kategoriye göre filtrelenebilsin.

**Architecture:** `ClubCategory`, `Faculty`/`Department` ile **birebir aynı sınıf** referans verisidir (A-12/A-27): `reference.manage` izniyle CRUD, hard delete serbest ama kullanımdaysa 409. `Club.ClubCategoryId` ve `ClubApplication.ProposedCategoryId` **nullable** — mevcut kayıtlar kategorisiz kalabilir. Filtreleme SQL'de (A-50/Y-62). Kategori adı kulüp DTO'sunda taşındığı için yazma uçları kulüp cache'ini de düşürür (Y-45).

**Tech Stack:** .NET 8 · EF Core 8 / MSSQL · AutoMapper · FluentValidation · xUnit + Moq · React 18 + Vite + TypeScript + MUI + react-hook-form + zod

**Spec:** [docs/PLAN-V6.md](../../PLAN-V6.md) §Faz 32 · [docs/MIMARI.md](../../MIMARI.md) K-35, A-60

**Bağımlılık:** Yok. Faz 30/31 uygulanmamış olsa da tek başına çalışır.

---

## Global Constraints

Bu bölüm her task'ın gereksinimlerine **örtük olarak dahildir.** Değerler `docs/MIMARI.md`'den birebir alınmıştır.

- **Y-01** — Controller içinde `DbContext`, `IXxxDal`, LINQ sorgusu veya iş kuralı bulunamaz.
- **Y-03** — İş kuralı yalnızca Business'ta yaşar.
- **Y-09** — Entity HTTP gövdesinde yer alamaz; giriş/çıkış DTO.
- **Y-11 / A-16** — Liste uçları sayfalanır: `GetListPagedAsync` + `PagedResult<T>`, varsayılan 20, üst sınır 100.
- **Y-27** — Uçtan uca async, `CancellationToken` taşınır, `.ConfigureAwait(false)`.
- **Y-29** — Kullanıcı mesajı yalnızca `Business/Constants/Messages`.
- **Y-31** — Nullable uyarısı susturulamaz. **Uyarılar zaten hata.**
- **Y-32** — AutoMapper profilinde koşullu mantık, DB çağrısı, iş kuralı **yok**; Entity→Entity map yok. `AssertConfigurationIsValid` testi zorunlu.
- **Y-35** — İş kuralı arayüzde tekrar yazılmaz.
- **Y-45** — Cache'i etkileyen her yazma ucu ilgili anahtarları düşürür.
- **Y-58** — Anonim vitrin ucu kişisel veri döndürmez; `DTOs/Public/` ailesi ayrıdır.
- **Y-62 / A-50** — Arama ve filtre **sunucuda**; istemcide `.filter()` yok, `pageSize: 200` yok.
- **Y-64** — Sıralama olmadan `Skip`/`Take` yok; `Id` daima son kırıcı.
- **A-12 / A-27** — Referans verisi hard delete edilebilir; kullanımdaysa 409.
- **A-60** — Kategori alanları **nullable**; mevcut kulüpler geçersiz duruma düşmez.
- **Sessiz onaylar** — Adlar İngilizce, mesajlar/yorumlar Türkçe. URL çoğul + kebab-case, fiil yok. Migration adı `YYYYMMDD_AçıklayıcıAd`, **her PR'da en fazla bir migration.**

---

## Bu fazın üç tuzağı

Uygulamaya başlamadan oku. Üçü de sessizce yanlış çalışır, derleme hatası vermez.

| # | Tuzak | Nerede kapanıyor |
|---|---|---|
| 1 | **AutoMapper.** `ClubListItemDto` ve `ClubDetailDto` profille eşleniyor. `ClubCategoryName` alanının `Club` üzerinde kaynağı **yok** → `AssertConfigurationIsValid` kırılır (Y-32). Adı bir join gerektirir; AutoMapper'da DB çağrısı **yasak.** | Task 3: profilde `Ignore()`, adı manager doldurur |
| 2 | **Cache.** Kategori adı kulüp listesi DTO'sunda taşınıyor; `IClubService.GetListPagedAsync` `[CacheAspect(5)]` altında. Kategori yazma uçlarının `[CacheRemoveAspect]`'i yalnızca `"ReferenceDataManager."` derse **liste eski adı servis eder** (Y-45). | Task 2: üç önek birlikte |
| 3 | **Cache anahtarı.** `GetListPagedAsync`'e yeni parametre eklemek `CacheAspect`'in ürettiği anahtarı değiştirir; `CacheKeyPatternContractTests` bunu denetliyor. | Task 3: test çalıştırılır, kırılırsa **sözleşme** düzeltilir |

---

## File Structure

| Dosya | Sorumluluk | Task |
|---|---|---|
| `src/Entities/ClubCategory.cs` | **Yeni.** Referans varlığı (`Faculty` ikizi) | 1 |
| `src/DataAccess/Configurations/ClubCategoryConfiguration.cs` | **Yeni.** Ad unique, 200 karakter | 1 |
| `src/Entities/Club.cs` | `ClubCategoryId` (nullable) | 1 |
| `src/Entities/ClubApplication.cs` | `ProposedCategoryId` (nullable) | 1 |
| `src/DataAccess/Configurations/ClubConfiguration.cs` | FK `Restrict` | 1 |
| `src/DataAccess/Configurations/ClubApplicationConfiguration.cs` | FK `Restrict` | 1 |
| `src/DataAccess/AppDbContext.cs` | `DbSet<ClubCategory>` | 1 |
| `src/DataAccess/Migrations/…_Faz32_ToplulukKategorisi.cs` | **Üretilecek** | 1 |
| `src/Business/DTOs/Reference/ClubCategoryListItemDto.cs` | **Yeni** | 2 |
| `src/Business/DTOs/Reference/CreateClubCategoryRequestDto.cs` | **Yeni** | 2 |
| `src/Business/DTOs/Reference/UpdateClubCategoryRequestDto.cs` | **Yeni** | 2 |
| `src/Business/ValidationRules/CreateClubCategoryRequestValidator.cs` | **Yeni** | 2 |
| `src/Business/ValidationRules/UpdateClubCategoryRequestValidator.cs` | **Yeni** | 2 |
| `src/Business/Abstract/IReferenceDataService.cs` | Dört metot sözleşmesi | 2 |
| `src/Business/Concrete/ReferenceDataManager.cs` | Dört metot | 2 |
| `src/Business/Constants/Messages.cs` | Beş mesaj | 2 |
| `src/WebAPI/Controllers/ClubCategoriesController.cs` | **Yeni** | 2 |
| `src/Business/DTOs/Clubs/ClubListItemDto.cs` · `ClubDetailDto.cs` | Kategori alanları | 3 |
| `src/Business/DTOs/Clubs/CreateClubRequestDto.cs` · `UpdateClubRequestDto.cs` | `ClubCategoryId` | 3 |
| `src/Business/Mappings/ClubMappingProfile.cs` | `ClubCategoryName` → `Ignore()` | 3 |
| `src/Business/Abstract/IClubService.cs` | `categoryId` parametresi | 3 |
| `src/Business/Concrete/ClubManager.cs` | Filtre + kategori adı doldurma + yazma | 3 |
| `src/WebAPI/Controllers/ClubsController.cs` | `categoryId` query parametresi | 3 |
| `src/Business/DTOs/ClubApplications/SubmitClubApplicationRequestDto.cs` | `ProposedCategoryId` | 4 |
| `src/Business/DTOs/ClubApplications/ClubApplicationListItemDto.cs` | Kategori alanları | 4 |
| `src/Business/Concrete/ClubApplicationManager.cs` | Kategori doğrulama + onayda taşıma | 4 |
| `src/Business/DTOs/Public/PublicClubListItemDto.cs` · `PublicClubDetailDto.cs` | Kategori adı | 5 |
| `src/Business/Concrete/PublicContentManager.cs` | Kategori filtresi + adı | 5 |
| `src/Business/Abstract/IPublicContentService.cs` | `categoryId` parametresi | 5 |
| `src/WebAPI/Controllers/PublicContentController.cs` | `categoryId` query parametresi | 5 |
| `arayuz/src/api/types.ts` | Tipler ve alanlar | 6 |
| `arayuz/src/pages/ReferenceDataPage.tsx` | Yeni sekme | 6 |
| `arayuz/src/schemas/clubForm.ts` · `clubApplicationForm.ts` | `clubCategoryId` | 6 |
| `arayuz/src/pages/ClubsPage.tsx` · `ClubDetailPage.tsx` | Filtre + rozet | 6 |
| `arayuz/src/pages/public/PublicClubsPage.tsx` · `PublicClubDetailPage.tsx` | Filtre + rozet | 6 |

**Test dosyaları:** `tests/Business.Tests/ReferenceDataManagerTests.cs` (2), `tests/Business.Tests/MappingProfileTests.cs` (3), `tests/WebAPI.IntegrationTests/ClubSearchPagingTests.cs` (3), `tests/WebAPI.IntegrationTests/ReferenceDataEndpointTests.cs` (2), `tests/WebAPI.IntegrationTests/ClubApplicationFlowTests.cs` (4), `tests/Business.Tests/PublicContentManagerTests.cs` (5)

---

### Task 1: Şema — `ClubCategory` varlığı ve iki yabancı anahtar

**Files:**
- Create: `src/Entities/ClubCategory.cs`
- Create: `src/DataAccess/Configurations/ClubCategoryConfiguration.cs`
- Modify: `src/Entities/Club.cs`, `src/Entities/ClubApplication.cs`
- Modify: `src/DataAccess/Configurations/ClubConfiguration.cs`, `src/DataAccess/Configurations/ClubApplicationConfiguration.cs`
- Modify: `src/DataAccess/AppDbContext.cs`
- Create (üretilecek): `src/DataAccess/Migrations/<timestamp>_20260826_Faz32_ToplulukKategorisi.cs`
- Test: `tests/WebAPI.IntegrationTests/DomainConstraintTests.cs`

**Interfaces:**
- Consumes: — (ilk task)
- Produces:
  - `Entities.ClubCategory` — `int Id`, `required string Name`
  - `Entities.Club.ClubCategoryId` — `int?`
  - `Entities.ClubApplication.ProposedCategoryId` — `int?`
  - `DataAccess.AppDbContext.ClubCategories` — `DbSet<ClubCategory>`

- [ ] **Step 1: Failing test'i yaz**

`tests/WebAPI.IntegrationTests/DomainConstraintTests.cs` dosyasının sonundaki kapanış süslü parantezinden önce ekle:

```csharp
    [Fact(DisplayName = "A-60: aynı adla ikinci kategori DB seviyesinde reddedilir (unique index)")]
    public async Task ClubCategory_DuplicateName_IsRejectedByDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var name = $"Kategori {Guid.NewGuid():N}"[..20];
        db.ClubCategories.Add(new ClubCategory { Name = name });
        await db.SaveChangesAsync();

        db.ClubCategories.Add(new ClubCategory { Name = name });

        await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
    }

    [Fact(DisplayName = "A-60: kullanımdaki kategori silinemez (FK Restrict)")]
    public async Task ClubCategory_InUse_CannotBeDeleted()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (club, _, _) = await SeedClubStudentTermAsync(db, "cat-inuse");

        var category = new ClubCategory { Name = $"Kullanımda {Guid.NewGuid():N}"[..20] };
        db.ClubCategories.Add(category);
        await db.SaveChangesAsync();

        club.ClubCategoryId = category.Id;
        await db.SaveChangesAsync();

        db.ClubCategories.Remove(category);

        await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
    }

    [Fact(DisplayName = "A-60: kategori nullable — kategorisiz kulüp kaydedilebilir")]
    public async Task Club_WithoutCategory_IsAccepted()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (club, _, _) = await SeedClubStudentTermAsync(db, "cat-null");

        var reloaded = await db.Clubs.AsNoTracking().SingleAsync(c => c.Id == club.Id);
        Assert.Null(reloaded.ClubCategoryId);
    }
```

- [ ] **Step 2: Test'i çalıştır, derlenmediğini gör**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~ClubCategory_|FullyQualifiedName~Club_WithoutCategory"
```

Beklenen: **derleme hatası** — `ClubCategory` tipi ve `db.ClubCategories` yok.

- [ ] **Step 3: Varlığı oluştur**

`src/Entities/ClubCategory.cs`:

```csharp
using Core.Entities;

namespace Entities;

/// <summary>
/// docs/MIMARI.md · K-35/A-60: topluluk kategorisi. Faculty/Department ile aynı sınıf referans
/// verisi — hard delete serbest (A-12), kullanımdaysa FK Restrict 409 üretir.
/// </summary>
public sealed class ClubCategory : IEntity
{
    public int Id { get; set; }

    public required string Name { get; set; }
}
```

- [ ] **Step 4: EF konfigürasyonunu oluştur**

`src/DataAccess/Configurations/ClubCategoryConfiguration.cs`:

```csharp
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>docs/MIMARI.md · A-60: FacultyConfiguration ile birebir aynı biçim.</summary>
public sealed class ClubCategoryConfiguration : IEntityTypeConfiguration<ClubCategory>
{
    public void Configure(EntityTypeBuilder<ClubCategory> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(c => c.Name).IsUnique();
    }
}
```

- [ ] **Step 5: `DbSet`'i ekle**

`src/DataAccess/AppDbContext.cs` — `public DbSet<Club> Clubs => Set<Club>();` satırının **üstüne**:

```csharp
    public DbSet<ClubCategory> ClubCategories => Set<ClubCategory>();
```

- [ ] **Step 6: İki varlığa yabancı anahtar alanını ekle**

`src/Entities/Club.cs` — `LogoFileId` özelliğinin altına:

```csharp
    /// <summary>docs/MIMARI.md · K-35/A-60: kategori. Nullable — mevcut kulüpler kategorisiz kalabilir.</summary>
    public int? ClubCategoryId { get; set; }
```

`src/Entities/ClubApplication.cs` — `ProposedAdvisorId` özelliğinin altına:

```csharp
    /// <summary>docs/MIMARI.md · K-35/A-60: önerilen kategori. Nullable — zorunlu değil.</summary>
    public int? ProposedCategoryId { get; set; }
```

- [ ] **Step 7: FK davranışlarını yapılandır**

`src/DataAccess/Configurations/ClubConfiguration.cs` — mevcut `HasOne<...>` bloklarının yanına:

```csharp
        // A-60: kullanımdaki kategori silinemesin — Restrict, ReferentialIntegrityConflictException
        // üzerinden 409'a dönüşür (DeleteDepartmentAsync precedent'i).
        builder.HasOne<ClubCategory>()
            .WithMany()
            .HasForeignKey(c => c.ClubCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
```

`src/DataAccess/Configurations/ClubApplicationConfiguration.cs` — aynı gerekçeyle:

```csharp
        // A-60: başvurudaki kategori de kullanım sayılır; silme 409 verir.
        builder.HasOne<ClubCategory>()
            .WithMany()
            .HasForeignKey(a => a.ProposedCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
```

- [ ] **Step 8: Migration üret**

```bash
dotnet ef migrations add 20260826_Faz32_ToplulukKategorisi --project src/DataAccess --startup-project src/DataAccess
```

`Up()` içinde şunlar olmalı ve başka **hiçbir şey** olmamalı: `CreateTable("ClubCategories")`, `AddColumn<int>("ClubCategoryId", "Clubs", nullable: true)`, `AddColumn<int>("ProposedCategoryId", "ClubApplications", nullable: true)`, üç `CreateIndex` ve iki `AddForeignKey`. Fazlası varsa **dur.**

- [ ] **Step 9: Test'i çalıştır, geçtiğini gör**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~ClubCategory_|FullyQualifiedName~Club_WithoutCategory"
```

Beklenen: **3 passed**. Çıktıdaki test sayısını gözle doğrula (`--filter` yazım hatası "No test matches" deyip başarılı çıkar).

- [ ] **Step 10: Tüm test takımını çalıştır ve commit**

```bash
dotnet build && dotnet test
git add src/Entities/ClubCategory.cs src/Entities/Club.cs src/Entities/ClubApplication.cs src/DataAccess tests/WebAPI.IntegrationTests/DomainConstraintTests.cs
git commit -m "Faz 32 adim 1: ClubCategory semasi (K-35, A-60)"
```

---

### Task 2: Kategori CRUD — servis, uçlar, 409

**Files:**
- Create: `src/Business/DTOs/Reference/ClubCategoryListItemDto.cs`, `CreateClubCategoryRequestDto.cs`, `UpdateClubCategoryRequestDto.cs`
- Create: `src/Business/ValidationRules/CreateClubCategoryRequestValidator.cs`, `UpdateClubCategoryRequestValidator.cs`
- Modify: `src/Business/Abstract/IReferenceDataService.cs`, `src/Business/Concrete/ReferenceDataManager.cs`, `src/Business/Constants/Messages.cs`
- Create: `src/WebAPI/Controllers/ClubCategoriesController.cs`
- Test: `tests/Business.Tests/ReferenceDataManagerTests.cs`, `tests/WebAPI.IntegrationTests/ReferenceDataEndpointTests.cs`

**Interfaces:**
- Consumes: `Entities.ClubCategory` (Task 1)
- Produces:
  - `ClubCategoryListItemDto` — `int Id`, `required string Name`
  - `CreateClubCategoryRequestDto` / `UpdateClubCategoryRequestDto` — `string Name`
  - `IReferenceDataService.GetClubCategoriesPagedAsync(int, int, CancellationToken)` → `Task<IDataResult<PagedResult<ClubCategoryListItemDto>>>`
  - `.CreateClubCategoryAsync(CreateClubCategoryRequestDto, CancellationToken)` → `Task<IDataResult<ClubCategoryListItemDto>>`
  - `.UpdateClubCategoryAsync(int, UpdateClubCategoryRequestDto, CancellationToken)` → `Task<IResult>`
  - `.DeleteClubCategoryAsync(int, CancellationToken)` → `Task<IResult>`
  - `GET/POST/PUT/DELETE /api/club-categories`

- [ ] **Step 1: Failing birim testlerini yaz**

`tests/Business.Tests/ReferenceDataManagerTests.cs` dosyasının sonundaki kapanış süslü parantezinden önce ekle. Dosyadaki mevcut mock alan adlarını kullanır; `ClubCategory` repository mock'u **yeni** — sınıfın alan bloğuna da eklemen gerekecek (Step 4'te yapılacak).

```csharp
    [Fact(DisplayName = "A-60: aynı adla ikinci kategori üretilmez, mevcut satır döner")]
    public async Task CreateClubCategoryAsync_DuplicateName_ReturnsExisting()
    {
        var existing = new ClubCategory { Id = 7, Name = "Bilim ve Teknoloji" };
        _clubCategoryRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubCategory, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _sut.CreateClubCategoryAsync(new CreateClubCategoryRequestDto { Name = "  Bilim ve Teknoloji  " });

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Data!.Id);
        _clubCategoryRepository.Verify(r => r.AddAsync(It.IsAny<ClubCategory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "A-60: yeni ad ile kategori oluşturulur ve adı kırpılır")]
    public async Task CreateClubCategoryAsync_NewName_AddsTrimmed()
    {
        ClubCategory? captured = null;
        _clubCategoryRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubCategory, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClubCategory?)null);
        _clubCategoryRepository
            .Setup(r => r.AddAsync(It.IsAny<ClubCategory>(), It.IsAny<CancellationToken>()))
            .Callback((ClubCategory c, CancellationToken _) => captured = c)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateClubCategoryAsync(new CreateClubCategoryRequestDto { Name = "  Sanat  " });

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Equal("Sanat", captured!.Name);
    }

    [Fact(DisplayName = "A-60: kullanımdaki kategori silinemez — 409")]
    public async Task DeleteClubCategoryAsync_InUse_ReturnsConflict()
    {
        _clubCategoryRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubCategory, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubCategory { Id = 3, Name = "Spor" });
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ReferentialIntegrityConflictException("FK ihlali", new InvalidOperationException("inner")));

        var result = await _sut.DeleteClubCategoryAsync(3);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact(DisplayName = "A-60: kullanılmayan kategori silinir")]
    public async Task DeleteClubCategoryAsync_NotInUse_ReturnsSuccess()
    {
        _clubCategoryRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubCategory, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubCategory { Id = 3, Name = "Spor" });
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.DeleteClubCategoryAsync(3);

        Assert.True(result.IsSuccess);
        _clubCategoryRepository.Verify(r => r.Delete(It.IsAny<ClubCategory>()), Times.Once);
    }

    [Fact(DisplayName = "A-60: olmayan kategori silinmek istenirse 404")]
    public async Task DeleteClubCategoryAsync_NotFound_ReturnsNotFound()
    {
        _clubCategoryRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubCategory, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClubCategory?)null);

        var result = await _sut.DeleteClubCategoryAsync(99);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
```

> `ReferentialIntegrityConflictException(string message, Exception innerException)` — **`innerException` nullable değil**, `null` geçmek derlenmez. Yukarıdaki çağrı doğru biçimi kullanıyor.

- [ ] **Step 2: Test'i çalıştır, derlenmediğini gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~ClubCategoryAsync"
```

Beklenen: **derleme hatası** — `_clubCategoryRepository` alanı ve `CreateClubCategoryAsync` metodu yok.

- [ ] **Step 3: DTO'ları ve validator'ları oluştur**

`src/Business/DTOs/Reference/ClubCategoryListItemDto.cs`:

```csharp
namespace Business.DTOs.Reference;

public sealed class ClubCategoryListItemDto
{
    public int Id { get; set; }

    public required string Name { get; set; }
}
```

`src/Business/DTOs/Reference/CreateClubCategoryRequestDto.cs`:

```csharp
namespace Business.DTOs.Reference;

public sealed class CreateClubCategoryRequestDto
{
    public string Name { get; set; } = string.Empty;
}
```

`src/Business/DTOs/Reference/UpdateClubCategoryRequestDto.cs`:

```csharp
namespace Business.DTOs.Reference;

public sealed class UpdateClubCategoryRequestDto
{
    public string Name { get; set; } = string.Empty;
}
```

`src/Business/ValidationRules/CreateClubCategoryRequestValidator.cs`:

```csharp
using Business.DTOs.Reference;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class CreateClubCategoryRequestValidator : AbstractValidator<CreateClubCategoryRequestDto>
{
    public CreateClubCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
```

`src/Business/ValidationRules/UpdateClubCategoryRequestValidator.cs`:

```csharp
using Business.DTOs.Reference;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class UpdateClubCategoryRequestValidator : AbstractValidator<UpdateClubCategoryRequestDto>
{
    public UpdateClubCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
```

- [ ] **Step 4: Mesajları ekle**

`src/Business/Constants/Messages.cs` — fakülte/bölüm mesajlarının yanına:

```csharp
    // Faz 32 — Topluluk kategorisi (K-35, A-60)
    public const string ClubCategoryNotFound = "Topluluk kategorisi bulunamadı.";
    public const string ClubCategoryAlreadyExists = "Bu isimde bir topluluk kategorisi zaten var.";
    public const string ClubCategoryUpdated = "Topluluk kategorisi güncellendi.";
    public const string ClubCategoryDeleted = "Topluluk kategorisi silindi.";
    public const string ClubCategoryInUse = "Bu kategori kullanımda olduğu için silinemez.";
```

- [ ] **Step 5: Servis sözleşmesini genişlet**

`src/Business/Abstract/IReferenceDataService.cs` — akademik personel bildirimlerinin **üstüne**:

```csharp
    /// <summary>
    /// docs/MIMARI.md · K-35/A-60: kategori seçici hem kulüp formunda hem başvuru formunda kullanılır,
    /// bu yüzden okuma izni `clubs.read` (Member rolünde var) — `reference.manage` değil.
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.ClubsRead)]
    [CacheAspect(durationMinutes: 60)]
    Task<IDataResult<PagedResult<ClubCategoryListItemDto>>> GetClubCategoriesPagedAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Y-45: kategori adı `ClubListItemDto`'da taşınır — kulüp ve vitrin cache'i de düşer.
    /// Yalnızca "ReferenceDataManager." demek listeye eski adı servis ettirir.
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [ValidationAspect(typeof(CreateClubCategoryRequestValidator))]
    [CacheRemoveAspect("ReferenceDataManager.", "ClubManager.", "PublicContentManager.")]
    [TransactionAspect]
    Task<IDataResult<ClubCategoryListItemDto>> CreateClubCategoryAsync(
        CreateClubCategoryRequestDto request, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [ValidationAspect(typeof(UpdateClubCategoryRequestValidator))]
    [CacheRemoveAspect("ReferenceDataManager.", "ClubManager.", "PublicContentManager.")]
    [TransactionAspect]
    Task<IResult> UpdateClubCategoryAsync(
        int categoryId, UpdateClubCategoryRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>A-12: hard delete serbest; kullanımdaysa FK Restrict → 409 (DeleteDepartmentAsync precedent'i).</summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [CacheRemoveAspect("ReferenceDataManager.", "ClubManager.", "PublicContentManager.")]
    [TransactionAspect]
    Task<IResult> DeleteClubCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
```

- [ ] **Step 6: Manager'a implementasyonu ekle**

`src/Business/Concrete/ReferenceDataManager.cs`:

**(a)** Birincil kurucuya yeni bağımlılığı ekle — `IEntityRepository<Club> clubRepository,` satırının altına:

```csharp
    IEntityRepository<ClubCategory> clubCategoryRepository,
```

**(b)** `GetAcademicStaffPagedAsync` metodunun **üstüne** dört metodu ekle:

```csharp
    public async Task<IDataResult<PagedResult<ClubCategoryListItemDto>>> GetClubCategoriesPagedAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        // Y-64: ada göre artan, Id son kırıcı — repository sözleşmesi ThenBy(Id) uygular.
        var paged = await clubCategoryRepository
            .GetListPagedAsync(pageIndex, ClampPageSize(pageSize), c => true, c => c.Name, descending: false, cancellationToken)
            .ConfigureAwait(false);

        var items = paged.Items.Select(c => new ClubCategoryListItemDto { Id = c.Id, Name = c.Name }).ToList();
        return DataResult<PagedResult<ClubCategoryListItemDto>>.Success(
            new PagedResult<ClubCategoryListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    public async Task<IDataResult<ClubCategoryListItemDto>> CreateClubCategoryAsync(
        CreateClubCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();

        // CreateFacultyAsync precedent'i: çakışma hata değil, mevcut satırı döndürmek.
        var existing = await clubCategoryRepository.GetAsync(c => c.Name == name, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return DataResult<ClubCategoryListItemDto>.Success(
                new ClubCategoryListItemDto { Id = existing.Id, Name = existing.Name }, Messages.ClubCategoryAlreadyExists);
        }

        var category = new ClubCategory { Name = name };
        await clubCategoryRepository.AddAsync(category, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return DataResult<ClubCategoryListItemDto>.Success(
            new ClubCategoryListItemDto { Id = category.Id, Name = category.Name });
    }

    public async Task<IResult> UpdateClubCategoryAsync(
        int categoryId, UpdateClubCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        var category = await clubCategoryRepository.GetAsync(c => c.Id == categoryId, cancellationToken).ConfigureAwait(false);
        if (category is null)
        {
            return Result.NotFound(Messages.ClubCategoryNotFound);
        }

        var name = request.Name.Trim();
        var duplicate = await clubCategoryRepository
            .GetAsync(c => c.Id != categoryId && c.Name == name, cancellationToken)
            .ConfigureAwait(false);
        if (duplicate is not null)
        {
            return Result.Conflict(Messages.ClubCategoryAlreadyExists);
        }

        category.Name = name;
        clubCategoryRepository.Update(category);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.ClubCategoryUpdated);
    }

    public async Task<IResult> DeleteClubCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var category = await clubCategoryRepository.GetAsync(c => c.Id == categoryId, cancellationToken).ConfigureAwait(false);
        if (category is null)
        {
            return Result.NotFound(Messages.ClubCategoryNotFound);
        }

        clubCategoryRepository.Delete(category);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ReferentialIntegrityConflictException)
        {
            // A-60: kulüp veya başvuru bu kategoriye bağlıysa FK Restrict devreye girer.
            return Result.Conflict(Messages.ClubCategoryInUse);
        }

        return Result.Success(Messages.ClubCategoryDeleted);
    }
```

**(c)** Test sınıfına da mock'u ekle: `tests/Business.Tests/ReferenceDataManagerTests.cs` içinde alan bloğuna

```csharp
    private readonly Mock<IEntityRepository<ClubCategory>> _clubCategoryRepository = new();
```

ve `_sut` kurulumundaki argüman listesine `_clubCategoryRepository.Object,` — **kurucudaki sırayla aynı konuma.**

- [ ] **Step 7: Controller'ı oluştur**

`src/WebAPI/Controllers/ClubCategoriesController.cs`:

```csharp
using Business.Abstract;
using Business.DTOs.Reference;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

/// <summary>docs/MIMARI.md · K-35/A-60: FacultiesController ile aynı biçim.</summary>
[ApiController]
[Route("api/club-categories")]
public sealed class ClubCategoriesController(IReferenceDataService referenceDataService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetClubCategories(
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await referenceDataService.GetClubCategoriesPagedAsync(pageIndex, pageSize, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> CreateClubCategory(CreateClubCategoryRequestDto request, CancellationToken cancellationToken)
    {
        var result = await referenceDataService.CreateClubCategoryAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateClubCategory(int id, UpdateClubCategoryRequestDto request, CancellationToken cancellationToken)
    {
        var result = await referenceDataService.UpdateClubCategoryAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteClubCategory(int id, CancellationToken cancellationToken)
    {
        var result = await referenceDataService.DeleteClubCategoryAsync(id, cancellationToken);
        return result.ToActionResult();
    }
}
```

- [ ] **Step 8: Test'leri çalıştır, geçtiğini gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~ClubCategoryAsync"
```

Beklenen: **5 passed**.

- [ ] **Step 9: Tüm test takımını çalıştır ve commit**

```bash
dotnet build && dotnet test
git add src/Business src/WebAPI/Controllers/ClubCategoriesController.cs tests/Business.Tests/ReferenceDataManagerTests.cs
git commit -m "Faz 32 adim 2: kategori CRUD ve 409 (K-35, A-60)"
```

---

### Task 3: Kulüp tarafı — kategori alanı, filtre ve AutoMapper tuzağı

**Files:**
- Modify: `src/Business/DTOs/Clubs/ClubListItemDto.cs`, `ClubDetailDto.cs`, `CreateClubRequestDto.cs`, `UpdateClubRequestDto.cs`
- Modify: `src/Business/Mappings/ClubMappingProfile.cs`
- Modify: `src/Business/Abstract/IClubService.cs`, `src/Business/Concrete/ClubManager.cs`
- Modify: `src/WebAPI/Controllers/ClubsController.cs`
- Test: `tests/Business.Tests/MappingProfileTests.cs`, `tests/WebAPI.IntegrationTests/ClubSearchPagingTests.cs`

**Interfaces:**
- Consumes: `Entities.Club.ClubCategoryId` (Task 1), `ClubCategoryListItemDto` (Task 2)
- Produces:
  - `ClubListItemDto.ClubCategoryId` (`int?`), `.ClubCategoryName` (`string?`)
  - `ClubDetailDto.ClubCategoryId` (`int?`), `.ClubCategoryName` (`string?`)
  - `CreateClubRequestDto.ClubCategoryId` (`int?`), `UpdateClubRequestDto.ClubCategoryId` (`int?`)
  - `IClubService.GetListPagedAsync(int, int, string?, bool?, int?, CancellationToken)` — **`categoryId` son parametreden önce eklenir**

> **Tuzak 1 burada kapanıyor.** `ClubCategoryName`'in `Club` üzerinde kaynağı yok. Profilde `Ignore()` demezsen `MappingProfileTests` kırılır; `Ignore()` deyip manager'da doldurmazsan liste boş ad gösterir.

- [ ] **Step 1: Failing test'leri yaz**

`tests/Business.Tests/MappingProfileTests.cs` — mevcut testin altına:

```csharp
    [Fact(DisplayName = "Y-32: ClubCategoryName profilde açıkça yok sayılır (kaynağı Club üzerinde yok)")]
    public void ClubMappingProfile_IgnoresClubCategoryName()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<ClubMappingProfile>(), NullLoggerFactory.Instance);
        var mapper = configuration.CreateMapper();

        var club = new Club
        {
            Id = 1, Name = "Test", AdvisorId = 1, IsActive = true,
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), ClubCategoryId = 5,
        };

        var dto = mapper.Map<ClubListItemDto>(club);

        Assert.Equal(5, dto.ClubCategoryId);
        Assert.Null(dto.ClubCategoryName);
    }
```

`using Business.DTOs.Clubs;` ve `using Entities;` ekle.

`tests/WebAPI.IntegrationTests/ClubSearchPagingTests.cs`. Bu sınıfın kendi yardımcıları var: `_adminToken` (alan), `GetClubsAsync(queryString)` → `PagedClubs`, ve `ClubRow` iç sınıfı.

**(a)** Önce `ClubRow` iç sınıfına iki alan ekle (dosyanın sonunda, ~193. satır):

```csharp
        public int? ClubCategoryId { get; init; }

        public string? ClubCategoryName { get; init; }
```

**(b)** Sonra testi, dosyanın sonundaki kapanış süslü parantezinden önce ekle:

```csharp
    [Fact(DisplayName = "A-50/Y-62: kulüp listesi categoryId ile sunucu tarafında filtrelenir ve kategori adını taşır")]
    public async Task GetClubs_CategoryFilter_ReturnsOnlyMatchingClubsWithCategoryName()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var categoryName = $"Kategori {suffix}";
        var inCategoryName = $"{Prefix}Kategorili{suffix}";
        var outOfCategoryName = $"{Prefix}Kategorisiz{suffix}";
        int categoryId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var advisorId = await db.AcademicStaff.Select(s => s.Id).FirstAsync();

            var category = new ClubCategory { Name = categoryName };
            db.ClubCategories.Add(category);
            await db.SaveChangesAsync();
            categoryId = category.Id;

            db.Clubs.AddRange(
                new Club { Name = inCategoryName, AdvisorId = advisorId, IsActive = true, CreatedAtUtc = DateTime.UtcNow, ClubCategoryId = categoryId },
                new Club { Name = outOfCategoryName, AdvisorId = advisorId, IsActive = true, CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        var filtered = await GetClubsAsync($"pageIndex=0&pageSize=100&categoryId={categoryId}");

        Assert.Contains(filtered.Items, c => c.Name == inCategoryName);
        Assert.DoesNotContain(filtered.Items, c => c.Name == outOfCategoryName);
        Assert.Equal(categoryName, filtered.Items.Single(c => c.Name == inCategoryName).ClubCategoryName);

        // Filtre verilmezse ikisi de gelir — filtre "gevşemez", sadece uygulanmaz.
        var unfiltered = await GetClubsAsync($"pageIndex=0&pageSize=100&search={Uri.EscapeDataString(suffix)}");
        Assert.Contains(unfiltered.Items, c => c.Name == inCategoryName);
        Assert.Contains(unfiltered.Items, c => c.Name == outOfCategoryName);
    }
```

> `Prefix` bu sınıfta zaten tanımlı bir statik alan (satır 26) — kulüp adlarının bu test sınıfına ait olduğunu işaretler. `_factory` erişimi için `Microsoft.Extensions.DependencyInjection` ve `DataAccess` `using`'leri dosyada zaten var.

- [ ] **Step 2: Test'leri çalıştır, kırmızı olduğunu gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~ClubMappingProfile_IgnoresClubCategoryName"
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~GetClubs_CategoryFilter"
```

Beklenen: **derleme hatası** — `ClubListItemDto.ClubCategoryId` yok.

- [ ] **Step 3: DTO'lara alanları ekle**

`src/Business/DTOs/Clubs/ClubListItemDto.cs` — `LogoFileId` altına:

```csharp
    /// <summary>docs/MIMARI.md · K-35/A-60: kategori. Null = kategorisiz.</summary>
    public int? ClubCategoryId { get; set; }

    /// <summary>
    /// Kategori adı. Y-32: AutoMapper doldurmaz (Club üzerinde kaynağı yok, join gerekir) —
    /// profilde Ignore edilir, ClubManager kategori sözlüğünden doldurur.
    /// </summary>
    public string? ClubCategoryName { get; set; }
```

`src/Business/DTOs/Clubs/ClubDetailDto.cs` — `LogoFileId` altına **aynı iki özelliği ve aynı yorumları** ekle:

```csharp
    /// <summary>docs/MIMARI.md · K-35/A-60: kategori. Null = kategorisiz.</summary>
    public int? ClubCategoryId { get; set; }

    /// <summary>
    /// Kategori adı. Y-32: AutoMapper doldurmaz (Club üzerinde kaynağı yok, join gerekir) —
    /// profilde Ignore edilir, ClubManager kategori sözlüğünden doldurur.
    /// </summary>
    public string? ClubCategoryName { get; set; }
```

`src/Business/DTOs/Clubs/CreateClubRequestDto.cs` — `AdvisorId` altına:

```csharp
    /// <summary>docs/MIMARI.md · A-60: opsiyonel. Null = kategorisiz.</summary>
    public int? ClubCategoryId { get; set; }
```

`src/Business/DTOs/Clubs/UpdateClubRequestDto.cs` — `AdvisorId` altına **aynı özelliği** ekle:

```csharp
    /// <summary>docs/MIMARI.md · A-60: opsiyonel. Null = kategorisiz.</summary>
    public int? ClubCategoryId { get; set; }
```

> **Dikkat — `UpdateClubRequestDto`'da `AdvisorId` semantiği farklı:** orada `null` = "değiştirme". Kategori için `null` = "kategorisiz yap". İki alan aynı DTO'da farklı anlam taşıyor; bunu Task 3 Step 5'teki koda yorum olarak yazacağız.

- [ ] **Step 4: AutoMapper profilini düzelt**

`src/Business/Mappings/ClubMappingProfile.cs` — constructor gövdesini şununla değiştir:

```csharp
        // Y-32: ClubCategoryName'in Club üzerinde kaynağı yok (join gerekir, AutoMapper'da DB
        // çağrısı yasak). Açıkça Ignore ediyoruz; adı ClubManager dolduruyor.
        CreateMap<Club, ClubListItemDto>()
            .ForMember(d => d.ClubCategoryName, o => o.Ignore());

        CreateMap<Club, ClubDetailDto>()
            .ForMember(d => d.ClubCategoryName, o => o.Ignore());
```

- [ ] **Step 5: Servis sözleşmesini ve manager'ı güncelle**

`src/Business/Abstract/IClubService.cs` — `GetListPagedAsync` imzasını şununla değiştir:

```csharp
    /// <summary>A-50: `search` ad üzerinde SQL tarafında; `isActive` null ise aktif/pasif ayrımı yapılmaz; `categoryId` null ise kategori filtresi yok (K-35).</summary>
    Task<IDataResult<PagedResult<ClubListItemDto>>> GetListPagedAsync(
        int pageIndex, int pageSize, string? search = null, bool? isActive = null, int? categoryId = null,
        CancellationToken cancellationToken = default);
```

`src/Business/Concrete/ClubManager.cs`:

**(a)** Kurucuya kategori repository'sini ekle — `IEntityRepository<AcademicStaff> academicStaffRepository,` satırının altına:

```csharp
    IEntityRepository<ClubCategory> clubCategoryRepository,
```

**(b)** `GetListPagedAsync`'i şununla değiştir:

```csharp
    public async Task<IDataResult<PagedResult<ClubListItemDto>>> GetListPagedAsync(
        int pageIndex, int pageSize, string? search = null, bool? isActive = null, int? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var clampedPageSize = ClampPageSize(pageSize);
        var term = SearchTerm.Normalize(search);

        // Y-11/A-16: sayfalama kırpma bir iş kuralıdır, controller'da değil burada yapılır.
        // A-50/Y-62: arama VE kategori filtresi SQL'de — liste çekip bellekte ayıklamak yok.
        var paged = await clubRepository
            .GetListPagedAsync(
                pageIndex,
                clampedPageSize,
                c => (isActive == null || c.IsActive == isActive)
                    && (categoryId == null || c.ClubCategoryId == categoryId)
                    && (term.Length == 0 || c.Name.Contains(term)),
                c => c.Name,
                descending: false,
                cancellationToken)
            .ConfigureAwait(false);

        var items = mapper.Map<IReadOnlyList<ClubListItemDto>>(paged.Items);
        await FillCategoryNamesAsync(items, cancellationToken).ConfigureAwait(false);

        var result = new PagedResult<ClubListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize);
        return DataResult<PagedResult<ClubListItemDto>>.Success(result);
    }
```

**(c)** `GetByIdAsync`'in `return` satırını şununla değiştir:

```csharp
        var dto = mapper.Map<ClubDetailDto>(club);
        if (club.ClubCategoryId is { } categoryId)
        {
            var category = await clubCategoryRepository.GetAsync(c => c.Id == categoryId, cancellationToken).ConfigureAwait(false);
            dto.ClubCategoryName = category?.Name;
        }

        return DataResult<ClubDetailDto>.Success(dto);
```

**(d)** Sınıfın altına, `ClampPageSize` metodunun üstüne yardımcıyı ekle:

```csharp
    /// <summary>
    /// Y-10/Y-32: kategori adı AutoMapper ile taşınamaz (join gerekir). Tek toplu sorguyla
    /// doldurulur — satır başına sorgu N+1 üretirdi. EventManager.MapWithClubNamesAsync deseni.
    /// </summary>
    private async Task FillCategoryNamesAsync(IReadOnlyList<ClubListItemDto> items, CancellationToken cancellationToken)
    {
        var categoryIds = items
            .Where(i => i.ClubCategoryId is not null)
            .Select(i => i.ClubCategoryId!.Value)
            .Distinct()
            .ToList();

        if (categoryIds.Count == 0)
        {
            return;
        }

        var namesById = (await clubCategoryRepository.GetListAsync(c => categoryIds.Contains(c.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(c => c.Id, c => c.Name);

        foreach (var item in items)
        {
            if (item.ClubCategoryId is { } id)
            {
                item.ClubCategoryName = namesById.GetValueOrDefault(id);
            }
        }
    }
```

**(e)** `CreateAsync` içindeki `new Club { … }` başlatıcısına ekle:

```csharp
            ClubCategoryId = request.ClubCategoryId,
```

**(f)** `UpdateAsync` içinde, ad/danışman atamalarının yanına ekle:

```csharp
        // A-60: AdvisorId'den FARKLI semantik — orada null "değiştirme" demek (K-33 kısmi güncelleme),
        // burada null "kategorisiz yap" demektir. Kategori zorunlu olmadığı için temizlenebilmeli.
        club.ClubCategoryId = request.ClubCategoryId;
```

- [ ] **Step 6: Controller'a query parametresini ekle**

`src/WebAPI/Controllers/ClubsController.cs` — `GetList` action'ını şununla değiştir:

```csharp
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await clubService.GetListPagedAsync(pageIndex, pageSize, search, isActive, categoryId, cancellationToken);
        return result.ToActionResult();
    }
```

- [ ] **Step 7: Test'leri çalıştır, geçtiğini gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~MappingProfileTests"
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~GetClubs_CategoryFilter"
```

Beklenen: **2 passed** + **1 passed**.

- [ ] **Step 8: Cache anahtarı sözleşmesini doğrula (Tuzak 3)**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~CacheKeyPatternContractTests"
```

Beklenen: PASS. Kırılırsa: `GetListPagedAsync`'e eklenen parametre cache anahtarını değiştirdi. Testin mesajını oku ve **sözleşmeyi** güncelle (anahtar deseni), `[CacheRemoveAspect]` öneklerini değil — `"ClubManager."` öneki parametre sayısından bağımsızdır.

- [ ] **Step 9: Tüm test takımını çalıştır ve commit**

```bash
dotnet build && dotnet test
git add src/Business src/WebAPI/Controllers/ClubsController.cs tests/Business.Tests/MappingProfileTests.cs tests/WebAPI.IntegrationTests/ClubSearchPagingTests.cs
git commit -m "Faz 32 adim 3: kulup kategorisi ve sunucu tarafli filtre (K-35, A-60)"
```

---

### Task 4: Başvuru tarafı — önerilen kategori ve onayda taşınması

**Files:**
- Modify: `src/Business/DTOs/ClubApplications/SubmitClubApplicationRequestDto.cs`
- Modify: `src/Business/DTOs/ClubApplications/ClubApplicationListItemDto.cs`
- Modify: `src/Business/Concrete/ClubApplicationManager.cs`
- Modify: `src/Business/Constants/Messages.cs` *(Task 2'de eklenen `ClubCategoryNotFound` kullanılır — yeni mesaj yok)*
- Test: `tests/WebAPI.IntegrationTests/ClubApplicationFlowTests.cs`

**Interfaces:**
- Consumes: `ClubApplication.ProposedCategoryId` (Task 1), `Messages.ClubCategoryNotFound` (Task 2)
- Produces:
  - `SubmitClubApplicationRequestDto.ProposedCategoryId` (`int?`)
  - `ClubApplicationListItemDto.ProposedCategoryId` (`int?`), `.ProposedCategoryName` (`string?`)

**Kural:** Kategori verilmişse **var olmalı** (404 değilse başvuru yazılmaz). Onayda kategori doğan kulübe taşınır.

- [ ] **Step 1: Failing test'i yaz**

`tests/WebAPI.IntegrationTests/ClubApplicationFlowTests.cs` dosyasının sonundaki kapanış süslü parantezinden önce ekle:

```csharp
    [Fact(DisplayName = "K-35: başvurudaki kategori onayda doğan kulübe taşınır")]
    public async Task Approve_CarriesProposedCategoryToCreatedClub()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var proposedName = $"Kategorili Kulüp {suffix}";
        int categoryId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = new ClubCategory { Name = $"Başvuru Kategorisi {suffix}" };
            db.ClubCategories.Add(category);
            await db.SaveChangesAsync();
            categoryId = category.Id;
        }

        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);
        var submitResponse = await SendWithBearerAsync(HttpMethod.Post, "/api/club-applications", studentToken, new
        {
            ProposedName = proposedName,
            Description = "Kategori testi",
            Justification = "Kategori testi gerekçesi",
            ProposedAdvisorId = _proposedAdvisorId,
            ProposedCategoryId = categoryId,
        });
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        var pendingResponse = await SendWithBearerAsync(HttpMethod.Get, "/api/club-applications?pageIndex=0&pageSize=200", adminToken);
        var pendingBody = await pendingResponse.Content.ReadAsStringAsync();
        Assert.Contains(proposedName, pendingBody, StringComparison.Ordinal);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var application = await db.ClubApplications.SingleAsync(a => a.ProposedName == proposedName);
            Assert.Equal(categoryId, application.ProposedCategoryId);

            var decisionResponse = await SendWithBearerAsync(
                HttpMethod.Put, $"/api/club-applications/{application.Id}/decision", adminToken,
                new { Status = "Approved", ReviewNote = (string?)null });
            Assert.Equal(HttpStatusCode.OK, decisionResponse.StatusCode);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var createdClub = await db.Clubs.SingleAsync(c => c.Name == proposedName);
            Assert.Equal(categoryId, createdClub.ClubCategoryId);
        }
    }

    [Fact(DisplayName = "K-35: var olmayan kategori ile başvuru 404 alır ve kayıt yazılmaz")]
    public async Task Submit_UnknownCategory_ReturnsNotFound()
    {
        var proposedName = $"Hayalet Kategori {Guid.NewGuid():N}"[..30];
        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);

        var response = await SendWithBearerAsync(HttpMethod.Post, "/api/club-applications", studentToken, new
        {
            ProposedName = proposedName,
            Description = "Test",
            Justification = "Test gerekçesi",
            ProposedAdvisorId = _proposedAdvisorId,
            ProposedCategoryId = 999_999,
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.ClubApplications.AnyAsync(a => a.ProposedName == proposedName));
    }
```

> **Faz 31 uygulandıysa** bu testler pencereye takılabilir. `ClubApplicationFlowTests.InitializeAsync` Faz 31 Task 3 Step 5'te pencereyi `ForceOpen` yapıyor — o adım uygulanmışsa sorun yok.

- [ ] **Step 2: Test'i çalıştır, kırmızı olduğunu gör**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~Approve_CarriesProposedCategory|FullyQualifiedName~Submit_UnknownCategory"
```

Beklenen: **2 failed** — kategori alanı taşınmıyor, bilinmeyen kategori 200 alıyor.

- [ ] **Step 3: DTO'lara alanları ekle**

`src/Business/DTOs/ClubApplications/SubmitClubApplicationRequestDto.cs` — `ProposedAdvisorId` altına:

```csharp
    /// <summary>docs/MIMARI.md · K-35/A-60: önerilen kategori. Opsiyonel; verilirse var olmalı.</summary>
    public int? ProposedCategoryId { get; set; }
```

`src/Business/DTOs/ClubApplications/ClubApplicationListItemDto.cs` — `ProposedAdvisorDisplayName` altına:

```csharp
    public int? ProposedCategoryId { get; set; }

    /// <summary>İnceleme ekranı adı gösterir; kategori seçilmemişse null.</summary>
    public string? ProposedCategoryName { get; set; }
```

- [ ] **Step 4: Manager'ı güncelle**

`src/Business/Concrete/ClubApplicationManager.cs`:

**(a)** Kurucuya ekle — `IEntityRepository<AcademicStaff> academicStaffRepository,` satırının altına:

```csharp
    IEntityRepository<ClubCategory> clubCategoryRepository,
```

**(b)** `SubmitAsync` içinde, danışman kontrolünden **sonra**, ad çakışması kontrolünden **önce**:

```csharp
        // A-60: kategori opsiyonel, ama verilmişse var olmalı — yoksa yetim FK ile başvuru yazılır
        // ve onayda kulüp oluşturma patlar (Restrict). Hata, öğrenciye başvuru anında söylenmeli.
        if (request.ProposedCategoryId is { } proposedCategoryId)
        {
            var category = await clubCategoryRepository
                .GetAsync(c => c.Id == proposedCategoryId, cancellationToken)
                .ConfigureAwait(false);
            if (category is null)
            {
                return Result.NotFound(Messages.ClubCategoryNotFound);
            }
        }
```

**(c)** `SubmitAsync` içindeki `new ClubApplication { … }` başlatıcısına ekle:

```csharp
            ProposedCategoryId = request.ProposedCategoryId,
```

**(d)** `DecideAsync` içindeki `new Club { … }` başlatıcısına ekle:

```csharp
                        ClubCategoryId = application.ProposedCategoryId,
```

**(e)** `GetPendingAsync` ve `MapWithAdvisorNamesAsync` — her ikisinde de kategori adını doldur. `GetPendingAsync` içinde, `advisorNames` sözlüğünün altına:

```csharp
        var categoryIds = paged.Items.Where(a => a.ProposedCategoryId is not null).Select(a => a.ProposedCategoryId!.Value).Distinct().ToList();
        var categoryNames = categoryIds.Count == 0
            ? new Dictionary<int, string>()
            : (await clubCategoryRepository.GetListAsync(c => categoryIds.Contains(c.Id), cancellationToken).ConfigureAwait(false))
                .ToDictionary(c => c.Id, c => c.Name);
```

ve `new ClubApplicationListItemDto { … }` başlatıcısına:

```csharp
            ProposedCategoryId = a.ProposedCategoryId,
            ProposedCategoryName = a.ProposedCategoryId is { } cid ? categoryNames.GetValueOrDefault(cid) : null,
```

`MapWithAdvisorNamesAsync` içine **aynı iki bloğu** ekle (bu metot `List<ClubApplication> applications` üzerinde çalışır; `paged.Items` yerine `applications` kullan):

```csharp
        var categoryIds = applications.Where(a => a.ProposedCategoryId is not null).Select(a => a.ProposedCategoryId!.Value).Distinct().ToList();
        var categoryNames = categoryIds.Count == 0
            ? new Dictionary<int, string>()
            : (await clubCategoryRepository.GetListAsync(c => categoryIds.Contains(c.Id), cancellationToken).ConfigureAwait(false))
                .ToDictionary(c => c.Id, c => c.Name);
```

```csharp
            ProposedCategoryId = a.ProposedCategoryId,
            ProposedCategoryName = a.ProposedCategoryId is { } cid ? categoryNames.GetValueOrDefault(cid) : null,
```

**(f)** Faz 31 uygulandıysa `ClubApplicationManagerTests` kurucu argüman listesine `_clubCategoryRepository.Object` eklenmeli — **kurucudaki sırayla aynı konuma.** Test sınıfının alan bloğuna:

```csharp
    private readonly Mock<IEntityRepository<ClubCategory>> _clubCategoryRepository = new();
```

- [ ] **Step 5: Test'i çalıştır, geçtiğini gör**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~Approve_CarriesProposedCategory|FullyQualifiedName~Submit_UnknownCategory"
```

Beklenen: **2 passed**.

- [ ] **Step 6: Tüm test takımını çalıştır ve commit**

```bash
dotnet build && dotnet test
git add src/Business tests
git commit -m "Faz 32 adim 4: basvuruda onerilen kategori (K-35)"
```

---

### Task 5: Anonim vitrin — kategori filtresi ve adı

**Files:**
- Modify: `src/Business/DTOs/Public/PublicClubListItemDto.cs`, `PublicClubDetailDto.cs`
- Modify: `src/Business/Abstract/IPublicContentService.cs`, `src/Business/Concrete/PublicContentManager.cs`
- Modify: `src/WebAPI/Controllers/PublicContentController.cs`
- Test: `tests/Business.Tests/PublicContentManagerTests.cs`

**Interfaces:**
- Consumes: `Entities.ClubCategory` (Task 1), `Club.ClubCategoryId` (Task 1)
- Produces:
  - `PublicClubListItemDto.ClubCategoryName` (`string?`), `PublicClubDetailDto.ClubCategoryName` (`string?`)
  - `IPublicContentService.GetClubsAsync(int, int, string?, int?, CancellationToken)` — `categoryId` eklenir

> **Y-58 kontrolü:** kategori adı kişisel veri **değil** — kurumsal sınıflandırma. Anonim yüzeye çıkması güvenli. `ClubListItemDto` yeniden kullanılmaz; `DTOs/Public/` ailesi ayrı kalır (A-42).

- [ ] **Step 1: Failing test'i yaz**

`tests/Business.Tests/PublicContentManagerTests.cs` — `GetClubsAsync_OnlyActiveClubsReturned` testinin altına:

```csharp
    [Fact(DisplayName = "K-35: anonim vitrin categoryId ile filtrelenir ve kategori adını taşır")]
    public async Task GetClubsAsync_CategoryFilter_ReturnsOnlyMatchingWithName()
    {
        var inCategory = new Club { Id = 1, Name = "Kategorili", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow, ClubCategoryId = 4 };
        var otherCategory = new Club { Id = 2, Name = "Başka Kategori", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow, ClubCategoryId = 9 };
        var noCategory = new Club { Id = 3, Name = "Kategorisiz", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow };
        SetupPagedFilter<Club, string>(_clubRepository, [inCategory, otherCategory, noCategory]);

        _clubCategoryRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubCategory, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ClubCategory { Id = 4, Name = "Bilim" }]);

        var result = await _sut.GetClubsAsync(0, 20, null, 4);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Data!.Items);
        Assert.Equal("Kategorili", item.Name);
        Assert.Equal("Bilim", item.ClubCategoryName);
    }

    [Fact(DisplayName = "K-35: categoryId verilmezse tüm aktif kulüpler döner (filtre gevşemez, sadece uygulanmaz)")]
    public async Task GetClubsAsync_NoCategoryFilter_ReturnsAllActive()
    {
        var withCategory = new Club { Id = 1, Name = "Kategorili", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow, ClubCategoryId = 4 };
        var withoutCategory = new Club { Id = 2, Name = "Kategorisiz", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow };
        var inactive = new Club { Id = 3, Name = "Pasif", AdvisorId = 1, IsActive = false, CreatedAtUtc = FixedNow, ClubCategoryId = 4 };
        SetupPagedFilter<Club, string>(_clubRepository, [withCategory, withoutCategory, inactive]);

        _clubCategoryRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubCategory, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ClubCategory { Id = 4, Name = "Bilim" }]);

        var result = await _sut.GetClubsAsync(0, 20);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Items.Count);
        Assert.DoesNotContain(result.Data.Items, i => i.Name == "Pasif");
    }
```

Test sınıfının alan bloğuna ekle:

```csharp
    private readonly Mock<IEntityRepository<ClubCategory>> _clubCategoryRepository = new();
```

ve `_sut` kurulumunu şununla değiştir:

```csharp
        _sut = new PublicContentManager(
            _clubRepository.Object, _eventRepository.Object, _announcementRepository.Object,
            _clubCategoryRepository.Object, _clock.Object);
```

- [ ] **Step 2: Test'i çalıştır, derlenmediğini gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~GetClubsAsync_CategoryFilter|FullyQualifiedName~GetClubsAsync_NoCategoryFilter"
```

Beklenen: **derleme hatası** — kurucu 5 argüman almıyor, `GetClubsAsync` 4 parametre almıyor.

- [ ] **Step 3: Vitrin DTO'larına kategori adını ekle**

`src/Business/DTOs/Public/PublicClubListItemDto.cs` — `LogoFileId` altına:

```csharp
    /// <summary>
    /// docs/MIMARI.md · K-35: kategori adı. Y-58 kontrolü: kurumsal sınıflandırma, kişisel veri değil.
    /// Kimlik (`ClubCategoryId`) taşınmaz — vitrinin ihtiyacı yalnızca görünen ad.
    /// </summary>
    public string? ClubCategoryName { get; set; }
```

`src/Business/DTOs/Public/PublicClubDetailDto.cs` — **aynı özelliği ve yorumu** ekle:

```csharp
    /// <summary>
    /// docs/MIMARI.md · K-35: kategori adı. Y-58 kontrolü: kurumsal sınıflandırma, kişisel veri değil.
    /// Kimlik (`ClubCategoryId`) taşınmaz — vitrinin ihtiyacı yalnızca görünen ad.
    /// </summary>
    public string? ClubCategoryName { get; set; }
```

- [ ] **Step 4: Servis sözleşmesini ve manager'ı güncelle**

`src/Business/Abstract/IPublicContentService.cs` — `GetClubsAsync` imzasını şununla değiştir:

```csharp
    Task<IDataResult<PagedResult<PublicClubListItemDto>>> GetClubsAsync(
        int pageIndex, int pageSize, string? search = null, int? categoryId = null, CancellationToken cancellationToken = default);
```

`src/Business/Concrete/PublicContentManager.cs`:

**(a)** Kurucuya ekle — `IEntityRepository<Announcement> announcementRepository,` satırının altına:

```csharp
    IEntityRepository<ClubCategory> clubCategoryRepository,
```

**(b)** `GetClubsAsync`'i şununla değiştir:

```csharp
    public async Task<IDataResult<PagedResult<PublicClubListItemDto>>> GetClubsAsync(
        int pageIndex, int pageSize, string? search = null, int? categoryId = null, CancellationToken cancellationToken = default)
    {
        var term = SearchTerm.Normalize(search);

        // A-50: arama ve kategori filtresi SQL'de. IsActive filtresi kodda sabit kalır (Y-58) —
        // ne search ne categoryId onu gevşetebilir.
        var paged = await clubRepository
            .GetListPagedAsync(
                pageIndex,
                ClampPageSize(pageSize),
                c => c.IsActive
                    && (categoryId == null || c.ClubCategoryId == categoryId)
                    && (term.Length == 0 || c.Name.Contains(term)),
                c => c.Name,
                descending: false,
                cancellationToken)
            .ConfigureAwait(false);

        var categoryNames = await GetCategoryNamesAsync(
            paged.Items.Where(c => c.ClubCategoryId is not null).Select(c => c.ClubCategoryId!.Value),
            cancellationToken).ConfigureAwait(false);

        var items = paged.Items
            .Select(c => new PublicClubListItemDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                LogoFileId = c.LogoFileId,
                ClubCategoryName = c.ClubCategoryId is { } id ? categoryNames.GetValueOrDefault(id) : null,
            })
            .ToList();

        return DataResult<PagedResult<PublicClubListItemDto>>.Success(
            new PagedResult<PublicClubListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }
```

**(c)** `GetClubByIdAsync`'in `return` bloğuna kategori adını ekle:

```csharp
        var categoryNames = await GetCategoryNamesAsync(
            club.ClubCategoryId is { } cid ? [cid] : [],
            cancellationToken).ConfigureAwait(false);

        return DataResult<PublicClubDetailDto>.Success(new PublicClubDetailDto
        {
            Id = club.Id,
            Name = club.Name,
            Description = club.Description,
            LogoFileId = club.LogoFileId,
            ClubCategoryName = club.ClubCategoryId is { } id ? categoryNames.GetValueOrDefault(id) : null,
        });
```

**(d)** Sınıfın altına, `ClampPageSize`'ın üstüne:

```csharp
    /// <summary>Y-10: tek toplu sorgu — satır başına sorgu N+1 üretirdi.</summary>
    private async Task<Dictionary<int, string>> GetCategoryNamesAsync(IEnumerable<int> categoryIds, CancellationToken cancellationToken)
    {
        var ids = categoryIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return (await clubCategoryRepository.GetListAsync(c => ids.Contains(c.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(c => c.Id, c => c.Name);
    }
```

- [ ] **Step 5: Controller'a query parametresini ekle**

`src/WebAPI/Controllers/PublicContentController.cs` — `GetClubs` action'ını şununla değiştir:

```csharp
    [HttpGet("clubs")]
    public async Task<IActionResult> GetClubs(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await publicContentService.GetClubsAsync(pageIndex, pageSize, search, categoryId, cancellationToken);
        return result.ToActionResult();
    }
```

- [ ] **Step 6: Test'leri çalıştır, geçtiğini gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~PublicContentManagerTests"
```

Beklenen: mevcut testler + 2 yeni test yeşil.

- [ ] **Step 7: Anonim yüzey sızıntı testini çalıştır**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~PublicSurfaceLeakTests"
```

Beklenen: PASS. Kategori adı kişisel veri olmadığı için Y-58 ihlali yok; test kırılırsa bir yerden `ClubListItemDto` sızmış demektir.

- [ ] **Step 8: Tüm test takımını çalıştır ve commit**

```bash
dotnet build && dotnet test
git add src/Business src/WebAPI/Controllers/PublicContentController.cs tests/Business.Tests/PublicContentManagerTests.cs
git commit -m "Faz 32 adim 5: vitrinde kategori filtresi (K-35, A-42)"
```

---

### Task 6: Arayüz — yönetim sekmesi, form alanları ve filtreler

**Files:**
- Modify: `arayuz/src/api/types.ts`
- Modify: `arayuz/src/pages/ReferenceDataPage.tsx`
- Modify: `arayuz/src/schemas/clubForm.ts`, `arayuz/src/schemas/clubApplicationForm.ts`
- Modify: `arayuz/src/pages/ClubsPage.tsx`, `arayuz/src/pages/ClubDetailPage.tsx`
- Modify: `arayuz/src/pages/public/PublicClubsPage.tsx`, `arayuz/src/pages/public/PublicClubDetailPage.tsx`

**Interfaces:**
- Consumes: `/api/club-categories` (Task 2), `categoryId` parametreleri (Task 3, 5), DTO alanları (Task 3, 4, 5)
- Produces: `arayuz/src/api/types.ts` → `ClubCategoryListItemDto`

- [ ] **Step 1: TS tiplerini ekle**

`arayuz/src/api/types.ts`:

**(a)** `FacultyListItemDto` arayüzünün yanına:

```typescript
// src/Business/DTOs/Reference/ClubCategoryListItemDto.cs
export interface ClubCategoryListItemDto {
  id: number
  name: string
}
```

**(b)** `ClubListItemDto` ve `ClubDetailDto` arayüzlerinin **her ikisine** ekle:

```typescript
  clubCategoryId: number | null
  clubCategoryName: string | null
```

**(c)** `ClubApplicationListItemDto` arayüzüne ekle:

```typescript
  proposedCategoryId: number | null
  proposedCategoryName: string | null
```

**(d)** `PublicClubListItemDto` ve `PublicClubDetailDto` arayüzlerinin **her ikisine** ekle:

```typescript
  clubCategoryName: string | null
```

- [ ] **Step 2: Referans Verisi sayfasına sekmeyi ekle**

`arayuz/src/pages/ReferenceDataPage.tsx`:

**(a)** `Tabs` bloğuna, "Akademik Dönemler"den **sonra**:

```tsx
        {/* K-35: topluluk kategorisi — Faculty/Department ile aynı sınıf referans verisi (A-60). */}
        <Tab label="Topluluk Kategorileri" />
```

**(b)** Sekme gövdelerini şu sıraya getir (`AcademicStaffTab` indeksi 2'den 3'e kayar):

```tsx
      {tab === 0 && <FacultiesTab />}
      {tab === 1 && <TermsTab />}
      {tab === 2 && <ClubCategoriesTab />}
      {tab === 3 && <AcademicStaffTab />}
```

**(c)** Dosyanın sonuna yeni bileşeni ekle:

```tsx
function ClubCategoriesTab() {
  const queryClient = useQueryClient()
  const notify = useNotifier()
  const dialog = useFormDialog()
  const [editTarget, setEditTarget] = useState<ClubCategoryListItemDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<ClubCategoryListItemDto | null>(null)

  const createForm = useForm<NameFormValues>({ resolver: zodResolver(nameFormSchema), defaultValues: emptyNameFormValues })
  const editForm = useForm<NameFormValues>({ resolver: zodResolver(nameFormSchema), defaultValues: emptyNameFormValues })

  const { paginationModel, setPaginationModel, query: categoriesQuery } = usePagedQuery({
    queryKey: ['club-categories'],
    queryFn: async (pageIndex, pageSize) =>
      (await apiClient.get<PagedResult<ClubCategoryListItemDto>>('/club-categories', { params: { pageIndex, pageSize } })).data,
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['club-categories'] })
    // Y-45'in arayüz karşılığı: kategori adı kulüp listelerinde de görünüyor.
    queryClient.invalidateQueries({ queryKey: ['clubs'] })
  }

  const createMutation = useMutation({
    mutationFn: async (values: NameFormValues) =>
      (await apiClient.post<ClubCategoryListItemDto>('/club-categories', { name: values.name })).data,
    onSuccess: (_data, values) => {
      notify({ message: `"${values.name}" kaydedildi (zaten varsa mevcut satır kullanıldı).`, severity: 'success' })
      dialog.closeDialog()
      createForm.reset(emptyNameFormValues)
      invalidate()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Kategori eklenemedi.'), severity: 'error' }),
  })

  const updateMutation = useMutation({
    mutationFn: async (values: NameFormValues) => {
      if (!editTarget) return
      await apiClient.put(`/club-categories/${editTarget.id}`, { name: values.name })
    },
    onSuccess: () => {
      notify({ message: 'Kategori güncellendi.', severity: 'success' })
      setEditTarget(null)
      invalidate()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Kategori güncellenemedi.'), severity: 'error' }),
  })

  const deleteMutation = useMutation({
    mutationFn: async (id: number) => {
      await apiClient.delete(`/club-categories/${id}`)
    },
    onSuccess: () => {
      notify({ message: 'Kategori silindi.', severity: 'success' })
      setDeleteTarget(null)
      invalidate()
    },
    onError: (error) => {
      // A-60: kullanımdaki kategori 409 döner; mesajı API veriyor (Y-35).
      notify({ message: extractErrorMessage(error, 'Kategori silinemedi.'), severity: 'error' })
      setDeleteTarget(null)
    },
  })

  const columns: GridColDef<ClubCategoryListItemDto>[] = [
    { field: 'name', headerName: 'Kategori', flex: 1, minWidth: 200 },
    {
      field: 'actions',
      headerName: '',
      width: 120,
      sortable: false,
      filterable: false,
      renderCell: (params) => (
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <IconButton
            size="small"
            onClick={() => {
              setEditTarget(params.row)
              editForm.reset({ name: params.row.name })
            }}
          >
            <EditOutlinedIcon fontSize="small" />
          </IconButton>
          <IconButton size="small" color="error" onClick={() => setDeleteTarget(params.row)}>
            <DeleteOutlineIcon fontSize="small" />
          </IconButton>
        </Stack>
      ),
    },
  ]

  return (
    <>
      <Box sx={{ mb: 2 }}>
        <Button variant="contained" onClick={dialog.openDialog}>
          Yeni Kategori
        </Button>
      </Box>

      <DataTable
        rows={categoriesQuery.data?.items ?? []}
        columns={columns}
        loading={categoriesQuery.isFetching}
        paginationMode="server"
        rowCount={categoriesQuery.data?.totalCount ?? 0}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        pageSizeOptions={[10, 20, 50]}
        emptyTitle="Henüz kategori yok"
        emptyDescription="Toplulukları sınıflandırmak için bir kategori ekleyin."
      />

      <Dialog
        open={dialog.open}
        onClose={() => {
          dialog.closeDialog()
          createForm.reset(emptyNameFormValues)
        }}
        fullWidth
        maxWidth="xs"
      >
        <DialogTitle>Yeni Kategori</DialogTitle>
        <DialogContent>
          <NameFormField control={createForm.control} label="Kategori adı" />
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => {
              dialog.closeDialog()
              createForm.reset(emptyNameFormValues)
            }}
          >
            Vazgeç
          </Button>
          <Button
            variant="contained"
            disabled={createMutation.isPending}
            onClick={createForm.handleSubmit((values) => createMutation.mutate(values))}
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={editTarget !== null} onClose={() => setEditTarget(null)} fullWidth maxWidth="xs">
        <DialogTitle>Kategoriyi Düzenle</DialogTitle>
        <DialogContent>
          <NameFormField control={editForm.control} label="Kategori adı" />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditTarget(null)}>Vazgeç</Button>
          <Button
            variant="contained"
            disabled={updateMutation.isPending}
            onClick={editForm.handleSubmit((values) => updateMutation.mutate(values))}
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Kategoriyi sil"
        description={
          deleteTarget
            ? `"${deleteTarget.name}" kategorisini silmek istediğinize emin misiniz? Kullanımdaysa silinemez.`
            : undefined
        }
        confirmLabel="Sil"
        destructive
        loading={deleteMutation.isPending}
        onConfirm={() => deleteTarget && deleteMutation.mutate(deleteTarget.id)}
        onCancel={() => setDeleteTarget(null)}
      />
    </>
  )
}
```

`api/types` import'una `ClubCategoryListItemDto` ekle. Sayfa açıklamasını da güncelle:

```tsx
      <PageHeader title="Referans Verisi" description="Fakülte, bölüm, akademik dönem, topluluk kategorisi ve akademik personel verilerini yönetin." />
```

- [ ] **Step 3: Form şemalarına kategori alanını ekle**

`arayuz/src/schemas/clubForm.ts` — `clubFormSchema`'ya:

```typescript
  // K-35: 0 = kategorisiz. API'ye null gider (A-60: kategori opsiyonel).
  clubCategoryId: z.number().int(),
```

`emptyClubFormValues` ve `emptyCreateClubFormValues`'a `clubCategoryId: 0` ekle. Dosyanın sonuna payload yardımcısı ekle:

```typescript
export function toClubCategoryPayload(clubCategoryId: number): number | null {
  return clubCategoryId > 0 ? clubCategoryId : null
}
```

`arayuz/src/schemas/clubApplicationForm.ts` — şemaya:

```typescript
  // K-35: 0 = kategori seçilmedi; API'ye null gider.
  proposedCategoryId: z.number().int(),
```

`emptyClubApplicationFormValues`'a `proposedCategoryId: 0` ekle.

- [ ] **Step 4: `ClubsPage`'e seçici ve filtre ekle**

`arayuz/src/pages/ClubsPage.tsx`:

**(a)** Kategori listesini çek (`useQuery` import'unu Faz 31'de eklemediysen ekle):

```tsx
  const categoriesQuery = useQuery({
    queryKey: ['club-categories'],
    queryFn: async () =>
      (await apiClient.get<PagedResult<ClubCategoryListItemDto>>('/club-categories', { params: { pageIndex: 0, pageSize: 100 } })).data,
  })
```

**(b)** Filtre durumu ve liste sorgusuna parametre:

```tsx
  const [categoryFilter, setCategoryFilter] = useState<number>(0)
```

Kulüp listesi sorgusunun `queryKey`'ine `categoryFilter`'ı, `params`'ına `categoryId: categoryFilter > 0 ? categoryFilter : undefined` ekle.
**`queryKey`'e eklemek zorunlu** — eklenmezse TanStack Query eski sonucu önbellekten servis eder ve filtre çalışmıyormuş gibi görünür.

**(c)** Arama alanının yanına filtre seçici:

```tsx
        <TextField
          select
          size="small"
          label="Kategori"
          value={categoryFilter}
          onChange={(event) => setCategoryFilter(Number(event.target.value))}
          sx={{ minWidth: 200 }}
        >
          <MenuItem value={0}>Tümü</MenuItem>
          {(categoriesQuery.data?.items ?? []).map((category) => (
            <MenuItem key={category.id} value={category.id}>
              {category.name}
            </MenuItem>
          ))}
        </TextField>
```

**(d)** Kulüp oluşturma/düzenleme diyaloglarına ve topluluk kurma diyaloğuna aynı biçimde bir `Controller` + `TextField select` ekle (`clubCategoryId` / `proposedCategoryId` alanı, `MenuItem value={0}` = "Kategorisiz").

**(e)** Kulüp kartlarında/listesinde kategori adını rozet olarak göster:

```tsx
{club.clubCategoryName && <Chip size="small" variant="outlined" label={club.clubCategoryName} />}
```

**(f)** Mutasyon gövdelerinde payload'a ekle: kulüp oluştur/düzenle → `clubCategoryId: toClubCategoryPayload(values.clubCategoryId)`, topluluk kurma → `proposedCategoryId: values.proposedCategoryId > 0 ? values.proposedCategoryId : null`.

`@mui/material` import'una `MenuItem` ve `Chip` ekle (yoksa); `api/types` import'una `ClubCategoryListItemDto` ekle.

- [ ] **Step 5: Detay sayfalarına ve vitrine rozeti/filtreyi ekle**

**(a)** `arayuz/src/pages/ClubDetailPage.tsx` — kulüp adının yanına:

```tsx
{clubQuery.data?.clubCategoryName && <Chip size="small" variant="outlined" label={clubQuery.data.clubCategoryName} />}
```

**(b)** `arayuz/src/pages/public/PublicClubDetailPage.tsx` — aynı rozeti ekle (alan adı yine `clubCategoryName`).

**(c)** `arayuz/src/pages/public/PublicClubsPage.tsx` — Step 4(a)(b)(c)(e)'nin **aynısını** uygula: kategori listesini `/club-categories` yerine **doğrudan çekemezsin** (uç `clubs.read` istiyor, sayfa anonim). Bunun yerine **filtreyi vitrinde kategori adı rozetiyle sınırla** — anonim sayfaya kategori filtresi eklemek yeni bir anonim uç açmayı gerektirir ve A-42'yi genişletir. Yalnızca rozet göster:

```tsx
{club.clubCategoryName && <Chip size="small" variant="outlined" label={club.clubCategoryName} />}
```

> **Bilinçli kısıt:** anonim vitrinde kategori **filtresi** yok, yalnızca **rozet** var. Filtre için `/api/public/club-categories` açmak gerekirdi; A-42 anonim yüzeyi dar tutuyor ve bu faz onu genişletmiyor. Backend `categoryId` parametresini destekliyor (Task 5) — filtre sonradan istenirse yalnızca anonim uç eklenir.

- [ ] **Step 6: Build ve lint çalıştır**

```bash
cd arayuz && npm run build && npm run lint
```

Beklenen: **sıfır TypeScript hatası, sıfır lint hatası.**

- [ ] **Step 7: Uygulamayı elle doğrula**

1. Yönetici → **Referans Verisi → Topluluk Kategorileri**: kategori ekle, adını değiştir, sil.
2. Bir kulübe kategori ata → **Kulüpler** listesinde rozet görünmeli.
3. Kategori filtresini seç → yalnızca o kategorinin kulüpleri gelmeli.
4. Kategorinin adını değiştir → kulüp listesi **anında** yeni adı göstermeli (Y-45 cache düşüşü).
5. Kullanımdaki kategoriyi silmeye çalış → **409** ve "kullanımda olduğu için silinemez" mesajı.
6. Öğrenci → topluluk kurma formunda kategori seçilebilmeli; onaydan sonra doğan kulüp o kategoriyi taşımalı.
7. Çıkış yap → anonim vitrinde kulüp kartlarında kategori rozeti görünmeli.

- [ ] **Step 8: Commit**

```bash
git add arayuz/src
git commit -m "Faz 32 adim 6: kategori yonetimi ve filtreler arayuze baglandi (K-35)"
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

- [ ] **"Bitti sayılır" kontrolü** (`docs/PLAN-V6.md` §Faz 32)

| Koşul | Nasıl doğrulanır |
|---|---|
| Kategori tanımlanıp seçilebiliyor | `CreateClubCategoryAsync_*` (2 test) + elle doğrulama 1-2 |
| Kulüp ve başvuru formunda seçilebiliyor | `Approve_CarriesProposedCategoryToCreatedClub` + elle doğrulama 6 |
| Liste kategoriye göre filtreleniyor (sunucuda) | `GetClubs_CategoryFilter_*` + `GetClubsAsync_CategoryFilter_*` |
| Kullanımdaki kategori silinemiyor | `DeleteClubCategoryAsync_InUse_ReturnsConflict` + `ClubCategory_InUse_CannotBeDeleted` + elle doğrulama 5 |
| **Kategori adı değişince liste anında yeni adı gösteriyor** | `[CacheRemoveAspect]` üç öneki + elle doğrulama 4 |
| AutoMapper sözleşmesi bozulmadı | `MappingProfileTests` (2 test) |

- [ ] **Faz commit'i**

```bash
git log --oneline master..HEAD
```

Altı adım commit'i görünmeli.

---

## Sonraki Faz

Faz 33 (Topluluk kuruluş evrakları) bu faza **bağlı değil**, ama aynı `ClubApplication` akışına dokunuyor — **Faz 32'den sonra uygulanması önerilir** ki başvuru DTO'su tek seferde multipart'a taşınsın.
