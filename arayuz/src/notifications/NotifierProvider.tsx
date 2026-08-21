import { Alert, Snackbar } from '@mui/material'
import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'

export type NotifySeverity = 'success' | 'error' | 'info' | 'warning'

interface NotifyOptions {
  message: string
  severity?: NotifySeverity
}

type NotifyFn = (options: NotifyOptions | string) => void

const NotifierContext = createContext<NotifyFn | null>(null)

export function NotifierProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<{ message: string; severity: NotifySeverity } | null>(null)

  const notify = useCallback<NotifyFn>((options) => {
    const normalized = typeof options === 'string' ? { message: options, severity: 'success' as const } : options
    setState({ message: normalized.message, severity: normalized.severity ?? 'success' })
  }, [])

  const value = useMemo(() => notify, [notify])

  return (
    <NotifierContext.Provider value={value}>
      {children}
      <Snackbar
        open={state !== null}
        autoHideDuration={4000}
        onClose={() => setState(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        {state ? (
          <Alert severity={state.severity} variant="filled">
            {state.message}
          </Alert>
        ) : undefined}
      </Snackbar>
    </NotifierContext.Provider>
  )
}

export function useNotifier(): NotifyFn {
  const context = useContext(NotifierContext)
  if (!context) {
    throw new Error('useNotifier yalnizca NotifierProvider icinde kullanilabilir.')
  }
  return context
}
