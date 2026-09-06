# Faz 45 — Topluluğun Birden Çok Kategorisi Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Bir topluluk birden çok kategori taşıyabilsin (referans tasarımdaki kartlarda iki rozet var: "Sosyal Sorumluluk + Bilim-Teknoloji"), kategori filtresi bunlardan **herhangi biriyle** eşleşsin.

**Architecture:** Kategori bağı `Club` satırındaki tek kolondan çıkarılıp ayrı bir bağ tablosuna (`ClubCategoryAssignment`) taşınır; `Club.ClubCategoryId` **kaldırılır** ve mevcut veri migration içinde bağ tablosuna kopyalanır — iki kaynak bırakmak "hangisi doğru" sorusunu doğurur (A-74'ün sosyal hesaplar için verdiği kararın aynısı). Bu projede navigation property kullanılmadığı için join'ler ayrı bir DAL'de (`IClubCategoryAssignmentDal`) SQL tarafında yapılır: filtre için "bu kategorideki kulüp id'leri", gösterim için "bu kulüplerin kategori adları" — ikisi de tek sorgu.

**Tech Stack:** .NET 8, EF Core 8, FluentValidation, React 18 + MUI 9.

**Spec:** `docs/MIMARI.md` (v6.11 → v6.12 bu fazda). Tadil edilen kararlar: **K-35** (kategori kapsamı) ve **A-60** (kategori referans verisi). İlgili: A-74 (satır vs kolon), Y-10/Y-42 (N+1 ve SQL'de sayım), A-12 (kullanımdaki referans veri silinemez).

## Global Constraints

- Belge önce, kod sonra (Task 1).
- Çalışma dalı `master`; faz başına **tek commit**; commit mesajında araç imzası/süreç anlatımı yok.
- **Navigation property eklenmez.** Bu kod tabanı düz `int` FK kullanır (`ClubManager`'daki "düz int FK, navigation property yok" yorumu); join gerektiren her okuma DAL'e iner (`IDashboardDal` precedent'i).
- **Y-10/Y-42:** kategori adları satır başına sorguyla okunmaz; sayfa başına **tek** toplu sorgu.
- **A-12:** kullanımdaki kategori silinemez — FK `Restrict` bağ tablosundan kategoriye kurulur, 409 davranışı korunur.
- Kategori üst sınırı **3**'tür; sunucu tarafı doğrulama `UpdateClubRequestValidator`/`CreateClubRequestValidator` içindedir (Y-35: arayüz kolaylık, kapı validator).
- Y-34: her test kendi verisini tohumlar.
- Test komutları: `dotnet test`; `cd arayuz && npm run build && npm run lint`.

### Bağımlılık ve ayrılabilirlik

Bu faz, **Faz 46'nın (topluluk listesi) görünür ön koşuludur** — referans tasarımdaki çoklu rozet ancak bununla mümkün. İstenmezse tamamen atlanabilir: Faz 46 o durumda tek kategori rozeti çizer ve `clubCategoryName` alanını kullanmaya devam eder; başka hiçbir görevi değişmez.

---

### Task 1: MIMARI — kapsam, karar, kural ve iki tadil (belge önce)

**Files:**
- Modify: `docs/MIMARI.md`

**Interfaces:**
- Produces: K-49, A-80, Y-85; K-35 ve A-60 tadilleri.

- [x] **Step 1: Sürüm ve sayaçlar**

`**Sürüm:** v6.11` → `v6.12`; kronolojik listeye kalın olarak ekle:

```markdown
· **v6.12: 6 Eylül 2026 (K-49, A-80, Y-85, Faz 45 — topluluğun birden çok kategorisi; K-35 ve A-60 tadil edildi)**
```

Giriş paragrafına: `**v6.12** topluluğa birden çok kategori tanımlayarak 1 kapsam maddesi (K-49), 1 karar (A-80) ve 1 kural (Y-85) ekledi ve **K-35 ile A-60'ı tadil etti**.`
Sayaçlar: `Karar | 80 (… + 1 v6.12)`, `Yasak kural | 85 (… + 1 v6.12)`, `Uygulama fazı | 45 (… + 1 v6.12)`.
İçindekiler: `Y-01 … Y-85`, `K-01 … K-49`, `A-01 … A-80`.

- [x] **Step 2: K-35'i tadil et (satırı değiştir, silme)**

Mevcut K-35 satırının "Gerçekleşme" sütununa ekle ve başlığa tadil notu düş:

```markdown
| **K-35** *(v6.12'de tadil edildi)* | **Topluluk kategorisi** | Kategori referans verisi; kulüp **bir veya birden çok** kategori taşır, kuruluş başvurusu tek kategori önerir, listeler kategoriye göre filtrelenir | `ClubCategory`, **`ClubCategoryAssignment` (ClubId + ClubCategoryId)**, `ClubApplication.ProposedCategoryId` (nullable, tekil kalır), cache geçersizleştirme. **v6.0-v6.11'deki hâli:** `Club.ClubCategoryId` tek ve nullable bir kolondu (A-60, A-80, Y-45, Y-85) |
```

- [x] **Step 3: A-60'ı tadil et**

A-60 satırının sonuna, kararın gerekçesini bozmadan ekle:

```markdown
**v6.12 tadili:** kategori artık `Club` üzerinde bir kolon değil, `ClubCategoryAssignment` satırıdır. A-60'ın kazancı korunur — kullanımdaki kategori hâlâ silinemez; FK `Restrict` bu kez bağ tablosundan kategoriye kurulur ve `ReferentialIntegrityConflictException` üzerinden aynı 409'u üretir (A-80).
```

- [x] **Step 4: K-49**

```markdown
| **K-49** | **Topluluğun birden çok kategorisi** | Bir topluluk en fazla **3** kategori taşır; vitrin kartında ve detayda hepsi rozet olarak görünür. Kategori filtresi, kulübün kategorilerinden **herhangi biri** eşleşince kulübü listeler | `ClubCategoryAssignment`, `IClubCategoryAssignmentDal` (iki toplu sorgu), `Update/CreateClubRequestDto.ClubCategoryIds` (A-80, Y-85) |
```

- [x] **Step 5: A-80**

```markdown
| **A-80** | Kategori bağı **satırdır, kolon değil**; tek kaynak bırakılır | A | `Club.ClubCategoryId` **kaldırılır**, veri migration içinde `ClubCategoryAssignment`'a taşınır. "Birincil kategori kolonu + çoklu bağ tablosu" ikilisi bilinçli olarak reddedildi: iki kaynak, biri güncellenirken öbürünün unutulması ve "hangisi doğru" sorusu demektir (A-74'ün sosyal hesaplar için verdiği kararın aynısı). Bu kod tabanında navigation property olmadığı için join'ler `IClubCategoryAssignmentDal`'de SQL tarafında yapılır: `GetClubIdsByCategoryAsync` (filtre) ve `GetNamesByClubAsync` (gösterim) — ikisi de tek sorgu, sayfa başına bir kez. Kuruluş başvurusundaki `ProposedCategoryId` **tekil kalır** (öneri tek bir sınıflandırmadır); onay dalında tek bir bağ satırına dönüşür (K-49, Y-85, A-60 tadili) |
```

- [x] **Step 6: Y-85**

```markdown
| **Y-85** | Kulüp kategorilerini satır başına sorguyla okumak veya kategori filtresini belleğe çekip `Where` ile uygulamak; `Club` üzerine ikinci bir kategori kolonu geri getirmek | Kategoriler sayfa başına **tek** toplu sorguyla okunur (`GetNamesByClubAsync`), filtre önce kategorideki kulüp id'lerini SQL'de bulur (`GetClubIdsByCategoryAsync`), sonra mevcut sayfalama sorgusuna `ids.Contains(c.Id)` olarak girer. Gerekçe: Y-10'un ve `FillCategoryNamesAsync`'in kategori karşılığı — 12 kulüplük bir sayfa 12 sorgu açmaz; kategori tek kaynakta (bağ tablosu) durur (K-49, A-80) |
```

---

### Task 2: Bağ tablosu, veri taşıma ve kolonun kaldırılması

**Files:**
- Create: `src/Entities/ClubCategoryAssignment.cs`
- Create: `src/DataAccess/Configurations/ClubCategoryAssignmentConfiguration.cs`
- Modify: `src/Entities/Club.cs`
- Modify: `src/DataAccess/AppDbContext.cs`
- Modify: `src/DataAccess/Configurations/ClubConfiguration.cs`
- Create: `src/DataAccess/Migrations/<timestamp>_20260906_Faz45_CokluKategori.cs` (EF üretir, **elle düzenlenir**)

**Interfaces:**
- Produces: `ClubCategoryAssignment { int Id; int ClubId; int ClubCategoryId; }`

- [x] **Step 1: Varlığı yaz**

```csharp
using Core.Entities;

namespace Entities;

/// <summary>docs/MIMARI.md · K-49/A-80: topluluk–kategori bağı. Kulüp başına en fazla 3 satır (validator).</summary>
public sealed class ClubCategoryAssignment : IEntity
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public int ClubCategoryId { get; set; }
}
```

- [x] **Step 2: Yapılandırmayı yaz**

```csharp
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>docs/MIMARI.md · A-80: kulüp silinince bağ gider (Cascade); kullanımdaki kategori silinemez (Restrict, A-12).</summary>
public sealed class ClubCategoryAssignmentConfiguration : IEntityTypeConfiguration<ClubCategoryAssignment>
{
    public void Configure(EntityTypeBuilder<ClubCategoryAssignment> builder)
    {
        builder.HasKey(a => a.Id);

        // Aynı kulübe aynı kategori iki kez bağlanamaz.
        builder.HasIndex(a => new { a.ClubId, a.ClubCategoryId }).IsUnique();

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(a => a.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ClubCategory>()
            .WithMany()
            .HasForeignKey(a => a.ClubCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [x] **Step 3: `Club`'tan kolonu kaldır, DbContext'e tabloyu ekle**

`src/Entities/Club.cs` içinden **sil**:

```csharp
    /// <summary>docs/MIMARI.md · K-35/A-60: kategori. Nullable — mevcut kulüpler kategorisiz kalabilir.</summary>
    public int? ClubCategoryId { get; set; }
```

`src/DataAccess/Configurations/ClubConfiguration.cs` içinden `ClubCategory` FK bloğunu **sil** (artık bağ tablosunda).

`src/DataAccess/AppDbContext.cs`: `DbSet<ClubCategoryAssignment> ClubCategoryAssignments => Set<ClubCategoryAssignment>();` ve `builder.ApplyConfiguration(new ClubCategoryAssignmentConfiguration());`.

- [x] **Step 4: Migration üret**

```bash
dotnet ef migrations add 20260906_Faz45_CokluKategori --project src/DataAccess
```
**Not:** `--startup-project src/WebAPI` EKLEME (tasarım zamanı fabrikası `AppDbContextFactory.cs`).

- [x] **Step 5: Migration'ı elle düzenle — veri kaybını önle**

EF üretilen `Up`'ta önce tabloyu kurar, sonra kolonu düşürür. **Aralarına veri taşımayı ekle**; sırayı bozma:

```csharp
        // 1) EF'in ürettiği CreateTable("ClubCategoryAssignments") burada kalır.

        // 2) A-80: mevcut tekil kategoriler bağ tablosuna taşınır — kolon düşmeden ÖNCE.
        migrationBuilder.Sql(@"
            INSERT INTO ClubCategoryAssignments (ClubId, ClubCategoryId)
            SELECT Id, ClubCategoryId FROM Clubs WHERE ClubCategoryId IS NOT NULL;");

        // 3) EF'in ürettiği DropIndex/DropForeignKey/DropColumn("ClubCategoryId", "Clubs") buraya alınır.
```

`Down` için de geri taşımayı yaz (kolon geri eklendikten sonra, kulüp başına ilk bağ):

```csharp
        migrationBuilder.Sql(@"
            UPDATE c SET c.ClubCategoryId = a.ClubCategoryId
            FROM Clubs c
            CROSS APPLY (SELECT TOP 1 ClubCategoryId FROM ClubCategoryAssignments WHERE ClubId = c.Id ORDER BY Id) a;");
```

- [x] **Step 6: Uygula ve derle**

```bash
dotnet ef database update --project src/DataAccess
dotnet build
```
Beklenen: `dotnet build` **kırmızı** — `ClubCategoryId`'yi kullanan 12 yer var (Task 3-4 bunları kapatır). Migration'ın kendisi hatasız uygulanmalı.

---

### Task 3: Bağ DAL'i (join'ler SQL'de)

**Files:**
- Create: `src/DataAccess/Repositories/IClubCategoryAssignmentDal.cs`
- Create: `src/DataAccess/Repositories/EfClubCategoryAssignmentDal.cs`
- Modify: `src/DataAccess/DependencyResolvers/DataAccessAutofacModule.cs`

**Interfaces:**
- Produces: `GetClubIdsByCategoryAsync(int categoryId, ct)` → `IReadOnlyCollection<int>`, `GetNamesByClubAsync(IReadOnlyCollection<int> clubIds, ct)` → `IReadOnlyDictionary<int, IReadOnlyList<string>>`, `ReplaceAsync(int clubId, IReadOnlyCollection<int> categoryIds, ct)`.

- [x] **Step 1: Arayüzü yaz**

```csharp
namespace DataAccess.Repositories;

/// <summary>
/// docs/MIMARI.md · A-80/Y-85: kategori bağının join gerektiren okumaları. Bu kod tabanında
/// navigation property yok; join'ler burada, SQL tarafında yapılır (IDashboardDal precedent'i).
/// </summary>
public interface IClubCategoryAssignmentDal
{
    /// <summary>Filtre için: bu kategoriye bağlı kulüp id'leri — tek sorgu.</summary>
    Task<IReadOnlyCollection<int>> GetClubIdsByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>Gösterim için: verilen kulüplerin kategori adları, ada göre sıralı — tek sorgu.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyList<string>>> GetNamesByClubAsync(
        IReadOnlyCollection<int> clubIds, CancellationToken cancellationToken = default);

    /// <summary>Kulübün kategorilerini topluca değiştirir (eskiler silinir, yeniler yazılır). SaveChanges çağıran katmana aittir.</summary>
    Task ReplaceAsync(int clubId, IReadOnlyCollection<int> categoryIds, CancellationToken cancellationToken = default);
}
```

- [x] **Step 2: Uygulamasını yaz**

```csharp
using Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

/// <summary>docs/MIMARI.md · Y-85: her metot tek sorgudur; satır başına sorgu yok.</summary>
public sealed class EfClubCategoryAssignmentDal(AppDbContext context) : IClubCategoryAssignmentDal
{
    public async Task<IReadOnlyCollection<int>> GetClubIdsByCategoryAsync(int categoryId, CancellationToken cancellationToken = default) =>
        await context.ClubCategoryAssignments.AsNoTracking()
            .Where(a => a.ClubCategoryId == categoryId)
            .Select(a => a.ClubId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<string>>> GetNamesByClubAsync(
        IReadOnlyCollection<int> clubIds, CancellationToken cancellationToken = default)
    {
        if (clubIds.Count == 0)
        {
            return new Dictionary<int, IReadOnlyList<string>>();
        }

        var rows = await context.ClubCategoryAssignments.AsNoTracking()
            .Where(a => clubIds.Contains(a.ClubId))
            .Join(context.ClubCategories.AsNoTracking(), a => a.ClubCategoryId, c => c.Id, (a, c) => new { a.ClubId, c.Name })
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .GroupBy(x => x.ClubId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.Name).ToList());
    }

    public async Task ReplaceAsync(int clubId, IReadOnlyCollection<int> categoryIds, CancellationToken cancellationToken = default)
    {
        var existing = await context.ClubCategoryAssignments
            .Where(a => a.ClubId == clubId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        context.ClubCategoryAssignments.RemoveRange(existing);

        foreach (var categoryId in categoryIds.Distinct())
        {
            await context.ClubCategoryAssignments
                .AddAsync(new ClubCategoryAssignment { ClubId = clubId, ClubCategoryId = categoryId }, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
```

- [x] **Step 3: Autofac kaydı**

`DataAccessAutofacModule.Load` içine, diğer DAL kayıtlarının yanına:

```csharp
        builder.RegisterType<EfClubCategoryAssignmentDal>()
            .As<IClubCategoryAssignmentDal>()
            .InstancePerLifetimeScope();
```

---

### Task 4: Business katmanı — 12 kullanım noktasının kapatılması

**Files:**
- Modify: `src/Business/DTOs/Clubs/ClubListItemDto.cs`, `ClubDetailDto.cs`, `CreateClubRequestDto.cs`, `UpdateClubRequestDto.cs`
- Modify: `src/Business/DTOs/Public/PublicClubListItemDto.cs`, `PublicClubDetailDto.cs`
- Modify: `src/Business/Concrete/ClubManager.cs`, `PublicContentManager.cs`, `ClubApplicationManager.cs`
- Modify: `src/Business/ValidationRules/CreateClubRequestValidator.cs`, `UpdateClubRequestValidator.cs`
- Modify: `src/Business/Mappings/ClubMappingProfile.cs`
- Test: `tests/Business.Tests/ClubManagerTests.cs`, `tests/Business.Tests/PublicContentManagerTests.cs`

**Interfaces:**
- Consumes: `IClubCategoryAssignmentDal` (Task 3).
- Produces: `*.ClubCategoryNames` (`IReadOnlyList<string>`), `Create/UpdateClubRequestDto.ClubCategoryIds` (`IReadOnlyList<int>`).

- [x] **Step 1: Başarısız testleri yaz**

`tests/Business.Tests/ClubManagerTests.cs` (mock alanlarına `private readonly Mock<IClubCategoryAssignmentDal> _clubCategoryAssignmentDal = new();` ekle ve kurucuya geçir):

```csharp
[Fact(DisplayName = "K-49: UpdateAsync kulübün kategorilerini topluca değiştirir")]
public async Task UpdateAsync_ReplacesCategories()
{
    var club = new Club { Id = 1, Name = "Kulüp", AdvisorId = 1, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
    _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((Expression<Func<Club, bool>> filter, CancellationToken _) => new[] { club }.AsQueryable().Where(filter).FirstOrDefault());

    var result = await _sut.UpdateAsync(1, new UpdateClubRequestDto { Name = "Kulüp", ClubCategoryIds = [3, 7] });

    Assert.True(result.IsSuccess);
    _clubCategoryAssignmentDal.Verify(
        d => d.ReplaceAsync(1, It.Is<IReadOnlyCollection<int>>(ids => ids.Contains(3) && ids.Contains(7)), It.IsAny<CancellationToken>()),
        Times.Once);
}

[Fact(DisplayName = "K-49/Y-85: GetByIdAsync kategori adlarını tek toplu sorgudan doldurur")]
public async Task GetByIdAsync_FillsCategoryNames()
{
    var club = new Club { Id = 1, Name = "Kulüp", AdvisorId = 1, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
    _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);
    _clubCategoryAssignmentDal
        .Setup(d => d.GetNamesByClubAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new Dictionary<int, IReadOnlyList<string>> { [1] = ["Bilim - Teknoloji", "Sosyal Sorumluluk"] });

    var result = await _sut.GetByIdAsync(1);

    Assert.Equal(["Bilim - Teknoloji", "Sosyal Sorumluluk"], result.Data!.ClubCategoryNames);
}
```

`tests/Business.Tests/PublicContentManagerTests.cs`:

```csharp
[Fact(DisplayName = "K-49: vitrin kategori filtresi bağ tablosundan gelen id kümesiyle daraltılır")]
public async Task GetClubsAsync_CategoryFilter_UsesAssignmentIds()
{
    var matching = new Club { Id = 1, Name = "Eşleşen", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow };
    var other = new Club { Id = 2, Name = "Eşleşmeyen", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow };
    SetupPagedFilter<Club, string>(_clubRepository, [matching, other]);
    _clubCategoryAssignmentDal
        .Setup(d => d.GetClubIdsByCategoryAsync(4, It.IsAny<CancellationToken>()))
        .ReturnsAsync([1]);
    _clubCategoryAssignmentDal
        .Setup(d => d.GetNamesByClubAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new Dictionary<int, IReadOnlyList<string>> { [1] = ["Bilim"] });

    var result = await _sut.GetClubsAsync(0, 20, null, 4);

    var item = Assert.Single(result.Data!.Items);
    Assert.Equal("Eşleşen", item.Name);
    Assert.Equal(["Bilim"], item.ClubCategoryNames);
}
```

- [x] **Step 2: Çalıştır, başarısız olduklarını gör**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~Categor"
```
Beklenen: derleme hatası (alanlar/DAL yok).

- [x] **Step 3: DTO'ları değiştir**

- `ClubListItemDto` ve `ClubDetailDto`: `int? ClubCategoryId` ve `string? ClubCategoryName` **kaldırılır**, yerine:

```csharp
    /// <summary>docs/MIMARI.md · K-49/A-80: ada göre sıralı kategori adları. Boş liste = kategorisiz.</summary>
    public IReadOnlyList<string> ClubCategoryNames { get; set; } = [];
```

- `ClubDetailDto`'ya ayrıca düzenleme formunun ihtiyacı olan kimlikler:

```csharp
    /// <summary>docs/MIMARI.md · K-49: düzenleme formu bu kimliklerle kutucukları işaretler.</summary>
    public IReadOnlyList<int> ClubCategoryIds { get; set; } = [];
```

- `CreateClubRequestDto` / `UpdateClubRequestDto`: `int? ClubCategoryId` → `public IReadOnlyList<int> ClubCategoryIds { get; set; } = [];`
- `PublicClubListItemDto` / `PublicClubDetailDto`: `string? ClubCategoryName` → `public IReadOnlyList<string> ClubCategoryNames { get; set; } = [];`

- [x] **Step 4: Doğrulayıcılara üst sınırı ekle**

`CreateClubRequestValidator` ve `UpdateClubRequestValidator`'ın ikisine de:

```csharp
        // K-49: kart tasarımı üç rozetten fazlasını taşımaz; kapı burada (Y-35).
        RuleFor(x => x.ClubCategoryIds)
            .Must(ids => ids.Count <= 3).WithMessage("En fazla 3 kategori seçilebilir.")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Aynı kategori birden fazla kez seçilemez.");
```

- [x] **Step 5: `ClubManager`'ı güncelle**

Kurucuya `IClubCategoryAssignmentDal clubCategoryAssignmentDal` ekle. Sonra:

- `GetListPagedAsync`: `c.ClubCategoryId == categoryId` yerine — sorgudan **önce** id kümesini al:

```csharp
        // Y-85: kategori filtresi SQL'de iki adımda: önce kategorideki kulüp id'leri, sonra sayfalama.
        IReadOnlyCollection<int>? categoryClubIds = categoryId is { } id
            ? await clubCategoryAssignmentDal.GetClubIdsByCategoryAsync(id, cancellationToken).ConfigureAwait(false)
            : null;

        var paged = await clubRepository.GetListPagedAsync(
            pageIndex,
            clampedPageSize,
            c => (isActive == null || c.IsActive == isActive)
                && (categoryClubIds == null || categoryClubIds.Contains(c.Id))
                && (term.Length == 0 || c.Name.Contains(term)),
            c => c.Name,
            descending: false,
            cancellationToken).ConfigureAwait(false);
```

- `FillCategoryNamesAsync`: gövdesini bağ DAL'ine çevir:

```csharp
    /// <summary>Y-85: sayfa başına TEK toplu sorgu — satır başına sorgu N+1 üretirdi.</summary>
    private async Task FillCategoryNamesAsync(IReadOnlyList<ClubListItemDto> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        var namesByClub = await clubCategoryAssignmentDal
            .GetNamesByClubAsync(items.Select(i => i.Id).ToList(), cancellationToken)
            .ConfigureAwait(false);

        foreach (var item in items)
        {
            item.ClubCategoryNames = namesByClub.GetValueOrDefault(item.Id, []);
        }
    }
```

- `GetByIdAsync`: kategori adı okuyan bloğu, `GetNamesByClubAsync([id])` çağrısıyla değiştir ve `dto.ClubCategoryIds`'i de doldur (`ClubCategoryAssignments` üzerinden — bunun için DAL'e ihtiyaç yok, `IEntityRepository<ClubCategoryAssignment>` ile `GetListAsync(a => a.ClubId == id)` yeterlidir; kurucuya bu repository'yi de ekle).
- `CreateAsync`: `ClubCategoryId = request.ClubCategoryId` satırını sil; `SaveChangesAsync`'ten **sonra** (club.Id oluştuktan sonra) `await clubCategoryAssignmentDal.ReplaceAsync(club.Id, request.ClubCategoryIds, ct)` ve ardından yine `SaveChangesAsync`.
- `UpdateAsync`: `club.ClubCategoryId = request.ClubCategoryId` satırını `await clubCategoryAssignmentDal.ReplaceAsync(clubId, request.ClubCategoryIds, ct)` ile değiştir (mevcut `SaveChangesAsync` ikisini birden yazar; `TransactionAspect` zaten sarıyor).

- [x] **Step 6: `PublicContentManager`'ı güncelle**

Kurucuya `IClubCategoryAssignmentDal clubCategoryAssignmentDal` ekle; `GetCategoryNamesAsync` yardımcısını sil; `GetClubsAsync`'te filtreyi `ClubManager` ile **birebir aynı** iki adımlı desene çevir; kart/detay DTO'sunda `ClubCategoryNames = namesByClub.GetValueOrDefault(c.Id, [])` doldur.

- [x] **Step 7: `ClubApplicationManager` onay dalını güncelle**

`ClubApplicationManager.cs:458` civarındaki `ClubCategoryId = application.ProposedCategoryId` satırını sil; kulüp kaydedildikten sonra:

```csharp
                    // A-80: öneri tekildir, onayda tek bir bağ satırına dönüşür.
                    if (application.ProposedCategoryId is { } proposedCategoryId)
                    {
                        await clubCategoryAssignmentDal
                            .ReplaceAsync(createdClub.Id, [proposedCategoryId], cancellationToken)
                            .ConfigureAwait(false);
                    }
```

(Kurucuya DAL'i ekle; değişken adını dosyadaki gerçek kulüp değişkeniyle eşleştir.)

- [x] **Step 8: AutoMapper profilini güncelle**

`ClubMappingProfile`: `ClubCategoryName` Ignore satırlarını `ClubCategoryNames` ve `ClubCategoryIds` için Ignore'a çevir (kaynağı `Club` üzerinde yok — manager dolduruyor).

- [x] **Step 9: Derle ve testleri çalıştır**

```bash
dotnet build
dotnet test tests/Business.Tests
```
Beklenen: PASS. Kalan derleme hataları, Step 3'te kaldırılan alanları kullanan test/DTO noktalarıdır — `grep -rn "ClubCategoryId\b" src tests` ile hepsini kapat.

---

### Task 5: Arayüz — çoklu seçim ve rozetler

**Files:**
- Modify: `arayuz/src/api/types.ts`
- Modify: `arayuz/src/pages/ClubDetailPage.tsx`
- Modify: `arayuz/src/pages/ClubsPage.tsx`
- Modify: `arayuz/src/pages/public/PublicClubsPage.tsx`, `arayuz/src/pages/public/PublicClubDetailPage.tsx`
- Modify: `arayuz/src/schemas/clubForm.ts`

- [x] **Step 1: Tipleri güncelle**

`ClubListItemDto`/`ClubDetailDto`/`PublicClubListItemDto`/`PublicClubDetailDto` içindeki `clubCategoryId`/`clubCategoryName` alanlarını kaldır; ekle:

```ts
  /** docs/MIMARI.md · K-49: ada göre sıralı kategori adları. */
  clubCategoryNames: string[]
```
`ClubDetailDto`'ya ayrıca `clubCategoryIds: number[]`.

- [x] **Step 2: Şemayı güncelle**

`arayuz/src/schemas/clubForm.ts`: `clubCategoryId: z.number().int()` → `clubCategoryIds: z.array(z.number().int()).max(3, 'En fazla 3 kategori seçebilirsiniz.')`; `emptyClubFormValues.clubCategoryIds = []`; `toCategoryPayload` yardımcısını **sil** (0 sentinel'i artık yok) ve çağıran yerleri `values.clubCategoryIds` ile değiştir.

- [x] **Step 3: Yönetim formunu çoklu seçime çevir**

`ClubDetailPage.tsx` (GeneralTab) ve `ClubsPage.tsx` (oluşturma formu) içindeki kategori `TextField select`'ini çoklu yap:

```tsx
          <Controller
            name="clubCategoryIds"
            control={control}
            render={({ field, fieldState }) => (
              <TextField
                {...field}
                select
                fullWidth
                margin="dense"
                label="Kategoriler (en fazla 3)"
                error={!!fieldState.error}
                helperText={fieldState.error?.message}
                slotProps={{ select: { multiple: true, renderValue: (selected) => (selected as number[]).length + ' kategori' } }}
              >
                {(categoriesQuery.data?.items ?? []).map((category) => (
                  <MenuItem key={category.id} value={category.id}>
                    {category.name}
                  </MenuItem>
                ))}
              </TextField>
            )}
          />
```

`openEditDialog`/`reset` çağrılarında `clubCategoryId: club?.clubCategoryId ?? 0` yerine `clubCategoryIds: club?.clubCategoryIds ?? []`.

- [x] **Step 4: Rozetleri çoğullaştır**

Tek `Chip` çizen her yeri listeye çevir (dört dosyada arayarak: `clubCategoryName`):

```tsx
{club.clubCategoryNames.map((name) => (
  <Chip key={name} size="small" variant="outlined" color="primary" label={name} />
))}
```

- [x] **Step 5: Derle ve lint'le**

```bash
cd arayuz && npm run build && npm run lint
```

---

### Task 6: Tam doğrulama ve tek commit

- [x] **Step 1: Tüm testler**

```bash
dotnet test
cd arayuz && npm run build && npm run lint
```
Beklenen: iki bilinen `/api/public/stats` hatası dışında tamamı PASS.

- [x] **Step 2: Elle doğrula**

- Migration sonrası mevcut kulüplerin kategorileri **kaybolmamalı** (bir kulübü aç, rozeti dursun).
- Bir kulübe 2 kategori ata → vitrin kartında iki rozet görünmeli; 4 kategori denemesi 400 dönmeli.
- Kategori filtresi: iki kategoriden herhangi biriyle filtreleyince kulüp listede çıkmalı.
- Kullanımdaki bir kategoriyi silmeyi dene → 409 gelmeli (A-12 korunuyor).

- [x] **Step 3: Commit**

```bash
git add -A -- docs/MIMARI.md docs/superpowers/plans/2026-09-06-faz-45-coklu-kategori.md src tests arayuz/src
git commit -m "$(cat <<'EOF'
Faz 45: toplulugun birden cok kategorisi

Docs: docs/MIMARI.md v6.12 (K-49, A-80, Y-85; K-35 ve A-60 tadil edildi).
EOF
)"
```
