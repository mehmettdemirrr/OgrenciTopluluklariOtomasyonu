# Faz 37 — Danışmanın Kulüpleri "Kulüplerim"de Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Akademik danışman "Kulüplerim" sayfasında danışmanı olduğu toplulukları görebilsin.

**Architecture:** Bugün `ClubMemberManager.GetMineAsync` yalnızca `Student` profili arıyor; danışman `AcademicStaff` olduğu için sorgu daha ilk adımda boş dönüyor (`src/Business/Concrete/ClubMemberManager.cs:35-39`). Çözüm, aynı ucu (`GET /api/clubs/mine`) korumak ve sonuca danışmanlık kayıtlarını **ayrı bir ilişki türü** olarak eklemektir. Danışmanlık bir üyelik değildir: dönem, katılım tarihi ve rol kavramları ona uymaz, "Ayrıl" eylemi hiç uymaz. Bu yüzden DTO'ya `Relationship` alanı gelir ve arayüz karta göre farklı davranır.

**Tech Stack:** .NET 8, EF Core 8, React 18 + MUI, TanStack Query.

**Spec:** `docs/MIMARI.md` (v6.3 → v6.4 bu fazda). İlgili mevcut karar: A-14 (AcademicStaff ↔ ApplicationUser 1-1), A-31 (kulübün danışmanı).

## Global Constraints

- Belge önce, kod sonra: `docs/MIMARI.md` güncellenmeden kod yazılmaz (Task 1).
- Çalışma dalı `master`; faz başına **tek commit**; commit mesajında araç imzası/süreç anlatımı yok.
- Y-34: her test kendi verisini tohumlar.
- §22.3 kararı korunur: üyelik kayıtları **yalnızca güncel dönem** için döner. Danışmanlık dönemsel değildir, bu yüzden dönem filtresi danışman dalına uygulanmaz — bu ayrım A-70'te yazılıdır.
- Aspect sırası: `SecuredOperation` → `ValidationAspect` → `TransactionAspect` → `CacheAspect`/`CacheRemoveAspect`.
- Test komutları: `dotnet test`; `cd arayuz && npm run build && npm run lint`.

---

### Task 1: MIMARI'ye kapsam ve kararı yaz (belge önce)

**Files:**
- Modify: `docs/MIMARI.md`

**Interfaces:**
- Produces: K-41, A-70, Y-77.

- [ ] **Step 1: Sürüm ve sayaçlar**

Sürüm bloğuna v6.4 kaydını ekle; sayaçları **70 karar / 77 kural / 37 faz** yap.

- [ ] **Step 2: K-41**

```markdown
- **K-41 — Danışmanın toplulukları:** Akademik danışman, "Kulüplerim" sayfasında danışmanı
  olduğu toplulukları görür. Öğrenci üyelikleriyle aynı listede, ilişkisi ayırt edilerek durur.
```

- [ ] **Step 3: A-70**

```markdown
- **A-70 — Tek uç, iki ilişki.** `GET /api/clubs/mine` tek uç olarak kalır; yanıt satırlarına
  `Relationship` (Member | Advisor) alanı eklenir. Üyelik satırları §22.3 gereği YALNIZCA güncel
  döneme aittir; danışmanlık dönemsel bir kayıt değildir (Club.AdvisorId), bu yüzden danışman
  satırlarına dönem filtresi uygulanmaz. Bir kullanıcı aynı kulübe hem üye hem danışman olamaz
  (biri öğrenci, diğeri personel profilidir), bu yüzden satırlar çakışmaz.
```

- [ ] **Step 4: Y-77**

```markdown
- **Y-77 — Danışmanlıktan "ayrılınamaz".** `Relationship == Advisor` satırında arayüz "Ayrıl"
  eylemini göstermez ve `DELETE /api/clubs/{id}/membership` çağrılmaz. Danışman değişikliği
  kulüp düzenleme akışının (AdvisorId) işidir; üyelikten çıkma akışının değil.
```

---

### Task 2: DTO + Business — danışman dalı

**Files:**
- Create: `src/Entities/Enums/ClubRelationship.cs`
- Modify: `src/Business/DTOs/Clubs/MyClubMembershipDto.cs`
- Modify: `src/Business/Concrete/ClubMemberManager.cs:28-80` (`GetMineAsync`)
- Test: `tests/Business.Tests/ClubMemberManagerTests.cs`

**Interfaces:**
- Produces:
  - `enum ClubRelationship { Member = 0, Advisor = 1 }`
  - `MyClubMembershipDto.Relationship` (`ClubRelationship`)
  - `GetMineAsync` artık danışman satırları da döner.

- [ ] **Step 1: Başarısız testleri yaz**

`tests/Business.Tests/ClubMemberManagerTests.cs`:

```csharp
[Fact]
public async Task GetMineAsync_ReturnsAdvisedClubs_ForAcademicStaff()
{
    // Y-34: bu test kendi verisini tohumlar.
    var staff = await SeedAcademicStaffAsync(applicationUserId: 77);
    await SeedClubAsync(name: "Robotik Topluluğu", advisorId: staff.Id);
    currentUser.UserId.Returns(77);

    var result = await sut.GetMineAsync();

    var row = Assert.Single(result.Data);
    Assert.Equal("Robotik Topluluğu", row.ClubName);
    Assert.Equal(ClubRelationship.Advisor, row.Relationship);
}

[Fact]
public async Task GetMineAsync_MarksStudentRowsAsMember()
{
    var student = await SeedStudentAsync(applicationUserId: 88);
    var term = await SeedCurrentTermAsync();
    await SeedMembershipAsync(studentId: student.Id, termId: term.Id, clubName: "Müzik Topluluğu");
    currentUser.UserId.Returns(88);

    var result = await sut.GetMineAsync();

    Assert.Equal(ClubRelationship.Member, Assert.Single(result.Data).Relationship);
}

[Fact]
public async Task GetMineAsync_IgnoresAdvisorTermFilter_WhenNoCurrentTerm()
{
    // Danışmanlık dönemsel değildir: güncel dönem yokken bile danışman kulübünü görür (A-70).
    var staff = await SeedAcademicStaffAsync(applicationUserId: 99);
    await SeedClubAsync(name: "Tiyatro Topluluğu", advisorId: staff.Id);
    currentUser.UserId.Returns(99);

    var result = await sut.GetMineAsync();

    Assert.Single(result.Data);
}
```

- [ ] **Step 2: Testleri çalıştır, başarısız olduklarını gör**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~GetMineAsync"
```
Beklenen: FAIL (`ClubRelationship` yok; danışman testleri boş liste alır).

- [ ] **Step 3: Enum'u ekle**

`src/Entities/Enums/ClubRelationship.cs`:

```csharp
namespace Entities.Enums;

/// <summary>
/// docs/MIMARI.md · K-41/A-70: kullanıcının BİR kulüple ilişkisi.
/// Bu fazda yalnızca Member ve Advisor üretilir; kalan değerler Faz 41'in
/// (ClubDetailDto.MyRelationship) ihtiyacı için baştan ayrılmıştır — sonradan araya
/// değer sokmak, tel üzerinde metin giden bir enum'da eski istemcileri bozar.
/// </summary>
public enum ClubRelationship
{
    None = 0,
    Member = 1,
    Officer = 2,
    President = 3,
    Advisor = 4,
    Administrator = 5,
}
```

- [ ] **Step 4: DTO'ya alanı ekle**

`src/Business/DTOs/Clubs/MyClubMembershipDto.cs`:

```csharp
    /// <summary>docs/MIMARI.md · A-70: Member = üyelik satırı, Advisor = danışmanlık satırı.</summary>
    public ClubRelationship Relationship { get; set; }
```

- [ ] **Step 5: `GetMineAsync`'i iki dallı hale getir**

`src/Business/Concrete/ClubMemberManager.cs`. Mevcut yapı "öğrenci yoksa boş dön" diyor; bu erken çıkışlar **kaldırılır**, yerine iki liste birleştirilir:

```csharp
    public async Task<IDataResult<IReadOnlyCollection<MyClubMembershipDto>>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return DataResult<IReadOnlyCollection<MyClubMembershipDto>>.Success([]);
        }

        var rows = new List<MyClubMembershipDto>();
        rows.AddRange(await GetMembershipRowsAsync(userId, cancellationToken).ConfigureAwait(false));
        rows.AddRange(await GetAdvisorRowsAsync(userId, cancellationToken).ConfigureAwait(false));

        return DataResult<IReadOnlyCollection<MyClubMembershipDto>>.Success(rows);
    }
```

Mevcut gövde `GetMembershipRowsAsync(int userId, ...)` özel metoduna taşınır (öğrenci yoksa boş liste döner, dönem filtresi orada kalır — §22.3 korunur) ve her satıra `Relationship = ClubRelationship.Member` yazılır.

- [ ] **Step 6: Danışman dalını yaz**

```csharp
    /// <summary>
    /// docs/MIMARI.md · A-70: danışmanlık dönemsel bir kayıt değildir (Club.AdvisorId), bu yüzden
    /// üyelik dalının dönem filtresi buraya uygulanmaz.
    /// </summary>
    private async Task<IReadOnlyCollection<MyClubMembershipDto>> GetAdvisorRowsAsync(int userId, CancellationToken cancellationToken)
    {
        var staff = await academicStaffRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false);
        if (staff is null)
        {
            return [];
        }

        var clubs = await clubRepository.GetListAsync(c => c.AdvisorId == staff.Id, cancellationToken).ConfigureAwait(false);

        return clubs
            .OrderBy(c => c.Name)
            .Select(c => new MyClubMembershipDto
            {
                ClubId = c.Id,
                ClubName = c.Name,
                ClubIsActive = c.IsActive,
                ClubRole = ClubRole.Member,
                ClubRoleName = null,
                JoinedAtUtc = c.CreatedAtUtc,
                AcademicTermName = string.Empty,
                Relationship = ClubRelationship.Advisor,
            })
            .ToList();
    }
```

`ClubMemberManager`'ın birincil kurucusuna `IEntityRepository<AcademicStaff> academicStaffRepository` parametresi ekle (zaten enjekte edilmiş değilse).

- [ ] **Step 7: Testleri çalıştır, geçtiklerini gör**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~GetMineAsync"
```
Beklenen: PASS.

---

### Task 3: Entegrasyon testi — danışman ucu uçtan uca

**Files:**
- Test: `tests/WebAPI.IntegrationTests/ClubMemberManagementTests.cs`

- [ ] **Step 1: Testi yaz**

```csharp
[Fact]
public async Task ClubsMine_ReturnsAdvisorRow_ForAdvisorUser()
{
    var (client, clubId) = await SeedClubWithAdvisorLoginAsync();

    var rows = await client.GetFromJsonAsync<JsonElement>("/api/clubs/mine");

    var first = rows.EnumerateArray().Single();
    Assert.Equal(clubId, first.GetProperty("clubId").GetInt32());
    Assert.Equal("Advisor", first.GetProperty("relationship").GetString());
}
```

- [ ] **Step 2: Çalıştır**

```bash
dotnet test tests/WebAPI.IntegrationTests --filter "FullyQualifiedName~ClubsMine_ReturnsAdvisorRow"
```
Beklenen: PASS (enum, `JsonStringEnumConverter` ile metin döner — `ClubCapability`'nin sayı dönmesi ona özel bir istisnadır).

---

### Task 4: Arayüz — danışman kartı

**Files:**
- Modify: `arayuz/src/api/types.ts` (`MyClubMembershipDto`)
- Modify: `arayuz/src/pages/MyClubsPage.tsx:45-106`

- [ ] **Step 1: Tipi güncelle**

```ts
// Backend enum'unun tamamı yazılır; bu uç yalnızca Member ve Advisor üretir,
// kalan değerler Faz 41'in ClubDetailDto.myRelationship alanında kullanılır.
export type ClubRelationship = 'None' | 'Member' | 'Officer' | 'President' | 'Advisor' | 'Administrator'
```
ve `MyClubMembershipDto`'ya `relationship: ClubRelationship`.

- [ ] **Step 2: Başlık metnini ilişkiye göre yaz**

`PageHeader` açıklaması bugün "üyesi olduğunuz topluluklar" diyor. Danışman satırı varsa bu metin yanlış olur:

```tsx
const hasAdvisorRows = items.some((row) => row.relationship === 'Advisor')
const hasMemberRows = items.some((row) => row.relationship === 'Member')
```
ve açıklamayı bu iki bayrağa göre kur ("Danışmanı olduğunuz topluluklar." / "…üyesi olduğunuz topluluklar…" / ikisi birden).

- [ ] **Step 3: Kartı ilişkiye göre çiz**

Kart içinde rol rozetleri yerine danışman satırında tek bir `<Chip size="small" color="secondary" label="Danışman" />` göster; `joinedAtUtc` satırını ("… tarihinden beri üye") danışman satırında **gösterme**. Eylemlerde (Y-77) "Ayrıl" düğmesini yalnızca `relationship === 'Member'` iken render et.

- [ ] **Step 4: Boş durum metnini düzelt**

`EmptyState` bugün "Henüz bir topluluğa üye değilsiniz" diyor; danışman için de doğru olacak şekilde "Görüntülenecek topluluk yok" başlığına çevir, açıklamayı olduğu gibi bırak.

- [ ] **Step 5: Derle ve lint'le**

```bash
cd arayuz && npm run build && npm run lint
```

---

### Task 5: Tam doğrulama ve tek commit

- [ ] **Step 1: Tüm testler**

```bash
dotnet test
```
Beklenen: tamamı PASS.

- [ ] **Step 2: Elle doğrula**

Danışman hesabıyla giriş yap, "Kulüplerim"de danışmanı olduğun kulübü gör, "Ayrıl" düğmesinin **olmadığını** doğrula. Öğrenci hesabıyla gir, kendi üyeliklerinin eskisi gibi göründüğünü ve "Ayrıl" düğmesinin durduğunu doğrula.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "Faz 37: danisman kulupleri Kuluplerim listesinde"
```
