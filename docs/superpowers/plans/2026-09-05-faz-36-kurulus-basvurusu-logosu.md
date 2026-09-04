# Faz 36 — Kuruluş Başvurusunda Logo Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Topluluk kurma başvurusuna, evraklarla aynı istekte opsiyonel bir logo yüklenebilsin; başvuru onaylanınca bu logo yeni kulübün logosu olsun.

**Architecture:** Logo, Faz 33'ün evrak yükleme akışının aynısını izler: `IFormFile` WebAPI katmanında kalır (Y-05/Y-09), Business `UploadFileRequestDto` görür. Evraktan tek farkı görünürlük ve tip kümesidir — logo **Public** ve yalnızca JPEG/PNG/WebP (A-64), evrak Protected ve yalnızca PDF. Dosya başvuru anında saklanır, `ClubApplication.LogoFileId`'de tutulur ve **yalnızca onay dalında** `Club.LogoFileId`'ye kopyalanır; reddedilen başvurunun logosu hiçbir kulübe geçmez.

**Tech Stack:** .NET 8, EF Core 8 (MSSQL LocalDB), Autofac aspect'leri, FluentValidation, React 18 + Vite + TypeScript + MUI, TanStack Query.

**Spec:** `docs/MIMARI.md` (v6.2 → v6.3 bu fazda). Mevcut emsal: Faz 33 planı `docs/superpowers/plans/2026-08-26-faz-33-kurulus-evraklari.md`.

## Global Constraints

- Belge önce, kod sonra: `docs/MIMARI.md` güncellenmeden kod yazılmaz (Task 1).
- Çalışma dalı `master`; feature branch açılmaz. Faz başına **tek commit** (task başına değil).
- Commit mesajında `Co-Authored-By` veya araç imzası yer almaz; süreçten/araçtan bahsedilmez.
- Y-05/Y-09: `IFormFile` Business katmanına girmez; controller stream'i açar, `UploadFileRequestDto` kullanır.
- Y-40: dosya tipi uzantıdan değil **magic byte**'tan belirlenir (`StoreFileAsync` bunu zaten yapar).
- A-64: izin verilen tip kümesi global değildir, çağrı yerine göre geçilir (`ImageTypes` / `DocumentTypes`).
- Y-70: kişisel veri içeren dosyalar Protected'tır. Logo kişisel veri değildir, kulüp kimliğidir → Public (mevcut `UploadClubLogoAsync` ile aynı).
- Y-34: her test kendi verisini tohumlar; başka testin bıraktığı satıra güvenilmez.
- Aspect sırası: `SecuredOperation` → `ValidationAspect` → `TransactionAspect` → `CacheAspect`/`CacheRemoveAspect`.
- Test komutları: `dotnet test` (kök), frontend için `npm run build` ve `npm run lint` (`arayuz/`).

---

### Task 1: MIMARI'ye kapsam ve kararı yaz (belge önce)

**Files:**
- Modify: `docs/MIMARI.md`

**Interfaces:**
- Produces: K-40, A-69, Y-76 numaraları — sonraki tüm task'lar kod yorumlarında bu numaralara atıf yapar.

- [ ] **Step 1: Sürüm satırını ve sayaçları güncelle**

`docs/MIMARI.md:3-17` aralığındaki sürüm bloğuna v6.3 kaydını ekle; "68 karar / 75 kural / 35 faz" sayaçlarını **69 karar / 76 kural / 36 faz** yap.

- [ ] **Step 2: K-40 kapsam maddesini ekle**

```markdown
- **K-40 — Kuruluş başvurusunda logo:** Öğrenci, topluluk kurma başvurusunda önerilen logoyu
  evraklarla aynı istekte yükleyebilir. Zorunlu değildir; başvuru logosuz da geçerlidir.
```

- [ ] **Step 3: A-69 kararını ekle**

```markdown
- **A-69 — Başvuru logosu bir kez saklanır, onayda kopyalanır.** Logo, başvuru anında Public
  görünürlükte saklanır ve `ClubApplication.LogoFileId`'de tutulur. Onay dalında `Club.LogoFileId`
  bu değeri **aynı dosyayı işaret ederek** alır; ikinci bir yükleme veya dosya kopyası yapılmaz.
  Gerekçe: dosya deposunda tek kayıt kalır, onay anında yükleme hatası riski doğmaz.
```

- [ ] **Step 4: Y-76 kuralını ekle**

```markdown
- **Y-76 — Reddedilen başvurunun logosu hiçbir kulübe geçmez.** `Club.LogoFileId` ataması
  YALNIZCA `DecideAsync`'in `ApplicationStatus.Approved` dalında yapılır. Logoyu başvuru
  kaydından okuyup koşulsuz kopyalayan bir satır yazılamaz.
```

- [ ] **Step 5: Commit yok — Task 6 ile birlikte tek commit atılacak**

Bu fazda commit yalnızca sonda atılır (Global Constraints).

---

### Task 2: Entity + migration

**Files:**
- Modify: `src/Entities/ClubApplication.cs`
- Create: `src/DataAccess/Migrations/<timestamp>_20260905_Faz36_BasvuruLogosu.cs` (EF üretir)

**Interfaces:**
- Produces: `ClubApplication.LogoFileId` (`int?`) — Task 3 ve Task 4 bu alanı okur/yazar.

- [ ] **Step 1: Alanı ekle**

`src/Entities/ClubApplication.cs` içinde `ProposedCategoryId`'nin hemen altına:

```csharp
    /// <summary>docs/MIMARI.md · K-40/A-69: önerilen logo. Null = logosuz başvuru (geçerli).</summary>
    public int? LogoFileId { get; set; }
```

- [ ] **Step 2: Migration üret**

```bash
dotnet ef migrations add 20260905_Faz36_BasvuruLogosu --project src/DataAccess --startup-project src/WebAPI
```

- [ ] **Step 3: Migration'ı gözden geçir**

Üretilen dosya tek bir `AddColumn<int>(name: "LogoFileId", table: "ClubApplications", nullable: true)` içermeli. Başka tabloya dokunuyorsa migration'ı sil, model değişikliğini düzelt, yeniden üret.

- [ ] **Step 4: Veritabanına uygula ve derle**

```bash
dotnet ef database update --project src/DataAccess --startup-project src/WebAPI
dotnet build
```
Beklenen: derleme hatasız.

---

### Task 3: Business — logoyu sakla ve onayda kopyala

**Files:**
- Modify: `src/Business/Abstract/IFileService.cs`
- Modify: `src/Business/Concrete/FileManager.cs:138-141`
- Modify: `src/Business/DTOs/ClubApplications/SubmitClubApplicationRequestDto.cs`
- Modify: `src/Business/DTOs/ClubApplications/ClubApplicationListItemDto.cs`
- Modify: `src/Business/Concrete/ClubApplicationManager.cs:133-170` (SubmitAsync), `:436-479` (DecideAsync onay dalı), `:240-255` ve `:510-528` (DTO eşlemeleri)
- Test: `tests/Business.Tests/ClubApplicationManagerTests.cs`

**Interfaces:**
- Consumes: `ClubApplication.LogoFileId` (Task 2).
- Produces:
  - `IFileService.StoreApplicationLogoAsync(UploadFileRequestDto, CancellationToken) → Task<IDataResult<UploadedFileDto>>`
  - `SubmitClubApplicationRequestDto.Logo` (`UploadFileRequestDto?`)
  - `ClubApplicationListItemDto.LogoFileId` (`int?`) — Task 5 arayüzde okur.

- [ ] **Step 1: Başarısız testleri yaz**

`tests/Business.Tests/ClubApplicationManagerTests.cs` içine (Y-34: her test kendi verisini tohumlar):

```csharp
[Fact]
public async Task SubmitAsync_StoresLogo_WhenProvided()
{
    // Arrange: öğrenci + güncel dönem + danışman tohumla (mevcut SeedAsync yardımcısıyla)
    var request = BuildValidSubmitRequest();
    request.Logo = new UploadFileRequestDto
    {
        Content = new MemoryStream(PngBytes),
        OriginalFileName = "logo.png",
        Length = PngBytes.Length,
    };

    var result = await sut.SubmitAsync(request);

    Assert.True(result.IsSuccess);
    var application = await context.ClubApplications.SingleAsync();
    Assert.NotNull(application.LogoFileId);
}

[Fact]
public async Task DecideAsync_CopiesLogoToClub_WhenApproved()
{
    var applicationId = await SeedPendingApplicationWithLogoAsync(logoFileId: 42);

    var result = await sut.DecideAsync(applicationId, new DecideClubApplicationRequestDto { Status = ApplicationStatus.Approved });

    Assert.True(result.IsSuccess);
    var club = await context.Clubs.SingleAsync();
    Assert.Equal(42, club.LogoFileId);
}

[Fact]
public async Task DecideAsync_DoesNotCreateClub_WhenRejected_SoLogoNeverTravels()
{
    var applicationId = await SeedPendingApplicationWithLogoAsync(logoFileId: 42);

    await sut.DecideAsync(applicationId, new DecideClubApplicationRequestDto { Status = ApplicationStatus.Rejected, ReviewNote = "Eksik" });

    Assert.False(await context.Clubs.AnyAsync());
}
```

- [ ] **Step 2: Testleri çalıştır, başarısız olduklarını gör**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~ClubApplicationManagerTests"
```
Beklenen: FAIL — `Logo` ve `LogoFileId` üyeleri yok (derleme hatası).

- [ ] **Step 3: `IFileService`'e logo saklama metodunu ekle**

`src/Business/Abstract/IFileService.cs` içine, `StoreApplicationDocumentAsync`'in yanına:

```csharp
    /// <summary>
    /// docs/MIMARI.md · K-40/A-64: başvuru logosu — yalnızca JPEG/PNG/WebP, Public.
    /// Controller ucu YOKTUR; yalnızca ClubApplicationManager çağırır (başvuru akışının parçası),
    /// bu yüzden [SecuredOperation] da yok — yetki SubmitAsync'te zaten kurulmuş durumda.
    /// Kulüp henüz yoktur; bu yüzden UploadClubLogoAsync'in danışman kontrolü BURADA YAPILAMAZ.
    /// </summary>
    Task<IDataResult<UploadedFileDto>> StoreApplicationLogoAsync(
        UploadFileRequestDto request, CancellationToken cancellationToken = default);
```

- [ ] **Step 4: `FileManager`'a gerçekleştirmeyi ekle**

`src/Business/Concrete/FileManager.cs`, `StoreApplicationDocumentAsync`'in hemen altına:

```csharp
    public Task<IDataResult<UploadedFileDto>> StoreApplicationLogoAsync(
        UploadFileRequestDto request, CancellationToken cancellationToken = default) =>
        // A-64: ImageTypes = JPEG/PNG/WebP. Logo kulüp kimliğidir, kişisel veri değil → Public.
        StoreFileAsync(request, FileVisibility.Public, ImageTypes, Messages.UnsupportedFileType, cancellationToken);
```

- [ ] **Step 5: DTO'lara alanları ekle**

`SubmitClubApplicationRequestDto.cs`:

```csharp
    /// <summary>docs/MIMARI.md · K-40: opsiyonel logo. Null = logosuz başvuru.</summary>
    public UploadFileRequestDto? Logo { get; set; }
```

`ClubApplicationListItemDto.cs`:

```csharp
    /// <summary>docs/MIMARI.md · K-40: başvuruyla gelen logo. Null = yüklenmemiş.</summary>
    public int? LogoFileId { get; set; }
```

- [ ] **Step 6: `SubmitAsync`'te logoyu sakla**

`src/Business/Concrete/ClubApplicationManager.cs`, `application` nesnesi eklendikten ve ilk `SaveChangesAsync` çağrıldıktan **sonra**, evrak döngüsünden **önce**:

```csharp
        if (request.Logo is not null)
        {
            // A-69: logo bir kez saklanır; onayda Club.LogoFileId aynı dosyayı işaret eder.
            // Tip hatası burada yakalanır ve başvuru [TransactionAspect] sayesinde geri alınır.
            var storedLogo = await fileService.StoreApplicationLogoAsync(request.Logo, cancellationToken).ConfigureAwait(false);
            if (!storedLogo.IsSuccess)
            {
                return Result.ValidationError(storedLogo.Message ?? Messages.UnsupportedFileType);
            }

            application.LogoFileId = storedLogo.Data.FileId;
            clubApplicationRepository.Update(application);
        }
```

- [ ] **Step 7: `DecideAsync` onay dalında kopyala**

`src/Business/Concrete/ClubApplicationManager.cs:438-446`, `new Club { ... }` başlatıcısına tek satır:

```csharp
                        // Y-76: bu atama YALNIZCA Approved dalındadır — reddedilen başvurunun logosu geçmez.
                        LogoFileId = application.LogoFileId,
```

- [ ] **Step 8: İki DTO eşleme yerini de güncelle**

`ClubApplicationManager.cs` içinde `ClubApplicationListItemDto` **iki ayrı yerde** kuruluyor (`:240-255` ve `:510-528`). İkisine de ekle — birini atlamak, listenin bir ucunda logonun kaybolması demektir:

```csharp
            LogoFileId = a.LogoFileId,
```

- [ ] **Step 9: Testleri çalıştır, geçtiklerini gör**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~ClubApplicationManagerTests"
```
Beklenen: PASS.

---

### Task 4: WebAPI — multipart forma logo alanı

**Files:**
- Modify: `src/WebAPI/Models/SubmitClubApplicationForm.cs`
- Modify: `src/WebAPI/Controllers/ClubApplicationsController.cs:20-71`
- Test: `tests/WebAPI.IntegrationTests/ClubApplicationTests.cs`

**Interfaces:**
- Consumes: `SubmitClubApplicationRequestDto.Logo` (Task 3).
- Produces: `POST /api/club-applications` çok parçalı istekte `Logo` alanı.

- [ ] **Step 1: Başarısız entegrasyon testini yaz**

`tests/WebAPI.IntegrationTests/ClubApplicationTests.cs` içine (mevcut multipart yardımcı desenini izle):

```csharp
[Fact]
public async Task Submit_WithLogo_StoresLogoFileId()
{
    var (client, _) = await SeedScenarioAsync();
    using var content = BuildValidMultipartContent();
    content.Add(new ByteArrayContent(PngBytes) { Headers = { ContentType = new MediaTypeHeaderValue("image/png") } }, "Logo", "logo.png");

    var response = await client.PostAsync("/api/club-applications", content);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var list = await client.GetFromJsonAsync<JsonElement>("/api/club-applications?pageIndex=0&pageSize=10");
    var first = list.GetProperty("items")[0];
    Assert.True(first.GetProperty("logoFileId").ValueKind is JsonValueKind.Number);
}
```

- [ ] **Step 2: Testi çalıştır, başarısız olduğunu gör**

```bash
dotnet test tests/WebAPI.IntegrationTests --filter "FullyQualifiedName~Submit_WithLogo"
```
Beklenen: FAIL — `logoFileId` `Null` geliyor.

- [ ] **Step 3: Form modeline alanı ekle**

`src/WebAPI/Models/SubmitClubApplicationForm.cs`:

```csharp
    /// <summary>docs/MIMARI.md · K-40: opsiyonel logo. Y-05: IFormFile bu katmanda kalır.</summary>
    public IFormFile? Logo { get; set; }
```

- [ ] **Step 4: Controller'da stream'i aç**

`ClubApplicationsController.Submit` içinde, `request` kurulmadan önce (mevcut `streams` listesi dispose'u zaten kapsıyor):

```csharp
            UploadFileRequestDto? logo = null;
            if (form.Logo is not null)
            {
                var logoStream = form.Logo.OpenReadStream();
                streams.Add(logoStream);
                logo = new UploadFileRequestDto
                {
                    Content = logoStream,
                    OriginalFileName = form.Logo.FileName,
                    Length = form.Logo.Length,
                };
            }
```

ve `SubmitClubApplicationRequestDto` başlatıcısına `Logo = logo,` satırını ekle.

- [ ] **Step 5: Testi çalıştır, geçtiğini gör**

```bash
dotnet test tests/WebAPI.IntegrationTests --filter "FullyQualifiedName~Submit_WithLogo"
```
Beklenen: PASS.

---

### Task 5: Arayüz — başvuru formunda logo seçimi, inceleme ekranında gösterim

**Files:**
- Modify: `arayuz/src/pages/ClubApplicationPage.tsx:60-90`
- Modify: `arayuz/src/pages/ClubApplicationsReviewPage.tsx`
- Modify: `arayuz/src/api/types.ts` (`ClubApplicationListItemDto`)

**Interfaces:**
- Consumes: `POST /api/club-applications` `Logo` alanı (Task 4), `ClubApplicationListItemDto.logoFileId` (Task 3).

- [ ] **Step 1: Tipe alanı ekle**

`arayuz/src/api/types.ts` içindeki `ClubApplicationListItemDto` arayüzüne:

```ts
  logoFileId: number | null
```

- [ ] **Step 2: Forma logo seçici ekle**

`ClubApplicationPage.tsx` içinde, evrak seçicilerinin üstüne bir `input type="file" accept="image/png,image/jpeg,image/webp"` alanı ve seçilince `URL.createObjectURL` ile 96×96 önizleme koy. Gönderimde (`:64-78` aralığındaki `FormData` kurulumuna):

```ts
      if (logoFile) {
        formData.append('Logo', logoFile)
      }
```

- [ ] **Step 3: İnceleme ekranında logoyu göster**

`ClubApplicationsReviewPage.tsx` DataGrid'ine, "Önerilen ad" sütununun soluna 40×40 avatar sütunu ekle: `logoFileId` doluysa `<Avatar src={`/api/files/${row.logoFileId}`} variant="rounded" />`, boşsa baş harf.

- [ ] **Step 4: Derle ve lint'le**

```bash
cd arayuz && npm run build && npm run lint
```
Beklenen: ikisi de hatasız.

---

### Task 6: Tam doğrulama ve tek commit

**Files:** yok (doğrulama + commit)

- [ ] **Step 1: Tüm .NET testlerini çalıştır**

```bash
dotnet test
```
Beklenen: tamamı PASS. Kırmızı varsa düzelt, bu adıma geri dön.

- [ ] **Step 2: Frontend'i derle**

```bash
cd arayuz && npm run build
```
Beklenen: hatasız.

- [ ] **Step 3: Elle doğrula**

Uygulamayı çalıştır; öğrenci olarak logolu bir başvuru gönder, yönetici olarak onayla, `/clubs/{id}` sayfasında logonun göründüğünü doğrula. Sonra logolu bir başvuruyu **reddet** ve yeni kulüp oluşmadığını gör (Y-76).

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Faz 36: kurulus basvurusunda logo — onayda kulube devrediliyor"
```
