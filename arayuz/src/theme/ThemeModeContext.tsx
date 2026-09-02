import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import { ThemeProvider } from '@mui/material'
import { useLocale } from '../i18n/LocaleContext'
import { createAppTheme } from './createAppTheme'

const STORAGE_KEY = 'app-theme-mode'

type ThemeMode = 'light' | 'dark'

interface ThemeModeContextValue {
  mode: ThemeMode
  toggleMode: () => void
}

const ThemeModeContext = createContext<ThemeModeContextValue | null>(null)

function readStoredMode(): ThemeMode {
  const stored = localStorage.getItem(STORAGE_KEY)
  return stored === 'dark' || stored === 'light' ? stored : 'light'
}

export function ThemeModeProvider({ children }: { children: ReactNode }) {
  const { locale } = useLocale()
  const [mode, setMode] = useState<ThemeMode>(readStoredMode)

  const toggleMode = useCallback(() => {
    setMode((current) => {
      const next = current === 'light' ? 'dark' : 'light'
      localStorage.setItem(STORAGE_KEY, next)
      return next
    })
  }, [])

  const theme = useMemo(() => createAppTheme(mode, locale), [mode, locale])
  const value = useMemo(() => ({ mode, toggleMode }), [mode, toggleMode])

  return (
    <ThemeModeContext.Provider value={value}>
      <ThemeProvider theme={theme}>{children}</ThemeProvider>
    </ThemeModeContext.Provider>
  )
}

export function useThemeMode() {
  const ctx = useContext(ThemeModeContext)
  if (!ctx) {
    throw new Error('useThemeMode must be used within ThemeModeProvider')
  }
  return ctx
}
