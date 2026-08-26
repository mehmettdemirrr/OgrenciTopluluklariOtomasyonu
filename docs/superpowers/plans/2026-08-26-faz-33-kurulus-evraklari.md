# Faz 33 — Topluluk Kuruluş Evrakları Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Öğrenci topluluk kurma başvurusunu zorunlu evraklarla birlikte tek istekte gönderebilsin; yönetici evrak tipi kataloğunu yönetebilsin ve inceleme ekranında her evrağı açıp okuyabilsin; evraklar kişisel veri olarak korunsun.

**Architecture:** Evrak tipi kataloğu (`ClubDocumentType`) referans verisidir; sekiz gerçek MTÜ formu migration `HasData` ile gelir (A-58). Başvuru ucu **multipart** olur — form + dosyalar tek istekte, atomik. `IFormFile` Business'a **girmez**: controller stream'i açar, Business ilkel DTO alır (Y-05/Y-09). Evraklar `Protected` görünürlükle saklanır ve **yeni bir korumalı uçtan**, yetki indirme anında yeniden kontrol edilerek servis edilir (Y-70). Core'a PDF imzası eklenir, ama izin verilen tip kümesi **çağrı yerine göre** geçilir — aksi hâlde logo ucu da PDF kabul ederdi (A-64).

**Tech Stack:** .NET 8 · EF Core 8 / MSSQL · FluentValidation · Hangfire · xUnit + Moq · React 18 + Vite + TypeScript + MUI + react-hook-form + zod

**Spec:** [docs/PLAN-V6.md](../../PLAN-V6.md) §Faz 33 · [docs/MIMARI.md](../../MIMARI.md) K-37, A-62, A-63, A-64, Y-70, Y-71

**Bağımlılık:** Yok, ama **Faz 32'den sonra uygulanması önerilir** — ikisi de `SubmitClubApplicationRequestDto`'ya dokunuyor, sırayla yapılırsa DTO tek seferde multipart'a taşınır.

---

## Global Constraints

Bu bölüm her task'ın gereksinimlerine **örtük olarak dahildir.** Değerler `docs/MIMARI.md`'den birebir alınmıştır.

- **Y-01** — Controller içinde `DbContext`, `IXxxDal`, LINQ sorgusu veya iş kuralı bulunamaz. Controller bir servis metodu çağırır, `IResult`'ı HTTP'ye çevirir.
- **Y-03** — İş kuralı yalnızca Business'ta yaşar.
- **Y-05 / Y-09** — Katman atlanmaz; **`IFormFile` Business'a geçmez**, entity HTTP gövdesinde yer almaz.
- **Y-16** — Olay kaydı (başvuru) fiziksel silinmez. **Evrak dosyası olay kaydı değildir** — saklama süresi dolunca silinir.
- **Y-17** — `DateTime.Now` yasak; UTC ve enjekte edilen `IClock`.
- **Y-25** — İstemciye nötr mesaj; exception/SQL detayı sızmaz.
- **Y-26** — Kişisel/gizli veri log'a, audit'e veya Hangfire iş parametresine yazılmaz.
- **Y-27** — Uçtan uca async, `CancellationToken` taşınır, `.ConfigureAwait(false)`.
- **Y-29** — Kullanıcı mesajı yalnızca `Business/Constants/Messages`.
- **Y-31** — Nullable uyarısı susturulamaz. **Uyarılar zaten hata.**
- **Y-35** — İş kuralı arayüzde tekrar yazılmaz.
- **Y-40** — Dosya tipi **uzantıya değil imzaya** bakılarak belirlenir; ad sunucuda üretilir; boyut sınırlanır.
- **Y-47** — Arka plan işine entity/DbContext/servis örneği geçilmez; iş idempotent yazılır.
- **Y-49** — Yükleme klasörü `UseStaticFiles` ile yayınlanmaz.
- **Y-51** — İndirme anında yetki **yeniden** kontrol edilir.
- **Y-52** — Anonim dosya ucu `Public` olmayan kaydı döndüremez; `StoredFile` görünürlük alanı olmadan kaydedilemez.
- **Y-70 (bu fazda doğuyor)** — Evrak `Protected`; anonim uçtan servis edilemez; indirmede yetki yeniden kontrol edilir.
- **Y-71 (bu fazda doğuyor)** — Zorunlu evrak bütünlüğü **arayüzde veya FluentValidation'da** kontrol edilemez; katalog okunarak Business'ta kontrol edilir.
- **A-58** — Gerçek kurumsal referans verisi migration `HasData` ile kalıcı ve belirleyici.
- **A-64** — İzin verilen dosya tipi kümesi **çağrı yerine göre** geçilir.
- **Sessiz onaylar** — Adlar İngilizce, mesajlar/yorumlar Türkçe. Yükleme sınırı: **evrak başına 5 MB, başvuru başına toplam 60 MB, yalnızca PDF.** Migration adı `YYYYMMDD_AçıklayıcıAd`, **her PR'da en fazla bir migration.**

---

## Bu fazın dört tuzağı

| # | Tuzak | Nerede kapanıyor |
|---|---|---|
| 1 | **Tek ortak `StoreFileAsync` yolu.** PDF'i imza listesine parametresiz eklemek **logo/afiş ucunu da PDF'e açar** — sessiz onaydaki "yalnızca JPEG/PNG/WebP" kuralı sessizce delinir. | Task 1: tip kümesi parametre + "logo ucuna PDF → 400" regresyon testi |
| 2 | **`IFormFile` sızıntısı.** Multipart'ı en kolay yazma yolu DTO'ya `IFormFile` koymaktır; bu Business'ı ASP.NET'e bağlar (Y-05/Y-09) ve `LayerDependencyTests`'i kırar. | Task 4: WebAPI'de ayrı bağlama modeli, Business'a `UploadFileRequestDto` |
| 3 | **Disk/DB ayrışması.** Dosyalar `SaveChanges`'ten önce diske yazılır; transaction geri alınırsa `StoredFile` satırları gider, **disk dosyaları kalır.** | Task 6: gecelik bakıma sahipsiz dosya temizliği |
| 4 | **Anonim sızıntı.** `GET /api/files/{id}` görünürlüğe bakıyor; evrak `Public` kaydedilirse adli sicil belgesi **anonim** indirilebilir hâle gelir. | Task 4: `Protected` sabit + Task 5: `PublicSurfaceLeakTests` |

---

## File Structure

| Dosya | Sorumluluk | Task |
|---|---|---|
| `src/Core/Utilities/Files/DetectedFileType.cs` | `Pdf` değeri + içerik tipi/uzantı | 1 |
| `src/Core/Utilities/Files/FileSignatureInspector.cs` | `%PDF-` imzası | 1 |
| `src/Business/Concrete/FileManager.cs` | `StoreFileAsync` tip kümesi parametresi + evrak saklama metodu | 1, 4 |
| `src/Business/Abstract/IFileService.cs` | `StoreApplicationDocumentAsync` sözleşmesi | 4 |
| `src/Business/Constants/Messages.cs` | Yedi mesaj | 1, 3, 4, 5 |
| `src/Entities/ClubDocumentType.cs` | **Yeni.** Evrak tipi kataloğu | 2 |
| `src/Entities/ClubApplicationDocument.cs` | **Yeni.** Başvuru–evrak bağı | 2 |
| `src/DataAccess/Configurations/ClubDocumentTypeConfiguration.cs` | **Yeni.** Kod unique + 8 form `HasData` | 2 |
| `src/DataAccess/Configurations/ClubApplicationDocumentConfiguration.cs` | **Yeni.** Bileşik unique + FK'lar | 2 |
| `src/DataAccess/AppDbContext.cs` | İki `DbSet` | 2 |
| `src/DataAccess/Migrations/…_Faz33_KurulusEvraklari.cs` | **Üretilecek** | 2 |
| `src/Business/DTOs/Reference/ClubDocumentTypeListItemDto.cs` · `Create…` · `Update…` | **Yeni** | 3 |
| `src/Business/ValidationRules/CreateClubDocumentTypeRequestValidator.cs` · `Update…` | **Yeni** | 3 |
| `src/Business/Abstract/IReferenceDataService.cs` · `Concrete/ReferenceDataManager.cs` | Katalog CRUD | 3 |
| `src/WebAPI/Controllers/ClubDocumentTypesController.cs` | **Yeni** | 3 |
| `src/Business/DTOs/ClubApplications/ClubApplicationDocumentUploadDto.cs` | **Yeni.** Business giriş | 4 |
| `src/Business/DTOs/ClubApplications/SubmitClubApplicationRequestDto.cs` | Evrak listesi | 4 |
| `src/WebAPI/Models/SubmitClubApplicationForm.cs` | **Yeni.** `[FromForm]` bağlama modeli | 4 |
| `src/Business/Concrete/ClubApplicationManager.cs` | Evrak bütünlüğü + saklama + indirme | 4, 5 |
| `src/WebAPI/Controllers/ClubApplicationsController.cs` | Multipart submit + indirme ucu | 4, 5 |
| `src/Business/DTOs/ClubApplications/ClubApplicationDocumentDto.cs` | **Yeni.** İnceleme çıkışı | 5 |
| `src/Business/DTOs/ClubApplications/ClubApplicationListItemDto.cs` | `Documents` listesi | 5 |
| `src/Business/Concrete/MaintenanceManager.cs` | 90 gün + sahipsiz dosya | 6 |
| `arayuz/src/api/types.ts` · `App.tsx` · yeni sayfa · `ClubApplicationsReviewPage.tsx` · `ReferenceDataPage.tsx` · `ClubsPage.tsx` | Arayüz | 7 |

---

### Task 1: Core — PDF imzası ve çağrı yerine göre tip kümesi

**Files:**
- Modify: `src/Core/Utilities/Files/DetectedFileType.cs`
- Modify: `src/Core/Utilities/Files/FileSignatureInspector.cs`
- Modify: `src/Business/Concrete/FileManager.cs`
- Modify: `src/Business/Constants/Messages.cs`
- Test: `tests/Business.Tests/FileSignatureInspectorTests.cs`, `tests/WebAPI.IntegrationTests/FileEndpointTests.cs`

**Interfaces:**
- Consumes: — (ilk task)
- Produces:
  - `Core.Utilities.Files.DetectedFileType.Pdf` — `application/pdf`, `.pdf`
  - `FileManager.StoreFileAsync(UploadFileRequestDto, FileVisibility, IReadOnlyCollection<DetectedFileType>, string, CancellationToken)` — **private**, tip kümesi ve hata mesajı parametreli
  - `Business.Constants.Messages.UnsupportedDocumentFileType` — `const string`

- [ ] **Step 1: Failing test'leri yaz**

`tests/Business.Tests/FileSignatureInspectorTests.cs` — dosyanın sonundaki kapanış süslü parantezinden önce:

```csharp
    [Fact(DisplayName = "A-64: %PDF- imzası Pdf olarak tanınır")]
    public void Detect_PdfSignature_ReturnsPdf()
    {
        byte[] header = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37, 0x0A, 0x00, 0x00, 0x00];

        var detected = FileSignatureInspector.Detect(header);

        Assert.Equal(DetectedFileType.Pdf, detected);
        Assert.Equal("application/pdf", detected.ToContentType());
        Assert.Equal(".pdf", detected.ToExtension());
    }

    [Fact(DisplayName = "Y-40: PDF'e benzeyen ama imzası bozuk içerik Unknown döner")]
    public void Detect_BrokenPdfSignature_ReturnsUnknown()
    {
        byte[] header = [0x25, 0x50, 0x44, 0x00, 0x2D, 0x31, 0x2E, 0x37, 0x0A, 0x00, 0x00, 0x00];

        Assert.Equal(DetectedFileType.Unknown, FileSignatureInspector.Detect(header));
    }
```

`tests/WebAPI.IntegrationTests/FileEndpointTests.cs` — **bu fazın en önemli testi.** Dosyanın sonundaki kapanış süslü parantezinden önce ekle; dosyadaki mevcut yükleme testinin gövdesini (çok parçalı içerik kurulumu, token alma) örnek alarak ad ve yardımcıları **dosyadan doğrula**:

```csharp
    [Fact(DisplayName = "A-64 REGRESYON: kulüp logosu ucuna PDF yüklenemez (400) — PDF eklenince tip kümesi gevşemedi")]
    public async Task UploadClubLogo_PdfContent_ReturnsBadRequest()
    {
        byte[] pdfBytes = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37, 0x0A, 0x25, 0xE2, 0xE3, 0xCF, 0xD3];

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "sahte-logo.pdf");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/clubs/{_clubId}/logo") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
```

> `_clubId`, `_adminToken`, `_client` adlarını `FileEndpointTests.cs`'ten **doğrula** ve gerekiyorsa dosyadaki gerçek adlarla değiştir. Bu test, PDF eklendikten sonra logo ucunun **hâlâ** sadece görsel kabul ettiğinin tek kanıtıdır.

- [ ] **Step 2: Test'leri çalıştır, kırmızı olduğunu gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~Detect_PdfSignature|FullyQualifiedName~Detect_BrokenPdfSignature"
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~UploadClubLogo_PdfContent"
```

Beklenen: birim testleri **derleme hatası** (`DetectedFileType.Pdf` yok); logo testi **PASS** (henüz PDF tanınmadığı için zaten 400 dönüyor). İkincisi kasıtlı: bu test şu an *yanlış sebepten* yeşil; Step 3-4'ten sonra **doğru sebepten** yeşil kalmalı.

- [ ] **Step 3: `DetectedFileType`'a PDF'i ekle**

`src/Core/Utilities/Files/DetectedFileType.cs` — enum'a `Webp`'den sonra:

```csharp
    /// <summary>docs/MIMARI.md · K-37/A-64: kuruluş evrakı. YALNIZCA evrak yolunda kabul edilir.</summary>
    Pdf,
```

`ToContentType` switch'ine:

```csharp
        DetectedFileType.Pdf => "application/pdf",
```

`ToExtension` switch'ine:

```csharp
        DetectedFileType.Pdf => ".pdf",
```

- [ ] **Step 4: İmzayı ekle**

`src/Core/Utilities/Files/FileSignatureInspector.cs`:

**(a)** Alan bloğuna:

```csharp
    // "%PDF-" — PDF dosyaları daima bu beş baytla başlar (ISO 32000-1 §7.5.2).
    private static readonly byte[] PdfSignature = [0x25, 0x50, 0x44, 0x46, 0x2D];
```

**(b)** `Detect` içinde, WebP kontrolünden **sonra**, `return DetectedFileType.Unknown;` satırından **önce**:

```csharp
        if (header.Length >= PdfSignature.Length && header[..PdfSignature.Length].SequenceEqual(PdfSignature))
        {
            return DetectedFileType.Pdf;
        }
```

- [ ] **Step 5: Mesajı ekle**

`src/Business/Constants/Messages.cs` — `UnsupportedFileType` sabitinin altına:

```csharp
    // Faz 33 — Kuruluş evrakları (K-37, A-64)
    // A-64: iki farklı bağlam, iki farklı mesaj. UnsupportedFileType görsel yolunda kalır.
    public const string UnsupportedDocumentFileType = "Desteklenmeyen dosya türü. Evraklar yalnızca PDF olarak yüklenebilir.";
```

- [ ] **Step 6: `StoreFileAsync`'e tip kümesi parametresini ekle (Tuzak 1)**

`src/Business/Concrete/FileManager.cs`:

**(a)** Sınıfın başına iki küme sabiti ekle (`UploadClubLogoAsync`'in üstüne):

```csharp
    /// <summary>docs/MIMARI.md · A-64: logo/afiş yolu. Sessiz onay: yalnızca JPEG/PNG/WebP.</summary>
    private static readonly DetectedFileType[] ImageTypes = [DetectedFileType.Jpeg, DetectedFileType.Png, DetectedFileType.Webp];

    /// <summary>docs/MIMARI.md · A-64: kuruluş evrakı yolu. Yalnızca PDF.</summary>
    private static readonly DetectedFileType[] DocumentTypes = [DetectedFileType.Pdf];
```

**(b)** `StoreFileAsync` imzasını ve tip kontrolünü değiştir:

```csharp
    private async Task<IDataResult<UploadedFileDto>> StoreFileAsync(
        UploadFileRequestDto request,
        FileVisibility visibility,
        IReadOnlyCollection<DetectedFileType> allowedTypes,
        string unsupportedTypeMessage,
        CancellationToken cancellationToken)
```

Metot içindeki tip kontrolü bloğunu şununla değiştir:

```csharp
        var detectedType = FileSignatureInspector.Detect(header.AsSpan(0, headerLength));
        var contentType = detectedType.ToContentType();
        var extension = detectedType.ToExtension();

        // A-64: PDF Core'a eklendi ama HER çağrı yeri kendi kümesini geçer. Bu satır olmadan
        // logo/afiş ucu da PDF kabul ederdi — "yalnızca JPEG/PNG/WebP" sessiz onayı sessizce delinirdi.
        if (contentType is null || extension is null || !allowedTypes.Contains(detectedType))
        {
            return DataResult<UploadedFileDto>.ValidationError(unsupportedTypeMessage);
        }
```

**(c)** İki mevcut çağrıyı güncelle:

`UploadClubLogoAsync` içinde:

```csharp
        var stored = await StoreFileAsync(request, FileVisibility.Public, ImageTypes, Messages.UnsupportedFileType, cancellationToken).ConfigureAwait(false);
```

`UploadEventPosterAsync` içinde:

```csharp
        var stored = await StoreFileAsync(request, FileVisibility.Public, ImageTypes, Messages.UnsupportedFileType, cancellationToken).ConfigureAwait(false);
```

- [ ] **Step 7: Test'leri çalıştır, geçtiğini gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~FileSignatureInspectorTests"
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~FileEndpointTests"
```

Beklenen: hepsi PASS. `UploadClubLogo_PdfContent` artık **doğru sebepten** yeşil: PDF tanınıyor ama `ImageTypes` kümesinde değil.

- [ ] **Step 8: Tüm test takımını çalıştır ve commit**

```bash
dotnet build && dotnet test
git add src/Core src/Business/Concrete/FileManager.cs src/Business/Constants/Messages.cs tests
git commit -m "Faz 33 adim 1: PDF imzasi ve cagri yerine gore tip kumesi (K-37, A-64)"
```

---

### Task 2: Şema — evrak tipi kataloğu ve başvuru–evrak bağı

**Files:**
- Create: `src/Entities/ClubDocumentType.cs`, `src/Entities/ClubApplicationDocument.cs`
- Create: `src/DataAccess/Configurations/ClubDocumentTypeConfiguration.cs`, `ClubApplicationDocumentConfiguration.cs`
- Modify: `src/DataAccess/AppDbContext.cs`
- Create (üretilecek): `src/DataAccess/Migrations/<timestamp>_20260826_Faz33_KurulusEvraklari.cs`
- Test: `tests/WebAPI.IntegrationTests/InstitutionalReferenceDataTests.cs`

**Interfaces:**
- Consumes: — (Task 1'den bağımsız)
- Produces:
  - `Entities.ClubDocumentType` — `int Id`, `required string Code`, `required string Name`, `bool IsRequired`, `bool IsActive`, `int DisplayOrder`
  - `Entities.ClubApplicationDocument` — `int Id`, `int ClubApplicationId`, `int ClubDocumentTypeId`, `int StoredFileId`
  - `AppDbContext.ClubDocumentTypes`, `AppDbContext.ClubApplicationDocuments`

- [ ] **Step 1: Failing test'i yaz**

`tests/WebAPI.IntegrationTests/InstitutionalReferenceDataTests.cs` dosyasının sonundaki kapanış süslü parantezinden önce ekle (bu dosya A-58 seed testlerinin evi):

```csharp
    [Fact(DisplayName = "A-58/A-62: sekiz gerçek MTÜ formu HasData ile seed edilir ve hepsi zorunludur")]
    public async Task ClubDocumentTypes_EightRealFormsAreSeeded()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var seeded = await db.ClubDocumentTypes.AsNoTracking().OrderBy(t => t.DisplayOrder).ToListAsync();

        string[] expectedCodes =
            ["FR-0230", "FR-0240", "FR-0241", "FR-0242", "FR-0243", "FR-0244", "FR-0245", "FR-0272"];

        Assert.Equal(expectedCodes, seeded.Select(t => t.Code).ToArray());
        Assert.All(seeded, t => Assert.True(t.IsRequired));
        Assert.All(seeded, t => Assert.True(t.IsActive));
        Assert.All(seeded, t => Assert.False(string.IsNullOrWhiteSpace(t.Name)));
    }

    [Fact(DisplayName = "A-62: aynı başvuruya aynı evrak tipi iki kez yüklenemez (bileşik unique index)")]
    public async Task ClubApplicationDocument_DuplicateTypePerApplication_IsRejected()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var application = await db.ClubApplications.FirstOrDefaultAsync();
        if (application is null)
        {
            return; // Bu fixture'da başvuru yoksa test anlamsız — Task 4'ün akış testleri kapsar.
        }

        var documentType = await db.ClubDocumentTypes.FirstAsync();
        var file = new StoredFile
        {
            GeneratedFileName = $"{Guid.NewGuid():N}.pdf",
            OriginalFileName = "evrak.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 10,
            Visibility = FileVisibility.Protected,
            UploadedByUserId = 1,
            UploadedAtUtc = DateTime.UtcNow,
        };
        db.StoredFiles.Add(file);
        await db.SaveChangesAsync();

        db.ClubApplicationDocuments.Add(new ClubApplicationDocument
        {
            ClubApplicationId = application.Id, ClubDocumentTypeId = documentType.Id, StoredFileId = file.Id,
        });
        await db.SaveChangesAsync();

        db.ClubApplicationDocuments.Add(new ClubApplicationDocument
        {
            ClubApplicationId = application.Id, ClubDocumentTypeId = documentType.Id, StoredFileId = file.Id,
        });

        await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
    }
```

- [ ] **Step 2: Test'i çalıştır, derlenmediğini gör**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~ClubDocumentTypes_EightRealForms|FullyQualifiedName~ClubApplicationDocument_DuplicateType"
```

Beklenen: **derleme hatası** — `db.ClubDocumentTypes` ve `ClubApplicationDocument` yok.

- [ ] **Step 3: Varlıkları oluştur**

`src/Entities/ClubDocumentType.cs`:

```csharp
using Core.Entities;

namespace Entities;

/// <summary>
/// docs/MIMARI.md · K-37/A-62: kuruluş evrakı tipi. Referans verisi — hard delete serbest (A-12),
/// kullanımdaysa FK Restrict 409 üretir. IsActive = false, geçmiş başvuruları bozmadan bir formu
/// yürürlükten kaldırmanın yoludur.
/// </summary>
public sealed class ClubDocumentType : IEntity
{
    public int Id { get; set; }

    /// <summary>Kurumsal form kodu, ör. "FR-0230". Benzersiz.</summary>
    public required string Code { get; set; }

    public required string Name { get; set; }

    /// <summary>docs/MIMARI.md · Y-71: başvuru formundaki `*` işaretinin ve bütünlük kontrolünün tek kaynağı.</summary>
    public bool IsRequired { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }
}
```

`src/Entities/ClubApplicationDocument.cs`:

```csharp
using Core.Entities;

namespace Entities;

/// <summary>
/// docs/MIMARI.md · K-37/A-63: başvuruya yüklenen evrak. Dosya daima Protected görünürlükte
/// saklanır (Y-70) ve yalnızca korumalı uçtan servis edilir.
/// Y-18: (ClubApplicationId, ClubDocumentTypeId) unique — aynı evrak iki kez yüklenemez.
/// </summary>
public sealed class ClubApplicationDocument : IEntity
{
    public int Id { get; set; }

    public int ClubApplicationId { get; set; }

    public int ClubDocumentTypeId { get; set; }

    public int StoredFileId { get; set; }
}
```

- [ ] **Step 4: EF konfigürasyonlarını ve seed'i oluştur**

`src/DataAccess/Configurations/ClubDocumentTypeConfiguration.cs`:

```csharp
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>
/// docs/MIMARI.md · A-58/A-62: sekiz gerçek MTÜ formu HasData ile — gerçek kurumsal referans
/// verisi kalıcı, belirleyici ve üretime gider (demo verisi DEĞİL, Y-68 kapsamı dışında).
/// </summary>
public sealed class ClubDocumentTypeConfiguration : IEntityTypeConfiguration<ClubDocumentType>
{
    public void Configure(EntityTypeBuilder<ClubDocumentType> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.HasIndex(t => t.Code).IsUnique();

        builder.HasData(
            new ClubDocumentType { Id = 1, Code = "FR-0230", Name = "Topluluk Akademik Danışman Dilekçesi", IsRequired = true, IsActive = true, DisplayOrder = 1 },
            new ClubDocumentType { Id = 2, Code = "FR-0240", Name = "Topluluk Asıl Üyeler (Yönetim Kurulu)", IsRequired = true, IsActive = true, DisplayOrder = 2 },
            new ClubDocumentType { Id = 3, Code = "FR-0241", Name = "Topluluk Faaliyet Planı", IsRequired = true, IsActive = true, DisplayOrder = 3 },
            new ClubDocumentType { Id = 4, Code = "FR-0242", Name = "Topluluk Kapak Sayfası", IsRequired = true, IsActive = true, DisplayOrder = 4 },
            new ClubDocumentType { Id = 5, Code = "FR-0243", Name = "Topluluk Kurucu Üye Dilekçesi", IsRequired = true, IsActive = true, DisplayOrder = 5 },
            new ClubDocumentType { Id = 6, Code = "FR-0244", Name = "Topluluk Kuruluş Dilekçesi", IsRequired = true, IsActive = true, DisplayOrder = 6 },
            new ClubDocumentType { Id = 7, Code = "FR-0245", Name = "Topluluk Üye Listesi", IsRequired = true, IsActive = true, DisplayOrder = 7 },
            new ClubDocumentType { Id = 8, Code = "FR-0272", Name = "Topluluk Örnek Tüzük", IsRequired = true, IsActive = true, DisplayOrder = 8 });
    }
}
```

`src/DataAccess/Configurations/ClubApplicationDocumentConfiguration.cs`:

```csharp
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

public sealed class ClubApplicationDocumentConfiguration : IEntityTypeConfiguration<ClubApplicationDocument>
{
    public void Configure(EntityTypeBuilder<ClubApplicationDocument> builder)
    {
        builder.HasKey(d => d.Id);

        // Y-18: aynı başvuruya aynı evrak tipi iki kez yüklenemez.
        builder.HasIndex(d => new { d.ClubApplicationId, d.ClubDocumentTypeId }).IsUnique();

        // Başvuru silinirse (Y-16 gereği soft delete, ama fiziksel silme olursa) evrak bağı da gider.
        builder.HasOne<ClubApplication>()
            .WithMany()
            .HasForeignKey(d => d.ClubApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        // A-62: kullanımdaki evrak tipi silinemesin.
        builder.HasOne<ClubDocumentType>()
            .WithMany()
            .HasForeignKey(d => d.ClubDocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // A-63: saklama temizliği önce bu satırı, sonra StoredFile'ı siler — Restrict sırayı zorunlu kılar.
        builder.HasOne<StoredFile>()
            .WithMany()
            .HasForeignKey(d => d.StoredFileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Step 5: `DbSet`'leri ekle**

`src/DataAccess/AppDbContext.cs` — `ClubApplications` satırının altına:

```csharp
    public DbSet<ClubDocumentType> ClubDocumentTypes => Set<ClubDocumentType>();

    public DbSet<ClubApplicationDocument> ClubApplicationDocuments => Set<ClubApplicationDocument>();
```

- [ ] **Step 6: Migration üret ve doğrula**

```bash
dotnet ef migrations add 20260826_Faz33_KurulusEvraklari --project src/DataAccess --startup-project src/WebAPI
```

`Up()` içinde iki `CreateTable`, üç `CreateIndex` ve **sekiz `InsertData`** satırı olmalı. `InsertData` yoksa `HasData` yanlış yazılmıştır. Fazladan tablo/kolon varsa **dur.**

- [ ] **Step 7: Test'i çalıştır, geçtiğini gör ve commit**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~ClubDocumentTypes_EightRealForms|FullyQualifiedName~ClubApplicationDocument_DuplicateType"
dotnet build && dotnet test
git add src/Entities src/DataAccess tests/WebAPI.IntegrationTests/InstitutionalReferenceDataTests.cs
git commit -m "Faz 33 adim 2: evrak katalogu semasi ve 8 MTU formu (K-37, A-62, A-58)"
```

---

### Task 3: Katalog yönetimi — evrak tipi CRUD

**Files:**
- Create: `src/Business/DTOs/Reference/ClubDocumentTypeListItemDto.cs`, `CreateClubDocumentTypeRequestDto.cs`, `UpdateClubDocumentTypeRequestDto.cs`
- Create: `src/Business/ValidationRules/CreateClubDocumentTypeRequestValidator.cs`, `UpdateClubDocumentTypeRequestValidator.cs`
- Modify: `src/Business/Abstract/IReferenceDataService.cs`, `src/Business/Concrete/ReferenceDataManager.cs`, `src/Business/Constants/Messages.cs`
- Create: `src/WebAPI/Controllers/ClubDocumentTypesController.cs`
- Test: `tests/Business.Tests/ReferenceDataManagerTests.cs`

**Interfaces:**
- Consumes: `Entities.ClubDocumentType` (Task 2)
- Produces:
  - `ClubDocumentTypeListItemDto` — `int Id`, `required string Code`, `required string Name`, `bool IsRequired`, `bool IsActive`, `int DisplayOrder`
  - `IReferenceDataService.GetClubDocumentTypesPagedAsync(int, int, bool, CancellationToken)` → `Task<IDataResult<PagedResult<ClubDocumentTypeListItemDto>>>` (üçüncü parametre `activeOnly`)
  - `.CreateClubDocumentTypeAsync` / `.UpdateClubDocumentTypeAsync` / `.DeleteClubDocumentTypeAsync`
  - `GET/POST/PUT/DELETE /api/club-document-types`

> **Okuma izni:** `clubs.read`. Başvuru formu katalogdan render edilir; `Member` rolünde bu izin var. Kategori kararının (Faz 32) aynısı.

- [ ] **Step 1: Failing test'leri yaz**

`tests/Business.Tests/ReferenceDataManagerTests.cs` dosyasının sonundaki kapanış süslü parantezinden önce ekle. Alan bloğuna `private readonly Mock<IEntityRepository<ClubDocumentType>> _clubDocumentTypeRepository = new();` ekle ve `_sut` kurucusuna **doğru konumda** `_clubDocumentTypeRepository.Object,` geçir.

```csharp
    [Fact(DisplayName = "A-62: aynı kodla ikinci evrak tipi oluşturulamaz — 409")]
    public async Task CreateClubDocumentTypeAsync_DuplicateCode_ReturnsConflict()
    {
        _clubDocumentTypeRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubDocumentType, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubDocumentType { Id = 1, Code = "FR-0230", Name = "Var olan", IsRequired = true, IsActive = true, DisplayOrder = 1 });

        var result = await _sut.CreateClubDocumentTypeAsync(new CreateClubDocumentTypeRequestDto
        {
            Code = "  FR-0230  ", Name = "Yeni", IsRequired = true, IsActive = true, DisplayOrder = 9,
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact(DisplayName = "A-62: yeni kod ile evrak tipi oluşturulur, kod ve ad kırpılır")]
    public async Task CreateClubDocumentTypeAsync_NewCode_AddsTrimmed()
    {
        ClubDocumentType? captured = null;
        _clubDocumentTypeRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubDocumentType, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClubDocumentType?)null);
        _clubDocumentTypeRepository
            .Setup(r => r.AddAsync(It.IsAny<ClubDocumentType>(), It.IsAny<CancellationToken>()))
            .Callback((ClubDocumentType t, CancellationToken _) => captured = t)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateClubDocumentTypeAsync(new CreateClubDocumentTypeRequestDto
        {
            Code = "  FR-0299  ", Name = "  Adli Sicil Belgesi  ", IsRequired = false, IsActive = true, DisplayOrder = 9,
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Equal("FR-0299", captured!.Code);
        Assert.Equal("Adli Sicil Belgesi", captured.Name);
        Assert.False(captured.IsRequired);
    }

    [Fact(DisplayName = "A-62: kullanımdaki evrak tipi silinemez — 409")]
    public async Task DeleteClubDocumentTypeAsync_InUse_ReturnsConflict()
    {
        _clubDocumentTypeRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<ClubDocumentType, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubDocumentType { Id = 1, Code = "FR-0230", Name = "Kullanımda", IsRequired = true, IsActive = true, DisplayOrder = 1 });
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ReferentialIntegrityConflictException("FK ihlali", new InvalidOperationException("inner")));

        var result = await _sut.DeleteClubDocumentTypeAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
    }
```

- [ ] **Step 2: Test'i çalıştır, derlenmediğini gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~ClubDocumentTypeAsync"
```

Beklenen: **derleme hatası.**

- [ ] **Step 3: DTO'ları oluştur**

`src/Business/DTOs/Reference/ClubDocumentTypeListItemDto.cs`:

```csharp
namespace Business.DTOs.Reference;

public sealed class ClubDocumentTypeListItemDto
{
    public int Id { get; set; }

    public required string Code { get; set; }

    public required string Name { get; set; }

    public bool IsRequired { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }
}
```

`src/Business/DTOs/Reference/CreateClubDocumentTypeRequestDto.cs`:

```csharp
namespace Business.DTOs.Reference;

public sealed class CreateClubDocumentTypeRequestDto
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }
}
```

`src/Business/DTOs/Reference/UpdateClubDocumentTypeRequestDto.cs` — **aynı beş özellik**:

```csharp
namespace Business.DTOs.Reference;

public sealed class UpdateClubDocumentTypeRequestDto
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }
}
```

- [ ] **Step 4: Validator'ları oluştur**

`src/Business/ValidationRules/CreateClubDocumentTypeRequestValidator.cs`:

```csharp
using Business.DTOs.Reference;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class CreateClubDocumentTypeRequestValidator : AbstractValidator<CreateClubDocumentTypeRequestDto>
{
    public CreateClubDocumentTypeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
```

`src/Business/ValidationRules/UpdateClubDocumentTypeRequestValidator.cs`:

```csharp
using Business.DTOs.Reference;
using FluentValidation;

namespace Business.ValidationRules;

public sealed class UpdateClubDocumentTypeRequestValidator : AbstractValidator<UpdateClubDocumentTypeRequestDto>
{
    public UpdateClubDocumentTypeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
```

- [ ] **Step 5: Mesajları ekle**

`src/Business/Constants/Messages.cs` — Task 1'de eklediğin bloğa:

```csharp
    public const string ClubDocumentTypeNotFound = "Evrak tipi bulunamadı.";
    public const string ClubDocumentTypeCodeTaken = "Bu kodla bir evrak tipi zaten var.";
    public const string ClubDocumentTypeUpdated = "Evrak tipi güncellendi.";
    public const string ClubDocumentTypeDeleted = "Evrak tipi silindi.";
    public const string ClubDocumentTypeInUse = "Bu evrak tipi başvurularda kullanıldığı için silinemez. Yürürlükten kaldırmak için pasife alın.";
```

- [ ] **Step 6: Servis sözleşmesini ve manager'ı yaz**

`src/Business/Abstract/IReferenceDataService.cs` — dört bildirim ekle:

```csharp
    /// <summary>
    /// docs/MIMARI.md · K-37/A-62: başvuru formu bu katalogdan render edilir, bu yüzden okuma izni
    /// `clubs.read`. `activeOnly = true` yalnızca yürürlükteki formları döner.
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.ClubsRead)]
    [CacheAspect(durationMinutes: 60)]
    Task<IDataResult<PagedResult<ClubDocumentTypeListItemDto>>> GetClubDocumentTypesPagedAsync(
        int pageIndex, int pageSize, bool activeOnly = false, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [ValidationAspect(typeof(CreateClubDocumentTypeRequestValidator))]
    [CacheRemoveAspect("ReferenceDataManager.")]
    [TransactionAspect]
    Task<IDataResult<ClubDocumentTypeListItemDto>> CreateClubDocumentTypeAsync(
        CreateClubDocumentTypeRequestDto request, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [ValidationAspect(typeof(UpdateClubDocumentTypeRequestValidator))]
    [CacheRemoveAspect("ReferenceDataManager.")]
    [TransactionAspect]
    Task<IResult> UpdateClubDocumentTypeAsync(
        int documentTypeId, UpdateClubDocumentTypeRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>A-62: kullanımdaysa 409 — yürürlükten kaldırmanın yolu IsActive = false.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReferenceManage)]
    [CacheRemoveAspect("ReferenceDataManager.")]
    [TransactionAspect]
    Task<IResult> DeleteClubDocumentTypeAsync(int documentTypeId, CancellationToken cancellationToken = default);
```

`src/Business/Concrete/ReferenceDataManager.cs` — kurucuya `IEntityRepository<ClubDocumentType> clubDocumentTypeRepository,` ekle ve dört metodu yaz:

```csharp
    public async Task<IDataResult<PagedResult<ClubDocumentTypeListItemDto>>> GetClubDocumentTypesPagedAsync(
        int pageIndex, int pageSize, bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        // Y-64: DisplayOrder'a göre artan, Id son kırıcı — form alanlarının sırası kurumun kararı.
        var paged = await clubDocumentTypeRepository
            .GetListPagedAsync(
                pageIndex, ClampPageSize(pageSize),
                t => !activeOnly || t.IsActive,
                t => t.DisplayOrder, descending: false, cancellationToken)
            .ConfigureAwait(false);

        var items = paged.Items.Select(ToDto).ToList();
        return DataResult<PagedResult<ClubDocumentTypeListItemDto>>.Success(
            new PagedResult<ClubDocumentTypeListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    public async Task<IDataResult<ClubDocumentTypeListItemDto>> CreateClubDocumentTypeAsync(
        CreateClubDocumentTypeRequestDto request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim();

        // Fakülte/kategoriden farklı: kod kurumsal bir kimliktir, sessizce mevcut satıra düşmek yanlış
        // olurdu — yönetici hangi kodu kullandığını bilmeli.
        var existing = await clubDocumentTypeRepository.GetAsync(t => t.Code == code, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return DataResult<ClubDocumentTypeListItemDto>.Conflict(Messages.ClubDocumentTypeCodeTaken);
        }

        var documentType = new ClubDocumentType
        {
            Code = code,
            Name = request.Name.Trim(),
            IsRequired = request.IsRequired,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder,
        };

        await clubDocumentTypeRepository.AddAsync(documentType, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return DataResult<ClubDocumentTypeListItemDto>.Success(ToDto(documentType));
    }

    public async Task<IResult> UpdateClubDocumentTypeAsync(
        int documentTypeId, UpdateClubDocumentTypeRequestDto request, CancellationToken cancellationToken = default)
    {
        var documentType = await clubDocumentTypeRepository
            .GetAsync(t => t.Id == documentTypeId, cancellationToken).ConfigureAwait(false);
        if (documentType is null)
        {
            return Result.NotFound(Messages.ClubDocumentTypeNotFound);
        }

        var code = request.Code.Trim();
        var duplicate = await clubDocumentTypeRepository
            .GetAsync(t => t.Id != documentTypeId && t.Code == code, cancellationToken).ConfigureAwait(false);
        if (duplicate is not null)
        {
            return Result.Conflict(Messages.ClubDocumentTypeCodeTaken);
        }

        documentType.Code = code;
        documentType.Name = request.Name.Trim();
        documentType.IsRequired = request.IsRequired;
        documentType.IsActive = request.IsActive;
        documentType.DisplayOrder = request.DisplayOrder;

        clubDocumentTypeRepository.Update(documentType);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.ClubDocumentTypeUpdated);
    }

    public async Task<IResult> DeleteClubDocumentTypeAsync(int documentTypeId, CancellationToken cancellationToken = default)
    {
        var documentType = await clubDocumentTypeRepository
            .GetAsync(t => t.Id == documentTypeId, cancellationToken).ConfigureAwait(false);
        if (documentType is null)
        {
            return Result.NotFound(Messages.ClubDocumentTypeNotFound);
        }

        clubDocumentTypeRepository.Delete(documentType);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ReferentialIntegrityConflictException)
        {
            return Result.Conflict(Messages.ClubDocumentTypeInUse);
        }

        return Result.Success(Messages.ClubDocumentTypeDeleted);
    }

    private static ClubDocumentTypeListItemDto ToDto(ClubDocumentType t) => new()
    {
        Id = t.Id, Code = t.Code, Name = t.Name, IsRequired = t.IsRequired, IsActive = t.IsActive, DisplayOrder = t.DisplayOrder,
    };
```

- [ ] **Step 7: Controller'ı oluştur**

`src/WebAPI/Controllers/ClubDocumentTypesController.cs`:

```csharp
using Business.Abstract;
using Business.DTOs.Reference;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

/// <summary>docs/MIMARI.md · K-37/A-62: evrak tipi kataloğu — FacultiesController ile aynı biçim.</summary>
[ApiController]
[Route("api/club-document-types")]
public sealed class ClubDocumentTypesController(IReferenceDataService referenceDataService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetClubDocumentTypes(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var result = await referenceDataService.GetClubDocumentTypesPagedAsync(pageIndex, pageSize, activeOnly, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> CreateClubDocumentType(CreateClubDocumentTypeRequestDto request, CancellationToken cancellationToken)
    {
        var result = await referenceDataService.CreateClubDocumentTypeAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateClubDocumentType(int id, UpdateClubDocumentTypeRequestDto request, CancellationToken cancellationToken)
    {
        var result = await referenceDataService.UpdateClubDocumentTypeAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteClubDocumentType(int id, CancellationToken cancellationToken)
    {
        var result = await referenceDataService.DeleteClubDocumentTypeAsync(id, cancellationToken);
        return result.ToActionResult();
    }
}
```

- [ ] **Step 8: Test'leri çalıştır ve commit**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~ClubDocumentTypeAsync"
dotnet build && dotnet test
git add src/Business src/WebAPI/Controllers/ClubDocumentTypesController.cs tests/Business.Tests/ReferenceDataManagerTests.cs
git commit -m "Faz 33 adim 3: evrak tipi katalogu CRUD (K-37, A-62)"
```

Beklenen: **3 passed** (filtre) + tüm takım yeşil.

---

### Task 4: Multipart başvuru — evrak bütünlüğü ve atomik yazma

**Files:**
- Create: `src/Business/DTOs/ClubApplications/ClubApplicationDocumentUploadDto.cs`
- Create: `src/WebAPI/Models/SubmitClubApplicationForm.cs`
- Modify: `src/Business/DTOs/ClubApplications/SubmitClubApplicationRequestDto.cs`
- Modify: `src/Business/Abstract/IFileService.cs`, `src/Business/Concrete/FileManager.cs`
- Modify: `src/Business/Concrete/ClubApplicationManager.cs`
- Modify: `src/WebAPI/Controllers/ClubApplicationsController.cs`
- Modify: `src/Business/Constants/Messages.cs`
- Test: `tests/WebAPI.IntegrationTests/ClubApplicationFlowTests.cs`

**Interfaces:**
- Consumes: `FileManager.DocumentTypes` (Task 1), `ClubDocumentType`/`ClubApplicationDocument` (Task 2)
- Produces:
  - `ClubApplicationDocumentUploadDto` — `int DocumentTypeId`, `required UploadFileRequestDto File`
  - `SubmitClubApplicationRequestDto.Documents` — `IReadOnlyList<ClubApplicationDocumentUploadDto>`
  - `IFileService.StoreApplicationDocumentAsync(UploadFileRequestDto, CancellationToken)` → `Task<IDataResult<UploadedFileDto>>`
  - `Messages.MissingRequiredClubDocuments`, `Messages.UnknownClubDocumentType`

**Kontrol sırası (`SubmitAsync`):** öğrenci → dönem → *(Faz 31 varsa pencere)* → danışman → *(Faz 32 varsa kategori)* → ad çakışması → çift başvuru → **evrak bütünlüğü** → dosyaları yaz → tek `SaveChanges`.

- [ ] **Step 1: Failing test'leri yaz**

`tests/WebAPI.IntegrationTests/ClubApplicationFlowTests.cs` — kapanış süslü parantezinden önce ekle:

```csharp
    private static byte[] FakePdfBytes() =>
        [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37, 0x0A, 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A];

    private static byte[] FakePngBytes() =>
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    /// <summary>Katalogdaki zorunlu evrak tiplerinin kimlikleri (A-62).</summary>
    private async Task<List<int>> GetRequiredDocumentTypeIdsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.ClubDocumentTypes
            .Where(t => t.IsActive && t.IsRequired)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => t.Id)
            .ToListAsync();
    }

    private async Task<HttpResponseMessage> SubmitMultipartAsync(
        string accessToken, string proposedName, IReadOnlyList<(int TypeId, byte[] Bytes, string FileName)> documents)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(proposedName), "ProposedName" },
            { new StringContent("Evrak testi"), "Description" },
            { new StringContent("Evrak testi gerekçesi"), "Justification" },
            { new StringContent(_proposedAdvisorId.ToString(CultureInfo.InvariantCulture)), "ProposedAdvisorId" },
        };

        for (var i = 0; i < documents.Count; i++)
        {
            content.Add(new StringContent(documents[i].TypeId.ToString(CultureInfo.InvariantCulture)), $"Documents[{i}].DocumentTypeId");

            var fileContent = new ByteArrayContent(documents[i].Bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, $"Documents[{i}].File", documents[i].FileName);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/club-applications") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await _client.SendAsync(request);
    }

    [Fact(DisplayName = "Y-71: eksik zorunlu evrakla başvuru 400 alır ve HİÇBİR kayıt yazılmaz")]
    public async Task Submit_MissingRequiredDocuments_ReturnsValidationErrorAndWritesNothing()
    {
        var requiredIds = await GetRequiredDocumentTypeIdsAsync();
        Assert.NotEmpty(requiredIds);

        var proposedName = $"Eksik Evrak {Guid.NewGuid():N}"[..30];
        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);

        // Yalnızca ilk zorunlu evrak yüklenir — geri kalanı eksik.
        var response = await SubmitMultipartAsync(studentToken, proposedName,
            [(requiredIds[0], FakePdfBytes(), "evrak1.pdf")]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.ClubApplications.AnyAsync(a => a.ProposedName == proposedName));
    }

    [Fact(DisplayName = "O-22: tam evrakla başvuru kabul edilir, evraklar Protected görünürlükle saklanır")]
    public async Task Submit_AllRequiredDocuments_IsAcceptedAndStoredProtected()
    {
        var requiredIds = await GetRequiredDocumentTypeIdsAsync();
        var proposedName = $"Tam Evrak {Guid.NewGuid():N}"[..30];
        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);

        var documents = requiredIds
            .Select((id, index) => (TypeId: id, Bytes: FakePdfBytes(), FileName: $"evrak{index}.pdf"))
            .ToList();

        var response = await SubmitMultipartAsync(studentToken, proposedName, documents);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var application = await db.ClubApplications.SingleAsync(a => a.ProposedName == proposedName);
        var stored = await db.ClubApplicationDocuments.Where(d => d.ClubApplicationId == application.Id).ToListAsync();
        Assert.Equal(requiredIds.Count, stored.Count);

        var fileIds = stored.Select(d => d.StoredFileId).ToList();
        var files = await db.StoredFiles.Where(f => fileIds.Contains(f.Id)).ToListAsync();

        // Y-70: adli sicil/kurucu üye dilekçesi kişisel veridir — Public kaydedilemez.
        Assert.All(files, f => Assert.Equal(FileVisibility.Protected, f.Visibility));
        Assert.All(files, f => Assert.Equal("application/pdf", f.ContentType));
        // Y-40: ad sunucuda üretilir — istemcinin verdiği "evrak0.pdf" saklanmaz.
        Assert.All(files, f => Assert.DoesNotContain("evrak", f.GeneratedFileName, StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "A-64: evrak yerine PNG yüklenirse 400 alınır ve kayıt yazılmaz")]
    public async Task Submit_NonPdfDocument_ReturnsValidationError()
    {
        var requiredIds = await GetRequiredDocumentTypeIdsAsync();
        var proposedName = $"Yanlis Tip {Guid.NewGuid():N}"[..30];
        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);

        var documents = requiredIds
            .Select((id, index) => (TypeId: id, Bytes: index == 0 ? FakePngBytes() : FakePdfBytes(), FileName: $"evrak{index}.pdf"))
            .ToList();

        var response = await SubmitMultipartAsync(studentToken, proposedName, documents);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.ClubApplications.AnyAsync(a => a.ProposedName == proposedName));
    }
```

Dosyanın `using` bloğuna ekle: `using System.Globalization;` ve `using System.Net.Http.Headers;` (ikincisi zaten var).

- [ ] **Step 2: Test'leri çalıştır, kırmızı olduğunu gör**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~Submit_MissingRequiredDocuments|FullyQualifiedName~Submit_AllRequiredDocuments|FullyQualifiedName~Submit_NonPdfDocument"
```

Beklenen: hepsi **FAIL** — uç hâlâ JSON bekliyor (415/400) ve evrak tablosu boş kalıyor.

- [ ] **Step 3: Business giriş DTO'sunu oluştur**

`src/Business/DTOs/ClubApplications/ClubApplicationDocumentUploadDto.cs`:

```csharp
using Business.DTOs.Files;

namespace Business.DTOs.ClubApplications;

/// <summary>
/// docs/MIMARI.md · Y-05/Y-09: Business'ın gördüğü evrak. `IFormFile` BURAYA GİRMEZ —
/// controller stream'i açar, mevcut `UploadFileRequestDto` sözleşmesini kullanır (FilesController deseni).
/// </summary>
public sealed class ClubApplicationDocumentUploadDto
{
    public int DocumentTypeId { get; set; }

    public required UploadFileRequestDto File { get; set; }
}
```

`src/Business/DTOs/ClubApplications/SubmitClubApplicationRequestDto.cs` — sınıfa ekle:

```csharp
    /// <summary>docs/MIMARI.md · K-37/Y-71: yüklenen evraklar. Bütünlük kontrolü Business'ta, katalog okunarak.</summary>
    public IReadOnlyList<ClubApplicationDocumentUploadDto> Documents { get; set; } = [];
```

- [ ] **Step 4: WebAPI bağlama modelini oluştur (Tuzak 2)**

`src/WebAPI/Models/SubmitClubApplicationForm.cs`:

```csharp
namespace WebAPI.Models;

/// <summary>
/// docs/MIMARI.md · Y-05/Y-09: `IFormFile` YALNIZCA bu katmanda yaşar. Controller bunu
/// Business'ın ilkel DTO'suna çevirir; Business ASP.NET'e bağlanmaz.
/// Model bağlama adları: `Documents[0].DocumentTypeId`, `Documents[0].File`.
/// </summary>
public sealed class SubmitClubApplicationForm
{
    public string ProposedName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Justification { get; set; } = string.Empty;

    public int ProposedAdvisorId { get; set; }

    /// <summary>Faz 32 uygulandıysa dolu gelir; uygulanmadıysa DTO'da karşılığı yoktur, atla.</summary>
    public int? ProposedCategoryId { get; set; }

    public List<SubmitClubApplicationDocumentForm> Documents { get; set; } = [];
}

public sealed class SubmitClubApplicationDocumentForm
{
    public int DocumentTypeId { get; set; }

    public IFormFile? File { get; set; }
}
```

- [ ] **Step 5: Evrak saklama metodunu `IFileService`'e ekle**

`src/Business/Abstract/IFileService.cs` — ekle:

```csharp
    /// <summary>
    /// docs/MIMARI.md · K-37/A-63/A-64: kuruluş evrakı — yalnızca PDF, daima Protected.
    /// Controller ucu YOKTUR; yalnızca ClubApplicationManager çağırır (başvuru akışının parçası).
    /// </summary>
    Task<IDataResult<UploadedFileDto>> StoreApplicationDocumentAsync(
        UploadFileRequestDto request, CancellationToken cancellationToken = default);
```

`src/Business/Concrete/FileManager.cs` — `GetPublicFileAsync`'in altına:

```csharp
    public Task<IDataResult<UploadedFileDto>> StoreApplicationDocumentAsync(
        UploadFileRequestDto request, CancellationToken cancellationToken = default) =>
        // A-64: DocumentTypes = yalnızca PDF. Y-70: görünürlük Protected, pazarlık yok.
        StoreFileAsync(request, FileVisibility.Protected, DocumentTypes, Messages.UnsupportedDocumentFileType, cancellationToken);
```

- [ ] **Step 6: Mesajları ekle**

`src/Business/Constants/Messages.cs` — Faz 33 bloğuna:

```csharp
    public const string MissingRequiredClubDocuments = "Zorunlu evrakların tamamı yüklenmeden başvuru gönderilemez.";
    public const string UnknownClubDocumentType = "Gönderilen evrak tiplerinden biri tanımlı değil.";
    public const string DuplicateClubDocumentUpload = "Aynı evrak tipi için birden fazla dosya gönderildi.";
```

- [ ] **Step 7: `SubmitAsync`'e evrak akışını ekle (Y-71)**

`src/Business/Concrete/ClubApplicationManager.cs`:

**(a)** Kurucuya ekle:

```csharp
    IEntityRepository<ClubDocumentType> clubDocumentTypeRepository,
    IEntityRepository<ClubApplicationDocument> clubApplicationDocumentRepository,
    IFileService fileService,
```

**(b)** `SubmitAsync` içinde, çift başvuru kontrolünden **sonra**, `var application = new ClubApplication` satırından **önce**:

```csharp
        // Y-71: bütünlük kontrolü BURADA — katalog DB'den okunur, biçimsel doğrulama değil iş kuralıdır.
        // FluentValidation'a konulamaz: "hangi evrak zorunlu" cevabı veritabanındadır.
        var activeTypes = await clubDocumentTypeRepository
            .GetListAsync(t => t.IsActive, cancellationToken)
            .ConfigureAwait(false);

        var submittedTypeIds = request.Documents.Select(d => d.DocumentTypeId).ToList();

        if (submittedTypeIds.Count != submittedTypeIds.Distinct().Count())
        {
            return Result.ValidationError(Messages.DuplicateClubDocumentUpload);
        }

        var activeTypeIds = activeTypes.Select(t => t.Id).ToHashSet();
        if (submittedTypeIds.Any(id => !activeTypeIds.Contains(id)))
        {
            return Result.ValidationError(Messages.UnknownClubDocumentType);
        }

        var requiredTypeIds = activeTypes.Where(t => t.IsRequired).Select(t => t.Id).ToList();
        if (requiredTypeIds.Any(id => !submittedTypeIds.Contains(id)))
        {
            return Result.ValidationError(Messages.MissingRequiredClubDocuments);
        }
```

**(c)** `application` eklendikten ve `SaveChangesAsync` çağrıldıktan **sonra** (başvuru kimliği için), evrakları yaz. `SubmitAsync`'in sonunu şununla değiştir:

```csharp
        await clubApplicationRepository.AddAsync(application, cancellationToken).ConfigureAwait(false);
        // Başvuru kimliği ClubApplicationDocument'in düz int FK'si için gerekli — ara SaveChanges,
        // bu kod tabanında navigation property kullanmamanın standart çözümü (DecideAsync precedent'i).
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var document in request.Documents)
        {
            // A-63/A-64: Protected + yalnızca PDF. Tip hatası burada yakalanır ve başvuru
            // [TransactionAspect] sayesinde geri alınır — yarım kayıt kalmaz (O-22 atomiklik).
            var stored = await fileService.StoreApplicationDocumentAsync(document.File, cancellationToken).ConfigureAwait(false);
            if (!stored.IsSuccess)
            {
                return Result.ValidationError(stored.Message ?? Messages.UnsupportedDocumentFileType);
            }

            await clubApplicationDocumentRepository.AddAsync(
                new ClubApplicationDocument
                {
                    ClubApplicationId = application.Id,
                    ClubDocumentTypeId = document.DocumentTypeId,
                    StoredFileId = stored.Data.FileId,
                },
                cancellationToken).ConfigureAwait(false);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.ClubApplicationSubmitted);
```

> `SubmitAsync` zaten `[TransactionAspect]` taşıyor (bkz. `IClubApplicationService`) — hata dönüşünde tüm DB yazımları geri alınır. **Disk dosyaları geri alınmaz**; Tuzak 3'ün cevabı Task 6'dadır.

- [ ] **Step 8: Controller'ı multipart'a çevir**

`src/WebAPI/Controllers/ClubApplicationsController.cs` — `Submit` action'ını şununla değiştir:

```csharp
    /// <summary>docs/MIMARI.md · O-22: form + evraklar tek istekte, atomik. Y-05/Y-09: IFormFile bu katmanda kalır.</summary>
    [HttpPost("club-applications")]
    [RequestSizeLimit(62_914_560)]
    [RequestFormLimits(MultipartBodyLengthLimit = 62_914_560)]
    public async Task<IActionResult> Submit([FromForm] SubmitClubApplicationForm form, CancellationToken cancellationToken)
    {
        var documents = new List<ClubApplicationDocumentUploadDto>(form.Documents.Count);
        var streams = new List<Stream>(form.Documents.Count);

        try
        {
            foreach (var part in form.Documents)
            {
                if (part.File is null)
                {
                    continue;
                }

                var stream = part.File.OpenReadStream();
                streams.Add(stream);
                documents.Add(new ClubApplicationDocumentUploadDto
                {
                    DocumentTypeId = part.DocumentTypeId,
                    File = new UploadFileRequestDto
                    {
                        Content = stream,
                        OriginalFileName = part.File.FileName,
                        Length = part.File.Length,
                    },
                });
            }

            var request = new SubmitClubApplicationRequestDto
            {
                ProposedName = form.ProposedName,
                Description = form.Description,
                Justification = form.Justification,
                ProposedAdvisorId = form.ProposedAdvisorId,
                Documents = documents,
            };

            var result = await clubApplicationService.SubmitAsync(request, cancellationToken);
            return result.ToActionResult();
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
    }
```

`using Business.DTOs.Files;` ve `using WebAPI.Models;` ekle.

> **Faz 32 uygulandıysa** `request` başlatıcısına `ProposedCategoryId = form.ProposedCategoryId,` satırını da ekle.
> **Y-01 kontrolü:** buradaki döngü *bağlama/dönüştürme*dir, iş kuralı değil — `if`'ler yalnızca null dosya atlıyor. Karar veren tek satır `SubmitAsync`'tedir.

- [ ] **Step 9: Test'leri çalıştır ve commit**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~Submit_MissingRequiredDocuments|FullyQualifiedName~Submit_AllRequiredDocuments|FullyQualifiedName~Submit_NonPdfDocument"
dotnet test tests/Architecture.Tests/Architecture.Tests.csproj
dotnet build && dotnet test
git add src/Business src/WebAPI tests/WebAPI.IntegrationTests/ClubApplicationFlowTests.cs
git commit -m "Faz 33 adim 4: multipart basvuru ve evrak butunlugu (K-37, Y-71, O-22)"
```

Beklenen: **3 passed**; `LayerDependencyTests` yeşil (Tuzak 2 kapandı — `IFormFile` Business'a sızmadı).

---

### Task 5: İnceleme ve korumalı indirme

**Files:**
- Create: `src/Business/DTOs/ClubApplications/ClubApplicationDocumentDto.cs`
- Modify: `src/Business/DTOs/ClubApplications/ClubApplicationListItemDto.cs`
- Modify: `src/Business/Abstract/IClubApplicationService.cs`, `src/Business/Concrete/ClubApplicationManager.cs`
- Modify: `src/WebAPI/Controllers/ClubApplicationsController.cs`
- Test: `tests/WebAPI.IntegrationTests/ClubApplicationFlowTests.cs`, `tests/WebAPI.IntegrationTests/PublicSurfaceLeakTests.cs`

**Interfaces:**
- Consumes: `ClubApplicationDocument` (Task 2), `IFileService.GetPublicFileAsync` deseni
- Produces:
  - `ClubApplicationDocumentDto` — `int DocumentId`, `int DocumentTypeId`, `required string Code`, `required string Name`, `bool IsRequired`, `required string OriginalFileName`, `long FileSizeBytes`
  - `ClubApplicationListItemDto.Documents` — `IReadOnlyList<ClubApplicationDocumentDto>`
  - `IClubApplicationService.GetDocumentAsync(int, int, CancellationToken)` → `Task<IDataResult<FileContentDto>>`
  - `GET /api/club-applications/{applicationId}/documents/{documentId}`

**Yetki (A-63/Y-70):** başvuran öğrenci **veya** `clubs.write` / `clubs.manage.all` taşıyan inceleyici. Kontrol **indirme anında** yapılır (Y-51 deseni).

- [ ] **Step 1: Failing test'leri yaz**

`tests/WebAPI.IntegrationTests/ClubApplicationFlowTests.cs` — kapanış süslü parantezinden önce:

```csharp
    [Fact(DisplayName = "A-63/Y-70: başvuran ve yönetici evrağı indirir; başka öğrenci 403 alır; anonim uçtan erişilemez")]
    public async Task DocumentDownload_EnforcesOwnershipAndVisibility()
    {
        var requiredIds = await GetRequiredDocumentTypeIdsAsync();
        var proposedName = $"Indirme {Guid.NewGuid():N}"[..30];
        var ownerToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);

        var documents = requiredIds.Select((id, i) => (TypeId: id, Bytes: FakePdfBytes(), FileName: $"e{i}.pdf")).ToList();
        Assert.Equal(HttpStatusCode.OK, (await SubmitMultipartAsync(ownerToken, proposedName, documents)).StatusCode);

        int applicationId;
        int documentId;
        int storedFileId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var application = await db.ClubApplications.SingleAsync(a => a.ProposedName == proposedName);
            applicationId = application.Id;
            var document = await db.ClubApplicationDocuments.FirstAsync(d => d.ClubApplicationId == applicationId);
            documentId = document.Id;
            storedFileId = document.StoredFileId;
        }

        var url = $"/api/club-applications/{applicationId}/documents/{documentId}";

        // 1. Başvuran indirebilir.
        var ownerResponse = await SendWithBearerAsync(HttpMethod.Get, url, ownerToken);
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.Equal("application/pdf", ownerResponse.Content.Headers.ContentType?.MediaType);

        // 2. Yönetici (clubs.write) indirebilir.
        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        Assert.Equal(HttpStatusCode.OK, (await SendWithBearerAsync(HttpMethod.Get, url, adminToken)).StatusCode);

        // 3. Başka bir öğrenci indiremez — Y-70.
        var strangerToken = await LoginAsync(MemberEmail, MemberPassword);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendWithBearerAsync(HttpMethod.Get, url, strangerToken)).StatusCode);

        // 4. Anonim dosya ucu Protected kaydı GÖREMEZ — Y-52/Y-70 ikinci savunma katmanı.
        var anonymousResponse = await _client.GetAsync($"/api/files/{storedFileId}");
        Assert.Equal(HttpStatusCode.NotFound, anonymousResponse.StatusCode);
    }

    [Fact(DisplayName = "K-37: inceleme listesi her başvurunun evraklarını kod ve adla taşır")]
    public async Task GetPending_IncludesDocuments()
    {
        var requiredIds = await GetRequiredDocumentTypeIdsAsync();
        var proposedName = $"Liste Evrak {Guid.NewGuid():N}"[..30];
        var studentToken = await LoginAsync(OtherStudentEmail, OtherStudentPassword);

        var documents = requiredIds.Select((id, i) => (TypeId: id, Bytes: FakePdfBytes(), FileName: $"e{i}.pdf")).ToList();
        Assert.Equal(HttpStatusCode.OK, (await SubmitMultipartAsync(studentToken, proposedName, documents)).StatusCode);

        var adminToken = await LoginAsync(AdminEmail, AdminPassword);
        var response = await SendWithBearerAsync(HttpMethod.Get, "/api/club-applications?pageIndex=0&pageSize=200", adminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(proposedName, body, StringComparison.Ordinal);
        Assert.Contains("FR-0230", body, StringComparison.Ordinal);
    }
```

- [ ] **Step 2: Test'i çalıştır, kırmızı olduğunu gör**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~DocumentDownload_EnforcesOwnership|FullyQualifiedName~GetPending_IncludesDocuments"
```

Beklenen: **2 failed** — indirme ucu 404, liste evrak taşımıyor.

- [ ] **Step 3: Çıkış DTO'sunu oluştur**

`src/Business/DTOs/ClubApplications/ClubApplicationDocumentDto.cs`:

```csharp
namespace Business.DTOs.ClubApplications;

/// <summary>
/// docs/MIMARI.md · K-37: inceleme ekranının gördüğü evrak. Dosyanın kendisi burada YOK —
/// yalnızca korumalı indirme ucundan alınır (A-63/Y-70). `StoredFileId` de taşınmaz:
/// istemcinin bilmesi gereken tek kimlik `DocumentId`.
/// </summary>
public sealed class ClubApplicationDocumentDto
{
    public int DocumentId { get; set; }

    public int DocumentTypeId { get; set; }

    public required string Code { get; set; }

    public required string Name { get; set; }

    public bool IsRequired { get; set; }

    public required string OriginalFileName { get; set; }

    public long FileSizeBytes { get; set; }
}
```

`src/Business/DTOs/ClubApplications/ClubApplicationListItemDto.cs` — sınıfa ekle:

```csharp
    /// <summary>docs/MIMARI.md · K-37: yüklenen evraklar. Zorunlu ama yüklenmemiş tipler bu listede YOKTUR — arayüz eksikliği katalogla karşılaştırarak gösterir.</summary>
    public IReadOnlyList<ClubApplicationDocumentDto> Documents { get; set; } = [];
```

- [ ] **Step 4: Servis sözleşmesine indirme metodunu ekle**

`src/Business/Abstract/IClubApplicationService.cs` — ekle:

```csharp
    /// <summary>
    /// docs/MIMARI.md · A-63/Y-70/Y-51: korumalı evrak indirme. Yetki İNDİRME ANINDA yeniden
    /// kontrol edilir — başvuran öğrenci veya clubs.write/clubs.manage.all taşıyan inceleyici.
    /// [SecuredOperation] YOK: öğrencinin kendi evrağını indirmesi izin gerektirmez, sahiplik yeter.
    /// </summary>
    Task<IDataResult<FileContentDto>> GetDocumentAsync(
        int applicationId, int documentId, CancellationToken cancellationToken = default);
```

`using Business.DTOs.Files;` ekle.

- [ ] **Step 5: Manager'a indirme ve liste doldurmayı ekle**

`src/Business/Concrete/ClubApplicationManager.cs`:

**(a)** Kurucuya ekle:

```csharp
    IEntityRepository<StoredFile> storedFileRepository,
    IFileStorage fileStorage,
```

**(b)** Sınıfa evrak sözlüğü yardımcısını ekle (`ClampPageSize`'ın üstüne):

```csharp
    /// <summary>Y-10: evraklar tek toplu sorguyla — başvuru başına sorgu N+1 üretirdi.</summary>
    private async Task<Dictionary<int, List<ClubApplicationDocumentDto>>> GetDocumentsByApplicationAsync(
        IReadOnlyCollection<int> applicationIds, CancellationToken cancellationToken)
    {
        if (applicationIds.Count == 0)
        {
            return [];
        }

        var links = await clubApplicationDocumentRepository
            .GetListAsync(d => applicationIds.Contains(d.ClubApplicationId), cancellationToken)
            .ConfigureAwait(false);

        if (links.Count == 0)
        {
            return [];
        }

        var typeIds = links.Select(d => d.ClubDocumentTypeId).Distinct().ToList();
        var typesById = (await clubDocumentTypeRepository.GetListAsync(t => typeIds.Contains(t.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(t => t.Id, t => t);

        var fileIds = links.Select(d => d.StoredFileId).Distinct().ToList();
        var filesById = (await storedFileRepository.GetListAsync(f => fileIds.Contains(f.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(f => f.Id, f => f);

        return links
            .GroupBy(d => d.ClubApplicationId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(d =>
                    {
                        typesById.TryGetValue(d.ClubDocumentTypeId, out var type);
                        filesById.TryGetValue(d.StoredFileId, out var file);
                        return new ClubApplicationDocumentDto
                        {
                            DocumentId = d.Id,
                            DocumentTypeId = d.ClubDocumentTypeId,
                            Code = type?.Code ?? string.Empty,
                            Name = type?.Name ?? string.Empty,
                            IsRequired = type?.IsRequired ?? false,
                            OriginalFileName = file?.OriginalFileName ?? string.Empty,
                            FileSizeBytes = file?.FileSizeBytes ?? 0,
                        };
                    })
                    .OrderBy(d => d.Code, StringComparer.Ordinal)
                    .ToList());
    }
```

**(c)** `GetPendingAsync` ve `MapWithAdvisorNamesAsync` içinde, DTO listesi kurulduktan **sonra** evrakları bağla:

```csharp
        var documentsByApplication = await GetDocumentsByApplicationAsync(
            items.Select(i => i.Id).ToList(), cancellationToken).ConfigureAwait(false);

        foreach (var item in items)
        {
            item.Documents = documentsByApplication.GetValueOrDefault(item.Id, []);
        }
```

**(d)** İndirme metodunu ekle:

```csharp
    public async Task<IDataResult<FileContentDto>> GetDocumentAsync(
        int applicationId, int documentId, CancellationToken cancellationToken = default)
    {
        var document = await clubApplicationDocumentRepository
            .GetAsync(d => d.Id == documentId && d.ClubApplicationId == applicationId, cancellationToken)
            .ConfigureAwait(false);
        if (document is null)
        {
            return DataResult<FileContentDto>.NotFound(Messages.FileNotFound);
        }

        var application = await clubApplicationRepository
            .GetAsync(a => a.Id == applicationId, cancellationToken)
            .ConfigureAwait(false);
        if (application is null)
        {
            return DataResult<FileContentDto>.NotFound(Messages.ClubApplicationNotFound);
        }

        // Y-51/Y-70: yetki İNDİRME ANINDA yeniden kontrol edilir — başvuru kuyrukta beklerken
        // öğrencinin veya inceleyicinin yetkisi değişmiş olabilir.
        var accessError = await EnsureDocumentAccessAsync(application, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return DataResult<FileContentDto>.Forbidden(accessError);
        }

        var file = await storedFileRepository
            .GetAsync(f => f.Id == document.StoredFileId, cancellationToken)
            .ConfigureAwait(false);
        if (file is null)
        {
            return DataResult<FileContentDto>.NotFound(Messages.FileNotFound);
        }

        var stream = await fileStorage.OpenReadAsync(file.GeneratedFileName, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return DataResult<FileContentDto>.NotFound(Messages.FileNotFound);
        }

        return DataResult<FileContentDto>.Success(new FileContentDto
        {
            Content = stream,
            ContentType = file.ContentType,
            DownloadFileName = file.OriginalFileName,
        });
    }

    /// <summary>
    /// Y-23/Y-70: izin claim'i tek başına yetmez. İnceleyici (clubs.write / clubs.manage.all) VEYA
    /// başvurunun sahibi öğrenci görebilir. Y-66: yönetici kontrolü ilk satırda.
    /// </summary>
    private async Task<string?> EnsureDocumentAccessAsync(ClubApplication application, CancellationToken cancellationToken)
    {
        if (currentUser.Permissions.Contains(IdentitySeedData.Permissions.ClubsManageAll)
            || currentUser.Permissions.Contains(IdentitySeedData.Permissions.ClubsWrite))
        {
            return null;
        }

        if (currentUser.UserId is not { } userId)
        {
            return Messages.ClubApplicationDocumentForbidden;
        }

        var student = await studentRepository
            .GetAsync(s => s.ApplicationUserId == userId, cancellationToken)
            .ConfigureAwait(false);

        return student is not null && student.Id == application.StudentId
            ? null
            : Messages.ClubApplicationDocumentForbidden;
    }
```

`using Core.Utilities.Files;` ve `using Business.DTOs.Files;` ekle.

**(e)** `src/Business/Constants/Messages.cs` — Faz 33 bloğuna:

```csharp
    public const string ClubApplicationDocumentForbidden = "Bu evrağı görüntüleme yetkiniz yok.";
```

- [ ] **Step 6: Controller ucunu ekle**

`src/WebAPI/Controllers/ClubApplicationsController.cs` — `Decide` action'ının altına:

```csharp
    /// <summary>
    /// docs/MIMARI.md · A-63/Y-70/Y-51: korumalı evrak indirme. FilesController'daki anonim ucun
    /// aksine Cache-Control YOK — kişisel veri tarayıcı önbelleğinde bırakılmaz.
    /// </summary>
    [HttpGet("club-applications/{applicationId:int}/documents/{documentId:int}")]
    public async Task<IActionResult> GetDocument(int applicationId, int documentId, CancellationToken cancellationToken)
    {
        var result = await clubApplicationService.GetDocumentAsync(applicationId, documentId, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ToActionResult();
        }

        return File(result.Data.Content, result.Data.ContentType, result.Data.DownloadFileName);
    }
```

- [ ] **Step 7: Test'leri çalıştır ve commit**

```bash
dotnet test tests/WebAPI.IntegrationTests/WebAPI.IntegrationTests.csproj --filter "FullyQualifiedName~DocumentDownload_EnforcesOwnership|FullyQualifiedName~GetPending_IncludesDocuments|FullyQualifiedName~PublicSurfaceLeakTests"
dotnet build && dotnet test
git add src/Business src/WebAPI/Controllers/ClubApplicationsController.cs tests
git commit -m "Faz 33 adim 5: inceleme ekrani evraklari ve korumali indirme (K-37, A-63, Y-70)"
```

---

### Task 6: Saklama — 90 gün ve sahipsiz dosya temizliği

**Files:**
- Modify: `src/Business/Concrete/MaintenanceManager.cs`
- Test: `tests/Business.Tests/MaintenanceManagerTests.cs`

**Interfaces:**
- Consumes: `ClubApplicationDocument` (Task 2), `IFileStorage.DeleteAsync`
- Produces: — (davranış değişikliği)

**Kural (A-63):** Karar tarihi 90 günden eski ve **reddedilmiş** başvuruların evrakları silinir — `ClubApplicationDocument` satırı, `StoredFile` satırı ve disk dosyası. **Onaylananınki kalır.** Başvuru kaydının kendisi durur (Y-16).

**Sahipsiz dosya (Tuzak 3):** `StoredFile` satırı olmayan disk dosyaları silinir.

- [ ] **Step 1: Failing test'leri yaz**

`tests/Business.Tests/MaintenanceManagerTests.cs` — kapanış süslü parantezinden önce ekle. Dosyadaki mevcut mock alanlarını kullanır; `_clubApplicationRepository` ve `_clubApplicationDocumentRepository` **yeni** — alan bloğuna ve `_sut` kurucusuna doğru konumda ekle.

```csharp
    [Fact(DisplayName = "A-63: 90 günü geçmiş REDDEDİLMİŞ başvurunun evrakları silinir")]
    public async Task RunNightlyMaintenanceAsync_OldRejectedApplication_DeletesDocuments()
    {
        var rejected = new ClubApplication
        {
            Id = 1, StudentId = 1, AcademicTermId = 1, ProposedName = "Eski Ret", Justification = "x",
            ProposedAdvisorId = 1, Status = ApplicationStatus.Rejected,
            AppliedAtUtc = FixedNow.AddDays(-200), ReviewedAtUtc = FixedNow.AddDays(-91),
        };
        var link = new ClubApplicationDocument { Id = 10, ClubApplicationId = 1, ClubDocumentTypeId = 1, StoredFileId = 100 };
        var file = new StoredFile
        {
            Id = 100, GeneratedFileName = "abc.pdf", OriginalFileName = "evrak.pdf", ContentType = "application/pdf",
            FileSizeBytes = 10, Visibility = FileVisibility.Protected, UploadedByUserId = 1, UploadedAtUtc = FixedNow.AddDays(-200),
        };

        SetupApplicationCleanupScenario([rejected], [link], [file]);

        var result = await _sut.RunNightlyMaintenanceAsync();

        Assert.True(result.IsSuccess);
        _clubApplicationDocumentRepository.Verify(r => r.Delete(It.Is<ClubApplicationDocument>(d => d.Id == 10)), Times.Once);
        _storedFileRepository.Verify(r => r.Delete(It.Is<StoredFile>(f => f.Id == 100)), Times.Once);
        _fileStorage.Verify(s => s.DeleteAsync("abc.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "A-63: ONAYLANMIŞ başvurunun evrakları 90 gün sonra da durur")]
    public async Task RunNightlyMaintenanceAsync_OldApprovedApplication_KeepsDocuments()
    {
        var approved = new ClubApplication
        {
            Id = 2, StudentId = 1, AcademicTermId = 1, ProposedName = "Eski Onay", Justification = "x",
            ProposedAdvisorId = 1, Status = ApplicationStatus.Approved, CreatedClubId = 5,
            AppliedAtUtc = FixedNow.AddDays(-200), ReviewedAtUtc = FixedNow.AddDays(-150),
        };
        var link = new ClubApplicationDocument { Id = 20, ClubApplicationId = 2, ClubDocumentTypeId = 1, StoredFileId = 200 };
        var file = new StoredFile
        {
            Id = 200, GeneratedFileName = "onay.pdf", OriginalFileName = "evrak.pdf", ContentType = "application/pdf",
            FileSizeBytes = 10, Visibility = FileVisibility.Protected, UploadedByUserId = 1, UploadedAtUtc = FixedNow.AddDays(-200),
        };

        SetupApplicationCleanupScenario([approved], [link], [file]);

        var result = await _sut.RunNightlyMaintenanceAsync();

        Assert.True(result.IsSuccess);
        _clubApplicationDocumentRepository.Verify(r => r.Delete(It.IsAny<ClubApplicationDocument>()), Times.Never);
        _fileStorage.Verify(s => s.DeleteAsync("onay.pdf", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "A-63: 90 günü DOLMAMIŞ reddedilmiş başvurunun evrakları durur")]
    public async Task RunNightlyMaintenanceAsync_RecentRejectedApplication_KeepsDocuments()
    {
        var rejected = new ClubApplication
        {
            Id = 3, StudentId = 1, AcademicTermId = 1, ProposedName = "Yeni Ret", Justification = "x",
            ProposedAdvisorId = 1, Status = ApplicationStatus.Rejected,
            AppliedAtUtc = FixedNow.AddDays(-40), ReviewedAtUtc = FixedNow.AddDays(-30),
        };
        var link = new ClubApplicationDocument { Id = 30, ClubApplicationId = 3, ClubDocumentTypeId = 1, StoredFileId = 300 };
        var file = new StoredFile
        {
            Id = 300, GeneratedFileName = "yeni.pdf", OriginalFileName = "evrak.pdf", ContentType = "application/pdf",
            FileSizeBytes = 10, Visibility = FileVisibility.Protected, UploadedByUserId = 1, UploadedAtUtc = FixedNow.AddDays(-40),
        };

        SetupApplicationCleanupScenario([rejected], [link], [file]);

        var result = await _sut.RunNightlyMaintenanceAsync();

        Assert.True(result.IsSuccess);
        _clubApplicationDocumentRepository.Verify(r => r.Delete(It.IsAny<ClubApplicationDocument>()), Times.Never);
    }

    /// <summary>Predicate'ler GERÇEKTEN çalıştırılır — Moq salt-geçiş olsaydı testler kuralı kanıtlamazdı.</summary>
    private void SetupApplicationCleanupScenario(
        ClubApplication[] applications, ClubApplicationDocument[] links, StoredFile[] files)
    {
        _clubApplicationRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubApplication, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ClubApplication, bool>> f, CancellationToken _) =>
                applications.AsQueryable().Where(f).ToList());

        _clubApplicationDocumentRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubApplicationDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ClubApplicationDocument, bool>> f, CancellationToken _) =>
                links.AsQueryable().Where(f).ToList());

        _storedFileRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<StoredFile, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<StoredFile, bool>> f, CancellationToken _) =>
                files.AsQueryable().Where(f).ToList());
    }
```

> Mevcut testler `_storedFileRepository.GetListAsync`'i başka bir davranışla kuruyorsa (rapor temizliği), bu yardımcı onu ezer. Rapor testleri kırılırsa yardımcıyı yalnızca yeni testlerde kullan ve rapor kurulumunu koru.

- [ ] **Step 2: Test'leri çalıştır, kırmızı olduğunu gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~RunNightlyMaintenanceAsync_OldRejected|FullyQualifiedName~RunNightlyMaintenanceAsync_OldApproved|FullyQualifiedName~RunNightlyMaintenanceAsync_RecentRejected"
```

Beklenen: **derleme hatası** (yeni mock alanları) veya **3 failed**.

- [ ] **Step 3: Bakım işine dördüncü ve beşinci adımı ekle**

`src/Business/Concrete/MaintenanceManager.cs`:

**(a)** Kurucuya ekle:

```csharp
    IEntityRepository<ClubApplication> clubApplicationRepository,
    IEntityRepository<ClubApplicationDocument> clubApplicationDocumentRepository,
```

**(b)** Saklama sabiti ekle:

```csharp
    /// <summary>docs/MIMARI.md · A-63: reddedilen başvurunun evrakları 90 gün sonra silinir (K-19 V1 dışı olduğu sürece yüzey büyümemeli).</summary>
    private static readonly TimeSpan RejectedApplicationDocumentRetention = TimeSpan.FromDays(90);
```

**(c)** `trafficLogDal.DeleteOlderThanAsync` çağrısından **önce** ekle:

```csharp
        // A-63: reddedilen başvuruların evrakları. Onaylananınki kulübün kuruluş dosyası olarak kalır;
        // başvuru kaydının kendisi hiçbir hâlde silinmez (Y-16 — o bir olay kaydıdır).
        var documentCutoff = now - RejectedApplicationDocumentRetention;
        var staleApplications = await clubApplicationRepository
            .GetListAsync(
                a => a.Status == ApplicationStatus.Rejected && a.ReviewedAtUtc != null && a.ReviewedAtUtc < documentCutoff,
                cancellationToken)
            .ConfigureAwait(false);

        if (staleApplications.Count > 0)
        {
            var staleApplicationIds = staleApplications.Select(a => a.Id).ToList();
            var staleLinks = await clubApplicationDocumentRepository
                .GetListAsync(d => staleApplicationIds.Contains(d.ClubApplicationId), cancellationToken)
                .ConfigureAwait(false);

            if (staleLinks.Count > 0)
            {
                var staleDocumentFileIds = staleLinks.Select(d => d.StoredFileId).Distinct().ToList();
                var staleDocumentFiles = await storedFileRepository
                    .GetListAsync(f => staleDocumentFileIds.Contains(f.Id), cancellationToken)
                    .ConfigureAwait(false);

                // Sıra zorunlu: FK Restrict önce bağ satırını, sonra StoredFile'ı ister (A-62 konfigürasyonu).
                foreach (var link in staleLinks)
                {
                    clubApplicationDocumentRepository.Delete(link);
                }

                foreach (var file in staleDocumentFiles)
                {
                    storedFileRepository.Delete(file);
                }

                // DB yazımı fiziksel silmeden ÖNCE commit edilir — yarıda kalırsa en fazla sahipsiz
                // bir disk dosyası kalır ve bir sonraki adım onu toplar.
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                foreach (var file in staleDocumentFiles)
                {
                    // Y-47: idempotent — zaten silinmişse false döner, akış bozulmaz.
                    await fileStorage.DeleteAsync(file.GeneratedFileName, cancellationToken).ConfigureAwait(false);
                }
            }
        }
```

- [ ] **Step 4: Test'leri çalıştır, geçtiğini gör**

```bash
dotnet test tests/Business.Tests/Business.Tests.csproj --filter "FullyQualifiedName~MaintenanceManagerTests"
```

Beklenen: mevcut testler + 3 yeni test yeşil.

- [ ] **Step 5: Sahipsiz disk dosyası temizliği (Tuzak 3)**

> **Kapsam kararı:** sahipsiz dosya taraması `IFileStorage`'a listeleme yeteneği eklemeyi gerektirir (`Task<IReadOnlyList<string>> ListAsync()`). Bu, Core'a yeni bir sözleşme demektir.
>
> **Karar: bu adımı uygula.** Gerekçesi Tuzak 3: `SubmitAsync` başarısız olduğunda diskte PDF kalır ve **kişisel veri** olduğu için orada süresiz duramaz (A-63'ün gerekçesiyle aynı).

`src/Core/Utilities/Files/IFileStorage.cs` — ekle:

```csharp
    /// <summary>
    /// docs/MIMARI.md · K-37 (Tuzak 3): depodaki tüm üretilmiş dosya adları. Yalnızca gecelik
    /// bakımın sahipsiz dosya taraması kullanır — iş kodu dosya listelemez.
    /// </summary>
    Task<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default);
```

`src/Core/Utilities/Files/LocalFileStorage.cs` — ekle:

```csharp
    public Task<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> names = Directory.EnumerateFiles(_rootPath)
            .Select(Path.GetFileName)
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(n => n!)
            .ToList();

        return Task.FromResult(names);
    }
```

`src/Business/Concrete/MaintenanceManager.cs` — metodun **en sonuna**, `return Result.Success();` satırından önce:

```csharp
        // K-37 Tuzak 3: başvuru transaction'ı geri alınırsa StoredFile satırı gider ama disk dosyası
        // kalır. Sahipsiz dosya asla erişilemez (her okuma StoredFile'dan geçer) ama kişisel veri
        // olduğu için diskte bırakılamaz. Bu tarama son adımdır: yukarıdaki silmeler zaten commit edildi.
        var diskFileNames = await fileStorage.ListAsync(cancellationToken).ConfigureAwait(false);
        if (diskFileNames.Count > 0)
        {
            var knownFileNames = (await storedFileRepository.GetListAsync(f => true, cancellationToken).ConfigureAwait(false))
                .Select(f => f.GeneratedFileName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var name in diskFileNames.Where(n => !knownFileNames.Contains(n)))
            {
                await fileStorage.DeleteAsync(name, cancellationToken).ConfigureAwait(false);
            }
        }
```

> **Testlerdeki `IFileStorage` mock'ları** `ListAsync` kurulmadığında boş liste döner (Moq varsayılanı `null` olabilir — `_fileStorage.Setup(s => s.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);` ekle, aksi hâlde `NullReferenceException` alırsın).

- [ ] **Step 6: Tüm test takımını çalıştır ve commit**

```bash
dotnet build && dotnet test
git add src/Core src/Business tests/Business.Tests/MaintenanceManagerTests.cs
git commit -m "Faz 33 adim 6: 90 gunluk evrak saklama ve sahipsiz dosya temizligi (K-37, A-63)"
```

---

### Task 7: Arayüz — başvuru sayfası, inceleme ve katalog sekmesi

**Files:**
- Modify: `arayuz/src/api/types.ts`, `arayuz/src/App.tsx`
- Create: `arayuz/src/pages/ClubApplicationPage.tsx`, `arayuz/src/schemas/clubDocumentTypeForm.ts`
- Modify: `arayuz/src/pages/ClubsPage.tsx`, `arayuz/src/pages/ClubApplicationsReviewPage.tsx`, `arayuz/src/pages/ReferenceDataPage.tsx`, `arayuz/src/api/download.ts`

**Interfaces:**
- Consumes: `/api/club-document-types` (Task 3), multipart `POST /api/club-applications` (Task 4), indirme ucu (Task 5)
- Produces: `arayuz/src/api/types.ts` → `ClubDocumentTypeListItemDto`, `ClubApplicationDocumentDto`

- [ ] **Step 1: TS tiplerini ekle**

`arayuz/src/api/types.ts`:

```typescript
// src/Business/DTOs/Reference/ClubDocumentTypeListItemDto.cs
export interface ClubDocumentTypeListItemDto {
  id: number
  code: string
  name: string
  isRequired: boolean
  isActive: boolean
  displayOrder: number
}

// src/Business/DTOs/ClubApplications/ClubApplicationDocumentDto.cs
export interface ClubApplicationDocumentDto {
  documentId: number
  documentTypeId: number
  code: string
  name: string
  isRequired: boolean
  originalFileName: string
  fileSizeBytes: number
}
```

`ClubApplicationListItemDto` arayüzüne ekle:

```typescript
  documents: ClubApplicationDocumentDto[]
```

- [ ] **Step 2: Başvuru sayfasını oluştur**

`arayuz/src/pages/ClubApplicationPage.tsx` (yeni dosya). Ekran görüntüsündeki düzen: **Topluluk Bilgileri** → **Zorunlu Evraklar** → tek gönder düğmesi.

```tsx
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Alert, Box, Button, MenuItem, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useNotifier } from '../notifications/NotifierProvider'
import { PageHeader } from '../components/ui/PageHeader'
import { SectionCard } from '../components/ui/SectionCard'
import { clubApplicationFormSchema, emptyClubApplicationFormValues, type ClubApplicationFormValues } from '../schemas/clubApplicationForm'
import type { ClubDocumentTypeListItemDto, PagedResult, SelectableAcademicStaffDto } from '../api/types'
import { useDocumentTitle } from '../hooks/useDocumentTitle'

export function ClubApplicationPage() {
  useDocumentTitle('Topluluk Kuruluş Başvurusu')

  const navigate = useNavigate()
  const notify = useNotifier()
  const [files, setFiles] = useState<Record<number, File>>({})

  const { control, handleSubmit } = useForm<ClubApplicationFormValues>({
    resolver: zodResolver(clubApplicationFormSchema),
    defaultValues: emptyClubApplicationFormValues,
  })

  const documentTypesQuery = useQuery({
    queryKey: ['club-document-types', 'active'],
    queryFn: async () =>
      (
        await apiClient.get<PagedResult<ClubDocumentTypeListItemDto>>('/club-document-types', {
          params: { pageIndex: 0, pageSize: 100, activeOnly: true },
        })
      ).data,
  })

  const advisorsQuery = useQuery({
    queryKey: ['academic-staff-selectable'],
    queryFn: async () => (await apiClient.get<SelectableAcademicStaffDto[]>('/academic-staff/selectable')).data,
  })

  const submitMutation = useMutation({
    mutationFn: async (values: ClubApplicationFormValues) => {
      const formData = new FormData()
      formData.append('ProposedName', values.proposedName)
      formData.append('Description', values.description)
      formData.append('Justification', values.justification)
      formData.append('ProposedAdvisorId', String(values.proposedAdvisorId))

      // Alan adları backend'in bağlama modeliyle birebir: Documents[i].DocumentTypeId / .File
      Object.entries(files).forEach(([typeId, file], index) => {
        formData.append(`Documents[${index}].DocumentTypeId`, typeId)
        formData.append(`Documents[${index}].File`, file)
      })

      await apiClient.post('/club-applications', formData)
    },
    onSuccess: () => {
      notify({ message: 'Başvurunuz alındı, yönetici onayı bekleniyor.', severity: 'success' })
      navigate('/basvurularim')
    },
    // Y-35: "hangi evrak eksik" kararı API'nin; mesajı olduğu gibi gösteriyoruz.
    onError: (error) => notify({ message: extractErrorMessage(error, 'Başvuru gönderilemedi.'), severity: 'error' }),
  })

  const documentTypes = documentTypesQuery.data?.items ?? []

  return (
    <>
      <PageHeader
        title="Yeni Topluluk Kuruluş Başvurusu"
        description="Tüm alanları eksiksiz doldurun. Zorunlu evrakların tamamı PDF olarak yüklenmelidir."
      />

      <SectionCard>
        <Typography variant="subtitle2" sx={{ mb: 2, fontWeight: 700 }}>
          TOPLULUK BİLGİLERİ
        </Typography>
        <Stack spacing={2}>
          <Controller
            name="proposedName"
            control={control}
            render={({ field, fieldState }) => (
              <TextField {...field} fullWidth required label="Topluluk Adı" error={!!fieldState.error} helperText={fieldState.error?.message} />
            )}
          />
          <Controller
            name="description"
            control={control}
            render={({ field }) => <TextField {...field} fullWidth multiline minRows={3} label="Topluluk Açıklaması" />}
          />
          <Controller
            name="justification"
            control={control}
            render={({ field, fieldState }) => (
              <TextField {...field} fullWidth required multiline minRows={2} label="Gerekçe" error={!!fieldState.error} helperText={fieldState.error?.message} />
            )}
          />
          <Controller
            name="proposedAdvisorId"
            control={control}
            render={({ field, fieldState }) => (
              <TextField
                {...field}
                select
                fullWidth
                required
                label="Akademik Danışman"
                onChange={(event) => field.onChange(Number(event.target.value))}
                error={!!fieldState.error}
                helperText={fieldState.error?.message}
              >
                <MenuItem value={0}>— Danışman Seçiniz —</MenuItem>
                {/* A-56: DTO'da `title` + `fullName` var, tek bir `displayName` alanı YOK. */}
                {(advisorsQuery.data ?? []).map((advisor) => (
                  <MenuItem key={advisor.id} value={advisor.id}>
                    {`${advisor.title} ${advisor.fullName}`.trim()}
                  </MenuItem>
                ))}
              </TextField>
            )}
          />
        </Stack>
      </SectionCard>

      <SectionCard sx={{ mt: 2 }}>
        <Typography variant="subtitle2" sx={{ mb: 1, fontWeight: 700 }}>
          ZORUNLU EVRAKLAR
        </Typography>
        <Alert severity="info" sx={{ mb: 2 }}>
          Evraklar yalnızca PDF olarak yüklenebilir. Dosya başına en fazla 5 MB.
        </Alert>

        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }, gap: 2 }}>
          {documentTypes.map((type) => (
            <Box key={type.id} sx={{ p: 2, border: 1, borderColor: 'divider', borderRadius: 1 }}>
              <Typography variant="body2" sx={{ fontWeight: 600, mb: 1 }}>
                {type.code} {type.name} {type.isRequired && <span style={{ color: 'red' }}>*</span>}
              </Typography>
              <input
                type="file"
                accept="application/pdf"
                onChange={(event) => {
                  const file = event.target.files?.[0]
                  setFiles((current) => {
                    const next = { ...current }
                    if (file) {
                      next[type.id] = file
                    } else {
                      delete next[type.id]
                    }
                    return next
                  })
                }}
              />
            </Box>
          ))}
        </Box>
      </SectionCard>

      <Box sx={{ mt: 3 }}>
        <Button
          variant="contained"
          size="large"
          disabled={submitMutation.isPending}
          onClick={handleSubmit((values) => submitMutation.mutate(values))}
        >
          Başvuruyu Gönder
        </Button>
      </Box>
    </>
  )
}
```

> **Y-35 kontrolü:** sayfa "zorunlu evrak eksik" kararını **vermiyor** — düğme her zaman aktif, kararı API veriyor ve mesajını gösteriyoruz. Eksik evrağı önden işaretlemek istersen bu bir *kolaylık* olur, yetki değil; API muhafızı yerinde kalır.
> **Faz 32 uygulandıysa** Topluluk Bilgileri bölümüne kategori seçicisini ve `formData.append('ProposedCategoryId', …)` satırını ekle.

- [ ] **Step 3: Rotayı ekle ve diyaloğu kaldır**

`arayuz/src/App.tsx` — korumalı rotalar arasına:

```tsx
<Route path="/topluluk-kur" element={<ClubApplicationPage />} />
```

`arayuz/src/pages/ClubsPage.tsx`:
- "Topluluk Kurmak İstiyorum" düğmesini diyalog açmak yerine `navigate('/topluluk-kur')` yapacak şekilde değiştir.
- Mevcut başvuru diyaloğunu, ilgili `useForm`, `useMutation`, `useFormDialog` ve `clubApplicationForm` import'larını **kaldır** — iki yerde iki form bakımı Y-29'un "tekrarlama" gerekçesiyle aynı sınıf.
- **Faz 31 uygulandıysa** pencere kontrolünü koru: düğme kapalıyken pasif kalmalı.

- [ ] **Step 4: İnceleme ekranına evrakları ekle**

`arayuz/src/pages/ClubApplicationsReviewPage.tsx`:

**(a)** İndirme yardımcısı. `arayuz/src/api/download.ts` **`downloadBlob(url, fallbackFileName)`** fonksiyonunu dışa aktarır (blob alma, `Content-Disposition` çözme ve geçici nesne URL'i onun içinde) — kendi blob kodunu yazma:

```tsx
  const downloadDocument = async (applicationId: number, document: ClubApplicationDocumentDto) => {
    try {
      // A-36: Authorization başlığı <a href> ile gönderilemez — downloadBlob axios blob'u kullanır.
      await downloadBlob(`/club-applications/${applicationId}/documents/${document.documentId}`, document.originalFileName)
    } catch (error) {
      notify({ message: extractErrorMessage(error, 'Evrak indirilemedi.'), severity: 'error' })
    }
  }
```

Import: `import { downloadBlob } from '../api/download'`

**(b)** `DataTable`'a genişletilebilir satır ekle — en basit yol, satır seçildiğinde bir `SectionCard` açmak:

```tsx
  const [expanded, setExpanded] = useState<ClubApplicationListItemDto | null>(null)
```

`columns` dizisine:

```tsx
    {
      field: 'documents',
      headerName: 'Evraklar',
      width: 120,
      sortable: false,
      filterable: false,
      renderCell: (params) => (
        <Button size="small" onClick={() => setExpanded(params.row)}>
          {params.row.documents.length} evrak
        </Button>
      ),
    },
```

Ve tablonun altına evrak paneli:

```tsx
      {expanded && (
        <SectionCard sx={{ mt: 2 }} action={<Button size="small" onClick={() => setExpanded(null)}>Kapat</Button>}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
            {expanded.proposedName} — Evraklar
          </Typography>
          <Stack spacing={1}>
            {expanded.documents.map((document) => (
              <Stack key={document.documentId} direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                <Typography variant="body2" sx={{ flex: 1 }}>
                  {document.code} {document.name}
                </Typography>
                <Button size="small" variant="outlined" onClick={() => downloadDocument(expanded.id, document)}>
                  İndir
                </Button>
              </Stack>
            ))}
            {expanded.documents.length === 0 && (
              <Typography variant="body2" color="text.secondary">
                Bu başvuruya evrak yüklenmemiş.
              </Typography>
            )}
          </Stack>
        </SectionCard>
      )}
```

- [ ] **Step 5: Referans Verisi'ne katalog sekmesini ekle**

`arayuz/src/pages/ReferenceDataPage.tsx` — "Kuruluş Evrakları" sekmesi. **Faz 32'nin `ClubCategoriesTab`'ıyla aynı iskelet**, farkları: kolonlar (`code`, `name`, `isRequired`, `isActive`, `displayOrder`), form alanları (kod, ad, iki `Switch`, sıra), uçlar `/club-document-types`.

Yeni form şeması `arayuz/src/schemas/clubDocumentTypeForm.ts`:

```typescript
import { z } from 'zod'

// Y-35: yalnızca biçim. "Bu kod alınmış mı" ve "zorunlu evraklar tam mı" kararları API'nin.
export const clubDocumentTypeFormSchema = z.object({
  code: z.string().min(1, 'Kod gerekli.').max(50),
  name: z.string().min(1, 'Ad gerekli.').max(300),
  isRequired: z.boolean(),
  isActive: z.boolean(),
  displayOrder: z.number().int().min(0),
})

export type ClubDocumentTypeFormValues = z.infer<typeof clubDocumentTypeFormSchema>

export const emptyClubDocumentTypeFormValues: ClubDocumentTypeFormValues = {
  code: '',
  name: '',
  isRequired: true,
  isActive: true,
  displayOrder: 0,
}
```

- [ ] **Step 6: Build ve lint çalıştır**

```bash
cd arayuz && npm run build && npm run lint
```

- [ ] **Step 7: Uygulamayı elle doğrula**

1. Yönetici → **Referans Verisi → Kuruluş Evrakları**: sekiz FR formu listelenmeli. Yeni bir tip ekle (`FR-0299`, zorunlu değil).
2. Öğrenci → **Kulüpler → Topluluk Kurmak İstiyorum** → `/topluluk-kur` sayfası açılmalı, evrak kartları katalogtan gelmeli, zorunlular `*` ile.
3. Eksik evrakla gönder → **"Zorunlu evrakların tamamı yüklenmeden…"** mesajı.
4. Evrak yerine PNG yükle → **"yalnızca PDF"** mesajı.
5. Tam evrakla gönder → başvuru alınmalı.
6. Yönetici → **Topluluk Kurma Başvuruları** → "8 evrak" düğmesi → her evrak indirilebilmeli, PDF açılmalı.
7. Kulüp logosuna PDF yüklemeyi dene → **reddedilmeli** (A-64 regresyonu).

- [ ] **Step 8: Commit**

```bash
git add arayuz/src
git commit -m "Faz 33 adim 7: basvuru sayfasi, evrak inceleme ve katalog sekmesi (K-37)"
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

- [ ] **Katman testini özellikle çalıştır (Tuzak 2)**

```bash
dotnet test tests/Architecture.Tests/Architecture.Tests.csproj
```

`LayerDependencyTests` yeşil olmalı — `IFormFile` Business'a sızmamış olmalı.

- [ ] **"Bitti sayılır" kontrolü** (`docs/PLAN-V6.md` §Faz 33)

| Koşul | Nasıl doğrulanır |
|---|---|
| Sekiz zorunlu evrak tek düğmeyle yükleniyor | `Submit_AllRequiredDocuments_IsAcceptedAndStoredProtected` + elle doğrulama 5 |
| Eksik evrakla başvuru reddediliyor, eksik olan söyleniyor | `Submit_MissingRequiredDocuments_*` + `Messages.MissingRequiredClubDocuments` |
| İnceleyici her evrağı açabiliyor | `DocumentDownload_EnforcesOwnershipAndVisibility` (2. adım) + elle doğrulama 6 |
| **Aynı evrak anonim uçtan indirilemiyor** | `DocumentDownload_…` (4. adım) + `PublicSurfaceLeakTests` |
| **Kulüp logosuna PDF yüklenemiyor** | `UploadClubLogo_PdfContent_ReturnsBadRequest` + elle doğrulama 7 |
| Reddedilen başvurunun evrakları 90 gün sonra siliniyor | `RunNightlyMaintenanceAsync_OldRejectedApplication_DeletesDocuments` |
| Onaylananınki duruyor | `RunNightlyMaintenanceAsync_OldApprovedApplication_KeepsDocuments` |

- [ ] **Faz commit'i**

```bash
git log --oneline master..HEAD
```

Yedi adım commit'i görünmeli.

---

## Sonraki Faz

Faz 34 (Dinamik topluluk içi roller) bu faza **bağlı değil.**
