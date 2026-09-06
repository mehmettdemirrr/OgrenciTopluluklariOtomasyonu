# Faz 44 — Erişilebilirlik Paneli Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Her sayfada sol altta duran bir erişilebilirlik düğmesi olsun; açılan panelden yazı boyutu, yüksek kontrast, hareketi azaltma ve bağlantı altı çizgisi ayarlanabilsin, tercihler kalıcı olsun ve klavye kullanıcısı için "içeriğe atla" bağlantısı bulunsun.

**Architecture:** Tercihler tek bir bağlamda (`AccessibilityProvider`) yaşar ve **tema katmanında** çözülür: `createAppTheme` bir `AccessibilityPreferences` argümanı alır, yazı ölçeğini `typography.fontSize` üzerinden, kontrastı palet/odak halkası üzerinden, hareket azaltmayı `transitions` ve `CssBaseline` üzerinden uygular. Hiçbir bileşen tercihi kendisi okumaz — herkes temadan boyanır. Renkler `theme/tokens.ts` dışında hiçbir yerde hex olarak yazılmaz (Y-56).

**Tech Stack:** React 18 + MUI 9 (yeni bağımlılık yok), .NET 8 (yalnızca mimari test).

**Spec:** `docs/MIMARI.md` (v6.10 → v6.11 bu fazda). İlgili kararlar: A-37 (kurumsal renk sistemi), Y-56 (marka renkleri tek yerde), A-72 (token → mod eşlemesi precedent'i).

## Global Constraints

- Belge önce, kod sonra (Task 1).
- Çalışma dalı `master`; faz başına **tek commit**; commit mesajında araç imzası/süreç anlatımı yok.
- **Backend'e dokunulmaz.** Tercihler yalnızca tarayıcıda (`localStorage`) durur; kullanıcı profiline yazılmaz.
- **Y-56:** yeni hex değerleri **yalnızca** `arayuz/src/theme/tokens.ts` içine eklenir; bileşenlerde/tema dosyasında düz hex yazılmaz.
- Yeni yazı tipi dosyası projeye **girmez** (disleksi fontu bu fazın dışında) — paket boyutu ve lisans yükü getirir.
- Varsayılan değerler: yazı ölçeği %100, yüksek kontrast kapalı, bağlantı altı çizgisi kapalı; **hareketi azalt** ise sistem tercihinden (`prefers-reduced-motion: reduce`) başlar.
- Test komutları: `dotnet test`; `cd arayuz && npm run build && npm run lint`.

### Bu planın kabul ettiği varsayım (kullanıcı onayı alınamadı, oturum "soru sorma" modundaydı)

Panel kapsamı **standart pakettir**: yazı boyutu (3 kademe), yüksek kontrast, hareketi azalt, bağlantıların altını çiz, sıfırla + "içeriğe atla" bağlantısı. Tema (açık/koyu) ve dil zaten üst bardaki `PreferenceControls` içinde olduğu için panele **tekrar konmaz** — aynı ayarın iki yeri olması, hangisinin kazandığı sorusunu doğurur.

---

### Task 1: MIMARI'ye kapsam, karar ve kuralı yaz (belge önce)

**Files:**
- Modify: `docs/MIMARI.md`

**Interfaces:**
- Produces: K-48, A-79, Y-84.

- [x] **Step 1: Sürüm ve sayaçlar**

`**Sürüm:** v6.10` → `v6.11`; kronolojik listenin sonuna kalın olarak:

```markdown
· **v6.11: 6 Eylül 2026 (K-48, A-79, Y-84, Faz 44 — erişilebilirlik paneli)**
```

Giriş paragrafının sonuna: `**v6.11** her sayfaya erişilebilirlik tercihleri panelini ekleyerek 1 kapsam maddesi (K-48), 1 karar (A-79) ve 1 kural (Y-84) ekledi.`
Sayaçlar: `Karar | 79 (… + 1 v6.11)`, `Yasak kural | 84 (… + 1 v6.11)`, `Uygulama fazı | 44 (… + 1 v6.11)`.
İçindekiler: `Y-01 … Y-84`, `K-01 … K-48`, `A-01 … A-79`.

- [x] **Step 2: K-48**

```markdown
| **K-48** | **Erişilebilirlik tercihleri** | Her sayfada sol altta bir erişilebilirlik düğmesi vardır; panelden yazı boyutu (%100/%115/%130), yüksek kontrast, hareketi azalt ve bağlantıların altını çiz ayarlanır, tek düğmeyle sıfırlanır. Klavye kullanıcısı için sayfanın ilk odağı "içeriğe atla" bağlantısıdır. Tercihler tarayıcıda saklanır, sunucuya gitmez | `AccessibilityProvider` + `createAppTheme(mode, locale, a11y)`, `SkipToContentLink`, `localStorage` (A-79, Y-84) |
```

- [x] **Step 3: A-79**

```markdown
| **A-79** | Erişilebilirlik tercihleri **tema katmanında** çözülür | A | Tercihler tek bir bağlamda tutulur ve `createAppTheme`'e argüman olarak girer: yazı ölçeği `typography.fontSize`, yüksek kontrast palet + odak halkası, hareket azaltma `transitions` ve `CssBaseline` üzerinden uygulanır. Bileşenler tercihi okumaz, yalnızca temadan boyanır (Y-84). Yüksek kontrast renkleri `theme/tokens.ts` içindeki `contrastPalette`'ten gelir — Y-56'nın istisnası değil, A-72 gibi **uzantısıdır**. Tercihler kullanıcı profiline değil `localStorage`'a yazılır: cihaza özgü bir ayardır, sunucuya taşımak yeni bir uç, yeni bir migration ve "hangi cihaz kazanır" sorusu demektir. Hareketi azalt ayarının başlangıç değeri `prefers-reduced-motion` sistem tercihinden okunur (K-48) |
```

- [x] **Step 4: Y-84**

```markdown
| **Y-84** | Erişilebilirlik tercihini bir bileşenin içinde okuyup elle stil uygulamak (`if (fontScale === 130) …`, `style={{ fontSize: … }}`); aynı ayarı panelin dışında ikinci bir yerde tanımlamak | Tercih yalnızca `AccessibilityProvider` ve `createAppTheme` tarafından okunur; bileşenler sonucu temadan alır. Gerekçe: ayarı okuyan her bileşen, ayar değiştiğinde güncellenmesi gereken yeni bir yerdir — bir tanesi unutulduğunda kullanıcı "bazı yerler büyüdü, bazıları büyümedi" ile kalır. İhlali `Architecture.Tests` kaynak taramasıyla yakalar: `useAccessibility` yalnızca `arayuz/src/a11y/` altında ve tema sağlayıcısında geçebilir (K-48, A-79) |
```

---

### Task 2: Tercih bağlamı ve depolama

**Files:**
- Create: `arayuz/src/a11y/AccessibilityContext.tsx`
- Modify: `arayuz/src/App.tsx`

**Interfaces:**
- Produces: `AccessibilityPreferences { fontScale: 1 | 1.15 | 1.3; highContrast: boolean; reduceMotion: boolean; underlineLinks: boolean }`, `AccessibilityProvider`, `useAccessibility()` → `{ prefs, setPref, reset }`.

- [x] **Step 1: Bağlamı yaz**

```tsx
import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'

const STORAGE_KEY = 'app-a11y-prefs'

export interface AccessibilityPreferences {
  /** 1 = %100, 1.15 = %115, 1.3 = %130 */
  fontScale: number
  highContrast: boolean
  reduceMotion: boolean
  underlineLinks: boolean
}

interface AccessibilityContextValue {
  prefs: AccessibilityPreferences
  setPref: <K extends keyof AccessibilityPreferences>(key: K, value: AccessibilityPreferences[K]) => void
  reset: () => void
}

const AccessibilityContext = createContext<AccessibilityContextValue | null>(null)

function systemPrefersReducedMotion(): boolean {
  return typeof window !== 'undefined' && window.matchMedia?.('(prefers-reduced-motion: reduce)').matches === true
}

/** docs/MIMARI.md · A-79: hareket azaltmanın BAŞLANGIÇ değeri sistem tercihinden gelir. */
function defaultPreferences(): AccessibilityPreferences {
  return { fontScale: 1, highContrast: false, reduceMotion: systemPrefersReducedMotion(), underlineLinks: false }
}

function readStoredPreferences(): AccessibilityPreferences {
  const fallback = defaultPreferences()
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) {
      return fallback
    }
    const parsed = JSON.parse(raw) as Partial<AccessibilityPreferences>
    return {
      fontScale: parsed.fontScale === 1.15 || parsed.fontScale === 1.3 ? parsed.fontScale : 1,
      highContrast: parsed.highContrast === true,
      reduceMotion: typeof parsed.reduceMotion === 'boolean' ? parsed.reduceMotion : fallback.reduceMotion,
      underlineLinks: parsed.underlineLinks === true,
    }
  } catch {
    // Bozuk/erişilemez depolama sayfayı kırmasın — varsayılana düş.
    return fallback
  }
}

export function AccessibilityProvider({ children }: { children: ReactNode }) {
  const [prefs, setPrefs] = useState<AccessibilityPreferences>(readStoredPreferences)

  const persist = useCallback((next: AccessibilityPreferences) => {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(next))
    } catch {
      // Depolama kapalıysa tercih yalnızca bu oturumda geçerli olur.
    }
  }, [])

  const setPref = useCallback<AccessibilityContextValue['setPref']>((key, value) => {
    setPrefs((current) => {
      const next = { ...current, [key]: value }
      persist(next)
      return next
    })
  }, [persist])

  const reset = useCallback(() => {
    const next = defaultPreferences()
    persist(next)
    setPrefs(next)
  }, [persist])

  const value = useMemo(() => ({ prefs, setPref, reset }), [prefs, setPref, reset])

  return <AccessibilityContext.Provider value={value}>{children}</AccessibilityContext.Provider>
}

export function useAccessibility() {
  const ctx = useContext(AccessibilityContext)
  if (!ctx) {
    throw new Error('useAccessibility must be used within AccessibilityProvider')
  }
  return ctx
}
```

- [x] **Step 2: Sağlayıcıyı App.tsx'e tak**

`LocaleProvider` ile `ThemeModeProvider` **arasına** koy — tema, tercihleri okuyacak:

```tsx
    <LocaleProvider>
      <AccessibilityProvider>
        <ThemeModeProvider>
          <CssBaseline />
          {/* … mevcut ağaç aynen … */}
        </ThemeModeProvider>
      </AccessibilityProvider>
    </LocaleProvider>
```

- [x] **Step 3: Derle**

```bash
cd arayuz && npm run build
```

---

### Task 3: Tercihlerin temada uygulanması

**Files:**
- Modify: `arayuz/src/theme/tokens.ts`
- Modify: `arayuz/src/theme/createAppTheme.ts`
- Modify: `arayuz/src/theme/ThemeModeContext.tsx`

**Interfaces:**
- Consumes: `AccessibilityPreferences` (Task 2).
- Produces: `createAppTheme(mode, locale, a11y)`.

- [x] **Step 1: Kontrast token'larını ekle (Y-56: hex yalnızca burada)**

`arayuz/src/theme/tokens.ts` sonuna:

```ts
/**
 * docs/MIMARI.md · A-79/Y-56: yüksek kontrast modunun renkleri. Y-56'nın istisnası değil,
 * A-72 gibi uzantısıdır — hex değerleri yine yalnızca bu dosyada.
 */
export const contrastPalette = {
  light: {
    textPrimary: '#0A0A0A',
    textSecondary: '#2B2B2B',
    divider: '#000000',
    focusRing: '#0B6E87',
  },
  dark: {
    textPrimary: '#FFFFFF',
    textSecondary: '#E8E8E8',
    divider: '#FFFFFF',
    focusRing: '#7FD8F0',
  },
} as const
```

- [x] **Step 2: `createAppTheme` imzasını genişlet**

```ts
import { brand, contrastPalette, surfaces } from './tokens'
import type { AccessibilityPreferences } from '../a11y/AccessibilityContext'

const defaultAccessibility: AccessibilityPreferences = {
  fontScale: 1, highContrast: false, reduceMotion: false, underlineLinks: false,
}

export function createAppTheme(
  mode: 'light' | 'dark',
  locale: 'tr' | 'en',
  a11y: AccessibilityPreferences = defaultAccessibility,
) {
  const surface = surfaces[mode]
  const contrast = contrastPalette[mode]
  // … mevcut muiLocale/gridLocale satırları aynen …
```

- [x] **Step 3: Yazı ölçeğini uygula**

Mevcut `typography: { … }` bloğunun içine, diğer alanların yanına:

```ts
      typography: {
        // A-79: tek kaynak — MUI tüm varyantları bu taban ölçüden türetir.
        fontSize: 14 * a11y.fontScale,
        // … mevcut alanlar aynen kalır …
      },
```

- [x] **Step 4: Yüksek kontrastı uygula**

`palette` bloğunun sonuna (mevcut `text`/`divider` tanımları varsa onları koşullu hâle getir):

```ts
        text: a11y.highContrast
          ? { primary: contrast.textPrimary, secondary: contrast.textSecondary }
          : undefined,
        divider: a11y.highContrast ? contrast.divider : undefined,
```

**Not:** `undefined` geçmek MUI'nin varsayılanını korur; mevcut dosyada `text`/`divider` zaten tanımlıysa `a11y.highContrast ? … : mevcutDeğer` biçiminde yaz, mevcut değeri silme.

- [x] **Step 5: Odak halkası, hareket azaltma ve bağlantı altı çizgisini uygula**

`components` bloğuna ekle (`MuiCssBaseline` zaten varsa `styleOverrides`ini genişlet):

```ts
        MuiCssBaseline: {
          styleOverrides: {
            // A-79: klavye odağı her zaman görünür; yüksek kontrastta daha kalın.
            '*:focus-visible': {
              outline: `${a11y.highContrast ? 3 : 2}px solid ${contrast.focusRing}`,
              outlineOffset: 2,
            },
            ...(a11y.reduceMotion
              ? {
                  '*, *::before, *::after': {
                    animationDuration: '0.01ms !important',
                    animationIterationCount: '1 !important',
                    transitionDuration: '0.01ms !important',
                    scrollBehavior: 'auto !important',
                  },
                }
              : {}),
            ...(a11y.underlineLinks
              ? { 'a:not(.MuiButtonBase-root)': { textDecoration: 'underline !important' } }
              : {}),
          },
        },
```

ve `createTheme` nesnesinin köküne (palette/typography ile aynı seviye):

```ts
      transitions: a11y.reduceMotion ? { create: () => 'none' } : {},
```

- [x] **Step 6: Sağlayıcıyı bağla**

`arayuz/src/theme/ThemeModeContext.tsx`:

```tsx
import { useAccessibility } from '../a11y/AccessibilityContext'
// …
export function ThemeModeProvider({ children }: { children: ReactNode }) {
  const { locale } = useLocale()
  const { prefs } = useAccessibility()
  const [mode, setMode] = useState<ThemeMode>(readStoredMode)
  // …
  const theme = useMemo(() => createAppTheme(mode, locale, prefs), [mode, locale, prefs])
```

- [x] **Step 7: Derle ve gözle doğrula**

```bash
cd arayuz && npm run build
```
Geliştirme sunucusunda `localStorage.setItem('app-a11y-prefs', JSON.stringify({ fontScale: 1.3, highContrast: true, reduceMotion: true, underlineLinks: true }))` yazıp sayfayı yenile: yazılar büyümeli, metin kararmalı, geçişler kesilmeli, bağlantıların altı çizilmeli.

---

### Task 4: Erişilebilirlik düğmesi ve paneli

**Files:**
- Create: `arayuz/src/a11y/AccessibilityFab.tsx`
- Modify: `arayuz/src/App.tsx`
- Modify: `arayuz/src/i18n/messages.ts`

**Interfaces:**
- Consumes: `useAccessibility()` (Task 2).

- [x] **Step 1: i18n anahtarları (tr + en)**

```ts
    'a11y.title': 'Erişilebilirlik',                    // 'Accessibility'
    'a11y.open': 'Erişilebilirlik ayarlarını aç',       // 'Open accessibility settings'
    'a11y.fontSize': 'Yazı boyutu',                     // 'Text size'
    'a11y.highContrast': 'Yüksek kontrast',             // 'High contrast'
    'a11y.reduceMotion': 'Hareketi azalt',              // 'Reduce motion'
    'a11y.underlineLinks': 'Bağlantıların altını çiz',  // 'Underline links'
    'a11y.reset': 'Sıfırla',                            // 'Reset'
    'a11y.skipToContent': 'İçeriğe atla',               // 'Skip to content'
```

- [x] **Step 2: Bileşeni yaz**

```tsx
import { Box, Fab, FormControlLabel, Popover, Stack, Switch, ToggleButton, ToggleButtonGroup, Tooltip, Typography, Button, Divider } from '@mui/material'
import AccessibilityNewRoundedIcon from '@mui/icons-material/AccessibilityNewRounded'
import { useState } from 'react'
import { useAccessibility } from './AccessibilityContext'
import { useLocale } from '../i18n/LocaleContext'

/** docs/MIMARI.md · K-48: her sayfada sol altta duran erişilebilirlik paneli. */
export function AccessibilityFab() {
  const { t } = useLocale()
  const { prefs, setPref, reset } = useAccessibility()
  const [anchor, setAnchor] = useState<HTMLElement | null>(null)

  return (
    <>
      <Tooltip title={t('a11y.open')} placement="right">
        <Fab
          color="primary"
          size="medium"
          aria-label={t('a11y.open')}
          onClick={(event) => setAnchor(event.currentTarget)}
          sx={{ position: 'fixed', left: 16, bottom: 16, zIndex: (theme) => theme.zIndex.drawer + 2 }}
        >
          <AccessibilityNewRoundedIcon />
        </Fab>
      </Tooltip>

      <Popover
        open={Boolean(anchor)}
        anchorEl={anchor}
        onClose={() => setAnchor(null)}
        anchorOrigin={{ vertical: 'top', horizontal: 'left' }}
        transformOrigin={{ vertical: 'bottom', horizontal: 'left' }}
        slotProps={{ paper: { sx: { p: 2, width: 280, borderRadius: 3 } } }}
      >
        <Typography variant="subtitle1" sx={{ fontWeight: 800, mb: 1.5 }}>
          {t('a11y.title')}
        </Typography>

        <Typography variant="caption" color="text.secondary">
          {t('a11y.fontSize')}
        </Typography>
        <ToggleButtonGroup
          exclusive
          fullWidth
          size="small"
          value={prefs.fontScale}
          onChange={(_, value: number | null) => {
            if (value) setPref('fontScale', value)
          }}
          aria-label={t('a11y.fontSize')}
          sx={{ mb: 1.5, mt: 0.5 }}
        >
          <ToggleButton value={1}>A</ToggleButton>
          <ToggleButton value={1.15} sx={{ fontSize: '1.05rem' }}>A</ToggleButton>
          <ToggleButton value={1.3} sx={{ fontSize: '1.2rem' }}>A</ToggleButton>
        </ToggleButtonGroup>

        <Divider sx={{ mb: 1 }} />

        <Stack>
          <FormControlLabel
            control={<Switch checked={prefs.highContrast} onChange={(_, checked) => setPref('highContrast', checked)} />}
            label={t('a11y.highContrast')}
          />
          <FormControlLabel
            control={<Switch checked={prefs.reduceMotion} onChange={(_, checked) => setPref('reduceMotion', checked)} />}
            label={t('a11y.reduceMotion')}
          />
          <FormControlLabel
            control={<Switch checked={prefs.underlineLinks} onChange={(_, checked) => setPref('underlineLinks', checked)} />}
            label={t('a11y.underlineLinks')}
          />
        </Stack>

        <Box sx={{ mt: 1.5 }}>
          <Button fullWidth variant="outlined" onClick={reset}>
            {t('a11y.reset')}
          </Button>
        </Box>
      </Popover>
    </>
  )
}
```

- [x] **Step 3: Uygulamaya tak**

`App.tsx` içinde `<BrowserRouter>`'ın **içinde**, `<Routes>`'un hemen ardına koy — böylece hem public, hem panel, hem giriş ekranlarında görünür:

```tsx
                </Routes>
                <AccessibilityFab />
```

- [x] **Step 4: Derle ve lint'le**

```bash
cd arayuz && npm run build && npm run lint
```

---

### Task 5: "İçeriğe atla" bağlantısı

**Files:**
- Create: `arayuz/src/a11y/SkipToContentLink.tsx`
- Modify: `arayuz/src/components/layout/AppShell.tsx`
- Modify: `arayuz/src/components/layout/PublicLayout.tsx`
- Modify: `arayuz/src/App.tsx`

- [x] **Step 1: Bağlantıyı yaz**

Sayfanın ilk odaklanabilir öğesi olmalı; odak almadan görünmez, odakta belirir:

```tsx
import { Box } from '@mui/material'
import { useLocale } from '../i18n/LocaleContext'

/** docs/MIMARI.md · K-48: klavye kullanıcısı menüyü atlayıp doğrudan içeriğe gider. */
export function SkipToContentLink() {
  const { t } = useLocale()

  return (
    <Box
      component="a"
      href="#main-content"
      sx={{
        position: 'absolute',
        left: 8,
        top: -64,
        zIndex: (theme) => theme.zIndex.tooltip + 1,
        px: 2,
        py: 1,
        borderRadius: 2,
        bgcolor: 'primary.dark',
        color: 'common.white',
        textDecoration: 'none',
        '&:focus-visible': { top: 8 },
      }}
    >
      {t('a11y.skipToContent')}
    </Box>
  )
}
```

- [x] **Step 2: Hedefi işaretle**

`AppShell.tsx` ve `PublicLayout.tsx` içindeki içerik `Container`'ına ekle (ikisinde de):

```tsx
        <Container id="main-content" component="main" tabIndex={-1} maxWidth="lg" sx={{ /* mevcut sx aynen */ }}>
          <Outlet />
        </Container>
```

- [x] **Step 3: Bağlantıyı en üste tak**

`App.tsx`, `<BrowserRouter>` açılışının hemen ardına (Routes'tan **önce**):

```tsx
              <SkipToContentLink />
```

- [x] **Step 4: Klavyeyle doğrula**

`npm run dev` ile aç, sayfa yüklenince ilk `Tab`: "İçeriğe atla" görünmeli; `Enter` içeriğe atlamalı. Sonraki `Tab`'larda odak halkası her öğede görünür olmalı.

---

### Task 6: Y-84 mimari testi

**Files:**
- Create: `tests/Architecture.Tests/AccessibilityArchitectureTests.cs`

- [x] **Step 1: Testi yaz**

```csharp
using Xunit;

namespace Architecture.Tests;

/// <summary>docs/MIMARI.md · Y-84: erişilebilirlik tercihi bileşenlerde okunmaz, temadan gelir.</summary>
public class AccessibilityArchitectureTests
{
    [Fact(DisplayName = "Y-84: useAccessibility yalnızca a11y klasöründe ve tema sağlayıcısında kullanılır")]
    public void Preferences_AreReadOnlyByProviderAndTheme()
    {
        var sourceRoot = Path.Combine(SolutionPaths.FindRepositoryRoot(), "arayuz", "src");
        Assert.True(Directory.Exists(sourceRoot), $"Kaynak dizini bulunamadı: {sourceRoot}");

        var allowed = new[]
        {
            Path.Combine("a11y", "AccessibilityContext.tsx"),
            Path.Combine("a11y", "AccessibilityFab.tsx"),
            Path.Combine("theme", "ThemeModeContext.tsx"),
        };

        var offenders = Directory
            .EnumerateFiles(sourceRoot, "*.tsx", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(sourceRoot, "*.ts", SearchOption.AllDirectories))
            .Where(path => File.ReadAllText(path).Contains("useAccessibility(", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(sourceRoot, path))
            .Where(relative => !allowed.Contains(relative, StringComparer.OrdinalIgnoreCase))
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"Y-84 ihlali — erişilebilirlik tercihi bileşende okunuyor: {string.Join(", ", offenders)}. " +
            "Sonucu temadan al (A-79); tercihi bileşende okuma.");
    }
}
```

- [x] **Step 2: Çalıştır**

```bash
dotnet test tests/Architecture.Tests --filter "FullyQualifiedName~AccessibilityArchitectureTests"
```
Beklenen: PASS. Kırmızıysa tercihi okuyan bileşeni temaya taşı — izin listesini genişletme.

---

### Task 7: Tam doğrulama ve tek commit

- [x] **Step 1: Tüm testler**

```bash
dotnet test
cd arayuz && npm run build && npm run lint
```
Beklenen: iki bilinen `/api/public/stats` hatası dışında tamamı PASS.

- [x] **Step 2: Elle doğrula**

- Sol alttaki düğme her sayfada (giriş, vitrin, panel) görünmeli ve klavyeyle açılabilmeli.
- %130 yazı boyutunda menü/tablo/formlar taşmamalı; taşan yer varsa `sx` ile değil, ilgili bileşenin `minWidth`/`noWrap` ayarıyla düzelt.
- Yüksek kontrast + koyu tema birlikte okunabilir olmalı.
- Sayfayı yenileyince tercihler korunmalı; "Sıfırla" varsayılanlara dönmeli.
- İşletim sisteminde "hareketi azalt" açıkken uygulama ilk açılışta bu ayarla gelmeli.

- [x] **Step 3: Commit**

```bash
git add -A -- docs/MIMARI.md docs/superpowers/plans/2026-09-06-faz-44-erisilebilirlik-paneli.md tests arayuz/src
git commit -m "$(cat <<'EOF'
Faz 44: erisilebilirlik tercihleri paneli

Docs: docs/MIMARI.md v6.11 (K-48, A-79, Y-84).
EOF
)"
```
