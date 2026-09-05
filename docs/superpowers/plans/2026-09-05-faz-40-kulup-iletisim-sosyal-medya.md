# Faz 40 — Kulüp İletişim ve Sosyal Medya Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Her topluluk kendi iletişim bilgisini ve sosyal medya hesaplarını tanımlayabilsin; bunlar kulüp sayfasında (giriş yapmamış ziyaretçiye de) görünsün.

**Architecture:** Sosyal hesaplar `Club` satırına altı kolon eklemek yerine **ayrı bir tabloda** durur (`ClubSocialLink`: platform + URL + sıra). Gerekçe: platform listesi zamanla değişir, kolon eklemek her seferinde migration ve DTO şişmesi demektir; ayrıca sıralama ve "aynı platformdan iki hesap" ihtiyacı satır modeliyle doğal karşılanır. İletişim alanları (e-posta, telefon) tek satırlık ve kulübe özgü olduğu için `Club` üzerinde kalır. Yetki: mevcut kulüp düzenleme kapısı (danışman/başkan veya `clubs.manage.all`) — yeni bir `ClubCapability` bayrağı **açılmaz**, çünkü A-68 kapalı bir kümedir.

**Tech Stack:** .NET 8, EF Core 8, FluentValidation, React 18 + MUI.

**Spec:** `docs/MIMARI.md` (v6.6 → v6.7 bu fazda). İlgili kararlar: A-68 (kapalı yetki kümesi), Y-75 (matris yalnızca daraltır).

## Global Constraints

- Belge önce, kod sonra (Task 1).
- Çalışma dalı `master`; faz başına **tek commit**; commit mesajında araç imzası/süreç anlatımı yok.
- A-68 kapalıdır: bu faz **yeni `ClubCapability` bayrağı eklemez**; yazma kapısı mevcut kulüp düzenleme kapısıdır.
- Y-16: yeni tablo soft delete gerektirmez (bağlantı silmek kalıcıdır), ama kulüp silinince satırların birlikte gitmesi için FK `OnDelete(DeleteBehavior.Cascade)` verilir.
- Y-34: her test kendi verisini tohumlar.
- Aspect sırası: `SecuredOperation` → `ValidationAspect` → `TransactionAspect` → `CacheRemoveAspect`.
- Test komutları: `dotnet test`; `cd arayuz && npm run build && npm run lint`.

---

### Task 1: MIMARI'ye kapsam, karar ve kuralı yaz (belge önce)

**Files:**
- Modify: `docs/MIMARI.md`

**Interfaces:**
- Produces: K-44, A-74, Y-80.

- [ ] **Step 1: Sürüm ve sayaçlar**

v6.7 kaydını ekle; sayaçları **74 karar / 80 kural / 40 faz** yap.

- [ ] **Step 2: K-44**

```markdown
- **K-44 — Topluluk iletişimi:** Topluluk kendi iletişim e-postasını, telefonunu ve sosyal medya
  hesaplarını tanımlar; bunlar kulüp sayfasında herkese görünür. Tümü isteğe bağlıdır.
```

- [ ] **Step 3: A-74**

```markdown
- **A-74 — Sosyal hesaplar satırdır, kolon değil.** Sosyal medya bağlantıları `ClubSocialLink`
  tablosunda (ClubId, Platform, Url, DisplayOrder) tutulur; `Club` satırına platform başına kolon
  eklenmez. Gerekçe: platform kümesi değişkendir, sıralama gerekir ve aynı platformdan ikinci bir
  hesap mümkündür. E-posta ve telefon kulübe birebir olduğu için `Club` üzerinde kalır.
```

- [ ] **Step 4: Y-80**

```markdown
- **Y-80 — Bağlantı yalnızca https'tir ve tanınan alan adına gider.** `ClubSocialLink.Url`
  sunucu tarafında doğrulanır: şema https, ana bilgisayar adı o platformun bilinen alan adları
  listesinde. `javascript:` şeması, http ve tanınmayan alan adı reddedilir. İstemci doğrulaması
  kolaylıktır; kapı validator'dadır.
```

---

### Task 2: Entity + migration

**Files:**
- Create: `src/Entities/ClubSocialLink.cs`
- Create: `src/Entities/Enums/SocialPlatform.cs`
- Modify: `src/Entities/Club.cs`
- Modify: `src/DataAccess/Concrete/EntityFramework/Contexts/ApplicationDbContext.cs`
- Create: `src/DataAccess/Migrations/<timestamp>_20260905_Faz40_KulupIletisim.cs` (EF üretir)

**Interfaces:**
- Produces: `ClubSocialLink`, `SocialPlatform`, `Club.ContactEmail`, `Club.ContactPhone`.

- [ ] **Step 1: Enum**

```csharp
namespace Entities.Enums;

/// <summary>docs/MIMARI.md · K-44/A-74: desteklenen sosyal medya platformları.</summary>
public enum SocialPlatform
{
    Instagram = 0,
    X = 1,
    LinkedIn = 2,
    YouTube = 3,
    Website = 4,
}
```

- [ ] **Step 2: Entity**

```csharp
using Core.Entities;
using Entities.Enums;

namespace Entities;

/// <summary>docs/MIMARI.md · K-44/A-74: topluluğun sosyal medya bağlantısı. Y-80: yalnızca https.</summary>
public sealed class ClubSocialLink : IEntity
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public SocialPlatform Platform { get; set; }

    public required string Url { get; set; }

    public int DisplayOrder { get; set; }
}
```

- [ ] **Step 3: `Club`'a iletişim alanları**

```csharp
    /// <summary>docs/MIMARI.md · K-44: topluluk iletişim e-postası. Null = tanımsız.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>docs/MIMARI.md · K-44: topluluk iletişim telefonu. Null = tanımsız.</summary>
    public string? ContactPhone { get; set; }
```

- [ ] **Step 4: DbContext yapılandırması**

```csharp
        modelBuilder.Entity<ClubSocialLink>(entity =>
        {
            entity.Property(e => e.Url).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => new { e.ClubId, e.DisplayOrder });
            entity.HasOne<Club>().WithMany().HasForeignKey(e => e.ClubId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Club>(entity =>
        {
            entity.Property(e => e.ContactEmail).HasMaxLength(256);
            entity.Property(e => e.ContactPhone).HasMaxLength(32);
        });
```

`DbSet<ClubSocialLink> ClubSocialLinks` ekle.

- [ ] **Step 5: Migration**

```bash
dotnet ef migrations add 20260905_Faz40_KulupIletisim --project src/DataAccess --startup-project src/WebAPI
dotnet ef database update --project src/DataAccess --startup-project src/WebAPI
dotnet build
```

---

### Task 3: URL doğrulaması (Y-80) — validator ve testleri

**Files:**
- Create: `src/Business/ValidationRules/FluentValidation/SetClubSocialLinksRequestValidator.cs`
- Create: `src/Business/DTOs/Clubs/ClubSocialLinkDto.cs`
- Create: `src/Business/DTOs/Clubs/SetClubSocialLinksRequestDto.cs`
- Test: `tests/Business.Tests/ClubSocialLinkValidationTests.cs`

**Interfaces:**
- Produces:
  - `ClubSocialLinkDto { SocialPlatform Platform; string Url; int DisplayOrder; }`
  - `SetClubSocialLinksRequestDto { string? ContactEmail; string? ContactPhone; IReadOnlyList<ClubSocialLinkDto> Links; }`

- [ ] **Step 1: Başarısız testleri yaz**

```csharp
[Theory]
[InlineData(SocialPlatform.Instagram, "https://www.instagram.com/topluluk", true)]
[InlineData(SocialPlatform.Instagram, "http://www.instagram.com/topluluk", false)]   // https değil
[InlineData(SocialPlatform.Instagram, "https://instagram.evil.com/topluluk", false)] // alan adı değil
[InlineData(SocialPlatform.Instagram, "javascript:alert(1)", false)]                 // şema
[InlineData(SocialPlatform.X, "https://x.com/topluluk", true)]
[InlineData(SocialPlatform.X, "https://twitter.com/topluluk", true)]
[InlineData(SocialPlatform.Website, "https://topluluk.ozal.edu.tr", true)]
public void Validator_AcceptsOnlyHttpsKnownHosts(SocialPlatform platform, string url, bool expected)
{
    var request = new SetClubSocialLinksRequestDto
    {
        Links = [new ClubSocialLinkDto { Platform = platform, Url = url, DisplayOrder = 0 }],
    };

    var result = new SetClubSocialLinksRequestValidator().Validate(request);

    Assert.Equal(expected, result.IsValid);
}
```

- [ ] **Step 2: Çalıştır, başarısız olduğunu gör**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~ClubSocialLinkValidation"
```

- [ ] **Step 3: Validator'ı yaz**

```csharp
/// <summary>docs/MIMARI.md · Y-80: yalnızca https ve platformun bilinen alan adı.</summary>
public sealed class SetClubSocialLinksRequestValidator : AbstractValidator<SetClubSocialLinksRequestDto>
{
    private static readonly IReadOnlyDictionary<SocialPlatform, string[]> KnownHosts =
        new Dictionary<SocialPlatform, string[]>
        {
            [SocialPlatform.Instagram] = ["instagram.com", "www.instagram.com"],
            [SocialPlatform.X] = ["x.com", "www.x.com", "twitter.com", "www.twitter.com"],
            [SocialPlatform.LinkedIn] = ["linkedin.com", "www.linkedin.com"],
            [SocialPlatform.YouTube] = ["youtube.com", "www.youtube.com", "youtu.be"],
        };

    public SetClubSocialLinksRequestValidator()
    {
        RuleFor(x => x.ContactEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        RuleFor(x => x.ContactPhone).MaximumLength(32);
        RuleFor(x => x.Links).Must(links => links.Count <= 10).WithMessage("En fazla 10 bağlantı eklenebilir.");
        RuleForEach(x => x.Links).ChildRules(link =>
        {
            link.RuleFor(l => l.Url).Must(BeAllowedUrl).WithMessage("Bağlantı https olmalı ve platformun adresine gitmelidir.");
            link.RuleFor(l => l.Platform).IsInEnum();
        });
    }

    private static bool BeAllowedUrl(ClubSocialLinkDto link, string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        // Website platformu serbest alan adıdır (kulübün kendi sitesi); şema kısıtı yeterli.
        return !KnownHosts.TryGetValue(link.Platform, out var hosts)
            || hosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase);
    }
}
```

**Not:** `BeAllowedUrl`'ün iki parametreli imzası FluentValidation'ın `Must((parent, value) => …)` aşırı yüklemesiyle bağlanır; `ChildRules` içinde `link.RuleFor(l => l.Url).Must((dto, url) => BeAllowedUrl(dto, url))` yaz.

- [ ] **Step 4: Testleri çalıştır**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~ClubSocialLinkValidation"
```
Beklenen: PASS.

---

### Task 4: Business + uç noktalar

**Files:**
- Modify: `src/Business/Abstract/IClubService.cs`
- Modify: `src/Business/Concrete/ClubManager.cs`
- Modify: `src/Business/DTOs/Clubs/ClubDetailDto.cs`
- Modify: `src/Business/DTOs/Public/PublicClubDetailDto.cs`
- Modify: `src/WebAPI/Controllers/ClubsController.cs`
- Test: `tests/WebAPI.IntegrationTests/ClubSocialLinkTests.cs`

**Interfaces:**
- Consumes: `SetClubSocialLinksRequestDto` (Task 3).
- Produces:
  - `PUT /api/clubs/{clubId}/contact`
  - `ClubDetailDto.ContactEmail/ContactPhone/SocialLinks`, aynı alanlar `PublicClubDetailDto`'da.

- [ ] **Step 1: Başarısız entegrasyon testlerini yaz**

```csharp
[Fact]
public async Task SetContact_Rejects_ForNonOfficerMember()
{
    var (client, clubId) = await SeedClubWithPlainMemberLoginAsync();

    var response = await client.PutAsJsonAsync($"/api/clubs/{clubId}/contact", ValidContactPayload());

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}

[Fact]
public async Task SetContact_ReplacesLinks_ForPresident()
{
    var (client, clubId) = await SeedClubWithPresidentLoginAsync();
    await client.PutAsJsonAsync($"/api/clubs/{clubId}/contact", ValidContactPayload());  // 2 bağlantı

    await client.PutAsJsonAsync($"/api/clubs/{clubId}/contact", SingleLinkPayload());    // 1 bağlantı

    var detail = await client.GetFromJsonAsync<JsonElement>($"/api/clubs/{clubId}");
    Assert.Equal(1, detail.GetProperty("socialLinks").GetArrayLength());
}

[Fact]
public async Task PublicClubDetail_ExposesSocialLinks_Anonymously()
{
    var (client, clubId) = await SeedPublicClubWithLinksAsync();
    var anonymous = CreateAnonymousClient();

    var detail = await anonymous.GetFromJsonAsync<JsonElement>($"/api/public/clubs/{clubId}");

    Assert.True(detail.GetProperty("socialLinks").GetArrayLength() > 0);
}
```

- [ ] **Step 2: Çalıştır, başarısız olduklarını gör**

```bash
dotnet test tests/WebAPI.IntegrationTests --filter "FullyQualifiedName~ClubSocialLinkTests"
```

- [ ] **Step 3: Servis imzasını ekle**

`IClubService`:

```csharp
    /// <summary>docs/MIMARI.md · K-44/Y-80: iletişim ve sosyal bağlantıları topluca değiştirir.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.ClubsWrite)]
    [ValidationAspect(typeof(SetClubSocialLinksRequestValidator))]
    [TransactionAspect]
    [CacheRemoveAspect("ClubManager.")]
    Task<IResult> SetContactAsync(int clubId, SetClubSocialLinksRequestDto request, CancellationToken cancellationToken = default);
```

- [ ] **Step 4: `ClubManager.SetContactAsync`'i yaz**

Kulübü bul (yoksa `NotFound`); **kulübün mevcut düzenleme kapısını** kullan (`ClubManager` içinde `UpdateAsync`'in kullandığı aynı özel metot — yeni bir kapı yazma, mevcut olanı çağır; yoksa `UpdateAsync`'ten çıkarıp paylaş). Sonra:

```csharp
        club.ContactEmail = string.IsNullOrWhiteSpace(request.ContactEmail) ? null : request.ContactEmail.Trim();
        club.ContactPhone = string.IsNullOrWhiteSpace(request.ContactPhone) ? null : request.ContactPhone.Trim();
        clubRepository.Update(club);

        // Bağlantılar toplu değiştirilir: eski satırlar silinir, yenileri yazılır.
        // Kısmi güncelleme (id eşleme) bu ekran için gereksiz karmaşıklıktır — YAGNI.
        var existing = await clubSocialLinkRepository.GetListAsync(l => l.ClubId == clubId, cancellationToken).ConfigureAwait(false);
        foreach (var link in existing)
        {
            clubSocialLinkRepository.Delete(link);
        }

        var order = 0;
        foreach (var link in request.Links)
        {
            await clubSocialLinkRepository.AddAsync(
                new ClubSocialLink { ClubId = clubId, Platform = link.Platform, Url = link.Url.Trim(), DisplayOrder = order++ },
                cancellationToken).ConfigureAwait(false);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(Messages.ClubContactUpdated);
```

`Messages.ClubContactUpdated` sabitini ekle ("İletişim bilgileri güncellendi.").

- [ ] **Step 5: DTO'lara alanları taşı**

`ClubDetailDto` ve `PublicClubDetailDto`'ya `ContactEmail`, `ContactPhone`, `IReadOnlyList<ClubSocialLinkDto> SocialLinks` ekle; `ClubManager`'ın detay kuran **tüm** noktalarında bağlantıları `DisplayOrder`'a göre sıralı doldur.

```bash
grep -rn "new ClubDetailDto\|new PublicClubDetailDto" src/Business
```

- [ ] **Step 6: Controller ucu**

```csharp
    [HttpPut("clubs/{clubId:int}/contact")]
    public async Task<IActionResult> SetContact(int clubId, SetClubSocialLinksRequestDto request, CancellationToken cancellationToken)
    {
        var result = await clubService.SetContactAsync(clubId, request, cancellationToken);
        return result.ToActionResult();
    }
```

- [ ] **Step 7: Testleri çalıştır**

```bash
dotnet test tests/WebAPI.IntegrationTests --filter "FullyQualifiedName~ClubSocialLinkTests"
```
Beklenen: PASS.

---

### Task 5: Arayüz — düzenleme formu ve gösterim

**Files:**
- Modify: `arayuz/src/api/types.ts`
- Modify: `arayuz/src/pages/ClubDetailPage.tsx` (GeneralTab)
- Modify: `arayuz/src/pages/public/PublicClubDetailPage.tsx`

- [ ] **Step 1: Tipleri ekle**

```ts
export type SocialPlatform = 'Instagram' | 'X' | 'LinkedIn' | 'YouTube' | 'Website'

export interface ClubSocialLinkDto {
  platform: SocialPlatform
  url: string
  displayOrder: number
}
```
ve `ClubDetailDto` / `PublicClubDetailDto` içine `contactEmail: string | null`, `contactPhone: string | null`, `socialLinks: ClubSocialLinkDto[]`.

- [ ] **Step 2: Düzenleme formu**

`GeneralTab` içinde, yalnızca `canManage` iken görünen bir "İletişim" bölümü: e-posta ve telefon alanları + platform seçici & URL alanından oluşan, satır ekle/sil düğmeli dinamik liste (`react-hook-form`'un `useFieldArray`'i). Kaydet `PUT /api/clubs/{id}/contact` çağırır, başarıda `['clubs', clubId]` sorgusunu invalidate eder.

- [ ] **Step 3: Gösterim**

Hem `ClubDetailPage` genel sekmesinde hem `PublicClubDetailPage`'de: e-posta `mailto:`, telefon `tel:` bağlantısı; sosyal bağlantılar platform ikonlu `IconButton`'lar (MUI `@mui/icons-material` içindeki `Instagram`, `LinkedIn`, `YouTube`, `Language` ikonları), her biri `target="_blank" rel="noopener noreferrer"`.

- [ ] **Step 4: Derle ve lint'le**

```bash
cd arayuz && npm run build && npm run lint
```

---

### Task 6: Tam doğrulama ve tek commit

- [ ] **Step 1: Tüm testler**

```bash
dotnet test
```

- [ ] **Step 2: Elle doğrula**

Başkan olarak iletişim bilgisi ve iki sosyal hesap kaydet; anonim pencerede kulübün genel sayfasında göründüklerini doğrula. `http://` ile bir bağlantı kaydetmeyi dene ve **reddedildiğini** gör (Y-80).

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "Faz 40: kulup iletisim bilgileri ve sosyal medya baglantilari"
```
