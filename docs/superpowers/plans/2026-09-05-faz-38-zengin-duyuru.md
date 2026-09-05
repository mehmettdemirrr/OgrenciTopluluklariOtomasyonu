# Faz 38 — Zengin Duyuru İçeriği Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Duyuru yazarken metin biçimlendirilebilsin — kalın, italik, altı çizili, üstü çizili, başlık, madde listesi, hizalama, bağlantı ve **renk** — ve duyuruya kapak görseli eklenebilsin. Yazan kişi bunu bir araç çubuğundan yapsın, işaretleme dili öğrenmek zorunda kalmasın.

**Architecture:** Üç karar bu fazın belkemiği:

1. **İçerik HTML olarak saklanmaz, yapısal JSON olarak saklanır.** Editör (TipTap/ProseMirror) belgeyi zaten bir düğüm ağacı olarak tutar; bunu olduğu gibi saklarız. Sunucu, yazma anında ağacı **izin listesine göre** dolaşır: tanımadığı düğüm, işaret veya öznitelik varsa isteği reddeder (fail-closed). Okuma tarafında HTML dizesi ayrıştırılmaz, `dangerouslySetInnerHTML` kullanılmaz — kendi render'ımız ağacı gezip React öğeleri üretir. Böylece "sanitize etmeyi unuttuğumuz bir okuma noktası" diye bir kategori hiç oluşmaz.
2. **Renk ham hex değil, anlamsal token'dır.** `arayuz/src/theme/tokens.ts` başlığındaki Y-56 "hex yalnızca burada" diyor ve uygulamanın koyu modu var (`surfaces.dark`). İçeriğe `#262F59` gömülürse koyu temada metin zeminle birleşir. Bu yüzden renk seçenekleri `accent | success | warning | danger | muted` gibi **isimlerdir**; hangi hex'e karşılık geldiklerine tema karar verir.
3. **Düz metin aynası korunur.** Mevcut `Announcement.Content` alanı silinmez; her kayıtta JSON'dan türetilen düz metin oraya yazılır. Arama (`GetFeedAsync`'in `search` parametresi), e-posta şablonları ve vitrindeki kısa önizleme bu alandan beslenmeye devam eder; eski duyurular `ContentJson` null olduğu için düz metin olarak render edilir — geriye dönük veri taşıma gerekmez.

**Tech Stack:** .NET 8, EF Core 8, System.Text.Json, React 18 + MUI, TipTap (`@tiptap/react`, `@tiptap/starter-kit`, `@tiptap/extension-underline`, `@tiptap/extension-link`, `@tiptap/extension-text-align`, `@tiptap/extension-text-style`).

**Spec:** `docs/MIMARI.md` (v6.4 → v6.5 bu fazda). İlgili mevcut kararlar: A-43 (null ClubId = sistem duyurusu), A-64 (tip kümesi çağrı yerine göre), A-68 (kulüp içi yetki matrisi), Y-56 (marka renkleri tek yerde), Y-57 (görünürlük açıkça seçilir).

## Global Constraints

- Belge önce, kod sonra (Task 1).
- Çalışma dalı `master`; faz başına **tek commit**; commit mesajında araç imzası/süreç anlatımı yok.
- Y-05/Y-09: `IFormFile` Business'a girmez.
- Y-40: dosya tipi magic byte'tan belirlenir. A-64: izin verilen küme çağrı yerine göre geçilir.
- Y-56: bileşen kodunda ham hex yazılmaz; renk daima tema token'ından gelir.
- A-68/Y-75: yetki matrisi yalnızca daraltır; controller'ın `[SecuredOperation]`'ı ilk kapı olarak kalır.
- Y-34: her test kendi verisini tohumlar.
- Test komutları: `dotnet test`; `cd arayuz && npm run build && npm run lint`.

---

### Task 1: MIMARI'ye kapsam, kararlar ve kuralı yaz (belge önce)

**Files:**
- Modify: `docs/MIMARI.md`

**Interfaces:**
- Produces: K-42, A-71, A-72, Y-78.

> **Not:** Bu faz **iki karar ama tek kural** üretir (A-71, A-72, Y-78). Bu yüzden sonraki fazlarda karar numaraları birer kayar, kural numaraları kaymaz: Faz 39 → A-73/Y-79, Faz 40 → A-74/Y-80, Faz 41 → A-75/Y-81. O planların Task 1'leri bu numaralarla yazılmıştır; ek bir düzeltme gerekmez.

- [ ] **Step 1: Sürüm ve sayaçlar**

v6.5 kaydını ekle; sayaçları **72 karar / 78 kural / 38 faz** yap.

- [ ] **Step 2: K-42**

```markdown
- **K-42 — Zengin duyuru:** Duyuru metni araç çubuğundan biçimlendirilebilir (kalın, italik,
  altı çizili, üstü çizili, başlık, madde/numara listesi, hizalama, bağlantı, renk) ve duyuruya
  bir kapak görseli eklenebilir. Görsel de biçimlendirme de isteğe bağlıdır.
```

- [ ] **Step 3: A-71**

```markdown
- **A-71 — Zengin metin yapısal JSON'dur, HTML değildir.** Duyuru ve etkinlik açıklaması,
  editörün düğüm ağacı olarak (`ContentJson`) saklanır; HTML dizesi ne veritabanına yazılır ne de
  arayüzde ayrıştırılır. Yazma anında sunucu ağacı izin listesine göre dolaşır, okuma anında
  arayüz ağacı gezip React öğeleri üretir. Gerekçe: HTML saklamak her okuma noktasında sanitize
  zorunluluğu doğurur; bir noktayı atlamak depolanmış XSS demektir. Ağaçta böyle bir kapı yoktur.
  Düz metin aynası (`Content`) korunur — arama, e-posta ve önizleme oradan okur; `ContentJson`
  null olan eski kayıtlar düz metin olarak render edilir.
```

- [ ] **Step 4: A-72**

```markdown
- **A-72 — İçerikteki renk anlamsal token'dır, hex değildir.** Yazar `accent | success | warning |
  danger | muted` arasından seçer; hangi hex'e karşılık geldiğine tema karar verir. Gerekçe:
  Y-56 hex'i tek yere hapsediyor ve uygulamanın koyu modu var — içeriğe gömülen sabit hex koyu
  temada zeminle birleşir. Serbest renk seçici bu iki nedenle sunulmaz.
```

- [ ] **Step 5: Y-78**

```markdown
- **Y-78 — İzin listesi kapalıdır ve kapı sunucudadır.** `ContentJson` yazılmadan önce sunucu
  ağacı dolaşır: izin listesinde olmayan düğüm tipi, işaret tipi, öznitelik veya renk token'ı
  isteği reddeder (fail-closed; sessizce temizleme YOK — reddedilen içerik kullanıcıya söylenir).
  Arayüzde `dangerouslySetInnerHTML` ve HTML ayrıştıran bir render yolu bulunamaz; ihlali mimari
  test yakalar.
```

---

### Task 2: Entity + migration

**Files:**
- Modify: `src/Entities/Announcement.cs`
- Create: `src/DataAccess/Migrations/<timestamp>_20260905_Faz38_ZenginDuyuru.cs` (EF üretir)
- Modify: `src/DataAccess/Concrete/EntityFramework/Contexts/ApplicationDbContext.cs`

**Interfaces:**
- Produces: `Announcement.ContentJson` (`string?`), `Announcement.ImageFileId` (`int?`).

- [ ] **Step 1: Alanları ekle**

`src/Entities/Announcement.cs`:

```csharp
    /// <summary>
    /// docs/MIMARI.md · K-42/A-71: biçimlendirilmiş içeriğin düğüm ağacı (JSON).
    /// Null = eski/düz metin duyuru; o durumda Content olduğu gibi gösterilir.
    /// Content bu alanın düz metin aynasıdır — arama ve e-posta oradan okur.
    /// </summary>
    public string? ContentJson { get; set; }

    /// <summary>docs/MIMARI.md · K-42: kapak görseli. Null = görselsiz duyuru.</summary>
    public int? ImageFileId { get; set; }
```

- [ ] **Step 2: Kolon tipini sabitle**

`ApplicationDbContext` içindeki `Announcement` yapılandırmasına:

```csharp
            entity.Property(e => e.ContentJson).HasColumnType("nvarchar(max)");
```

- [ ] **Step 3: Migration üret ve uygula**

```bash
dotnet ef migrations add 20260905_Faz38_ZenginDuyuru --project src/DataAccess --startup-project src/WebAPI
dotnet ef database update --project src/DataAccess --startup-project src/WebAPI
dotnet build
```
Beklenen: iki `AddColumn` (`ContentJson` nvarchar(max) null, `ImageFileId` int null), başka tabloya dokunmuyor.

---

### Task 3: İzin listesi doğrulayıcısı ve düz metin türetimi (fazın güvenlik çekirdeği)

**Files:**
- Create: `src/Business/RichText/RichTextSchema.cs`
- Create: `src/Business/RichText/RichTextDocumentValidator.cs`
- Create: `src/Business/RichText/RichTextPlainTextExtractor.cs`
- Modify: `src/Business/Constants/Messages.cs`
- Test: `tests/Business.Tests/RichTextDocumentValidatorTests.cs`

**Interfaces:**
- Produces:
  - `RichTextSchema.AllowedNodes`, `AllowedMarks`, `AllowedColorTokens`, `AllowedHeadingLevels`, `AllowedAlignments`
  - `RichTextDocumentValidator.Validate(string? json) → RichTextValidationResult { bool IsValid; string? Error; }`
  - `RichTextPlainTextExtractor.Extract(string json) → string`

- [ ] **Step 1: Başarısız testleri yaz**

`tests/Business.Tests/RichTextDocumentValidatorTests.cs`:

```csharp
private const string ValidDoc = """
{"type":"doc","content":[
  {"type":"paragraph","attrs":{"textAlign":"left"},"content":[
    {"type":"text","text":"Kayıtlar ","marks":[{"type":"bold"}]},
    {"type":"text","text":"15 Ekim","marks":[{"type":"textColor","attrs":{"token":"accent"}}]}
  ]}
]}
""";

[Fact]
public void Validate_AcceptsAllowedNodesMarksAndColorTokens()
{
    var result = RichTextDocumentValidator.Validate(ValidDoc);

    Assert.True(result.IsValid);
}

[Fact]
public void Validate_RejectsUnknownNodeType()
{
    var doc = """{"type":"doc","content":[{"type":"iframe","attrs":{"src":"https://evil.example"}}]}""";

    var result = RichTextDocumentValidator.Validate(doc);

    Assert.False(result.IsValid);
}

[Fact]
public void Validate_RejectsUnknownMarkType()
{
    var doc = """{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"x","marks":[{"type":"script"}]}]}]}""";

    Assert.False(RichTextDocumentValidator.Validate(doc).IsValid);
}

[Fact]
public void Validate_RejectsRawHexColor()
{
    // A-72: renk token'dır; hex kabul edilmez (koyu modda okunmaz hale gelir).
    var doc = """{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"x","marks":[{"type":"textColor","attrs":{"token":"#ff0000"}}]}]}]}""";

    Assert.False(RichTextDocumentValidator.Validate(doc).IsValid);
}

[Fact]
public void Validate_RejectsNonHttpsLink()
{
    var doc = """{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"x","marks":[{"type":"link","attrs":{"href":"javascript:alert(1)"}}]}]}]}""";

    Assert.False(RichTextDocumentValidator.Validate(doc).IsValid);
}

[Fact]
public void Validate_RejectsDocumentDeeperThanLimit()
{
    // Özyinelemeli gezinme yığın taşmasına sürüklenemez.
    var doc = BuildNestedBulletLists(depth: 40);

    Assert.False(RichTextDocumentValidator.Validate(doc).IsValid);
}

[Fact]
public void Validate_RejectsMalformedJson()
{
    Assert.False(RichTextDocumentValidator.Validate("{ not json").IsValid);
}

[Fact]
public void Extract_ProducesPlainTextMirror()
{
    var text = RichTextPlainTextExtractor.Extract(ValidDoc);

    Assert.Equal("Kayıtlar 15 Ekim", text);
}
```

- [ ] **Step 2: Testleri çalıştır, başarısız olduklarını gör**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~RichTextDocumentValidatorTests"
```
Beklenen: FAIL (tipler yok — derleme hatası).

- [ ] **Step 3: Şemayı yaz**

`src/Business/RichText/RichTextSchema.cs`:

```csharp
namespace Business.RichText;

/// <summary>
/// docs/MIMARI.md · A-71/A-72/Y-78: zengin metnin KAPALI izin listesi.
/// Buraya bir tip eklemek, arayüzdeki render'a da karşılık eklemeyi gerektirir —
/// sunucunun kabul edip arayüzün çizemediği bir düğüm, boş görünen bir duyuru demektir.
/// </summary>
public static class RichTextSchema
{
    public static readonly IReadOnlySet<string> AllowedNodes = new HashSet<string>(StringComparer.Ordinal)
    {
        "doc", "paragraph", "text", "heading", "bulletList", "orderedList", "listItem", "blockquote", "hardBreak",
    };

    public static readonly IReadOnlySet<string> AllowedMarks = new HashSet<string>(StringComparer.Ordinal)
    {
        "bold", "italic", "underline", "strike", "link", "textColor",
    };

    /// <summary>A-72: renk token'ları; hex DEĞİL. Karşılıkları arayüzdeki temadadır.</summary>
    public static readonly IReadOnlySet<string> AllowedColorTokens = new HashSet<string>(StringComparer.Ordinal)
    {
        "accent", "success", "warning", "danger", "muted",
    };

    public static readonly IReadOnlySet<string> AllowedAlignments = new HashSet<string>(StringComparer.Ordinal)
    {
        "left", "center", "right",
    };

    public static readonly IReadOnlySet<int> AllowedHeadingLevels = new HashSet<int> { 3, 4 };

    /// <summary>Yığın taşmasına ve devasa belgelere karşı sınırlar.</summary>
    public const int MaxDepth = 12;
    public const int MaxNodes = 2_000;
    public const int MaxJsonLength = 200_000;
}
```

- [ ] **Step 4: Doğrulayıcıyı yaz**

`src/Business/RichText/RichTextDocumentValidator.cs`: `JsonDocument.Parse` ile ağacı aç; kök `type == "doc"` olmalı. Özyinelemeli `ValidateNode(JsonElement node, int depth, ref int nodeCount)`:

- `depth > RichTextSchema.MaxDepth` → geçersiz.
- `type` yoksa veya `AllowedNodes` içinde değilse → geçersiz.
- `attrs` varsa yalnızca şu anahtarlara izin ver: `paragraph`/`heading` için `textAlign` (`AllowedAlignments`), `heading` için `level` (`AllowedHeadingLevels`). Başka anahtar → geçersiz.
- `marks` dizisindeki her işaret: `type` `AllowedMarks` içinde olmalı; `textColor` ise `attrs.token` `AllowedColorTokens` içinde; `link` ise `attrs.href` mutlak ve `Uri.UriSchemeHttps` olmalı, `target`/`rel` dahil başka öznitelik kabul edilmez.
- `text` düğümünde `text` alanı string olmalı.
- `content` dizisi varsa her çocuk için özyinele.
- `nodeCount > MaxNodes` → geçersiz.

Hata durumunda `RichTextValidationResult.Invalid(Messages.UnsupportedRichTextContent)` dön; **sessizce temizleme yok** (Y-78).

- [ ] **Step 5: Düz metin türetimini yaz**

`RichTextPlainTextExtractor.Extract`: ağacı gez, `text` düğümlerinin `text` değerlerini topla; blok düğümler (`paragraph`, `heading`, `listItem`, `blockquote`) arasına `\n` koy; `hardBreak` için `\n`; sonuçta ardışık boşlukları tek boşluğa indir ve `Trim()` uygula.

- [ ] **Step 6: Mesaj sabitini ekle**

`src/Business/Constants/Messages.cs`:

```csharp
    public const string UnsupportedRichTextContent = "Duyuru içeriğinde desteklenmeyen biçimlendirme var.";
    public const string ImageUploaded = "Görsel yüklendi.";
```

- [ ] **Step 7: Testleri çalıştır, geçtiklerini gör**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~RichTextDocumentValidatorTests"
```
Beklenen: PASS (sekizinin tamamı).

---

### Task 4: Duyuru yazma akışına zengin içeriği bağla

**Files:**
- Modify: `src/Business/DTOs/Announcements/CreateAnnouncementRequestDto.cs`
- Modify: `src/Business/DTOs/Announcements/UpdateAnnouncementRequestDto.cs`
- Modify: `src/Business/DTOs/Announcements/AnnouncementListItemDto.cs`
- Modify: `src/Business/DTOs/Public/PublicAnnouncementListItemDto.cs`
- Modify: `src/Business/Concrete/AnnouncementManager.cs`
- Test: `tests/Business.Tests/AnnouncementManagerTests.cs`

**Interfaces:**
- Consumes: `RichTextDocumentValidator`, `RichTextPlainTextExtractor` (Task 3).
- Produces: `CreateAnnouncementRequestDto.ContentJson`, aynı alan `Update`'te; `AnnouncementListItemDto.ContentJson`, `PublicAnnouncementListItemDto.ContentJson`.

- [ ] **Step 1: Başarısız testleri yaz**

```csharp
[Fact]
public async Task CreateAsync_StoresContentJson_AndDerivesPlainText()
{
    SeedMembershipWithCapabilities(clubId: 1, ClubCapability.AnnouncementsManage);

    var result = await sut.CreateAsync(1, new CreateAnnouncementRequestDto
    {
        Title = "Kayıtlar açıldı",
        ContentJson = ValidDoc,          // "Kayıtlar 15 Ekim"
        Visibility = AnnouncementVisibility.Public,
    });

    Assert.True(result.IsSuccess);
    var announcement = await context.Announcements.SingleAsync();
    Assert.NotNull(announcement.ContentJson);
    Assert.Equal("Kayıtlar 15 Ekim", announcement.Content);
}

[Fact]
public async Task CreateAsync_Rejects_WhenContentJsonHasDisallowedNode()
{
    SeedMembershipWithCapabilities(clubId: 1, ClubCapability.AnnouncementsManage);

    var result = await sut.CreateAsync(1, new CreateAnnouncementRequestDto
    {
        Title = "Kötü",
        ContentJson = """{"type":"doc","content":[{"type":"iframe"}]}""",
        Visibility = AnnouncementVisibility.Public,
    });

    Assert.False(result.IsSuccess);
    Assert.False(await context.Announcements.AnyAsync());
}

[Fact]
public async Task CreateAsync_AcceptsPlainContent_WhenContentJsonIsNull()
{
    // Geriye dönük: düz metin duyuru hâlâ yazılabilir.
    SeedMembershipWithCapabilities(clubId: 1, ClubCapability.AnnouncementsManage);

    var result = await sut.CreateAsync(1, new CreateAnnouncementRequestDto
    {
        Title = "Düz", Content = "Sadece metin", Visibility = AnnouncementVisibility.Public,
    });

    Assert.True(result.IsSuccess);
    Assert.Null((await context.Announcements.SingleAsync()).ContentJson);
}
```

- [ ] **Step 2: Çalıştır, başarısız olduklarını gör**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~AnnouncementManagerTests"
```

- [ ] **Step 3: DTO alanlarını ekle**

`Create`/`Update` istek DTO'larına:

```csharp
    /// <summary>docs/MIMARI.md · K-42/A-71: biçimlendirilmiş içerik ağacı. Null = düz metin duyuru.</summary>
    public string? ContentJson { get; set; }
```

Liste DTO'larına (hem yönetim hem vitrin):

```csharp
    /// <summary>docs/MIMARI.md · A-71: null ise Content düz metin olarak gösterilir.</summary>
    public string? ContentJson { get; set; }

    /// <summary>docs/MIMARI.md · K-42: kapak görseli.</summary>
    public int? ImageFileId { get; set; }
```

- [ ] **Step 4: `CreateAsync` ve `UpdateAsync`'e doğrulamayı ekle**

Mevcut yetki kapısından **sonra**, kayıt kurulmadan önce:

```csharp
        // Y-78: izin listesi kapalıdır; reddedilen içerik sessizce temizlenmez, hata döner.
        var content = request.Content?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(request.ContentJson))
        {
            var validation = RichTextDocumentValidator.Validate(request.ContentJson);
            if (!validation.IsValid)
            {
                return Result.ValidationError(validation.Error ?? Messages.UnsupportedRichTextContent);
            }

            // A-71: düz metin aynası JSON'dan türetilir — arama ve e-posta bu alanı okur.
            content = RichTextPlainTextExtractor.Extract(request.ContentJson);
        }
```

ve varlık kurulurken `Content = content, ContentJson = request.ContentJson`.

- [ ] **Step 5: Tüm DTO kurulum noktalarını doldur**

```bash
grep -rn "new AnnouncementListItemDto\|new PublicAnnouncementListItemDto" src/Business
```
Bulunan **her** noktaya `ContentJson = a.ContentJson,` ve `ImageFileId = a.ImageFileId,` ekle. (Faz 35'te `EventListItemDto`'nun ikinci kurulum yeri atlanmış ve alan bir uçta sessizce kaybolmuştu; bu adım o hatanın tekrarını önler.)

- [ ] **Step 6: Testleri çalıştır**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~AnnouncementManagerTests"
```
Beklenen: PASS.

---

### Task 5: Kapak görseli yükleme ucu

**Files:**
- Modify: `src/Business/Abstract/IFileService.cs`
- Modify: `src/Business/Concrete/FileManager.cs`
- Modify: `src/WebAPI/Controllers/FilesController.cs`
- Test: `tests/Business.Tests/FileManagerTests.cs`

**Interfaces:**
- Produces: `IFileService.UploadAnnouncementImageAsync(int announcementId, UploadFileRequestDto, CancellationToken)`, `POST /api/announcements/{id}/image`.

- [ ] **Step 1: Başarısız testleri yaz**

```csharp
[Fact]
public async Task UploadAnnouncementImageAsync_Rejects_WhenCallerCannotManageAnnouncements()
{
    var announcementId = await SeedClubAnnouncementAsync(clubId: 1);
    SeedMembershipWithCapabilities(clubId: 1, ClubCapability.MembersView); // AnnouncementsManage YOK

    var result = await sut.UploadAnnouncementImageAsync(announcementId, PngUpload());

    Assert.Equal(ResultStatus.Forbidden, result.Status);
}

[Fact]
public async Task UploadAnnouncementImageAsync_StoresImage_WhenCallerCanManageAnnouncements()
{
    var announcementId = await SeedClubAnnouncementAsync(clubId: 1);
    SeedMembershipWithCapabilities(clubId: 1, ClubCapability.AnnouncementsManage);

    var result = await sut.UploadAnnouncementImageAsync(announcementId, PngUpload());

    Assert.True(result.IsSuccess);
    Assert.NotNull((await context.Announcements.SingleAsync()).ImageFileId);
}

[Fact]
public async Task UploadAnnouncementImageAsync_Rejects_PdfDisguisedAsPng()
{
    // Y-40: tip uzantıdan değil magic byte'tan belirlenir.
    var announcementId = await SeedClubAnnouncementAsync(clubId: 1);
    SeedMembershipWithCapabilities(clubId: 1, ClubCapability.AnnouncementsManage);

    var result = await sut.UploadAnnouncementImageAsync(announcementId, PdfBytesNamed("kapak.png"));

    Assert.False(result.IsSuccess);
}
```

- [ ] **Step 2: Çalıştır, başarısız olduklarını gör**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~UploadAnnouncementImage"
```

- [ ] **Step 3: Servis imzasını ekle**

```csharp
    /// <summary>
    /// docs/MIMARI.md · K-42/A-64: duyuru kapak görseli — yalnızca JPEG/PNG/WebP, Public.
    /// Yetki: duyuruyu yönetebilen yönetir (kulüp duyurusunda A-68 AnnouncementsManage,
    /// sistem duyurusunda announcements.global).
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.FilesUpload)]
    [TransactionAspect]
    Task<IDataResult<UploadedFileDto>> UploadAnnouncementImageAsync(
        int announcementId, UploadFileRequestDto request, CancellationToken cancellationToken = default);
```

- [ ] **Step 4: `FileManager`'da gerçekleştir**

Duyuruyu bul (yoksa `NotFound`). Yetki kapısı, `AnnouncementManager.cs:160-225` aralığındaki mevcut kapının **aynısıdır**; ikinci bir kopya yazmak yerine o mantığı tek bir yerden çağır: kapı `AnnouncementManager`'ın özel metodundaysa, `IAnnouncementService`'e `Task<IResult> EnsureCanManageAsync(int announcementId, CancellationToken)` olarak çıkar ve `FileManager` onu çağırsın. Ardından:

```csharp
        var stored = await StoreFileAsync(request, FileVisibility.Public, ImageTypes, Messages.UnsupportedFileType, cancellationToken).ConfigureAwait(false);
        if (!stored.IsSuccess)
        {
            return stored;
        }

        announcement.ImageFileId = stored.Data.FileId;
        announcementRepository.Update(announcement);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return DataResult<UploadedFileDto>.Success(stored.Data, Messages.ImageUploaded);
```

- [ ] **Step 5: Controller ucu (kulüp logosu deseniyle birebir)**

```csharp
    [HttpPost("announcements/{announcementId:int}/image")]
    [RequestSizeLimit(5_242_880)]
    [RequestFormLimits(MultipartBodyLengthLimit = 5_242_880)]
    public async Task<IActionResult> UploadAnnouncementImage(int announcementId, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await fileService.UploadAnnouncementImageAsync(
            announcementId,
            new UploadFileRequestDto { Content = stream, OriginalFileName = file.FileName, Length = file.Length },
            cancellationToken);

        return result.ToActionResult();
    }
```

- [ ] **Step 6: Testleri çalıştır**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~UploadAnnouncementImage"
```
Beklenen: PASS.

---

### Task 6: Arayüz — editör, render ve renk token'ları

**Files:**
- Modify: `arayuz/package.json`
- Create: `arayuz/src/components/richtext/richTextColors.ts`
- Create: `arayuz/src/components/richtext/TextColorMark.ts`
- Create: `arayuz/src/components/richtext/RichTextEditor.tsx`
- Create: `arayuz/src/components/richtext/RichTextContent.tsx`
- Modify: `arayuz/src/theme/tokens.ts`
- Modify: `arayuz/src/pages/AnnouncementsPage.tsx`
- Modify: `arayuz/src/pages/ClubDetailAnnouncementsTab.tsx`
- Modify: `arayuz/src/components/ui/AnnouncementCard.tsx`
- Modify: `arayuz/src/api/types.ts`

**Interfaces:**
- Consumes: `contentJson`, `imageFileId` (Task 4), `POST /api/announcements/{id}/image` (Task 5).
- Produces: `<RichTextEditor value onChange />`, `<RichTextContent json fallbackText />` — **Faz 39 bu iki bileşeni aynen kullanır.**

- [ ] **Step 1: Bağımlılıkları kur**

```bash
cd arayuz && npm install @tiptap/react @tiptap/pm @tiptap/starter-kit @tiptap/extension-underline @tiptap/extension-link @tiptap/extension-text-align @tiptap/extension-text-style
```

- [ ] **Step 2: Renk token'larını temaya ekle**

`arayuz/src/theme/tokens.ts` sonuna (Y-56: hex yalnızca bu dosyada):

```ts
/** docs/MIMARI.md · A-72: içerik renkleri anlamsal token'dır; hex burada, iki mod için ayrı. */
export const richTextColors = {
  light: {
    accent: brand.turquoiseDark,
    success: '#2E7D32',
    warning: '#B45309',
    danger: '#C62828',
    muted: brand.greyDark,
  },
  dark: {
    accent: brand.turquoise,
    success: '#7BC67E',
    warning: '#E5A13A',
    danger: '#F28B82',
    muted: surfaces.dark.textMuted,
  },
} as const

export type RichTextColorToken = keyof typeof richTextColors.light
```

- [ ] **Step 3: Renk işaretini (mark) yaz**

`TextColorMark.ts`: TipTap `Mark.create({ name: 'textColor' })`; tek öznitelik `token`; `parseHTML`/`renderHTML` **kullanılmaz** çünkü belge HTML'e çevrilmiyor — sadece `addAttributes` ve komut (`setTextColor(token)`, `unsetTextColor()`). İzin verilen token listesi `richTextColors.light`'ın anahtarlarından türetilir; sunucudaki `AllowedColorTokens` ile **aynı** beş isim olmalıdır (biri diğerine eklenirse öteki reddeder).

- [ ] **Step 4: `RichTextContent` render'ını yaz**

```tsx
/**
 * docs/MIMARI.md · A-71/Y-78: HTML ayrıştırılmaz, dangerouslySetInnerHTML kullanılmaz.
 * Ağaç gezilir, izin listesindeki düğümler React öğesine çevrilir; tanınmayan düğüm ATLANIR.
 */
export function RichTextContent({ json, fallbackText }: { json: string | null; fallbackText: string }) {
  const doc = useMemo(() => safeParse(json), [json])
  if (!doc) {
    return <Typography variant="body2" sx={{ whiteSpace: 'pre-line', lineHeight: 1.7 }}>{fallbackText}</Typography>
  }
  return <Box sx={{ '& > :first-of-type': { mt: 0 }, '& > :last-child': { mb: 0 } }}>{renderNodes(doc.content)}</Box>
}
```

`renderNodes` `switch (node.type)` ile yalnızca `paragraph | heading | bulletList | orderedList | listItem | blockquote | hardBreak | text` üretir; `text` düğümünde işaretler sırayla sarılır (`bold` → `<strong>`, `italic` → `<em>`, `underline`/`strike` → `Box component="span"` + `textDecoration`, `link` → MUI `Link` (`target="_blank" rel="noopener noreferrer"`, `href` https değilse **bağlantı üretme, düz metin bas**), `textColor` → `color: richTextColors[mode][token]`, token tanınmıyorsa renk uygulama).

`safeParse` `try/catch` ile `JSON.parse` yapar ve kök `type === 'doc'` değilse `null` döner — bozuk kayıt ekranı çökertmez, düz metne düşer.

- [ ] **Step 5: `RichTextEditor`'ı yaz**

`useEditor` ile `StarterKit` (heading seviyeleri 3-4 ile sınırlı: `heading: { levels: [3, 4] }`), `Underline`, `Link` (`protocols: ['https']`, `autolink: false`), `TextAlign` (`types: ['paragraph', 'heading']`), `TextStyle`, `TextColorMark`. Üstünde MUI `ToggleButtonGroup` araç çubuğu: kalın, italik, altı çizili, üstü çizili, H3, H4, madde listesi, numaralı liste, hizalama (sol/orta/sağ), bağlantı ekle/kaldır, renk (beş token'lı küçük menü) ve "biçimi temizle". `onChange`, `editor.getJSON()` çıktısını `JSON.stringify` ile üst bileşene verir.

**Dikkat:** `StarterKit`'in varsayılan olarak açtığı `codeBlock`, `horizontalRule` ve `image` uzantılarını **kapat** (`codeBlock: false` vb.) — sunucudaki izin listesinde yoklar; açık kalırlarsa kullanıcı yazar, kaydet düğmesi hata verir.

- [ ] **Step 6: Formları ve gösterimi bağla**

- `AnnouncementsPage.tsx` (sistem duyurusu) ve `ClubDetailAnnouncementsTab.tsx` (kulüp duyurusu): içerik alanını `RichTextEditor` ile değiştir; kaydederken `contentJson` gönder. Kayıttan sonra görsel seçildiyse `POST /api/announcements/{id}/image` çağır ve ilgili sorguları invalidate et (mevcut afiş yükleme akışının `EventDetailPage`'deki deseni).
- `AnnouncementCard.tsx`: metni `<RichTextContent json={contentJson} fallbackText={content} />` ile bas; `imageFileId` doluysa kartın üstüne `CardMedia` (yükseklik 160, `objectFit: 'cover'`, `src={`/api/files/${imageFileId}`}`) ekle.
- `api/types.ts`: `AnnouncementListItemDto` ve `PublicAnnouncementListItemDto`'ya `contentJson: string | null`, `imageFileId: number | null`.

- [ ] **Step 7: Derle ve lint'le**

```bash
cd arayuz && npm run build && npm run lint
```

---

### Task 7: Y-78'i mimari testle koru

**Files:**
- Test: `tests/Architecture.Tests/RichTextSafetyTests.cs`

- [ ] **Step 1: Testi yaz**

```csharp
/// <summary>
/// docs/MIMARI.md · Y-78: zengin içerik HTML olarak basılamaz.
/// .NET tarafında karşılığı olmayan bu kuralın tek bekçisi kaynak taramasıdır.
/// </summary>
[Fact]
public void Frontend_NeverInjectsHtml()
{
    var root = LocateRepositoryRoot();
    var offenders = Directory
        .EnumerateFiles(Path.Combine(root, "arayuz", "src"), "*.ts*", SearchOption.AllDirectories)
        .Where(path => File.ReadAllText(path).Contains("dangerouslySetInnerHTML", StringComparison.Ordinal))
        .Select(path => Path.GetRelativePath(root, path))
        .ToList();

    Assert.True(offenders.Count == 0, $"Y-78 ihlali: {string.Join(", ", offenders)}");
}

/// <summary>
/// Şema iki yerde yaşıyor (sunucu izin listesi + arayüz render'ı); renk token'ları ayrışırsa
/// kullanıcı arayüzde seçtiği rengi kaydedemez. Bu test iki listeyi karşılaştırır.
/// </summary>
[Fact]
public void ColorTokens_MatchBetweenServerAndFrontend()
{
    var root = LocateRepositoryRoot();
    var frontendTokens = ExtractRichTextColorTokens(Path.Combine(root, "arayuz", "src", "theme", "tokens.ts"));

    Assert.Equal(RichTextSchema.AllowedColorTokens.OrderBy(x => x), frontendTokens.OrderBy(x => x));
}
```

- [ ] **Step 2: Testlerin gerçekten ısırdığını kanıtla**

`RichTextContent.tsx` içine geçici olarak `// dangerouslySetInnerHTML` satırını ekle → ilk test **FAIL** vermeli. `tokens.ts`'teki `richTextColors.light`'tan bir token'ı geçici olarak sil → ikinci test **FAIL** vermeli. İkisini de geri al.

```bash
dotnet test tests/Architecture.Tests --filter "FullyQualifiedName~RichTextSafetyTests"
```

- [ ] **Step 3: Temiz halde çalıştır**

Beklenen: PASS.

---

### Task 8: Tam doğrulama ve tek commit

- [ ] **Step 1: Tüm testler**

```bash
dotnet test
```
Beklenen: tamamı PASS.

- [ ] **Step 2: Frontend derlemesi**

```bash
cd arayuz && npm run build
```

- [ ] **Step 3: Elle doğrula**

- Kulüp yetkilisi olarak kalın, renkli, listeli ve bağlantılı bir duyuru yaz; kapak görseli ekle. Vitrinde ve duyuru listesinde doğru göründüğünü doğrula.
- **Koyu moda geç**: renkli metnin hâlâ okunabildiğini gör (A-72'nin sınavı budur).
- Eski (düz metin) bir duyurunun bozulmadan göründüğünü doğrula.
- Tarayıcı konsolundan `contentJson` alanına elle `{"type":"doc","content":[{"type":"iframe"}]}` göndermeyi dene; isteğin **reddedildiğini** ve kaydın oluşmadığını gör (Y-78).

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Faz 38: duyurularda bicimlendirilmis icerik ve kapak gorseli"
```
