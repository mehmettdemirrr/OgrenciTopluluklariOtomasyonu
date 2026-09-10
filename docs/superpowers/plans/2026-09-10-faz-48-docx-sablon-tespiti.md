# Faz 48 — Şablon Yüklemede Docx Tespiti Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Word'ün ürettiği gerçek `.docx` dosyaları evrak şablonu olarak yüklenebilsin; xlsx/pptx/düz ZIP reddedilmeye devam etsin.

**Architecture:** OOXML tespiti ham baytta dize aramayı bırakır, paketi `System.IO.Compression.ZipArchive` ile **açarak** doğrular: `word/document.xml` girdisi var mı ve `[Content_Types].xml` **içeriği** `wordprocessingml` taşıyor mu. Bu iki soru ancak paket açılınca cevaplanabilir; ZIP girdi adları sıkıştırılmadan durduğu için ilk soru ham baytta da görünür, ama ikincisi deflate ile sıkıştırılmış olduğu için görünmez — mevcut hatanın kaynağı tam olarak budur.

**Tech Stack:** .NET 8 (`System.IO.Compression` — BCL, yeni bağımlılık yok), xUnit.

**Spec:** `docs/MIMARI.md` (v6.14 → v6.15 bu fazda). İlgili kararlar: A-64 (izin verilen tip çağrı yerine göre belirlenir), Y-40 (içerik imzasıyla tip kontrolü), K-37 (kuruluş evrakları).

## Kök neden (uygulamadan önce oku)

`4f36aa8` commit'i `FileSignatureInspector.LooksLikeDocx` içinde şunu yaptı:

```csharp
content.IndexOf("word/"u8) >= 0 && content.IndexOf("wordprocessingml"u8) >= 0
```

Depodaki gerçek şablonlarda ölçüldü (`grep -c -a -F`, `arayuz/public/club-document-templates/FR-0239.docx`):

| aranan dize | ham baytta bulundu mu |
|---|---|
| `[Content_Types].xml` (ZIP girdi **adı**) | 2 kez |
| `word/document.xml` (ZIP girdi **adı**) | 2 kez |
| `wordprocessingml` (o XML'in **içeriği**) | **0 kez** |

ZIP girdi adları yerel başlıkta sıkıştırılmadan yazılır, dosya içerikleri deflate ile sıkıştırılır. `wordprocessingml` yalnızca `[Content_Types].xml`'in **içinde** geçtiği için ham baytlarda hiç bulunmaz → koşul daima `false` → `DetectContent` `Unknown` → yükleme "desteklenmeyen tip" ile reddedilir. **Word'ün ürettiği hiçbir .docx geçemez; yalnızca PDF çalışır.**

Testlerin bunu yakalamama sebebi: hem `FileSignatureInspectorTests.FakeOfficeBytes` hem `FileManagerTests.FakeOfficeBytes`, ZIP sihirli baytının ardına iki dizeyi de **sıkıştırmadan** düz ASCII olarak yazan sahte bir tampon üretiyor. Gerçek bir OOXML paketi hiç denenmemiş.

## Global Constraints

- Belge önce, kod sonra (Task 1).
- Çalışma dalı `master`; faz başına **tek commit**; commit mesajında araç imzası/süreç anlatımı yok.
- **Y-40 gevşetilmez:** tip yalnızca içerikten belirlenir; istemcinin uzantısı/`Content-Type` başlığı hiçbir dalda okunmaz.
- **A-64 gevşetilmez:** Docx **yalnızca** `DocumentTemplateTypes` kümesinde kalır. Başvuru evrakı (`DocumentTypes`) PDF-only, görsel yolları (`ImageTypes`) JPEG/PNG/WebP olarak kalır.
- Paket açma untrusted girdi üzerinde çalışır: yalnızca girdi **adları** listelenir ve **tek** küçük girdi (`[Content_Types].xml`) okunur; okuma öncesi boyut tavanı uygulanır. Tüm arşiv belleğe açılmaz.
- Y-34: her test kendi verisini üretir.
- Test komutları: `dotnet test`; `cd arayuz && npm run build && npm run lint`.

---

### Task 1: MIMARI'ye kapsam, karar ve kuralı yaz (belge önce)

**Files:**
- Modify: `docs/MIMARI.md`

**Interfaces:**
- Produces: K-52, A-83, Y-88.

- [x] **Step 1: Sürüm ve sayaçlar**

`**Sürüm:** v6.14` → `v6.15`; kronolojik listenin sonuna kalın olarak ekle (mevcut son girdi `v6.14`'ün kalınlığını kaldır):

```markdown
· **v6.15: 10 Eylül 2026 (K-52, A-83, Y-88, Faz 48 — evrak şablonu ve OOXML tespiti)**
```

Giriş paragrafının sonuna: `**v6.15** evrak tipi kataloğuna indirilebilir şablon ekleyip OOXML tespitini paket açarak yapmaya çevirerek 1 kapsam maddesi (K-52), 1 karar (A-83) ve 1 kural (Y-88) ekledi.`

Sayaç tablosu: `Karar | 83 (… + 1 v6.15)`, `Yasak kural | 88 (… + 1 v6.15)`, `Uygulama fazı | 48 (… + 1 v6.15)`.
İçindekiler satırları: `Y-01 … Y-88`, `K-01 … K-52`, `A-01 … A-83`.

- [x] **Step 2: K-52'yi K-51'in altına ekle**

```markdown
| **K-52** | **Evrak şablonu** | Admin, evrak tipi kataloğundaki her tipe boş kurumsal formu (Word veya PDF) yükler; başvuru sahibi formu başvuru sayfasından indirir. Şablon boş formdur, doldurulmuş evrak değildir | `ClubDocumentType.TemplateFileId`, `POST/GET /api/club-document-types/{id}/template`, `IFileService.StoreDocumentTemplateAsync` (A-83, Y-88, A-64) |
```

- [x] **Step 3: A-83'ü A-82'nin altına ekle**

```markdown
| **A-83** | OOXML tespiti paketi **açarak** yapılır, ham baytta dize aranmaz | A | `.docx` bir ZIP paketidir: girdi **adları** yerel başlıkta sıkıştırılmadan durur, girdi **içerikleri** deflate ile sıkıştırılır. Bu yüzden `word/document.xml` ham baytta görünür ama `[Content_Types].xml`'in içindeki `wordprocessingml` **görünmez** — dize taramasıyla yazılan ilk sürüm (Faz 47 sonrası) Word'ün ürettiği her dosyayı reddediyordu. Tespit `ZipArchive` ile yapılır: `word/document.xml` girdisi aranır **ve** `[Content_Types].xml` okunup içeriğinde `wordprocessingml` doğrulanır; ikisi birlikte olmadan Docx sayılmaz (xlsx `xl/`+`spreadsheetml`, pptx `ppt/`+`presentationml` taşır). Untrusted girdi olduğu için yalnızca girdi adları listelenir ve tek küçük girdi, boyut tavanıyla okunur. Şablonun boş form olması `Public` görünürlüğü haklı çıkarır; doldurulmuş evrak A-63 gereği `Protected` kalır (K-52, Y-88, Y-40) |
```

- [x] **Step 4: Y-88'i Y-87'nin altına ekle**

```markdown
| **Y-88** | Sıkıştırılmış bir kapsayıcının (ZIP/OOXML) tipini ham baytta dize arayarak doğrulamak; ZIP sihirli baytını tek başına Docx saymak | Kapsayıcı `ZipArchive` ile açılır, girdi adı **ve** içerik tipi birlikte doğrulanır (A-83). Gerekçe: sıkıştırılmış içerikteki bir dize ham baytlarda bulunmaz — böyle bir kontrol üretimde **her zaman** yanlış negatif verir ve sahte içerikle yazılmış bir test bunu gizler. Regresyon, gerçek bir OOXML paketiyle (test içinde `ZipArchive` ile üretilmiş, sıkıştırılmış) kilitlenir; ham baytta işaretin bulunmadığı testte açıkça doğrulanır (K-52, A-83, Y-40) |
```

- [x] **Step 5: Sayaç bütünlüğünü doğrula**

```bash
grep -oE '^\| \*\*[AYK]-[0-9]+\*\*' docs/MIMARI.md | sort | uniq -c | awk '$1>1'
```
Beklenen: yalnızca `2 | **K-13**` (belgede açıklanan kasıtlı bölünme).

---

### Task 2: Gerçek OOXML paketiyle başarısız testi yaz

**Files:**
- Modify: `tests/Business.Tests/FileSignatureInspectorTests.cs`

**Interfaces:**
- Produces: `BuildOfficePackage(string entryPath, string contentTypeMarker)` — testlerin paylaştığı gerçek ZIP üreteci.

- [x] **Step 1: Sahte üreteci gerçeğiyle değiştir**

`FakeOfficeBytes` metodunu **sil** ve yerine dosyanın sonuna şunu ekle (dosyanın başına `using System.IO.Compression;` ve `using System.Text;` ekle):

```csharp
    /// <summary>
    /// Gerçek bir OOXML paketi üretir: girdi ADLARI sıkıştırılmadan, girdi İÇERİKLERİ deflate ile
    /// yazılır — Word'ün ürettiği dosyanın davranışı. Sahte (düz ASCII) tampon, A-83'teki hatayı gizler.
    /// </summary>
    private static byte[] BuildOfficePackage(string entryPath, string contentTypeMarker)
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            var contentTypes = archive.CreateEntry("[Content_Types].xml", CompressionLevel.Optimal);
            using (var writer = new StreamWriter(contentTypes.Open()))
            {
                // Deflate'in gerçekten sıkıştırma seçmesi için tekrarlı ve yeterince uzun içerik.
                writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types>");
                for (var i = 0; i < 40; i++)
                {
                    writer.Write($"<Default Extension=\"rels{i}\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
                }

                writer.Write($"<Override PartName=\"/{entryPath}\" ContentType=\"application/vnd.openxmlformats-officedocument.{contentTypeMarker}.document.main+xml\"/></Types>");
            }

            var document = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
            using var documentWriter = new StreamWriter(document.Open());
            documentWriter.Write("<w:document><w:body/></w:document>");
        }

        return buffer.ToArray();
    }
```

- [x] **Step 2: Regresyon testini yaz**

`DetectContent_DocxZip_ReturnsDocx` testini şununla **değiştir**:

```csharp
    [Fact(DisplayName = "A-83: Word'ün ürettiği gerçek .docx paketi Docx olarak tanınır")]
    public void DetectContent_RealDocxPackage_ReturnsDocx()
    {
        var content = BuildOfficePackage("word/document.xml", "wordprocessingml");

        // Hatanın kendisi: işaret SIKIŞTIRILMIŞ girdinin içindedir, ham baytlarda yoktur.
        // Bu satır düşerse test artık hatayı üretmiyordur — dize taraması yeniden geçer hâle gelir.
        Assert.DoesNotContain("wordprocessingml", Encoding.ASCII.GetString(content), StringComparison.Ordinal);

        var detected = FileSignatureInspector.DetectContent(content);

        Assert.Equal(DetectedFileType.Docx, detected);
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", detected.ToContentType());
        Assert.Equal(".docx", detected.ToExtension());
    }
```

- [x] **Step 3: xlsx ve düz ZIP testlerini gerçek pakete çevir**

`DetectContent_XlsxZip_ReturnsUnknown` içindeki `LooksLikeDocx` iddiasını **kaldır** (metot Task 3'te siliniyor) ve iki testi de yeni üretece bağla:

```csharp
    [Fact(DisplayName = "Y-88: xlsx (xl/ + spreadsheetml) Docx sayılmaz")]
    public void DetectContent_XlsxPackage_ReturnsUnknown()
    {
        var content = BuildOfficePackage("xl/workbook.xml", "spreadsheetml");

        Assert.Equal(DetectedFileType.Unknown, FileSignatureInspector.DetectContent(content));
    }

    [Fact(DisplayName = "Y-88: düz ZIP Docx sayılmaz")]
    public void DetectContent_PlainZip_ReturnsUnknown()
    {
        var content = BuildOfficePackage("readme.txt", "plain-text");

        Assert.Equal(DetectedFileType.Unknown, FileSignatureInspector.DetectContent(content));
    }

    [Fact(DisplayName = "Y-88: bozuk/kesik ZIP çökmez, Unknown döner")]
    public void DetectContent_TruncatedZip_ReturnsUnknown()
    {
        var content = BuildOfficePackage("word/document.xml", "wordprocessingml");
        var truncated = content.AsSpan(0, content.Length / 2).ToArray();

        Assert.Equal(DetectedFileType.Unknown, FileSignatureInspector.DetectContent(truncated));
    }
```

- [x] **Step 4: Depodaki gerçek şablonla ikinci güvenceyi yaz**

Aynı dosyanın sonuna, üretilmiş paketin yanına gerçek dosyayı da koy:

```csharp
    [Fact(DisplayName = "A-83: depodaki gerçek MTÜ şablonu (FR-0239.docx) kabul edilir")]
    public void DetectContent_RepositoryTemplate_ReturnsDocx()
    {
        var path = Path.Combine(FindRepositoryRoot(), "arayuz", "public", "club-document-templates", "FR-0239.docx");
        Assert.True(File.Exists(path), $"Şablon bulunamadı: {path}");

        Assert.Equal(DetectedFileType.Docx, FileSignatureInspector.DetectContent(File.ReadAllBytes(path)));
    }

    /// <summary>Architecture.Tests'teki SolutionPaths ile aynı yöntem: *.slnx dosyasına kadar yukarı yürü.</summary>
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && directory.GetFiles("*.slnx").Length == 0)
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Çözüm kökü (*.slnx) bulunamadı: " + AppContext.BaseDirectory);
    }
```

- [x] **Step 5: Çalıştır, kırmızı olduğunu gör**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~FileSignatureInspectorTests"
```
Beklenen: önce derleme hatası (`FakeOfficeBytes` silindi, `FileManagerTests` onu kullanıyor) — **Task 4'e kadar normaldir**; `FileManagerTests`'i geçici olarak düzeltmeden önce yalnızca bu dosyayı derlemek mümkün olmadığından Task 4'ü Task 3'ten hemen sonra tamamla. Derleme düzeldiğinde beklenen sonuç: `DetectContent_RealDocxPackage_ReturnsDocx` ve `DetectContent_RepositoryTemplate_ReturnsDocx` **FAIL** (`Unknown` dönüyor).

---

### Task 3: Tespiti ZipArchive'e çevir

**Files:**
- Modify: `src/Core/Utilities/Files/FileSignatureInspector.cs:47-70`
- Modify: `src/Business/Concrete/FileManager.cs:204`

**Interfaces:**
- Consumes: yok.
- Produces: `FileSignatureInspector.DetectContent(byte[] content)` → `DetectedFileType`. `LooksLikeDocx` **kaldırılır**; `IsZipLocalFileHeader` kalır.

- [x] **Step 1: `DetectContent`'i byte[] alacak şekilde değiştir**

`FileSignatureInspector.cs` dosyasının başına `using System.IO.Compression;` ekle, sonra mevcut `DetectContent` + `LooksLikeDocx` bloğunu şununla değiştir:

```csharp
    /// <summary>
    /// docs/MIMARI.md · A-83: header imzası, sonra OOXML paket doğrulaması. ZIP sihirli baytı tek
    /// başına Docx sayılmaz; paket açılıp girdi adı ve içerik tipi birlikte doğrulanır (Y-88).
    /// </summary>
    public static DetectedFileType DetectContent(byte[] content)
    {
        var headerLength = Math.Min(12, content.Length);
        var detected = Detect(content.AsSpan(0, headerLength));
        if (detected != DetectedFileType.Unknown)
        {
            return detected;
        }

        return IsZipLocalFileHeader(content) && IsWordPackage(content)
            ? DetectedFileType.Docx
            : DetectedFileType.Unknown;
    }

    public static bool IsZipLocalFileHeader(ReadOnlySpan<byte> content) =>
        content.Length >= 4 &&
        content[0] == 0x50 && content[1] == 0x4B && content[2] == 0x03 && content[3] == 0x04;

    /// <summary>Untrusted paket: yalnızca girdi adları listelenir, tek küçük girdi tavanla okunur.</summary>
    private const int MaxContentTypesBytes = 64 * 1024;

    private static bool IsWordPackage(byte[] content)
    {
        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            if (archive.GetEntry("word/document.xml") is null)
            {
                return false;
            }

            var contentTypes = archive.GetEntry("[Content_Types].xml");
            if (contentTypes is null || contentTypes.Length > MaxContentTypesBytes)
            {
                return false;
            }

            using var entryStream = contentTypes.Open();
            using var reader = new StreamReader(entryStream);
            return reader.ReadToEnd().Contains("wordprocessingml", StringComparison.Ordinal);
        }
        catch (InvalidDataException)
        {
            // Bozuk/kesik arşiv: tip doğrulanamadı → Unknown (çağıran reddeder).
            return false;
        }
    }
```

- [x] **Step 2: Çağrı yerini doğrula**

`FileManager.StoreFileAsync` zaten `var bytes = buffer.ToArray();` yapıp `FileSignatureInspector.DetectContent(bytes)` çağırıyor — imza `byte[]` olduğu için **değişiklik gerekmez**. Yalnızca derlemenin temiz olduğunu doğrula:

```bash
dotnet build
```
Beklenen: `src/` tarafında hata yok.

---

### Task 4: `FileManagerTests`'i gerçek pakete çevir

**Files:**
- Modify: `tests/Business.Tests/FileManagerTests.cs:260-310`

- [x] **Step 1: Yerel sahte üreteci sil, gerçeğini ekle**

`FileManagerTests` içindeki `FakeOfficeBytes` metodunu **sil**, dosyanın başına `using System.IO.Compression;` ekle ve sınıfın sonuna şunu ekle:

```csharp
    /// <summary>
    /// Gerçek bir OOXML paketi üretir: girdi ADLARI sıkıştırılmadan, girdi İÇERİKLERİ deflate ile
    /// yazılır — Word'ün ürettiği dosyanın davranışı (A-83).
    /// </summary>
    private static byte[] BuildOfficePackage(string entryPath, string contentTypeMarker)
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            var contentTypes = archive.CreateEntry("[Content_Types].xml", CompressionLevel.Optimal);
            using (var writer = new StreamWriter(contentTypes.Open()))
            {
                writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types>");
                for (var i = 0; i < 40; i++)
                {
                    writer.Write($"<Default Extension=\"rels{i}\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
                }

                writer.Write($"<Override PartName=\"/{entryPath}\" ContentType=\"application/vnd.openxmlformats-officedocument.{contentTypeMarker}.document.main+xml\"/></Types>");
            }

            var document = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
            using var documentWriter = new StreamWriter(document.Open());
            documentWriter.Write("<w:document><w:body/></w:document>");
        }

        return buffer.ToArray();
    }
```

Bu metot `FileSignatureInspectorTests` içindekiyle aynıdır; iki test sınıfı için ortak bir yardımcı sınıf çıkarmak bu fazın kapsamı dışıdır ve kopya bilinçlidir.

- [x] **Step 2: Şablon testlerini gerçek paketle çalıştır**

`StoreDocumentTemplateAsync_AcceptsDocx_AsPublic` ve `StoreDocumentTemplateAsync_RejectsXlsx` testlerindeki çağrıları güncelle:

```csharp
        var docx = BuildOfficePackage("word/document.xml", "wordprocessingml");
```
```csharp
        var xlsx = BuildOfficePackage("xl/workbook.xml", "spreadsheetml");
```

- [x] **Step 3: Testleri çalıştır**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~FileSignatureInspectorTests|FullyQualifiedName~FileManagerTests"
```
Beklenen: tamamı PASS. `StoreDocumentTemplateAsync_AcceptsDocx_AsPublic` artık gerçek bir paketle geçiyor.

---

### Task 5: Eksik dosya alanında 500 yerine 400 dön

**Files:**
- Modify: `src/WebAPI/Controllers/ClubDocumentTypesController.cs:49`

- [x] **Step 1: Null kontrolünü ekle**

`UploadTemplate` gövdesinin ilk satırı olarak:

```csharp
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { status = 400, title = Messages.UnsupportedDocumentTemplateType });
        }
```

`using Business.Constants;` ekle. Gerekçe: form alanı adı yanlış geldiğinde `file` null olur ve `file.OpenReadStream()` `NullReferenceException` → 500 üretir; istemci hatası 400 olmalıdır.

- [x] **Step 2: Derle**

```bash
dotnet build
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

- Referans Verileri → Evrak Tipleri: bir tipe `arayuz/public/club-document-templates/FR-0239.docx` dosyasını yükle → **başarılı** olmalı (bu faz öncesinde "desteklenmeyen tip" veriyordu).
- Aynı yere bir `.xlsx` yüklemeyi dene → 400 gelmeli.
- Aynı yere bir `.pdf` yükle → başarılı olmalı (mevcut davranış bozulmadı).
- Kulüp logosu ucuna `.docx` yüklemeyi dene → 400 gelmeli (A-64: Docx yalnızca şablon yolunda).
- Başvuru sayfasından şablonu indir → dosya `.docx` olarak açılmalı.

- [x] **Step 3: Commit**

```bash
git add -A -- docs/MIMARI.md docs/superpowers/plans/2026-09-10-faz-48-docx-sablon-tespiti.md src tests
git commit -m "$(cat <<'EOF'
Faz 48: evrak sablonunda gercek docx tespiti

Docs: docs/MIMARI.md v6.15 (K-52, A-83, Y-88).
EOF
)"
```
