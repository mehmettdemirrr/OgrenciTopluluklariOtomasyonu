# Faz 51 — Etkinlik ve Duyuru Listelerinde Panel/Vitrin Görünüm Birliği Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Giriş yapmış kullanıcı yaklaşan etkinlik listesinde ve duyuru listesinde vitrindekiyle aynı kart görünümünü görsün; iki taraf tek bileşeni paylaşsın.

**Architecture:** Faz 50'nin A-85 deseni (ilkel alanlar + `ReactNode` slotları) etkinlik ve duyuru kartlarına uygulanır: `EventCard` ve `AnnouncementGridCard`. Duyuru kartında `to` **isteğe bağlıdır** — vitrin kartı detay sayfasına bağlanır, panel kartı bağlanmaz (panelde duyuru detay rotası yoktur ve üyelere özel duyuru vitrin detayında 404 döner, oraya bağlanamaz). Panelin etkinlik **yönetim sekmesi** bir `DataGrid` tablosudur; kart değildir ve bu fazın kapsamı dışındadır.

**Tech Stack:** React 18 + MUI 9, TypeScript; .NET 8 (yalnızca mimari test).

**Spec:** `docs/MIMARI.md` (v6.17 → v6.18 bu fazda). İlgili kararlar: **K-54/A-85/Y-89** (Faz 50'de kondu, bu fazda kapsamı genişletiliyor), A-42/Y-58 (DTO aileleri ayrı), Y-72 (kitle rozeti).

## Kapsam sınırı

| Ekran | Bu fazda | Gerekçe |
|---|---|---|
| `PublicEventsPage` + `EventsPage` → `UpcomingTab` | **Birleşir** | İkisi de kart ızgarası; aynı varlık, iki işaretleme |
| `EventsPage` → `EventsTab` | **Dokunulmaz** | `DataGrid` yönetim tablosu (durum/kitle kolonları, "Onaya Gönder") — kart değil |
| `PublicAnnouncementsPage` + `AnnouncementsPage` | **Birleşir** | İkisi de liste; biri grid kart, öbürü eski satır kartı |
| Panel duyuru **detay** sayfası | **Açılmaz** | Panelde `/announcements/:id` rotası yok; üyelere özel duyuru vitrin detayında 404 döner (Y-57). Ayrı bir faz konusu |
| `AnnouncementCard` (kulüp sekmeleri) | **Dokunulmaz** | Kulüp detay sekmelerinin satır görünümü; liste ızgarası değil |

## Global Constraints

- Belge önce, kod sonra (Task 1).
- Çalışma dalı `master`; faz başına **tek commit**; commit mesajında araç imzası/süreç anlatımı yok.
- **A-85:** ortak bileşen DTO tipini görmez; ilkel alan ve `ReactNode` slotu alır.
- **Y-72:** etkinlik kartındaki kitle rozeti (`ClubMembers`) korunur — kullanıcı "Katıl"a basmadan önce üyelik şartını görmelidir (K-38).
- Panelin katılım düğmeleri ("Katıl"/"Ayrıl") uygunluk kararına bağlanmaz; yalnızca `isPending` ile çift gönderim engellenir (Y-86 deseni).
- MUI 9: `Stack`/`Typography` üzerinde `alignItems`, `gap`, `flexWrap`, `fontWeight` doğrudan prop verilmez — `sx` içine; `gap` yerine `spacing` + `useFlexGap`.
- Test komutları: `dotnet test`; `cd arayuz && npm run build && npm run lint`.

---

### Task 1: MIMARI — K-54 ve Y-89 kapsamını genişlet (belge önce)

**Files:**
- Modify: `docs/MIMARI.md`

**Interfaces:**
- Produces: K-54 ve Y-89 tadilleri. **Yeni numaralı madde eklenmez.**

- [x] **Step 1: Sürüm ve sayaçlar**

`**Sürüm:** v6.17` → `v6.18`; kronolojik listeye kalın olarak:

```markdown
· **v6.18: 10 Eylül 2026 (Faz 51 — etkinlik ve duyuru listelerinde görünüm birliği; K-54 ve Y-89 tadil edildi)**
```

Giriş paragrafına: `**v6.18** etkinlik ve duyuru listelerini de tek görünüm bileşenine taşıyarak **K-54 ile Y-89'u tadil etti**.`

Sayaçlarda **yalnızca** `Uygulama fazı | 51 (… + 1 v6.18)` güncellenir. `Karar` 85'te, `Yasak kural` 89'da kalır; İçindekiler satırları değişmez.

- [x] **Step 2: K-54'ü tadil et**

K-54 satırının "Gerçekleşme" sütununu şununla değiştir:

```markdown
`components/clubs/ClubCard.tsx`, `components/clubs/ClubProfileHeader.tsx`, **`components/events/EventCard.tsx`, `components/announcements/AnnouncementGridCard.tsx`**; `ClubsPage`/`PublicClubsPage`, `ClubDetailPage`/`PublicClubDetailPage`, `EventsPage`(Yaklaşan)/`PublicEventsPage` ve `AnnouncementsPage`/`PublicAnnouncementsPage` bunları sarar. Panelin etkinlik yönetim sekmesi `DataGrid` tablosu olarak kalır — o bir kart değildir (A-85, Y-89, A-76)
```

- [x] **Step 3: Y-89'u tadil et**

Y-89 satırının başlığına `*(v6.18'de kapsamı genişletildi)*` ekle ve "Gerçekleşme" sütununun son cümlesini şununla değiştir:

```markdown
İhlali `Architecture.Tests` kaynak taramasıyla yakalar: kulüp listesi/detay, **yaklaşan etkinlik listesi ve duyuru listesi** sayfaları ortak bileşeni içe aktarmak **zorundadır** ve kendi `<CardMedia>`'sını çizemez. **Kapsam dışı:** `DataGrid` tabanlı yönetim tabloları (etkinlik yönetim sekmesi) — onlar liste kartı değildir (K-54, A-85, A-76)
```

- [x] **Step 4: Sayaç bütünlüğünü doğrula**

```bash
grep -oE '^\| \*\*[AYK]-[0-9]+\*\*' docs/MIMARI.md | sort | uniq -c | awk '$1>1'
```
Beklenen: yalnızca `2 | **K-13**`.

---

### Task 2: Ortak `EventCard` bileşeni

**Files:**
- Create: `arayuz/src/components/events/EventCard.tsx`

**Interfaces:**
- Produces: `EventCard(props: { to, title, clubName, startDateUtc, location, posterFileId, badges?, actions? })`.

- [x] **Step 1: Bileşeni yaz**

Vitrin kartının görünümünü temel al, panelin rozet ve eylemlerini slota çıkar:

```tsx
import { Box, Card, CardContent, CardMedia, Stack, Typography } from '@mui/material'
import PlaceOutlinedIcon from '@mui/icons-material/PlaceOutlined'
import type { ReactNode } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { DateBadge } from '../ui/DateBadge'
import { useLocale } from '../../i18n/LocaleContext'

export interface EventCardProps {
  /** Başlığın gittiği adres — panelde /events/:id, vitrinde /etkinlikler/:id. */
  to: string
  title: string
  clubName: string
  startDateUtc: string
  location: string | null
  posterFileId: number | null
  /** Kontenjan, kitle (ClubMembers) gibi rozetler — sayfa kendi setini verir (Y-72). */
  badges?: ReactNode
  /** Panelde Detay/Katıl/Ayrıl; vitrinde yok (kartın kendisi bağlantı). */
  actions?: ReactNode
}

/** docs/MIMARI.md · K-54/A-85: etkinlik liste kartı — panel ve vitrin bu tek bileşeni sarar. */
export function EventCard({ to, title, clubName, startDateUtc, location, posterFileId, badges, actions }: EventCardProps) {
  const { dateLocale } = useLocale()

  return (
    <Card variant="outlined" sx={{ height: '100%', display: 'flex', flexDirection: 'column', borderRadius: 3 }}>
      {posterFileId && (
        <CardMedia
          component={RouterLink}
          to={to}
          image={`/api/files/${posterFileId}`}
          sx={{ height: 160, display: 'block' }}
        />
      )}
      <CardContent sx={{ flex: 1 }}>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'flex-start', mb: 1.5 }}>
          <DateBadge iso={startDateUtc} />
          <Box sx={{ minWidth: 0, flex: 1 }}>
            <Typography
              component={RouterLink}
              to={to}
              variant="h6"
              sx={{ fontWeight: 800, fontSize: 18, display: 'block', textDecoration: 'none', color: 'inherit' }}
              noWrap
            >
              {title}
            </Typography>
            <Typography variant="body2" color="text.secondary" noWrap>
              {clubName}
            </Typography>
          </Box>
        </Stack>

        {badges && (
          <Stack direction="row" spacing={0.5} useFlexGap sx={{ flexWrap: 'wrap', mb: 1 }}>
            {badges}
          </Stack>
        )}

        <Typography variant="body2" sx={{ mb: 0.5 }}>
          {new Date(startDateUtc).toLocaleString(dateLocale)}
        </Typography>
        {location && (
          <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center', color: 'text.secondary' }}>
            <PlaceOutlinedIcon fontSize="inherit" />
            <Typography variant="caption">{location}</Typography>
          </Stack>
        )}

        {actions && (
          <Stack direction="row" spacing={1} sx={{ mt: 2 }}>
            {actions}
          </Stack>
        )}
      </CardContent>
    </Card>
  )
}
```

Kart **sarmalayıcı bağlantı değildir**; afiş ve başlık ayrı ayrı bağlantıdır. Böylece panelin eylem düğmeleri iç içe `<a>` üretmez.

---

### Task 3: İki etkinlik listesini de ortak karta bağla

**Files:**
- Modify: `arayuz/src/pages/public/PublicEventsPage.tsx:43-81`
- Modify: `arayuz/src/pages/EventsPage.tsx:107-160`

**Interfaces:**
- Consumes: `EventCard` (Task 2).

- [x] **Step 1: Vitrin listesini çevir**

`PublicEventsPage.tsx`'teki `<Card …>` … `</Card>` bloğunu şununla değiştir:

```tsx
              <EventCard
                to={`/etkinlikler/${event.id}`}
                title={event.title}
                clubName={event.clubName}
                startDateUtc={event.startDateUtc}
                location={event.location}
                posterFileId={event.posterFileId}
                badges={event.capacity ? <Chip size="small" variant="outlined" label={t('common.capacity', { count: event.capacity })} /> : undefined}
              />
```

`EventCard` importunu ekle; kullanılmayan `Card`, `CardContent`, `CardMedia`, `Box`, `DateBadge`, `PlaceOutlinedIcon` importlarını kaldır.

- [x] **Step 2: Panel "Yaklaşan" sekmesini çevir**

`EventsPage.tsx` içindeki `UpcomingTab`'ın `<Card variant="outlined" …>` … `</Card>` bloğunu şununla değiştir:

```tsx
              <EventCard
                to={`/events/${event.id}`}
                title={event.title}
                clubName={event.clubName}
                startDateUtc={event.startDateUtc}
                location={event.location}
                posterFileId={event.posterFileId}
                badges={
                  <>
                    <Chip size="small" variant="outlined" label={event.capacity ? `Kontenjan: ${event.capacity}` : 'Sınırsız'} />
                    {/* K-38/Y-72: "Katıl"a basmadan önce üyelik şartını görsün. */}
                    {event.audience === 'ClubMembers' && <EventAudienceChip audience={event.audience} />}
                  </>
                }
                actions={
                  <>
                    <Button size="small" component={RouterLink} to={`/events/${event.id}`}>
                      Detay
                    </Button>
                    {isRegistered ? (
                      <Button size="small" color="error" disabled={cancelMutation.isPending} onClick={() => cancelMutation.mutate(event.id)}>
                        Ayrıl
                      </Button>
                    ) : (
                      <Button size="small" variant="outlined" disabled={registerMutation.isPending} onClick={() => registerMutation.mutate(event.id)}>
                        Katıl
                      </Button>
                    )}
                  </>
                }
              />
```

`EventCard` importunu ekle; `UpcomingTab` içinde kullanılmayan hâle gelen `CardActions`, `CardContent`, `DateBadge`, `PlaceOutlinedIcon`, `Box` importlarını kaldır — **`EventsTab` hâlâ kullanıyorsa bırak** (dosyada iki bileşen var).

- [x] **Step 3: Derle ve lint'le**

```bash
cd arayuz && npm run build && npm run lint
```

---

### Task 4: Ortak `AnnouncementGridCard` bileşeni

**Files:**
- Create: `arayuz/src/components/announcements/AnnouncementGridCard.tsx`
- Delete: `arayuz/src/components/announcements/PublicAnnouncementGridCard.tsx`

**Interfaces:**
- Produces: `AnnouncementGridCard(props: { to?, title, content, clubName, publishedAtUtc, imageFileId, badges?, actions? })`.

- [x] **Step 1: Bileşeni yaz**

`PublicAnnouncementGridCard.tsx`'in görünümünü koru; DTO yerine ilkel alanlar, `to` isteğe bağlı, rozet ve eylem slotları ekle:

```tsx
import ArrowForwardRoundedIcon from '@mui/icons-material/ArrowForwardRounded'
import CalendarMonthOutlinedIcon from '@mui/icons-material/CalendarMonthOutlined'
import CampaignOutlinedIcon from '@mui/icons-material/CampaignOutlined'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import { Box, CardMedia, Stack, Typography, alpha, useMediaQuery } from '@mui/material'
import type { ReactNode } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { useLocale } from '../../i18n/LocaleContext'
import { announcementExcerpt, formatAnnouncementDateTime } from '../../utils/announcementFormat'

export interface AnnouncementGridCardProps {
  /** Verilirse kart tıklanabilir olur ve "Devamını oku" satırı çizilir. Panelde duyuru detay rotası yok — verilmez. */
  to?: string
  title: string
  content: string
  clubName: string | null
  publishedAtUtc: string
  imageFileId: number | null
  /** Panelde görünürlük çipi (Üyelere özel / Herkese açık). */
  badges?: ReactNode
  /** Panelde Düzenle/Kaldır. */
  actions?: ReactNode
}

/** docs/MIMARI.md · K-54/A-85: duyuru liste kartı — panel ve vitrin bu tek bileşeni sarar. */
export function AnnouncementGridCard({
  to, title, content, clubName, publishedAtUtc, imageFileId, badges, actions,
}: AnnouncementGridCardProps) {
  const { t, dateLocale } = useLocale()
  const reduceMotion = useMediaQuery('(prefers-reduced-motion: reduce)')

  return (
    <Box
      {...(to ? { component: RouterLink, to } : { component: 'div' })}
      sx={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        textDecoration: 'none',
        color: 'inherit',
        borderRadius: 2.5,
        overflow: 'hidden',
        bgcolor: 'background.paper',
        border: '1px solid',
        borderColor: (theme) => alpha(theme.palette.secondary.main, 0.08),
        boxShadow: (theme) => `0 4px 20px ${alpha(theme.palette.secondary.main, 0.04)}`,
        transition: 'transform 180ms ease, box-shadow 180ms ease, border-color 180ms ease',
        ...(to
          ? {
              '&:hover': {
                transform: reduceMotion ? 'none' : 'translateY(-8px)',
                borderColor: 'primary.main',
                boxShadow: (theme) => `0 12px 30px ${alpha(theme.palette.secondary.main, 0.08)}`,
              },
            }
          : {}),
      }}
    >
      {imageFileId ? (
        <CardMedia
          component="img"
          image={`/api/files/${imageFileId}`}
          alt=""
          sx={{ height: 200, objectFit: 'cover', borderBottom: '1px solid', borderColor: 'divider' }}
        />
      ) : (
        <Box
          sx={{
            height: 200,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'primary.main',
            background: (theme) =>
              `linear-gradient(135deg, ${alpha(theme.palette.primary.main, 0.1)} 0%, ${alpha(theme.palette.secondary.main, 0.05)} 100%)`,
          }}
        >
          <CampaignOutlinedIcon sx={{ fontSize: 48 }} />
        </Box>
      )}

      <Box sx={{ p: 3, flex: 1, display: 'flex', flexDirection: 'column' }}>
        <Stack direction="row" spacing={1.25} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center', mb: 1.5 }}>
          <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center', color: 'text.secondary' }}>
            <CalendarMonthOutlinedIcon sx={{ fontSize: 16, color: 'primary.main' }} />
            <Typography variant="caption" sx={{ fontWeight: 700, letterSpacing: '0.04em', textTransform: 'uppercase' }}>
              {formatAnnouncementDateTime(publishedAtUtc, dateLocale)}
            </Typography>
          </Stack>
          {clubName && (
            <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center', color: 'text.secondary' }}>
              <GroupsOutlinedIcon sx={{ fontSize: 16, color: 'primary.main' }} />
              <Typography variant="caption" sx={{ fontWeight: 700, letterSpacing: '0.04em', textTransform: 'uppercase' }}>
                {clubName}
              </Typography>
            </Stack>
          )}
          {badges}
        </Stack>

        <Typography variant="h6" component="h2" sx={{ fontWeight: 800, color: 'text.primary', mb: 1.5, lineHeight: 1.4 }}>
          {title}
        </Typography>
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{ mb: 2.5, lineHeight: 1.6, display: '-webkit-box', WebkitLineClamp: 3, WebkitBoxOrient: 'vertical', overflow: 'hidden' }}
        >
          {announcementExcerpt(content)}
        </Typography>

        {to && (
          <Stack direction="row" spacing={1} sx={{ mt: 'auto', alignItems: 'center', color: 'primary.main' }}>
            <Typography variant="body2" sx={{ fontWeight: 700 }}>
              {t('public.readMore')}
            </Typography>
            <ArrowForwardRoundedIcon sx={{ fontSize: 18 }} />
          </Stack>
        )}

        {actions && (
          <Stack direction="row" spacing={1} sx={{ mt: 'auto', pt: 2 }}>
            {actions}
          </Stack>
        )}
      </Box>
    </Box>
  )
}
```

- [x] **Step 2: Eski bileşeni sil**

```bash
rm arayuz/src/components/announcements/PublicAnnouncementGridCard.tsx
```

---

### Task 5: İki duyuru listesini de ortak karta bağla

**Files:**
- Modify: `arayuz/src/pages/public/PublicAnnouncementsPage.tsx`
- Modify: `arayuz/src/pages/public/PublicClubDetailPage.tsx`
- Modify: `arayuz/src/pages/AnnouncementsPage.tsx`

**Interfaces:**
- Consumes: `AnnouncementGridCard` (Task 4).

- [x] **Step 1: Vitrin kullanımlarını çevir**

`PublicAnnouncementsPage.tsx` ve `PublicClubDetailPage.tsx` içindeki `PublicAnnouncementGridCard` kullanımlarını şu biçimle değiştir (import adını da güncelle):

```tsx
                <AnnouncementGridCard
                  to={`/duyurular/${announcement.id}`}
                  title={announcement.title}
                  content={announcement.content}
                  clubName={announcement.clubName}
                  publishedAtUtc={announcement.publishedAtUtc}
                  imageFileId={announcement.imageFileId}
                />
```

- [x] **Step 2: Panel duyuru listesini ızgaraya çevir**

`AnnouncementsPage.tsx` içindeki `<Stack spacing={1.5}>` + `AnnouncementCard` bloğunu şununla değiştir:

```tsx
        <Grid container spacing={3.5}>
          {items.map((announcement) => (
            <Grid key={announcement.id} size={{ xs: 12, sm: 6, md: 4 }}>
              <AnnouncementGridCard
                title={announcement.title}
                content={announcement.content}
                clubName={announcement.clubName ?? 'Sistem Duyurusu'}
                publishedAtUtc={announcement.publishedAtUtc}
                imageFileId={announcement.imageFileId}
                badges={<AnnouncementVisibilityChip visibility={announcement.visibility} />}
                actions={
                  canCreateGlobal && announcement.clubId === null ? (
                    <Button size="small" component={RouterLink} to={`/announcements/${announcement.id}/edit?returnTo=/announcements`}>
                      Düzenle
                    </Button>
                  ) : undefined
                }
              />
            </Grid>
          ))}
        </Grid>
```

`Grid` ve `AnnouncementGridCard` importlarını ekle; kullanılmayan `AnnouncementCard` importunu kaldır. Arama, sayfalama ve `PageHeader` **aynen kalır**.

- [x] **Step 3: Derle ve lint'le**

```bash
cd arayuz && npm run build && npm run lint
```

---

### Task 6: Y-89 mimari testini genişlet

**Files:**
- Create: `tests/Architecture.Tests/EventAnnouncementViewArchitectureTests.cs`

- [x] **Step 1: Testi yaz**

```csharp
using Xunit;

namespace Architecture.Tests;

/// <summary>docs/MIMARI.md · Y-89 (v6.18 tadili): etkinlik ve duyuru liste kartları tek bileşendedir.</summary>
public class EventAnnouncementViewArchitectureTests
{
    [Theory(DisplayName = "Y-89: etkinlik listesi sayfaları ortak EventCard'ı sarar")]
    [InlineData("EventsPage.tsx")]
    [InlineData("public/PublicEventsPage.tsx")]
    public void EventListPages_UseSharedCard(string relativePath)
    {
        var source = ReadPage(relativePath);

        Assert.True(
            source.Contains("EventCard", StringComparison.Ordinal),
            $"Y-89 ihlali — {relativePath} ortak etkinlik kartını kullanmıyor (A-85).");
        Assert.False(
            source.Contains("<CardMedia", StringComparison.Ordinal),
            $"Y-89 ihlali — {relativePath} kendi afişini çiziyor; afiş ortak bileşene aittir (A-85).");
    }

    [Theory(DisplayName = "Y-89: duyuru listesi sayfaları ortak AnnouncementGridCard'ı sarar")]
    [InlineData("AnnouncementsPage.tsx")]
    [InlineData("public/PublicAnnouncementsPage.tsx")]
    public void AnnouncementListPages_UseSharedCard(string relativePath)
    {
        var source = ReadPage(relativePath);

        Assert.True(
            source.Contains("AnnouncementGridCard", StringComparison.Ordinal),
            $"Y-89 ihlali — {relativePath} ortak duyuru kartını kullanmıyor (A-85).");
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
dotnet test tests/Architecture.Tests --filter "FullyQualifiedName~EventAnnouncementViewArchitectureTests"
```
Beklenen: 4 test PASS.

---

### Task 7: Tam doğrulama ve tek commit

- [x] **Step 1: Tüm testler**

```bash
dotnet test
cd arayuz && npm run build && npm run lint
```
Beklenen: iki bilinen `/api/public/stats` hatası dışında tamamı PASS.

- [x] **Step 2: Elle doğrula**

- `/events` "Yaklaşan" sekmesi: kartlar vitrindekiyle aynı görünmeli; kontenjan ve (varsa) "Üyelere özel" rozeti durmalı; "Detay"/"Katıl"/"Ayrıl" çalışmalı.
- `/events` "Etkinlikler" sekmesi: **tablo olarak kalmalı** (değişmemiş olmalı).
- `/announcements`: üç sütunlu ızgara, görsel/tarih/kulüp şeridi ve görünürlük çipi; sistem duyurusunda "Düzenle" düğmesi çalışmalı; kart **tıklanabilir olmamalı** (panelde detay rotası yok).
- `/duyurular` ve `/duyurular/:id`: bu fazdan önceki görünümle birebir aynı kalmalı; kart tıklanınca detaya gitmeli.
- Üyelere özel bir duyurunun panelde göründüğünü, vitrinde görünmediğini doğrula (Y-57 bozulmadı).

- [x] **Step 3: Commit**

```bash
git add -A -- docs/MIMARI.md docs/superpowers/plans/2026-09-10-faz-51-etkinlik-duyuru-gorunum-birligi.md tests arayuz/src
git commit -m "$(cat <<'EOF'
Faz 51: etkinlik ve duyuru listeleri panel ile vitrinde tek bilesene tasindi

Docs: docs/MIMARI.md v6.18 (K-54 ve Y-89 tadil edildi).
EOF
)"
```
