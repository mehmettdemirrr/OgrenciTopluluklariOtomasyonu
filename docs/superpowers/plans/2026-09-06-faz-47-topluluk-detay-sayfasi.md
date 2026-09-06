# Faz 47 — Topluluk Detay Sayfası Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Anonim vitrin topluluk detayı referans tasarıma geçsin: daire logolu kapak ve künye rozetleri (kuruluş yılı, üye, görüntülenme, etkinlik), solda "topluluk durumu / giriş" kartı + "iletişim & sosyal" kartı + paylaşım (QR) kartı, sağda "Hakkımızda" ve mevcut etkinlik/duyuru bölümleri.

**Architecture:** Sayfa iki sütuna ayrılır: sol sütun **eylem ve künye** (durum/CTA, iletişim, paylaş), sağ sütun **içerik** (hakkımızda, etkinlikler, duyurular). Kuruluş yılı `Club.FoundedYear` alanından gelir — `CreatedAtUtc` kaydın oluşturulma tarihidir, kuruluş yılı değildir ve ondan türetilirse yanlış bilgi basılır. Görüntülenme sayacı **A-77'nin aynı deseni**dir: ayrı bir yazma ucu, `ExecuteUpdateAsync` ile tek SQL, istemcide oturum başına tek sayım. İletişim ve sosyal veriler Faz 40'ta zaten var; bu faz onlara Facebook platformunu ve tasarımdaki yerleşimi ekler.

**Tech Stack:** .NET 8, EF Core 8, React 18 + MUI 9, `qrcode` (yeni ve tek bağımlılık — Task 5).

**Spec:** `docs/MIMARI.md` (v6.13 → v6.14 bu fazda). İlgili kararlar: A-74/Y-80 (iletişim ve sosyal bağlantılar), A-77 (görüntülenme deseni), A-81 (vitrin sayıları), Y-58 (vitrin sızıntısı).

## Global Constraints

- Belge önce, kod sonra (Task 1).
- Çalışma dalı `master`; faz başına **tek commit**; commit mesajında araç imzası/süreç anlatımı yok.
- **"Topluluk Yöneticilerine Ulaşın" kartı YOKTUR.** Referans tasarımın alt kısmındaki bu kart, kullanıcının kapsam dışı bıraktığı "soru sor" özelliğinin aynısıdır (mesajlaşma, gelen kutusu, moderasyon getirir). Kulübe ulaşma yolu "İLETİŞİM & SOSYAL" kartıdır (Faz 40).
- **Y-58:** vitrin DTO'su isim/öğrenci no/katılımcı listesi taşımaz — yalnızca sayı.
- **A-81:** üye sayısı güncel dönemin üyelikleri, etkinlik sayısı vitrinde görünen (Published + Public) etkinliklerdir; ikisi de `IClubStatsDal`'den gelir.
- MUI 9: `Stack`/`Typography` üzerinde `alignItems`, `gap`, `flexWrap`, `fontWeight` doğrudan prop verilmez — `sx` içine yazılır.
- Y-56: yeni hex değer yalnızca `theme/tokens.ts`'e.
- Y-34: her test kendi verisini tohumlar.
- Test komutları: `dotnet test`; `cd arayuz && npm run build && npm run lint`.

### Bağımlılıklar

- **Faz 46** hem `IClubStatsDal`'i hem `public.memberCount`/`public.eventCount` i18n anahtarlarını sağlar. Faz 46 yapılmadıysa Task 3 DAL'i bu fazda oluşturur (Faz 46 Task 2 Step 3-5 buraya kopyalanır) ve iki i18n anahtarı Task 5 Step 2'deki listeye eklenir.
- **Faz 42** (A-77, görüntülenme deseni) yapılmadıysa Task 4 deseni bu fazda kurar; yapıldıysa `IEventViewDal` ile birebir aynı biçimde `IClubViewDal` yazılır — ikinci bir çözüm icat edilmez.
- **Faz 45** (çoklu kategori) yapıldıysa künyede rozetler çoğul çizilir; yapılmadıysa tek ad kullanılır.

---

### Task 1: MIMARI'ye kapsam, karar ve kuralı yaz (belge önce)

**Files:**
- Modify: `docs/MIMARI.md`

**Interfaces:**
- Produces: K-51, A-82, Y-87.

- [x] **Step 1: Sürüm ve sayaçlar**

`**Sürüm:** v6.13` → `v6.14`; kronolojik listeye kalın olarak:

```markdown
· **v6.14: 6 Eylül 2026 (K-51, A-82, Y-87, Faz 47 — topluluk detay sayfası)**
```

Giriş paragrafına: `**v6.14** vitrin topluluk detayını künye/eylem/içerik sütunlarına ayırarak 1 kapsam maddesi (K-51), 1 karar (A-82) ve 1 kural (Y-87) ekledi.`
Sayaçlar: `Karar | 82 (… + 1 v6.14)`, `Yasak kural | 87 (… + 1 v6.14)`, `Uygulama fazı | 47 (… + 1 v6.14)`.
İçindekiler: `Y-01 … Y-87`, `K-01 … K-51`, `A-01 … A-82`.

- [x] **Step 2: K-51**

```markdown
| **K-51** | **Topluluk detay sayfası** | Vitrin detayı daire logolu kapak, künye rozetleri (kuruluş yılı, üye, görüntülenme, etkinlik) ve iki sütun taşır: solda topluluk durumu/giriş, iletişim & sosyal, paylaşım (QR); sağda "Hakkımızda", etkinlikler ve duyurular. Kulüp yöneticilerine mesaj gönderme kapsam dışıdır | `Club.FoundedYear`/`ViewCount`, `PublicClubDetailDto` künye alanları, `POST /api/public/clubs/{id}/view`, `SocialPlatform.Facebook` (A-82, Y-87) |
```

- [x] **Step 3: A-82**

```markdown
| **A-82** | Vitrin detayı **künye/eylem** ve **içerik** olarak iki sütundur; sayaç deseni tekrar edilir, yeniden icat edilmez | A | Sol sütun kullanıcının **yapabileceği** şeyi (giriş/katıl), kulübe **ulaşma** yolunu (Faz 40'ın iletişim/sosyal verisi) ve **paylaşımı** taşır; sağ sütun okunacak içeriği (hakkımızda, etkinlikler, duyurular). Görüntülenme sayacı A-77'nin **birebir aynı** desenidir: `GET` sayacı artırmaz, ayrı `POST /api/public/clubs/{id}/view` ucu `IClubViewDal.IncrementAsync` içinde `ExecuteUpdateAsync` ile tek SQL cümlesi çalıştırır (`Club.RowVersion` yüzünden oku-değiştir-kaydet çakışırdı), istemci `sessionStorage` ile oturum başına bir kez çağırır. Gerekçe: aynı sorunun ikinci kez farklı çözülmesi, iki ayrı hata yüzeyi demektir (K-51, A-77, Y-87) |
```

- [x] **Step 4: Y-87**

```markdown
| **Y-87** | Kuruluş yılını `Club.CreatedAtUtc`'den (kaydın sisteme girildiği tarih) türetip "Kuruluş: 2026" diye basmak; alan boşken yıl uydurmak | Kuruluş yılı ayrı ve isteğe bağlı bir alandır (`Club.FoundedYear`, nullable); **boşsa rozet hiç çizilmez**. Gerekçe: 2012'de kurulmuş bir topluluk sisteme 2026'da girildiyse `CreatedAtUtc` 2026'dır — türetilen değer sessizce yanlış bir kurumsal bilgidir ve kullanıcı bunu doğru sanır (K-51, A-82) |
```

---

### Task 2: Künye alanları — kuruluş yılı, sayaç ve Facebook

**Files:**
- Modify: `src/Entities/Club.cs`
- Modify: `src/Entities/Enums/SocialPlatform.cs`
- Modify: `src/Business/ValidationRules/SetClubSocialLinksRequestValidator.cs`
- Modify: `src/Business/DTOs/Clubs/ClubDetailDto.cs`, `UpdateClubRequestDto.cs`
- Modify: `src/Business/Concrete/ClubManager.cs`
- Create: `src/DataAccess/Migrations/<timestamp>_20260906_Faz47_TopluklukKunyesi.cs` (EF üretir)
- Test: `tests/Business.Tests/ClubSocialLinkValidationTests.cs`

**Interfaces:**
- Produces: `Club.FoundedYear` (int?), `Club.ViewCount` (int), `SocialPlatform.Facebook = 5`.

- [x] **Step 1: Başarısız testi yaz**

`tests/Business.Tests/ClubSocialLinkValidationTests.cs` içindeki `[Theory]` listesine ekle:

```csharp
    [InlineData(SocialPlatform.Facebook, "https://www.facebook.com/topluluk", true)]
    [InlineData(SocialPlatform.Facebook, "https://facebook.evil.com/topluluk", false)]
```

- [x] **Step 2: Çalıştır, başarısız olduğunu gör**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~ClubSocialLinkValidation"
```
Beklenen: derleme hatası (`SocialPlatform.Facebook` yok).

- [x] **Step 3: Enum'a Facebook ekle**

`src/Entities/Enums/SocialPlatform.cs` — **sona** ekle; mevcut sayıları değiştirme (Y-06: sayısal değerler veritabanında sabittir):

```csharp
    Facebook = 5,
```

`SetClubSocialLinksRequestValidator.KnownHosts` sözlüğüne (Y-80):

```csharp
            [SocialPlatform.Facebook] = ["facebook.com", "www.facebook.com", "m.facebook.com"],
```

- [x] **Step 4: `Club`'a künye alanlarını ekle**

```csharp
    /// <summary>
    /// docs/MIMARI.md · K-51/Y-87: topluluğun KURULUŞ yılı. Null = bilinmiyor (rozet çizilmez).
    /// CreatedAtUtc (kaydın sisteme girildiği an) ile karıştırılmaz.
    /// </summary>
    public int? FoundedYear { get; set; }

    /// <summary>docs/MIMARI.md · A-82/A-77: yaklaşık görüntülenme sayısı; yalnızca IClubViewDal artırır.</summary>
    public int ViewCount { get; set; }
```

- [x] **Step 5: DTO ve yönetim akışı**

- `ClubDetailDto`'ya `public int? FoundedYear { get; set; }` ve `public int ViewCount { get; set; }` (AutoMapper ad eşlemesiyle otomatik dolar).
- `UpdateClubRequestDto`'ya `public int? FoundedYear { get; set; }`.
- `UpdateClubRequestValidator`'a:

```csharp
        // Y-87: yıl, üniversitenin kuruluşundan bugüne makul bir aralıkta olmalı; boş bırakılabilir.
        RuleFor(x => x.FoundedYear)
            .InclusiveBetween(1900, DateTime.UtcNow.Year)
            .When(x => x.FoundedYear is not null);
```

- `ClubManager.UpdateAsync` içinde `club.Name = name;` satırlarının yanına `club.FoundedYear = request.FoundedYear;`.

- [x] **Step 6: Migration**

```bash
dotnet ef migrations add 20260906_Faz47_TopluklukKunyesi --project src/DataAccess
dotnet ef database update --project src/DataAccess
dotnet build
```
**Not:** `--startup-project src/WebAPI` EKLEME.

- [x] **Step 7: Testleri çalıştır**

```bash
dotnet test tests/Business.Tests
```
Beklenen: PASS (13 satırlık theory artık Facebook'u da kapsıyor).

---

### Task 3: Vitrin detayına künye sayıları

**Files:**
- Modify: `src/Business/DTOs/Public/PublicClubDetailDto.cs`
- Modify: `src/Business/Concrete/PublicContentManager.cs`
- Test: `tests/WebAPI.IntegrationTests/PublicClubDetailKunyeTests.cs`

**Interfaces:**
- Consumes: `IClubStatsDal` (Faz 46; yoksa oradaki Task 2 Step 3-5 buraya kopyalanır).
- Produces: `PublicClubDetailDto.FoundedYear/MemberCount/EventCount/ViewCount`.

- [x] **Step 1: Başarısız entegrasyon testini yaz**

```csharp
[Fact(DisplayName = "K-51: vitrin detayı künye alanlarını taşır")]
public async Task PublicClubDetail_CarriesProfileStats()
{
    var clubId = await SeedClubWithMemberAndEventAsync("kunye", foundedYear: 2012);

    var body = await _client.GetFromJsonAsync<JsonElement>($"/api/public/clubs/{clubId}");

    Assert.Equal(2012, body.GetProperty("foundedYear").GetInt32());
    Assert.Equal(1, body.GetProperty("memberCount").GetInt32());
    Assert.Equal(1, body.GetProperty("eventCount").GetInt32());
    Assert.Equal(0, body.GetProperty("viewCount").GetInt32());
}

[Fact(DisplayName = "Y-87: kuruluş yılı boşsa alan null döner, CreatedAtUtc'den türetilmez")]
public async Task PublicClubDetail_LeavesFoundedYearNull_WhenUnknown()
{
    var clubId = await SeedClubWithMemberAndEventAsync("kunye-null", foundedYear: null);

    var body = await _client.GetFromJsonAsync<JsonElement>($"/api/public/clubs/{clubId}");

    Assert.Equal(JsonValueKind.Null, body.GetProperty("foundedYear").ValueKind);
}
```

Tohumlama yardımcısını `ClubDetailVisibilityTests`'ten uyarla; kulübe güncel dönemde bir üyelik ve bir `Published+Public` etkinlik ekle, `FoundedYear`'ı parametreden yaz.

- [x] **Step 2: Çalıştır, başarısız olduklarını gör**

```bash
dotnet test tests/WebAPI.IntegrationTests --filter "FullyQualifiedName~PublicClubDetailKunyeTests"
```

- [x] **Step 3: DTO'yu genişlet**

`PublicClubDetailDto`'ya:

```csharp
    /// <summary>docs/MIMARI.md · Y-87: kuruluş yılı; null ise arayüz rozeti çizmez.</summary>
    public int? FoundedYear { get; set; }

    /// <summary>docs/MIMARI.md · A-81: güncel dönemin üye sayısı.</summary>
    public int MemberCount { get; set; }

    /// <summary>docs/MIMARI.md · A-81: vitrinde görünen etkinlik sayısı.</summary>
    public int EventCount { get; set; }

    /// <summary>docs/MIMARI.md · A-82/A-77: yaklaşık görüntülenme sayısı.</summary>
    public int ViewCount { get; set; }
```

- [x] **Step 4: `GetClubByIdAsync`'i doldur**

```csharp
        var stats = await clubStatsDal.GetCountsAsync([id], cancellationToken).ConfigureAwait(false);
        var clubStats = stats.TryGetValue(id, out var found) ? found : new ClubCardStats(0, 0);
```
ve DTO kurulumuna `FoundedYear = club.FoundedYear, MemberCount = clubStats.MemberCount, EventCount = clubStats.EventCount, ViewCount = club.ViewCount`.

- [x] **Step 5: Testleri çalıştır**

```bash
dotnet test tests/WebAPI.IntegrationTests --filter "FullyQualifiedName~PublicClubDetailKunyeTests"
```
Beklenen: PASS.

---

### Task 4: Görüntülenme ucu (A-77 deseninin aynısı)

**Files:**
- Create: `src/DataAccess/Repositories/IClubViewDal.cs`, `EfClubViewDal.cs`
- Modify: `src/DataAccess/DependencyResolvers/DataAccessAutofacModule.cs`
- Modify: `src/Business/Abstract/IPublicContentService.cs`, `src/Business/Concrete/PublicContentManager.cs`
- Modify: `src/WebAPI/Controllers/PublicContentController.cs`
- Test: `tests/WebAPI.IntegrationTests/PublicClubDetailKunyeTests.cs`

- [x] **Step 1: Başarısız testi yaz**

```csharp
[Fact(DisplayName = "A-82: /view ucu kulüp görüntülenmesini artırır, GET artırmaz")]
public async Task ClubViewEndpoint_IncrementsCounter()
{
    var clubId = await SeedClubWithMemberAndEventAsync("kunye-view", foundedYear: 2012);

    var before = await _client.GetFromJsonAsync<JsonElement>($"/api/public/clubs/{clubId}");
    Assert.Equal(0, before.GetProperty("viewCount").GetInt32());

    var viewResponse = await _client.PostAsync($"/api/public/clubs/{clubId}/view", null);
    Assert.Equal(HttpStatusCode.OK, viewResponse.StatusCode);

    var after = await _client.GetFromJsonAsync<JsonElement>($"/api/public/clubs/{clubId}");
    Assert.Equal(1, after.GetProperty("viewCount").GetInt32());
}
```

- [x] **Step 2: DAL'i yaz**

```csharp
namespace DataAccess.Repositories;

/// <summary>docs/MIMARI.md · A-82/A-77: sayaç tek SQL UPDATE ile artar (Club.RowVersion çakışmasın).</summary>
public interface IClubViewDal
{
    /// <summary>Eşleşen satır yoksa 0 döner (kulüp yok ya da pasif).</summary>
    Task<int> IncrementAsync(int clubId, CancellationToken cancellationToken = default);
}
```

```csharp
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

/// <summary>docs/MIMARI.md · Y-58: pasif kulübün sayacı artmaz — vitrin filtresi burada da uygulanır.</summary>
public sealed class EfClubViewDal(AppDbContext context) : IClubViewDal
{
    public async Task<int> IncrementAsync(int clubId, CancellationToken cancellationToken = default) =>
        await context.Clubs
            .Where(c => c.Id == clubId && c.IsActive)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.ViewCount, c => c.ViewCount + 1), cancellationToken)
            .ConfigureAwait(false);
}
```

Autofac kaydını `DataAccessAutofacModule`'e ekle (`EfClubStatsDal` kaydının yanına).

- [x] **Step 3: Servis ve uç**

`IPublicContentService`:

```csharp
    /// <summary>docs/MIMARI.md · A-82: sayacı tek SQL cümlesiyle artırır; okuma yolu bunu çağırmaz.</summary>
    Task<IResult> RegisterClubViewAsync(int id, CancellationToken cancellationToken = default);
```

`PublicContentManager`:

```csharp
    public async Task<IResult> RegisterClubViewAsync(int id, CancellationToken cancellationToken = default)
    {
        var affected = await clubViewDal.IncrementAsync(id, cancellationToken).ConfigureAwait(false);
        return affected == 0 ? Result.NotFound(Messages.ClubNotFound) : Result.Success();
    }
```

`PublicContentController` (A-42: anonim yüzeyin tek dosyası):

```csharp
    [HttpPost("clubs/{id:int}/view")]
    public async Task<IActionResult> RegisterClubView(int id, CancellationToken cancellationToken)
    {
        var result = await publicContentService.RegisterClubViewAsync(id, cancellationToken);
        return result.ToActionResult();
    }
```

- [x] **Step 4: Testleri çalıştır**

```bash
dotnet test tests/WebAPI.IntegrationTests --filter "FullyQualifiedName~PublicClubDetailKunyeTests"
```
Beklenen: PASS.

---

### Task 5: Paylaşım kartı ve QR

**Files:**
- Create: `arayuz/src/components/clubs/ClubShareCard.tsx`
- Modify: `arayuz/package.json` (yeni bağımlılık: `qrcode`)
- Modify: `arayuz/src/i18n/messages.ts`

- [x] **Step 1: Bağımlılığı kur**

```bash
cd arayuz && npm install qrcode && npm install --save-dev @types/qrcode
```
**Karar notu:** Bu, fazın tek yeni bağımlılığıdır (~50 KB, bağımlılıksız). İstenmezse bu görev tamamen düşülür ve kart yalnızca "Bağlantıyı kopyala" düğmesi gösterir — sayfanın geri kalanı etkilenmez.

- [x] **Step 2: i18n anahtarları (tr + en)**

```ts
    'club.shareTitle': 'Arkadaşlarını davet etmek için paylaş.',   // 'Share to invite your friends.'
    'club.shareShow': 'Görüntüle',                                  // 'Show'
    'club.shareCopy': 'Bağlantıyı kopyala',                         // 'Copy link'
    'club.shareCopied': 'Bağlantı kopyalandı.',                     // 'Link copied.'
    'club.qrAlt': 'Topluluk sayfasının QR kodu',                    // 'QR code of the club page'
    'club.status': 'TOPLULUK DURUMU',                               // 'CLUB STATUS'
    'club.loginToJoinLead': 'Topluluk etkinliklerine katılmak ve üye olmak için giriş yapmalısınız.',
    'club.contactSocial': 'İLETİŞİM & SOSYAL',                      // 'CONTACT & SOCIAL'
    'club.about': 'HAKKIMIZDA',                                     // 'ABOUT US'
    'club.founded': 'Kuruluş',                                      // 'Founded'
    'club.views': 'Görüntülenme',                                   // 'Views'
```

- [x] **Step 3: Kartı yaz**

```tsx
import { Box, Button, Card, CardContent, Dialog, DialogContent, Stack, Typography, alpha } from '@mui/material'
import QrCode2OutlinedIcon from '@mui/icons-material/QrCode2Outlined'
import { useEffect, useState } from 'react'
import QRCode from 'qrcode'
import { useLocale } from '../../i18n/LocaleContext'
import { useNotifier } from '../../notifications/NotifierProvider'

/** docs/MIMARI.md · K-51: paylaşım kartı — QR istemcide üretilir, sunucuya uç eklenmez. */
export function ClubShareCard({ clubName, clubId }: { clubName: string; clubId: number }) {
  const { t } = useLocale()
  const notify = useNotifier()
  const [open, setOpen] = useState(false)
  const [dataUrl, setDataUrl] = useState<string | null>(null)
  const url = `${window.location.origin}/kulupler/${clubId}`

  useEffect(() => {
    if (!open) {
      return
    }
    QRCode.toDataURL(url, { width: 320, margin: 1 })
      .then(setDataUrl)
      .catch(() => setDataUrl(null))
  }, [open, url])

  const copyLink = async () => {
    await navigator.clipboard.writeText(url).catch(() => undefined)
    notify({ message: t('club.shareCopied'), severity: 'success' })
  }

  return (
    <>
      <Card
        variant="outlined"
        sx={{
          borderRadius: 3,
          color: 'common.white',
          background: (theme) => `linear-gradient(135deg, ${theme.palette.secondary.main} 0%, ${alpha(theme.palette.secondary.dark ?? theme.palette.secondary.main, 0.9)} 100%)`,
        }}
      >
        <CardContent>
          <Stack spacing={1.5} sx={{ alignItems: 'center', textAlign: 'center' }}>
            <QrCode2OutlinedIcon sx={{ fontSize: 56, opacity: 0.9 }} />
            <Typography variant="body2">{t('club.shareTitle')}</Typography>
            <Stack direction="row" spacing={1}>
              <Button size="small" variant="contained" color="inherit" sx={{ color: 'secondary.main' }} onClick={() => setOpen(true)}>
                {t('club.shareShow')}
              </Button>
              <Button size="small" variant="outlined" color="inherit" onClick={copyLink}>
                {t('club.shareCopy')}
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="xs">
        <DialogContent sx={{ textAlign: 'center' }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 2 }}>
            {clubName}
          </Typography>
          {dataUrl && <Box component="img" src={dataUrl} alt={t('club.qrAlt')} sx={{ width: '100%', maxWidth: 280 }} />}
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1, wordBreak: 'break-all' }}>
            {url}
          </Typography>
        </DialogContent>
      </Dialog>
    </>
  )
}
```

**Not:** Bu `Dialog` bir **form değildir** (yalnızca QR gösterir), dolayısıyla Y-83'ü ihlal etmez — o kural form alanlarını modalda kurmayı yasaklar.

---

### Task 6: Detay sayfasının yeniden düzenlenmesi

**Files:**
- Modify: `arayuz/src/pages/public/PublicClubDetailPage.tsx`
- Modify: `arayuz/src/api/types.ts`
- Modify: `arayuz/src/components/ui/SocialLinks.tsx`

**Interfaces:**
- Consumes: `PublicClubDetailDto` künye alanları (Task 3), `ClubShareCard` (Task 5).

- [x] **Step 1: Tipleri güncelle**

`PublicClubDetailDto`'ya `foundedYear: number | null`, `memberCount: number`, `eventCount: number`, `viewCount: number`.
`SocialPlatform` birleşimine `'Facebook'` ekle.

- [x] **Step 2: Facebook ikonunu ekle**

`arayuz/src/components/ui/SocialLinks.tsx`:

```tsx
import FacebookIcon from '@mui/icons-material/Facebook'
// …
export const SOCIAL_PLATFORMS: SocialPlatform[] = ['Instagram', 'X', 'Facebook', 'LinkedIn', 'YouTube', 'Website']

export const SOCIAL_PLATFORM_ICONS: Record<SocialPlatform, typeof InstagramIcon> = {
  Instagram: InstagramIcon,
  X: XIcon,
  Facebook: FacebookIcon,
  LinkedIn: LinkedInIcon,
  YouTube: YouTubeIcon,
  Website: LanguageIcon,
}

export const SOCIAL_PLATFORM_LABELS: Record<SocialPlatform, string> = {
  Instagram: 'Instagram',
  X: 'X (Twitter)',
  Facebook: 'Facebook',
  LinkedIn: 'LinkedIn',
  YouTube: 'YouTube',
  Website: 'Web Sitesi',
}
```

- [x] **Step 3: Sayfayı iki sütuna geçir**

`PublicClubDetailPage.tsx`'te mevcut `DetailHero` + iletişim satırı + `SectionCard`'lar yerine:

```tsx
      {/* Künye: daire logo + ad + rozetler */}
      <Card variant="outlined" sx={{ borderRadius: 3, textAlign: 'center', pt: 4, pb: 3, px: 2 }}>
        <Box
          component={club.logoFileId ? 'img' : 'div'}
          src={club.logoFileId ? `/api/files/${club.logoFileId}` : undefined}
          alt=""
          sx={{
            width: 108, height: 108, borderRadius: '50%', objectFit: 'contain',
            bgcolor: 'background.paper', border: '4px solid', borderColor: 'background.paper',
            boxShadow: 3, mx: 'auto', display: 'block', p: 1,
          }}
        />
        <Typography variant="h4" component="h1" sx={{ fontWeight: 800, mt: 2 }}>
          {club.name}
        </Typography>
        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', justifyContent: 'center', mt: 2 }}>
          {/* Y-87: yıl yoksa rozet HİÇ çizilmez — CreatedAtUtc'den türetme. */}
          {club.foundedYear !== null && (
            <Chip icon={<CalendarMonthOutlinedIcon />} label={`${t('club.founded')}: ${club.foundedYear}`} />
          )}
          <Chip icon={<GroupsOutlinedIcon />} label={t('public.memberCount', { count: club.memberCount })} />
          <Chip icon={<VisibilityOutlinedIcon />} label={`${club.viewCount.toLocaleString(dateLocale)} ${t('club.views')}`} />
          <Chip icon={<EventOutlinedIcon />} label={t('public.eventCount', { count: club.eventCount })} />
          {club.clubCategoryNames.map((name) => (
            <Chip key={name} variant="outlined" color="primary" label={name} />
          ))}
        </Stack>
      </Card>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 4 }}>
          <Stack spacing={2}>
            <Card variant="outlined" sx={{ borderRadius: 3 }}>
              <CardContent>
                <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700, letterSpacing: 0.4 }}>
                  {t('club.status')}
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ my: 1.5 }}>
                  {t('club.loginToJoinLead')}
                </Typography>
                <Button fullWidth size="large" variant="contained" startIcon={<LoginOutlinedIcon />} component={RouterLink} to="/login">
                  {t('common.login')}
                </Button>
              </CardContent>
            </Card>

            <Card variant="outlined" sx={{ borderRadius: 3 }}>
              <CardContent>
                <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700, letterSpacing: 0.4 }}>
                  {t('club.contactSocial')}
                </Typography>
                <Stack spacing={1} sx={{ mt: 1.5 }}>
                  {club.contactEmail && (
                    <Stack direction="row" spacing={1} component="a" href={`mailto:${club.contactEmail}`} sx={{ alignItems: 'center', color: 'primary.main', textDecoration: 'none' }}>
                      <EmailOutlinedIcon fontSize="small" />
                      <Typography variant="body2">{club.contactEmail}</Typography>
                    </Stack>
                  )}
                  {club.contactPhone && (
                    <Stack direction="row" spacing={1} component="a" href={`tel:${club.contactPhone}`} sx={{ alignItems: 'center', color: 'text.primary', textDecoration: 'none' }}>
                      <PhoneOutlinedIcon fontSize="small" />
                      <Typography variant="body2">{club.contactPhone}</Typography>
                    </Stack>
                  )}
                  <SocialLinkIcons links={club.socialLinks} />
                </Stack>
              </CardContent>
            </Card>

            <ClubShareCard clubId={club.id} clubName={club.name} />
          </Stack>
        </Grid>

        <Grid size={{ xs: 12, md: 8 }}>
          <Stack spacing={3}>
            <Card variant="outlined" sx={{ borderRadius: 3 }}>
              <CardContent sx={{ p: { xs: 2.5, md: 3.5 } }}>
                <Typography variant="h6" sx={{ fontWeight: 800, mb: 2 }}>
                  # {t('club.about')}
                </Typography>
                <Typography variant="body1" color="text.secondary" sx={{ lineHeight: 1.9, whiteSpace: 'pre-line' }}>
                  {club.description || t('common.noDescription')}
                </Typography>
              </CardContent>
            </Card>

            {/* Mevcut etkinlik ve duyuru SectionCard'ları buraya taşınır — içerikleri değişmez. */}
          </Stack>
        </Grid>
      </Grid>
```

Eksik importları ekle (`Card`, `CardContent`, `Chip`, `Grid`, `Button`, `Stack`, ikonlar, `RouterLink`, `SocialLinkIcons`, `ClubShareCard`) ve artık kullanılmayan `DetailHero`/`DetailMedia`/`InfoTile` importlarını kaldır.

- [x] **Step 4: Görüntülenme sayacını çağır**

Sayfaya, A-77'nin istemci tarafı deseninin aynısını ekle:

```tsx
  // docs/MIMARI.md · A-82: oturum başına bir kez; hata yutulur, sayfa etkilenmez.
  useEffect(() => {
    if (!clubQuery.isSuccess) {
      return
    }
    const key = `club-view-${clubId}`
    if (sessionStorage.getItem(key)) {
      return
    }
    sessionStorage.setItem(key, '1')
    apiClient.post(`/public/clubs/${clubId}/view`).catch(() => undefined)
  }, [clubQuery.isSuccess, clubId])
```

- [x] **Step 5: Derle ve lint'le**

```bash
cd arayuz && npm run build && npm run lint
```

---

### Task 7: Tam doğrulama ve tek commit

- [x] **Step 1: Tüm testler**

```bash
dotnet test
cd arayuz && npm run build && npm run lint
```
Beklenen: iki bilinen `/api/public/stats` hatası dışında tamamı PASS.

- [x] **Step 2: Elle doğrula**

- Anonim `/kulupler/:id`: daire logo, ad, rozetler (kuruluş yılı **yalnızca doluysa**), sol sütunda giriş/iletişim/paylaş, sağ sütunda hakkımızda + etkinlikler + duyurular.
- "Görüntüle" → QR açılmalı, telefonla okutunca kulüp sayfasına gitmeli; "Bağlantıyı kopyala" panoya yazmalı.
- Sayfayı yenile → görüntülenme **artmamalı** (aynı oturum); yeni sekmede artmalı.
- Yönetimden bir kulübe Facebook bağlantısı ekle → detay sayfasında Facebook ikonu görünmeli; `http://` ile denenirse 400 gelmeli (Y-80).
- Kuruluş yılı alanını boş bırak → rozet **hiç görünmemeli** (Y-87).

- [x] **Step 3: Commit**

```bash
git add -A -- docs/MIMARI.md docs/superpowers/plans/2026-09-06-faz-47-topluluk-detay-sayfasi.md src tests arayuz/src arayuz/package.json arayuz/package-lock.json
git commit -m "$(cat <<'EOF'
Faz 47: topluluk detay sayfasi kunyesi ve iki sutunlu duzen

Docs: docs/MIMARI.md v6.14 (K-51, A-82, Y-87).
EOF
)"
```
