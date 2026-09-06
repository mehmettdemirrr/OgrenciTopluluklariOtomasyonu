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
