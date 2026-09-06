import { alpha, createTheme } from '@mui/material'
import { enUS, trTR } from '@mui/material/locale'
import { enUS as dataGridEn, trTR as dataGridTr } from '@mui/x-data-grid/locales'
import { brand, contrastPalette, surfaces } from './tokens'
import type { AccessibilityPreferences } from '../a11y/AccessibilityContext'

declare module '@mui/material/styles' {
  interface Palette {
    accent: Palette['primary']
  }
  interface PaletteOptions {
    accent?: PaletteOptions['primary']
  }
}

const navyShadow = (opacity: number) => `0 8px 24px ${alpha(brand.navy, opacity)}`

const defaultAccessibility: AccessibilityPreferences = {
  fontScale: 1,
  highContrast: false,
  reduceMotion: false,
  underlineLinks: false,
}

export function createAppTheme(
  mode: 'light' | 'dark',
  locale: 'tr' | 'en',
  a11y: AccessibilityPreferences = defaultAccessibility,
) {
  const surface = surfaces[mode]
  const contrast = contrastPalette[mode]
  const muiLocale = locale === 'tr' ? trTR : enUS
  const gridLocale = locale === 'tr' ? dataGridTr : dataGridEn

  return createTheme(
    {
      palette: {
        mode,
        primary: {
          main: brand.turquoise,
          dark: brand.turquoiseDark,
          contrastText: '#ffffff',
        },
        secondary: {
          main: brand.navy,
          contrastText: '#ffffff',
        },
        warning: {
          main: brand.orange,
        },
        accent: {
          main: brand.gold,
          contrastText: '#ffffff',
        },
        grey: {
          400: brand.grey,
        },
        divider: a11y.highContrast ? contrast.divider : alpha(brand.grey, mode === 'dark' ? 0.28 : 0.35),
        text: a11y.highContrast
          ? { primary: contrast.textPrimary, secondary: contrast.textSecondary }
          : {
              primary: surface.text,
              secondary: surface.textMuted,
            },
        background: {
          default: surface.default,
          paper: surface.paper,
        },
      },
      shape: {
        borderRadius: 12,
      },
      transitions: a11y.reduceMotion ? { create: () => 'none' } : undefined,
      typography: {
        // A-79: tek kaynak — MUI tüm varyantları bu taban ölçüden türetir.
        fontSize: 14 * a11y.fontScale,
        fontFamily: '"InterVariable", "Inter", "Roboto", "Helvetica", "Arial", sans-serif',
        h1: { fontWeight: 800, letterSpacing: '-0.03em' },
        h2: { fontWeight: 800, letterSpacing: '-0.025em' },
        h3: { fontWeight: 800, letterSpacing: '-0.02em' },
        h4: { fontWeight: 800, letterSpacing: '-0.02em' },
        h5: { fontWeight: 700, letterSpacing: '-0.015em' },
        h6: { fontWeight: 700, letterSpacing: '-0.01em' },
        subtitle1: { fontWeight: 600 },
        subtitle2: { fontWeight: 600 },
        button: { fontWeight: 600 },
        overline: { fontWeight: 700, letterSpacing: '0.08em' },
      },
      components: {
        MuiCssBaseline: {
          styleOverrides: {
            html: {
              scrollBehavior: 'smooth',
            },
            body: {
              WebkitFontSmoothing: 'antialiased',
              MozOsxFontSmoothing: 'grayscale',
            },
            '::selection': {
              backgroundColor: alpha(brand.turquoise, 0.22),
            },
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
        MuiButton: {
          styleOverrides: {
            root: {
              textTransform: 'none',
              borderRadius: 10,
              fontWeight: 600,
            },
            sizeLarge: {
              paddingInline: 22,
              paddingBlock: 10,
            },
            contained: {
              boxShadow: 'none',
              '&:hover': {
                boxShadow: navyShadow(0.18),
              },
            },
          },
          variants: [
            {
              props: { variant: 'contained', color: 'primary' },
              style: {
                backgroundColor: brand.turquoiseDark,
                '&:hover': {
                  backgroundColor: brand.navy,
                },
              },
            },
          ],
        },
        MuiPaper: {
          styleOverrides: {
            root: {
              borderRadius: 16,
              backgroundImage: 'none',
            },
            outlined: {
              borderColor: alpha(brand.grey, mode === 'dark' ? 0.24 : 0.32),
            },
          },
          defaultProps: {
            elevation: 0,
          },
        },
        MuiCard: {
          styleOverrides: {
            root: {
              borderRadius: 16,
              overflow: 'hidden',
              border: `1px solid ${alpha(brand.grey, mode === 'dark' ? 0.24 : 0.32)}`,
              boxShadow: `0 1px 2px ${alpha(brand.navy, mode === 'dark' ? 0.28 : 0.04)}`,
              transition: 'transform 180ms ease, box-shadow 180ms ease, border-color 180ms ease',
              '&:hover': {
                transform: 'translateY(-3px)',
                borderColor: alpha(brand.turquoise, 0.45),
                boxShadow: `0 14px 32px ${alpha(brand.navy, mode === 'dark' ? 0.35 : 0.1)}`,
              },
            },
          },
        },
        MuiChip: {
          styleOverrides: {
            root: {
              fontWeight: 600,
              borderRadius: 8,
            },
          },
        },
        MuiAppBar: {
          styleOverrides: {
            root: {
              boxShadow: 'none',
              borderBottom: `1px solid ${alpha(brand.grey, mode === 'dark' ? 0.2 : 0.25)}`,
            },
          },
          defaultProps: {
            color: 'transparent',
          },
        },
        MuiTextField: {
          defaultProps: {
            size: 'small',
          },
        },
        MuiOutlinedInput: {
          styleOverrides: {
            root: {
              borderRadius: 10,
              backgroundColor: surface.paper,
              '&:hover .MuiOutlinedInput-notchedOutline': {
                borderColor: alpha(brand.turquoise, 0.55),
              },
              '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
                borderWidth: 1.5,
              },
            },
          },
        },
        MuiDialog: {
          styleOverrides: {
            paper: {
              borderRadius: 16,
            },
          },
        },
        MuiDialogTitle: {
          styleOverrides: {
            root: {
              fontWeight: 700,
            },
          },
        },
        MuiTabs: {
          styleOverrides: {
            indicator: {
              height: 3,
              borderRadius: 3,
            },
          },
        },
        MuiTab: {
          styleOverrides: {
            root: {
              textTransform: 'none',
              fontWeight: 600,
              minHeight: 48,
            },
          },
        },
        MuiToggleButton: {
          styleOverrides: {
            root: {
              textTransform: 'none',
              fontWeight: 700,
              borderRadius: 10,
            },
          },
        },
        MuiAlert: {
          styleOverrides: {
            root: {
              borderRadius: 12,
            },
          },
        },
        MuiTooltip: {
          styleOverrides: {
            tooltip: {
              borderRadius: 8,
              fontSize: 12,
            },
          },
        },
        MuiLink: {
          styleOverrides: {
            root: {
              fontWeight: 600,
            },
          },
        },
        MuiListItemButton: {
          styleOverrides: {
            root: {
              borderRadius: 10,
            },
          },
        },
        MuiMenu: {
          styleOverrides: {
            paper: {
              borderRadius: 12,
              border: `1px solid ${alpha(brand.grey, mode === 'dark' ? 0.22 : 0.28)}`,
              boxShadow: `0 16px 40px ${alpha(brand.navy, mode === 'dark' ? 0.4 : 0.14)}`,
            },
          },
        },
        MuiPaginationItem: {
          styleOverrides: {
            root: {
              fontWeight: 600,
            },
          },
        },
      },
    },
    muiLocale,
    gridLocale,
  )
}
