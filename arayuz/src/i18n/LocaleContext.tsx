import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { messages, type Locale } from './messages'

const STORAGE_KEY = 'app-locale'

type Translate = (key: string, vars?: Record<string, string | number>) => string

interface LocaleContextValue {
  locale: Locale
  setLocale: (locale: Locale) => void
  t: Translate
  dateLocale: string
}

const LocaleContext = createContext<LocaleContextValue | null>(null)

function readStoredLocale(): Locale {
  const stored = localStorage.getItem(STORAGE_KEY)
  return stored === 'en' || stored === 'tr' ? stored : 'tr'
}

function lookup(tree: unknown, path: string): string {
  let current: unknown = tree
  for (const part of path.split('.')) {
    if (current && typeof current === 'object' && part in current) {
      current = (current as Record<string, unknown>)[part]
    } else {
      return path
    }
  }
  return typeof current === 'string' ? current : path
}

export function LocaleProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>(readStoredLocale)

  const setLocale = useCallback((next: Locale) => {
    setLocaleState(next)
    localStorage.setItem(STORAGE_KEY, next)
  }, [])

  useEffect(() => {
    document.documentElement.lang = locale
  }, [locale])

  const t = useCallback<Translate>(
    (key, vars) => {
      let text = lookup(messages[locale], key)
      if (vars) {
        for (const [name, value] of Object.entries(vars)) {
          text = text.replaceAll(`{${name}}`, String(value))
        }
      }
      return text
    },
    [locale],
  )

  const value = useMemo(
    () => ({ locale, setLocale, t, dateLocale: locale === 'tr' ? 'tr-TR' : 'en-US' }),
    [locale, setLocale, t],
  )

  return <LocaleContext.Provider value={value}>{children}</LocaleContext.Provider>
}

export function useLocale() {
  const ctx = useContext(LocaleContext)
  if (!ctx) {
    throw new Error('useLocale must be used within LocaleProvider')
  }
  return ctx
}
