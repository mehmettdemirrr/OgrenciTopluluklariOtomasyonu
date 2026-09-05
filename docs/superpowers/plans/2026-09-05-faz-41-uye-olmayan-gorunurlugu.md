# Faz 41 — Üye Olmayan Öğrenci İçin Kulüp Görünürlüğü Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Üyesi olmadığı bir kulübün sayfasını açan öğrenci, o kulübün **yayınlanmış etkinliklerini görebilsin**; üye listesi ve rolleri görmesin ve 403 hata ekranıyla karşılaşmasın.

**Architecture:** Sorun iki katmanlı. (1) `ClubDetailPage` sekmeleri **global** izinlere bakıyor (`arayuz/src/pages/ClubDetailPage.tsx:86-89`): başka bir kulüpte yetkili olan öğrencide `memberships.read` bulunduğu için, hiç ilgisi olmayan kulübün "Üyeler"/"Roller" sekmeleri de açılıyor ve uç 403 dönüyor. (2) Etkinlik sekmesi her durumda yönetim ucunu (`GET /api/clubs/{id}/events`, `EventManager.GetForClubAsync` → yazma yetkisi ister) çağırıyor; oysa yayınlanmış etkinlikler için yetki istemeyen `GET /api/events?clubId=` ucu zaten var (`EventManager.GetPublishedAsync`).

Çözüm: `ClubDetailDto`, çağıran kullanıcının **o kulüpteki** ilişkisini ve kapasitelerini taşısın; arayüz sekmeleri buna göre çizsin ve etkinlik sekmesi yetkisizken yayınlanmış uca düşsün. Uçların kendi 403'leri **kaldırılmaz** (Y-35: sekmeyi gizlemek yetki değildir); değişen yalnızca arayüzün doğru ucu seçmesi ve kullanıcıya hata yerine anlamlı bir görünüm vermesidir.

**Tech Stack:** .NET 8, EF Core 8, React 18 + MUI, TanStack Query.

**Spec:** `docs/MIMARI.md` (v6.7 → v6.8 bu fazda). İlgili kararlar: A-68 (kulüp içi yetki matrisi), Y-35 (sekme gizlemek yetki değildir), Y-75 (matris yalnızca daraltır), K-25 (kapsam çözümü).

## Global Constraints

- Belge önce, kod sonra (Task 1).
- Çalışma dalı `master`; faz başına **tek commit**; commit mesajında araç imzası/süreç anlatımı yok.
- **Uçların yetki kapıları gevşetilmez.** `GetForClubAsync`, `GetMembersAsync` ve rol tanımı uçları 403 dönmeye devam eder; bu faz yalnızca arayüzün hangi ucu çağırdığını ve ne çizdiğini değiştirir.
- A-68 kapalıdır: yeni `ClubCapability` bayrağı eklenmez.
- Y-75: matris yalnızca daraltır; `[SecuredOperation]` ilk kapı olarak kalır.
- Y-34: her test kendi verisini tohumlar.
- Test komutları: `dotnet test`; `cd arayuz && npm run build && npm run lint`.

---

### Task 1: MIMARI'ye kapsam, karar ve kuralı yaz (belge önce)

**Files:**
- Modify: `docs/MIMARI.md`

**Interfaces:**
- Produces: K-45, A-75, Y-81.

- [ ] **Step 1: Sürüm ve sayaçlar**

v6.8 kaydını ekle; sayaçları **75 karar / 81 kural / 41 faz** yap.

- [ ] **Step 2: K-45**

```markdown
- **K-45 — Üye olmayanın kulüp görünürlüğü:** Giriş yapmış ama kulübe üye olmayan öğrenci,
  kulübün genel bilgilerini ve YAYINLANMIŞ etkinliklerini görür; üye listesini ve rol
  tanımlarını görmez. Taslak/onay bekleyen etkinlikler yalnızca yetkililere görünür.
```

- [ ] **Step 3: A-75**

```markdown
- **A-75 — Sekme görünürlüğü global izinle değil, kulüpteki ilişkiyle belirlenir.** `ClubDetailDto`,
  çağıran kullanıcının o kulüpteki ilişkisini (`MyRelationship`) ve kapasitelerini (`MyCapabilities`)
  taşır; arayüz sekmeleri bu iki alandan çizilir. Global izin (ör. `memberships.read`) tek başına
  başka bir kulübün üye sekmesini açmaz.
```

- [ ] **Step 4: Y-81**

```markdown
- **Y-81 — Yetkisiz kullanıcı yönetim ucunu çağırmaz.** Kulüp etkinlik listesi, kullanıcı o
  kulüpte `EventsManage` kapasitesine sahip değilken yayınlanmış etkinlik ucundan okunur
  (`GET /api/events?clubId=`); yönetim ucu (`GET /api/clubs/{id}/events`) çağrılmaz. Uçların
  kendi 403'leri yerinde kalır — Y-35 gereği gizleme yetki değildir, bu kural yalnızca
  kullanıcının hatasız bir ekran görmesini garanti eder.
```

---

### Task 2: `ClubDetailDto`'ya ilişki ve kapasite alanları

**Files:**
- Modify: `src/Business/DTOs/Clubs/ClubDetailDto.cs`
- Modify: `src/Business/Concrete/ClubManager.cs` (`GetByIdAsync` ve `ClubDetailDto` kuran diğer noktalar)
- Test: `tests/Business.Tests/ClubManagerTests.cs`

**Interfaces:**
- Consumes: `ClubRelationship` (Faz 37'de tanımlandı — tüm değerler orada ayrılmıştı; bu fazda yeni enum **yazılmaz**).
- Produces: `ClubDetailDto.MyRelationship` (`ClubRelationship`), `ClubDetailDto.MyCapabilities` (`ClubCapability`)

- [ ] **Step 1: Başarısız testleri yaz**

```csharp
[Fact]
public async Task GetByIdAsync_ReportsNoneRelationship_ForNonMember()
{
    var clubId = await SeedClubAsync();
    await SeedStudentLoginAsync(memberOfClubId: null);

    var result = await sut.GetByIdAsync(clubId);

    Assert.Equal(ClubRelationship.None, result.Data.MyRelationship);
    Assert.Equal(ClubCapability.None, result.Data.MyCapabilities);
}

[Fact]
public async Task GetByIdAsync_ReportsPresidentCapabilities_ForPresident()
{
    var clubId = await SeedClubAsync();
    await SeedPresidentLoginAsync(clubId);

    var result = await sut.GetByIdAsync(clubId);

    Assert.Equal(ClubRelationship.President, result.Data.MyRelationship);
    Assert.True(result.Data.MyCapabilities.HasFlag(ClubCapability.MembersManage));
}

[Fact]
public async Task GetByIdAsync_ReportsAdvisor_ForAdvisorOfThatClub()
{
    var clubId = await SeedClubAsync();
    await SeedAdvisorLoginAsync(clubId);

    var result = await sut.GetByIdAsync(clubId);

    Assert.Equal(ClubRelationship.Advisor, result.Data.MyRelationship);
}

[Fact]
public async Task GetByIdAsync_ReportsAdministrator_ForClubsManageAll()
{
    var clubId = await SeedClubAsync();
    await SeedAdminLoginAsync();

    var result = await sut.GetByIdAsync(clubId);

    Assert.Equal(ClubRelationship.Administrator, result.Data.MyRelationship);
}
```

- [ ] **Step 2: Çalıştır, başarısız olduklarını gör**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~GetByIdAsync_Reports"
```

- [ ] **Step 3: Enum'u doğrula (yeni enum yazma)**

`src/Entities/Enums/ClubRelationship.cs` Faz 37'de eklendi ve `None/Member/Officer/President/Advisor/Administrator` değerlerini zaten taşıyor. Dosyayı aç, altı değerin de durduğunu doğrula. Eksikse Faz 37'nin tanımını tamamla; **ikinci bir ilişki enum'u açma** — iki enum, aynı kavramın iki ayrı doğruluk kaynağı demektir.

- [ ] **Step 4: DTO alanlarını ekle**

```csharp
    /// <summary>docs/MIMARI.md · A-75: arayüz sekmeleri bu alandan çizilir, global izinden değil.</summary>
    public ClubRelationship MyRelationship { get; set; }

    /// <summary>
    /// docs/MIMARI.md · A-68/A-75: çağıranın BU kulüpteki kapasiteleri.
    /// [Flags] olduğu için tel üzerinde SAYI gider — Program.cs'teki JsonNumberEnumConverter&lt;ClubCapability&gt;
    /// kaydı bunu sağlar; string'e dönerse arayüzün bit maskesi çalışmaz.
    /// </summary>
    public ClubCapability MyCapabilities { get; set; }
```

- [ ] **Step 5: `GetByIdAsync`'te doldur**

Y-66 gereği yönetici kontrolü ilk satırdır:

```csharp
    private async Task<(ClubRelationship Relationship, ClubCapability Capabilities)> ResolveViewerAsync(
        Club club, CancellationToken cancellationToken)
    {
        // Y-66: yönetici kontrolü her kapsam metodunun İLK satırıdır (A-55).
        if (currentUser.Permissions.Contains(IdentitySeedData.Permissions.ClubsManageAll))
        {
            return (ClubRelationship.Administrator, ClubCapabilityDefaults.ForRole(ClubRole.President) | ClubCapability.MembersManage);
        }

        if (currentUser.UserId is not { } userId)
        {
            return (ClubRelationship.None, ClubCapability.None);
        }

        var advisor = await academicStaffRepository.GetAsync(s => s.Id == club.AdvisorId, cancellationToken).ConfigureAwait(false);
        if (advisor is not null && advisor.ApplicationUserId == userId)
        {
            return (ClubRelationship.Advisor, ClubCapabilityDefaults.ForRole(ClubRole.President) | ClubCapability.MembersManage);
        }

        var student = await studentRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false);
        if (student is null)
        {
            return (ClubRelationship.None, ClubCapability.None);
        }

        // §22.3 / EnsureClubWriteAccessAsync ile aynı dönem: güncel dönemin üyeliği.
        var term = await academicTermRepository.GetAsync(t => t.IsCurrent, cancellationToken).ConfigureAwait(false);
        if (term is null)
        {
            return (ClubRelationship.None, ClubCapability.None);
        }

        var membership = await clubMembershipRepository
            .GetAsync(m => m.ClubId == club.Id && m.StudentId == student.Id && m.AcademicTermId == term.Id, cancellationToken)
            .ConfigureAwait(false);

        if (membership is null)
        {
            return (ClubRelationship.None, ClubCapability.None);
        }

        var relationship = membership.ClubRole switch
        {
            ClubRole.President => ClubRelationship.President,
            ClubRole.Officer => ClubRelationship.Officer,
            _ => ClubRelationship.Member,
        };

        return (relationship, membership.Capabilities);
    }
```

`GetByIdAsync`'te DTO kurulurken bu ikiliyi yaz. **Dikkat:** Bu metot bir yetki kapısı değildir, kapsam çözümüdür (K-25) — mimari testin `CapabilityGuardTests` dışlama listesine `ClubManager.GetByIdAsync` eklenmesi gerekirse, gerekçesi bu cümledir.

- [ ] **Step 6: Testleri çalıştır**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~GetByIdAsync_Reports"
```
Beklenen: PASS.

- [ ] **Step 7: Mimari testi çalıştır**

```bash
dotnet test tests/Architecture.Tests
```
Beklenen: PASS. `CapabilityGuardTests` kırmızıysa, Step 5'teki gerekçeyle dışlamayı testin doküman yorumuna ekleyerek yaz — dışlamayı yorumsuz ekleme.

---

### Task 3: Kapasitenin tel üzerinde sayı gittiğini doğrula

**Files:**
- Test: `tests/WebAPI.IntegrationTests/ClubDetailVisibilityTests.cs`

**Interfaces:**
- Consumes: `ClubDetailDto.MyCapabilities` (Task 2).

- [ ] **Step 1: Testi yaz**

```csharp
/// <summary>
/// Faz 35'te aynı hata yaşandı: [Flags] enum, Program.cs'teki JsonStringEnumConverter yüzünden
/// "MembersView, EventsManage" gibi metin olarak gitti ve arayüzün bit maskesi sessizce boş kaldı.
/// </summary>
[Fact]
public async Task ClubDetail_CapabilitiesTravelAsNumber()
{
    var (client, clubId) = await SeedClubWithPresidentLoginAsync();

    var detail = await client.GetFromJsonAsync<JsonElement>($"/api/clubs/{clubId}");

    Assert.Equal(JsonValueKind.Number, detail.GetProperty("myCapabilities").ValueKind);
    Assert.Equal("President", detail.GetProperty("myRelationship").GetString());
}
```

- [ ] **Step 2: Çalıştır**

```bash
dotnet test tests/WebAPI.IntegrationTests --filter "FullyQualifiedName~CapabilitiesTravelAsNumber"
```
Beklenen: PASS (`ClubCapability` için sayı dönüştürücü Faz 35'te zaten kayıtlı; kırmızıysa `Program.cs`'teki kayıt sırası bozulmuştur).

---

### Task 4: Üye olmayan için yayınlanmış etkinlik ucu — entegrasyon testi

**Files:**
- Test: `tests/WebAPI.IntegrationTests/ClubDetailVisibilityTests.cs`

- [ ] **Step 1: Testleri yaz**

```csharp
[Fact]
public async Task PublishedEvents_AreVisible_ToNonMemberStudent()
{
    var (client, clubId) = await SeedClubWithPublishedEventAndNonMemberLoginAsync();

    var response = await client.GetAsync($"/api/events?clubId={clubId}&pageIndex=0&pageSize=20");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var page = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.Equal(1, page.GetProperty("items").GetArrayLength());
}

[Fact]
public async Task DraftEvents_AreNotVisible_ToNonMemberStudent()
{
    var (client, clubId) = await SeedClubWithDraftEventAndNonMemberLoginAsync();

    var page = await client.GetFromJsonAsync<JsonElement>($"/api/events?clubId={clubId}&pageIndex=0&pageSize=20");

    Assert.Equal(0, page.GetProperty("items").GetArrayLength());
}

[Fact]
public async Task Members_StayForbidden_ForNonMemberStudent()
{
    var (client, clubId) = await SeedClubWithNonMemberLoginAsync();

    var response = await client.GetAsync($"/api/clubs/{clubId}/members?pageIndex=0&pageSize=20");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

- [ ] **Step 2: Çalıştır**

```bash
dotnet test tests/WebAPI.IntegrationTests --filter "FullyQualifiedName~ClubDetailVisibilityTests"
```
Beklenen: üçü de PASS **kod değişikliği olmadan** — bu uçlar zaten böyle davranıyor. Kırmızı çıkan olursa backend'de gerçek bir boşluk var demektir; düzelt.

---

### Task 5: Arayüz — sekmeler ilişkiden çizilsin, etkinlik sekmesi doğru uca gitsin

**Files:**
- Modify: `arayuz/src/api/types.ts`
- Modify: `arayuz/src/pages/ClubDetailPage.tsx:81-132`
- Modify: `arayuz/src/pages/ClubDetailEventsTab.tsx`

**Interfaces:**
- Consumes: `ClubDetailDto.myRelationship`, `ClubDetailDto.myCapabilities` (Task 2).

- [ ] **Step 1: Tipleri ekle**

`ClubRelationship` tipi Faz 37'de altı değerle eklendi; yeniden tanımlama. Yalnızca `ClubDetailDto`'ya `myRelationship: ClubRelationship` ve `myCapabilities: number` alanlarını ekle.

- [ ] **Step 2: Sekme koşullarını değiştir**

`ClubDetailPage.tsx:86-89` bloğunu, global izin yerine kulüpteki kapasiteye bağla:

```tsx
const capabilities = clubQuery.data?.myCapabilities ?? 0
const canViewMembers = (capabilities & ClubCapability.MembersView) !== 0
const canViewRoles = (capabilities & ClubCapability.MembersManage) !== 0
const canManageEvents = (capabilities & ClubCapability.EventsManage) !== 0
// Etkinlik sekmesi HERKESE açıktır: yetkisiz kullanıcı yayınlanmışları görür (K-45).
const canViewEvents = true
```

`canManageClubs` (düzenleme formu) `myRelationship === 'Administrator' || myRelationship === 'Advisor'` ile hesaplanır; global `ClubsWrite` izni tek başına yeterli sayılmaz (A-75).

- [ ] **Step 3: Etkinlik sekmesini iki uçlu yap**

`ClubDetailEventsTab.tsx`, `canManage: boolean` prop'u alsın:

```tsx
const eventsQuery = useQuery({
  queryKey: ['club-events', clubId, canManage],
  queryFn: async () =>
    canManage
      ? (await apiClient.get<PagedResult<EventListItemDto>>(`/clubs/${clubId}/events`, { params: { pageIndex, pageSize } })).data
      : (await apiClient.get<PagedResult<EventListItemDto>>('/events', { params: { clubId, pageIndex, pageSize } })).data,
})
```

Yetkisizken "Yeni Etkinlik", "Onaya Gönder", "Sil" gibi eylem düğmelerini **render etme**; boş listede "Bu toplulukta yayınlanmış etkinlik yok." metnini göster.

- [ ] **Step 4: Sekme çağrı yerini güncelle**

```tsx
{tab === 'events' && <ClubEventsTab clubId={clubId} canManage={canManageEvents} />}
{tab === 'roles' && canViewRoles && <RoleDefinitionsTab clubId={clubId} />}
```

- [ ] **Step 5: Derle ve lint'le**

```bash
cd arayuz && npm run build && npm run lint
```

---

### Task 6: Tam doğrulama ve tek commit

- [ ] **Step 1: Tüm testler**

```bash
dotnet test
```
Beklenen: tamamı PASS.

- [ ] **Step 2: Elle doğrula — hatanın kendisi**

Bir kulüpte yetkili, başka bir kulüpte hiç üye olmayan bir öğrenci hesabıyla gir. Üye **olmadığı** kulübün sayfasını aç:
- "Üyeler" ve "Roller" sekmeleri **görünmemeli**.
- "Etkinlikler" sekmesi görünmeli, açıldığında **403 değil**, yayınlanmış etkinlikler listesi gelmeli.
- Yetkili **olduğu** kulüpte hiçbir şeyin bozulmadığını, taslak etkinliklerin ve yönetim düğmelerinin durduğunu doğrula.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "Faz 41: uye olmayan ogrenci kulubun yayinlanmis etkinliklerini gorebiliyor"
```
