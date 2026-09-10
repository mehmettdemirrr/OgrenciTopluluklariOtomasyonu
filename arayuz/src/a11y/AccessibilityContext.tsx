import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'

const STORAGE_KEY = 'app-a11y-prefs-v2'

export type TextAlignPref = 'start' | 'center' | 'justify'
export type ContrastPref = 'off' | 'high' | 'invert'
export type SaturationPref = 'off' | 'low' | 'high'
export type PanelSide = 'left' | 'right'
export type CycleLevel = 0 | 1 | 2 | 3

export const FONT_SCALES = [1, 1.15, 1.3] as const
export const ZOOM_FACTORS = [1, 1.1, 1.25, 1.5] as const
export const LINE_HEIGHT_FACTORS = [0, 1.5, 2, 2.5] as const
export const LETTER_SPACING_FACTORS = [0, 0.1, 0.15, 0.2] as const
export const ALIGN_CYCLE = ['start', 'center', 'justify'] as const satisfies readonly TextAlignPref[]
export const CONTRAST_CYCLE = ['off', 'high', 'invert'] as const satisfies readonly ContrastPref[]
export const SATURATION_CYCLE = ['off', 'low', 'high'] as const satisfies readonly SaturationPref[]
export const LEVEL_CYCLE = [0, 1, 2, 3] as const satisfies readonly CycleLevel[]

export interface AccessibilityPreferences {
  /** 1 = %100, 1.15 = %115, 1.3 = %130 */
  fontScale: number
  contrast: ContrastPref
  reduceMotion: boolean
  underlineLinks: boolean
  saturation: SaturationPref
  /** 0 = kapalı; 1/2/3 = %110 / %125 / %150 */
  zoom: CycleLevel
  textAlign: TextAlignPref
  lineHeight: CycleLevel
  letterSpacing: CycleLevel
  largeTools: boolean
  largeCursor: boolean
  readingGuide: boolean
  readingMask: boolean
  dyslexiaFriendly: boolean
  selectionReader: boolean
  panelSide: PanelSide
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

function asLevel(value: unknown): CycleLevel {
  return value === 1 || value === 2 || value === 3 ? value : 0
}

/** Tema katmanının a11y argümansız düştüğü nötr küme (A-79). */
export const FALLBACK_PREFERENCES: AccessibilityPreferences = {
  fontScale: 1,
  contrast: 'off',
  reduceMotion: false,
  underlineLinks: false,
  saturation: 'off',
  zoom: 0,
  textAlign: 'start',
  lineHeight: 0,
  letterSpacing: 0,
  largeTools: false,
  largeCursor: false,
  readingGuide: false,
  readingMask: false,
  dyslexiaFriendly: false,
  selectionReader: false,
  panelSide: 'right',
}

/** docs/MIMARI.md · A-79: hareket azaltmanın BAŞLANGIÇ değeri sistem tercihinden gelir. */
export function defaultPreferences(): AccessibilityPreferences {
  return {
    ...FALLBACK_PREFERENCES,
    reduceMotion: systemPrefersReducedMotion(),
  }
}

function readStoredPreferences(): AccessibilityPreferences {
  const fallback = defaultPreferences()
  try {
    const raw = localStorage.getItem(STORAGE_KEY) ?? localStorage.getItem('app-a11y-prefs')
    if (!raw) {
      return fallback
    }
    const parsed = JSON.parse(raw) as Omit<
      Partial<AccessibilityPreferences>,
      'zoom' | 'lineHeight' | 'letterSpacing'
    > & {
      highContrast?: boolean
      grayscale?: boolean
      zoom?: number
      lineHeight?: number
      letterSpacing?: number
    }
    const contrast: ContrastPref =
      parsed.contrast === 'high' || parsed.contrast === 'invert'
        ? parsed.contrast
        : parsed.highContrast === true
          ? 'high'
          : 'off'
    const saturation: SaturationPref =
      parsed.saturation === 'low' || parsed.saturation === 'high'
        ? parsed.saturation
        : parsed.grayscale === true
          ? 'low'
          : 'off'
    const legacy = typeof parsed.highContrast === 'boolean' && parsed.contrast === undefined
    const zoom: CycleLevel = legacy
      ? parsed.zoom === 1.15
        ? 1
        : parsed.zoom === 1.3
          ? 2
          : 0
      : asLevel(parsed.zoom)
    const lineHeight: CycleLevel = legacy
      ? parsed.lineHeight === 1.7
        ? 1
        : parsed.lineHeight === 2
          ? 2
          : 0
      : asLevel(parsed.lineHeight)
    const letterSpacing: CycleLevel = legacy
      ? parsed.letterSpacing === 0.04
        ? 1
        : parsed.letterSpacing === 0.08
          ? 2
          : 0
      : asLevel(parsed.letterSpacing)
    return {
      fontScale: parsed.fontScale === 1.15 || parsed.fontScale === 1.3 ? parsed.fontScale : 1,
      contrast,
      reduceMotion: typeof parsed.reduceMotion === 'boolean' ? parsed.reduceMotion : fallback.reduceMotion,
      underlineLinks: parsed.underlineLinks === true,
      saturation,
      zoom,
      textAlign: parsed.textAlign === 'center' || parsed.textAlign === 'justify' ? parsed.textAlign : 'start',
      lineHeight,
      letterSpacing,
      largeTools: parsed.largeTools === true,
      largeCursor: parsed.largeCursor === true,
      readingGuide: parsed.readingGuide === true,
      readingMask: parsed.readingMask === true,
      dyslexiaFriendly: parsed.dyslexiaFriendly === true,
      selectionReader: parsed.selectionReader === true,
      panelSide: parsed.panelSide === 'left' ? 'left' : 'right',
    }
  } catch {
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
    setPrefs((current) => {
      const next = { ...defaultPreferences(), panelSide: current.panelSide }
      persist(next)
      return next
    })
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

export function nextCycle<T>(values: readonly T[], current: T): T {
  const index = values.indexOf(current)
  return values[(index < 0 ? 0 : index + 1) % values.length]
}
