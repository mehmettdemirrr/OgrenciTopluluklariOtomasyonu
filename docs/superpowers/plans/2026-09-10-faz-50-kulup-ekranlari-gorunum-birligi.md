# Faz 50 — Kulüp Ekranlarında Panel/Vitrin Görünüm Birliği Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Giriş yapmış kullanıcı kulüp listesinde ve kulüp detayında vitrindekiyle **aynı** görünümü görsün; iki taraf tek bileşeni paylaşsın.

**Architecture:** Faz 42'nin A-76 kararı (`EventDetailLayout`: düzen tek bileşendir, sayfa onu `primaryAction`/`children` slotlarıyla giydirir) kulüp ekranlarına genellenir. İki yeni ortak bileşen çıkar: `ClubCard` (liste kartı) ve `ClubProfileHeader` (künye). Bileşenler **DTO tipine bağlanmaz** — ilkel alanlar ve `ReactNode` slotları alır; çünkü panel `ClubListItemDto`/`ClubDetailDto`, vitrin `PublicClubListItemDto`/`PublicClubDetailDto` taşır ve bu iki aile bilinçli olarak ayrıdır (A-42/Y-58).

**Tech Stack:** React 18 + MUI 9, TypeScript; .NET 8 (yalnızca mimari test).

**Spec:** `docs/MIMARI.md` (v6.16 → v6.17 bu fazda). İlgili kararlar: **A-76** (düzen tek bileşendir — bu fazda genelleniyor), A-75 (sekme görünürlüğü kulüpteki ilişkiden), **Y-86** (katılım uygunluk kararı sunucuda), Y-58/A-42 (vitrin DTO ailesi ayrı).

## Sorunun tespiti

Faz 46-47 kulüp listesini ve detayını yeniledi, ama yalnızca `arayuz/src/pages/public/` altında. Panel rotaları (`/clubs`, `/clubs/:id`) kendi eski uygulamalarını taşımaya devam ediyor ve yan menü giriş yapmış kullanıcıyı oraya gönderiyor:

| Ekran | Vitrin (yeni) | Panel (eski) |
|---|---|---|
| Kulüp listesi | `PublicClubsPage` → `ClubBrowseCard` | `ClubsPage.tsx:252-308` — satır içi `<Card>` |
| Kulüp detay | `PublicClubDetailPage` — künye + iki sütun | `ClubDetailPage.tsx:267-325` — `DetailHero` + `InfoTile` |

Etkinlik **detayı** bu tuzağa düşmedi çünkü A-76 orada uygulanmıştı (`EventDetailLayout` iki sayfayı da besliyor). Bu faz aynı deseni kulüp ekranlarına taşır.

## Global Constraints

- Belge önce, kod sonra (Task 1).
- Çalışma dalı `master`; faz başına **tek commit**; commit mesajında araç imzası/süreç anlatımı yok.
- **Vitrin ve panel DTO aileleri birleştirilmez** (A-42/Y-58). Ortak olan yalnızca **görünüm bileşeni**dir; her sayfa kendi DTO'sundan ilkel alanları geçirir.
- **Y-86:** panel kartındaki "Başvur" düğmesi uygunluk kararına bağlanmaz. Mevcut `disabled={!club.isActive || applyMutation.isPending}` ifadesindeki `!club.isActive` **kaldırılır** — kulüp pasifse kararı ve mesajı sunucu döndürür. `isPending` kalır (çift gönderimi engeller, uygunluk kararı değildir).
- MUI 9: `Stack`/`Typography` üzerinde `alignItems`, `gap`, `flexWrap`, `fontWeight`, `justifyContent` doğrudan prop verilmez — `sx` içine yazılır; `gap` yerine `spacing` + `useFlexGap`.
- Panelin yönetim yetenekleri (logo yükleme, düzenleme, pasife alma, sekmeler) **kaybolmaz**; slotlara taşınır.
- Test komutları: `dotnet test`; `cd arayuz && npm run build && npm run lint`.

---

### Task 1: MIMARI'ye kapsam, karar ve kuralı yaz (belge önce)

**Files:**
- Modify: `docs/MIMARI.md`

**Interfaces:**
- Produces: K-54, A-85, Y-89.

- [x] **Step 1: Sürüm ve sayaçlar**

`**Sürüm:** v6.16` → `v6.17`; kronolojik listeye kalın olarak:

```markdown
· **v6.17: 10 Eylül 2026 (K-54, A-85, Y-89, Faz 50 — kulüp ekranlarında görünüm birliği)**
```

Giriş paragrafına: `**v6.17** panel ve vitrin kulüp ekranlarını tek görünüm bileşeninde birleştirerek 1 kapsam maddesi (K-54), 1 karar (A-85) ve 1 kural (Y-89) ekledi.`

Sayaçlar: `Karar | 85 (… + 1 v6.17)`, `Yasak kural | 89 (… + 1 v6.17)`, `Uygulama fazı | 50 (… + 1 v6.17)`.
İçindekiler: `Y-01 … Y-89`, `K-01 … K-54`, `A-01 … A-85`.

- [x] **Step 2: K-54'ü K-53'ün altına ekle**

```markdown
| **K-54** | **Panel ve vitrin aynı görünümü paylaşır** | Bir varlığın liste kartı ve künyesi tek bileşende yazılır; giriş yapmış kullanıcı ile anonim ziyaretçi aynı görünümü görür. Fark yalnızca **eylemlerdedir**: panelde yönetim düğmeleri ve durum rozeti, vitrinde katılım/paylaşım eylemleri | `components/clubs/ClubCard.tsx`, `components/clubs/ClubProfileHeader.tsx`; `ClubsPage`/`PublicClubsPage` ve `ClubDetailPage`/`PublicClubDetailPage` bunları sarar (A-85, Y-89, A-76) |
```

- [x] **Step 3: A-85'i A-84'ün altına ekle**

```markdown
| **A-85** | Görünüm bileşeni **DTO'ya değil, ilkel alanlara** bağlanır | A | A-76'nın (`EventDetailLayout`) genellenmesi: ortak bileşen `to`, `name`, `logoFileId`, `categoryNames` gibi ilkel alanlar ve `ReactNode` slotları (`badges`, `stats`, `primaryAction`, `secondaryActions`) alır; `PublicClubListItemDto` veya `ClubListItemDto` **tipini görmez**. Gerekçe: A-42/Y-58 gereği vitrin ve panel DTO aileleri kasıtlı olarak ayrıdır ve birleştirilemez — bileşeni bir DTO'ya bağlamak, ya iki bileşen ya da iki ailenin birleşmesi anlamına gelirdi; ikisi de istenmiyor. Slot deseni panelin yönetim eylemlerini (logo, düzenle, pasife al) görünümü çatallamadan taşır (K-54, Y-89) |
```

- [x] **Step 4: Y-89'u Y-88'in altına ekle**

```markdown
| **Y-89** | Aynı varlık için ikinci bir kart/künye uygulaması yazmak — panelde ve vitrinde ayrı `<Card>` işaretlemesi tutmak | Liste kartı ve künye `components/` altındaki tek bileşendedir; sayfalar onu sarar (A-85). Gerekçe: Faz 46-47'de kulüp kartı ve künyesi yalnızca vitrin sayfalarında yenilendi, panel eski hâlinde kaldı ve giriş yapmış kullanıcı iki farklı ürün gördü — aynı ayrışma A-76'nın önlediği şeyin ta kendisidir. İhlali `Architecture.Tests` kaynak taramasıyla yakalar: kulüp listesi/detay sayfaları ortak bileşeni içe aktarmak **zorundadır** ve kendi `<CardMedia>`'sını çizemez (K-54, A-85, A-76) |
```

- [x] **Step 5: Sayaç bütünlüğünü doğrula**

```bash
grep -oE '^\| \*\*[AYK]-[0-9]+\*\*' docs/MIMARI.md | sort | uniq -c | awk '$1>1'
```
Beklenen: yalnızca `2 | **K-13**`.

---

### Task 2: Ortak `ClubCard` bileşeni

**Files:**
- Create: `arayuz/src/components/clubs/ClubCard.tsx`
- Delete: `arayuz/src/components/clubs/ClubBrowseCard.tsx`

**Interfaces:**
- Produces: `ClubCard(props: ClubCardProps)` — `{ to, name, description, logoFileId, categoryNames, stats?, badges?, onShare?, primaryAction?, secondaryActions? }`.

- [x] **Step 1: Bileşeni yaz**

`ClubBrowseCard.tsx`'in görünümünü koru, DTO bağımlılığını ve katılım mantığını slotlara çıkar:

```tsx
import { Box, Card, CardContent, Chip, IconButton, Stack, Tooltip, Typography, alpha } from '@mui/material'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import ShareOutlinedIcon from '@mui/icons-material/ShareOutlined'
import type { ReactNode } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { useLocale } from '../../i18n/LocaleContext'

export interface ClubCardProps {
  /** Kart başlığının gittiği adres — panelde /clubs/:id, vitrinde /kulupler/:id. */
  to: string
  name: string
  description: string | null
  logoFileId: number | null
  categoryNames: string[]
  /** "N Üye" / "N Etkinlik" çipleri. Panel DTO'su sayı taşımadığı için orada verilmez. */
  stats?: ReactNode
  /** Sayfaya özgü rozet (panelde Aktif/Pasif durumu). */
  badges?: ReactNode
  onShare?: () => void
  primaryAction?: ReactNode
  secondaryActions?: ReactNode
}

/** docs/MIMARI.md · K-54/A-85: kulüp liste kartı — panel ve vitrin bu tek bileşeni sarar. */
export function ClubCard({
  to, name, description, logoFileId, categoryNames, stats, badges, onShare, primaryAction, secondaryActions,
}: ClubCardProps) {
  const { t } = useLocale()

  return (
    <Card variant="outlined" sx={{ height: '100%', display: 'flex', flexDirection: 'column', borderRadius: 3, position: 'relative' }}>
      {onShare && (
        <Tooltip title={t('public.shareClub')}>
          <IconButton
            size="small"
            aria-label={t('public.shareClub')}
            onClick={onShare}
            sx={{ position: 'absolute', top: 8, left: 8, zIndex: 1 }}
          >
            <ShareOutlinedIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      )}

      <Stack
        direction="row"
        spacing={0.5}
        useFlexGap
        sx={{ position: 'absolute', top: 8, right: 8, zIndex: 1, flexWrap: 'wrap', justifyContent: 'flex-end', maxWidth: '65%' }}
      >
        {badges}
        {categoryNames.map((categoryName) => (
          <Chip key={categoryName} size="small" label={categoryName} sx={{ bgcolor: 'text.primary', color: 'background.paper', fontWeight: 700 }} />
        ))}
      </Stack>

      <Box
        sx={{
          height: 170,
          mt: 4,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          px: 3,
          background: (theme) => (logoFileId ? 'transparent' : `linear-gradient(135deg, ${alpha(theme.palette.secondary.main, 0.08)} 0%, ${alpha(theme.palette.primary.main, 0.12)} 100%)`),
        }}
      >
        {logoFileId ? (
          <Box component="img" src={`/api/files/${logoFileId}`} alt="" sx={{ maxHeight: '100%', maxWidth: '100%', objectFit: 'contain' }} />
        ) : (
          <GroupsOutlinedIcon sx={{ fontSize: 56, color: 'primary.dark' }} />
        )}
      </Box>

      <CardContent sx={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        <Typography
          component={RouterLink}
          to={to}
          variant="subtitle1"
          sx={{ fontWeight: 800, textTransform: 'uppercase', mb: 1, display: 'block', textDecoration: 'none', color: 'inherit' }}
        >
          {name}
        </Typography>

        {stats && (
          <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', mb: 1.5 }}>
            {stats}
          </Stack>
        )}

        <Typography
          variant="body2"
          color="text.secondary"
          sx={{ display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical', overflow: 'hidden', mb: 2, minHeight: 40 }}
        >
          {description || t('common.noDescription')}
        </Typography>

        <Stack spacing={1} sx={{ mt: 'auto' }}>
          {primaryAction}
          {secondaryActions}
        </Stack>
      </CardContent>
    </Card>
  )
}
```

Kart **sarmalayıcı bağlantı değildir**; yalnızca başlık `to` adresine bağlanır. Böylece slotlardaki eylem düğmeleri iç içe `<a>` üretmez (geçersiz HTML).

- [x] **Step 2: Eski bileşeni sil**

```bash
rm arayuz/src/components/clubs/ClubBrowseCard.tsx
```

- [x] **Step 3: Derle (kırmızı olmalı)**

```bash
cd arayuz && npm run build
```
Beklenen: `PublicClubsPage.tsx` `ClubBrowseCard`'ı bulamadığı için FAIL — Task 3 kapatır.

---

### Task 3: İki kulüp listesi sayfasını da ortak karta bağla

**Files:**
- Modify: `arayuz/src/pages/public/PublicClubsPage.tsx`
- Modify: `arayuz/src/pages/ClubsPage.tsx:252-308`

**Interfaces:**
- Consumes: `ClubCard` (Task 2).

- [x] **Step 1: Vitrin sayfasını çevir**

`PublicClubsPage.tsx` içindeki `ClubBrowseCard` importunu `ClubCard` ile değiştir ve grid içindeki kullanımı şununla değiştir (gerekli ikon/`Chip`/`Button` importlarını ekle):

```tsx
              <ClubCard
                to={`/kulupler/${club.id}`}
                name={club.name}
                description={club.description}
                logoFileId={club.logoFileId}
                categoryNames={club.clubCategoryNames}
                onShare={() => handleShare(club)}
                stats={
                  <>
                    <Chip size="small" icon={<GroupsOutlinedIcon />} label={t('public.memberCount', { count: club.memberCount })} />
                    <Chip size="small" icon={<EventAvailableOutlinedIcon />} label={t('public.eventCount', { count: club.eventCount })} />
                  </>
                }
                primaryAction={
                  isAuthenticated ? (
                    <Button variant="contained" color="success" disabled={joinMutation.isPending} onClick={() => joinMutation.mutate(club.id)}>
                      {t('public.join')}
                    </Button>
                  ) : (
                    <Button variant="contained" color="success" startIcon={<LoginOutlinedIcon />} component={RouterLink} to="/login">
                      {t('public.loginAndJoin')}
                    </Button>
                  )
                }
                secondaryActions={
                  <Button variant="outlined" component={RouterLink} to={`/kulupler/${club.id}`}>
                    {t('public.inspect')}
                  </Button>
                }
              />
```

- [x] **Step 2: Panel sayfasını çevir**

`ClubsPage.tsx` içindeki `<Card variant="outlined" …>` … `</Card>` bloğunun **tamamını** şununla değiştir:

```tsx
              <ClubCard
                to={`/clubs/${club.id}`}
                name={club.name}
                description={club.description}
                logoFileId={club.logoFileId}
                categoryNames={club.clubCategoryNames}
                badges={
                  <Chip
                    size="small"
                    label={club.isActive ? 'Aktif' : 'Pasif'}
                    color={club.isActive ? 'success' : 'default'}
                    variant={club.isActive ? 'filled' : 'outlined'}
                  />
                }
                primaryAction={
                  // Y-86: uygunluk kararı sunucudadır — kulüp pasifse mesajı uç döndürür.
                  <Button variant="contained" disabled={applyMutation.isPending} onClick={() => applyMutation.mutate(club.id)}>
                    Başvur
                  </Button>
                }
                secondaryActions={
                  <Stack direction="row" spacing={1}>
                    <Button fullWidth variant="outlined" component={RouterLink} to={`/clubs/${club.id}`}>
                      Detay
                    </Button>
                    {canUploadLogo && (
                      <Button fullWidth variant="outlined" disabled={logoMutation.isPending} onClick={() => handleLogoButtonClick(club.id)}>
                        Logo Yükle
                      </Button>
                    )}
                  </Stack>
                }
              />
```

`ClubCard` importunu ekle; artık kullanılmayan `CardMedia`, `CardActions`, `CardContent` importlarını kaldır (`Card` başka yerde kullanılmıyorsa onu da).

- [x] **Step 3: Derle ve lint'le**

```bash
cd arayuz && npm run build && npm run lint
```
Beklenen: temiz. `TS6133` (kullanılmayan import) uyarısı gelirse ilgili importu sil.

---

### Task 4: Ortak `ClubProfileHeader` bileşeni

**Files:**
- Create: `arayuz/src/components/clubs/ClubProfileHeader.tsx`

**Interfaces:**
- Produces: `ClubProfileHeader(props: { name, logoFileId, badges?, actions? })`.

- [x] **Step 1: Bileşeni yaz**

`PublicClubDetailPage.tsx`'teki künye kartını (daire logo + ad + rozet şeridi) bileşene çıkar:

```tsx
import { Box, Card, Stack, Typography } from '@mui/material'
import type { ReactNode } from 'react'

export interface ClubProfileHeaderProps {
  name: string
  logoFileId: number | null
  /** Kuruluş yılı, üye/görüntülenme/etkinlik sayısı, kategori rozetleri — sayfa kendi setini verir. */
  badges?: ReactNode
  /** Panelde "Düzenle"/"Pasife Al"; vitrinde yok. */
  actions?: ReactNode
}

/** docs/MIMARI.md · K-54/A-85: kulüp künyesi — panel ve vitrin bu tek bileşeni sarar. */
export function ClubProfileHeader({ name, logoFileId, badges, actions }: ClubProfileHeaderProps) {
  return (
    <Card variant="outlined" sx={{ borderRadius: 3, textAlign: 'center', pt: 4, pb: 3, px: 2 }}>
      <Box
        component={logoFileId ? 'img' : 'div'}
        src={logoFileId ? `/api/files/${logoFileId}` : undefined}
        alt=""
        sx={{
          width: 108, height: 108, borderRadius: '50%', objectFit: 'contain',
          bgcolor: 'background.paper', border: '4px solid', borderColor: 'background.paper',
          boxShadow: 3, mx: 'auto', display: 'block', p: 1,
        }}
      />
      <Typography variant="h4" component="h1" sx={{ fontWeight: 800, mt: 2 }}>
        {name}
      </Typography>
      {badges && (
        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', justifyContent: 'center', mt: 2 }}>
          {badges}
        </Stack>
      )}
      {actions && (
        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', justifyContent: 'center', mt: 2.5 }}>
          {actions}
        </Stack>
      )}
    </Card>
  )
}
```

---

### Task 5: İki kulüp detay sayfasını da künyeye bağla

**Files:**
- Modify: `arayuz/src/pages/public/PublicClubDetailPage.tsx:87-114`
- Modify: `arayuz/src/pages/ClubDetailPage.tsx:267-325`

**Interfaces:**
- Consumes: `ClubProfileHeader` (Task 4).

- [x] **Step 1: Vitrin detayını çevir**

`PublicClubDetailPage.tsx`'teki künye `<Card …>` bloğunu şununla değiştir (rozet içerikleri **aynen** korunur):

```tsx
      <ClubProfileHeader
        name={club.name}
        logoFileId={club.logoFileId}
        badges={
          <>
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
          </>
        }
      />
```

`ClubProfileHeader` importunu ekle; `Card` başka yerde kullanılmıyorsa importunu kaldırma (sağ/sol sütun kartları hâlâ kullanıyor).

- [x] **Step 2: Panel detayını çevir**

`ClubDetailPage.tsx`'teki `<DetailHero …>` … `</DetailHero>` bloğunun tamamını şununla değiştir:

```tsx
      <ClubProfileHeader
        name={club.name}
        logoFileId={club.logoFileId}
        badges={
          <>
            <Chip
              size="small"
              label={club.isActive ? 'Aktif' : 'Pasif'}
              color={club.isActive ? 'success' : 'default'}
            />
            {club.foundedYear !== null && (
              <Chip icon={<CalendarMonthOutlinedIcon />} label={`Kuruluş: ${club.foundedYear}`} />
            )}
            <Chip icon={<VisibilityOutlinedIcon />} label={`${club.viewCount.toLocaleString('tr-TR')} Görüntülenme`} />
            <Chip icon={<CalendarMonthOutlinedIcon />} label={`Kayıt: ${new Date(club.createdAtUtc).toLocaleDateString('tr-TR')}`} />
            {club.clubCategoryNames.map((name) => (
              <Chip key={name} variant="outlined" color="primary" label={name} />
            ))}
          </>
        }
        actions={
          canManage ? (
            <>
              <Button variant="outlined" onClick={openEditDialog}>
                Düzenle
              </Button>
              <Button
                color={club.isActive ? 'error' : 'success'}
                disabled={statusMutation.isPending}
                onClick={() => statusMutation.mutate(!club.isActive)}
              >
                {club.isActive ? 'Pasife Al' : 'Aktifleştir'}
              </Button>
            </>
          ) : undefined
        }
      />

      {club.description && (
        <Typography variant="body1" color="text.secondary" sx={{ lineHeight: 1.9, whiteSpace: 'pre-line' }}>
          {club.description}
        </Typography>
      )}
```

`ClubProfileHeader` ve `CalendarMonthOutlinedIcon`/`VisibilityOutlinedIcon` importlarını ekle; artık kullanılmayan `DetailHero`, `DetailMedia`, `InfoTile`, `GroupsOutlinedIcon`, `VerifiedOutlinedIcon`, `CategoryOutlinedIcon` importlarını kaldır. **Sekmeler (`Tabs`) ve tüm sekme içerikleri olduğu gibi kalır** — bu faz yalnızca künyeyi değiştirir.

- [x] **Step 3: Derle ve lint'le**

```bash
cd arayuz && npm run build && npm run lint
```

---

### Task 6: Y-89 mimari testi

**Files:**
- Create: `tests/Architecture.Tests/ClubViewArchitectureTests.cs`

- [x] **Step 1: Testi yaz**

```csharp
using Xunit;

namespace Architecture.Tests;

/// <summary>docs/MIMARI.md · Y-89: kulüp kartı ve künyesi tek bileşendedir; sayfalar kendi kartını çizmez.</summary>
public class ClubViewArchitectureTests
{
    [Theory(DisplayName = "Y-89: kulüp listesi sayfaları ortak ClubCard'ı sarar")]
    [InlineData("ClubsPage.tsx")]
    [InlineData("public/PublicClubsPage.tsx")]
    public void ClubListPages_UseSharedCard(string relativePath)
    {
        var source = ReadPage(relativePath);

        Assert.True(
            source.Contains("ClubCard", StringComparison.Ordinal),
            $"Y-89 ihlali — {relativePath} ortak kartı kullanmıyor; components/clubs/ClubCard.tsx sarılmalı (A-85).");
        Assert.False(
            source.Contains("<CardMedia", StringComparison.Ordinal),
            $"Y-89 ihlali — {relativePath} kendi kart görselini çiziyor; görsel ortak bileşene aittir (A-85).");
    }

    [Theory(DisplayName = "Y-89: kulüp detay sayfaları ortak ClubProfileHeader'ı sarar")]
    [InlineData("ClubDetailPage.tsx")]
    [InlineData("public/PublicClubDetailPage.tsx")]
    public void ClubDetailPages_UseSharedHeader(string relativePath)
    {
        var source = ReadPage(relativePath);

        Assert.True(
            source.Contains("ClubProfileHeader", StringComparison.Ordinal),
            $"Y-89 ihlali — {relativePath} ortak künyeyi kullanmıyor (A-85).");
        Assert.False(
            source.Contains("DetailHero", StringComparison.Ordinal),
            $"Y-89 ihlali — {relativePath} künyeyi DetailHero ile ayrı çiziyor; ortak bileşene taşı (A-85).");
    }

    private static string ReadPage(string relativePath)
    {
        var path = Path.Combine(SolutionPaths.FindRepositoryRoot(), "arayuz", "src", "pages", relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), $"Sayfa bulunamadı: {path}");
        return File.ReadAllText(path);
    }
}
```

- [x] **Step 2: Çalıştır**

```bash
dotnet test tests/Architecture.Tests --filter "FullyQualifiedName~ClubViewArchitectureTests"
```
Beklenen: 4 test PASS. Kırmızıysa ilgili sayfa hâlâ kendi kartını/künyesini çiziyordur — izin listesi genişletme, sayfayı düzelt.

---

### Task 7: Tam doğrulama ve tek commit

- [x] **Step 1: Tüm testler**

```bash
dotnet test
cd arayuz && npm run build && npm run lint
```
Beklenen: iki bilinen `/api/public/stats` hatası dışında tamamı PASS.

- [x] **Step 2: Elle doğrula**

- Giriş yapmış kullanıcıyla `/clubs`: kartlar vitrindekiyle aynı görünmeli; sağ üstte kategori rozetleri, durum rozeti; altta "Başvur" + "Detay" (+ yetkiliyse "Logo Yükle").
- **Y-86 kontrolü:** pasif bir kulüpte "Başvur" düğmesi **tıklanabilir** olmalı ve sunucunun hata mesajı görünmeli (düğme gizlenmemeli/pasifleşmemeli).
- `/clubs/:id`: daire logolu künye, rozetler ve "Düzenle"/"Pasife Al" düğmeleri görünmeli; **sekmeler (Üyeler/Roller/Etkinlikler/Duyurular) ve içerikleri çalışmaya devam etmeli**.
- Anonim `/kulupler` ve `/kulupler/:id`: bu fazdan önceki görünümle **birebir aynı** kalmalı (paylaş düğmesi, katıl/incele, QR kartı).
- Yetkisiz bir kullanıcıyla `/clubs/:id`: yönetim düğmeleri görünmemeli (A-75 kapsam kararı değişmedi).

- [x] **Step 3: Commit**

```bash
git add -A -- docs/MIMARI.md docs/superpowers/plans/2026-09-10-faz-50-kulup-ekranlari-gorunum-birligi.md tests arayuz/src
git commit -m "$(cat <<'EOF'
Faz 50: kulup listesi ve kunyesi panel ile vitrinde tek bilesene tasindi

Docs: docs/MIMARI.md v6.17 (K-54, A-85, Y-89).
EOF
)"
```
